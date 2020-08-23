using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace NodeMarkup.Manager
{
    public class MarkupFiller : IUpdate, IToXml
    {
        public static IEnumerable<IFillerVertex> GetBeginCandidates(Markup markup)
        {
            foreach (var intersect in markup.Intersects)
            {
                yield return new IntersectFillerVertex(intersect.Pair);
            }
            foreach (var enter in markup.Enters)
            {
                foreach (var point in enter.Points.Where(p => p.IsEdge || p.Lines.Any()))
                {
                    yield return new EnterFillerVertex(point);
                }
            }
        }
        public static string XmlName { get; } = "F";

        public Markup Markup { get; }

        FillerStyle _style;
        public FillerStyle Style
        {
            get => _style;
            set
            {
                _style = value;
                _style.OnStyleChanged = OnStyleChanged;
                OnStyleChanged();
            }
        }


        List<IFillerVertex> SupportPoints { get; } = new List<IFillerVertex>();
        public IFillerVertex First => SupportPoints.FirstOrDefault();
        public IFillerVertex Last => SupportPoints.LastOrDefault();
        public IFillerVertex Prev => VertexCount >= 2 ? SupportPoints[SupportPoints.Count - 2] : null;
        public IEnumerable<IFillerVertex> Vertices => SupportPoints;
        public int VertexCount => SupportPoints.Count;
        public bool IsEmpty => VertexCount == 0;

        List<MarkupLinePart> LineParts { get; } = new List<MarkupLinePart>();
        public IEnumerable<MarkupLinePart> Parts => LineParts;
        public MarkupStyleDash[] Dashes { get; private set; } = new MarkupStyleDash[0];
        public bool IsMedian => LineParts.Any(p => p.Line is MarkupEnterLine);

        public IEnumerable<ILineTrajectory> TrajectoriesRaw
        {
            get
            {
                foreach (var part in LineParts)
                {
                    if (part.GetTrajectory(out ILineTrajectory trajectory))
                        yield return trajectory;
                    else
                        yield return null;
                }
            }
        }

        public IEnumerable<ILineTrajectory> Trajectories => TrajectoriesRaw.Where(t => t != null).Select(t => t);

        public string XmlSection => XmlName;

        public MarkupFiller(Markup markup, FillerStyle style)
        {
            Markup = markup;
            Style = style;
        }
        public MarkupFiller(Markup markup, Style.StyleType fillerType) : this(markup, TemplateManager.GetDefault<FillerStyle>(fillerType)) { }

        public bool Add(IFillerVertex supportPoint)
        {
            if (supportPoint.Equals(First))
            {
                LineParts.Add(GetFillerLine(Last, First));
                return true;
            }
            else
            {
                SupportPoints.Add(supportPoint);
                if (VertexCount >= 2)
                    LineParts.Add(GetFillerLine(Prev, Last));

                return false;
            }
        }
        public void Remove()
        {
            if (SupportPoints.Any())
                SupportPoints.RemoveAt(SupportPoints.Count - 1);
            if (LineParts.Any())
                LineParts.RemoveAt(LineParts.Count - 1);
        }

        private void OnStyleChanged() => Markup.Update(this, true);
        public bool ContainsLine(MarkupLine line) => LineParts.Any(p => !(p.Line is MarkupEnterLine) && p.Line.PointPair == line.PointPair);
        public bool ContainsPoint(MarkupPoint point) => SupportPoints.Any(s => s is EnterFillerVertex vertex && vertex.Point == point);

        public FillerLinePart GetFillerLine(IFillerVertex first, IFillerVertex second)
        {
            var line = first.GetCommonLine(second);
            var linePart = new FillerLinePart(line, first, second);
            return linePart;
        }
        public IEnumerable<IFillerVertex> GetNextСandidates()
        {
            if (Last is IFillerVertex last)
                return last.GetNextCandidates(this, Prev);
            else
                return GetBeginCandidates(Markup);
        }

        public void GetMinMaxT(IFillerVertex fillerVertex, MarkupLine line, out float resultT, out float resultMinT, out float resultMaxT)
        {
            fillerVertex.GetT(line, out float t);
            var minT = -1f;
            var maxT = 2f;

            foreach (var linePart in LineParts)
            {
                linePart.GetFromT(out float fromT);
                linePart.GetToT(out float toT);

                if (linePart.Line == line)
                {
                    Set(fromT, false);
                    Set(toT, false);
                }
                else if (Markup.GetIntersect(new MarkupLinePair(line, linePart.Line)) is MarkupLinesIntersect intersect && intersect.IsIntersect)
                {
                    var linePartT = intersect[linePart.Line];

                    if ((fromT <= linePartT && linePartT <= toT) || (toT <= linePartT && linePartT <= fromT))
                        Set(intersect[line], true);
                }
                else if (linePart.Line.IsEnterLine)
                {
                    if (line.Start.Enter == linePart.Line.Start.Enter && CheckEnter(line.Start.Num, linePart.Line.Start.Num, linePart.Line.End.Num))
                        Set(0, true);
                    if (line.End.Enter == linePart.Line.Start.Enter && CheckEnter(line.End.Num, linePart.Line.Start.Num, linePart.Line.End.Num))
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
            bool CheckEnter(byte num, byte start, byte end) => (start <= num && num <= end) || (end <= num && num <= start);

            resultT = t;
            resultMinT = minT;
            resultMaxT = maxT;
        }
        public void GetMinMaxNum(EnterFillerVertex vertex, out byte resultNum, out byte resultMinNum, out byte resultMaxNum)
        {
            var num = vertex.Point.Num;
            var minNum = (byte)0;
            var maxNum = (byte)(vertex.Enter.PointCount + 1);

            foreach (var linePart in LineParts)
            {
                if (linePart.From is EnterSupportPoint fromVertex && fromVertex.Point.Enter == vertex.Enter)
                    Set(fromVertex.Point.Num);
                if (linePart.To is EnterSupportPoint toVertex && toVertex.Point.Enter == vertex.Enter)
                    Set(toVertex.Point.Num);
            }

            void Set(byte n)
            {
                if (minNum < n && n < num)
                    minNum = n;

                if (maxNum > n && n > num)
                    maxNum = n;
            }

            resultNum = num;
            resultMinNum = minNum;
            resultMaxNum = maxNum;
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
                case EnterFillerVertex firstE when line.ContainsPoint(firstE.Point) && ((line.Start == firstE.Point && minT == 0) || (line.End == firstE.Point && maxT == 1)):
                    yield return firstE;
                    break;
                case IntersectFillerVertex firstI when firstI.LinePair.ContainLine(line) && firstI.GetT(line, out float firstT) && (firstT == minT || firstT == maxT):
                    yield return firstI;
                    break;
            }

            if (line.Start.Type == MarkupPoint.PointType.Enter && t != 0 && minT < 0 && 0 < maxT)
                yield return new EnterFillerVertex(line.Start);

            if (line.End.Type == MarkupPoint.PointType.Enter && t != 1 && minT < 1 && 1 < maxT)
                yield return new EnterFillerVertex(line.End);
        }

        public void Update(bool onlySelfUpdate = false)
        {
            foreach (var part in LineParts)
            {
                if (part.Line is MarkupEnterLine fakeLine)
                    fakeLine.Update(true);
            }
        }
        public void RecalculateDashes() => Dashes = Style.Calculate(this).ToArray();

        public XElement ToXml()
        {
            var config = new XElement(XmlSection, Style.ToXml());
            foreach (var supportPoint in SupportPoints)
            {
                config.Add(supportPoint.ToXml());
            }
            return config;
        }
        public static bool FromXml(XElement config, Markup markup, Dictionary<ObjectId, ObjectId> map, out MarkupFiller filler)
        {
            if (!(config.Element(Manager.Style.XmlName) is XElement styleConfig) || !Manager.Style.FromXml(styleConfig, out FillerStyle style))
            {
                filler = default;
                return false;
            }

            filler = new MarkupFiller(markup, style);

            foreach (var supportConfig in config.Elements(FillerVertex.XmlName))
            {
                if (FillerVertex.FromXml(supportConfig, markup, map, out IFillerVertex vertex))
                    filler.Add(vertex);
                else
                    return false;
            }
            if(filler.First == null)
                return false;

            filler.Add(filler.First);

            return true;
        }

        public override string ToString() => Math.Abs(GetHashCode()).ToString();
    }
    public class FillerLinePart : MarkupLinePart
    {
        public override string XmlSection => throw new NotImplementedException();
        public new IFillerVertex From
        {
            get => base.From as IFillerVertex;
            set => base.From = value;
        }
        public new IFillerVertex To
        {
            get => base.To as IFillerVertex;
            set => base.To = value;
        }
        public FillerLinePart(MarkupLine line, IFillerVertex from, IFillerVertex to) : base(line, from, to) { }
    }
}

