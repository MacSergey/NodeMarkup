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
    public abstract class MarkupIntersect
    {
        public static float DeltaAngle = 5f;
        public static float MaxLength = 1f;
        public static float MinLength = 0.5f;

        public float FirstT { get; protected set; }
        public float SecondT { get; protected set; }

        public MarkupIntersect(float firstT, float secondT)
        {
            FirstT = firstT;
            SecondT = secondT;
        }

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
    public class MarkupBeziersIntersect : MarkupIntersect
    {
        public MarkupLinePair Pair { get; private set; }

        public bool IsIntersect => 0 <= FirstT && FirstT <= 1 && 0 <= SecondT && SecondT <= 1;
        public float this[MarkupLine line] => Pair.First == line ? FirstT : (Pair.Second == line ? SecondT : -1);
        public Vector3 Position => (Pair.First.Trajectory.Position(FirstT) + Pair.Second.Trajectory.Position(SecondT)) / 2;

        public MarkupBeziersIntersect(MarkupLinePair pair, float firstT, float secondT) : base (firstT, secondT)
        {
            Pair = pair;
        }

        public static MarkupBeziersIntersect NotIntersect(MarkupLinePair pair) => new MarkupBeziersIntersect(pair, -1, -1);

        public static bool Calculate(MarkupLinePair pair, out MarkupBeziersIntersect intersect)
        {
            intersect = NotIntersect(pair);

            if (!pair.CanIntersect)
                return false;
            else
            {
                var isIntersect = Intersect(pair.First.Trajectory, pair.Second.Trajectory, out float firstT, out float secondT);
                intersect.FirstT = firstT;
                intersect.SecondT = secondT;
                return isIntersect;
            }
        }

        public static bool Intersect(Bezier3 first, Bezier3 second, out float firstT, out float secondT)
        {
            if (/*IntersectSections(first.a, first.d, second.a, second.d, out _, out _) && */Intersect(first, second, out int firstIndex, out int firstOf, out int secondIndex, out int secondOf))
            {
                firstT = 1f / firstOf * firstIndex;
                secondT = 1f / secondOf * secondIndex;
                return true;
            }
            else
            {
                firstT = -1;
                secondT = -1;
                return false;
            }
        }
        private static bool Intersect(Bezier3 first, Bezier3 second, out int firstIndex, out int firstOf, out int secondIndex, out int secondOf)
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
                return true;
            }

            for (var i = 0; i < firstParts; i += 1)
            {
                for (var j = 0; j < secondParts; j += 1)
                {
                    if (IntersectSections(firstPos[i], firstPos[i + 1], secondPos[j], secondPos[j + 1], out float p, out float q))
                    {
                        if (Intersect(first, second, firstPoints, secondPoints, WillTryParts(i, firstParts, p), WillTryParts(j, secondParts, q), out int resI, out int resJ, out firstIndex, out firstOf, out secondIndex, out secondOf))
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
            return false;
        }
        private static bool Intersect(Bezier3 first, Bezier3 second, float[] firstPoints, float[] secondPoints, IEnumerable<int> firstIs, IEnumerable<int> secondJs, out int resI, out int resJ, out int firstIndex, out int firstOf, out int secondIndex, out int secondOf)
        {
            foreach (var i in firstIs)
            {
                foreach (var j in secondJs)
                {
                    var firstCut = first.Cut(firstPoints[i], firstPoints[i + 1]);
                    var secondCut = second.Cut(secondPoints[j], secondPoints[j + 1]);

                    if (Intersect(firstCut, secondCut, out firstIndex, out firstOf, out secondIndex, out secondOf))
                    {
                        resI = i;
                        resJ = j;
                        return true;
                    }
                }
            }

            firstIndex = firstOf = secondIndex = secondOf = resI = resJ = 0;
            return false;
        }

        private static bool IntersectSections(Vector3 a, Vector3 b, Vector3 c, Vector3 d, out float p, out float q)
        {
            if (Line2.Intersect(VectorUtils.XZ(a), VectorUtils.XZ(b), VectorUtils.XZ(c), VectorUtils.XZ(d), out p, out q))
                if ((0 <= p && p <= 1) && (0 <= q && q <= 1))
                    return true;
            return false;
        }        
        private static IEnumerable<int> WillTryParts(int i, int count, float p)
        {
            yield return i;
            if (p < 0.1f && i != 0)
                yield return i - 1;
            if (0.9f < p && i + 1 < count)
                yield return i + 1;
        }
    }
    public class MarkupBezierLineIntersect : MarkupIntersect, IComparable<MarkupBezierLineIntersect>
    {
        public float Angle { get; private set; }
        public MarkupBezierLineIntersect(float firstT, float secondT, float angle) : base(firstT, secondT)
        {
            Angle = angle;
        }

        public static List<MarkupBezierLineIntersect> Intersect(Bezier3 bezier, Vector3 from, Vector3 to)
        {
            var ts = new List<MarkupBezierLineIntersect>();
            Intersect(bezier, from, to, ts);
            return ts;
        }
        private static bool Intersect(Bezier3 bezier, Vector3 from, Vector3 to, List<MarkupBezierLineIntersect> results)
        {
            CalcParts(bezier, out int parts, out float[] points, out Vector3[] pos);

            if (parts == 1)
            {
                if (IntersectSectionAndRay(bezier.a, bezier.d, from, to, out float p, out float t))
                {
                    var tangent = bezier.Tangent(p);
                    var angle = Vector3.Angle(tangent, to - from);
                    var result = new MarkupBezierLineIntersect(t, -1, (angle > 90 ? 180 -angle : angle) * Mathf.Deg2Rad);
                    results.Add(result);
                    return true;
                }
                else
                    return false;
            }

            bool intersect = false;
            for (var i = 0; i < parts; i += 1)
            {
                if (IntersectSectionAndRay(pos[i], pos[i + 1], from, to, out float p, out float q))
                {
                    var cut = bezier.Cut(points[i], points[i + 1]);
                    intersect |= Intersect(cut, from, to, results);
                }
            }
            return intersect;
        }
        private static bool IntersectSectionAndRay(Vector3 a, Vector3 b, Vector3 c, Vector3 d, out float p, out float t)
        {
            if (Line2.Intersect(VectorUtils.XZ(a), VectorUtils.XZ(b), VectorUtils.XZ(c), VectorUtils.XZ(d), out p, out t))
                if ((0 <= p && p <= 1))
                    return true;
            return false;
        }
        public override bool Equals(object obj) => obj is MarkupBezierLineIntersect fillerIntersect && fillerIntersect.FirstT == FirstT;
        public override int GetHashCode() => FirstT.GetHashCode();

        public int CompareTo(MarkupBezierLineIntersect other) => FirstT.CompareTo(other.FirstT);
    }
}
