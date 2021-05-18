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
        public static List<IFillerVertex> GetBeginCandidates(Markup markup)
        {
            var points = new List<IFillerVertex>();

            foreach (var intersect in markup.Intersects)
                points.Add(new IntersectFillerVertex(intersect.Pair));

            foreach (var enter in markup.Enters)
            {
                foreach (var point in enter.Points)
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

        public Markup Markup { get; }
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

        public FillerLinePart[] RawParts { get; private set; } = new FillerLinePart[0];
        public FillerLinePart[] ProcessedParts { get; private set; } = new FillerLinePart[0];
        public int RawCount => RawParts.Length;
        public int ProcessedCount => ProcessedParts.Length;

        public List<ITrajectory> TrajectoriesRaw => GetTrajectories(RawParts);
        public List<ITrajectory> TrajectoriesProcessed => GetTrajectories(ProcessedParts);

        public bool IsMedian
        {
            get
            {
                for (var i = 0; i < VertexCount; i += 1)
                {
                    if (SupportPoints[i] is EnterFillerVertexBase enterVertex1 && SupportPoints[(i + 1) % VertexCount] is EnterFillerVertexBase enterVertex2 && enterVertex1.Enter == enterVertex2.Enter)
                        return true;
                }

                return false;
            }
        }

        public FillerContour(Markup markup, IEnumerable<IFillerVertex> vertices = null)
        {
            Markup = markup;

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
                if (end.Point.Enter != nextEnd.Point.Enter && end.Line != nextEnd.Line && end.GetCommonLine(nextEnd) is MarkupRegularLine line)
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
                if (SupportPoints[prevI] is EnterFillerVertexBase prevEnter && enter.GetCommonLine(prevEnter) is MarkupRegularLine prevLine)
                {
                    SupportPoints.RemoveAt(i);
                    SupportPoints.Insert(prevI < i ? i : prevI, new LineEndFillerVertex(prevLine.PointPair.GetOther(enter.Point), prevLine));
                    SupportPoints.Insert(prevI < i ? i + 1 : i, new LineEndFillerVertex(enter.Point, prevLine));
                    i += prevI < i ? 1 : 0;
                }
                else if (SupportPoints[nextI] is EnterFillerVertexBase nextEnter && enter.GetCommonLine(nextEnter) is MarkupRegularLine nextLine)
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
                if (SupportPoints[prevI] is IFillerLineVertex prev && (prev.Contains(intersect.First) ? intersect.Second : intersect.First) is MarkupRegularLine line)
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
                        if (lastEnterVertex.GetCommonLine(newEnterVertex) is MarkupRegularLine line)
                        {
                            SupportPoints.Remove(lastEnterVertex);
                            SupportPoints.Add(FixVertexByLine(lastEnterVertex, line));

                            newVertex = FixVertexByLine(newEnterVertex, line);
                        }
                    }
                    break;

                case LineEndFillerVertex lastLineEndVertex when newEnterVertex.Enter != lastLineEndVertex.Enter:
                    {
                        if (lastLineEndVertex.GetCommonLine(newEnterVertex) is MarkupRegularLine line)
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
        static LineEndFillerVertex FixVertex(EnterFillerVertexBase enterVertex, IntersectFillerVertex intersectVertex) => FixVertexByLine(enterVertex, intersectVertex.LinePair.GetLine(enterVertex.Point) as MarkupRegularLine);
        static LineEndFillerVertex FixVertexByLine(EnterFillerVertexBase enterVertex, MarkupRegularLine line) => new LineEndFillerVertex(enterVertex.Point, line);

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
        public List<IFillerVertex> GetNextСandidates() => Last is IFillerVertex last ? last.GetNextCandidates(this, Prev) : GetBeginCandidates(Markup);

        public void GetMinMaxT(IFillerVertex fillerVertex, MarkupLine line, out float resultT, out float resultMinT, out float resultMaxT)
        {
            fillerVertex.GetT(line, out float vertexT);
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
        public void GetMinMaxNum(MarkupPoint point, out byte minNum, out byte maxNum)
        {
            if (VertexCount > 2 && First is EnterFillerVertexBase firstVertex && firstVertex.Point == point)
            {
                minNum = point.Num;
                maxNum = point.Num;
            }
            else
            {
                minNum = 0;
                maxNum = (byte)(point.Enter.PointCount + 1);

                foreach (var vertex in SupportPoints)
                {
                    if (vertex is EnterFillerVertexBase enterVertex && enterVertex.Point.Enter == point.Enter)
                    {
                        var n = enterVertex.Point.Num;

                        if (minNum < n && n < point.Num)
                            minNum = n;

                        if (maxNum > n && n > point.Num)
                            maxNum = n;
                    }
                }
            }
        }
        public bool IsAvailable(MarkupPoint point)
        {
            for (var i = 0; i < VertexCount - 1; i += 1)
            {
                if (SupportPoints[i] is EnterFillerVertexBase enterVertex1 &&
                    SupportPoints[i + 1] is EnterFillerVertexBase enterVertex2 &&
                    enterVertex1.Enter == point.Enter &&
                    enterVertex2.Enter == point.Enter &&
                    Math.Min(enterVertex1.Point.Num, enterVertex2.Point.Num) <= point.Num &&
                    point.Num <= Math.Max(enterVertex1.Point.Num, enterVertex2.Point.Num))
                    return false;
            }

            return true;
        }

        public List<IFillerVertex> GetLinePoints(IFillerVertex fillerVertex, MarkupLine line)
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

            if (line.Start.Type == MarkupPoint.PointType.Enter && t != 0 && minT < 0 && 0 < maxT)
                points.Add(new EnterFillerVertex(line.Start, line.GetAlignment(line.Start)));

            if (line.End.Type == MarkupPoint.PointType.Enter && t != 1 && minT < 1 && 1 < maxT)
                points.Add(new EnterFillerVertex(line.End, line.GetAlignment(line.End)));

            return points;
        }

        private List<ITrajectory> GetTrajectories(FillerLinePart[] parts)
        {
            var trajectories = new List<ITrajectory>(parts.Length);
            foreach (var part in parts)
            {
                part.GetTrajectory(out ITrajectory trajectory);
                trajectories.Add(trajectory);
            }
            return trajectories;
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
        public int GetCorrectIndex(int value) => value >= 0 ? value % ProcessedCount : value % ProcessedCount + ProcessedCount;

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
                if (vertex1 is not LineEndFillerVertex lineEnd1 || vertex2 is not LineEndFillerVertex lineEnd2 || lineEnd1.Point != lineEnd2.Point || isRaw)
                    yield return GetFillerLine(vertex1, vertex2);
            }
        }

        public void Render(OverlayData data)
        {
            foreach (var trajectory in TrajectoriesRaw)
                trajectory?.Render(data);
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
