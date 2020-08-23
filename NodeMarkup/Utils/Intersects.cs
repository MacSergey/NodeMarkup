using ColossalFramework.Math;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeMarkup.Utils
{
    public class MarkupIntersect
    {
        public static float DeltaAngle = 5f;
        public static float MaxLength = 1f;
        public static float MinLength = 0.5f;
        public static MarkupIntersectComparer FirstComparer { get; } = new MarkupIntersectComparer(true);
        public static MarkupIntersectComparer SecondComparer { get; } = new MarkupIntersectComparer(false);
        public static MarkupIntersect NotIntersect => new MarkupIntersect();

        public float FirstT { get; protected set; }
        public float SecondT { get; protected set; }
        public float Angle { get; private set; }
        public bool IsIntersect { get; }

        public MarkupIntersect(float firstT, float secondT, float angle)
        {
            IsIntersect = true;
            FirstT = firstT;
            SecondT = secondT;
            Angle = angle;
        }
        protected MarkupIntersect()
        {
            IsIntersect = false;
        }

        public static MarkupIntersect CalculateSingle(ILineTrajectory trajectory1, ILineTrajectory trajectory2) => Calculate(trajectory1, trajectory2).FirstOrDefault() ?? NotIntersect;
        public static List<MarkupIntersect> Calculate(ILineTrajectory trajectory1, ILineTrajectory trajectory2)
        {
            if (trajectory1.TrajectoryType == TrajectoryType.Bezier)
            {
                if (trajectory2.TrajectoryType == TrajectoryType.Bezier)
                    return Calculate(trajectory1 as BezierTrajectory, trajectory2 as BezierTrajectory);
                else if (trajectory2.TrajectoryType == TrajectoryType.Line)
                    return Calculate(trajectory1 as BezierTrajectory, trajectory2 as StraightTrajectory);
            }
            else if (trajectory1.TrajectoryType == TrajectoryType.Line)
            {
                if (trajectory2.TrajectoryType == TrajectoryType.Bezier)
                    return Calculate(trajectory1 as StraightTrajectory, trajectory2 as BezierTrajectory);
                else if (trajectory2.TrajectoryType == TrajectoryType.Line)
                    return Calculate(trajectory1 as StraightTrajectory, trajectory2 as StraightTrajectory);
            }

            return new List<MarkupIntersect>();
        }
        public static List<MarkupIntersect> Calculate(ILineTrajectory trajectory, IEnumerable<ILineTrajectory> otherTrajectories, bool onlyIntersect = false) 
            => otherTrajectories.SelectMany(t => Calculate(trajectory, t)).Where(i => !onlyIntersect || i.IsIntersect).ToList();

        #region BEZIER - BEZIER
        public static List<MarkupIntersect> Calculate(BezierTrajectory bezier1, BezierTrajectory bezier2)
        {
            var intersects = new List<MarkupIntersect>();
            Intersect(intersects, bezier1, bezier2);
            return intersects;
        }
        private static bool Intersect(List<MarkupIntersect> results, Bezier3 first, Bezier3 second, int fIdx = 0, int fOf = 1, int sIdx = 0, int sOf = 1)
        {
            CalcParts(first, out int fParts, out float[] fPoints, out Vector3[] fPos);
            CalcParts(second, out int sParts, out float[] sPoints, out Vector3[] sPos);

            if (fParts == 1 && sParts == 1)
            {
                IntersectSections(first.a, first.d, second.a, second.d, out float firstT, out float secondT);
                firstT = 1f / fOf * (fIdx + firstT);
                secondT = 1f / sOf * (sIdx + secondT);
                var angle = Vector2.Angle(first.d.XZ() - first.a.XZ(), second.d.XZ() - second.a.XZ()) * Mathf.Deg2Rad;
                results.Add(new MarkupIntersect(firstT, secondT, angle));
                return true;
            }

            for (var i = 0; i < fParts; i += 1)
            {
                for (var j = 0; j < sParts; j += 1)
                {
                    if (IntersectSections(fPos[i], fPos[i + 1], sPos[j], sPos[j + 1], out float p, out float q))
                    {
                        foreach (var ii in WillTryParts(i, fParts, p))
                        {
                            foreach (var jj in WillTryParts(j, sParts, q))
                            {
                                var firstCut = first.Cut(fPoints[ii], fPoints[ii + 1]);
                                var secondCut = second.Cut(sPoints[jj], sPoints[jj + 1]);

                                if (Intersect(results, firstCut, secondCut, fIdx * fParts + ii, fOf * fParts, sIdx * sParts + jj, sOf * sParts))
                                    return true;
                            }
                        }
                    }
                }
            }

            return false;
        }
        private static bool IntersectSections(Vector3 a, Vector3 b, Vector3 c, Vector3 d, out float p, out float q)
            => Line2.Intersect(a.XZ(), b.XZ(), c.XZ(), d.XZ(), out p, out q) && CorrectT(p) && CorrectT(q);
        private static IEnumerable<int> WillTryParts(int i, int count, float p)
        {
            yield return i;
            if (p < 0.1f && i != 0)
                yield return i - 1;
            if (0.9f < p && i + 1 < count)
                yield return i + 1;
        }

        #endregion

        #region BEZIER - STRAIGHT

        public static List<MarkupIntersect> Calculate(StraightTrajectory straight, BezierTrajectory bezier)
        {
            var intersects = new List<MarkupIntersect>();
            Intersect(intersects, straight, bezier, false);
            return intersects;
        }
        public static List<MarkupIntersect> Calculate(BezierTrajectory bezier, StraightTrajectory straight)
        {
            var intersects = new List<MarkupIntersect>();
            Intersect(intersects, straight, bezier, true);
            return intersects;
        }

        private static void Intersect(List<MarkupIntersect> results, StraightTrajectory line, BezierTrajectory bezier, bool invert, int idx = 0, int of = 1)
        {
            CalcParts(bezier, out int parts, out float[] points, out Vector3[] pos);

            if(parts > 1)
            {
                for (var i = 0; i < parts; i += 1)
                {
                    if (IntersectSectionAndRay(line, pos[i], pos[i + 1], out _, out _))
                    {
                        var cut = bezier.Cut(points[i], points[i + 1]) as BezierTrajectory;
                        Intersect(results, line, cut,  invert, idx * parts + i, of * parts);
                    }
                }
            }
            else if (IntersectSectionAndRay(line, bezier.StartPosition, bezier.EndPosition, out float p, out float q))
            {
                var tangent = bezier.Tangent(p);
                var angle = Vector3.Angle(tangent, line.Direction);
                q = 1f / of * (idx + q);
                var result = new MarkupIntersect(invert ? q : p, invert ? p : q, (angle > 90 ? 180 - angle : angle) * Mathf.Deg2Rad);
                results.Add(result);
            }
        }
        private static bool IntersectSectionAndRay(StraightTrajectory line, Vector3 start, Vector3 end, out float p, out float q) =>
            Line2.Intersect(line.StartPosition.XZ(), line.EndPosition.XZ(), start.XZ(), end.XZ(), out p, out q) && (!line.IsSection || CorrectT(p)) && CorrectT(q);

        #endregion

        #region STRAIGHT - STRAIGHT
        public static List<MarkupIntersect> Calculate(StraightTrajectory straight1, StraightTrajectory straight2)
        {
            var intersects = new List<MarkupIntersect>();
            var trajectory1 = straight1.Trajectory;
            var trajectory2 = straight2.Trajectory;
            if (Line2.Intersect(trajectory1.a.XZ(), trajectory1.b.XZ(), trajectory2.a.XZ(), trajectory2.b.XZ(), out float p, out float q) && (!straight1.IsSection || CorrectT(p)) && (!straight2.IsSection || CorrectT(q)))
            {
                var angle = Vector2.Angle(trajectory1.b.XZ() - trajectory1.a.XZ(), trajectory2.b.XZ() - trajectory2.a.XZ()) * Mathf.Deg2Rad;
                var intersect = new MarkupIntersect(p, q, angle);
                intersects.Add(intersect);
            }
            return intersects;
        }
        #endregion


        protected static void CalcParts(Bezier3 bezier, out int parts, out float[] points, out Vector3[] positons)
        {
            bezier.Divide(out Bezier3 b1, out Bezier3 b2);
            var length = (b1.d - b1.a).magnitude + (b2.d - b2.a).magnitude;
            parts = Math.Min((int)Math.Ceiling(length / MinLength), 10);

            points = new float[parts + 1];
            points[0] = 0;
            points[parts] = 1;

            positons = new Vector3[parts + 1];
            positons[0] = bezier.a;
            positons[parts] = bezier.d;

            for (var i = 1; i < parts; i += 1)
            {
                points[i] = (1f / parts) * i;
                positons[i] = bezier.Position(points[i]);
            }
        }
        public static bool CorrectT(float t) => 0 <= t && t <= 1;
    }
    public class MarkupLinesIntersect : MarkupIntersect
    {
        public MarkupLinePair Pair { get; private set; }
        public Vector3 Position => (Pair.First.Trajectory.Position(FirstT) + Pair.Second.Trajectory.Position(SecondT)) / 2;
        protected MarkupLinesIntersect(MarkupLinePair pair, float firstT, float secondT, float angle) : base(firstT, secondT, angle)
        {
            Pair = pair;
        }
        protected MarkupLinesIntersect(MarkupLinePair pair) : base()
        {
            Pair = pair;
        }

        public static MarkupLinesIntersect Calculate(MarkupLine first, MarkupLine second) => Calculate(new MarkupLinePair(first, second));
        public static MarkupLinesIntersect Calculate(MarkupLinePair pair)
        {
            if (pair.CanIntersect && Calculate(pair.First.Trajectory, pair.Second.Trajectory).FirstOrDefault() is MarkupIntersect intersect && intersect.IsIntersect)
                return new MarkupLinesIntersect(pair, intersect.FirstT, intersect.SecondT, intersect.Angle);
            else
                return new MarkupLinesIntersect(pair);
        }

        public float this[MarkupLine line] => Pair.First == line ? FirstT : (Pair.Second == line ? SecondT : -1);
    }

    public class MarkupIntersectComparer : IComparer<MarkupIntersect>
    {
        bool _isFirst;
        public MarkupIntersectComparer(bool isFirst = true)
        {
            _isFirst = isFirst;
        }
        public int Compare(MarkupIntersect x, MarkupIntersect y) => _isFirst ? x.FirstT.CompareTo(y.FirstT) : x.SecondT.CompareTo(y.SecondT);
    }
}
