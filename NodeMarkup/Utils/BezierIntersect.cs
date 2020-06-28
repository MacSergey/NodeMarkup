using ColossalFramework.Math;
using ColossalFramework.PlatformServices;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.Utils
{
    public static class MarkupLineIntersect
    {
        public static float DeltaAngle = 5f;
        public static float MaxLength = 1f;

        public static bool Calculate(MarkupLinePair pair, out LineIntersect intersect)
        {
            intersect = new LineIntersect() { Pair = pair, FirstT = -1, SecondT = -1 };

            if (pair.First.Start == pair.Second.Start)
            {
                intersect.FirstT = 0;
                intersect.SecondT = 0;
                return true;
            }
            else if (pair.First.Start == pair.Second.End)
            {
                intersect.FirstT = 0;
                intersect.SecondT = 1;
                return true;
            }
            else if (pair.First.End == pair.Second.Start)
            {
                intersect.FirstT = 1;
                intersect.SecondT = 0;
                return true;
            }
            else if (pair.First.End == pair.Second.End)
            {
                intersect.FirstT = 1;
                intersect.SecondT = 1;
                return true;
            }
            else
            {
                var isIntersect = Intersect(pair.First.Trajectory, pair.Second.Trajectory, out intersect);
                intersect.Pair = pair;
                return isIntersect;
            }
        }
        private static bool Intersect(Bezier3 first, Bezier3 second, out LineIntersect intersect)
        {
            intersect = new LineIntersect() {FirstT = -1, SecondT = -1 };

            if (!Line2.Intersect(VectorUtils.XZ(first.a), VectorUtils.XZ(first.d), VectorUtils.XZ(second.a), VectorUtils.XZ(second.d), out intersect.FirstT, out intersect.SecondT))
            {
                intersect.FirstT = -1;
                intersect.SecondT = -1;
                return false;
            }
            if(!intersect.IsIntersect)
            {
                intersect.FirstT = -1;
                intersect.SecondT = -1;
                return false;
            }


            var firstAngle = first.DeltaAngle();
            var secondAngle = second.DeltaAngle();

            if ((firstAngle <= DeltaAngle && secondAngle <= DeltaAngle) || intersect.IsDelta)
                return true;

            first.Divide(out Bezier3 firstA, out Bezier3 firstB, intersect.FirstT);
            second.Divide(out Bezier3 secondA, out Bezier3 secondB, intersect.SecondT);

            if (Intersect(firstA, secondA, true, true, ref intersect))
                return true;
            else if (Intersect(firstA, secondB, true, false, ref intersect))
                return true;
            else if (Intersect(firstB, secondA, false, true, ref intersect))
                return true;
            else if (Intersect(firstB, secondB, false, false, ref intersect))
                return true;
            else
                return false;
        }

        private static bool Intersect(Bezier3 first, Bezier3 second, bool IsFirstStart, bool IsSecondStart, ref LineIntersect intersect)
        {
            if (Intersect(first, second, out LineIntersect partIntersect))
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
