using ModsCommon.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public class ContourGroup : List<Contour>
    {
        private Rect? limits;
        private Vector3[] points;

        public Rect Limits => limits ??= this.GetLimits();
        public Vector3[] Points => points ??= this.GetPoints();

        public ContourGroup() { }
        public ContourGroup(IEnumerable<Contour> edges) : base(edges) { }

        public new void Add(Contour edge)
        {
            base.Add(edge);
            limits = null;
            points = null;
        }
        public new void AddRange(IEnumerable<Contour> edges)
        {
            base.AddRange(edges);
            limits = null;
            points = null;
        }
        public new void RemoveAt(int index)
        {
            base.RemoveAt(index);
            limits = null;
            points = null;
        }

        public bool CanIntersect(in StraightTrajectory line, bool precise) => precise ? Intersection.CanIntersect(Points, line, out _) : Intersection.CanIntersect(Limits, line, out _);
    }
    public class Contour : List<ContourEdge>
    {
        public TrajectoryHelper.Direction Direction => this.Select(i => i.trajectory).GetDirection();

        private Rect? limits;
        private Vector3[] points;

        public Rect Limits
        {
            get
            {
                if (limits == null)
                {
                    var limits = new Rect();
                    for (var i = 0; i < Count; i += 1)
                    {
                        if (i == 0)
                        {
                            var pos = this[i].trajectory.StartPosition;
                            limits = Rect.MinMaxRect(pos.x, pos.z, pos.x, pos.z);
                            continue;
                        }

                        switch (this[i].trajectory)
                        {
                            case BezierTrajectory bezierTrajectory:
                                SetRect(ref limits, bezierTrajectory.Trajectory.a);
                                SetRect(ref limits, bezierTrajectory.Trajectory.b);
                                SetRect(ref limits, bezierTrajectory.Trajectory.c);
                                SetRect(ref limits, bezierTrajectory.Trajectory.d);
                                break;
                            case StraightTrajectory straightTrajectory:
                                SetRect(ref limits, straightTrajectory.Trajectory.a);
                                SetRect(ref limits, straightTrajectory.Trajectory.b);
                                break;
                        }
                    }
                    this.limits = limits;
                }
                return limits.Value;

                static void SetRect(ref Rect rect, Vector3 pos)
                {
                    if (pos.x < rect.xMin)
                        rect.xMin = pos.x;
                    else if (pos.x > rect.xMax)
                        rect.xMax = pos.x;

                    if (pos.z < rect.yMin)
                        rect.yMin = pos.z;
                    else if (pos.z > rect.yMax)
                        rect.yMax = pos.z;
                }
            }
        }
        public Vector3[] Points
        {
            get
            {
                if (points == null)
                {
                    var count = 0;

                    for (var i = 0; i < Count; i += 1)
                    {
                        switch (this[i].trajectory)
                        {
                            case BezierTrajectory:
                                count += 3;
                                break;
                            case StraightTrajectory:
                                count += 1;
                                break;
                        }
                    }

                    points = new Vector3[count];

                    count = 0;
                    for (var i = 0; i < Count; i += 1)
                    {
                        switch (this[i].trajectory)
                        {
                            case BezierTrajectory bezier:
                                points[count++] = bezier.Trajectory.a;
                                points[count++] = bezier.Trajectory.b;
                                points[count++] = bezier.Trajectory.c;
                                break;
                            case StraightTrajectory straight:
                                points[count++] = straight.Trajectory.a;
                                break;
                        }
                    }
                }
                return points;
            }
        }

        public Contour() { }
        public Contour(int capacity) : base(capacity) { }
        public Contour(IEnumerable<ContourEdge> items) : base(items) { }

        public new void Add(ContourEdge edge)
        {
            base.Add(edge);
            limits = null;
            points = null;
        }
        public new void Insert(int index, ContourEdge edge)
        {
            base.Insert(index, edge);
            limits = null;
            points = null;
        }
        public new void AddRange(IEnumerable<ContourEdge> edges)
        {
            base.AddRange(edges);
            limits = null;
            points = null;
        }
        public new void RemoveAt(int index)
        {
            base.RemoveAt(index);
            limits = null;
            points = null;
        }

        public bool CanIntersect(in StraightTrajectory line, bool precise) => precise ? Intersection.CanIntersect(Points, line, out _) : Intersection.CanIntersect(Limits, line, out _);

        public ContourGroup Cut(in StraightTrajectory line, Intersection.Side cutSide)
        {
            var result = new ContourGroup();

            if (Count <= 1)
                return result;

            HashSet<Intersection> intersections = GetIntersections(line);

            if (intersections.Count <= 1)
            {
                var point = this.AverageOrDefault(i => i.trajectory.StartPosition, Vector3.zero);
                var pos = line.StartPosition;
                var dir = line.Direction;

                var side = Intersection.GetSide(dir, point - pos);
                if (side == cutSide)
                    result.Add(this);

                return result;
            }

            var linePoints = intersections.OrderBy(i => i, Intersection.FirstComparer).ToArray();
            var setPoints = intersections.OrderBy(i => i, Intersection.SecondComparer).ToArray();
            var pairs = new List<IntersectionPairEdge>();

            for (int i = 1; i < linePoints.Length; i += 2)
                pairs.Add(new IntersectionPairEdge(false, linePoints[i - 1], linePoints[i]));

            var edgeIndex = Mathf.FloorToInt(setPoints[0].secondT);
            var t = setPoints[0].secondT - edgeIndex;
            var edgeSide = Intersection.GetSide(line.Direction, this[edgeIndex].trajectory.Tangent(t));
            if (edgeSide == cutSide)
            {
                for (int i = 1; i < setPoints.Length; i += 2)
                    pairs.Add(new IntersectionPairEdge(true, setPoints[i - 1].GetReverse(), setPoints[i].GetReverse()));
            }
            else
            {
                for (int i = setPoints.Length - 1; i > 0; i -= 2)
                    pairs.Add(new IntersectionPairEdge(true, setPoints[i].GetReverse(), setPoints[(i + 1) % setPoints.Length].GetReverse()));
            }

            var count = pairs.Count;
            for (var i = 0; i < count && pairs.Count > 0; i += 1)
            {
                var area = new List<IntersectionPairEdge>();
                var start = pairs[0];
                var current = start;
                var index = 0;
                var iteration = 0;
                while (true)
                {
                    pairs.RemoveAt(index);
                    area.Add(current);

                    var searchFor = current.pair.to.GetReverse();
                    var nextIndex = pairs.FindIndex(i => i.pair.Contain(searchFor));
                    if (nextIndex == -1)
                        break;

                    var next = pairs[nextIndex].pair.from == searchFor ? pairs[nextIndex] : pairs[nextIndex].Reverse;
                    current = next;
                    index = nextIndex;
                    iteration += 1;
                }

                if (area.Count <= 1)
                    continue;

                var newSet = new Contour();
                foreach (var areaPart in area)
                {
                    if (areaPart.isContour)
                    {
                        var startIndex = Mathf.FloorToInt(areaPart.pair.from.firstT);
                        var endIndex = Mathf.FloorToInt(areaPart.pair.to.firstT);

                        if (!areaPart.Inverted)
                        {
                            if (endIndex < startIndex || (endIndex == startIndex && areaPart.pair.to.firstT < areaPart.pair.from.firstT))
                                endIndex += Count;
                        }
                        else
                        {
                            if (startIndex < endIndex || (startIndex == endIndex && areaPart.pair.from.firstT < areaPart.pair.to.firstT))
                                startIndex += Count;
                        }

                        var k = startIndex;
                        while (true)
                        {
                            var fromT = k == startIndex ?
                                areaPart.pair.from.firstT - (startIndex % Count) :
                                (!areaPart.Inverted ? 0f : 1f);

                            var toT = k == endIndex ?
                                areaPart.pair.to.firstT - (endIndex % Count) :
                                (!areaPart.Inverted ? 1f : 0f);

                            var trajectory = this[k % Count].trajectory.Cut(fromT, toT);
                            newSet.Add(new ContourEdge(trajectory, this[k % Count].isEnter));

                            if (k == endIndex)
                                break;
                            else if (!areaPart.Inverted)
                                k += 1;
                            else
                                k -= 1;
                        }
                    }
                    else
                    {
                        var startIndex = Mathf.FloorToInt(areaPart.pair.from.secondT);
                        var endIndex = Mathf.FloorToInt(areaPart.pair.to.secondT);
                        var startT = areaPart.pair.from.secondT - startIndex;
                        var endT = areaPart.pair.to.secondT - endIndex;
                        var startPos = this[startIndex].trajectory.Position(startT);
                        var endPos = this[endIndex].trajectory.Position(endT);
                        newSet.Add(new ContourEdge(new StraightTrajectory(startPos, endPos)));
                    }
                }
                result.Add(newSet);
            }

            return result;
        }

        public ContourGroup SetOffset(float lineOffset, float medianOffset)
        {
            var result = new ContourGroup();

            if (lineOffset <= 0f && medianOffset <= 0f)
                result.Add(this);
            else
            {
                var direction = Direction;

                var movedParts = Move(direction, lineOffset, medianOffset);

                var partsIntersections = new List<List<Intersection>>();
                for (int i = 0; i < movedParts.Count; i += 1)
                    partsIntersections.Add(new List<Intersection>());

                for (int i = 0; i < movedParts.Count - 1; i += 1)
                {
                    for (int j = i + 1; j < movedParts.Count; j += 1)
                    {
                        var inters = Intersection.Calculate(movedParts[i].trajectory, movedParts[j].trajectory);
                        foreach (var inter in inters)
                        {
                            partsIntersections[i].Add(new Intersection(inter.firstT, inter.secondT + j));
                            partsIntersections[j].Add(new Intersection(inter.secondT, inter.firstT + i));
                        }
                    }
                }
            }
            return result;
        }

        private Contour Move(TrajectoryHelper.Direction direction, float lineOffset, float medianOffset)
        {
            var result = new Contour(Count);

            foreach (var part in this)
            {
                var offset = (direction == TrajectoryHelper.Direction.ClockWise ? 1f : -1f) * (part.isEnter ? medianOffset : lineOffset);

                if (offset == 0f)
                    result.Add(part);
                else
                {
                    var movedTrajectory = part.trajectory.Shift(offset, offset);
                    var newPart = new ContourEdge(movedTrajectory, part.isEnter);
                    result.Add(newPart);
                }
            }

            return result;
        }
        //private List<List<Intersection>> GetIntersections(Contour contour)
        //{

        //}

        public HashSet<Intersection> GetIntersections(in StraightTrajectory line)
        {
            HashSet<Intersection> intersections = new HashSet<Intersection>();
            for (int i = 0; i < Count; i += 1)
            {
                foreach (var inter in Intersection.Calculate(line, this[i].trajectory))
                {
                    var index = inter.secondT + i;
                    if (Mathf.Abs(index - Count) < float.Epsilon)
                        index = 0f;
                    intersections.Add(new Intersection(inter.firstT, index));
                }
            }
            return intersections;
        }

        public override string ToString() => $"{Count} Edges";
    }
    public readonly struct ContourEdge
    {
        public readonly ITrajectory trajectory;
        public readonly bool isEnter;

        public ContourEdge(ITrajectory trajectory, bool isEnter = false)
        {
            this.trajectory = trajectory;
            this.isEnter = isEnter;
        }

        public override string ToString() => $"{trajectory} {isEnter}";
    }
    readonly struct IntersectionPairEdge
    {
        public readonly bool isContour;
        public readonly IntersectionPair pair;

        public bool Inverted => pair.Inverted;
        public IntersectionPairEdge Reverse => new IntersectionPairEdge(isContour, pair.Reverse);

        public IntersectionPairEdge(bool isContour, IntersectionPair pair)
        {
            this.isContour = isContour;
            this.pair = pair;
        }
        public IntersectionPairEdge(bool isContour, Intersection from, Intersection to) : this(isContour, new IntersectionPair(from, to)) { }

        public override string ToString() => $"{(isContour ? "Contour" : "Straight")} {pair}";
    }

    public static class ContourUtil
    {
        public static Rect GetLimits(this IEnumerable<Contour> contours)
        {
            int index = 0;
            Rect limits = default;
            foreach (var contour in contours)
            {
                if (index == 0)
                    limits = contour.Limits;
                else
                {
                    var thisLimits = contour.Limits;
                    limits.min = Vector2.Min(limits.min, thisLimits.min);
                    limits.max = Vector2.Max(limits.max, thisLimits.max);
                }

                index += 1;
            }
            return limits;
        }
        public static Vector3[] GetPoints(this IEnumerable<Contour> contours)
        {
            var count = 0;

            foreach (var contour in contours)
                count += contour.Points.Length;

            var points = new Vector3[count];
            count = 0;
            foreach (var contour in contours)
            {
                var thisPoints = contour.Points;
                for (var i = 0; i < thisPoints.Length; i += 1)
                    points[i + count] = thisPoints[i];
                count += thisPoints.Length;
            }

            return points;
        }

    }
}
