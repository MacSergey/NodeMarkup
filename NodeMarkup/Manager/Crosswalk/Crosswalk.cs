using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace NodeMarkup.Manager
{
    public class MarkupCrosswalk : IToXml
    {
        #region PROPERTIES

        public static string XmlName { get; } = "C";
        public string XmlSection => XmlName;

        public Markup Markup { get; }
        public Action OnCrosswalkChanged { private get; set; }
        public MarkupCrosswalkLine CrosswalkLine { get; }

        MarkupRegularLine _rightBorder;
        MarkupRegularLine _leftBorder;
        CrosswalkStyle _style;

        public MarkupRegularLine RightBorder
        {
            get => _rightBorder;
            set
            {
                _rightBorder = value;
                CrosswalkChanged();
            }
        }
        public MarkupRegularLine LeftBorder
        {
            get => _leftBorder;
            set
            {
                _leftBorder = value;
                CrosswalkChanged();
            }
        }
        public CrosswalkStyle Style
        {
            get => _style;
            set
            {
                _style = value;
                _style.OnStyleChanged = CrosswalkChanged;
                CrosswalkChanged();
            }
        }



        #endregion

        public MarkupCrosswalk(Markup markup, MarkupCrosswalkLine crosswalkLine, CrosswalkStyle style, MarkupRegularLine rightBorder = null, MarkupRegularLine leftBorder = null)
        {
            Markup = markup;
            CrosswalkLine = crosswalkLine;
            Style = style;
            RightBorder = rightBorder;
            LeftBorder = leftBorder;
        }

        protected void CrosswalkChanged() => OnCrosswalkChanged?.Invoke();

        public MarkupRegularLine GetBorder(BorderPosition borderType) => borderType == BorderPosition.Right ? RightBorder : LeftBorder;

        #region XML

        public XElement ToXml()
        {
            var config = new XElement(XmlName);
            config.Add(new XAttribute(MarkupLine.XmlName, CrosswalkLine.PointPair.Hash));
            if (RightBorder != null)
                config.Add(new XAttribute("RB", RightBorder.PointPair.Hash));
            if (LeftBorder != null)
                config.Add(new XAttribute("LB", LeftBorder.PointPair.Hash));
            config.Add(Style.ToXml());
            return config;
        }
        public static bool FromXml(XElement config, MarkupCrosswalkLine line, Dictionary<ObjectId, ObjectId> map, out MarkupCrosswalk rule)
        {
            if (config.Element(Manager.Style.XmlName) is XElement styleConfig && Manager.Style.FromXml(styleConfig, out CrosswalkStyle style))
            {
                var rightBorder = GetBorder("RB", config, line);
                var leftBorder = GetBorder("LB", config, line);

                rule = new MarkupCrosswalk(line.Markup, line, style, rightBorder, leftBorder);
                return true;
            }
            else
            {
                rule = default;
                return false;
            }
        }
        public static MarkupRegularLine GetBorder(string key, XElement config, MarkupCrosswalkLine line)
        {
            if (config.GetAttrValue<string>(key) is string hashString && ulong.TryParse(hashString, out ulong hash) && line.Markup.TryGetLine(hash, out MarkupRegularLine border))
                return border;
            else
                return null;
        }

        #endregion
    }
}
