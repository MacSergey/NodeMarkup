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
    public class MarkupLineIntersect
    {
        public static float DeltaAngle = 5f;
        public static float MaxLength = 1f;
        public static float MinLength = 0.5f;


        public MarkupLinePair Pair { get; private set; }
        public float FirstT { get; private set; }
        public float SecondT { get; private set; }

        public bool IsIntersect => 0 <= FirstT && FirstT <= 1 && 0 <= SecondT && SecondT <= 1;
        public bool IsDelta => (FirstT <= 0.05f || 0.95f <= FirstT) && (SecondT <= 0.05f || 0.95f <= SecondT);
        public float this[MarkupLine line] => Pair.First == line ? FirstT : (Pair.Second == line ? SecondT : -1);
        public Vector3 Position => (Pair.First.Trajectory.Position(FirstT) + Pair.Second.Trajectory.Position(SecondT)) / 2;

        public MarkupLineIntersect(MarkupLinePair pair, float firstT, float secondT)
        {
            Pair = pair;
            FirstT = firstT;
            SecondT = secondT;
        }

        public static MarkupLineIntersect NotIntersect(MarkupLinePair pair) => new MarkupLineIntersect(pair, -1, -1);

        public static bool Calculate(MarkupLinePair pair, out MarkupLineIntersect intersect)
        {
            intersect = NotIntersect(pair);

            if (pair.First.IsEnterLine || pair.Second.IsEnterLine || pair.First.Start == pair.Second.Start || pair.First.Start == pair.Second.End || pair.First.End == pair.Second.Start || pair.First.End == pair.Second.End)
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
            if (IntersectSections(first.a, first.d, second.a, second.d, out _, out _) && Intersect(first, second, out int firstIndex, out int firstOf, out int secondIndex, out int secondOf))
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

        public static bool Intersect(Bezier3 bezier, Vector3 from, Vector3 to, out float firstT, out float lineT)
        {
            if (IntersectSectionAndRay(bezier.a, bezier.d, from, to, out _, out _) && Intersect(bezier, from, to, out int firstIndex, out int firstOf, out lineT))
            {
                firstT = 1f / firstOf * firstIndex;
                return true;
            }
            else
            {
                firstT = -1;
                lineT = -1;
                return false;
            }
        }
        private static bool Intersect(Bezier3 bezier, Vector3 from, Vector3 to, out int firstIndex, out int firstOf, out float lineT)
        {
            CalcParts(bezier, out int parts, out float[] points, out Vector3[] pos);

            if (parts == 1)
            {
                IntersectSectionAndRay(bezier.a, bezier.d, from, to, out float firstT, out lineT);
                firstIndex = (int)(firstT * 100).RoundToNearest(1f);
                firstOf = 100;
                return true;
            }

            for (var i = 0; i < parts; i += 1)
            {
                if (IntersectSectionAndRay(pos[i], pos[i + 1], from, to, out float p, out float q))
                {
                    if (Intersect(bezier, from, to, points, WillTryParts(i, parts, p), out int resI, out firstIndex, out firstOf, out lineT))
                    {
                        firstIndex += resI * firstOf;
                        firstOf *= parts;
                        return true;
                    }
                    else
                        return false;
                }
            }

            firstIndex = firstOf = 0;
            lineT = 0;
            return false;
        }
        private static bool Intersect(Bezier3 first, Vector3 from, Vector3 to, float[] points, IEnumerable<int> _is, out int resI, out int index, out int of, out float lineT)
        {
            foreach (var i in _is)
            {
                var firstCut = first.Cut(points[i], points[i + 1]);

                if (Intersect(firstCut, from, to, out index, out of, out lineT))
                {
                    resI = i;
                    return true;
                }
            }

            index = of = resI = 0;
            lineT = 0f;
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
        private static bool IntersectSections(Vector3 a, Vector3 b, Vector3 c, Vector3 d, out float p, out float q)
        {
            if (Line2.Intersect(VectorUtils.XZ(a), VectorUtils.XZ(b), VectorUtils.XZ(c), VectorUtils.XZ(d), out p, out q))
                if ((0 <= p && p <= 1) && (0 <= q && q <= 1))
                    return true;
            return false;
        }
        private static bool IntersectSectionAndRay(Vector3 a, Vector3 b, Vector3 c, Vector3 d, out float p, out float q)
        {
            if (Line2.Intersect(VectorUtils.XZ(a), VectorUtils.XZ(b), VectorUtils.XZ(c), VectorUtils.XZ(d), out p, out q))
                if ((0 <= p && p <= 1))
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
}
