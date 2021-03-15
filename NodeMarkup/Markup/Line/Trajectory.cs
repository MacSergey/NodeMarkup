using ColossalFramework.Math;
using ModsCommon.Utilities;
using NodeMarkup.Tools;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public enum TrajectoryType
    {
        Line,
        Bezier
    }
    public interface ITrajectory : IRender
    {
        TrajectoryType TrajectoryType { get; }
        float Length { get; }
        float Magnitude { get; }
        float DeltaAngle { get; }
        Vector3 Direction { get; }
        Vector3 StartDirection { get; }
        Vector3 EndDirection { get; }
        Vector3 StartPosition { get; }
        Vector3 EndPosition { get; }
        ITrajectory Cut(float t0, float t1);
        void Divide(out ITrajectory trajectory1, out ITrajectory trajectory2);
        Vector3 Tangent(float t);
        Vector3 Position(float t);
        float Travel(float start, float distance);
        ITrajectory Invert();
        ITrajectory Copy();
    }
    public class BezierTrajectory : ITrajectory
    {
        public TrajectoryType TrajectoryType => TrajectoryType.Bezier;
        public Bezier3 Trajectory { get; }
        public float Length { get; }
        public float Magnitude { get; }
        public float DeltaAngle { get; }
        public Vector3 Direction { get; }
        public Vector3 StartDirection { get; }
        public Vector3 EndDirection { get; }
        public Vector3 StartPosition => Trajectory.a;
        public Vector3 EndPosition => Trajectory.d;
        public BezierTrajectory(Bezier3 trajectory)
        {
            Trajectory = trajectory;

            Length = Trajectory.Length();
            Magnitude = (Trajectory.d - Trajectory.a).magnitude;
            DeltaAngle = Trajectory.DeltaAngle();
            Direction = (Trajectory.d - Trajectory.a).normalized;
            StartDirection = (Trajectory.b - Trajectory.a).normalized;
            EndDirection = (Trajectory.c - Trajectory.d).normalized;
        }
        public BezierTrajectory(Vector3 startPos, Vector3 startDir, Vector3 endPos, Vector3 endDir) : this(GetBezier(startPos, startDir, endPos, endDir)) { }
        private static Bezier3 GetBezier(Vector3 startPos, Vector3 startDir, Vector3 endPos, Vector3 endDir)
        {
            var bezier = new Bezier3()
            {
                a = startPos,
                b = startDir.normalized,
                c = endDir.normalized,
                d = endPos,
            };
            NetSegment.CalculateMiddlePoints(bezier.a, bezier.b, bezier.d, bezier.c, true, true, out bezier.b, out bezier.c);
            return bezier;
        }
        public BezierTrajectory(ITrajectory trajectory) : this(trajectory.StartPosition, trajectory.StartDirection, trajectory.EndPosition, trajectory.EndDirection) { }

        public ITrajectory Cut(float t0, float t1) => new BezierTrajectory(Trajectory.Cut(t0, t1));
        public void Divide(out ITrajectory trajectory1, out ITrajectory trajectory2)
        {
            Trajectory.Divide(out Bezier3 bezier1, out Bezier3 bezier2);
            trajectory1 = new BezierTrajectory(bezier1);
            trajectory2 = new BezierTrajectory(bezier2);
        }
        public Vector3 Tangent(float t) => Trajectory.Tangent(t);
        public Vector3 Position(float t) => Trajectory.Position(t);
        public float Travel(float start, float distance) => Trajectory.Travel(start, distance);
        public ITrajectory Invert() => new BezierTrajectory(Trajectory.Invert());
        public ITrajectory Copy() => new BezierTrajectory(Trajectory);

        public void Render(RenderManager.CameraInfo cameraInfo, Color? color = null, float? width = null, bool? alphaBlend = null, bool? cut = null)
            => NodeMarkupTool.RenderBezier(cameraInfo, Trajectory, color, width, alphaBlend, cut);

        public static implicit operator Bezier3(BezierTrajectory trajectory) => trajectory.Trajectory;
        public static explicit operator BezierTrajectory(Bezier3 bezier) => new BezierTrajectory(bezier);
    }
    public class StraightTrajectory : ITrajectory
    {
        public TrajectoryType TrajectoryType => TrajectoryType.Line;
        public Line3 Trajectory { get; }
        public bool IsSection { get; }
        public float Length { get; }
        public float Magnitude => Length;
        public float DeltaAngle => 0f;
        public Vector3 Direction { get; }
        public Vector3 StartDirection => Direction;
        public Vector3 EndDirection => -Direction;
        public Vector3 StartPosition => Trajectory.a;
        public Vector3 EndPosition => Trajectory.b;
        public StraightTrajectory(Line3 trajectory, bool isSection = true)
        {
            Trajectory = trajectory;
            IsSection = isSection;

            Length = (Trajectory.b - Trajectory.a).magnitude;
            Direction = (Trajectory.b - Trajectory.a).normalized;
        }
        public StraightTrajectory(Vector3 start, Vector3 end, bool isSection = true) : this(new Line3(start, end), isSection) { }
        public StraightTrajectory(ITrajectory trajectory, bool isSection = true) : this(new Line3(trajectory.StartPosition, trajectory.EndPosition), isSection) { }

        public ITrajectory Cut(float t0, float t1) => new StraightTrajectory(Position(t0), Position(t1));

        public void Divide(out ITrajectory trajectory1, out ITrajectory trajectory2)
        {
            var middle = (Trajectory.a + Trajectory.b) / 2;
            trajectory1 = new StraightTrajectory(Trajectory.a, middle);
            trajectory2 = new StraightTrajectory(middle, Trajectory.b);
        }
        public Vector3 Tangent(float t) => Direction;
        public Vector3 Position(float t) => Trajectory.a + (Trajectory.b - Trajectory.a) * t;
        public float Travel(float start, float distance) => start + (distance / Length);
        public ITrajectory Invert() => new StraightTrajectory(Trajectory.b, Trajectory.a, IsSection);
        public ITrajectory Copy() => new StraightTrajectory(Trajectory, IsSection);

        public void Render(RenderManager.CameraInfo cameraInfo, Color? color = null, float? width = null, bool? alphaBlend = null, bool? cut = null)
            => NodeMarkupTool.RenderBezier(cameraInfo, Trajectory.GetBezier(), color, width, alphaBlend, cut);

        public static implicit operator Line3(StraightTrajectory trajectory) => trajectory.Trajectory;
        public static explicit operator StraightTrajectory(Line3 line) => new StraightTrajectory(line);
    }
    public class TrajectoryBound : IRender
    {
        private static float Coef { get; } = Mathf.Sin(45 * Mathf.Deg2Rad);
        public ITrajectory Trajectory { get; }
        public float Size { get; }
        private List<Bounds> BoundsList { get; } = new List<Bounds>();
        public IEnumerable<Bounds> Bounds => BoundsList;
        public TrajectoryBound(ITrajectory trajectory, float size)
        {
            Trajectory = trajectory;
            Size = size;
            CalculateBounds();
        }

        private void CalculateBounds()
        {
            var size = Size * Coef;
            var t = 0f;
            while (t < 1f)
            {
                t = Trajectory.Travel(t, size / 2);
                BoundsList.Add(new Bounds(Trajectory.Position(t), Vector3.one * size));
            }
            BoundsList.Add(new Bounds(Trajectory.Position(0), Vector3.one * Size));
            BoundsList.Add(new Bounds(Trajectory.Position(1), Vector3.one * Size));
        }

        public bool IntersectRay(Ray ray) => BoundsList.Any(b => b.IntersectRay(ray));
        public bool Intersects(Bounds bounds) => BoundsList.Any(b => b.Intersects(bounds));

        public void Render(RenderManager.CameraInfo cameraInfo, Color? color = null, float? width = null, bool? alphaBlend = null, bool? cut = null)
            => Trajectory.Render(cameraInfo, color, width, alphaBlend, cut);
    }
}
