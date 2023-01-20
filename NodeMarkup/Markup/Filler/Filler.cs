using ModsCommon.Utilities;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace NodeMarkup.Manager
{
    public class MarkingFiller : IStyleItem, IToXml, ISupport
    {
        public static string XmlName { get; } = "F";

        public string DeleteCaptionDescription => Localize.FillerEditor_DeleteCaptionDescription;
        public string DeleteMessageDescription => Localize.FillerEditor_DeleteMessageDescription;
        public Marking.SupportType Support => Marking.SupportType.Fillers;

        public Marking Marking { get; }
        public int Id { get; }
        public FillerContour Contour { get; }

        public PropertyValue<FillerStyle> Style { get; }
        public List<IStyleData> StyleData { get; } = new List<IStyleData>();
        public bool IsMedian => Contour.IsMedian;

        public string XmlSection => XmlName;

        public MarkingFiller(FillerContour contour, FillerStyle style)
        {
            Id = Math.Abs(GetHashCode());
            Contour = contour;
            Marking = Contour.Markup;
            style.OnStyleChanged = FillerChanged;
            Style = new PropertyClassValue<FillerStyle>(StyleChanged, style);
        }

        private void StyleChanged()
        {
            Style.Value.OnStyleChanged = FillerChanged;
            FillerChanged();
        }
        private void FillerChanged() => Marking?.Update(this, true);
        public bool ContainsLine(MarkingLine line) => Contour.RawParts.Any(p => p.Line is not MarkingEnterLine && p.Line.PointPair == line.PointPair);
        public bool ContainsPoint(MarkingPoint point) => Contour.RawVertices.Any(s => s is EnterFillerVertexBase vertex && vertex.Point == point);

        public void Update(bool onlySelfUpdate = false) => Contour.Update();
        public void RecalculateStyleData()
        {
#if DEBUG_RECALCULATE
            Mod.Logger.Debug($"Recalculate filler {this}");
#endif
            StyleData.Clear();
            StyleData.AddRange(Style.Value.Calculate(this));
        }
        public Dependences GetDependences() => new Dependences();

        public XElement ToXml()
        {
            var config = new XElement(XmlSection, Style.Value.ToXml());

            foreach (var supportPoint in Contour.RawVertices)
                config.Add(supportPoint.ToXml());

            return config;
        }
        public static bool FromXml(XElement config, Marking markup, ObjectsMap map, out MarkingFiller filler)
        {
            filler = default;

            if (config.Element(Manager.Style.XmlName) is not XElement styleConfig || !Manager.Style.FromXml(styleConfig, map, false, false, out FillerStyle style))
                return false;

            var vertixes = config.Elements(FillerVertex.XmlName).Select(e => FillerVertex.FromXml(e, markup, map, out IFillerVertex vertex) ? vertex : null).ToArray();
            if (vertixes.Any(v => v == null))
                return false;

            var contour = new FillerContour(markup, vertixes);

            if (contour.IsEmpty)
                return false;

            filler = new MarkingFiller(contour, style);
            return true;

        }

        public void Render(OverlayData data)
        {
            Contour.Render(data);
            Style.Value.Render(this, data);
        }

        public override string ToString() => Id.ToString();
    }
    public class FillerLinePart : MarkingLinePart
    {
        public override string XmlSection => throw new NotImplementedException();
        public new IFillerVertex From
        {
            get => base.From.Value as IFillerVertex;
            set => base.From.Value = value;
        }
        public new IFillerVertex To
        {
            get => base.To.Value as IFillerVertex;
            set => base.To.Value = value;
        }
        public bool IsPoint { get; } = false;
        public bool IsMedian { get; } = false;
        public FillerLinePart(MarkingLine line, IFillerVertex from, IFillerVertex to) : base(line, from, to)
        {
            if (from is EnterFillerVertexBase first && to is EnterFillerVertexBase second)
            {
                IsPoint = first.Point == second.Point;
                IsMedian = first.Enter == second.Enter;
            }
        }
    }
}

