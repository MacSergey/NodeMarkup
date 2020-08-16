using ColossalFramework.Math;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                    return Calculate((trajectory1 as BezierTrajectory).Trajectory, (trajectory2 as BezierTrajectory).Trajectory);
                else if (trajectory2.TrajectoryType == TrajectoryType.Line)
                    return Calculate((trajectory1 as BezierTrajectory).Trajectory, (trajectory2 as StraightTrajectory).Trajectory);
            }
            else if (trajectory1.TrajectoryType == TrajectoryType.Line)
            {
                if (trajectory2.TrajectoryType == TrajectoryType.Bezier)
                    return Calculate((trajectory1 as StraightTrajectory).Trajectory, (trajectory2 as BezierTrajectory).Trajectory);
                else if (trajectory2.TrajectoryType == TrajectoryType.Line)
                    return Calculate((trajectory1 as StraightTrajectory).Trajectory, (trajectory2 as StraightTrajectory).Trajectory);
            }

            return new List<MarkupIntersect>();
        }

        #region BEZIER - BEZIER
        public static List<MarkupIntersect> Calculate(Bezier3 bezier1, Bezier3 bezier2)
        {
            var intersects = new List<MarkupIntersect>();
            if (Intersect(bezier1, bezier2, out int firstIndex, out int firstOf, out int secondIndex, out int secondOf, out float angle))
            {
                var intersect = new MarkupIntersect(1f / firstOf * firstIndex, 1f / secondOf * secondIndex, angle);
                intersects.Add(intersect);
            }
            return intersects;
        }
        private static bool Intersect(Bezier3 first, Bezier3 second, out int firstIndex, out int firstOf, out int secondIndex, out int secondOf, out float angle)
        {
            CalcParts(first, out int firstParts, out float[] firstPoints, out Vector3[] firstPos);
            CalcParts(second, out int secondParts, out float[] secondPoints, out Vector3[] secondPos);

            if (firstParts == 1 && secondParts == 1)
            {
                IntersectSections(first.a, first.d, second.a, second.d, out float firstT, out float secondT);
                firstIndex = (int)(firstT * 100).RoundToNearest(1f);
                firstOf = 100;
                secondIndex = (int)(secondT * 100).RoundToNearest(1f);
                secondOf = 100;
                angle = Vector2.Angle(first.d.XZ() - first.a.XZ(), second.d.XZ() - second.a.XZ()) * Mathf.Deg2Rad;
                return true;
            }

            for (var i = 0; i < firstParts; i += 1)
            {
                for (var j = 0; j < secondParts; j += 1)
                {
                    if (IntersectSections(firstPos[i], firstPos[i + 1], secondPos[j], secondPos[j + 1], out float p, out float q))
                    {
                        if (Intersect(first, second, firstPoints, secondPoints, WillTryParts(i, firstParts, p), WillTryParts(j, secondParts, q), out int resI, out int resJ, out firstIndex, out firstOf, out secondIndex, out secondOf, out angle))
                        {
                            firstIndex += resI * firstOf;
                            firstOf *= firstParts;
                            secondIndex += resJ * secondOf;
                            secondOf *= secondParts;
                            return true;
                        }
                        else
                            return false;
                    }
                }
            }

            firstIndex = firstOf = secondIndex = secondOf = 0;
            angle = 0;
            return false;
        }
        private static bool Intersect(Bezier3 first, Bezier3 second, float[] firstPoints, float[] secondPoints, IEnumerable<int> firstIs, IEnumerable<int> secondJs, out int resI, out int resJ, out int firstIndex, out int firstOf, out int secondIndex, out int secondOf, out float angle)
        {
            foreach (var i in firstIs)
            {
                foreach (var j in secondJs)
                {
                    var firstCut = first.Cut(firstPoints[i], firstPoints[i + 1]);
                    var secondCut = second.Cut(secondPoints[j], secondPoints[j + 1]);

                    if (Intersect(firstCut, secondCut, out firstIndex, out firstOf, out secondIndex, out secondOf, out angle))
                    {
                        resI = i;
                        resJ = j;
                        return true;
                    }
                }
            }

            firstIndex = firstOf = secondIndex = secondOf = resI = resJ = 0;
            angle = 0;
            return false;
        }
        private static bool IntersectSections(Vector3 a, Vector3 b, Vector3 c, Vector3 d, out float p, out float q)
            => Line2.Intersect(a.XZ(), b.XZ(), c.XZ(), d.XZ(), out p, out q) && (0 <= p && p <= 1) && (0 <= q && q <= 1);
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

        public static List<MarkupIntersect> Calculate(Line3 straight, Bezier3 bezier)
        {
            var intersects = new List<MarkupIntersect>();
            Intersect(straight, bezier, intersects, false);
            return intersects;
        }
        public static List<MarkupIntersect> Calculate(Bezier3 bezier, Line3 straight)
        {
            var intersects = new List<MarkupIntersect>();
            Intersect(straight, bezier, intersects, true);
            return intersects;
        }

        private static bool Intersect(Line3 line, Bezier3 bezier, List<MarkupIntersect> results, bool invert)
        {
            CalcParts(bezier, out int parts, out float[] points, out Vector3[] pos);

            if (parts == 1)
            {
                if (IntersectSectionAndRay(line, bezier.a, bezier.d, out float p, out float t))
                {
                    var tangent = bezier.Tangent(p);
                    var angle = Vector3.Angle(tangent, line.b - line.a);
                    var result = new MarkupIntersect(invert ? t : p, invert ? p : t, (angle > 90 ? 180 - angle : angle) * Mathf.Deg2Rad);
                    results.Add(result);
                    return true;
                }
                else
                    return false;
            }

            bool intersect = false;
            for (var i = 0; i < parts; i += 1)
            {
                if (IntersectSectionAndRay(line, pos[i], pos[i + 1], out _, out _))
                {
                    var cut = bezier.Cut(points[i], points[i + 1]);
                    intersect |= Intersect(line, cut, results, invert);
                }
            }
            return intersect;
        }
        private static bool IntersectSectionAndRay(Line3 line, Vector3 start, Vector3 end, out float p, out float t) =>
            Line2.Intersect(line.a.XZ(), line.b.XZ(), start.XZ(), end.XZ(), out p, out t) && 0 <= t && t <= 1;

        #endregion

        #region STRAIGHT - STRAIGHT
        public static List<MarkupIntersect> Calculate(Line3 straight1, Line3 straight2)
        {
            var intersects = new List<MarkupIntersect>();
            if (Line2.Intersect(straight1.a.XZ(), straight1.b.XZ(), straight2.a.XZ(), straight2.b.XZ(), out float p, out float q))
            {
                var angle = Vector2.Angle(straight1.b.XZ() - straight1.a.XZ(), straight2.b.XZ() - straight2.a.XZ()) * Mathf.Deg2Rad;
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

        public static MarkupLinesIntersect Calculate(MarkupLinePair pair)
        {
            if (Calculate(pair.First.Trajectory, pair.Second.Trajectory).FirstOrDefault() is MarkupIntersect intersect && intersect.IsIntersect)
                return new MarkupLinesIntersect(pair, intersect.FirstT, intersect.SecondT, intersect.Angle);
            else
                return new MarkupLinesIntersect(pair);
        }

        public float? this[MarkupLine line] => Pair.First == line ? FirstT : (Pair.Second == line ? SecondT : -1);
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
