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
            var mustIntersect = pair.MustIntersect;

            if (pair.MustIntersect != false)
            {
                var firstTrajectory = GetTrajectory(pair.First, mustIntersect);
                var secondTrajectory = GetTrajectory(pair.Second, mustIntersect);

                var intersect = CalculateSingle(firstTrajectory, secondTrajectory);
                if (intersect.IsIntersect)
                    return new MarkupLinesIntersect(pair, intersect.FirstT, intersect.SecondT, intersect.Angle);
            }

            return new MarkupLinesIntersect(pair);

            static ITrajectory GetTrajectory(MarkupLine line, bool? mustIntersect)
                    => mustIntersect == true && line.Trajectory is StraightTrajectory straight ? new StraightTrajectory(straight.Trajectory, false) : line.Trajectory;
        }

        public float this[MarkupLine line] => Pair.First == line ? FirstT : (Pair.Second == line ? SecondT : -1);
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
