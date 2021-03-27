using ModsCommon.Utilities;
using NodeMarkup.Utilities;
using System;
using System.Linq;
using System.Xml.Linq;

namespace NodeMarkup.Manager
{
    public class MarkupFiller : IStyleItem, IToXml
    {
        public static string XmlName { get; } = "F";

        public string DeleteCaptionDescription => Localize.FillerEditor_DeleteCaptionDescription;
        public string DeleteMessageDescription => Localize.FillerEditor_DeleteMessageDescription;

        public Markup Markup { get; }
        public FillerContour Contour { get; }

        public PropertyValue<FillerStyle> Style { get; }
        public LodDictionary<IStyleData> StyleData { get; } = new LodDictionary<IStyleData>();
        public bool IsMedian => Contour.IsMedian;

        public string XmlSection => XmlName;

        public MarkupFiller(FillerContour contour, FillerStyle style)
        {
            Contour = contour;
            Contour.IsComplite = true;
            Markup = Contour.Markup;
            style.OnStyleChanged = FillerChanged;
            Style = new PropertyClassValue<FillerStyle>(StyleChanged, style);
        }

        private void StyleChanged()
        {
            Style.Value.OnStyleChanged = FillerChanged;
            FillerChanged();
        }
        private void FillerChanged() => Markup?.Update(this, true);
        public bool ContainsLine(MarkupLine line) => Contour.Parts.Any(p => p.Line is not MarkupEnterLine && p.Line.PointPair == line.PointPair);
        public bool ContainsPoint(MarkupPoint point) => Contour.RawVertices.Any(s => s is EnterFillerVertex vertex && vertex.Point == point);

        public void Update(bool onlySelfUpdate = false) => Contour.Update();
        public void RecalculateStyleData()
        {
#if DEBUG_RECALCULATE
            Mod.Logger.Debug($"Recalculate filler {this}");
#endif
            foreach (var lod in EnumExtension.GetEnumValues<MarkupLOD>())
                RecalculateStyleData(lod);
        }
        public void RecalculateStyleData(MarkupLOD lod) => StyleData[lod] = Style.Value.Calculate(this, lod);

        public Dependences GetDependences() => new Dependences();

        public XElement ToXml()
        {
            var config = new XElement(XmlSection, Style.Value.ToXml());

            foreach (var supportPoint in Contour.RawVertices)
                config.Add(supportPoint.ToXml());

            return config;
        }
        public static bool FromXml(XElement config, Markup markup, ObjectsMap map, out MarkupFiller filler)
        {
            if (config.Element(Manager.Style.XmlName) is not XElement styleConfig || !Manager.Style.FromXml(styleConfig, map, false, out FillerStyle style))
            {
                filler = default;
                return false;
            }

            var contour = new FillerContour(markup);

            foreach (var supportConfig in config.Elements(FillerVertex.XmlName))
            {
                if (FillerVertex.FromXml(supportConfig, markup, map, out IFillerVertex vertex))
                    contour.Add(vertex);
                else
                {
                    filler = default;
                    return false;
                }
            }
            if (contour.First == null)
            {
                filler = default;
                return false;
            }

            contour.Add(contour.First);

            filler = new MarkupFiller(contour, style);
            return true;
        }

        public void Render(OverlayData data)
        {
            Contour.Render(data);
            Style.Value.Render(this, data);
        }

        public override string ToString() => Math.Abs(GetHashCode()).ToString();
    }
    public class FillerLinePart : MarkupLinePart
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
        public FillerLinePart(MarkupLine line, IFillerVertex from, IFillerVertex to) : base(line, from, to) { }
    }
}

