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
        List<IFillerVertex> GetNextCandidates(FillerContour contour, IFillerVertex prev);
        IFillerVertex ProcessedVertex { get; }
    }
    public interface IFillerLineVertex : IFillerVertex
    {
        bool Contains(MarkupLine line);
    }
    public static class FillerVertex
    {
        public static string XmlName { get; } = "V";
        public static bool FromXml(XElement config, Markup markup, Utilities.ObjectsMap map, out IFillerVertex fillerVertex)
        {
            var type = (SupportType)config.GetAttrValue<int>("T");
            switch (type)
            {
                case SupportType.EnterPoint when EnterFillerVertexBase.FromXml(config, markup, map, out EnterFillerVertexBase enterPoint):
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
        public static bool FromXml(XElement config, Markup markup, Utilities.ObjectsMap map, out EnterFillerVertexBase enterPoint)
        {
            var pointId = config.GetAttrValue<int>(MarkupPoint.XmlName);
            if (MarkupPoint.FromId(pointId, markup, map, out MarkupPoint point))
            {
                var lineId = config.GetAttrValue<ulong>(MarkupLine.XmlName);
                if (markup.TryGetLine(lineId, map, out MarkupRegularLine line))
                    enterPoint = new LineEndFillerVertex(point, line);
                else
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
        public abstract IFillerVertex ProcessedVertex { get; }

        public EnterFillerVertexBase(MarkupPoint point) : base(point) { }
        public override void Update() => Init(Point.GetPosition(Alignment));
        public override bool Equals(EnterSupportPoint other) => base.Equals(other) && (!Point.IsSplit || other is not EnterFillerVertexBase otherVertex || otherVertex.Alignment == Alignment);

        public override bool GetT(MarkupLine line, out float t)
        {
            if (line is MarkupEnterLine enterLine && line.Start == line.End)
            {
                if (enterLine.StartAlignment == Alignment)
                {
                    t = 0f;
                    return true;
                }
                else if (enterLine.EndAlignment == Alignment)
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
        public MarkupLine GetCommonLine(IFillerVertex other) => other switch
        {
            EnterFillerVertexBase otherE when Enter == otherE.Enter => new MarkupEnterLine(Point.Markup, Point, otherE.Point, Alignment, otherE.Alignment),
            EnterFillerVertexBase otherE when Point.Lines.Intersect(otherE.Point.Lines).FirstOrDefault() is MarkupLine line => line,
            EnterFillerVertexBase otherE when Enter.Markup.EntersCount > 2 && ((Enter.Next == otherE.Enter && Point.IsLast && otherE.Point.IsFirst) || (Enter.Prev == otherE.Enter && Point.IsFirst && otherE.Point.IsLast)) => new MarkupEnterLine(Point.Markup, Point, otherE.Point, Alignment, otherE.Alignment),
            EnterFillerVertexBase otherE => new MarkupFillerTempLine(Point.Markup, Point, otherE.Point, alignment: Point.IsSplit ? Alignment : (otherE.Point.IsSplit ? otherE.Alignment.Invert() : Alignment.Centre)),
            IntersectFillerVertex otherI => otherI.LinePair.First.ContainsPoint(Point) ? otherI.LinePair.First : otherI.LinePair.Second,
            _ => null,
        };

        public List<IFillerVertex> GetNextCandidates(FillerContour contour, IFillerVertex prev)
        {
            var points = new List<IFillerVertex>();
            points.AddRange(GetEnterOtherPoints(contour, prev));
            points.AddRange(GetPointLinesPoints(contour));
            return points;
        }
        private List<IFillerVertex> GetEnterOtherPoints(FillerContour contour, IFillerVertex prev)
        {
            var points = new List<IFillerVertex>();
            contour.GetMinMaxNum(Point, out byte minNum, out byte maxNum);

            if (prev is not EnterFillerVertexBase prevE || Enter != prevE.Point.Enter)
            {
                foreach (var point in Enter.Points)
                {
                    if (minNum < point.Num && point.Num < maxNum)
                    {
                        if (point != Point)
                        {
                            points.Add(new EnterFillerVertex(point));
                            if (point.IsSplit)
                            {
                                points.Add(new EnterFillerVertex(point, Alignment.Left));
                                points.Add(new EnterFillerVertex(point, Alignment.Right));
                            }
                        }
                        else if (Point.IsSplit)
                        {
                            foreach (var alingment in EnumExtension.GetEnumValues<Alignment>())
                            {
                                if (alingment != Alignment)
                                    points.Add(new EnterFillerVertex(point, alingment));
                            }
                        }
                    }
                }
            }

            if (contour.PossibleComplite && contour.First is EnterFillerVertexBase first && first.Enter == Enter && (first.Point.Num == minNum || first.Point.Num == maxNum))
                points.Add(first);

            return points;
        }
        private List<IFillerVertex> GetPointLinesPoints(FillerContour contour)
        {
            var points = new List<IFillerVertex>();

            foreach (var enter in Point.Markup.Enters)
            {
                if (enter == Point.Enter)
                    continue;

                foreach (var point in enter.Points)
                {
                    if (point.Markup.TryGetLine(Point, point, out MarkupRegularLine line))
                    {
                        if (!Point.IsSplit || line.GetAlignment(Point) == Alignment)
                            points.AddRange(contour.GetLinePoints(this, line));
                    }
                    else
                    {
                        var alignments = new List<Alignment>();

                        if (contour.First is EnterFillerVertexBase lastEnter && lastEnter.Point == point)
                        {
                            if (lastEnter.Point.IsSplit)
                                alignments.Add(lastEnter.Alignment.Invert());
                            else
                                alignments.AddRange(EnumExtension.GetEnumValues<Alignment>());
                        }
                        else if (contour.IsAvailable(point))
                            alignments.AddRange(EnumExtension.GetEnumValues<Alignment>());
                        else
                            continue;

                        if (Point.IsSplit)
                            alignments.RemoveAll(a => a != Alignment);
                        else if (!point.IsSplit)
                            alignments.RemoveAll(a => a != Alignment.Centre);

                        foreach (var alignment in alignments)
                        {
                            line = new MarkupFillerTempLine(point.Markup, Point, point, alignment: alignment);
                            contour.GetMinMaxT(this, line, out float t, out float minT, out float maxT);
                            if ((t == 0f && maxT >= 1f) || (t == 1f && minT <= 0f))
                                points.Add(new EnterFillerVertex(point, alignment.Invert()));
                        }
                    }
                }
            }

            return points;
        }
        public override int GetHashCode() => Point.GetHashCode();
        public override string ToString() => $"{Point} - {Alignment}";
    }
    public class EnterFillerVertex : EnterFillerVertexBase
    {
        public override Alignment Alignment => RawAlignment;
        public Alignment RawAlignment { get; }
        public override IFillerVertex ProcessedVertex => new EnterFillerVertex(Point);
        public EnterFillerVertex(MarkupPoint point, Alignment alignment = Alignment.Centre) : base(point)
        {
            RawAlignment = alignment;
            Update();
        }
    }
    public class LineEndFillerVertex : EnterFillerVertexBase, IFillerLineVertex
    {
        public override Alignment Alignment => Line?.GetAlignment(Point) ?? Alignment.Centre;
        public override IFillerVertex ProcessedVertex => new EnterFillerVertex(Point);
        public MarkupRegularLine Line { get; private set; }
        public LineEndFillerVertex(MarkupPoint point, MarkupRegularLine line) : base(point)
        {
            Line = line;
            Update();
        }
        public override void Update()
        {
            if (Line is MarkupFillerTempLine && Point.Markup.TryGetLine<MarkupRegularLine>(Line.PointPair, out var regularLine))
                Line = regularLine;

            base.Update();
        }
        public bool Contains(MarkupLine line) => Line == line;
        public override bool Equals(EnterSupportPoint other) => base.Equals(other) && other is LineEndFillerVertex lineEndVertex && lineEndVertex.Line == Line;

        public override string ToString() => $"{Point} ({Line}) - {Alignment}";

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.AddAttr(MarkupLine.XmlName, Line.PointPair.Hash);
            return config;
        }
    }

    public class IntersectFillerVertex : IntersectSupportPoint, IFillerVertex, IFillerLineVertex
    {
        public static bool FromXml(XElement config, Markup markup, Utilities.ObjectsMap map, out IntersectFillerVertex linePoint)
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
        public IFillerVertex ProcessedVertex => this;

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

        public List<IFillerVertex> GetNextCandidates(FillerContour contour, IFillerVertex prev)
        {
            return prev switch
            {
                EnterFillerVertex prevE => contour.GetLinePoints(this, First.ContainsPoint(prevE.Point) ? Second : First),
                IntersectFillerVertex prevI => contour.GetLinePoints(this, prevI.LinePair.ContainLine(First) ? Second : First),
                _ => GetNextEmptyCandidates(contour),
            };
        }
        private List<IFillerVertex> GetNextEmptyCandidates(FillerContour contour)
        {
            var points = new List<IFillerVertex>();
            points.AddRange(contour.GetLinePoints(this, First));
            points.AddRange(contour.GetLinePoints(this, Second));
            return points;
        }

        public bool Contains(MarkupLine line) => LinePair.ContainLine(line);

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.AddAttr(MarkupPointPair.XmlName1, First.Id);
            config.AddAttr(MarkupPointPair.XmlName2, Second.Id);
            return config;
        }
        public override int GetHashCode() => LinePair.GetHashCode();
    }
}
