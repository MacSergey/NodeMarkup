using ColossalFramework.Math;
using ModsCommon;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeMarkup.Tools
{

    public interface ITarget<SourceType>
        where SourceType : Source<SourceType>
    {
        public Vector3 GetSourcePosition(SourceType source);
        public void Render(RenderManager.CameraInfo cameraInfo, BaseOrderToolMode<SourceType> toolMode);
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

        public override string ToString() => Num.ToString();
    }

    #region SOURCE

    public abstract class Source<SourceType> : PasteItem
        where SourceType : Source<SourceType>
    {
        public virtual ITarget<SourceType> Target { get; set; }
        public bool HasTarget => Target != null;

        public Source(int num, Vector3? position = null) : base(num, position) { }

        public void Render(RenderManager.CameraInfo cameraInfo, BaseOrderToolMode<SourceType> toolMode)
        {
            var hue = (byte)(toolMode.SelectedSource == this || toolMode.HoverSource == this ? 255 : 192);
            var position = toolMode.SelectedSource == this ? (toolMode.IsHoverTarget ? toolMode.HoverTarget.Position : SingletonTool<NodeMarkupTool>.Instance.MouseWorldPosition) : Position;
            var size = BoundsSize;
            var color = Colors.GetOverlayColor(Num, 255, hue);
            while (size > 0)
            {
                position.RenderCircle(new OverlayData(cameraInfo) { Color = color, Width = size });
                size -= 0.43f;
            }
        }
        public override string ToString() => $"{base.ToString()} - {Target}";
    }
    public class SourceEnter : Source<SourceEnter>
    {
        public static float Size => 2f;
        protected override float BoundsSize => Size;

        public bool IsMirror { get; set; }
        public EnterData Enter { get; }

        private ITarget<SourceEnter> _target;

        public override ITarget<SourceEnter> Target
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
        protected override Vector3 GetPosition(BaseOrderToolMode toolMode) => Target?.GetSourcePosition(this) ?? Vector3.zero;
    }
    public class SourcePoint : Source<SourcePoint>
    {
        protected override float BoundsSize => 0.5f;

        public SourcePoint(int num) : base(num) { }
        protected override Vector3 GetPosition(BaseOrderToolMode toolMode) => Target?.GetSourcePosition(this) ?? Vector3.zero;
    }

    #endregion

    #region TARGET

    public abstract class Target : PasteItem
    {
        public Target(int num, Vector3? position = null) : base(num, position) { }
    }
    public abstract class Target<SourceType> : Target, ITarget<SourceType>
        where SourceType : Source<SourceType>
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
                Position.RenderCircle(new OverlayData(cameraInfo) { Width = BoundsSize, AlphaBlend = false });
                if (toolMode.IsSelectedSource)
                {
                    if (toolMode.HoverTarget == this && toolMode.SelectedSource.Target != this)
                        Position.RenderCircle(new OverlayData(cameraInfo) { Color = Colors.Green, Width = BoundsSize + 0.43f });
                    else if (toolMode.HoverTarget != this && toolMode.SelectedSource.Target == this)
                        Position.RenderCircle(new OverlayData(cameraInfo) { Color = Colors.Red, Width = BoundsSize + 0.43f });
                }
            }
            else
                Position.RenderCircle(new OverlayData(cameraInfo) { Color = Colors.Gray, Width = BoundsSize, AlphaBlend = false });
        }
        public abstract Vector3 GetSourcePosition(SourceType source);
    }
    public class TargetEnter : Target<SourceEnter>
    {
        public static float Size => 3f;
        protected override float BoundsSize => Size;

        public EnterData Enter { get; }

        public TargetPoint[] Points { get; }

        public TargetEnter(Enter enter, int num) : base(num, enter.Position)
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

        public override Vector3 GetSourcePosition(SourceEnter source) => Position;

        public override string ToString() => $"{base.ToString()} ({Enter})";
    }
    public class TargetPoint : Target<SourcePoint>
    {
        public static float Size => 1.2f;
        protected override float BoundsSize => Size;
        public TargetPoint(MarkupEnterPoint point, int num) : base(num, point.ZeroPosition) { }
        protected override Vector3 GetPosition(BaseOrderToolMode toolMode) => ZeroPosition;

        public override Vector3 GetSourcePosition(SourcePoint source) => Position;
    }

    #endregion

    #region BORDERS

    public abstract class AvalibleBorders<SourceType>
        where SourceType : Source<SourceType>
    {
        public Target<SourceType> From { get; }
        public Target<SourceType> To { get; }
        public AvalibleBorders(BaseOrderToolMode<SourceType> toolMode, SourceType source)
        {
            var sourcesLenght = toolMode.Sources.Length;

            var prev = GetAvailableBorder(toolMode.Sources, source, s => s.PrevIndex(sourcesLenght)) ?? GetDefaultPrev(toolMode);
            var next = GetAvailableBorder(toolMode.Sources, source, s => s.NextIndex(sourcesLenght)) ?? GetDefaultNext(toolMode);

            From = !toolMode.IsMirror ? prev : next;
            To = !toolMode.IsMirror ? next : prev;
        }
        protected virtual Target<SourceType> GetDefaultPrev(BaseOrderToolMode<SourceType> toolMode) => toolMode.Targets.First();
        protected virtual Target<SourceType> GetDefaultNext(BaseOrderToolMode<SourceType> toolMode) => toolMode.Targets.Last();
        protected abstract Target<SourceType> GetAvailableBorder(SourceType[] sources, SourceType source, Func<int, int> func);

        public abstract IEnumerable<Target<SourceType>> GetTargets(BaseOrderToolMode<SourceType> toolMode, Target<SourceType>[] targets);
    }
    public class EntersBorders : AvalibleBorders<SourceEnter>
    {
        public static Comp Comparer { get; } = new Comp();
        public EntersBorders(BaseOrderToolMode<SourceEnter> toolMode, SourceEnter source) : base(toolMode, source) { }
        public class Comp : IEqualityComparer<EntersBorders>
        {
            public bool Equals(EntersBorders x, EntersBorders y) => x.From == y.From && x.To == y.To;
            public int GetHashCode(EntersBorders obj) => obj.GetHashCode();
        }
        protected override Target<SourceEnter> GetAvailableBorder(SourceEnter[] sources, SourceEnter source, Func<int, int> func)
        {
            var i = func(source.Num);
            while (i != source.Num && !(sources[i].Target is Target<SourceEnter>))
                i = func(i);
            return sources[i].Target as Target<SourceEnter>;
        }
        public override IEnumerable<Target<SourceEnter>> GetTargets(BaseOrderToolMode<SourceEnter> toolMode, Target<SourceEnter>[] targets)
        {
            yield return From;

            for (var target = Next(From); target != To; target = Next(target))
                yield return target;

            if (To != From)
                yield return To;

            Target<SourceEnter> Next(Target<SourceEnter> target) => targets[target.Num.NextIndex(targets.Length)];
        }
    }
    public class PointsBorders : AvalibleBorders<SourcePoint>
    {
        public static Comp Comparer { get; } = new Comp();
        public PointsBorders(BaseOrderToolMode<SourcePoint> toolMode, SourcePoint source) : base(toolMode, source) { }
        public class Comp : IEqualityComparer<PointsBorders>
        {
            public bool Equals(PointsBorders x, PointsBorders y) => x.From == y.From && x.To == y.To;
            public int GetHashCode(PointsBorders obj) => obj.GetHashCode();
        }
        protected override Target<SourcePoint> GetDefaultPrev(BaseOrderToolMode<SourcePoint> toolMode) => !toolMode.IsMirror ? toolMode.Targets.First() : toolMode.Targets.Last();
        protected override Target<SourcePoint> GetDefaultNext(BaseOrderToolMode<SourcePoint> toolMode) => !toolMode.IsMirror ? toolMode.Targets.Last() : toolMode.Targets.First();
        protected override Target<SourcePoint> GetAvailableBorder(SourcePoint[] sources, SourcePoint source, Func<int, int> func)
        {
            var i = source.Num;
            var j = func(i);
            while (true)
            {
                if ((i == 0 && j == sources.Length - 1) || (i == sources.Length - 1 && j == 0))
                    return null;
                else if (sources[j].Target is Target<SourcePoint> target)
                    return target;

                i = j;
                j = func(j);
            }
        }
        public override IEnumerable<Target<SourcePoint>> GetTargets(BaseOrderToolMode<SourcePoint> toolMode, Target<SourcePoint>[] targets)
        {
            for (var target = From; target != To; target = targets[target.Num.NextIndex(targets.Length)])
                yield return target;

            yield return To;
        }
    }

    #endregion

    #region BASKET

    public abstract class Basket<SourceType> : ITarget<SourceType>
        where SourceType : Source<SourceType>
    {
        protected static Color Color { get; } = new Color32(255, 255, 255, 128);

        public List<SourceType> Items { get; }
        public int Count => Items.Count;

        public Basket(IEnumerable<SourceType> items)
        {
            Items = items.ToList();
            foreach (var item in Items)
                item.Target = this;
        }

        public virtual Vector3 GetSourcePosition(SourceType source) => Vector3.zero;
        public abstract void Render(RenderManager.CameraInfo cameraInfo, BaseOrderToolMode<SourceType> toolMode);
    }
    public class EntersBasket : Basket<SourceEnter>
    {
        private float FromAngle { get; }
        private float ToAngle { get; }
        private Vector3 Centre { get; }
        private float Radius { get; }
        private float HalfWidthAngle { get; }

        private float MiddleAngle => (FromAngle + ToAngle) / 2;

        private float DeltaAngle => FromAngle - ToAngle;
        public EntersBasket(BaseEntersOrderToolMode toolMode, EntersBorders borders, IEnumerable<SourceEnter> items) : base(items)
        {
            Centre = toolMode.Centre;
            Radius = toolMode.Radius + 2 * TargetEnter.Size;
            FromAngle = (borders.From.Position - Centre).AbsoluteAngle();
            ToAngle = (borders.To.Position - Centre).AbsoluteAngle();
            if (FromAngle <= ToAngle)
                FromAngle += Mathf.PI * 2;

            var length = TargetEnter.Size * (Count - 1);
            HalfWidthAngle = GetAngle(length) / 2;
        }

        public override void Render(RenderManager.CameraInfo cameraInfo, BaseOrderToolMode<SourceEnter> toolMode)
        {
            var n = Mathf.CeilToInt(DeltaAngle / (Mathf.PI / 2));
            var deltaAngle = (FromAngle - ToAngle) / n;

            for (var i = 0; i < n; i += 1)
            {
                var bezier = GetBezier(ToAngle + deltaAngle * i, ToAngle + deltaAngle * (i + 1));
                bezier.RenderBezier(new OverlayData(cameraInfo) { Color = Color, Cut = true });
            }

            var fromDir = FromAngle.Direction();
            new StraightTrajectory(Centre + toolMode.Radius * fromDir, Centre + Radius * fromDir).Render(new OverlayData(cameraInfo) { Color = Color });
            var toDir = ToAngle.Direction();
            new StraightTrajectory(Centre + toolMode.Radius * toDir, Centre + Radius * toDir).Render(new OverlayData(cameraInfo) { Color = Color });

            if (Count <= 1)
            {
                var position = Centre + MiddleAngle.Direction() * Radius;
                position.RenderCircle(new OverlayData(cameraInfo) { Width = TargetEnter.Size, AlphaBlend = false });
            }
            else
            {
                var bezier = GetBezier(MiddleAngle - HalfWidthAngle, MiddleAngle + HalfWidthAngle);
                bezier.RenderBezier(new OverlayData(cameraInfo) { Width = TargetEnter.Size, AlphaBlend = false });
            }
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
        private float GetAngle(float length) => 2 * Mathf.PI * (length / (2 * Mathf.PI * Radius));
        public override Vector3 GetSourcePosition(SourceEnter source)
        {
            var index = Items.IndexOf(source);
            if (index < 0)
                return base.GetSourcePosition(source);
            else
            {
                var length = TargetEnter.Size * index;
                var angle = MiddleAngle - HalfWidthAngle + GetAngle(length);
                return Centre + angle.Direction() * Radius;
            }

        }
    }
    public class PointsBasket : Basket<SourcePoint>
    {
        private Vector3 Direction { get; }
        private Vector3 Position { get; }
        private StraightTrajectory Line { get; }
        private StraightTrajectory Connect { get; }

        private float Shift => 3 * TargetPoint.Size;

        private float Width { get; }
        public PointsBasket(PointsOrderToolMode toolMode, PointsBorders borders, IEnumerable<SourcePoint> items) : base(items)
        {
            Direction = toolMode.TargetEnter.Enter.CornerAngle.Direction().Turn90(false);
            var middlePos = (borders.From.Position + borders.To.Position) / 2;
            Position = middlePos + Direction * Shift;
            var length = TargetPoint.Size * (Count - 1);
            Line = new StraightTrajectory(Position, Position + Direction * length);
            Connect = new StraightTrajectory(middlePos, middlePos + Direction * Shift);
            Width = (borders.From.Position - borders.To.Position).magnitude;
        }
        public override void Render(RenderManager.CameraInfo cameraInfo, BaseOrderToolMode<SourcePoint> toolMode)
        {
            Connect.Render(new OverlayData(cameraInfo) { Color = Color, Width = Width, Cut = true });

            if (Count <= 0)
                Position.RenderCircle(new OverlayData(cameraInfo) { Width = TargetPoint.Size, AlphaBlend = false });
            else
                Line.Render(new OverlayData(cameraInfo) { Width = TargetPoint.Size, AlphaBlend = false });

        }
        public override Vector3 GetSourcePosition(SourcePoint source)
        {
            var index = Items.IndexOf(source);
            return index < 0 ? base.GetSourcePosition(source) : Position + TargetPoint.Size * index * Direction;
        }
    }

    #endregion
}
