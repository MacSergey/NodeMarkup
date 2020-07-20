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
    public interface ILineStyle { }
    public abstract class LineStyle : Style, ILineStyle
    {
        public static float DefaultDashLength { get; } = 1.5f;
        public static float DefaultSpaceLength { get; } = 1.5f;
        public static float DefaultOffser { get; } = 0.15f;
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

        public static LineStyle GetDefault(StyleType type)
        {
            switch (type)
            {
                case StyleType.LineSolid: return DefaultSolid;
                case StyleType.LineDashed: return DefaultDashed;
                case StyleType.LineDoubleSolid: return DefaultDoubleSolid;
                case StyleType.LineDoubleDashed: return DefaultDoubleDashed;
                case StyleType.LineSolidAndDashed: return DefaultSolidAndDashed;
                case StyleType.StopLineSolid: return DefaultSolidStop;
                case StyleType.StopLineDashed: return DefaultDashedStop;
                default: return null;
            }
        }
        public static string GetShortName(StyleType type)
        {
            switch (type)
            {
                case StyleType.LineSolid: return Localize.LineStyle_SolidShort;
                case StyleType.LineDashed: return Localize.LineStyle_DashedShort;
                case StyleType.LineDoubleSolid: return Localize.LineStyle_DoubleSolidShort;
                case StyleType.LineDoubleDashed: return Localize.LineStyle_DoubleDashedShort;
                case StyleType.LineSolidAndDashed: return Localize.LineStyle_SolidAndDashedShort;
                case StyleType.StopLineSolid: return Localize.LineStyle_StopShort;
                case StyleType.StopLineDashed: return Localize.LineStyle_DashedStopShort;
                default: return null;
            }
        }

        public LineStyle(Color32 color, float width) : base(color, width) { }

        public abstract IEnumerable<MarkupStyleDash> Calculate(Bezier3 trajectory);
        public abstract LineStyle Copy();
        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute("T", (int)Type));
            return config;
        }

        public static bool FromXml(XElement config, out LineStyle style)
        {
            var type = (StyleType)config.GetAttrValue<int>("T");

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


        public enum SimpleLineType
        {
            [Description("LineStyle_Solid")]
            Solid = StyleType.LineSolid,

            [Description("LineStyle_Dashed")]
            Dashed = StyleType.LineDashed,

            [Description("LineStyle_DoubleSolid")]
            DoubleSolid = StyleType.LineDoubleSolid,

            [Description("LineStyle_DoubleDashed")]
            DoubleDashed = StyleType.LineDoubleDashed,

            [Description("LineStyle_SolidAndDashed")]
            SolidAndDashed = StyleType.LineSolidAndDashed,
        }
        public enum StopLineType
        {
            [Description("LineStyle_Stop")]
            Solid = StyleType.StopLineSolid,

            [Description("LineStyle_Stop")]
            Dashed = StyleType.StopLineDashed,
        }
        public class SpecialLineAttribute : Attribute { }

        protected IEnumerable<MarkupStyleDash> CalculateSolid(Bezier3 trajectory, int depth, Func<Bezier3, IEnumerable<MarkupStyleDash>> calculateDashes)
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
        protected IEnumerable<MarkupStyleDash> CalculateDashed(Bezier3 trajectory, float dashLength, float spaceLength, Func<Bezier3, float, float, IEnumerable<MarkupStyleDash>> calculateDashes)
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

        protected MarkupStyleDash CalculateDashedDash(Bezier3 trajectory, float startT, float endT, float dashLength, float offset)
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

            var dash = new MarkupStyleDash(position, angle, dashLength, Width, Color);
            return dash;
        }
        protected MarkupStyleDash CalculateSolidDash(Bezier3 trajectory, float offset)
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

            var dash = new MarkupStyleDash(position, angle, direction.magnitude, Width, Color);
            return dash;
        }
    }
}
