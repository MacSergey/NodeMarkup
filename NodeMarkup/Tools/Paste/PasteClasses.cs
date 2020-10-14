using ColossalFramework.Math;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.Tools
{
    public abstract class PasteItem
    {
        protected abstract float BoundsSize { get; }
        private Bounds _bounds;
        public Vector3 Position
        {
            get => _bounds.center;
            protected set => _bounds.center = value;
        }
        public int Num { get; }

        public PasteItem(int num, Vector3? position = null)
        {
            _bounds = new Bounds(position ?? Vector3.zero, Vector3.one * BoundsSize);
            Num = num;
        }

        public virtual void Update<SourceType, TargetType>(BasePasteMarkupToolMode<SourceType, TargetType> toolMode)
            where SourceType : Source<TargetType>
            where TargetType : Target
        {
            Position = GetPosition(toolMode);
        }
        protected virtual Vector3 GetPosition<SourceType, TargetType>(BasePasteMarkupToolMode<SourceType, TargetType> toolMode)
            where SourceType : Source<TargetType>
            where TargetType : Target
        {
            return Position;
        }

        public bool IsHover(Ray ray) => _bounds.IntersectRay(ray);
    }
    public abstract class Source<TargetType> : PasteItem
        where TargetType : Target
    {
        public virtual TargetType Target { get; set; }
        public bool HasTarget => Target != null;

        public Source(int num, Vector3? position = null) : base(num, position) { }

        public void Render<SourceType>(RenderManager.CameraInfo cameraInfo, BasePasteMarkupToolMode<SourceType, TargetType> toolMode)
            where SourceType : Source<TargetType>
        {
            var hue = (byte)(toolMode.SelectedSource == this || toolMode.HoverSource == this ? 255 : 192);
            var position = toolMode.SelectedSource == this ? (toolMode.IsHoverTarget ? toolMode.HoverTarget.Position : NodeMarkupTool.MouseWorldPosition) : Position;
            var size = BoundsSize;
            while (size > 0)
            {
                NodeMarkupTool.RenderCircle(cameraInfo, Colors.GetOverlayColor(Num, 255, hue), position, size);
                size -= 0.43f;
            }
        }
    }
    public abstract class Target : PasteItem
    {
        protected Vector3 ZeroPosition { get; }
        public Target(int num, Vector3 zeroPosition) : base(num) 
        {
            ZeroPosition = zeroPosition;
        }

        public void Render<SourceType, TargetType>(RenderManager.CameraInfo cameraInfo, BasePasteMarkupToolMode<SourceType, TargetType> toolMode)
            where SourceType : Source<TargetType>
            where TargetType : Target
        {
            if (toolMode.AvailableTargets.Contains(this))
            {
                NodeMarkupTool.RenderCircle(cameraInfo, Colors.White, Position, BoundsSize, false);
                if (toolMode.IsSelectedSource)
                {
                    if (toolMode.HoverTarget == this && toolMode.SelectedSource.Target != this)
                        NodeMarkupTool.RenderCircle(cameraInfo, Colors.Green, Position, BoundsSize + 0.43f);
                    else if (toolMode.HoverTarget != this && toolMode.SelectedSource.Target == this)
                        NodeMarkupTool.RenderCircle(cameraInfo, Colors.Red, Position, BoundsSize + 0.43f);
                }
            }
            else
                NodeMarkupTool.RenderCircle(cameraInfo, new Color32(192, 192, 192, 255), Position, BoundsSize, false);
        }
    }

    public class SourceEnter : Source<TargetEnter>
    {
        public static float Size => 2f;
        protected override float BoundsSize => Size;

        public bool IsMirror { get; set; }
        public EnterData Enter { get; }

        private TargetEnter _target;

        public override TargetEnter Target
        {
            get => _target;
            set
            {
                _target = value;

                for (var i = 0; i < Points.Length; i += 1)
                    Points[i].Target = _target != null && i < _target.Enter.Points ? _target.Points[!IsMirror ? i : _target.Points.Length - i - 1] : null;
            }
        }
        public SourcePoint[] Points { get; }

        public SourceEnter(EnterData enter, int num) : base(num)
        {
            Enter = enter;
            Points = Enumerable.Range(0, Enter.Points).Select(i => new SourcePoint(i)).ToArray();
        }
        protected override Vector3 GetPosition<SourceType, TargetType>(BasePasteMarkupToolMode<SourceType, TargetType> toolMode)
        {
            if (Target == null)
            {
                return Vector3.zero;
                //var i = toolMode.Sources.Take(Num).Count(s => s.Target == null);
                //return toolMode.Basket.Position + toolMode.Basket.Direction * ((TargetEnter.Size * (i + 1) + Size * i - toolMode.Basket.Width) / 2);
            }
            else
                return Target.Position;
        }
    }
    public class TargetEnter : Target
    {
        public static float Size => 3f;
        protected override float BoundsSize => Size;

        public EnterData Enter { get; }

        public TargetPoint[] Points { get; }

        public TargetEnter(Enter enter, int num) : base(num, enter.Position.Value)
        {
            Enter = enter.Data;
            Points = enter.Points.Select((p, i) => new TargetPoint(p, i)).ToArray();
        }
        public override void Update<SourceType, TargetType>(BasePasteMarkupToolMode<SourceType, TargetType> toolMode)
        {
            base.Update(toolMode);

            foreach (var point in Points)
                point.Update(toolMode);
        }
        protected override Vector3 GetPosition<SourceType, TargetType>(BasePasteMarkupToolMode<SourceType, TargetType> toolMode)
        {
            var dir = (ZeroPosition - toolMode.Markup.Position).normalized;
            var normal = dir.Turn90(true);

            Line2.Intersect(toolMode.Centre.XZ(), (toolMode.Centre + normal).XZ(), toolMode.Markup.Position.XZ(), (toolMode.Markup.Position + dir).XZ(), out float p, out _);
            var point = toolMode.Centre + normal * p;
            var distance = Mathf.Sqrt(Mathf.Pow(toolMode.Radius, 2) - Mathf.Pow(Math.Abs(p), 2));
            return point + dir * distance;
        }
    }

    public class SourcePoint : Source<TargetPoint>
    {
        protected override float BoundsSize => 0.5f;

        public SourcePoint(int num) : base(num) { }
        protected override Vector3 GetPosition<SourceType, TargetType>(BasePasteMarkupToolMode<SourceType, TargetType> toolMode)
        {
            if (Target == null)
                return Vector3.zero;
            else
                return Target.Position;
        }
    }
    public class TargetPoint : Target
    {
        protected override float BoundsSize => 1f;
        public TargetPoint(MarkupEnterPoint point, int num) : base(num, point.ZeroPosition) { }
        protected override Vector3 GetPosition<SourceType, TargetType>(BasePasteMarkupToolMode<SourceType, TargetType> toolMode) => ZeroPosition;
    }

    public abstract class Basket<SourceType, TargetType>
        where SourceType : Source<TargetType>
        where TargetType : Target
    {
        public Vector3 Position { get; private set; }
        public Vector3 Direction { get; private set; }
        protected abstract bool IsBoth { get; }

        public SourceType[] Items { get; }

        public Basket(IEnumerable<SourceType> items)
        {
            Items = items.ToArray();
        }
    }

    public class Basket
    {
        private PasteMarkupEntersOrderToolMode ToolMode { get; }

        public Vector3 Position { get; private set; }
        public Vector3 Direction { get; private set; }
        public float Width { get; private set; }

        public int Count { get; set; }
        public bool IsEmpty => Count == 0;

        public Basket(PasteMarkupEntersOrderToolMode toolMode)
        {
            ToolMode = toolMode;
        }

        public void Update()
        {
            Count = ToolMode.Sources.Count(s => s.Target == null);

            if (!IsEmpty)
            {
                var cameraDir = -NodeMarkupTool.CameraDirection;
                cameraDir.y = 0;
                cameraDir.Normalize();
                Direction = cameraDir.Turn90(false);
                Position = ToolMode.Centre + cameraDir * (ToolMode.Radius + 2 * TargetEnter.Size);
                Width = (TargetEnter.Size * (Count + 1) + SourceEnter.Size * (Count - 1)) / 2;
            }
        }

        public void Render(RenderManager.CameraInfo cameraInfo)
        {
            if (!IsEmpty && !ToolMode.IsSelectedSource)
            {
                var halfWidth = (Width - TargetEnter.Size) / 2;
                var basket = new StraightTrajectory(Position - Direction * halfWidth, Position + Direction * halfWidth);
                NodeMarkupTool.RenderTrajectory(cameraInfo, Colors.White, basket, TargetEnter.Size, alphaBlend: false);
            }
        }
    }
}
