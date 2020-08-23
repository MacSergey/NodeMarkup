using ColossalFramework.Math;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeMarkup.UI
{
    public class PointsSelector<PointType>
        where PointType : class, ISupportPoint
    {
        private List<PointsGroup<PointType>> Groups { get; } = new List<PointsGroup<PointType>>();
        private PointsGroup<PointType> HoverGroup { get; set; }
        private bool IsHoverGroup => HoverGroup != null;
        public PointType HoverPoint => HoverGroup?.HoverPoint;
        public bool IsHoverPoint => HoverPoint != null;

        public PointsSelector(IEnumerable<PointType> points, Color color, float size = 0.5f)
        {
            foreach (var point in points)
            {
                if (!(Groups.FirstOrDefault(g => g.Intersect(point)) is PointsGroup<PointType> group))
                {
                    group = Settings.GroupPointsType != 1 ? (PointsGroup<PointType>)new RoundPointsGroup<PointType>(color, size) : (PointsGroup<PointType>)new LinePointsGroup<PointType>(color, size);
                    Groups.Add(group);
                }

                group.Add(point);
            }

            foreach (var group in Groups)
                group.Update();
        }

        public void OnUpdate()
        {
            if (NodeMarkupTool.MouseRayValid)
            {
                if (!IsHoverGroup || !HoverGroup.OnUpdate())
                    HoverGroup = Groups.FirstOrDefault(g => g.OnUpdate());
            }
            else
                HoverGroup = null;
        }
        public void Render(RenderManager.CameraInfo cameraInfo)
        {
            foreach (var group in Groups)
                group.Render(cameraInfo);

            if (IsHoverGroup)
                HoverGroup.Render(cameraInfo);
        }
    }
    public abstract class PointsGroup<PointType>
        where PointType : ISupportPoint
    {
        protected Color Color { get; }
        protected float Space { get; } = 0.5f;
        protected float OverlaySize { get; }
        protected float IntersectSize { get; }
        protected List<PointType> Points { get; } = new List<PointType>();
        public int Count => Points.Count;
        protected Dictionary<PointType, Bounds> PointsBounds { get; } = new Dictionary<PointType, Bounds>();
        protected Vector3 Position { get; set; }
        public Bounds HoverBounds { get; set; }

        public bool IsHover { get; private set; }

        public PointType HoverPoint { get; set; }
        public bool IsHoverPoint => HoverPoint != null;

        public PointsGroup(Color color, float overlaySize)
        {
            Color = color;
            OverlaySize = overlaySize;
            IntersectSize = Settings.GroupPoints ? 1.5f * OverlaySize : 0f;
        }

        public void Add(PointType point) => Points.Add(point);
        public bool Intersect(PointType point) => Points.Any(p => (p.Position - point.Position).magnitude < IntersectSize);
        public void Update()
        {
            UpdatePosition();
            HoverBounds = new Bounds(Position, Vector3.one * OverlaySize);
            PointsBounds.Clear();
            UpdateBounds();
        }
        protected void UpdatePosition()
        {
            var position = Vector3.zero;
            foreach (var point in Points)
            {
                position += point.Position - Points.First().Position;
            }
            Position = Points.First().Position + position / Points.Count;
        }
        protected abstract void UpdateBounds();

        public bool OnUpdate()
        {
            IsHover = IsHover ? OnLeave(NodeMarkupTool.MouseRay) : HoverBounds.IntersectRay(NodeMarkupTool.MouseRay);
            if (IsHover)
                HoverPoint = PointsBounds.FirstOrDefault(p => p.Value.IntersectRay(NodeMarkupTool.MouseRay)).Key;

            return IsHover;
        }
        protected abstract bool OnLeave(Ray ray);
        public abstract bool Intersects(PointsGroup<PointType> group);

        public void Render(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHover)
            {
                if (PointsBounds.Count > 1)
                {
                    if (IsHoverPoint)
                        HoverPoint.Render(cameraInfo, Color);
                    RenderGroup(cameraInfo);
                }

                foreach (var bound in PointsBounds.Values)
                    NodeMarkupTool.RenderCircle(cameraInfo, Color, bound.center, OverlaySize);

                if (IsHoverPoint)
                    NodeMarkupTool.RenderCircle(cameraInfo, MarkupColors.White, PointsBounds[HoverPoint].center, OverlaySize + Space);
            }
            else
                NodeMarkupTool.RenderCircle(cameraInfo, Color, Position, OverlaySize);
        }
        protected abstract void RenderGroup(RenderManager.CameraInfo cameraInfo);

        public override string ToString() => $"{Points.Count} Points";
    }
    public class RoundPointsGroup<PointType> : PointsGroup<PointType>
        where PointType : ISupportPoint
    {
        private float R { get; set; }
        private float GroupSize { get; set; }

        public Bounds LeaveBounds { get; set; }

        public RoundPointsGroup(Color color, float size) : base(color, size) { }

        protected override void UpdateBounds()
        {
            R = Points.Count > 1 ? (OverlaySize + Space) / 2 / Mathf.Sin(180 / Points.Count * Mathf.Deg2Rad) : OverlaySize;
            GroupSize = Points.Count > 1 ? (R + OverlaySize + Space) * 2 : OverlaySize;
            LeaveBounds = new Bounds(Position, Vector3.one * GroupSize);

            foreach (var point in Points)
            {
                var pointPosition = Points.Count > 1 ? Position + Vector3.forward.TurnDeg(360 / Points.Count * PointsBounds.Count, true) * R : Position;
                var pointBounds = new Bounds(pointPosition, Vector3.one * OverlaySize);
                PointsBounds.Add(point, pointBounds);
            }
        }
        protected override void RenderGroup(RenderManager.CameraInfo cameraInfo)
        {
            NodeMarkupTool.RenderCircle(cameraInfo, MarkupColors.White, Position, GroupSize -0.43f, false);
            NodeMarkupTool.RenderCircle(cameraInfo, MarkupColors.Blue, Position, GroupSize);
        }

        protected override bool OnLeave(Ray ray) => LeaveBounds.IntersectRay(ray);
        public override bool Intersects(PointsGroup<PointType> group) => 2 * (group.HoverBounds.center - LeaveBounds.center).magnitude <= group.HoverBounds.size.XZ().magnitude + LeaveBounds.size.XZ().magnitude;
    }
    public class LinePointsGroup<PointType> : PointsGroup<PointType>
        where PointType : ISupportPoint
    {
        BezierBounds LeaveBounds { get; set; }

        public LinePointsGroup(Color color, float size) : base(color, size) { }


        protected override void UpdateBounds()
        {
            var dir = NodeMarkupTool.CameraDirection;
            dir.y = 0;
            dir.Normalize();
            var step = OverlaySize + Space;
            var bezier = new Bezier3();
            bezier.a = bezier.c = Position;
            bezier.d = bezier.b = Position + dir * (step * Points.Count);
            LeaveBounds = new BezierBounds(bezier, OverlaySize + 4 * Space);

            foreach (var point in Points)
            {
                var pointPosition = Points.Count > 1 ? Position + dir * (step * (PointsBounds.Count + 1)) : Position;
                var pointBounds = new Bounds(pointPosition, Vector3.one * OverlaySize);
                PointsBounds.Add(point, pointBounds);
            }
        }


        protected override void RenderGroup(RenderManager.CameraInfo cameraInfo)
        {
            NodeMarkupTool.RenderBezier(cameraInfo, MarkupColors.White, LeaveBounds.Bezier, LeaveBounds.Size - 0.43f, alphaBlend: false);
            NodeMarkupTool.RenderBezier(cameraInfo, MarkupColors.Blue, LeaveBounds.Bezier, LeaveBounds.Size);
        }

        protected override bool OnLeave(Ray ray) => LeaveBounds.IntersectRay(ray);
        public override bool Intersects(PointsGroup<PointType> group) => LeaveBounds.Bounds.Any(b => 2 * (group.HoverBounds.center - b.center).magnitude <= group.HoverBounds.size.XZ().magnitude + b.size.XZ().magnitude);
    }
}
