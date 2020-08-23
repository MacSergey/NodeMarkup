using NodeMarkup.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace NodeMarkup.Manager
{
    public interface IFillerVertex : ISupportPoint
    {
        MarkupLine GetCommonLine(IFillerVertex other);
        IEnumerable<IFillerVertex> GetNextCandidates(MarkupFiller filler, IFillerVertex prev);
    }
    public static class FillerVertex
    {
        public static string XmlName { get; } = "V";
        public static bool FromXml(XElement config, Markup markup, Dictionary<ObjectId, ObjectId> map, out IFillerVertex fillerVertex)
        {
            var type = (SupportType)config.GetAttrValue<int>("T");
            switch (type)
            {
                case SupportType.EnterPoint when EnterFillerVertex.FromXml(config, markup, map, out EnterFillerVertex enterPoint):
                    fillerVertex = enterPoint;
                    return true;
                case SupportType.LinesIntersect when IntersectFillerVertex.FromXml(config, markup, map, out IntersectFillerVertex linePoint):
                    fillerVertex = linePoint;
                    return true;
                default:
                    fillerVertex = null;
                    return false;
            }
        }
    }
    public class EnterFillerVertex : EnterSupportPoint, IFillerVertex
    {
        public static bool FromXml(XElement config, Markup markup, Dictionary<ObjectId, ObjectId> map, out EnterFillerVertex enterPoint)
        {
            var pointId = config.GetAttrValue<int>(MarkupPoint.XmlName);
            if (MarkupPoint.FromId(pointId, markup, map, out MarkupPoint point))
            {
                enterPoint = new EnterFillerVertex(point);
                return true;
            }
            else
            {
                enterPoint = null;
                return false;
            }
        }

        public override string XmlSection => FillerVertex.XmlName;
        public EnterFillerVertex(MarkupPoint point) : base(point) { }

        public MarkupLine GetCommonLine(IFillerVertex other)
        {
            switch (other)
            {
                case EnterSupportPoint otherE:
                    if (Enter == otherE.Enter || !(Point.Lines.Intersect(otherE.Point.Lines).FirstOrDefault() is MarkupLine line))
                        line = new MarkupEnterLine(Point.Markup, Point, otherE.Point);
                    return line;
                case IntersectSupportPoint otherI:
                    return otherI.LinePair.First.ContainsPoint(Point) ? otherI.LinePair.First : otherI.LinePair.Second;
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
        public static bool FromXml(XElement config, Markup markup, Dictionary<ObjectId, ObjectId> map, out IntersectFillerVertex linePoint)
        {
            var lineId1 = config.GetAttrValue<ulong>(MarkupPointPair.XmlName1);
            var lineId2 = config.GetAttrValue<ulong>(MarkupPointPair.XmlName2);

            if (markup.TryGetLine(lineId1, map, out MarkupLine line1) && markup.TryGetLine(lineId2, map, out MarkupLine line2))
            {
                linePoint = new IntersectFillerVertex(line1, line2);
                return true;
            }
            else
            {
                linePoint = null;
                return false;
            }
        }
        public override string XmlSection => FillerVertex.XmlName;

        public IntersectFillerVertex(MarkupLinePair linePair) : base(linePair) { }
        public IntersectFillerVertex(MarkupLine first, MarkupLine second) : this(new MarkupLinePair(first, second)) { }

        public MarkupLine GetCommonLine(IFillerVertex other)
        {
            switch (other)
            {
                case EnterSupportPoint otherE:
                    return First.ContainsPoint(otherE.Point) ? First : Second;
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
                    return filler.GetLinePoints(this, First.ContainsPoint(prevE.Point) ? Second : First);
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

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute(MarkupPointPair.XmlName1, First.Id));
            config.Add(new XAttribute(MarkupPointPair.XmlName2, Second.Id));
            return config;
        }
    }
}
