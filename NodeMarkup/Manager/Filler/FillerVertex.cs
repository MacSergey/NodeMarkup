using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.Manager
{
    public interface IFillerVertex : ISupportPoint
    {
        MarkupLine GetCommonLine(IFillerVertex other);
        IEnumerable<IFillerVertex> GetNextCandidates(MarkupFiller filler, IFillerVertex prev);
    }
    public class EnterFillerVertex : EnterSupportPoint, IFillerVertex
    {
        public EnterFillerVertex(MarkupPoint point) : base(point) { }

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

        public IEnumerable<IFillerVertex> GetNextCandidates(MarkupFiller filler, IFillerVertex prev)
        {
            if(!(prev is EnterFillerVertex prevE && Enter == prevE.Point.Enter))
                foreach (var vertex in GetEnterOtherPoints(filler))
                    yield return vertex;

            if (Point.IsEdge)
            {
                foreach (var vertex in GetOtherEnterPoint(filler))
                    yield return vertex;
            }

            foreach (var vertex in GetPointLinesPoints(filler))
                yield return vertex;
        }
        private IEnumerable<IFillerVertex> GetOtherEnterPoint(MarkupFiller filler)
        {
            var otherEnterPoint = Point.IsFirst ? Enter.Next.LastPoint : Enter.Prev.FirstPoint;
            var vertex = new EnterFillerVertex(otherEnterPoint);
            if (vertex.Equals(filler.First) || !filler.Vertices.Any(v => vertex.Equals(v)))
                yield return vertex;
        }
        private IEnumerable<IFillerVertex> GetEnterOtherPoints(MarkupFiller filler)
        {
            filler.GetMinMaxNum(this, out byte num, out byte minNum, out byte maxNum);

            foreach (var point in Enter.Points.Where(p => p.Num != num && minNum < p.Num && p.Num < maxNum && (p.IsEdge || p.Lines.Any())))
                yield return new EnterFillerVertex(point);

            if (filler.First is EnterFillerVertex first && first.Enter == Enter && (minNum == first.Point.Num || first.Point.Num == maxNum))
                yield return first;
        }
        private IEnumerable<IFillerVertex> GetPointLinesPoints(MarkupFiller filler)
        {
            foreach (var line in Point.Lines)
            {
                foreach (var vertex in filler.GetLinePoints(this, line))
                {
                    yield return vertex;
                }
            }
        }
    }
    public class IntersectFillerVertex : IntersectSupportPoint, IFillerVertex
    {
        public IntersectFillerVertex(MarkupLinePair linePair) : base(linePair) { }
        public IntersectFillerVertex(MarkupLine first, MarkupLine second) : this(new MarkupLinePair(first, second)) { }

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

        public IEnumerable<IFillerVertex> GetNextCandidates(MarkupFiller filler, IFillerVertex prev)
        {
            switch (prev)
            {
                case EnterFillerVertex prevE:
                    return filler.GetLinePoints(this, First.ContainPoint(prevE.Point) ? Second : First);
                case IntersectFillerVertex prevI:
                    return filler.GetLinePoints(this, prevI.LinePair.ContainLine(First) ? Second : First);
                default:
                    return GetNextEmptyCandidates(filler);
            }
        }
        private IEnumerable<IFillerVertex> GetNextEmptyCandidates(MarkupFiller filler)
        {
            foreach (var vertex in filler.GetLinePoints(this, First))
                yield return vertex;

            foreach (var vertex in filler.GetLinePoints(this, Second))
                yield return vertex;
        }
    }
}
