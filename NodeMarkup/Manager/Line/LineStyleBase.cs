using ColossalFramework.Math;
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
    public interface ILineStyle : IWidthStyle, IColorStyle { }
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

        public static LineStyle GetDefault(RegularLineType type)
        {
            switch (type)
            {
                case RegularLineType.Solid: return DefaultSolid;
                case RegularLineType.Dashed: return DefaultDashed;
                case RegularLineType.DoubleSolid: return DefaultDoubleSolid;
                case RegularLineType.DoubleDashed: return DefaultDoubleDashed;
                case RegularLineType.SolidAndDashed: return DefaultSolidAndDashed;
                default: return null;
            }
        }
        public static LineStyle GetDefault(StopLineType type)
        {
            switch (type)
            {
                case StopLineType.Solid: return DefaultSolidStop;
                case StopLineType.Dashed: return DefaultDashedStop;
                default: return null;
            }
        }

        public LineStyle(Color32 color, float width) : base(color, width) { }

        public abstract IEnumerable<MarkupStyleDash> Calculate(Bezier3 trajectory);
        public override Style Copy() => CopyLineStyle();
        public abstract LineStyle CopyLineStyle();
        protected static UIComponent AddDashLengthProperty(IDashedLine dashedStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var dashLengthProperty = parent.AddUIComponent<FloatPropertyPanel>();
            dashLengthProperty.Text = Localize.LineEditor_DashedLength;
            dashLengthProperty.UseWheel = true;
            dashLengthProperty.WheelStep = 0.1f;
            dashLengthProperty.CheckMin = true;
            dashLengthProperty.MinValue = 0.1f;
            dashLengthProperty.Init();
            dashLengthProperty.Value = dashedStyle.DashLength;
            dashLengthProperty.OnValueChanged += (float value) => dashedStyle.DashLength = value;
            AddOnHoverLeave(dashLengthProperty, onHover, onLeave);
            return dashLengthProperty;
        }
        protected static UIComponent AddSpaceLengthProperty(IDashedLine dashedStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var spaceLengthProperty = parent.AddUIComponent<FloatPropertyPanel>();
            spaceLengthProperty.Text = Localize.LineEditor_SpaceLength;
            spaceLengthProperty.UseWheel = true;
            spaceLengthProperty.WheelStep = 0.1f;
            spaceLengthProperty.CheckMin = true;
            spaceLengthProperty.MinValue = 0.1f;
            spaceLengthProperty.Init();
            spaceLengthProperty.Value = dashedStyle.SpaceLength;
            spaceLengthProperty.OnValueChanged += (float value) => dashedStyle.SpaceLength = value;
            AddOnHoverLeave(spaceLengthProperty, onHover, onLeave);
            return spaceLengthProperty;
        }
        protected static UIComponent AddOffsetProperty(IDoubleLine doubleStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var offsetProperty = parent.AddUIComponent<FloatPropertyPanel>();
            offsetProperty.Text = Localize.LineEditor_Offset;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = 0.05f;
            offsetProperty.Init();
            offsetProperty.Value = doubleStyle.Offset;
            offsetProperty.OnValueChanged += (float value) => doubleStyle.Offset = value;
            AddOnHoverLeave(offsetProperty, onHover, onLeave);
            return offsetProperty;
        }
        protected static UIComponent AddInvertProperty(IAsymLine asymStyle, UIComponent parent)
        {
            var invertProperty = parent.AddUIComponent<BoolPropertyPanel>();
            invertProperty.Text = Localize.LineEditor_Invert;
            invertProperty.Init();
            invertProperty.Value = asymStyle.Invert;
            invertProperty.OnValueChanged += (bool value) => asymStyle.Invert = value;
            return invertProperty;
        }

        public enum RegularLineType
        {
            [Description(nameof(Localize.LineStyle_Solid))]
            Solid = StyleType.LineSolid,

            [Description(nameof(Localize.LineStyle_Dashed))]
            Dashed = StyleType.LineDashed,

            [Description(nameof(Localize.LineStyle_DoubleSolid))]
            DoubleSolid = StyleType.LineDoubleSolid,

            [Description(nameof(Localize.LineStyle_DoubleDashed))]
            DoubleDashed = StyleType.LineDoubleDashed,

            [Description(nameof(Localize.LineStyle_SolidAndDashed))]
            SolidAndDashed = StyleType.LineSolidAndDashed,
        }
        public enum StopLineType
        {
            [Description(nameof(Localize.LineStyle_Stop))]
            Solid = StyleType.StopLineSolid,

            [Description(nameof(Localize.LineStyle_Stop))]
            Dashed = StyleType.StopLineDashed,
        }
        public class SpecialLineAttribute : Attribute { }

        protected IEnumerable<MarkupStyleDash> CalculateSolid(Bezier3 trajectory, int depth, Func<Bezier3, IEnumerable<MarkupStyleDash>> calculateDashes)
        {
            var deltaAngle = trajectory.DeltaAngle();
            var direction = trajectory.d - trajectory.a;
            var length = direction.magnitude;

            if (depth < 5 && (deltaAngle > AngleDelta || (length > MaxLength && depth != 0)) && length >= MinLength)
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
