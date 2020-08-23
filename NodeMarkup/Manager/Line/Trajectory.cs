using ColossalFramework.Math;
using NodeMarkup.Utils;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public enum TrajectoryType
    {
        Line,
        Bezier
    }
    public interface ILineTrajectory
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
        ILineTrajectory Cut(float t0, float t1);
        void Divide(out ILineTrajectory trajectory1, out ILineTrajectory trajectory2);
        Vector3 Tangent(float t);
        Vector3 Position(float t);
        float Travel(float start, float distance);
        ILineTrajectory Invert();
        ILineTrajectory Copy();
    }
    public class BezierTrajectory : ILineTrajectory
    {
        public TrajectoryType TrajectoryType => TrajectoryType.Bezier;
        public Bezier3 Trajectory { get; }
        public float Length => Trajectory.Length();
        public float Magnitude => Direction.magnitude;
        public float DeltaAngle => Trajectory.DeltaAngle();
        public Vector3 Direction => Trajectory.d - Trajectory.a;
        public Vector3 StartDirection => Trajectory.b - Trajectory.a;
        public Vector3 EndDirection => Trajectory.c - Trajectory.d;
        public Vector3 StartPosition => Trajectory.a;
        public Vector3 EndPosition => Trajectory.d;
        public BezierTrajectory(Bezier3 trajectory)
        {
            Trajectory = trajectory;
        }

        public ILineTrajectory Cut(float t0, float t1) => new BezierTrajectory(Trajectory.Cut(t0, t1));
        public void Divide(out ILineTrajectory trajectory1, out ILineTrajectory trajectory2)
        {
            Trajectory.Divide(out Bezier3 bezier1, out Bezier3 bezier2);
            trajectory1 = new BezierTrajectory(bezier1);
            trajectory2 = new BezierTrajectory(bezier2);
        }
        public Vector3 Tangent(float t) => Trajectory.Tangent(t);
        public Vector3 Position(float t) => Trajectory.Position(t);
        public float Travel(float start, float distance) => Trajectory.Travel(start, distance);
        public ILineTrajectory Invert() => new BezierTrajectory(Trajectory.Invert());
        public ILineTrajectory Copy() => new BezierTrajectory(Trajectory);

        public static implicit operator Bezier3(BezierTrajectory trajectory) => trajectory.Trajectory;
        public static explicit operator BezierTrajectory(Bezier3 bezier) => new BezierTrajectory(bezier);
    }
    public class StraightTrajectory : ILineTrajectory
    {
        public TrajectoryType TrajectoryType => TrajectoryType.Line;
        public Line3 Trajectory { get; }
        public bool IsSection { get; }
        public float Length => Direction.magnitude;
        public float Magnitude => Length;
        public float DeltaAngle => 0f;
        public Vector3 Direction => Trajectory.b - Trajectory.a;
        public Vector3 StartDirection => Direction;
        public Vector3 EndDirection => -Direction;
        public Vector3 StartPosition => Trajectory.a;
        public Vector3 EndPosition => Trajectory.b;
        public StraightTrajectory(Line3 trajectory, bool isSection = true)
        {
            Trajectory = trajectory;
            IsSection = isSection;
        }
        public StraightTrajectory(Vector3 start, Vector3 end, bool isSection = true) : this(new Line3(start, end), isSection) { }

        public ILineTrajectory Cut(float t0, float t1) => new StraightTrajectory(Position(t0), Position(t1));

        public void Divide(out ILineTrajectory trajectory1, out ILineTrajectory trajectory2)
        {
            var middle = (Trajectory.a + Trajectory.b) / 2;
            trajectory1 = new StraightTrajectory(Trajectory.a, middle);
            trajectory2 = new StraightTrajectory(middle, Trajectory.b);
        }
        public Vector3 Tangent(float t) => Direction.normalized;
        public Vector3 Position(float t) => Trajectory.a + Direction * t;
        public float Travel(float start, float distance) => start + (distance / Length);
        public ILineTrajectory Invert() => new StraightTrajectory(Trajectory.b, Trajectory.a, IsSection);
        public ILineTrajectory Copy() => new StraightTrajectory(Trajectory, IsSection);

        public static implicit operator Line3(StraightTrajectory trajectory) => trajectory.Trajectory;
        public static explicit operator StraightTrajectory(Line3 line) => new StraightTrajectory(line);
    }
}
