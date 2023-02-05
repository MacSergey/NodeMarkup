using IMT.Manager;
using ModsCommon.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace IMT.Utilities
{
    public readonly struct MarkingLinesIntersect
    {
        public readonly MarkingLinePair pair;
        public readonly Intersection intersection;
        public float FirstT => intersection.firstT;
        public float SecondT => intersection.secondT;
        public bool IsIntersect => intersection.isIntersect;
        public Vector3 Position => (pair.first.Trajectory.Position(FirstT) + pair.second.Trajectory.Position(SecondT)) / 2;
        private MarkingLinesIntersect(MarkingLinePair pair, Intersection intersection)
        {
            this.pair = pair;
            this.intersection = intersection;
        }

        public static MarkingLinesIntersect Calculate(MarkingLine first, MarkingLine second) => Calculate(new MarkingLinePair(first, second));
        public static MarkingLinesIntersect Calculate(MarkingLinePair pair)
        {
            var mustIntersect = pair.MustIntersect;

            if (pair.MustIntersect != false)
            {
                var firstTrajectory = GetTrajectory(pair.first, mustIntersect);
                var secondTrajectory = GetTrajectory(pair.second, mustIntersect);

                var intersect = Intersection.CalculateSingle(firstTrajectory, secondTrajectory);
                if (intersect.isIntersect)
                    return new MarkingLinesIntersect(pair, intersect);
            }

            return new MarkingLinesIntersect(pair, Intersection.NotIntersect);

            static ITrajectory GetTrajectory(MarkingLine line, bool? mustIntersect)
            {
                return mustIntersect == true && line.Trajectory is StraightTrajectory straight ? new StraightTrajectory(straight.Trajectory, false) : line.Trajectory;
            }
        }

        public float this[MarkingLine line] => pair.first == line ? FirstT : (pair.second == line ? SecondT : -1);
    }

    public class MarkupIntersectComparer : IComparer<Intersection>
    {
        private readonly bool isFirst;
        public MarkupIntersectComparer(bool isFirst = true)
        {
            this.isFirst = isFirst;
        }
        public int Compare(Intersection x, Intersection y) => isFirst ? x.firstT.CompareTo(y.firstT) : x.secondT.CompareTo(y.secondT);
    }
}
