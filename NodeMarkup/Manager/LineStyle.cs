using ColossalFramework.Math;
using ColossalFramework.PlatformServices;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public interface IDashedLine
    {
        float DashLength { get; set; }
        float SpaceLength { get; set; }
    }
    public interface IDoubleLine
    {
        float Offset { get; set; }
    }

    public abstract class LineStyle : IToXml
    {
        public static string XmlName { get; } = "S";

        public static Color DefaultColor { get; } = new Color(0.1f, 0.1f, 0.1f, 0.5f);
        public static float DefaultDashLength { get; } = 1.5f;
        public static float DefaultSpaceLength { get; } = 1.5f;
        public static float DefaultOffser { get; } = 0.15f;

        public static float AngleDelta { get; } = 5f;
        public static float MaxLength { get; } = 10f;
        public static float MinLength { get; } = 1f;

        public static SolidLineStyle DefaultSolid => new SolidLineStyle(DefaultColor);
        public static DashedLineStyle DefaultDashed => new DashedLineStyle(DefaultColor, DefaultDashLength, DefaultSpaceLength);
        public static DoubleSolidLineStyle DefaultDoubleSolid => new DoubleSolidLineStyle(DefaultColor, DefaultOffser);
        public static DoubleDashedStyle DefaultDoubleDashed => new DoubleDashedStyle(DefaultColor, DefaultDashLength, DefaultSpaceLength, DefaultOffser);

        public static LineStyle GetDefault(LineType type)
        {
            switch (type)
            {
                case LineType.Solid: return DefaultSolid;
                case LineType.Dash: return DefaultDashed;
                case LineType.DoubleSolid: return DefaultDoubleSolid;
                case LineType.DoubleDash: return DefaultDoubleDashed;
                default: return null;
            }
        }


        Color _color;

        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                StyleChanged();
            }
        }
        public Action OnStyleChanged { private get; set; }
        public abstract LineType Type { get; }
        public string XmlSection => XmlName;

        public LineStyle(Color color)
        {
            Color = color;
        }

        public abstract IEnumerable<MarkupDash> Calculate(Bezier3 trajectory);
        protected void StyleChanged() => OnStyleChanged?.Invoke();
        public virtual XElement ToXml()
        {
            var config = new XElement(XmlSection,
                new XAttribute("T", (int)Type),
                new XElement("C",
                    new XAttribute("R", Color.r.ToString("0.00")),
                    new XAttribute("G", Color.g.ToString("0.00")),
                    new XAttribute("B", Color.b.ToString("0.00")),
                    new XAttribute("A", Color.a.ToString("0.00"))
            )
            );
            return config;
        }

        public static bool FromXml(XElement config, out LineStyle style)
        {
            var type = (LineType)config.GetAttrValue<int>("T");

            if(GetDefault(type) is LineStyle defaultStyle)
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
            if (config.Element("C") is XElement colorConfig)
            {
                Color = new Color
                {
                    r = colorConfig.GetAttrValue<float>("R"),
                    g = colorConfig.GetAttrValue<float>("G"),
                    b = colorConfig.GetAttrValue<float>("B"),
                    a = colorConfig.GetAttrValue<float>("A")
                };
            }
            else
                Color = DefaultColor;
        }

        public enum LineType
        {
            Solid,
            Dash,
            DoubleSolid,
            DoubleDash
        }
    }
    public class SolidLineStyle : LineStyle
    {
        public override LineType Type { get; } = LineType.Solid;

        public SolidLineStyle(Color color) : base(color) { }

        public override IEnumerable<MarkupDash> Calculate(Bezier3 trajectory)
        {
            var deltaAngle = trajectory.DeltaAngle();
            var direction = trajectory.d - trajectory.a;
            var length = direction.magnitude;

            if ((deltaAngle > AngleDelta || length > MaxLength) && length >= MinLength)
            {
                trajectory.Divide(out Bezier3 first, out Bezier3 second);
                foreach (var dash in Calculate(first))
                {
                    yield return dash;
                }
                foreach (var dash in Calculate(second))
                {
                    yield return dash;
                }
            }
            else
            {
                foreach (var dash in CalculateDashes(trajectory, direction, length))
                {
                    yield return dash;
                }
            }
        }
        protected virtual IEnumerable<MarkupDash> CalculateDashes(Bezier3 trajectory, Vector3 direction, float length)
        {
            var position = (trajectory.d + trajectory.a) / 2;
            var angle = Mathf.Atan2(direction.z, direction.x);
            var dash = new MarkupDash(position, angle, length, Color);
            yield return dash;
        }
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

        public DoubleSolidLineStyle(Color color, float offset) : base(color)
        {
            Offset = offset;
        }

        protected override IEnumerable<MarkupDash> CalculateDashes(Bezier3 trajectory, Vector3 direction, float length)
        {
            var startDirection = (trajectory.b - trajectory.a).Turn90(true).normalized;
            var endDirection = (trajectory.d - trajectory.c).Turn90(true).normalized;

            yield return CalculateDash(trajectory, startDirection, endDirection, 1);
            yield return CalculateDash(trajectory, startDirection, endDirection, -1);
        }
        private MarkupDash CalculateDash(Bezier3 trajectory, Vector3 startDirection, Vector3 endDirection, float sign)
        {
            var startPosition = trajectory.a + sign * startDirection * Offset;
            var endPosition = trajectory.d + sign * endDirection * Offset;

            var position = (endPosition + startPosition) / 2;
            var direction = endPosition - startPosition;
            var angle = Mathf.Atan2(direction.z, direction.x);

            var dash = new MarkupDash(position, angle, direction.magnitude, Color);

            return dash;
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
    }
    public class DashedLineStyle : LineStyle, IDashedLine
    {
        public override LineType Type { get; } = LineType.Dash;

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

        public DashedLineStyle(Color color, float dashLength, float spaceLength) : base(color)
        {
            DashLength = dashLength;
            SpaceLength = spaceLength;
        }

        public override IEnumerable<MarkupDash> Calculate(Bezier3 trajectory)
        {
            var length = trajectory.Length();
            if (length == 0)
                yield break;

            var dashCount = (int)((length - SpaceLength) / (DashLength + SpaceLength));

            var startSpaceT = (1 - ((DashLength + SpaceLength) * dashCount - SpaceLength) / length) / 2;
            var dashT = DashLength / length;
            var spaceT = SpaceLength / length;

            int index = 0;
            while (true)
            {
                var startT = startSpaceT + (dashT + spaceT) * index;
                var endT = startT + dashT;

                if (endT >= 1)
                    break;

                foreach (var dash in CalculateDashes(trajectory, startT, endT))
                    yield return dash;

                index += 1;
            }
        }

        protected virtual IEnumerable<MarkupDash> CalculateDashes(Bezier3 trajectory, float startT, float endT)
        {
            var startPosition = trajectory.Position(startT);
            var endPosition = trajectory.Position(endT);

            var position = (startPosition + endPosition) / 2;
            var direction = trajectory.Tangent((startT + endT) / 2);

            var angle = Mathf.Atan2(direction.z, direction.x);

            var dash = new MarkupDash(position, angle, DashLength, Color);
            yield return dash;
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
    }
    public class DoubleDashedStyle : DashedLineStyle, IDoubleLine
    {
        public override LineType Type { get; } = LineType.DoubleDash;

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

        public DoubleDashedStyle(Color color, float dashLength, float spaceLength, float offset) : base(color, dashLength, spaceLength)
        {
            Offset = offset;
        }

        protected override IEnumerable<MarkupDash> CalculateDashes(Bezier3 trajectory, float startT, float endT)
        {
            var startPosition = trajectory.Position(startT);
            var endPosition = trajectory.Position(endT);

            var startDirection = trajectory.Tangent(startT).Turn90(true).normalized;
            var endDirection = trajectory.Tangent(endT).Turn90(true).normalized;

            yield return CalculateDash(startPosition, endPosition, startDirection, endDirection, 1);
            yield return CalculateDash(startPosition, endPosition, startDirection, endDirection, -1);
        }
        private MarkupDash CalculateDash(Vector3 startCentrePos, Vector3 endCentrePos, Vector3 startDirection, Vector3 endDirection, float sign)
        {
            var startPosition = startCentrePos + sign * startDirection * Offset;
            var endPosition = endCentrePos + sign * endDirection * Offset;

            var position = (startPosition + endPosition) / 2;
            var direction = (endPosition - startPosition);

            var angle = Mathf.Atan2(direction.z, direction.x);

            var dash = new MarkupDash(position, angle, DashLength, Color);
            return dash;
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
    }

    public class MarkupDash
    {
        public Vector3 Position { get; }
        public float Angle { get; }
        public float Length { get; }
        //public float Width { get; }
        public Color Color { get; }

        public MarkupDash(Vector3 position, float angle, float length, /*float width, */Color color)
        {
            Position = position;
            Angle = angle;
            Length = length;
            //Width = width;
            Color = color;
        }
    }
}
