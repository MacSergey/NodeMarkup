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
    public interface IDashedLine
    {
        float DashLength { get; set; }
        float SpaceLength { get; set; }
    }
    public interface IDoubleLine
    {
        float Offset { get; set; }
    }
    public interface IDoubleAlignmentLine : IDoubleLine
    {
        LineStyle.StyleAlignment Alignment { get; set; }
    }
    public interface IAsymLine
    {
        bool Invert { get; set; }
    }
    public interface ISharkLIne
    {
        float Base { get; set; }
        float Height { get; set; }
        float Space { get; set; }
    }
    public interface IParallel
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
    public interface IDashedCrosswalk
    {
        float DashLength { get; set; }
        float SpaceLength { get; set; }
    }

    public abstract class LineStyle : Style
    {
        public static float DefaultDashLength { get; } = 1.5f;
        public static float DefaultSpaceLength { get; } = 1.5f;
        public static float DefaultOffset { get; } = 0.15f;

        public static float DefaultSharkBaseLength { get; } = 0.5f;
        public static float DefaultSharkSpaceLength { get; } = 0.5f;
        public static float DefaultSharkHeight { get; } = 0.6f;

        public LineStyle(Color32 color, float width) : base(color, width) { }

        public abstract IEnumerable<MarkupStyleDash> Calculate(MarkupLine line, ILineTrajectory trajectory);
        public override Style Copy() => CopyLineStyle();
        public abstract LineStyle CopyLineStyle();

        protected static FloatPropertyPanel AddOffsetProperty(IDoubleLine doubleStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var offsetProperty = parent.AddUIComponent<FloatPropertyPanel>();
            offsetProperty.Text = Localize.StyleOption_Offset;
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
        protected static FloatPropertyPanel AddBaseProperty(ISharkLIne sharkTeethStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var baseProperty = parent.AddUIComponent<FloatPropertyPanel>();
            baseProperty.Text = Localize.StyleOption_SharkToothBase;
            baseProperty.UseWheel = true;
            baseProperty.WheelStep = 0.1f;
            baseProperty.CheckMin = true;
            baseProperty.MinValue = 0.3f;
            baseProperty.Init();
            baseProperty.Value = sharkTeethStyle.Base;
            baseProperty.OnValueChanged += (float value) => sharkTeethStyle.Base = value;
            AddOnHoverLeave(baseProperty, onHover, onLeave);
            return baseProperty;
        }
        protected static FloatPropertyPanel AddHeightProperty(ISharkLIne sharkTeethStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var heightProperty = parent.AddUIComponent<FloatPropertyPanel>();
            heightProperty.Text = Localize.StyleOption_SharkToothHeight;
            heightProperty.UseWheel = true;
            heightProperty.WheelStep = 0.1f;
            heightProperty.CheckMin = true;
            heightProperty.MinValue = 0.3f;
            heightProperty.Init();
            heightProperty.Value = sharkTeethStyle.Height;
            heightProperty.OnValueChanged += (float value) => sharkTeethStyle.Height = value;
            AddOnHoverLeave(heightProperty, onHover, onLeave);
            return heightProperty;
        }
        protected static FloatPropertyPanel AddSpaceProperty(ISharkLIne sharkTeethStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var spaceProperty = parent.AddUIComponent<FloatPropertyPanel>();
            spaceProperty.Text = Localize.StyleOption_SharkToothSpace;
            spaceProperty.UseWheel = true;
            spaceProperty.WheelStep = 0.1f;
            spaceProperty.CheckMin = true;
            spaceProperty.MinValue = 0.1f;
            spaceProperty.Init();
            spaceProperty.Value = sharkTeethStyle.Space;
            spaceProperty.OnValueChanged += (float value) => sharkTeethStyle.Space = value;
            AddOnHoverLeave(spaceProperty, onHover, onLeave);
            return spaceProperty;
        }
        protected static LineAlignmentPropertyPanel AddAlignmentProperty(IDoubleAlignmentLine alignmentStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var alignmentProperty = parent.AddUIComponent<LineAlignmentPropertyPanel>();
            alignmentProperty.Text = Localize.StyleOption_Alignment;
            alignmentProperty.Init();
            alignmentProperty.SelectedObject = alignmentStyle.Alignment;
            alignmentProperty.OnSelectObjectChanged += (value) => alignmentStyle.Alignment = value;
            return alignmentProperty;
        }
        public enum StyleAlignment
        {
            [Description(nameof(Localize.StyleOption_AlignmentLeft))]
            Left,

            [Description(nameof(Localize.StyleOption_AlignmentCenter))]
            Centre,

            [Description(nameof(Localize.StyleOption_AlignmentRight))]
            Right
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
            {RegularLineType.SolidAndDashed, new SolidAndDashedLineStyle(DefaultColor, DefaultWidth, DefaultDashLength, DefaultSpaceLength, DefaultOffset)},
            {RegularLineType.SharkTeeth, new SharkTeethLineStyle(DefaultColor, DefaultSharkBaseLength, DefaultSharkHeight, DefaultSharkSpaceLength) },
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

            [Description(nameof(Localize.LineStyle_SharkTeeth))]
            SharkTeeth = StyleType.LineSharkTeeth,

            [Description(nameof(Localize.LineStyle_Empty))]
            [NotVisible]
            Empty = StyleType.EmptyLine
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
            {StopLineType.SolidAndDashed, new SolidAndDashedStopLineStyle(DefaultColor, DefaultWidth, DefaultDashLength, DefaultSpaceLength, DefaultStopOffset)},
            {StopLineType.SharkTeeth, new SharkTeethStopLineStyle(DefaultColor, DefaultSharkBaseLength, DefaultSharkHeight, DefaultSharkSpaceLength) },
        };

        public static LineStyle GetDefault(StopLineType type) => Defaults.TryGetValue(type, out StopLineStyle style) ? style.CopyStopLineStyle() : null;

        public StopLineStyle(Color32 color, float width) : base(color, width) { }

        public override LineStyle CopyLineStyle() => CopyStopLineStyle();
        public abstract StopLineStyle CopyStopLineStyle();

        public override IEnumerable<MarkupStyleDash> Calculate(MarkupLine line, ILineTrajectory trajectory) => line is MarkupStopLine stopLine ? Calculate(stopLine, trajectory) : new MarkupStyleDash[0];
        protected abstract IEnumerable<MarkupStyleDash> Calculate(MarkupStopLine stopLine, ILineTrajectory trajectory);

        public enum StopLineType
        {
            [Description(nameof(Localize.LineStyle_StopSolid))]
            Solid = StyleType.StopLineSolid,

            [Description(nameof(Localize.LineStyle_StopDashed))]
            Dashed = StyleType.StopLineDashed,

            [Description(nameof(Localize.LineStyle_StopDouble))]
            DoubleSolid = StyleType.StopLineDoubleSolid,

            [Description(nameof(Localize.LineStyle_StopDoubleDashed))]
            DoubleDashed = StyleType.StopLineDoubleDashed,

            [Description(nameof(Localize.LineStyle_StopSolidAndDashed))]
            SolidAndDashed = StyleType.StopLineSolidAndDashed,

            [Description(nameof(Localize.LineStyle_StopSharkTeeth))]
            SharkTeeth = StyleType.StopLineSharkTeeth,
        }
    }
}
