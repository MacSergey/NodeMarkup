using ColossalFramework.Math;
using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.UI;
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
        PropertyValue<float> DashLength { get; }
        PropertyValue<float> SpaceLength { get; }
    }
    public interface IDoubleLine
    {
        PropertyValue<float> Offset { get; }
    }
    public interface IDoubleAlignmentLine : IDoubleLine
    {
        PropertyEnumValue<LineStyle.StyleAlignment> Alignment { get; }
    }
    public interface IAsymLine
    {
        PropertyBoolValue Invert { get; }
    }
    public interface ISharkLine
    {
        PropertyValue<float> Base { get; }
        PropertyValue<float> Height { get; }
        PropertyValue<float> Space { get; }
    }
    public interface IParallel
    {
        PropertyBoolValue Parallel { get; }
    }
    public interface IDoubleCrosswalk
    {
        PropertyValue<float> Offset { get; }
    }
    public interface ILinedCrosswalk
    {
        PropertyValue<float> LineWidth { get; }
    }
    public interface IDashedCrosswalk
    {
        PropertyValue<float> DashLength { get; }
        PropertyValue<float> SpaceLength { get; }
    }

    public abstract class LineStyle : Style
    {
        public static float DefaultDashLength => 1.5f;
        public static float DefaultSpaceLength => 1.5f;
        public static float DefaultOffset => 0.15f;

        public static float DefaultSharkBaseLength => 0.5f;
        public static float DefaultSharkSpaceLength => 0.5f;
        public static float DefaultSharkHeight => 0.6f;

        public static float Default3DWidth => 0.3f;
        public static float Default3DHeigth => 0.3f;

        public LineStyle(Color32 color, float width) : base(color, width) { }

        public abstract IStyleData Calculate(MarkupLine line, ILineTrajectory trajectory, int lod);
        public override Style Copy() => CopyLineStyle();
        public abstract LineStyle CopyLineStyle();

        protected virtual float LodLength => 0.5f;
        protected virtual float LodWidth => 0.15f;
        protected bool CheckDashedLod(int lod, float width, float length) => lod != 1 || width > LodWidth || length > LodLength;

        protected static FloatPropertyPanel AddOffsetProperty(IDoubleLine doubleStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var offsetProperty = ComponentPool.Get<FloatPropertyPanel>(parent);
            offsetProperty.Text = Localize.StyleOption_Offset;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.WheelTip = Editor.WheelTip;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = 0.05f;
            offsetProperty.Init();
            offsetProperty.Value = doubleStyle.Offset;
            offsetProperty.OnValueChanged += (float value) => doubleStyle.Offset.Value = value;
            AddOnHoverLeave(offsetProperty, onHover, onLeave);
            return offsetProperty;
        }
        protected static FloatPropertyPanel AddBaseProperty(ISharkLine sharkTeethStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var baseProperty = ComponentPool.Get<FloatPropertyPanel>(parent);
            baseProperty.Text = Localize.StyleOption_SharkToothBase;
            baseProperty.UseWheel = true;
            baseProperty.WheelStep = 0.1f;
            baseProperty.WheelTip = Editor.WheelTip;
            baseProperty.CheckMin = true;
            baseProperty.MinValue = 0.3f;
            baseProperty.Init();
            baseProperty.Value = sharkTeethStyle.Base;
            baseProperty.OnValueChanged += (float value) => sharkTeethStyle.Base.Value = value;
            AddOnHoverLeave(baseProperty, onHover, onLeave);
            return baseProperty;
        }
        protected static FloatPropertyPanel AddHeightProperty(ISharkLine sharkTeethStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var heightProperty = ComponentPool.Get<FloatPropertyPanel>(parent);
            heightProperty.Text = Localize.StyleOption_SharkToothHeight;
            heightProperty.UseWheel = true;
            heightProperty.WheelStep = 0.1f;
            heightProperty.WheelTip = Editor.WheelTip;
            heightProperty.CheckMin = true;
            heightProperty.MinValue = 0.3f;
            heightProperty.Init();
            heightProperty.Value = sharkTeethStyle.Height;
            heightProperty.OnValueChanged += (float value) => sharkTeethStyle.Height.Value = value;
            AddOnHoverLeave(heightProperty, onHover, onLeave);
            return heightProperty;
        }
        protected static FloatPropertyPanel AddSpaceProperty(ISharkLine sharkTeethStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var spaceProperty = ComponentPool.Get<FloatPropertyPanel>(parent);
            spaceProperty.Text = Localize.StyleOption_SharkToothSpace;
            spaceProperty.UseWheel = true;
            spaceProperty.WheelStep = 0.1f;
            spaceProperty.WheelTip = Editor.WheelTip;
            spaceProperty.CheckMin = true;
            spaceProperty.MinValue = 0.1f;
            spaceProperty.Init();
            spaceProperty.Value = sharkTeethStyle.Space;
            spaceProperty.OnValueChanged += (float value) => sharkTeethStyle.Space.Value = value;
            AddOnHoverLeave(spaceProperty, onHover, onLeave);
            return spaceProperty;
        }
        protected static LineAlignmentPropertyPanel AddAlignmentProperty(IDoubleAlignmentLine alignmentStyle, UIComponent parent)
        {
            var alignmentProperty = ComponentPool.Get<LineAlignmentPropertyPanel>(parent);
            alignmentProperty.Text = Localize.StyleOption_Alignment;
            alignmentProperty.Init();
            alignmentProperty.SelectedObject = alignmentStyle.Alignment;
            alignmentProperty.OnSelectObjectChanged += (value) => alignmentStyle.Alignment.Value = value;
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
            {RegularLineType.Pavement, new PavementLineStyle(Default3DWidth, Default3DHeigth) },
            //{RegularLineType.Grass, new GrassLineStyle(Default3DWidth, Default3DHeigth) },
        };
        public static LineStyle GetDefault(RegularLineType type) => Defaults.TryGetValue(type, out RegularLineStyle style) ? style.CopyRegularLineStyle() : null;

        public RegularLineStyle(Color32 color, float width) : base(color, width) { }

        public sealed override LineStyle CopyLineStyle() => CopyRegularLineStyle();
        public abstract RegularLineStyle CopyRegularLineStyle();

        public sealed override List<EditorItem> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            if (editObject is MarkupRegularLine line)
                GetUIComponents(line, components, parent, onHover, onLeave, isTemplate);
            else if(isTemplate)
                GetUIComponents(null, components, parent, onHover, onLeave, isTemplate);
            return components;
        }
        public virtual void GetUIComponents(MarkupRegularLine line, List<EditorItem> components, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false) { }

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

            [Description(nameof(Localize.LineStyle_Pavement))]
            Pavement = StyleType.LinePavement,

            //[Description(nameof(Localize.LineStyle_Grass))]
            //Grass = StyleType.LineGrass,

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

        public sealed override LineStyle CopyLineStyle() => CopyStopLineStyle();
        public abstract StopLineStyle CopyStopLineStyle();

        public sealed override IStyleData Calculate(MarkupLine line, ILineTrajectory trajectory, int lod) => line is MarkupStopLine stopLine ? Calculate(stopLine, trajectory, lod) : new MarkupStyleParts();
        protected abstract IStyleData Calculate(MarkupStopLine stopLine, ILineTrajectory trajectory, int lod);

        public sealed override List<EditorItem> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            if (editObject is MarkupStopLine line)
                GetUIComponents(line, components, parent, onHover, onLeave, isTemplate);
            else if (isTemplate)
                GetUIComponents(null, components, parent, onHover, onLeave, isTemplate);
            return components;
        }
        public virtual void GetUIComponents(MarkupStopLine line, List<EditorItem> components, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false) { }

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
