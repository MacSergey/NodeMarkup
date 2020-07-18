using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.Manager
{
    public class MarkupFiller
    {
        public Markup Markup { get; }
        List<IFillerVertex> SupportPoints { get; } = new List<IFillerVertex>();
        public IFillerVertex First => SupportPoints.FirstOrDefault();
        public IFillerVertex Last => SupportPoints.LastOrDefault();
        public IFillerVertex Prev => SupportPoints.Count >= 2 ? SupportPoints[SupportPoints.Count - 2] : null;
        public IEnumerable<IFillerVertex> Vertices => SupportPoints;
        public int VertexCount => SupportPoints.Count;

        List<MarkupLinePart> LineParts { get; } = new List<MarkupLinePart>();
        public IEnumerable<MarkupLinePart> Parts => LineParts;


        public MarkupFiller(Markup markup)
        {
            Markup = markup;
        }

        public void Add(IFillerVertex supportPoint)
        {
            SupportPoints.Add(supportPoint);
            if (VertexCount >= 2)
                LineParts.Add(GetFillerLine(Last, Prev));
        }
        public void Remove()
        {
            if (SupportPoints.Any())
                SupportPoints.RemoveAt(SupportPoints.Count - 1);
            if (LineParts.Any())
                LineParts.RemoveAt(LineParts.Count - 1);
        }
        public FillerLinePart GetFillerLine(IFillerVertex first, IFillerVertex second)
        {
            var line = first.GetСommonLine(second);
            var linePart = new FillerLinePart(line, first.GetPartEdge(line), second.GetPartEdge(line));
            return linePart;
        }
    }
    public class FillerLinePart : MarkupLinePart
    {
        public override string XmlSection => throw new NotImplementedException();
        public FillerLinePart(MarkupLine line, ILinePartEdge from, ILinePartEdge to) : base(line, from, to) { }
    }

    public interface IFillerVertex : ISupportPoint
    {
        List<IFillerVertex> Next(IFillerVertex prev);
        MarkupLine GetСommonLine(IFillerVertex other);
    }
    public class EnterVertexPoint : EnterSupportPoint, IFillerVertex
    {
        public EnterVertexPoint(MarkupPoint point) : base(point) { }

        public List<IFillerVertex> Next(IFillerVertex prev)
        {
            var next = new List<IFillerVertex>();

            if (prev is EnterVertexPoint enterSupport)
            {
                if (enterSupport.Point.Enter == Point.Enter)
                {
                    if (Point.IsEdge)
                        next.Add(GetOtherEnterPoint());
                }
                else
                    next.AddRange(GetEnterOtherPoints());

                next.AddRange(GetPointLinesPoints());
            }
            else if (prev is IntersectVertexPoint)
            {
                next.AddRange(GetEnterOtherPoints());
                if (Point.IsEdge)
                    next.Add(GetOtherEnterPoint());
            }
            else
            {
                next.AddRange(GetEnterOtherPoints());
                if (Point.IsEdge)
                    next.Add(GetOtherEnterPoint());
                next.AddRange(GetPointLinesPoints());
            }

            return next;
        }
        private IFillerVertex GetOtherEnterPoint()
        {
            var otherEnterPoint = Point.IsFirst ? Point.Enter.Next.LastPoint : Point.Enter.Prev.FirstPoint;
            return new EnterVertexPoint(otherEnterPoint);
        }
        private IEnumerable<IFillerVertex> GetEnterOtherPoints()
        {
            foreach (var point in Point.Enter.Points.Where(p => p != Point && p.Lines.Any()))
            {
                yield return new EnterVertexPoint(point);
            }
        }
        private IEnumerable<IFillerVertex> GetPointLinesPoints()
        {
            foreach (var line in Point.Lines)
            {
                foreach (var intersectLine in line.IntersectLines.Where(l => !l.ContainPoint(Point)))
                {
                    yield return new IntersectVertexPoint(line, intersectLine);
                }
                yield return new EnterVertexPoint(line.PointPair.GetOther(Point));
            }
        }

        public MarkupLine GetСommonLine(IFillerVertex other)
        {
            if (other is EnterVertexPoint otherE)
            {
                if (!(Point.Lines.Intersect(otherE.Point.Lines).FirstOrDefault() is MarkupLine line))
                {
                    line = new MarkupLine(Point.Markup, Point, otherE.Point);
                    line.Update();
                }
                return line;
            }
            else if (other is IntersectVertexPoint otherI)
                return otherI.LinePair.First.ContainPoint(Point) ? otherI.LinePair.First : otherI.LinePair.Second;
            else
                return null;
        }
    }
    public class IntersectVertexPoint : IntersectSupportPoint, IFillerVertex
    {
        public IntersectVertexPoint(MarkupLinePair linePair) : base(linePair) { }
        public IntersectVertexPoint(MarkupLine first, MarkupLine second) : base(first, second) { }

        public List<IFillerVertex> Next(IFillerVertex prev)
        {
            var next = new List<IFillerVertex>();

            if (prev is EnterVertexPoint enterSupport)
                next.AddRange(GetNextLinePoints(LinePair.First.ContainPoint(enterSupport.Point) ? LinePair.First : LinePair.Second));
            else if (prev is IntersectVertexPoint intersectSupport)
                next.AddRange(GetNextLinePoints(intersectSupport.LinePair.ContainLine(LinePair.First) ? LinePair.First : LinePair.Second));
            else
            {
                next.AddRange(GetNextLinePoints(LinePair.Second));
                next.AddRange(GetNextLinePoints(LinePair.First));
            }

            return next;
        }

        private IEnumerable<IFillerVertex> GetNextLinePoints(MarkupLine ignore)
        {
            var line = LinePair.GetOther(ignore);
            foreach (var intersectLine in line.IntersectLines.Where(l => l != ignore))
            {
                yield return new IntersectVertexPoint(line, intersectLine);
            }
            yield return new EnterVertexPoint(line.Start);
            yield return new EnterVertexPoint(line.End);
        }
        public MarkupLine GetСommonLine(IFillerVertex other)
        {
            if (other is EnterVertexPoint otherE)
                return LinePair.First.ContainPoint(otherE.Point) ? LinePair.First : LinePair.Second;
            else if (other is IntersectVertexPoint otherI)
                return LinePair.ContainLine(otherI.LinePair.First) ? otherI.LinePair.First : otherI.LinePair.Second;
            else
                return null;
        }
    }
}
   
