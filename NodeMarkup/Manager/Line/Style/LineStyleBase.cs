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
    public interface IRegularLine : ILineStyle { }
    public interface IStopLine : ILineStyle { }
    public interface ICrosswalkStyle : ILineStyle { }
    public interface IDashedLine : ILineStyle
    {
        float DashLength { get; set; }
        float SpaceLength { get; set; }
    }
    public interface IDoubleLine : ILineStyle
    {
        float Offset { get; set; }
    }
    public interface IAsymLine : ILineStyle
    {
        bool Invert { get; set; }
    }
    public interface IParallel : ILineStyle
    {
        bool Parallel { get; set; }
    }
    public interface IDoubleCrosswalk
    {
        float Offset { get; set; }
    }
    public interface ILinedCrosswalk
    {
        float LineWidth { get; set; }
    }
    public interface IDashedCrosswalk : IDashedLine { }

    public abstract class LineStyle : Style
    {
        public static float DefaultDashLength { get; } = 1.5f;
        public static float DefaultSpaceLength { get; } = 1.5f;
        public static float DefaultOffset { get; } = 0.15f;

        public static float AngleDelta { get; } = 5f;
        public static float MaxLength { get; } = 10f;
        public static float MinLength { get; } = 1f;

        public LineStyle(Color32 color, float width) : base(color, width) { }

        public abstract IEnumerable<MarkupStyleDash> Calculate(MarkupLine line, Bezier3 trajectory);
        public override Style Copy() => CopyLineStyle();
        public abstract LineStyle CopyLineStyle();

        protected static FloatPropertyPanel AddOffsetProperty(IDoubleLine doubleStyle, UIComponent parent, Action onHover, Action onLeave)
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
        protected static ButtonsPanel AddInvertProperty(IAsymLine asymStyle, UIComponent parent)
        {
            var buttonsPanel = parent.AddUIComponent<ButtonsPanel>();
            var invertIndex = buttonsPanel.AddButton(Localize.LineEditor_Invert);
            buttonsPanel.Init();
            buttonsPanel.OnButtonClick += OnButtonClick;

            void OnButtonClick(int index)
            {
                if (index == invertIndex)
                    asymStyle.Invert = !asymStyle.Invert;
            }

            return buttonsPanel;
        }

        protected IEnumerable<MarkupStyleDash> CalculateSolid(Bezier3 trajectory, int depth, Func<Bezier3, IEnumerable<MarkupStyleDash>> calculateDashes)
        {
            var deltaAngle = trajectory.DeltaAngle();
            var direction = trajectory.d - trajectory.a;
            var length = direction.magnitude;

            if (depth < 5 && ((deltaAngle > AngleDelta && length >= MinLength) || length > MaxLength || depth == 0))
            {
                trajectory.Divide(out Bezier3 first, out Bezier3 second);

                foreach (var dash in CalculateSolid(first, depth + 1, calculateDashes))
                    yield return dash;

                foreach (var dash in CalculateSolid(second, depth + 1, calculateDashes))
                    yield return dash;
            }
            else
            {
                foreach (var dash in calculateDashes(trajectory))
                    yield return dash;
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
            if (offset == 0)
                return CalculateDashedDash(trajectory, startT, endT, dashLength, Vector3.zero, Vector3.zero);
            else
            {
                var startOffset = trajectory.Tangent(startT).Turn90(true).normalized * offset;
                var endOffset = trajectory.Tangent(endT).Turn90(true).normalized * offset;
                return CalculateDashedDash(trajectory, startT, endT, dashLength, startOffset, endOffset);
            }
        }
        protected MarkupStyleDash CalculateDashedDash(Bezier3 trajectory, float startT, float endT, float dashLength, Vector3 startOffset, Vector3 endOffset, float? angle = null, float? width = null)
        {
            var startPosition = trajectory.Position(startT);
            var endPosition = trajectory.Position(endT);

            startPosition += startOffset;
            endPosition += endOffset;

            if (angle == null)
                return new MarkupStyleDash(startPosition, endPosition, endPosition - startPosition, dashLength, width ?? Width, Color);
            else
                return new MarkupStyleDash(startPosition, endPosition, angle.Value, dashLength, width ?? Width, Color);
        }

        protected MarkupStyleDash CalculateSolidDash(Bezier3 trajectory, float offset)
        {
            if (offset == 0)
                return CalculateSolidDash(trajectory, Vector3.zero, Vector3.zero);
            else
            {
                var startOffset = (trajectory.b - trajectory.a).Turn90(true).normalized * offset;
                var endOffset = (trajectory.d - trajectory.c).Turn90(true).normalized * offset;
                return CalculateSolidDash(trajectory, startOffset, endOffset);
            }
        }
        protected MarkupStyleDash CalculateSolidDash(Bezier3 trajectory, Vector3 startOffset, Vector3 endOffset, float? width = null)
        {
            var startPosition = trajectory.a;
            var endPosition = trajectory.d;

            startPosition += startOffset;
            endPosition += endOffset;

            return new MarkupStyleDash(startPosition, endPosition, endPosition - startPosition, width ?? Width, Color);
        }
    }

    public abstract class RegularLineStyle : LineStyle
    {
        static Dictionary<RegularLineType, RegularLineStyle> Defaults { get; } = new Dictionary<RegularLineType, RegularLineStyle>()
        {
            {RegularLineType.Solid, new SolidLineStyle(DefaultColor, DefaultWidth)},
            {RegularLineType.Dashed, new DashedLineStyle(DefaultColor, DefaultWidth, DefaultDashLength, DefaultSpaceLength)},
            {RegularLineType.DoubleSolid, new DoubleSolidLineStyle(DefaultColor, DefaultWidth, DefaultOffset)},
            {RegularLineType.DoubleDashed, new DoubleDashedLineStyle(DefaultColor, DefaultWidth, DefaultDashLength, DefaultSpaceLength, DefaultOffset)},
            {RegularLineType.SolidAndDashed, new SolidAndDashedLineStyle(DefaultColor, DefaultWidth, DefaultDashLength, DefaultSpaceLength, DefaultOffset, false, false)}
        };
        public static LineStyle GetDefault(RegularLineType type) => Defaults.TryGetValue(type, out RegularLineStyle style) ? style.CopyRegularLineStyle() : null;

        public RegularLineStyle(Color32 color, float width) : base(color, width) { }

        public override LineStyle CopyLineStyle() => CopyRegularLineStyle();
        public abstract RegularLineStyle CopyRegularLineStyle();

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
    }
    public abstract class StopLineStyle : LineStyle
    {
        public static float DefaultStopWidth { get; } = 0.3f;
        public static float DefaultStopOffset { get; } = 0.3f;

        static Dictionary<StopLineType, StopLineStyle> Defaults { get; } = new Dictionary<StopLineType, StopLineStyle>()
        {
            {StopLineType.Solid, new SolidStopLineStyle(DefaultColor, DefaultStopWidth)},
            {StopLineType.Dashed, new DashedStopLineStyle(DefaultColor, DefaultStopWidth, DefaultDashLength, DefaultSpaceLength)},
            {StopLineType.DoubleSolid, new DoubleSolidStopLineStyle(DefaultColor, DefaultStopWidth, DefaultStopOffset)},
            {StopLineType.DoubleDashed, new DoubleDashedStopLineStyle(DefaultColor, DefaultStopWidth, DefaultDashLength, DefaultSpaceLength, DefaultStopOffset)},
        };

        public static LineStyle GetDefault(StopLineType type) => Defaults.TryGetValue(type, out StopLineStyle style) ? style.CopyStopLineStyle() : null;

        public StopLineStyle(Color32 color, float width) : base(color, width) { }

        public override LineStyle CopyLineStyle() => CopyStopLineStyle();
        public abstract StopLineStyle CopyStopLineStyle();

        public override IEnumerable<MarkupStyleDash> Calculate(MarkupLine line, Bezier3 trajectory) => line is MarkupStopLine stopLine ? Calculate(stopLine, trajectory) : new MarkupStyleDash[0];
        protected abstract IEnumerable<MarkupStyleDash> Calculate(MarkupStopLine stopLine, Bezier3 trajectory);

        public enum StopLineType
        {
            [Description(nameof(Localize.LineStyle_Stop))]
            Solid = StyleType.StopLineSolid,

            [Description(nameof(Localize.LineStyle_Stop))]
            Dashed = StyleType.StopLineDashed,

            [Description(nameof(Localize.LineStyle_StopDouble))]
            DoubleSolid = StyleType.StopLineDoubleSolid,

            [Description(nameof(Localize.LineStyle_StopDoubleDashed))]
            DoubleDashed = StyleType.StopLineDoubleDashed,
        }
    }
    public abstract class CrosswalkStyle : LineStyle
    {
        public static float DefaultCrosswalkWidth { get; } = 2f;
        public static float DefaultCrosswalkDashLength { get; } = 0.4f;
        public static float DefaultCrosswalkSpaceLength { get; } = 0.6f;
        public static float DefaultCrosswalkOffset { get; } = 0.3f;

        static Dictionary<CrosswalkType, CrosswalkStyle> Defaults { get; } = new Dictionary<CrosswalkType, CrosswalkStyle>()
        {
            {CrosswalkType.Existent, new ExistCrosswalkStyle(DefaultCrosswalkWidth) },
            {CrosswalkType.Zebra, new ZebraCrosswalkStyle(DefaultColor, DefaultCrosswalkWidth, DefaultCrosswalkOffset, DefaultCrosswalkOffset, DefaultCrosswalkDashLength, DefaultCrosswalkSpaceLength, true) },
            {CrosswalkType.DoubleZebra, new DoubleZebraCrosswalkStyle(DefaultColor, DefaultCrosswalkWidth, DefaultCrosswalkOffset, DefaultCrosswalkOffset, DefaultCrosswalkDashLength, DefaultCrosswalkSpaceLength, true, DefaultCrosswalkOffset) },
            {CrosswalkType.ParallelLines, new ParallelLinesCrosswalkStyle(DefaultColor, DefaultCrosswalkWidth, DefaultCrosswalkOffset, DefaultCrosswalkOffset, DefaultWidth) },
            //{CrosswalkType.Ladder, new LadderCrosswalkStyle(DefaultColor, DefaultCrosswalkWidth, DefaultCrosswalkOffset, DefaultCrosswalkOffset, DefaultWidth, DefaultCrosswalkDashLength, DefaultCrosswalkSpaceLength) }
        };

        public static LineStyle GetDefault(CrosswalkType type) => Defaults.TryGetValue(type, out CrosswalkStyle style) ? style.CopyCrosswalkStyle() : null;

        public abstract float GetTotalWidth(MarkupCrosswalk crosswalk);

        public CrosswalkStyle(Color32 color, float width) : base(color, width) { }

        public override LineStyle CopyLineStyle() => CopyCrosswalkStyle();
        public abstract CrosswalkStyle CopyCrosswalkStyle();

        public override IEnumerable<MarkupStyleDash> Calculate(MarkupLine line, Bezier3 trajectory) => line is MarkupCrosswalk crosswalk ? Calculate(crosswalk, trajectory) : new MarkupStyleDash[0];
        protected abstract IEnumerable<MarkupStyleDash> Calculate(MarkupCrosswalk crosswalk, Bezier3 trajectory);

        public enum CrosswalkType
        {
            [Description(nameof(Localize.CrosswalkStyle_Existent))]
            Existent = StyleType.CrosswalkExistent,

            [Description(nameof(Localize.CrosswalkStyle_Zebra))]
            Zebra = StyleType.CrosswalkZebra,

            [Description(nameof(Localize.CrosswalkStyle_DoubleZebra))]
            DoubleZebra = StyleType.CrosswalkDoubleZebra,

            [Description(nameof(Localize.CrosswalkStyle_ParallelLines))]
            ParallelLines = StyleType.CrosswalkParallelLines,
        }
    }
}
