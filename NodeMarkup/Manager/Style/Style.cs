using ColossalFramework.Math;
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
    public abstract class BaseStyle : IToXml
    {
        public static string XmlName { get; } = "S";

        public static Color32 DefaultColor { get; } = new Color32(136, 136, 136, 224);
        public static float DefaultDashLength { get; } = 1.5f;
        public static float DefaultSpaceLength { get; } = 1.5f;
        public static float DefaultOffser { get; } = 0.15f;
        public static float DefaultWidth { get; } = 0.15f;
        public static float DefaultStopWidth { get; } = 0.3f;

        public static float AngleDelta { get; } = 5f;
        public static float MaxLength { get; } = 10f;
        public static float MinLength { get; } = 1f;

        public static SolidLineStyle DefaultSolid => new SolidLineStyle(DefaultColor, DefaultWidth);
        public static DashedLineStyle DefaultDashed => new DashedLineStyle(DefaultColor, DefaultWidth, DefaultDashLength, DefaultSpaceLength);
        public static DoubleSolidLineStyle DefaultDoubleSolid => new DoubleSolidLineStyle(DefaultColor, DefaultWidth, DefaultOffser);
        public static DoubleDashedLineStyle DefaultDoubleDashed => new DoubleDashedLineStyle(DefaultColor, DefaultWidth, DefaultDashLength, DefaultSpaceLength, DefaultOffser);
        public static SolidAndDashedLineStyle DefaultSolidAndDashed => new SolidAndDashedLineStyle(DefaultColor, DefaultWidth, DefaultDashLength, DefaultSpaceLength, DefaultOffser, false);
        public static SolidStopLineStyle DefaultSolidStop => new SolidStopLineStyle(DefaultColor, DefaultStopWidth);
        public static DashedStopLineStyle DefaultDashedStop => new DashedStopLineStyle(DefaultColor, DefaultStopWidth, DefaultDashLength, DefaultSpaceLength);

        public static BaseStyle GetDefault(LineType type)
        {
            switch (type)
            {
                case LineType.Solid: return DefaultSolid;
                case LineType.Dashed: return DefaultDashed;
                case LineType.DoubleSolid: return DefaultDoubleSolid;
                case LineType.DoubleDashed: return DefaultDoubleDashed;
                case LineType.SolidAndDashed: return DefaultSolidAndDashed;
                case LineType.StopSolid: return DefaultSolidStop;
                case LineType.StopDashed: return DefaultDashedStop;
                default: return null;
            }
        }
        public static string GetShortName(LineType type)
        {
            switch (type)
            {
                case LineType.Solid: return Localize.LineStyle_SolidShort;
                case LineType.Dashed: return Localize.LineStyle_DashedShort;
                case LineType.DoubleSolid: return Localize.LineStyle_DoubleSolidShort;
                case LineType.DoubleDashed: return Localize.LineStyle_DoubleDashedShort;
                case LineType.SolidAndDashed: return Localize.LineStyle_SolidAndDashedShort;
                case LineType.StopSolid: return Localize.LineStyle_StopShort;
                case LineType.StopDashed: return Localize.LineStyle_DashedStopShort;
                default: return null;
            }
        }


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

        public Action OnStyleChanged { private get; set; }
        public abstract LineType Type { get; }
        public string XmlSection => XmlName;

        public BaseStyle(Color32 color, float width)
        {
            Color = color;
            Width = width;
        }

        public abstract IEnumerable<MarkupDash> Calculate(Bezier3 trajectory);
        public abstract BaseStyle Copy();
        protected void StyleChanged() => OnStyleChanged?.Invoke();
        public virtual XElement ToXml()
        {
            var config = new XElement(XmlSection,
                new XAttribute("T", (int)Type),
                new XAttribute("C", Color.ToInt()),
                new XAttribute("W", Width)
            );
            return config;
        }

        public static bool FromXml(XElement config, out BaseStyle style)
        {
            var type = (LineType)config.GetAttrValue<int>("T");

            if (TemplateManager.GetDefault(type) is BaseStyle defaultStyle)
            {
                style = defaultStyle;
                style.FromXml(config);
                return true;
            }
            else
            {
                style = default;
                return false;
            }
        }

        public virtual void FromXml(XElement config)
        {
            var colorInt = config.GetAttrValue<int>("C");
            Color = colorInt != 0 ? colorInt.ToColor() : DefaultColor;
            Width = config.GetAttrValue("W", DefaultWidth);
        }

        public enum LineType
        {
            [Description("LineStyle_Solid")]
            Solid,

            [Description("LineStyle_Dashed")]
            Dashed,

            [Description("LineStyle_DoubleSolid")]
            DoubleSolid,

            [Description("LineStyle_DoubleDashed")]
            DoubleDashed,

            [Description("LineStyle_SolidAndDashed")]
            SolidAndDashed,

            [Description("LineStyle_Stop")]
            StopSolid,

            [Description("LineStyle_Stop")]
            StopDashed,
        }
        public enum SimpleLineType
        {
            [Description("LineStyle_Solid")]
            Solid = LineType.Solid,

            [Description("LineStyle_Dashed")]
            Dashed = LineType.Dashed,

            [Description("LineStyle_DoubleSolid")]
            DoubleSolid = LineType.DoubleSolid,

            [Description("LineStyle_DoubleDashed")]
            DoubleDashed = LineType.DoubleDashed,

            [Description("LineStyle_SolidAndDashed")]
            SolidAndDashed = LineType.SolidAndDashed,
        }
        public enum StopLineType
        {
            [Description("LineStyle_Stop")]
            Solid = LineType.StopSolid,

            [Description("LineStyle_Stop")]
            Dashed = LineType.StopDashed,
        }
        public class SpecialLineAttribute : Attribute { }

        protected IEnumerable<MarkupDash> CalculateSolid(Bezier3 trajectory, int depth, Func<Bezier3, IEnumerable<MarkupDash>> calculateDashes)
        {
            var deltaAngle = trajectory.DeltaAngle();
            var direction = trajectory.d - trajectory.a;
            var length = direction.magnitude;

            if (depth < 5 && (deltaAngle > AngleDelta || length > MaxLength) && length >= MinLength)
            {
                trajectory.Divide(out Bezier3 first, out Bezier3 second);
                foreach (var dash in CalculateSolid(first, depth + 1, calculateDashes))
                {
                    yield return dash;
                }
                foreach (var dash in CalculateSolid(second, depth + 1, calculateDashes))
                {
                    yield return dash;
                }
            }
            else
            {
                foreach (var dash in calculateDashes(trajectory))
                {
                    yield return dash;
                }
            }
        }
        protected IEnumerable<MarkupDash> CalculateDashed(Bezier3 trajectory, float dashLength, float spaceLength, Func<Bezier3, float, float, IEnumerable<MarkupDash>> calculateDashes)
        {
            var dashesT = new List<float[]>();

            var startSpace = spaceLength / 2;
            for (var i = 0; i < 3; i += 1)
            {
                dashesT.Clear();
                var isDash = false;

                var prevT = 0f;
                var currentT = 0f;
                var nextT = trajectory.Travel(currentT, startSpace);

                while (nextT < 1)
                {
                    if (isDash)
                        dashesT.Add(new float[] { currentT, nextT });

                    isDash = !isDash;

                    prevT = currentT;
                    currentT = nextT;
                    nextT = trajectory.Travel(currentT, isDash ? dashLength : spaceLength);
                }

                float endSpace;
                if (isDash || ((trajectory.Position(1) - trajectory.Position(currentT)).magnitude is float tempLength && tempLength < spaceLength / 2))
                    endSpace = (trajectory.Position(1) - trajectory.Position(prevT)).magnitude;
                else
                    endSpace = tempLength;

                startSpace = (startSpace + endSpace) / 2;

                if (Mathf.Abs(startSpace - endSpace) / (startSpace + endSpace) < 0.05)
                    break;
            }

            foreach (var dashT in dashesT)
            {
                foreach (var dash in calculateDashes(trajectory, dashT[0], dashT[1]))
                    yield return dash;
            }
        }

        protected MarkupDash CalculateDashedDash(Bezier3 trajectory, float startT, float endT, float dashLength, float offset)
        {
            var startPosition = trajectory.Position(startT);
            var endPosition = trajectory.Position(endT);

            if (offset != 0f)
            {
                var startDirection = trajectory.Tangent(startT).Turn90(true).normalized;
                var endDirection = trajectory.Tangent(endT).Turn90(true).normalized;

                startPosition += startDirection * offset;
                endPosition += endDirection * offset;
            }

            var position = (startPosition + endPosition) / 2;
            var direction = (endPosition - startPosition);

            var angle = Mathf.Atan2(direction.z, direction.x);

            var dash = new MarkupDash(position, angle, dashLength, Width, Color);
            return dash;
        }

        protected MarkupDash CalculateSolidDash(Bezier3 trajectory, float offset)
        {
            var startPosition = trajectory.a;
            var endPosition = trajectory.d;

            if (offset != 0f)
            {
                var startDirection = (trajectory.b - trajectory.a).Turn90(true).normalized;
                var endDirection = (trajectory.d - trajectory.c).Turn90(true).normalized;

                startPosition += startDirection * offset;
                endPosition += endDirection * offset;
            }

            var position = (endPosition + startPosition) / 2;
            var direction = endPosition - startPosition;
            var angle = Mathf.Atan2(direction.z, direction.x);

            var dash = new MarkupDash(position, angle, direction.magnitude, Width, Color);
            return dash;
        }
    }

    public class MarkupDash
    {
        public Vector3 Position { get; set; }
        public float Angle { get; set; }
        public float Length { get; set; }
        public float Width { get; set; }
        public Color Color { get; set; }

        public MarkupDash(Vector3 position, float angle, float length, float width, Color color)
        {
            Position = position;
            Angle = angle;
            Length = length;
            Width = width;
            Color = color;
        }
    }
    public class LineStyleTemplate : IToXml
    {
        public static string XmlName { get; } = "T";

        string _name;
        BaseStyle _style;

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
        public BaseStyle Style
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
        public Action<LineStyleTemplate, BaseStyle> OnStyleChanged { private get; set; }
        public Func<LineStyleTemplate, string, bool> OnNameChanged { private get; set; }

        public string XmlSection => XmlName;

        public LineStyleTemplate(string name, BaseStyle style)
        {
            _name = name;
            _style = style.Copy();
            Style.OnStyleChanged = TemplateChanged;
        }
        private void TemplateChanged() => OnTemplateChanged?.Invoke();

        public override string ToString() => IsEmpty ? Name : $"{BaseStyle.GetShortName(Style.Type)}-{Name}";

        public static bool FromXml(XElement config, out LineStyleTemplate template)
        {
            var name = config.GetAttrValue<string>("N");
            if (!string.IsNullOrEmpty(name) && config.Element(BaseStyle.XmlName) is XElement styleConfig && BaseStyle.FromXml(styleConfig, out BaseStyle style))
            {
                template = new LineStyleTemplate(name, style);
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
