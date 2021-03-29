using ModsCommon.Utilities;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public class FillerContour : IOverlay
    {
        private static Comparer VertexComparer { get; } = new Comparer();
        public static IEnumerable<IFillerVertex> GetBeginCandidates(Markup markup)
        {
            foreach (var intersect in markup.Intersects)
                yield return new IntersectFillerVertex(intersect.Pair);

            foreach (var enter in markup.Enters)
            {
                foreach (var point in enter.Points)
                {
                    var alingments = point.Lines.OfType<MarkupRegularLine>().Select(l => l.GetAlignment(point)).Distinct().ToArray();
                    if (alingments.Any())
                    {
                        foreach (var alingment in alingments)
                            yield return new EnterFillerVertex(point, alingment);
                    }
                    else if (point.IsEdge)
                        yield return new EnterFillerVertex(point);
                }
            }
        }

        public Markup Markup { get; }
        public bool IsComplite { get; private set; }

        private List<IFillerVertex> SupportPoints { get; } = new List<IFillerVertex>();
        public IFillerVertex First => SupportPoints.FirstOrDefault();
        public IFillerVertex Last => SupportPoints.LastOrDefault();
        public IFillerVertex Prev => VertexCount >= 2 ? SupportPoints[SupportPoints.Count - 2] : null;
        public IFillerVertex PrePrev => VertexCount >= 3 ? SupportPoints[SupportPoints.Count - 3] : null;

        public IEnumerable<IFillerVertex> RawVertices => SupportPoints;
        public IFillerVertex[] ProcessedVertex { get; private set; } = new IFillerVertex[0];
        private int VertexCount => SupportPoints.Count;
        public bool IsEmpty => VertexCount == 0;

        public FillerLinePart[] RawParts { get; private set; } = new FillerLinePart[0];
        public FillerLinePart[] ProcessedParts { get; private set; } = new FillerLinePart[0];
        public int RawCount => RawParts.Length;
        public int ProcessedCount => ProcessedParts.Length;

        public IEnumerable<ITrajectory> TrajectoriesRaw => GetTrajectories(RawParts);
        public IEnumerable<ITrajectory> TrajectoriesProcessed => GetTrajectories(ProcessedParts);

        public bool IsMedian
        {
            get
            {
                for (var i = 0; i < VertexCount; i += 1)
                {
                    if (SupportPoints[i] is EnterFillerVertex enterVertex1 && (enterVertex1.Point.IsSplit || (SupportPoints[(i + 1) % VertexCount] is EnterFillerVertex enterVertex2 && enterVertex1.Enter == enterVertex2.Enter)))
                        return true;
                }

                return false;
            }
        }

        public FillerContour(Markup markup)
        {
            Markup = markup;
        }
        public bool Add(IFillerVertex newPoint)
        {
            if (!IsComplite)
            {
                if (newPoint.Equals(First))
                    IsComplite = true;
                else
                {
                    switch (newPoint)
                    {
                        case IntersectFillerVertex newIntersectVertex when Last is EnterFillerVertex lastEnterVertex:
                            SupportPoints.Remove(lastEnterVertex);
                            SupportPoints.Add(FixPoint(lastEnterVertex, newIntersectVertex));
                            break;

                        case IntersectFillerVertex newIntersectVertex when Last is LineEndFillerVertex lastLineEndVertex:
                            SupportPoints.Add(FixPoint(lastLineEndVertex, newIntersectVertex));
                            break;

                        case EnterFillerVertex newEnterVertex when Last is EnterFillerVertex lastEnterVertex:
                            if (lastEnterVertex.Point.Enter != newEnterVertex.Point.Enter && Markup.TryGetLine(lastEnterVertex.Point, newEnterVertex.Point, out MarkupRegularLine line1))
                            {
                                SupportPoints.Remove(lastEnterVertex);
                                SupportPoints.Add(FixPointByLine(lastEnterVertex, line1));
                                newPoint = FixPointByLine(newEnterVertex, line1);
                            }
                            break;

                        case EnterFillerVertex newEnterVertex when Last is LineEndFillerVertex lastLineEndVertex:
                            if (!lastLineEndVertex.Line.ContainsPoint(newEnterVertex.Point) && Markup.TryGetLine(lastLineEndVertex.Point, newEnterVertex.Point, out MarkupRegularLine line2))
                            {
                                SupportPoints.Add(FixPointByLine(lastLineEndVertex, line2));
                                newPoint = FixPointByLine(newEnterVertex, line2);
                            }
                            break;

                        case EnterFillerVertex newEnterVertex when Last is IntersectFillerVertex lastIntersectVertex:
                            newPoint = FixPoint(newEnterVertex, lastIntersectVertex);
                            break;
                    }

                    SupportPoints.Add(newPoint);
                }
                Update();
            }
            return IsComplite;

            static LineEndFillerVertex FixPoint(EnterFillerVertexBase enterVertex, IntersectFillerVertex intersectVertex) =>
                FixPointByLine(enterVertex, intersectVertex.LinePair.GetLine(enterVertex.Point) as MarkupRegularLine);

            static LineEndFillerVertex FixPointByLine(EnterFillerVertexBase enterVertex, MarkupRegularLine line) => new LineEndFillerVertex(enterVertex.Point, line);
        }

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
        public IEnumerable<IFillerVertex> GetNextСandidates() => Last is IFillerVertex last ? last.GetNextCandidates(this, Prev) : GetBeginCandidates(Markup);

        public void GetMinMaxT(IFillerVertex fillerVertex, MarkupLine line, out float resultT, out float resultMinT, out float resultMaxT)
        {
            fillerVertex.GetT(line, out float t);
            var minT = -1f;
            var maxT = 2f;

            foreach (var part in RawParts)
            {
                part.GetFromT(out float fromT);
                part.GetToT(out float toT);

                if (part.Line == line)
                {
                    Set(fromT, false);
                    Set(toT, false);
                }
                else if (Markup.GetIntersect(new MarkupLinePair(line, part.Line)) is MarkupLinesIntersect intersect && intersect.IsIntersect)
                {
                    var linePartT = intersect[part.Line];

                    if ((fromT <= linePartT && linePartT <= toT) || (toT <= linePartT && linePartT <= fromT))
                        Set(intersect[line], true);
                }
                else if (part.Line.IsEnterLine)
                {
                    if (line.Start.Enter == part.Line.Start.Enter && CheckEnter(line.Start.Num, part.Line.Start.Num, part.Line.End.Num))
                        Set(0, true);
                    if (line.End.Enter == part.Line.Start.Enter && CheckEnter(line.End.Num, part.Line.Start.Num, part.Line.End.Num))
                        Set(1, true);
                }
            }

            void Set(float tt, bool isStrict)
            {
                if (minT < tt && (isStrict ? tt < t : tt <= t))
                    minT = tt;

                if (maxT > tt && (isStrict ? tt > t : tt >= t))
                    maxT = tt;
            }

            static bool CheckEnter(byte num, byte start, byte end) => Math.Min(start, end) <= num && num <= Math.Max(end, start);

            resultT = t;
            resultMinT = minT;
            resultMaxT = maxT;
        }
        public void GetMinMaxNum(EnterFillerVertexBase vertex, out byte num, out byte minNum, out byte maxNum)
        {
            num = vertex.Point.Num;

            if (VertexCount > 2 && First is EnterFillerVertexBase firstVertex && firstVertex.Point == vertex.Point)
            {
                minNum = vertex.Point.Num;
                maxNum = vertex.Point.Num;
            }
            else
            {
                minNum = 0;
                maxNum = (byte)(vertex.Enter.PointCount + 1);

                foreach (var point in SupportPoints)
                {
                    if (point is EnterFillerVertexBase enterVertex && enterVertex.Point.Enter == vertex.Enter)
                    {
                        var n = enterVertex.Point.Num;

                        if (minNum < n && n < num)
                            minNum = n;

                        if (maxNum > n && n > num)
                            maxNum = n;
                    }
                }
            }
        }
        public IEnumerable<IFillerVertex> GetLinePoints(IFillerVertex fillerVertex, MarkupLine line)
        {
            GetMinMaxT(fillerVertex, line, out float t, out float minT, out float maxT);

            foreach (var intersectLine in line.IntersectLines)
            {
                var vertex = new IntersectFillerVertex(line, intersectLine);
                if (vertex.GetT(line, out float tt) && tt != t && minT < tt && tt < maxT)
                    yield return vertex;
            }

            switch (First)
            {
                case EnterFillerVertex firstE when line.ContainsPoint(firstE.Point) && ((line.IsStart(firstE.Point) && minT == 0) || (line.IsEnd(firstE.Point) && maxT == 1)):
                    yield return firstE;
                    break;
                case IntersectFillerVertex firstI when firstI.LinePair.ContainLine(line) && firstI.GetT(line, out float firstT) && (firstT == minT || firstT == maxT):
                    yield return firstI;
                    break;
            }

            if (line.Start.Type == MarkupPoint.PointType.Enter && t != 0 && minT < 0 && 0 < maxT)
                yield return new EnterFillerVertex(line.Start, line.GetAlignment(line.Start));

            if (line.End.Type == MarkupPoint.PointType.Enter && t != 1 && minT < 1 && 1 < maxT)
                yield return new EnterFillerVertex(line.End, line.GetAlignment(line.End));
        }

        private IEnumerable<ITrajectory> GetTrajectories(FillerLinePart[] parts)
        {
            foreach (var part in parts)
            {
                if (part.GetTrajectory(out ITrajectory trajectory))
                    yield return trajectory;
                else
                    yield return null;
            }
        }
        public ITrajectory GetRail(int a1, int b1, int a2, int b2)
        {
            var min1 = GetCorrectIndex(Math.Min(a1, b1));
            var max1 = GetCorrectIndex(Math.Max(a1, b1));
            var min2 = GetCorrectIndex(Math.Min(a2, b2));
            var max2 = GetCorrectIndex(Math.Max(a2, b2));

            if (max1 <= min2 || max2 <= min1 || (min2 <= min1 && max1 <= max2))
                return GetRail(min1, max1);
            else
                return GetRail(max1, min1);
        }
        private ITrajectory GetRail(int a, int b)
        {
            var trajectories = TrajectoriesProcessed.ToArray();

            if (Mathf.Abs(b - a) == 1)
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
        public int GetCorrectIndex(int value) => value >= 0 ? value % VertexCount : value % VertexCount + VertexCount;

        public int IndexOfRaw(IFillerVertex vertex) => IndexOf(SupportPoints, vertex);
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
            RawParts = GetParts(true).ToArray();
            ProcessedParts = GetParts(false).ToArray();
        }
        private IEnumerable<FillerLinePart> GetParts(bool isRaw)
        {
            var count = IsComplite ? VertexCount : VertexCount - 1;
            for (var i = 0; i < count; i += 1)
            {
                var vertex1 = SupportPoints[i];
                var vertex2 = SupportPoints[(i + 1) % VertexCount];
                if (vertex1 is not LineEndFillerVertex lineEnd1 || vertex2 is not LineEndFillerVertex lineEnd2 || lineEnd1.Point != lineEnd2.Point || (isRaw && lineEnd1.Alignment != lineEnd2.Alignment))
                    yield return GetFillerLine(vertex1, vertex2);
            }
        }


        public void Render(OverlayData data)
        {
            foreach (var trajectory in TrajectoriesRaw)
                trajectory.Render(data);
        }

        private class Comparer : IEqualityComparer<IFillerVertex>
        {
            public bool Equals(IFillerVertex x, IFillerVertex y) => x.Equals(y);
            public int GetHashCode(IFillerVertex vertex) => vertex.GetHashCode();
        }
    }

    public class FillerRail
    {
        public int A { get; }
        public int B { get; }

        public FillerRail(int a, int b)
        {
            A = Math.Max(a, 0);
            B = Math.Max(b, 0);
        }
        public static FillerRail operator +(FillerRail rail, int delta) => new FillerRail(rail.A + delta, rail.B + delta);
        public static FillerRail operator %(FillerRail rail, int max) => new FillerRail(rail.A % max, rail.B % max);

        public override string ToString() => $"{A + 1}-{B + 1}";
    }
}
