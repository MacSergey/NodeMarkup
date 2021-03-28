using ModsCommon.Utilities;
using NodeMarkup.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace NodeMarkup.Manager
{
    public interface IFillerVertex : ISupportPoint
    {
        MarkupLine GetCommonLine(IFillerVertex other);
        IEnumerable<IFillerVertex> GetNextCandidates(FillerContour contour, IFillerVertex prev);
    }
    public static class FillerVertex
    {
        public static string XmlName { get; } = "V";
        public static bool FromXml(XElement config, Markup markup, ObjectsMap map, out IFillerVertex fillerVertex)
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
    public abstract class EnterFillerVertexBase : EnterSupportPoint, IFillerVertex
    {
        public static bool FromXml(XElement config, Markup markup, ObjectsMap map, out EnterFillerVertex enterPoint)
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
        public abstract Alignment Alignment { get; }

        public EnterFillerVertexBase(MarkupPoint point) : base(point) { }
        public override void Update() => Init(Point.GetPosition(Alignment));
        public override bool Equals(EnterSupportPoint other) => base.Equals(other) && (other is not EnterFillerVertexBase otherVertex || otherVertex.Alignment == Alignment);

        public override bool GetT(MarkupLine line, out float t)
        {
            if (line is MarkupEnterLine enterLine && line.Start == line.End)
            {
                if(enterLine.StartAlignment == Alignment)
                {
                    t = 0f;
                    return true;
                }
                else if(enterLine.EndAlignment == Alignment)
                {
                    t = 1f;
                    return true;
                }
                else
                {
                    t = -1f;
                    return false;
                }
            }
            else
                return base.GetT(line, out t);
        }
        public MarkupLine GetCommonLine(IFillerVertex other)
        {
            switch (other)
            {
                case EnterFillerVertexBase otherE:
                    if (Enter == otherE.Enter || Point.Lines.Intersect(otherE.Point.Lines).FirstOrDefault() is not MarkupLine line)
                        line = new MarkupEnterLine(Point.Markup, Point, otherE.Point, Alignment, otherE.Alignment);
                    return line;
                case IntersectFillerVertex otherI:
                    return otherI.LinePair.First.ContainsPoint(Point) ? otherI.LinePair.First : otherI.LinePair.Second;
                default:
                    return null;
            }
        }

        public IEnumerable<IFillerVertex> GetNextCandidates(FillerContour contour, IFillerVertex prev)
        {
            if (prev is not EnterFillerVertexBase prevE || Enter != prevE.Point.Enter)
            {
                foreach (var vertex in GetEnterOtherPoints(contour))
                    yield return vertex;
            }

            if (Point.IsEdge)
            {
                foreach (var vertex in GetOtherEnterPoint(contour))
                    yield return vertex;
            }

            foreach (var vertex in GetPointLinesPoints(contour))
                yield return vertex;
        }
        private IEnumerable<IFillerVertex> GetOtherEnterPoint(FillerContour contour)
        {
            var otherEnterPoint = Point.IsFirst ? Enter.Prev.LastPoint : Enter.Next.FirstPoint;
            var vertex = new EnterFillerVertex(otherEnterPoint);
            var isCanEnd = vertex.Equals(contour.First) && contour.VertexCount >= 3;
            var isUsed = contour.RawVertices.Any(v => vertex.Equals(v));
            var isEdgeLine = Point.Lines.Any(l => l.ContainsPoint(otherEnterPoint));
            if ((isCanEnd || !isUsed) && !isEdgeLine)
                yield return vertex;
        }
        private IEnumerable<IFillerVertex> GetEnterOtherPoints(FillerContour contour)
        {
            contour.GetMinMaxNum(this, out byte num, out byte minNum, out byte maxNum);

            foreach (var point in Enter.Points)
            {
                if (point.Num != num && minNum < point.Num && point.Num < maxNum)
                {
                    if (point.HaveLines)
                    {
                        yield return new EnterFillerVertex(point);
                        if (point.IsSplit)
                        {
                            yield return new EnterFillerVertex(point, Alignment.Left);
                            yield return new EnterFillerVertex(point, Alignment.Right);
                        }
                    }
                    else if (point.IsEdge)
                        yield return new EnterFillerVertex(point);
                }
            }

            if (contour.First is EnterFillerVertexBase first && first.Enter == Enter && (first.Point.Num == minNum || first.Point.Num == maxNum))
                yield return first;
        }
        private IEnumerable<IFillerVertex> GetPointLinesPoints(FillerContour contour)
        {
            foreach (var line in Point.Lines.OfType<MarkupRegularLine>())
            {
                foreach (var vertex in contour.GetLinePoints(this, line))
                    yield return vertex;
            }
        }
        public override string ToString() => $"{Point} - {Alignment}";
    }
    public class EnterFillerVertex : EnterFillerVertexBase
    {
        public override Alignment Alignment => RawAlignment;
        public Alignment RawAlignment { get; }
        public EnterFillerVertex(MarkupPoint point, Alignment alignment = Alignment.Centre) : base(point)
        {
            RawAlignment = alignment;
            Update();
        }
    }
    public class LineEndFillerVertex : EnterFillerVertexBase
    {
        public override Alignment Alignment => Line?.GetAlignment(Point) ?? Alignment.Centre;
        public MarkupRegularLine Line { get; private set; }
        public LineEndFillerVertex(MarkupPoint point, MarkupRegularLine line = null) : base(point)
        {
            Line = line;
            Update();
        }
        public override string ToString() => $"{Point} ({Line}) - {Alignment}";
    }

    public class IntersectFillerVertex : IntersectSupportPoint, IFillerVertex
    {
        public static bool FromXml(XElement config, Markup markup, ObjectsMap map, out IntersectFillerVertex linePoint)
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
            return other switch
            {
                EnterSupportPoint otherE => First.ContainsPoint(otherE.Point) ? First : Second,
                IntersectSupportPoint otherI => LinePair.ContainLine(otherI.LinePair.First) ? otherI.LinePair.First : otherI.LinePair.Second,
                _ => null,
            };
        }

        public IEnumerable<IFillerVertex> GetNextCandidates(FillerContour contour, IFillerVertex prev)
        {
            return prev switch
            {
                EnterFillerVertex prevE => contour.GetLinePoints(this, First.ContainsPoint(prevE.Point) ? Second : First),
                IntersectFillerVertex prevI => contour.GetLinePoints(this, prevI.LinePair.ContainLine(First) ? Second : First),
                _ => GetNextEmptyCandidates(contour),
            };
        }
        private IEnumerable<IFillerVertex> GetNextEmptyCandidates(FillerContour contour)
        {
            foreach (var vertex in contour.GetLinePoints(this, First))
                yield return vertex;

            foreach (var vertex in contour.GetLinePoints(this, Second))
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
