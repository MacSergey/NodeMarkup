using ColossalFramework.Math;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeMarkup.Utilities
{
    public class MarkupLinesIntersect : Intersection
    {
        public MarkingLinePair Pair { get; private set; }
        public Vector3 Position => (Pair.First.Trajectory.Position(FirstT) + Pair.Second.Trajectory.Position(SecondT)) / 2;
        protected MarkupLinesIntersect(MarkingLinePair pair, float firstT, float secondT) : base(firstT, secondT)
        {
            Pair = pair;
        }
        protected MarkupLinesIntersect(MarkingLinePair pair) : base()
        {
            Pair = pair;
        }

        public static MarkupLinesIntersect Calculate(MarkingLine first, MarkingLine second) => Calculate(new MarkingLinePair(first, second));
        public static MarkupLinesIntersect Calculate(MarkingLinePair pair)
        {
            var mustIntersect = pair.MustIntersect;

            if (pair.MustIntersect != false)
            {
                var firstTrajectory = GetTrajectory(pair.First, mustIntersect);
                var secondTrajectory = GetTrajectory(pair.Second, mustIntersect);

                var intersect = CalculateSingle(firstTrajectory, secondTrajectory);
                if (intersect.IsIntersect)
                    return new MarkupLinesIntersect(pair, intersect.FirstT, intersect.SecondT);
            }

            return new MarkupLinesIntersect(pair);

            static ITrajectory GetTrajectory(MarkingLine line, bool? mustIntersect)
                    => mustIntersect == true && line.Trajectory is StraightTrajectory straight ? new StraightTrajectory(straight.Trajectory, false) : line.Trajectory;
        }

        public float this[MarkingLine line] => Pair.First == line ? FirstT : (Pair.Second == line ? SecondT : -1);
    }

    public class MarkupIntersectComparer : IComparer<Intersection>
    {
        private readonly bool _isFirst;
        public MarkupIntersectComparer(bool isFirst = true)
        {
            _isFirst = isFirst;
        }
        public int Compare(Intersection x, Intersection y) => _isFirst ? x.FirstT.CompareTo(y.FirstT) : x.SecondT.CompareTo(y.SecondT);
    }
}
