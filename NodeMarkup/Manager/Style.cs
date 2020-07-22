using ColossalFramework.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public abstract class Style : IToXml
    {
        public static Color32 DefaultColor { get; } = new Color32(136, 136, 136, 224);
        public static float DefaultWidth { get; } = 0.15f;

        public static T GetDefault<T>(StyleType type) where T : Style
        {
            switch (type & StyleType.GroupMask)
            {
                case StyleType.RegularLine when LineStyle.GetDefault((LineStyle.RegularLineType)(int)type) is T tStyle:
                    return tStyle;
                case StyleType.StopLine when LineStyle.GetDefault((LineStyle.StopLineType)(int)type) is T tStyle:
                    return tStyle;
                case StyleType.Filler when FillerStyle.GetDefault((FillerStyle.FillerType)(int)type) is T tStyle:
                    return tStyle;
                default:
                    return null;
            }
        }

        public static string XmlName { get; } = "S";

        public Action OnStyleChanged { private get; set; }
        public string XmlSection => XmlName;
        public abstract StyleType Type { get; }

        protected void StyleChanged() => OnStyleChanged?.Invoke();

        Color32 _color;
        float _width;

        public Color32 Color
        {
            get => _color;
            set
            {
                _color = value;
                StyleChanged();
            }
        }
        public float Width
        {
            get => _width;
            set
            {
                _width = value;
                StyleChanged();
            }
        }
        public Style(Color32 color, float width)
        {
            Color = color;
            Width = width;
        }
        public virtual XElement ToXml()
        {
            var config = new XElement(XmlSection,
                new XAttribute("C", Color.ToInt()),
                new XAttribute("W", Width)
            );
            return config;
        }
        public virtual void FromXml(XElement config)
        {
            var colorInt = config.GetAttrValue<int>("C");
            Color = colorInt != 0 ? colorInt.ToColor() : DefaultColor;
            Width = config.GetAttrValue("W", DefaultWidth);
        }

        public abstract Style Copy();

        public virtual List<UIComponent> GetUIComponents(UIComponent parent, Action onHover = null, Action onLeave = null)
        {
            var components = new List<UIComponent>
            {
                AddColorProperty(parent),
                AddWidthProperty(parent, onHover, onLeave),
            };

            return components;
        }
        private UIComponent AddColorProperty(UIComponent parent)
        {
            var colorProperty = parent.AddUIComponent<ColorPropertyPanel>();
            colorProperty.Text = Localize.LineEditor_Color;
            colorProperty.Init();
            colorProperty.Value = Color;
            colorProperty.OnValueChanged += (Color32 color) => Color = color;
            return colorProperty;
        }
        private UIComponent AddWidthProperty(UIComponent parent, Action onHover, Action onLeave)
        {
            var widthProperty = parent.AddUIComponent<FloatPropertyPanel>();
            widthProperty.Text = Localize.LineEditor_Width;
            widthProperty.UseWheel = true;
            widthProperty.WheelStep = 0.01f;
            widthProperty.CheckMin = true;
            widthProperty.MinValue = 0.05f;
            widthProperty.Init();
            widthProperty.Value = Width;
            widthProperty.OnValueChanged += (float value) => Width = value;
            AddOnHoverLeave(widthProperty, onHover, onLeave);

            return widthProperty;
        }
        protected static void AddOnHoverLeave<T>(FieldPropertyPanel<T> fieldPanel, Action onHover, Action onLeave)
        {
            if (onHover != null)
                fieldPanel.OnHover += onHover;
            if (onLeave != null)
                fieldPanel.OnLeave += onLeave;
        }


        public enum StyleType
        {
            GroupMask = ~0xFF,


            RegularLine = 0x0,

            [Description("LineStyle_Solid")]
            LineSolid = 0 + RegularLine,

            [Description("LineStyle_Dashed")]
            LineDashed = 1 + RegularLine,

            [Description("LineStyle_DoubleSolid")]
            LineDoubleSolid = 2 + RegularLine,

            [Description("LineStyle_DoubleDashed")]
            LineDoubleDashed = 3 + RegularLine,

            [Description("LineStyle_SolidAndDashed")]
            LineSolidAndDashed = 4 + RegularLine,


            StopLine = 0x100,

            [Description("LineStyle_Stop")]
            StopLineSolid = 0 + StopLine,

            [Description("LineStyle_Stop")]
            StopLineDashed = 1 + StopLine,


            Filler = 0x200,

            [Description("FillerStyle_Stroke")]
            FillerStroke = 0 + Filler,
        }
    }

    public class MarkupStyleDash
    {
        public Vector3 Position { get; set; }
        public float Angle { get; set; }
        public float Length { get; set; }
        public float Width { get; set; }
        public Color Color { get; set; }

        public MarkupStyleDash(Vector3 position, float angle, float length, float width, Color color)
        {
            Position = position;
            Angle = angle;
            Length = length;
            Width = width;
            Color = color;
        }
    }
    public class StyleTemplate : IToXml
    {
        public static string XmlName { get; } = "T";

        string _name;
        Style _style;

        public string Name
        {
            get => _name;
            set
            {
                if (OnNameChanged?.Invoke(this, value) == true)
                {
                    _name = value;
                    TemplateChanged();
                }
            }
        }
        public Style Style
        {
            get => _style;
            set
            {
                OnStyleChanged?.Invoke(this, value);
                _style = value;
                TemplateChanged();
            }
        }
        public bool IsEmpty { get; set; } = false;

        public Action OnTemplateChanged { private get; set; }
        public Action<StyleTemplate, Style> OnStyleChanged { private get; set; }
        public Func<StyleTemplate, string, bool> OnNameChanged { private get; set; }

        public string XmlSection => XmlName;

        public StyleTemplate(string name, Style style)
        {
            _name = name;
            _style = style.Copy();
            Style.OnStyleChanged = TemplateChanged;
        }
        private void TemplateChanged() => OnTemplateChanged?.Invoke();

        public override string ToString() => IsEmpty ? Name : $"{LineStyle.GetShortName(Style.Type)}-{Name}";

        public static bool FromXml(XElement config, out StyleTemplate template)
        {
            var name = config.GetAttrValue<string>("N");
            if (!string.IsNullOrEmpty(name) && config.Element(Style.XmlName) is XElement styleConfig && LineStyle.FromXml(styleConfig, out LineStyle style))
            {
                template = new StyleTemplate(name, style);
                return true;
            }
            else
            {
                template = default;
                return false;
            }
        }

        public XElement ToXml()
        {
            var config = new XElement(XmlName,
                new XAttribute("N", Name),
                Style.ToXml()
                );
            return config;
        }
    }
}
