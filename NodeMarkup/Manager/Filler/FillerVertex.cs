using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.Manager
{
    public interface IFillerVertex : ISupportPoint
    {
        MarkupFiller Filler { get; }
        MarkupLine GetCommonLine(IFillerVertex other);
        IEnumerable<IFillerVertex> GetNextCandidates(IFillerVertex prev);
    }
    public class EnterFillerVertex : EnterSupportPoint, IFillerVertex
    {
        public MarkupFiller Filler { get; }
        public EnterFillerVertex(MarkupFiller filler, MarkupPoint point) : base(point)
        {
            Filler = filler;
        }

        public MarkupLine GetCommonLine(IFillerVertex other)
        {
            switch (other)
            {
                case EnterSupportPoint otherE:
                    if (Enter == otherE.Enter || !(Point.Lines.Intersect(otherE.Point.Lines).FirstOrDefault() is MarkupLine line))
                        line = new MarkupFakeLine(Point.Markup, Point, otherE.Point);
                    return line;
                case IntersectSupportPoint otherI:
                    return otherI.LinePair.First.ContainPoint(Point) ? otherI.LinePair.First : otherI.LinePair.Second;
                default:
                    return null;
            }
        }

        public IEnumerable<IFillerVertex> GetNextCandidates(IFillerVertex prev)
        {
            if(!(prev is EnterFillerVertex prevE && Enter == prevE.Point.Enter))
                foreach (var vertex in GetEnterOtherPoints())
                    yield return vertex;

            if (Point.IsEdge)
            {
                foreach (var vertex in GetOtherEnterPoint())
                    yield return vertex;
            }

            foreach (var vertex in GetPointLinesPoints())
                yield return vertex;
        }

        private IEnumerable<IFillerVertex> GetNextEnterCandidates(EnterFillerVertex prev)
        {
            if (Enter != prev.Point.Enter)
            {
                foreach (var vertex in GetEnterOtherPoints())
                    yield return vertex;
            }

            if (Point.IsEdge)
            {
                foreach (var vertex in GetOtherEnterPoint())
                    yield return vertex;
            }

            foreach (var vertex in GetPointLinesPoints())
                yield return vertex;
        }
        private IEnumerable<IFillerVertex> GetNextIntersectCandidates(IntersectFillerVertex prev)
        {
            foreach (var vertex in GetEnterOtherPoints())
                yield return vertex;

            if (Point.IsEdge)
            {
                foreach (var vertex in GetOtherEnterPoint())
                    yield return vertex;
            }

            foreach (var vertex in GetPointLinesPoints())
                yield return vertex;
        }
        private IEnumerable<IFillerVertex> GetNextEmptyCandidates()
        {
            foreach (var vertex in GetEnterOtherPoints())
                yield return vertex;

            if (Point.IsEdge)
            {
                foreach (var vertex in GetOtherEnterPoint())
                    yield return vertex;
            }

            foreach (var vertex in GetPointLinesPoints())
                yield return vertex;
        }
        private IEnumerable<IFillerVertex> GetOtherEnterPoint()
        {
            var otherEnterPoint = Point.IsFirst ? Enter.Next.LastPoint : Enter.Prev.FirstPoint;
            var vertex = new EnterFillerVertex(Filler, otherEnterPoint);
            if (vertex.Equals(Filler.First) || !Filler.Vertices.Any(v => vertex.Equals(v)))
                yield return vertex;
        }
        private IEnumerable<IFillerVertex> GetEnterOtherPoints()
        {
            Filler.GetMinMaxNum(this, out byte num, out byte minNum, out byte maxNum);

            foreach (var point in Enter.Points.Where(p => p.Num != num && minNum < p.Num && p.Num < maxNum && (p.IsEdge || p.Lines.Any())))
                yield return new EnterFillerVertex(Filler, point);

            if (Filler.First is EnterFillerVertex first && first.Enter == Enter && (minNum == first.Point.Num || first.Point.Num == maxNum))
                yield return first;
        }
        private IEnumerable<IFillerVertex> GetPointLinesPoints()
        {
            foreach (var line in Point.Lines)
            {
                foreach (var vertex in Filler.GetLinePoints(this, line))
                {
                    yield return vertex;
                }
            }
        }
    }
    public class IntersectFillerVertex : IntersectSupportPoint, IFillerVertex
    {
        public MarkupFiller Filler { get; }
        public IntersectFillerVertex(MarkupFiller filler, MarkupLinePair linePair) : base(linePair)
        {
            Filler = filler;
        }
        public IntersectFillerVertex(MarkupFiller filler, MarkupLine first, MarkupLine second) : this(filler, new MarkupLinePair(first, second)) { }

        public MarkupLine GetCommonLine(IFillerVertex other)
        {
            switch (other)
            {
                case EnterSupportPoint otherE:
                    return First.ContainPoint(otherE.Point) ? First : Second;
                case IntersectSupportPoint otherI:
                    return LinePair.ContainLine(otherI.LinePair.First) ? otherI.LinePair.First : otherI.LinePair.Second;
                default:
                    return null;
            }
        }

        public IEnumerable<IFillerVertex> GetNextCandidates(IFillerVertex prev)
        {
            switch (prev)
            {
                case EnterFillerVertex prevE:
                    return GetNextEnterCandidates(prevE);
                case IntersectFillerVertex prevI:
                    return GetNextIntersectCandidates(prevI);
                default:
                    return GetNextEmptyCandidates();
            }
        }
        private IEnumerable<IFillerVertex> GetNextEnterCandidates(EnterFillerVertex prev) => Filler.GetLinePoints(this, First.ContainPoint(prev.Point) ? Second : First);
        private IEnumerable<IFillerVertex> GetNextIntersectCandidates(IntersectFillerVertex prev) => Filler.GetLinePoints(this, prev.LinePair.ContainLine(First) ? Second : First);
        private IEnumerable<IFillerVertex> GetNextEmptyCandidates()
        {
            foreach (var vertex in Filler.GetLinePoints(this, First))
                yield return vertex;

            foreach (var vertex in Filler.GetLinePoints(this, Second))
                yield return vertex;
        }
    }
}
