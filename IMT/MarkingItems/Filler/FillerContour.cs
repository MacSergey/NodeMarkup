﻿using ColossalFramework.Math;
using IMT.Utilities;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public class FillerContour : IOverlay
    {
        private static Comparer VertexComparer { get; } = new Comparer();
        public static List<IFillerVertex> GetBeginCandidates(Marking marking)
        {
            var points = new List<IFillerVertex>();

            foreach (var intersect in marking.Intersects)
                points.Add(new IntersectFillerVertex(intersect.pair));

            foreach (var enter in marking.Enters)
            {
                foreach (var point in enter.EnterPoints)
                {
                    points.Add(new EnterFillerVertex(point, Alignment.Centre));
                    if (point.IsSplit)
                    {
                        points.Add(new EnterFillerVertex(point, Alignment.Left));
                        points.Add(new EnterFillerVertex(point, Alignment.Right));
                    }
                }
            }

            return points;
        }

        public Marking Marking { get; }
        public bool IsComplite { get; private set; }

        private List<IFillerVertex> SupportPoints { get; } = new List<IFillerVertex>();
        public IFillerVertex First => SupportPoints.FirstOrDefault();
        public IFillerVertex Second => VertexCount >= 2 ? SupportPoints[1] : null;
        public IFillerVertex Last => SupportPoints.LastOrDefault();
        public IFillerVertex Prev => VertexCount >= 2 ? SupportPoints[SupportPoints.Count - 2] : null;
        public IFillerVertex PrePrev => VertexCount >= 3 ? SupportPoints[SupportPoints.Count - 3] : null;

        public IEnumerable<IFillerVertex> RawVertices => SupportPoints;
        public IFillerVertex[] ProcessedVertex { get; private set; } = new IFillerVertex[0];
        public int VertexCount => SupportPoints.Count;
        public bool IsEmpty => VertexCount == 0;
        public bool PossibleComplite => VertexCount >= 3;

        private FillerLinePart[] RawPartsArray { get; set; } = new FillerLinePart[0];
        private FillerLinePart[] ProcessedPartsArray { get; set; } = new FillerLinePart[0];

        public IEnumerable<FillerLinePart> RawEdges => RawPartsArray;
        public IEnumerable<FillerLinePart> ProcessedEdges => ProcessedPartsArray;

        public int RawCount => RawPartsArray.Length;
        public int ProcessedCount => ProcessedPartsArray.Length;

        public List<ITrajectory> TrajectoriesRaw => GetTrajectories(RawPartsArray);
        public List<ITrajectory> TrajectoriesProcessed => GetTrajectories(ProcessedPartsArray);
        public TrajectoryHelper.Direction Direction => TrajectoriesRaw.GetDirection();

        public bool IsMedian => Edges.Any(p => p.isEnter);
        public Contour Edges
        {
            get
            {
                var edges = new Contour();

                foreach (var part in RawPartsArray)
                {
                    if (part.Line is MarkingEnterLine enterLine && enterLine.IsDot)
                        continue;

                    if (part.GetTrajectory(out ITrajectory trajectory))
                        edges.Add(new ContourEdge(trajectory, part.Line is MarkingEnterLine));
                }

                return edges;
            }
        }

        public FillerContour(Marking marking, IEnumerable<IFillerVertex> vertices = null)
        {
            Marking = marking;

            if (vertices != null)
            {
                foreach (var vertex in vertices)
                    AddImpl(vertex);

                IsComplite = true;

                FixVertices();
                Update();
            }
        }
        private void FixVertices()
        {
            for (var i = 0; i < SupportPoints.Count; i += 1)
            {
                switch (SupportPoints[i])
                {
                    case LineEndFillerVertex endVertex:
                        FixLineEndVertex(endVertex, ref i);
                        break;
                    case EnterFillerVertex enterVertex:
                        FixEnterVertex(enterVertex, ref i);
                        break;
                    case IntersectFillerVertex intersectVertex:
                        FixIntersect(intersectVertex, ref i);
                        break;
                }
            }
        }
        private void FixLineEndVertex(LineEndFillerVertex end, ref int i)
        {
            if (SupportPoints[i.NextIndex(SupportPoints.Count)] is LineEndFillerVertex nextEnd)
            {
                if (end.Point.Enter != nextEnd.Point.Enter && end.Line != nextEnd.Line && end.GetCommonLine(nextEnd) is MarkingRegularLine line)
                {
                    SupportPoints.Insert(i + 1, new LineEndFillerVertex(end.Point, line));
                    SupportPoints.Insert(i + 2, new LineEndFillerVertex(nextEnd.Point, line));
                    i += 2;
                }
            }
        }
        private void FixEnterVertex(EnterFillerVertex enter, ref int i)
        {
            var prevI = i.PrevIndex(SupportPoints.Count);
            var nextI = i.NextIndex(SupportPoints.Count);

            if ((SupportPoints[prevI] is LineEndFillerVertex prevEnd && enter.Some(prevEnd)) || (SupportPoints[nextI] is LineEndFillerVertex nextEnd && enter.Some(nextEnd)))
            {
                SupportPoints.RemoveAt(i);
                i -= 1;
            }
            else
            {
                if (SupportPoints[prevI] is EnterFillerVertexBase prevEnter && enter.GetCommonLine(prevEnter) is MarkingRegularLine prevLine)
                {
                    SupportPoints.RemoveAt(i);
                    SupportPoints.Insert(prevI < i ? i : prevI, new LineEndFillerVertex(prevLine.PointPair.GetOther(enter.Point), prevLine));
                    SupportPoints.Insert(prevI < i ? i + 1 : i, new LineEndFillerVertex(enter.Point, prevLine));
                    i += prevI < i ? 1 : 0;
                }
                else if (SupportPoints[nextI] is EnterFillerVertexBase nextEnter && enter.GetCommonLine(nextEnter) is MarkingRegularLine nextLine)
                {
                    SupportPoints.RemoveAt(i);
                    SupportPoints.Insert(i, new LineEndFillerVertex(enter.Point, nextLine));
                    SupportPoints.Insert(i + 1, new LineEndFillerVertex(nextLine.PointPair.GetOther(enter.Point), nextLine));
                    i += 1;
                }
            }
        }
        private void FixIntersect(IntersectFillerVertex intersect, ref int i)
        {
            var prevI = i.PrevIndex(SupportPoints.Count);
            var nextI = i.NextIndex(SupportPoints.Count);

            if (SupportPoints[nextI] is LineEndFillerVertex nextEnd && !intersect.Contains(nextEnd.Line))
            {
                if (SupportPoints[prevI] is IFillerLineVertex prev && (prev.Contains(intersect.First) ? intersect.Second : intersect.First) is MarkingRegularLine line)
                {
                    SupportPoints.Insert(i + 1, new LineEndFillerVertex(nextEnd.Point, line));
                    i += 1;
                }
            }
        }

        public bool Add(IFillerVertex newVertex)
        {
            if (!IsComplite)
            {
                AddImpl(newVertex);
                Update();
            }

            return IsComplite;
        }
        private void AddImpl(IFillerVertex newVertex)
        {
            if (newVertex.Equals(Last))
                return;

            switch (newVertex)
            {
                case IntersectFillerVertex newIntersectVertex:
                    AddIntersectVertex(newIntersectVertex, ref newVertex);
                    break;

                case EnterFillerVertex newEnterVertex:
                    AddEnterVertex(newEnterVertex, ref newVertex);
                    break;
            }

            if (PossibleComplite && newVertex.Some(First))
                IsComplite = true;

            if (!IsComplite)
                SupportPoints.Add(newVertex);
            else if (newVertex is LineEndFillerVertex newEnd && First is EnterFillerVertex firstEnter)
            {
                SupportPoints.Remove(firstEnter);
                SupportPoints.Insert(0, newEnd);
            }
            else if (newVertex is EnterFillerVertexBase newEnter)
            {
                var removedFirst = false;
                {
                    if (First is EnterFillerVertexBase firstBaseEnter && Second is EnterFillerVertexBase secondEnter && firstBaseEnter.Enter == secondEnter.Enter && firstBaseEnter.Enter == newEnter.Enter)
                    {
                        SupportPoints.Remove(firstBaseEnter);
                        removedFirst = true;
                    }
                }
                {
                    if (First is EnterFillerVertexBase firstBaseEnter && Last is EnterFillerVertexBase lastEnter && (firstBaseEnter.Enter != newEnter.Enter || newEnter.Enter != lastEnter.Enter))
                    {
                        if (removedFirst)
                            SupportPoints.Insert(0, newVertex);
                        else
                            SupportPoints.Add(newVertex);
                    }
                }
            }
        }
        private void AddIntersectVertex(IntersectFillerVertex newIntersectVertex, ref IFillerVertex newVertex)
        {
            switch (Last)
            {
                case EnterFillerVertex lastEnterVertex:
                    SupportPoints.Remove(lastEnterVertex);
                    SupportPoints.Add(FixVertex(lastEnterVertex, newIntersectVertex));
                    break;

                case LineEndFillerVertex lastLineEndVertex:
                    if (!newIntersectVertex.Contains(lastLineEndVertex.Line))
                        SupportPoints.Add(FixVertex(lastLineEndVertex, newIntersectVertex));
                    break;
            }
        }
        private void AddEnterVertex(EnterFillerVertex newEnterVertex, ref IFillerVertex newVertex)
        {
            switch (Last)
            {
                case EnterFillerVertex lastEnterVertex when lastEnterVertex.Enter != newEnterVertex.Enter:
                    {
                        if (lastEnterVertex.GetCommonLine(newEnterVertex) is MarkingRegularLine line)
                        {
                            SupportPoints.Remove(lastEnterVertex);
                            SupportPoints.Add(FixVertexByLine(lastEnterVertex, line));

                            newVertex = FixVertexByLine(newEnterVertex, line);
                        }
                    }
                    break;

                case LineEndFillerVertex lastLineEndVertex when newEnterVertex.Enter != lastLineEndVertex.Enter:
                    {
                        if (lastLineEndVertex.GetCommonLine(newEnterVertex) is MarkingRegularLine line)
                        {
                            if (Prev is not LineEndFillerVertex prevLineEndVertex || prevLineEndVertex.Point != lastLineEndVertex.Point)
                                SupportPoints.Add(FixVertexByLine(lastLineEndVertex, line));

                            newVertex = FixVertexByLine(newEnterVertex, line);
                        }
                    }
                    break;

                case IntersectFillerVertex lastIntersectVertex:
                    newVertex = FixVertex(newEnterVertex, lastIntersectVertex);
                    break;
            }
        }
        static LineEndFillerVertex FixVertex(EnterFillerVertexBase enterVertex, IntersectFillerVertex intersectVertex)
        {
            if (intersectVertex.LinePair.first.ContainsPoint(enterVertex.Point) && intersectVertex.LinePair.first.GetAlignment(enterVertex.Point) == enterVertex.Alignment)
                return FixVertexByLine(enterVertex, intersectVertex.LinePair.first as MarkingRegularLine);

            else if (intersectVertex.LinePair.second.ContainsPoint(enterVertex.Point) && intersectVertex.LinePair.second.GetAlignment(enterVertex.Point) == enterVertex.Alignment)
                return FixVertexByLine(enterVertex, intersectVertex.LinePair.second as MarkingRegularLine);

            else
                return FixVertexByLine(enterVertex, intersectVertex.LinePair.GetLine(enterVertex.Point) as MarkingRegularLine);
        }
        static LineEndFillerVertex FixVertexByLine(EnterFillerVertexBase enterVertex, MarkingRegularLine line) => new LineEndFillerVertex(enterVertex.Point, line);

        public void Remove()
        {
            if (IsComplite)
                return;

            var last = Last;

            if (Prev is LineEndFillerVertex endLineVertex)
            {
                if (PrePrev is LineEndFillerVertex preEndLineVertex && preEndLineVertex.Point == endLineVertex.Point && !endLineVertex.Point.IsSplit)
                    SupportPoints.Remove(endLineVertex);
                else if (last is IntersectFillerVertex lastIntersectVertex)
                    FixPoint(endLineVertex, lastIntersectVertex.LinePair.GetLine(endLineVertex.Point).GetAlignment(endLineVertex.Point));
                else if (last is LineEndFillerVertex lastEndLineVertex)
                    FixPoint(endLineVertex, lastEndLineVertex.Line.GetAlignment(endLineVertex.Point));
            }

            SupportPoints.Remove(last);
            Update();

            void FixPoint(EnterFillerVertexBase enterVertex, Alignment alignment)
            {
                SupportPoints.Remove(enterVertex);
                SupportPoints.Add(new EnterFillerVertex(enterVertex.Point, alignment));

            }
        }

        public FillerLinePart GetFillerLine(IFillerVertex first, IFillerVertex second)
        {
            var line = first.GetCommonLine(second);
            var linePart = new FillerLinePart(line, first, second);
            return linePart;
        }
        public List<IFillerVertex> GetNextСandidates() => Last is IFillerVertex last ? last.GetNextCandidates(this, Prev) : GetBeginCandidates(Marking);

        public void GetMinMaxT(IFillerVertex fillerVertex, MarkingLine line, out float resultT, out float resultMinT, out float resultMaxT)
        {
            fillerVertex.GetT(line, out float vertexT);
            var minT = -1f;
            var maxT = 2f;

            foreach (var part in RawPartsArray)
            {
                part.GetFromT(out float fromT);
                part.GetToT(out float toT);

                if (part.Line == line)
                {
                    Set(fromT, false);
                    Set(toT, false);
                }
                else if (Marking.GetIntersect(new MarkingLinePair(line, part.Line)) is MarkingLinesIntersect intersect && intersect.IsIntersect)
                {
                    var linePartT = intersect[part.Line];

                    if ((fromT <= linePartT && linePartT <= toT) || (toT <= linePartT && linePartT <= fromT))
                        Set(intersect[line], true);
                }
                else if (part.Line.IsEnterLine)
                {
                    if (line.Start.Enter == part.Line.Start.Enter && CheckEnter(line.Start.Index, part.Line.Start.Index, part.Line.End.Index))
                        Set(0, true);
                    if (line.End.Enter == part.Line.Start.Enter && CheckEnter(line.End.Index, part.Line.Start.Index, part.Line.End.Index))
                        Set(1, true);
                }
            }

            void Set(float t, bool isStrict)
            {
                if (minT < t && (isStrict ? t < vertexT : t <= vertexT))
                    minT = t;

                if (maxT > t && (isStrict ? t > vertexT : t >= vertexT))
                    maxT = t;
            }

            static bool CheckEnter(byte num, byte start, byte end) => Math.Min(start, end) <= num && num <= Math.Max(end, start);

            resultT = vertexT;
            resultMinT = minT;
            resultMaxT = maxT;
        }
        public void GetMinMaxNum(MarkingPoint point, out byte minNum, out byte maxNum)
        {
            if (VertexCount > 2 && First is EnterFillerVertexBase firstVertex && firstVertex.Point == point)
            {
                minNum = point.Index;
                maxNum = point.Index;
            }
            else
            {
                minNum = 0;
                maxNum = (byte)(point.Enter.PointCount + 1);

                foreach (var vertex in SupportPoints)
                {
                    if (vertex is EnterFillerVertexBase enterVertex && enterVertex.Point.Enter == point.Enter)
                    {
                        var n = enterVertex.Point.Index;

                        if (minNum < n && n < point.Index)
                            minNum = n;

                        if (maxNum > n && n > point.Index)
                            maxNum = n;
                    }
                }
            }
        }
        public bool IsAvailable(MarkingPoint point)
        {
            for (var i = 0; i < VertexCount - 1; i += 1)
            {
                if (SupportPoints[i] is EnterFillerVertexBase enterVertex1 &&
                    SupportPoints[i + 1] is EnterFillerVertexBase enterVertex2 &&
                    enterVertex1.Enter == point.Enter &&
                    enterVertex2.Enter == point.Enter &&
                    Math.Min(enterVertex1.Point.Index, enterVertex2.Point.Index) <= point.Index &&
                    point.Index <= Math.Max(enterVertex1.Point.Index, enterVertex2.Point.Index))
                    return false;
            }

            return true;
        }

        public List<IFillerVertex> GetLinePoints(IFillerVertex fillerVertex, MarkingLine line)
        {
            var points = new List<IFillerVertex>();
            GetMinMaxT(fillerVertex, line, out float t, out float minT, out float maxT);

            foreach (var intersectLine in line.IntersectLines)
            {
                var vertex = new IntersectFillerVertex(line, intersectLine);
                if (vertex.GetT(line, out float tt) && tt != t && minT < tt && tt < maxT)
                    points.Add(vertex);
            }

            switch (First)
            {
                case EnterFillerVertex firstE when line.ContainsPoint(firstE.Point) && ((line.IsStart(firstE.Point) && minT == 0) || (line.IsEnd(firstE.Point) && maxT == 1)):
                    points.Add(firstE);
                    break;
                case IntersectFillerVertex firstI when firstI.LinePair.ContainLine(line) && firstI.GetT(line, out float firstT) && (firstT == minT || firstT == maxT):
                    points.Add(firstI);
                    break;
            }

            if (line.Start.Type == MarkingPoint.PointType.Enter && t != 0 && minT < 0 && 0 < maxT)
                points.Add(new EnterFillerVertex(line.Start, line.GetAlignment(line.Start)));

            if (line.End.Type == MarkingPoint.PointType.Enter && t != 1 && minT < 1 && 1 < maxT)
                points.Add(new EnterFillerVertex(line.End, line.GetAlignment(line.End)));

            return points;
        }

        private List<ITrajectory> GetTrajectories(FillerLinePart[] parts)
        {
            var trajectories = new List<ITrajectory>(parts.Length);
            foreach (var part in parts)
            {
                if (part.GetTrajectory(out ITrajectory trajectory))
                    trajectories.Add(trajectory);
            }
            return trajectories;
        }
        public ITrajectory GetGuide(int a1, int b1, int a2, int b2)
        {
            var min1 = GetCorrectIndex(Math.Min(a1, b1));
            var max1 = GetCorrectIndex(Math.Max(a1, b1));
            var min2 = GetCorrectIndex(Math.Min(a2, b2));
            var max2 = GetCorrectIndex(Math.Max(a2, b2));

            if (max1 <= min2 || max2 <= min1 || (min2 <= min1 && max1 <= max2))
                return GetGuide(min1, max1);
            else
                return GetGuide(max1, min1);
        }
        private ITrajectory GetGuide(int a, int b)
        {
            var trajectories = TrajectoriesProcessed.ToArray();

            if (trajectories.Length == 0)
                return null;
            else if (Mathf.Abs(b - a) == 1)
                return trajectories[Math.Min(a, b)];
            else if (Mathf.Abs(b - a) == trajectories.Length - 1)
                return trajectories.Last();
            else
            {
                var first = trajectories[a];
                var second = trajectories[(b - 1 + trajectories.Length) % trajectories.Length];
                return new BezierTrajectory(first.StartPosition, first.StartDirection, second.EndPosition, second.EndDirection);
            }
        }
        public int GetCorrectIndex(int value) => value >= 0 ? value % ProcessedCount : value % ProcessedCount + ProcessedCount;

        public int IndexOfProcessed(IFillerVertex vertex) => IndexOf(ProcessedVertex, vertex.ProcessedVertex);
        private int IndexOf(IEnumerable<IFillerVertex> vertices, IFillerVertex vertex)
        {
            var i = 0;
            foreach (var v in vertices)
            {
                if (VertexComparer.Equals(vertex, v))
                    return i;
                i += 1;
            }
            return -1;
        }

        public void Update()
        {
            foreach (var supportPoint in SupportPoints)
                supportPoint.Update();

            ProcessedVertex = SupportPoints.Select(p => p.ProcessedVertex).Distinct(VertexComparer).ToArray();
            RawPartsArray = GetParts(true).ToArray();
            ProcessedPartsArray = GetParts(false).ToArray();
        }
        private IEnumerable<FillerLinePart> GetParts(bool isRaw)
        {
            var count = IsComplite ? VertexCount : VertexCount - 1;
            for (var i = 0; i < count; i += 1)
            {
                var vertex1 = SupportPoints[i];
                var vertex2 = SupportPoints[(i + 1) % VertexCount];
                if (vertex1 is not LineEndFillerVertex lineEnd1 || vertex2 is not LineEndFillerVertex lineEnd2 || lineEnd1.Point != lineEnd2.Point || isRaw)
                    yield return GetFillerLine(vertex1, vertex2);
            }
        }

        public void Render(OverlayData data)
        {
            if (IsComplite)
            {
                data.AlphaBlend = false;
                var triangles = Triangulator.TriangulateSimple(TrajectoriesRaw, out var points, minAngle: 5, maxLength: 10f);
                points.RenderArea(triangles, data);
            }
            else
            {
                foreach (var part in Edges)
                    part.trajectory.Render(data);
            }
        }

        private class Comparer : IEqualityComparer<IFillerVertex>
        {
            public bool Equals(IFillerVertex x, IFillerVertex y) => x.Equals(y);
            public int GetHashCode(IFillerVertex vertex) => vertex.GetHashCode();
        }


        public readonly struct EdgePart
        {
            public readonly ContourEdge part;
            public readonly TrajectoryIntersect start;
            public readonly TrajectoryIntersect end;
            public ContourEdge Processed => new ContourEdge(part.trajectory.Cut(start.t, end.t), part.isEnter);
            public EdgePart(ContourEdge part, TrajectoryIntersect start, TrajectoryIntersect end)
            {
                this.part = part;
                this.start = start;
                this.end = end;
            }

            public override string ToString() => $"{start} — {end}";
        }
    }

    public readonly struct FillerGuide
    {
        public readonly int a;
        public readonly int b;

        public FillerGuide(int a, int b)
        {
            this.a = Math.Max(a, 0);
            this.b = Math.Max(b, 0);
        }
        public static FillerGuide operator +(FillerGuide guide, int delta) => new FillerGuide(guide.a + delta, guide.b + delta);
        public static FillerGuide operator %(FillerGuide guide, int max) => new FillerGuide(guide.a % max, guide.b % max);

        public override string ToString() => $"{a + 1}-{b + 1}";
    }
}
