using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public class MarkupCrosswalk : IToXml
    {
        #region PROPERTIES

        public static string XmlName { get; } = "C";
        public string XmlSection => XmlName;

        public Markup Markup { get; }
        public MarkupCrosswalkLine Line { get; }

        public MarkupStyleDash[] Dashes { get; private set; } = new MarkupStyleDash[0];

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
        public float TotalWidth => Style.GetTotalWidth(this);



        #endregion
        public MarkupCrosswalk(Markup markup, MarkupCrosswalkLine crosswalkLine, CrosswalkStyle.CrosswalkType crosswalkType = CrosswalkStyle.CrosswalkType.Existent) :
            this(markup, crosswalkLine, TemplateManager.GetDefault<CrosswalkStyle>((Style.StyleType)(int)crosswalkType))
        { }
        public MarkupCrosswalk(Markup markup, MarkupCrosswalkLine line, CrosswalkStyle style, MarkupRegularLine rightBorder = null, MarkupRegularLine leftBorder = null)
        {
            Markup = markup;
            Line = line;
            Style = style;
            RightBorder = rightBorder;
            LeftBorder = leftBorder;
        }

        protected void CrosswalkChanged() => Markup.Update(this);

        public MarkupRegularLine GetBorder(BorderPosition borderType) => borderType == BorderPosition.Right ? RightBorder : LeftBorder;
        public void Update()
        {

        }
        public void RecalculateDashes() => Dashes = Style.Calculate(this, Line.Trajectory).ToArray();
        public void Render(RenderManager.CameraInfo cameraInfo, Color32 white)
        {

        }

        #region XML

        public XElement ToXml()
        {
            var config = new XElement(XmlName);
            config.Add(new XAttribute(MarkupLine.XmlName, Line.PointPair.Hash));
            if (RightBorder != null)
                config.Add(new XAttribute("RB", RightBorder.PointPair.Hash));
            if (LeftBorder != null)
                config.Add(new XAttribute("LB", LeftBorder.PointPair.Hash));
            config.Add(Style.ToXml());
            return config;
        }
        public void FromXml(XElement config, Dictionary<ObjectId, ObjectId> map)
        {
            _rightBorder = GetBorder("RB");
            _leftBorder = GetBorder("LB");
            if (config.Element(Manager.Style.XmlName) is XElement styleConfig && Manager.Style.FromXml(styleConfig, out CrosswalkStyle style))
            {
                _style = style;
                _style.OnStyleChanged = CrosswalkChanged;
            }

            MarkupRegularLine GetBorder(string key)
            {
                var lineId = config.GetAttrValue<ulong>(key);
                return Markup.TryGetLine(lineId, map, out MarkupRegularLine line) ? line : null;
            }
        }

        public static bool FromXml(XElement config, Markup markup, Dictionary<ObjectId, ObjectId> map, out MarkupCrosswalk crosswalk)
        {
            var lineId = config.GetAttrValue<ulong>(MarkupLine.XmlName);
            if (markup.TryGetLine(lineId, map, out MarkupCrosswalkLine line))
            {
                crosswalk = line.Crosswalk;
                crosswalk.FromXml(config, map);
                return true;
            }
            else
            {
                crosswalk = null;
                return false;
            }
        }

        #endregion
    }
}
