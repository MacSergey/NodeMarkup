using ModsCommon;
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

        public ContourGroup Cut(ITrajectory line, Intersection.Side cutSide)
        {
            if (Count <= 1)
                return new ContourGroup();

            HashSet<Intersection> intersections = GetIntersections(line);

            if (intersections.Count <= 1)
            {
                var point = this.AverageOrDefault(i => i.trajectory.StartPosition, Vector3.zero);
                var pos = line.StartPosition;
                var dir = line.Direction;

                var group = new ContourGroup();
                var side = Intersection.GetSide(dir, point - pos);
                if (side == cutSide)
                    group.Add(this);

                return group;
            }
            else
            {
                var pairs = GetCutPairs(line, cutSide, intersections);
                return ConnectEdges(this, pairs, false);
            }
        }

        public ContourGroup SetOffset(float lineOffset, float medianOffset)
        {
            if (lineOffset <= 0f && medianOffset <= 0f)
            {
                return new ContourGroup() { this };
            }
            else
            {
                var movedEdges = Move(lineOffset, medianOffset);
#if DEBUG
                SingletonMod<Mod>.Logger.Debug("Original");
                foreach (var edge in this)
                    SingletonMod<Mod>.Logger.Debug("\n" + edge.trajectory.Table);

                SingletonMod<Mod>.Logger.Debug("Moved");
                foreach (var movedEdge in movedEdges)
                    SingletonMod<Mod>.Logger.Debug("\n" + movedEdge.edge.trajectory.Table);
#endif
                var allInters = GetAllIntersections(movedEdges);
                var pairs = GetIntersectionPairs(movedEdges, allInters);
                var movedContour = new Contour(movedEdges.Select(e => e.edge));
                return ConnectEdges(movedContour, pairs, true);
            }
        }

        private List<IntersectionPairEdge> GetCutPairs(ITrajectory line, Intersection.Side cutSide, HashSet<Intersection> intersections)
        {
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

            return pairs;
        }
        private static ContourGroup ConnectEdges(Contour contour, List<IntersectionPairEdge> pairs, bool oneDir)
        {
            var group = new ContourGroup();

            var count = pairs.Count;
            for (var i = 0; i < count && pairs.Count > 0; i += 1)
            {
                var area = new List<IntersectionPairEdge>();
                var start = pairs[0];
                var current = start;
                var index = 0;
                while (true)
                {
                    pairs.RemoveAt(index);
                    area.Add(current);

                    var searchFor = current.pair.to.GetReverse();
                    var nextIndex = pairs.FindIndex(i => oneDir ? i.pair.from == searchFor : i.pair.Contain(searchFor));
                    if (nextIndex == -1)
                        break;

                    var next = pairs[nextIndex].pair.from == searchFor ? pairs[nextIndex] : pairs[nextIndex].Reverse;

                    if (!oneDir || next.pair.from.firstT <= next.pair.to.firstT)
                    {
                        current = next;
                        index = nextIndex;
                    }
                    else
                    {
                        pairs.RemoveAt(nextIndex);
                        area.Clear();
                        break;
                    }
                }

                if (area.Count <= 1)
                    continue;

                if (area[area.Count - 1].pair.to != area[0].pair.from.GetReverse())
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
                                endIndex += contour.Count;
                        }
                        else
                        {
                            if (startIndex < endIndex || (startIndex == endIndex && areaPart.pair.from.firstT < areaPart.pair.to.firstT))
                                startIndex += contour.Count;
                        }

                        var k = startIndex;
                        while (true)
                        {
                            var fromT = k == startIndex ?
                                areaPart.pair.from.firstT - (startIndex % contour.Count) :
                                (!areaPart.Inverted ? 0f : 1f);

                            var toT = k == endIndex ?
                                areaPart.pair.to.firstT - (endIndex % contour.Count) :
                                (!areaPart.Inverted ? 1f : 0f);

                            var trajectory = contour[k % contour.Count].trajectory.Cut(fromT, toT);
                            if (trajectory is CombinedTrajectory combined)
                            {
                                foreach (var trajectoryPart in combined)
                                {
                                    if (trajectoryPart.Magnitude >= 0.05f)
                                        newSet.Add(new ContourEdge(trajectoryPart, contour[k % contour.Count].isEnter));
                                }
                            }
                            else
                                newSet.Add(new ContourEdge(trajectory, contour[k % contour.Count].isEnter));

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
                        var startPos = contour[startIndex].trajectory.Position(startT);
                        var endPos = contour[endIndex].trajectory.Position(endT);
                        newSet.Add(new ContourEdge(new StraightTrajectory(startPos, endPos)));
                    }
                }
                group.Add(newSet);
            }

            return group;
        }

        private List<MovedEdge> Move(float lineOffset, float medianOffset)
        {
            var result = new List<MovedEdge>(Count);
            var direction = Direction;

            for (int i = 0; i < Count; i += 1)
            {
                var offset = (direction == TrajectoryHelper.Direction.ClockWise ? 1f : -1f) * (this[i].isEnter ? medianOffset : lineOffset);

                var moved = offset != 0f;
                var newEdge = moved ? this[i].trajectory.Shift(offset, offset) : this[i].trajectory;
                if (newEdge.TrajectoryType == TrajectoryType.Line)
                {
                    var length = newEdge.Length;
                    var ratio = 100f / length;
                    newEdge = newEdge.Cut(-ratio, 1f + ratio);

                    var movedEdge = new MovedEdge(i, new ContourEdge(newEdge, this[i].isEnter), 100f / newEdge.Length, (100f + length) / newEdge.Length, moved);
                    result.Add(movedEdge);
                }
                else
                {
                    var beforeEdge = new StraightTrajectory(newEdge.StartPosition - newEdge.StartDirection * 100f, newEdge.StartPosition);
                    var afterEdge = new StraightTrajectory(newEdge.EndPosition, newEdge.EndPosition - newEdge.EndDirection * 100f);
                    var combined = new CombinedTrajectory(beforeEdge, newEdge, afterEdge);

                    var movedEdge = new MovedEdge(i, new ContourEdge(combined, this[i].isEnter), combined.Parts[1], combined.Parts[2], moved);
                    result.Add(movedEdge);
                }
            }

            return result;
        }

        private static List<MovedEdgeIntersections> GetAllIntersections(List<MovedEdge> contour)
        {
            var allInters = new List<MovedEdgeIntersections>();
            for (int i = 0; i < contour.Count; i += 1)
                allInters.Add(new MovedEdgeIntersections(contour[i]));

            for (int i = 0; i < contour.Count - 1; i += 1)
            {
                for (int j = i + 1; j < contour.Count; j += 1)
                {
                    if ((j - 1 == i || j + 1 - contour.Count == i) && !contour[i].moved && !contour[j].moved)
                    {
                        allInters[i].inters.Add(new Intersection(contour[i].maxT + i, contour[j].minT + j));
                        allInters[j].inters.Add(new Intersection(contour[j].minT + j, contour[i].maxT + i));
                        continue;
                    }

                    var trajectoryI = contour[i].edge.trajectory;
                    var trajectoryJ = contour[j].edge.trajectory;
                    CombinedTrajectory? combinedI = null;
                    CombinedTrajectory? combinedJ = null;

                    if (trajectoryI.TrajectoryType == TrajectoryType.Combined)
                        combinedI = (CombinedTrajectory)trajectoryI;

                    if (trajectoryJ.TrajectoryType == TrajectoryType.Combined)
                        combinedJ = (CombinedTrajectory)trajectoryJ;

                    var inters = Intersection.Calculate(combinedI != null ? combinedI.Value[1] : trajectoryI, combinedJ != null ? combinedJ.Value[1] : trajectoryJ);

                    var mainI = false;
                    var mainJ = false;
                    foreach (var inter in inters)
                    {
                        AddIntersection(allInters, i, j, combinedI, combinedJ, 1, 1, inter);

                        if (contour[i].minT <= inter.firstT && inter.firstT <= contour[i].maxT)
                            mainI = true;

                        if (contour[j].minT <= inter.secondT && inter.secondT <= contour[j].maxT)
                            mainJ = true;
                    }

                    if(inters.Count == 0 || (!mainI && combinedI != null) || (!mainJ && combinedJ != null))
                    {
                        if(combinedI != null && combinedJ != null)
                        {
                            inters = Intersection.Calculate(combinedI.Value[0], combinedJ.Value[0]);
                            foreach (var inter in inters)
                                AddIntersection(allInters, i, j, combinedI, combinedJ, 0, 0, inter);

                            inters = Intersection.Calculate(combinedI.Value[0], combinedJ.Value[2]);
                            foreach (var inter in inters)
                                AddIntersection(allInters, i, j, combinedI, combinedJ, 0, 2, inter);

                            inters = Intersection.Calculate(combinedI.Value[2], combinedJ.Value[0]);
                            foreach (var inter in inters)
                                AddIntersection(allInters, i, j, combinedI, combinedJ, 2, 0, inter);

                            inters = Intersection.Calculate(combinedI.Value[2], combinedJ.Value[2]);
                            foreach (var inter in inters)
                                AddIntersection(allInters, i, j, combinedI, combinedJ, 2, 2, inter);
                        }
                        else if (combinedI != null)
                        {
                            inters = Intersection.Calculate(combinedI.Value[0], trajectoryJ);
                            foreach (var inter in inters)
                                AddIntersection(allInters, i, j, combinedI, null, 0, 0, inter);

                            inters = Intersection.Calculate(combinedI.Value[2], trajectoryJ);
                            foreach (var inter in inters)
                                AddIntersection(allInters, i, j, combinedI, null, 2, 0, inter);
                        }
                        else if(combinedJ != null)
                        {
                            inters = Intersection.Calculate(trajectoryI, combinedJ.Value[0]);
                            foreach (var inter in inters)
                                AddIntersection(allInters, i, j, null, combinedJ, 0, 0, inter);

                            inters = Intersection.Calculate(trajectoryI, combinedJ.Value[2]);
                            foreach (var inter in inters)
                                AddIntersection(allInters, i, j, null, combinedJ, 2, 2, inter);
                        }
                    }
                }
            }

            for (var i = 0; i < allInters.Count; i += 1)
                allInters[i].inters.Sort(Intersection.FirstComparer);

            return allInters;

            static void AddIntersection(List<MovedEdgeIntersections> allInters, int i, int j, CombinedTrajectory? combinedI, CombinedTrajectory? combinedJ, int partI, int partJ, Intersection inter)
            {
                var firstT = combinedI != null ? combinedI.Value.FromPartT(partI, inter.firstT) : inter.firstT;
                var secondT = combinedJ != null ? combinedJ.Value.FromPartT(partJ, inter.secondT) : inter.secondT;

                allInters[i].inters.Add(new Intersection(firstT + i, secondT + j));
                allInters[j].inters.Add(new Intersection(secondT + j, firstT + i));
            }
        }
        private static List<IntersectionPairEdge> GetIntersectionPairs(List<MovedEdge> contour, List<MovedEdgeIntersections> allInters)
        {
            RemoveSame(contour, allInters);
            RemoveSingle(contour, allInters);
            RemoveEmpty(contour, allInters);

            var count = allInters.Count;

            for (var i = 0; i < count; i += 1)
            {
                if (allInters[i].Count == 0)
                    continue;

                if (allInters[i].Count == 1)
                {
                    RemoveAt(allInters, i, 0);
                    continue;
                }

                var startInterI = 0;
                var endInterI = allInters[i].Count - 1;

                if (allInters[i].Count > 2)
                {
                    var prevI = (i + count - 1) % count;
                    var nextI = (i + 1) % count;

                    for (var index = 1; index < allInters[i].Count; index += 1)
                    {
                        var firstI = allInters[i].GetSecondIndex(index - 1);
                        var secondI = allInters[i].GetSecondIndex(index);
                        if (firstI == prevI && secondI == nextI)
                        {
                            startInterI = index - 1;
                            endInterI = index;

                            if (index - 2 >= 0 && allInters[i].GetSecondIndex(index - 2) == prevI && allInters[i].GetFirstT(index - 2) >= allInters[i].movedEdge.minT)
                                startInterI = index - 2;

                            if (index + 1 < allInters[i].Count && allInters[i].GetSecondIndex(index + 1) == nextI && allInters[i].GetFirstT(index + 1) <= allInters[i].movedEdge.maxT)
                                endInterI = index + 1;

                            break;
                        }
                    }

                    if (allInters[i].Count > 3 && startInterI == 0 && endInterI == allInters[i].Count - 1)
                    {
                        for (var j = 0; j < allInters[i].Count; j += 1)
                        {
                            if (allInters[i].inters[j].firstT - allInters[i].movedEdge.index >= allInters[i].movedEdge.minT)
                            {
                                startInterI = j;
                                break;
                            }
                        }
                        for (var j = allInters[i].Count - 1; j >= 0; j -= 1)
                        {
                            if (allInters[i].inters[j].firstT - allInters[i].movedEdge.index <= allInters[i].movedEdge.maxT)
                            {
                                endInterI = j;
                                break;
                            }
                        }

                        if (startInterI == 0 && startInterI == endInterI)
                            endInterI = allInters[i].Count - 1;
                        else if (endInterI == allInters[i].Count - 1 && endInterI == startInterI)
                            startInterI = 0;
                        else if ((endInterI - startInterI + 1) % 2 == 1)
                        {
                            var beforeI = startInterI > 0 ? allInters[i].GetSecondIndex(startInterI - 1) : -1;
                            var afterI = endInterI < allInters[i].Count - 1 ? allInters[i].GetSecondIndex(endInterI + 1) : -1;

                            if (beforeI != -1 && afterI != -1)
                            {
                                var startI = allInters[i].GetSecondIndex(startInterI);
                                var endI = allInters[i].GetSecondIndex(endInterI);
                                //var beforeDeltaI = startI - (beforeI > startI ? beforeI - count : beforeI);
                                //var afterDeltaI = (afterI < endI ? afterI + count : afterI) - endI;
                                var beforeDeltaI = (startI - beforeI + count) % count;
                                var afterDeltaI = (afterI - endI + count) % count;
                                if (beforeDeltaI < afterDeltaI)
                                    startInterI -= 1;
                                else if (afterDeltaI < beforeDeltaI)
                                    endInterI += 1;
                                else
                                {
                                    var a = "ups";
                                }
                            }
                            else if (beforeI != -1)
                                startInterI -= 1;
                            else if (afterI != -1)
                                endInterI += 1;
                        }
                    }
                }

                for (int interI = 0; interI < allInters[i].Count; interI += 1)
                {
                    if (interI == startInterI)
                        interI = endInterI;
                    else if (interI < startInterI || interI > endInterI)
                    {
                        if (RemoveAt(allInters, i, interI))
                        {
                            if (interI < startInterI)
                                startInterI -= 1;
                            if (interI < endInterI)
                                endInterI -= 1;
                            interI -= 1;
                        }
                    }
                }
            }

            RemoveSingle(contour, allInters);

            var pairs = new List<IntersectionPairEdge>();
            for (var i = 0; i < allInters.Count; i += 1)
            {
                for (int j = 1; j < allInters[i].Count; j += 1)
                {
                    pairs.Add(new IntersectionPairEdge(true, allInters[i].inters[j - 1], allInters[i].inters[j]));
                }
            }
            return pairs;


            static bool RemoveAt(List<MovedEdgeIntersections> allInters, int i, int interI)
            {
                var inverted = allInters[i].inters[interI].GetReverse();
                allInters[i].inters.RemoveAt(interI);
                var j = Mathf.FloorToInt(inverted.firstT);

                var foundIndex = allInters[j].inters.FindIndex(inter => inter == inverted);
                if (foundIndex >= 0)
                {
                    allInters[j].inters.RemoveAt(foundIndex);

                    return true;
                }

                return false;
            }
            static void RemoveSame(List<MovedEdge> contour, List<MovedEdgeIntersections> allInters)
            {
                for (var i = 0; i < allInters.Count; i += 1)
                {
                    for (var j = 1; j < allInters[i].Count; j += 1)
                    {
                        var pos1 = contour[i].edge.trajectory.Position(allInters[i].GetFirstT(j - 1));
                        var pos2 = contour[i].edge.trajectory.Position(allInters[i].GetFirstT(j));
                        var dist = (pos2 - pos1).sqrMagnitude;
                        if (dist < 0.0001f)
                        {
                            var prev = allInters[i].GetSecondIndex(j - 1);
                            var next = allInters[i].GetSecondIndex(j);
                            if ((i + allInters.Count - 1) % allInters.Count == prev && (i + 1) % allInters.Count == next)
                            {
                                RemoveAt(allInters, i, j);
                                RemoveAt(allInters, i, j - 1);
                            }
                        }
                    }
                }
            }
            static void RemoveSingle(List<MovedEdge> contour, List<MovedEdgeIntersections> allInters)
            {
                var i = 0;
                for (var iter = 0; iter < allInters.Count; iter += 1)
                {
                    if (allInters[i].Count == 1)
                    {
                        RemoveAt(allInters, i, 0);
                        iter = 0;
                    }

                    i = (i + 1) % allInters.Count;
                }
            }
            static void RemoveEmpty(List<MovedEdge> contour, List<MovedEdgeIntersections> allInters)
            {
                for (var i = 0; i < allInters.Count; i += 1)
                {
                    if (allInters[i].Count == 0)
                    {
                        for (var k = 0; k < allInters.Count; k += 1)
                        {
                            if (k > i)
                            {
                                contour[k] = new MovedEdge(contour[k].index - 1, contour[k].edge, contour[k].minT, contour[k].maxT, contour[k].moved);
                                allInters[k] = new MovedEdgeIntersections(contour[k], allInters[k].inters);
                            }

                            for (var l = 0; l < allInters[k].Count; l += 1)
                            {
                                var inter = allInters[k].inters[l];
                                var index = Mathf.FloorToInt(inter.secondT);
                                if (k > i && index > i)
                                    allInters[k].inters[l] = new Intersection(inter.firstT - 1f, inter.secondT - 1f);
                                else if (k > i)
                                    allInters[k].inters[l] = new Intersection(inter.firstT - 1f, inter.secondT);
                                else if (index > i)
                                    allInters[k].inters[l] = new Intersection(inter.firstT, inter.secondT - 1f);
                            }
                        }

                        allInters.RemoveAt(i);
                        contour.RemoveAt(i);
                        i -= 1;
                    }
                }
            }
        }

        public HashSet<Intersection> GetIntersections(ITrajectory line)
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

    readonly struct MovedEdge
    {
        public readonly int index;
        public readonly ContourEdge edge;
        public readonly float minT;
        public readonly float maxT;
        public readonly bool moved;

        public MovedEdge(int index, ContourEdge edge, float minT, float maxT, bool moved)
        {
            this.index = index;
            this.edge = edge;
            this.minT = minT;
            this.maxT = maxT;
            this.moved = moved;
        }

        public override string ToString() => $"{index}: {minT:0.###} ÷ {maxT:0.###}";
    }
    readonly struct MovedEdgeIntersections
    {
        public readonly MovedEdge movedEdge;
        public readonly List<Intersection> inters;

        public int Count => inters.Count;

        public MovedEdgeIntersections(MovedEdge movedEdge, List<Intersection> intersections = null)
        {
            this.movedEdge = movedEdge;
            this.inters = intersections ?? new List<Intersection>();
        }

        public int GetFirstIndex(int index) => Mathf.FloorToInt(inters[index].firstT);
        public int GetSecondIndex(int index) => Mathf.FloorToInt(inters[index].secondT);

        public float GetFirstT(int index) => inters[index].firstT - GetFirstIndex(index);
        public float GetSecondT(int index) => inters[index].secondT - GetSecondIndex(index);

        public override string ToString() => $"{movedEdge} - {inters.Count} inters";
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
        public static void Process(this Queue<Contour> contours, ITrajectory line, Intersection.Side side)
        {
            var count = contours.Count;
            for (var i = 0; i < count; i += 1)
            {
                var newContours = contours.Dequeue().Cut(line, side);
                for (var j = 0; j < newContours.Count; j += 1)
                    contours.Enqueue(newContours[j]);
            }
        }
    }
}
