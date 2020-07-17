using ColossalFramework.Math;
using ColossalFramework.PlatformServices;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    abstract class StyleData
    {
        protected Action DataChanged { get;}
        public StyleData(Action dataChanged)
        {
            DataChanged = dataChanged;
        }
    }

    class DashedData : StyleData
    {
        float _dashLength;
        float _spaceLength;
        public float DashLength
        {
            get => _dashLength;
            set
            {
                _dashLength = value;
                DataChanged();
            }
        }
        public float SpaceLength
        {
            get => _spaceLength;
            set
            {
                _spaceLength = value;
                DataChanged();
            }
        }
        public DashedData(Action dataChanged, float dashLength, float spaceLength) : base(dataChanged)
        {
            DashLength = dashLength;
            SpaceLength = spaceLength;
        }
    }

    public interface IDashedLine
    {
        float DashLength { get; set; }
        float SpaceLength { get; set; }
    }
    public interface IDoubleLine
    {
        float Offset { get; set; }
    }
    public interface IAsymLine
    {
        bool Invert { get; set; }
    }

    public abstract class LineStyle : IToXml
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
        public static StopLineStyle DefaultStop => new StopLineStyle(DefaultColor, DefaultStopWidth);

        public static LineStyle GetDefault(LineType type)
        {
            switch (type)
            {
                case LineType.Solid: return DefaultSolid;
                case LineType.Dashed: return DefaultDashed;
                case LineType.DoubleSolid: return DefaultDoubleSolid;
                case LineType.DoubleDashed: return DefaultDoubleDashed;
                case LineType.SolidAndDashed: return DefaultSolidAndDashed;
                case LineType.Stop: return DefaultStop;
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
                case LineType.Stop: return Localize.LineStyle_StopShort;
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

        public LineStyle(Color32 color, float width)
        {
            Color = color;
            Width = width;
        }

        public abstract IEnumerable<MarkupDash> Calculate(Bezier3 trajectory);
        public abstract LineStyle Copy();
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

        public static bool FromXml(XElement config, out LineStyle style)
        {
            var type = (LineType)config.GetAttrValue<int>("T");

            if (TemplateManager.GetDefault(type) is LineStyle defaultStyle)
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
            [SpecialLine]
            Stop,
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

    public class SolidLineStyle : LineStyle
    {
        public override LineType Type { get; } = LineType.Solid;

        public SolidLineStyle(Color color, float width) : base(color, width) { }

        public override IEnumerable<MarkupDash> Calculate(Bezier3 trajectory) => CalculateSolid(trajectory, 0, CalculateDashes);
        protected virtual IEnumerable<MarkupDash> CalculateDashes(Bezier3 trajectory)
        {
            yield return CalculateSolidDash(trajectory, 0f);
        }

        public override LineStyle Copy() => new SolidLineStyle(Color, Width);
    }
    public class DoubleSolidLineStyle : SolidLineStyle, IDoubleLine
    {
        public override LineType Type { get; } = LineType.DoubleSolid;

        float _offset;
        public float Offset
        {
            get => _offset;
            set
            {
                _offset = value;
                StyleChanged();
            }
        }

        public DoubleSolidLineStyle(Color color, float width, float offset) : base(color, width)
        {
            Offset = offset;
        }

        protected override IEnumerable<MarkupDash> CalculateDashes(Bezier3 trajectory)
        {
            yield return CalculateSolidDash(trajectory, Offset);
            yield return CalculateSolidDash(trajectory, -Offset);
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute("O", Offset));
            return config;
        }
        public override void FromXml(XElement config)
        {
            base.FromXml(config);
            Offset = config.GetAttrValue("O", DefaultOffser);
        }
        public override LineStyle Copy() => new DoubleSolidLineStyle(Color, Width, Offset);
    }
    public class DashedLineStyle : LineStyle, IDashedLine
    {
        public override LineType Type { get; } = LineType.Dashed;

        float _dashLength;
        float _spaceLength;
        public float DashLength
        {
            get => _dashLength;
            set
            {
                _dashLength = value;
                StyleChanged();
            }
        }
        public float SpaceLength
        {
            get => _spaceLength;
            set
            {
                _spaceLength = value;
                StyleChanged();
            }
        }

        public DashedLineStyle(Color color, float width, float dashLength, float spaceLength) : base(color, width)
        {
            DashLength = dashLength;
            SpaceLength = spaceLength;
        }

        public override IEnumerable<MarkupDash> Calculate(Bezier3 trajectory) => CalculateDashed(trajectory, DashLength, SpaceLength, CalculateDashes);

        protected virtual IEnumerable<MarkupDash> CalculateDashes(Bezier3 trajectory, float startT, float endT)
        {
            yield return CalculateDashedDash(trajectory, startT, endT, DashLength, 0);
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute("DL", DashLength));
            config.Add(new XAttribute("SL", SpaceLength));
            return config;
        }
        public override void FromXml(XElement config)
        {
            base.FromXml(config);
            DashLength = config.GetAttrValue("DL", DefaultDashLength);
            SpaceLength = config.GetAttrValue("SL", DefaultSpaceLength);
        }

        public override LineStyle Copy() => new DashedLineStyle(Color, Width, DashLength, SpaceLength);
    }
    public class DoubleDashedLineStyle : DashedLineStyle, IDoubleLine
    {
        public override LineType Type { get; } = LineType.DoubleDashed;

        float _offset;
        public float Offset
        {
            get => _offset;
            set
            {
                _offset = value;
                StyleChanged();
            }
        }

        public DoubleDashedLineStyle(Color color, float width, float dashLength, float spaceLength, float offset) : base(color, width, dashLength, spaceLength)
        {
            Offset = offset;
        }

        protected override IEnumerable<MarkupDash> CalculateDashes(Bezier3 trajectory, float startT, float endT)
        {
            yield return CalculateDashedDash(trajectory, startT, endT, DashLength, Offset);
            yield return CalculateDashedDash(trajectory, startT, endT, DashLength, -Offset);
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute("O", Offset));
            return config;
        }
        public override void FromXml(XElement config)
        {
            base.FromXml(config);
            Offset = config.GetAttrValue("O", DefaultOffser);
        }
        public override LineStyle Copy() => new DoubleDashedLineStyle(Color, Width, DashLength, SpaceLength, Offset);
    }
    public class SolidAndDashedLineStyle : LineStyle, IDoubleLine, IDashedLine, IAsymLine
    {
        public override LineType Type => LineType.SolidAndDashed;

        float _offset;
        float _dashLength;
        float _spaceLength;
        bool _invert;
        public float Offset
        {
            get => _offset;
            set
            {
                _offset = value;
                StyleChanged();
            }
        }
        public float DashLength
        {
            get => _dashLength;
            set
            {
                _dashLength = value;
                StyleChanged();
            }
        }
        public float SpaceLength
        {
            get => _spaceLength;
            set
            {
                _spaceLength = value;
                StyleChanged();
            }
        }
        public bool Invert
        {
            get => _invert;
            set
            {
                _invert = value;
                StyleChanged();
            }
        }

        public SolidAndDashedLineStyle(Color color, float width, float dashLength, float spaceLength, float offset, bool invert) : base(color, width)
        {
            Offset = offset;
            DashLength = dashLength;
            SpaceLength = spaceLength;
            Invert = invert;
        }


        public override IEnumerable<MarkupDash> Calculate(Bezier3 trajectory)
        {
            foreach (var dash in CalculateSolid(trajectory, 0, CalculateSolidDash))
            {
                yield return dash;
            }
            foreach (var dash in CalculateDashed(trajectory, DashLength, SpaceLength, CalculateDashedDash))
            {
                yield return dash;
            }
        }

        protected IEnumerable<MarkupDash> CalculateSolidDash(Bezier3 trajectory)
        {
            yield return CalculateSolidDash(trajectory, Invert ? Offset : -Offset);
        }
        protected IEnumerable<MarkupDash> CalculateDashedDash(Bezier3 trajectory, float startT, float endT)
        {
            yield return CalculateDashedDash(trajectory, startT, endT, DashLength, Invert ? -Offset : Offset);
        }

        public override LineStyle Copy() => new SolidAndDashedLineStyle(Color, Width, DashLength, SpaceLength, Offset, Invert);
        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute("O", Offset));
            config.Add(new XAttribute("DL", DashLength));
            config.Add(new XAttribute("SL", SpaceLength));
            config.Add(new XAttribute("I", Invert ? 1 : 0));
            return config;
        }
        public override void FromXml(XElement config)
        {
            base.FromXml(config);
            Offset = config.GetAttrValue("O", DefaultOffser);
            DashLength = config.GetAttrValue("DL", DefaultDashLength);
            SpaceLength = config.GetAttrValue("SL", DefaultSpaceLength);
            Invert = config.GetAttrValue("I", 0) == 1;
        }
    }
    public class StopLineStyle : LineStyle
    {
        public override LineType Type => LineType.Stop;

        public StopLineStyle(Color32 color, float width) : base(color, width) { }

        public override IEnumerable<MarkupDash> Calculate(Bezier3 trajectory)
        {
            var dash = CalculateSolidDash(trajectory, 0f);
            dash.Position += (trajectory.a - trajectory.b).normalized * (Width / 2);
            yield return dash;
        }

        public override LineStyle Copy() => new StopLineStyle(Color, Width);
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
        LineStyle _style;

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
        public LineStyle Style
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
        public Action<LineStyleTemplate, LineStyle> OnStyleChanged { private get; set; }
        public Func<LineStyleTemplate, string, bool> OnNameChanged { private get; set; }

        public string XmlSection => XmlName;

        public LineStyleTemplate(string name, LineStyle style)
        {
            _name = name;
            _style = style.Copy();
            Style.OnStyleChanged = TemplateChanged;
        }
        private void TemplateChanged() => OnTemplateChanged?.Invoke();

        public override string ToString() => IsEmpty ? Name : $"{LineStyle.GetShortName(Style.Type)}-{Name}";

        public static bool FromXml(XElement config, out LineStyleTemplate template)
        {
            var name = config.GetAttrValue<string>("N");
            if (!string.IsNullOrEmpty(name) && config.Element(LineStyle.XmlName) is XElement styleConfig && LineStyle.FromXml(styleConfig, out LineStyle style))
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
