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
    //public interface ITarget 
    //{
    //    public Vector3 GetSourcePosition(Source source);
    //}
    public interface ITarget<SourceType> /*: ITarget*/
        where SourceType : Source
    {
        public Vector3 GetSourcePosition(SourceType source);
        public void Render(RenderManager.CameraInfo cameraInfo, BaseOrderToolMode toolMode);
    }
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

        public virtual void Update(BaseOrderToolMode toolMode)
        {
            Position = GetPosition(toolMode);
        }
        protected virtual Vector3 GetPosition(BaseOrderToolMode toolMode)
        {
            return Position;
        }

        public bool IsHover(Ray ray) => _bounds.IntersectRay(ray);
    }

    #region SOURCE

    public abstract class Source : PasteItem
    {
        public virtual ITarget Target { get; set; }
        public bool HasTarget => Target != null;

        public Source(int num, Vector3? position = null) : base(num, position) { }

        public void Render<SourceType>(RenderManager.CameraInfo cameraInfo, BaseOrderToolMode<SourceType> toolMode)
            where SourceType : Source
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
    public class SourceEnter : Source
    {
        public static float Size => 2f;
        protected override float BoundsSize => Size;

        public bool IsMirror { get; set; }
        public EnterData Enter { get; }

        private ITarget _target;

        public override ITarget Target
        {
            get => _target;
            set
            {
                _target = value;

                for (var i = 0; i < Points.Length; i += 1)
                    Points[i].Target = _target is TargetEnter targetEnter && i < targetEnter.Enter.Points ? targetEnter.Points[!IsMirror ? i : targetEnter.Points.Length - i - 1] : null;
            }
        }
        public SourcePoint[] Points { get; }

        public SourceEnter(EnterData enter, int num) : base(num)
        {
            Enter = enter;
            Points = Enumerable.Range(0, Enter.Points).Select(i => new SourcePoint(i)).ToArray();
        }
        protected override Vector3 GetPosition(BaseOrderToolMode toolMode)
        {
            if (Target == null)
            {
                return Vector3.zero;
                //var i = toolMode.Sources.Take(Num).Count(s => s.Target == null);
                //return toolMode.Basket.Position + toolMode.Basket.Direction * ((TargetEnter.Size * (i + 1) + Size * i - toolMode.Basket.Width) / 2);
            }
            else
                return Target.GetSourcePosition(this);
        }
    }
    public class SourcePoint : Source
    {
        protected override float BoundsSize => 0.5f;

        public SourcePoint(int num) : base(num) { }
        protected override Vector3 GetPosition(BaseOrderToolMode toolMode)
        {
            if (Target == null)
                return Vector3.zero;
            else
                return Target.GetSourcePosition(this);
        }
    }

    #endregion

    #region TARGET

    public abstract class Target<SourceType> : PasteItem, ITarget
        where SourceType : Source
    {
        protected Vector3 ZeroPosition { get; }
        public Target(int num, Vector3 zeroPosition) : base(num)
        {
            ZeroPosition = zeroPosition;
        }

        public void Render(RenderManager.CameraInfo cameraInfo, BaseOrderToolMode<SourceType> toolMode)

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
                NodeMarkupTool.RenderCircle(cameraInfo, Colors.Gray, Position, BoundsSize, false);
        }
        public abstract Vector3 GetSourcePosition(Source source);
    }
    public class TargetEnter : Target<SourceEnter>
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
        public override void Update(BaseOrderToolMode toolMode)
        {
            base.Update(toolMode);

            foreach (var point in Points)
                point.Update(toolMode);
        }
        protected override Vector3 GetPosition(BaseOrderToolMode toolMode)
        {
            var dir = (ZeroPosition - toolMode.Markup.Position).normalized;
            var normal = dir.Turn90(true);

            Line2.Intersect(toolMode.Centre.XZ(), (toolMode.Centre + normal).XZ(), toolMode.Markup.Position.XZ(), (toolMode.Markup.Position + dir).XZ(), out float p, out _);
            var point = toolMode.Centre + normal * p;
            var distance = Mathf.Sqrt(Mathf.Pow(toolMode.Radius, 2) - Mathf.Pow(Math.Abs(p), 2));
            return point + dir * distance;
        }

        public override Vector3 GetSourcePosition(Source source) => Position;
    }
    public class TargetPoint : Target<SourcePoint>
    {
        protected override float BoundsSize => 1f;
        public TargetPoint(MarkupEnterPoint point, int num) : base(num, point.ZeroPosition) { }
        protected override Vector3 GetPosition(BaseOrderToolMode toolMode) => ZeroPosition;

        public override Vector3 GetSourcePosition(Source source) => Position;
    }

    #endregion

    #region BORDERS

    public class AvalibleBorders<SourceType>
        where SourceType : Source
    {
        public Target<SourceType> Left { get; }
        public Target<SourceType> Right { get; }
        public AvalibleBorders(BaseOrderToolMode<SourceType> toolMode, SourceType source)
        {
            var sourcesLenght = toolMode.Sources.Length;
            Left = GetAvailableBorder(toolMode.Sources, source, s => !toolMode.IsMirror ? s.PrevIndex(sourcesLenght) : s.NextIndex(sourcesLenght), toolMode.AvailableTargetsGetter) ?? toolMode.Targets.First();
            Right = GetAvailableBorder(toolMode.Sources, source, s => !toolMode.IsMirror ? s.NextIndex(sourcesLenght) : s.PrevIndex(sourcesLenght), toolMode.AvailableTargetsGetter) ?? toolMode.Targets.Last();
        }
        private static Target<SourceType> GetAvailableBorder(SourceType[] sources, SourceType source, Func<int, int> func, Func<int, SourceType, bool> condition)
        {
            var i = func(source.Num);
            while (condition(i, source) && !(sources[i].Target is Target<SourceType>))
                i = func(i);
            return sources[i].Target as Target<SourceType>;
        }

        public IEnumerable<Target<SourceType>> GetTargets(Target<SourceType>[] targets)
        {
            yield return Left;
            for (var target = targets[Left.Num.NextIndex(targets.Length)]; target != Right; target = targets[target.Num.NextIndex(targets.Length)])
                yield return target;
            if (Right != Left)
                yield return Right;
        }

    }
    public class EntersBorders : AvalibleBorders<SourceEnter>
    {
        public static Comp Comparer { get; } = new Comp();
        public EntersBorders(BaseOrderToolMode<SourceEnter> toolMode, SourceEnter source) : base(toolMode, source) { }
        public class Comp : IEqualityComparer<EntersBorders>
        {
            public bool Equals(EntersBorders x, EntersBorders y) => x.Left == y.Left && x.Right == y.Right;
            public int GetHashCode(EntersBorders obj) => obj.GetHashCode();
        }
    }
    public class PointsBorders : AvalibleBorders<SourcePoint>
    {
        public PointsBorders(BaseOrderToolMode<SourcePoint> toolMode, SourcePoint source) : base(toolMode, source) { }
    }

    #endregion

    #region BASKET

    public abstract class Basket<SourceType> : ITarget<SourceType>
        where SourceType : Source
    {
        public Vector3 Position { get; protected set; }
        public Vector3 Direction { get; protected set; }

        public List<SourceType> Items { get; }
        public int Count => Items.Count;

        public Basket(IEnumerable<SourceType> items)
        {
            Items = items.ToList();
            foreach (var item in Items)
                item.Target = this;
        }

        public virtual Vector3 GetSourcePosition(Source source) => Vector3.zero;
        public abstract void Render(RenderManager.CameraInfo cameraInfo, BaseOrderToolMode toolMode);
    }
    public class EntersBasket : Basket<SourceEnter>
    {
        float LeftAngle { get; }
        float RigthAngle { get; }
        Vector3 Centre { get; }
        float Radius { get; }
        float WidthAngle { get; }
        float MiddleAngle => (LeftAngle + RigthAngle) / 2;
        float DeltaAngle => RigthAngle - LeftAngle;
        public EntersBasket(EntersOrderToolMode toolMode, EntersBorders borders, IEnumerable<SourceEnter> items) : base(items)
        {
            Centre = toolMode.Centre;
            Radius = toolMode.Radius + 2 * TargetEnter.Size;
            LeftAngle = (borders.Left.Position - Centre).AbsoluteAngle();
            RigthAngle = (borders.Right.Position - Centre).AbsoluteAngle();
            if (RigthAngle <= LeftAngle)
                RigthAngle += Mathf.PI * 2;

            var length = (TargetEnter.Size * (Count + 1) + SourceEnter.Size * (Count - 1)) / 2;
            WidthAngle = 2 * Mathf.PI * (length / (2 * Mathf.PI * Radius));
        }

        public override void Render(RenderManager.CameraInfo cameraInfo, BaseOrderToolMode toolMode)
        {
            var n = Mathf.CeilToInt(DeltaAngle / (Mathf.PI / 2));
            var deltaAngle = (RigthAngle - LeftAngle) / n;

            for (var i = 0; i < n; i += 1)
                NodeMarkupTool.RenderBezier(cameraInfo, Colors.Gray, GetBezier(LeftAngle + deltaAngle * i, LeftAngle + deltaAngle * (i + 1)), cut: true);

            var leftDir = LeftAngle.Direction();
            NodeMarkupTool.RenderTrajectory(cameraInfo, Colors.Gray, new StraightTrajectory(Centre + toolMode.Radius * leftDir, Centre + Radius * leftDir));
            var rightDir = RigthAngle.Direction();
            NodeMarkupTool.RenderTrajectory(cameraInfo, Colors.Gray, new StraightTrajectory(Centre + toolMode.Radius * rightDir, Centre + Radius * rightDir));

            NodeMarkupTool.RenderBezier(cameraInfo, Colors.White, GetBezier(MiddleAngle - DeltaAngle, MiddleAngle + DeltaAngle), TargetEnter.Size, alphaBlend: false);
        }
        private Bezier3 GetBezier(float aAngle, float dAngle)
        {
            var deltaAngle = Math.Abs(aAngle - dAngle);
            var dist = Radius * 4f / 3f * Mathf.Tan(deltaAngle / 4);

            var aDir = aAngle.Direction();
            var dDir = dAngle.Direction();

            var bezier = new Bezier3()
            {
                a = Centre + aDir * Radius,
                d = Centre + dDir * Radius,
            };
            bezier.b = bezier.a + aDir.Turn90(false) * dist;
            bezier.c = bezier.d + dDir.Turn90(true) * dist;

            return bezier;
        }
        public override Vector3 GetSourcePosition(Source source)
        {
            var index = Items.IndexOf(source);
            if (index < 0)
                return base.GetSourcePosition(source);
            else
                return;

        }
        //public override Vector3 GetSourcePosition<SourceType>(SourceEnter source)
        //    where SourceType : Source
        //{
        //    var index = Items.IndexOf(source);
        //    if(index < 0)
        //        return base.GetSourcePosition<SourceEnter>(source);
        //    else
        //    {
        //        //return toolMode.Basket.Position + toolMode.Basket.Direction * ((TargetEnter.Size * (i + 1) + Size * i - toolMode.Basket.Width) / 2);


        //    }
        //}
    }
    public class PointsBasket : Basket<SourcePoint>
    {
        public PointsBasket(AvalibleBorders<SourcePoint> borders, IEnumerable<SourcePoint> items) : base(items)
        {
        }
        public override void Render(RenderManager.CameraInfo cameraInfo, BaseOrderToolMode toolMode)
        {
            throw new NotImplementedException();
        }

        //public override Vector3 GetSourcePosition(Source source)
        //{
        //    if (!Items.Contains(source))
        //        return base.GetSourcePosition(source);
        //    else
        //    {

        //    }
        //}
    }

    #endregion

    //public class Basket
    //{
    //    private PasteMarkupEntersOrderToolMode ToolMode { get; }

    //    public Vector3 Position { get; private set; }
    //    public Vector3 Direction { get; private set; }
    //    public float Width { get; private set; }

    //    public int Count { get; set; }
    //    public bool IsEmpty => Count == 0;

    //    public Basket(PasteMarkupEntersOrderToolMode toolMode)
    //    {
    //        ToolMode = toolMode;
    //    }

    //    public void Update()
    //    {
    //        Count = ToolMode.Sources.Count(s => s.Target == null);

    //        if (!IsEmpty)
    //        {
    //            var cameraDir = -NodeMarkupTool.CameraDirection;
    //            cameraDir.y = 0;
    //            cameraDir.Normalize();
    //            Direction = cameraDir.Turn90(false);
    //            Position = ToolMode.Centre + cameraDir * (ToolMode.Radius + 2 * TargetEnter.Size);
    //            Width = (TargetEnter.Size * (Count + 1) + SourceEnter.Size * (Count - 1)) / 2;
    //        }
    //    }

    //    public void Render(RenderManager.CameraInfo cameraInfo)
    //    {
    //        if (!IsEmpty && !ToolMode.IsSelectedSource)
    //        {
    //            var halfWidth = (Width - TargetEnter.Size) / 2;
    //            var basket = new StraightTrajectory(Position - Direction * halfWidth, Position + Direction * halfWidth);
    //            NodeMarkupTool.RenderTrajectory(cameraInfo, Colors.White, basket, TargetEnter.Size, alphaBlend: false);
    //        }
    //    }
    //}
}
