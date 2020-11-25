using ColossalFramework.Math;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public PointsSelector(IEnumerable<PointType> points, Color color, float pointSize = 0.5f)
        {
            foreach (var point in points)
            {
                if (!(Groups.FirstOrDefault(g => g.Intersect(point)) is PointsGroup<PointType> group))
                {
                    group = Settings.GroupPointsType != 1 ? (PointsGroup<PointType>)new RoundPointsGroup<PointType>(color, pointSize) : (PointsGroup<PointType>)new LinePointsGroup<PointType>(color, pointSize);
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
        protected float PointSize { get; }
        protected float Step => PointSize + Space;
        protected float IntersectSize { get; }
        protected List<PointType> Points { get; } = new List<PointType>();
        public int Count => Points.Count;
        protected Dictionary<PointType, Bounds> PointsBounds { get; } = new Dictionary<PointType, Bounds>();
        protected Vector3 Position { get; set; }
        public Bounds HoverBounds { get; set; }

        public bool IsHover { get; private set; }

        public PointType HoverPoint { get; set; }
        public bool IsHoverPoint => HoverPoint != null;

        public PointsGroup(Color color, float pointSize)
        {
            Color = color;
            PointSize = pointSize;
            IntersectSize = Settings.GroupPoints ? 1.5f * PointSize : 0f;
        }

        public void Add(PointType point) => Points.Add(point);
        public bool Intersect(PointType point) => Points.Any(p => (p.Position - point.Position).magnitude < IntersectSize);
        public void Update()
        {
            UpdatePosition();
            HoverBounds = new Bounds(Position, Vector3.one * PointSize);
            PointsBounds.Clear();
            UpdateBounds();
        }
        protected void UpdatePosition()
        {
            var position = Vector3.zero;
            foreach (var point in Points)
                position += point.Position - Points.First().Position;

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

        public void Render(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHover)
            {
                if (PointsBounds.Count > 1)
                {
                    RenderGroupBG(cameraInfo);
                    if (IsHoverPoint)
                        HoverPoint.Render(cameraInfo, Color);
                    RenderGroupFG(cameraInfo);
                }

                foreach (var bound in PointsBounds.Values)
                {
                    NodeMarkupTool.RenderCircle(cameraInfo, bound.center, Colors.White, PointSize + 0.1f);
                    NodeMarkupTool.RenderCircle(cameraInfo, bound.center, Colors.White, PointSize - 0.05f);
                    NodeMarkupTool.RenderCircle(cameraInfo, bound.center, Color, PointSize);
                }

                if (IsHoverPoint)
                    NodeMarkupTool.RenderCircle(cameraInfo, PointsBounds[HoverPoint].center, Colors.Hover, PointSize + Space);
            }
            else
                NodeMarkupTool.RenderCircle(cameraInfo, Position, Color, PointSize);
        }
        protected abstract void RenderGroupBG(RenderManager.CameraInfo cameraInfo);
        protected abstract void RenderGroupFG(RenderManager.CameraInfo cameraInfo);

        public override string ToString() => $"{Points.Count} Points";
    }
    public class RoundPointsGroup<PointType> : PointsGroup<PointType>
        where PointType : ISupportPoint
    {
        private Bounds CircleLeaveBounds { get; set; }
        private TrajectoryBound LineLeaveBounds { get; set; }
        private float Width => Points.Count > 2 ? CircleLeaveBounds.Magnitude() : LineLeaveBounds.Size;

        public RoundPointsGroup(Color color, float pointSize) : base(color, pointSize) { }

        protected override void UpdateBounds()
        {
            var r = Mathf.Max((Step / 2) / Mathf.Sin(180 / Points.Count * Mathf.Deg2Rad), Step);
            var dir = NodeMarkupTool.CameraDirection.Turn90(true);

            CircleLeaveBounds = new Bounds(Position, Vector3.one * (Points.Count > 1 ? (2 * r + PointSize + 3 * Space) : PointSize));
            LineLeaveBounds = new TrajectoryBound(new StraightTrajectory(Position - dir * r, Position + dir * r), PointSize + 3 * Space);

            foreach (var point in Points)
            {
                var pointPosition = Points.Count > 1 ? Position + dir.TurnDeg(360 / Points.Count * PointsBounds.Count, true) * r : Position;
                var pointBounds = new Bounds(pointPosition, Vector3.one * PointSize);
                PointsBounds.Add(point, pointBounds);
            }
        }
        protected override void RenderGroupBG(RenderManager.CameraInfo cameraInfo) => RenderGroup(cameraInfo, Colors.White, Width - 0.43f, false);
        protected override void RenderGroupFG(RenderManager.CameraInfo cameraInfo) => RenderGroup(cameraInfo, Colors.Blue, Width);
        private void RenderGroup(RenderManager.CameraInfo cameraInfo, Color color, float width, bool? alphaBlend = null)
        {
            if (Points.Count > 2)
                NodeMarkupTool.RenderCircle(cameraInfo, Position, color, width, alphaBlend);
            else
                LineLeaveBounds.Render(cameraInfo, color, width, alphaBlend);
        }

        protected override bool OnLeave(Ray ray) => Points.Count != 2 ? CircleLeaveBounds.IntersectRay(ray) : LineLeaveBounds.IntersectRay(ray);
    }
    public class LinePointsGroup<PointType> : PointsGroup<PointType>
        where PointType : ISupportPoint
    {
        private TrajectoryBound LeaveBounds { get; set; }

        public LinePointsGroup(Color color, float pointSize) : base(color, pointSize) { }

        protected override void UpdateBounds()
        {
            LeaveBounds = new TrajectoryBound(new StraightTrajectory(Position, Position + NodeMarkupTool.CameraDirection * (Step * Points.Count)), PointSize + 3 * Space);

            foreach (var point in Points)
            {
                var pointPosition = Points.Count > 1 ? Position + NodeMarkupTool.CameraDirection * (Step * (PointsBounds.Count + 1)) : Position;
                var pointBounds = new Bounds(pointPosition, Vector3.one * PointSize);
                PointsBounds.Add(point, pointBounds);
            }
        }

        protected override void RenderGroupBG(RenderManager.CameraInfo cameraInfo) => LeaveBounds.Render(cameraInfo, Colors.White, LeaveBounds.Size - 0.43f, false);
        protected override void RenderGroupFG(RenderManager.CameraInfo cameraInfo) => LeaveBounds.Render(cameraInfo, Colors.Blue, LeaveBounds.Size);

        protected override bool OnLeave(Ray ray) => LeaveBounds.IntersectRay(ray);
    }
}
