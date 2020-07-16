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
    public static class MarkupLineIntersect
    {
        public static float DeltaAngle = 5f;
        public static float MaxLength = 1f;
        public static float MinLength = 0.5f;

        public static bool Calculate(MarkupLinePair pair, out LineIntersect intersect)
        {
            intersect = new LineIntersect() { Pair = pair, FirstT = -1, SecondT = -1 };

            if (pair.First.IsEnterLine || pair.Second.IsEnterLine || pair.First.Start == pair.Second.Start || pair.First.Start == pair.Second.End || pair.First.End == pair.Second.Start || pair.First.End == pair.Second.End)
                return false;
            else
            {
                var isIntersect = Intersect(pair.First.Trajectory, pair.Second.Trajectory, out intersect);
                intersect.Pair = pair;
                return isIntersect;
            }
        }

        private static bool Intersect(Bezier3 first, Bezier3 second, out LineIntersect intersect)
        {
            if (IntersectLines(first.a, first.d, second.a, second.d, out _, out _) &&
                Intersect(first, second, out int firstIndex, out int firstOf, out int secondIndex, out int secondOf))
            {
                intersect = new LineIntersect() { FirstT = 1f / firstOf * firstIndex, SecondT = 1f / secondOf * secondIndex };
                return true;
            }
            else
            {
                intersect = new LineIntersect() { FirstT = -1, SecondT = -1 };
                return false;
            }
        }
        private static bool Intersect(Bezier3 first, Bezier3 second, out int firstIndex, out int firstOf, out int secondIndex, out int secondOf)
        {
            CalcParts(first, out int firstParts, out float[] firstPoints, out Vector3[] firstPos);
            CalcParts(second, out int secondParts, out float[] secondPoints, out Vector3[] secondPos);

            if (firstParts == 1 && secondParts == 1)
            {
                IntersectLines(first.a, first.d, second.a, second.d, out float firstT, out float secondT);
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
                    if (IntersectLines(firstPos[i], firstPos[i + 1], secondPos[j], secondPos[j + 1], out float p, out float q))
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

        private static void CalcParts(Bezier3 bezier, out int parts, out float[] points, out Vector3[] positons)
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
        private static bool IntersectLines(Vector3 a, Vector3 b, Vector3 c, Vector3 d, out float p, out float q)
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



        private static bool IntersectV1(Bezier3 first, Bezier3 second, out LineIntersect intersect, int depth = 0)
        {
            intersect = new LineIntersect() { FirstT = -1, SecondT = -1 };

            if (!Line2.Intersect(VectorUtils.XZ(first.a), VectorUtils.XZ(first.d), VectorUtils.XZ(second.a), VectorUtils.XZ(second.d), out intersect.FirstT, out intersect.SecondT))
            {
                intersect.FirstT = -1;
                intersect.SecondT = -1;
                return false;
            }
            if (!intersect.IsIntersect)
            {
                intersect.FirstT = -1;
                intersect.SecondT = -1;
                return false;
            }


            var firstAngle = first.DeltaAngle();
            var secondAngle = second.DeltaAngle();

            if ((firstAngle <= DeltaAngle && secondAngle <= DeltaAngle) || intersect.IsDelta || depth >= 5)
                return true;

            first.Divide(out Bezier3 firstA, out Bezier3 firstB, intersect.FirstT);
            second.Divide(out Bezier3 secondA, out Bezier3 secondB, intersect.SecondT);

            if (IntersectV1(firstA, secondA, true, true, ref intersect, depth))
                return true;
            else if (IntersectV1(firstA, secondB, true, false, ref intersect, depth))
                return true;
            else if (IntersectV1(firstB, secondA, false, true, ref intersect, depth))
                return true;
            else if (IntersectV1(firstB, secondB, false, false, ref intersect, depth))
                return true;
            else
                return false;
        }
        private static bool IntersectV1(Bezier3 first, Bezier3 second, bool IsFirstStart, bool IsSecondStart, ref LineIntersect intersect, int depth)
        {
            if (IntersectV1(first, second, out LineIntersect partIntersect, depth + 1))
            {
                intersect.FirstT = CalculateT(intersect.FirstT, partIntersect.FirstT, IsFirstStart);
                intersect.SecondT = CalculateT(intersect.SecondT, partIntersect.SecondT, IsSecondStart);
                return true;
            }
            return false;
        }
        private static float CalculateT(float t, float tempT, bool IsStart) => IsStart ? t * tempT : t + (1 - t) * tempT;
    }
    public struct LineIntersect
    {
        public MarkupLinePair Pair;
        public float FirstT;
        public float SecondT;

        public bool IsIntersect => 0 <= FirstT && FirstT <= 1 && 0 <= SecondT && SecondT <= 1;
        public bool IsDelta => (FirstT <= 0.05f || 0.95f <= FirstT) && (SecondT <= 0.05f || 0.95f <= SecondT);
        public float this[MarkupLine line] => Pair.First == line ? FirstT : (Pair.Second == line ? SecondT : -1);

        public static LineIntersect NotIntersect(MarkupLinePair pair) => new LineIntersect() { Pair = pair, FirstT = -1, SecondT = -1 };
    }
}
