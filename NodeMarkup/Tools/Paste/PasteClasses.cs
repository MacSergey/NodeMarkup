using ColossalFramework.Math;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions.Must;

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
            var position = toolMode.SelectedSource == this ? (toolMode.IsHoverTarget ? toolMode.HoverTarget.Position : NodeMarkupTool.MouseWorldPosition) : Position;
            var size = BoundsSize;
            var color = Colors.GetOverlayColor(Num, 255, hue);
            while (size > 0)
            {
                NodeMarkupTool.RenderCircle(cameraInfo, position, color, size);
                size -= 0.43f;
            }
        }
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
                NodeMarkupTool.RenderCircle(cameraInfo, Position, width: BoundsSize, alphaBlend: false);
                if (toolMode.IsSelectedSource)
                {
                    if (toolMode.HoverTarget == this && toolMode.SelectedSource.Target != this)
                        NodeMarkupTool.RenderCircle(cameraInfo, Position, Colors.Green, BoundsSize + 0.43f);
                    else if (toolMode.HoverTarget != this && toolMode.SelectedSource.Target == this)
                        NodeMarkupTool.RenderCircle(cameraInfo, Position, Colors.Red, BoundsSize + 0.43f);
                }
            }
            else
                NodeMarkupTool.RenderCircle(cameraInfo, Position, Colors.Gray, BoundsSize, false);
        }
        public abstract Vector3 GetSourcePosition(SourceType source);
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

        public override Vector3 GetSourcePosition(SourceEnter source) => Position;
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
        public Target<SourceType> Left { get; }
        public Target<SourceType> Right { get; }
        public AvalibleBorders(BaseOrderToolMode<SourceType> toolMode, SourceType source)
        {
            var sourcesLenght = toolMode.Sources.Length;

            var prev = GetAvailableBorder(toolMode.Sources, source, s => s.PrevIndex(sourcesLenght)) ?? toolMode.Targets.First();
            var next = GetAvailableBorder(toolMode.Sources, source, s => s.NextIndex(sourcesLenght)) ?? toolMode.Targets.Last();

            Left = !toolMode.IsMirror ? prev : next;
            Right = !toolMode.IsMirror ? next : prev;
        }
        protected abstract Target<SourceType> GetAvailableBorder(SourceType[] sources, SourceType source, Func<int, int> func);

        public abstract IEnumerable<Target<SourceType>> GetTargets(BaseOrderToolMode<SourceType> toolMode, Target<SourceType>[] targets);
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
        protected override Target<SourceEnter> GetAvailableBorder(SourceEnter[] sources, SourceEnter source, Func<int, int> func)
        {
            var i = func(source.Num);
            while (i != source.Num && !(sources[i].Target is Target<SourceEnter>))
                i = func(i);
            return sources[i].Target as Target<SourceEnter>;
        }
        public override IEnumerable<Target<SourceEnter>> GetTargets(BaseOrderToolMode<SourceEnter> toolMode, Target<SourceEnter>[] targets)
        {
            if (Left != Right)
                yield return Left;

            for (var target = targets[Left.Num.NextIndex(targets.Length)]; target != Right; target = targets[target.Num.NextIndex(targets.Length)])
                yield return target;

            if (Right != Left)
                yield return Right;
        }
    }
    public class PointsBorders : AvalibleBorders<SourcePoint>
    {
        public static Comp Comparer { get; } = new Comp();
        public PointsBorders(BaseOrderToolMode<SourcePoint> toolMode, SourcePoint source) : base(toolMode, source) { }
        public class Comp : IEqualityComparer<PointsBorders>
        {
            public bool Equals(PointsBorders x, PointsBorders y) => x.Left == y.Left && x.Right == y.Right;
            public int GetHashCode(PointsBorders obj) => obj.GetHashCode();
        }
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
            for (var target = Left; target != Right; target = targets[Func(target.Num)])
                yield return target;

            yield return Right;

            int Func(int i) => !toolMode.IsMirror ? i.NextIndex(targets.Length) : i.PrevIndex(targets.Length);
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
        float LeftAngle { get; }
        float RigthAngle { get; }
        Vector3 Centre { get; }
        float Radius { get; }
        float HalfWidthAngle { get; }
        float MiddleAngle => (LeftAngle + RigthAngle) / 2;
        float DeltaAngle => RigthAngle - LeftAngle;
        public EntersBasket(BaseEntersOrderToolMode toolMode, EntersBorders borders, IEnumerable<SourceEnter> items) : base(items)
        {
            Centre = toolMode.Centre;
            Radius = toolMode.Radius + 2 * TargetEnter.Size;
            LeftAngle = (borders.Left.Position - Centre).AbsoluteAngle();
            RigthAngle = (borders.Right.Position - Centre).AbsoluteAngle();
            if (RigthAngle <= LeftAngle)
                RigthAngle += Mathf.PI * 2;

            var length = TargetEnter.Size * (Count - 1);
            HalfWidthAngle = GetAngle(length) / 2;
        }

        public override void Render(RenderManager.CameraInfo cameraInfo, BaseOrderToolMode<SourceEnter> toolMode)
        {
            var n = Mathf.CeilToInt(DeltaAngle / (Mathf.PI / 2));
            var deltaAngle = (RigthAngle - LeftAngle) / n;

            for (var i = 0; i < n; i += 1)
                NodeMarkupTool.RenderBezier(cameraInfo, GetBezier(LeftAngle + deltaAngle * i, LeftAngle + deltaAngle * (i + 1)), Color, cut: true);

            var leftDir = LeftAngle.Direction();
            new StraightTrajectory(Centre + toolMode.Radius * leftDir, Centre + Radius * leftDir).Render(cameraInfo, Color);
            var rightDir = RigthAngle.Direction();
            new StraightTrajectory(Centre + toolMode.Radius * rightDir, Centre + Radius * rightDir).Render(cameraInfo, Color);

            if (Count <= 1)
                NodeMarkupTool.RenderCircle(cameraInfo, Centre + MiddleAngle.Direction() * Radius, width: TargetEnter.Size, alphaBlend: false);
            else
                NodeMarkupTool.RenderBezier(cameraInfo, GetBezier(MiddleAngle - HalfWidthAngle, MiddleAngle + HalfWidthAngle), width: TargetEnter.Size, alphaBlend: false);
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
        Vector3 Direction { get; }
        Vector3 Position { get; }
        StraightTrajectory Line { get; }
        StraightTrajectory Connect { get; }
        float Shift => 3 * TargetPoint.Size;
        float Width { get; }
        public PointsBasket(PointsOrderToolMode toolMode, PointsBorders borders, IEnumerable<SourcePoint> items) : base(items)
        {
            Direction = toolMode.TargetEnter.Enter.Corner.Turn90(false);
            var middlePos = (borders.Left.Position + borders.Right.Position) / 2;
            Position = middlePos + Direction * Shift;
            var length = TargetPoint.Size * (Count - 1);
            Line = new StraightTrajectory(Position, Position + Direction * length);
            Connect = new StraightTrajectory(middlePos, middlePos + Direction * Shift);
            Width = (borders.Left.Position - borders.Right.Position).magnitude;
        }
        public override void Render(RenderManager.CameraInfo cameraInfo, BaseOrderToolMode<SourcePoint> toolMode)
        {
            Connect.Render(cameraInfo, Color, Width);

            if (Count <= 0)
                NodeMarkupTool.RenderCircle(cameraInfo, Position, width: TargetPoint.Size, alphaBlend: false);
            else
                Line.Render(cameraInfo, width: TargetPoint.Size, alphaBlend: false);

        }
        public override Vector3 GetSourcePosition(SourcePoint source)
        {
            var index = Items.IndexOf(source);
            return index < 0 ? base.GetSourcePosition(source) : Position + TargetPoint.Size * index * Direction;
        }
    }

    #endregion
}
