using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utilities;
using System.Collections.Generic;
using System.ComponentModel;
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
        PropertyEnumValue<Alignment> Alignment { get; }
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
    public interface I3DLine
    {
        PropertyValue<float> Elevation { get; }
    }

    public abstract class LineStyle : Style<LineStyle>
    {
        public LineStyle(Color32 color, float width) : base(color, width) { }

        public abstract IStyleData Calculate(MarkupLine line, ITrajectory trajectory, MarkupLOD lod);

        protected virtual float LodLength => 0.5f;
        protected virtual float LodWidth => 0.15f;
        public virtual bool CanOverlap => false;

        protected bool CheckDashedLod(MarkupLOD lod, float width, float length) => lod != MarkupLOD.LOD1 || width > LodWidth || length > LodLength;

        protected FloatPropertyPanel AddOffsetProperty(IDoubleLine doubleStyle, UIComponent parent)
        {
            var offsetProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(doubleStyle.Offset));
            offsetProperty.Text = Localize.StyleOption_Offset;
            offsetProperty.Format = Localize.NumberFormat_Meter;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.WheelTip = Settings.ShowToolTip;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = 0.05f;
            offsetProperty.Init();
            offsetProperty.Value = doubleStyle.Offset;
            offsetProperty.OnValueChanged += (float value) => doubleStyle.Offset.Value = value;

            return offsetProperty;
        }
        protected FloatPropertyPanel AddBaseProperty(ISharkLine sharkTeethStyle, UIComponent parent)
        {
            var baseProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(sharkTeethStyle.Base));
            baseProperty.Text = Localize.StyleOption_SharkToothBase;
            baseProperty.Format = Localize.NumberFormat_Meter;
            baseProperty.UseWheel = true;
            baseProperty.WheelStep = 0.1f;
            baseProperty.WheelTip = Settings.ShowToolTip;
            baseProperty.CheckMin = true;
            baseProperty.MinValue = 0.3f;
            baseProperty.Init();
            baseProperty.Value = sharkTeethStyle.Base;
            baseProperty.OnValueChanged += (float value) => sharkTeethStyle.Base.Value = value;

            return baseProperty;
        }
        protected FloatPropertyPanel AddHeightProperty(ISharkLine sharkTeethStyle, UIComponent parent)
        {
            var heightProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(sharkTeethStyle.Height));
            heightProperty.Text = Localize.StyleOption_SharkToothHeight;
            heightProperty.Format = Localize.NumberFormat_Meter;
            heightProperty.UseWheel = true;
            heightProperty.WheelStep = 0.1f;
            heightProperty.WheelTip = Settings.ShowToolTip;
            heightProperty.CheckMin = true;
            heightProperty.MinValue = 0.3f;
            heightProperty.Init();
            heightProperty.Value = sharkTeethStyle.Height;
            heightProperty.OnValueChanged += (float value) => sharkTeethStyle.Height.Value = value;

            return heightProperty;
        }
        protected FloatPropertyPanel AddSpaceProperty(ISharkLine sharkTeethStyle, UIComponent parent)
        {
            var spaceProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(sharkTeethStyle.Space));
            spaceProperty.Text = Localize.StyleOption_SharkToothSpace;
            spaceProperty.Format = Localize.NumberFormat_Meter;
            spaceProperty.UseWheel = true;
            spaceProperty.WheelStep = 0.1f;
            spaceProperty.WheelTip = Settings.ShowToolTip;
            spaceProperty.CheckMin = true;
            spaceProperty.MinValue = 0.1f;
            spaceProperty.Init();
            spaceProperty.Value = sharkTeethStyle.Space;
            spaceProperty.OnValueChanged += (float value) => sharkTeethStyle.Space.Value = value;

            return spaceProperty;
        }
        protected LineAlignmentPropertyPanel AddAlignmentProperty(IDoubleAlignmentLine alignmentStyle, UIComponent parent)
        {
            var alignmentProperty = ComponentPool.Get<LineAlignmentPropertyPanel>(parent, nameof(alignmentStyle.Alignment));
            alignmentProperty.Text = Localize.StyleOption_Alignment;
            alignmentProperty.Init();
            alignmentProperty.SelectedObject = alignmentStyle.Alignment;
            alignmentProperty.OnSelectObjectChanged += (value) => alignmentStyle.Alignment.Value = value;
            return alignmentProperty;
        }
        protected FloatPropertyPanel AddElevationProperty(I3DLine line3DStyle, UIComponent parent)
        {
            var elevationProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(line3DStyle.Elevation));
            elevationProperty.Text = Localize.LineStyle_Elevation;
            elevationProperty.Format = Localize.NumberFormat_Meter;
            elevationProperty.UseWheel = true;
            elevationProperty.WheelStep = 0.1f;
            elevationProperty.CheckMin = true;
            elevationProperty.MinValue = 0f;
            elevationProperty.CheckMax = true;
            elevationProperty.MaxValue = 1f;
            elevationProperty.Init();
            elevationProperty.Value = line3DStyle.Elevation;
            elevationProperty.OnValueChanged += (float value) => line3DStyle.Elevation.Value = value;

            return elevationProperty;
        }
    }
    public abstract class LineStyle<StyleType> : LineStyle
        where StyleType : LineStyle<StyleType>
    {
        public static float DefaultDoubleOffset => 0.15f;

        public static float DefaultSharkBaseLength => 0.5f;
        public static float DefaultSharkSpaceLength => 0.5f;
        public static float DefaultSharkHeight => 0.6f;
        public static float DefaultSharkAngle => 0.0f;

        public static float Default3DWidth => 0.3f;
        public static float Default3DHeigth => 0.3f;

        public static float DefaultObjectStep => 5f;
        public static float DefaultObjectAngle=> 0f;
        public static float DefaultObjectShift => 0f;
        public static float DefaultObjectScale => 1f;
        public static float DefaultObjectElevation => 0f;
        public static float DefaultObjectOffsetBefore => 0f;
        public static float DefaultObjectOffsetAfter => 0f;

        public LineStyle(Color32 color, float width) : base(color, width) { }

        public override LineStyle CopyStyle() => CopyLineStyle();
        public abstract StyleType CopyLineStyle();
    }

    public abstract class RegularLineStyle : LineStyle<RegularLineStyle>
    {
        public static Dictionary<RegularLineType, RegularLineStyle> Defaults { get; } = new Dictionary<RegularLineType, RegularLineStyle>()
        {
            {RegularLineType.Solid, new SolidLineStyle(DefaultColor, DefaultWidth)},
            {RegularLineType.Dashed, new DashedLineStyle(DefaultColor, DefaultWidth, DefaultDashLength, DefaultSpaceLength)},
            {RegularLineType.DoubleSolid, new DoubleSolidLineStyle(DefaultColor, DefaultWidth, DefaultDoubleOffset)},
            {RegularLineType.DoubleDashed, new DoubleDashedLineStyle(DefaultColor, DefaultWidth, DefaultDashLength, DefaultSpaceLength, DefaultDoubleOffset)},
            {RegularLineType.SolidAndDashed, new SolidAndDashedLineStyle(DefaultColor, DefaultWidth, DefaultDashLength, DefaultSpaceLength, DefaultDoubleOffset)},
            {RegularLineType.SharkTeeth, new SharkTeethLineStyle(DefaultColor, DefaultSharkBaseLength, DefaultSharkHeight, DefaultSharkSpaceLength, DefaultSharkAngle) },
            {RegularLineType.Pavement, new PavementLineStyle(Default3DWidth, Default3DHeigth) },
            {RegularLineType.Prop, new PropLineStyle(string.Empty, PropLineStyle.DefaultColorOption, PropLineStyle.DefaultColor, DefaultObjectStep, DefaultObjectAngle, DefaultObjectAngle, false, DefaultObjectShift, DefaultObjectScale, DefaultObjectScale, false, DefaultObjectElevation, DefaultObjectOffsetBefore, DefaultObjectOffsetAfter) },
            {RegularLineType.Tree, new TreeLineStyle(string.Empty, DefaultObjectStep, DefaultObjectAngle, DefaultObjectAngle, false, DefaultObjectShift, DefaultObjectScale, DefaultObjectScale, false, DefaultObjectElevation, DefaultObjectOffsetBefore, DefaultObjectOffsetAfter) },
        };

        public RegularLineStyle(Color32 color, float width) : base(color, width) { }

        public sealed override IStyleData Calculate(MarkupLine line, ITrajectory trajectory, MarkupLOD lod) => line is MarkupRegularLine regularLine ? Calculate(regularLine, trajectory, lod) : new MarkupStyleParts();
        protected abstract IStyleData Calculate(MarkupRegularLine line, ITrajectory trajectory, MarkupLOD lod);

        public sealed override List<EditorItem> GetUIComponents(object editObject, UIComponent parent, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, isTemplate);
            if (editObject is MarkupRegularLine line)
                GetUIComponents(line, components, parent, isTemplate);
            else if (isTemplate)
                GetUIComponents(null, components, parent, isTemplate);
            return components;
        }
        public virtual void GetUIComponents(MarkupRegularLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false) { }

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

            [Description(nameof(Localize.LineStyle_Prop))]
            Prop = StyleType.LineProp,

            [Description(nameof(Localize.LineStyle_Tree))]
            Tree = StyleType.LineTree,

            [Description(nameof(Localize.LineStyle_Empty))]
            [NotVisible]
            Empty = StyleType.EmptyLine,

            [Description(nameof(Localize.Style_FromClipboard))]
            [NotVisible]
            Buffer = StyleType.LineBuffer,
        }
    }
    public abstract class StopLineStyle : LineStyle<StopLineStyle>
    {
        public static float DefaultStopWidth { get; } = 0.3f;
        public static float DefaultStopOffset { get; } = 0.3f;

        public static Dictionary<StopLineType, StopLineStyle> Defaults { get; } = new Dictionary<StopLineType, StopLineStyle>()
        {
            {StopLineType.Solid, new SolidStopLineStyle(DefaultColor, DefaultStopWidth)},
            {StopLineType.Dashed, new DashedStopLineStyle(DefaultColor, DefaultStopWidth, DefaultDashLength, DefaultSpaceLength)},
            {StopLineType.DoubleSolid, new DoubleSolidStopLineStyle(DefaultColor, DefaultStopWidth, DefaultStopOffset)},
            {StopLineType.DoubleDashed, new DoubleDashedStopLineStyle(DefaultColor, DefaultStopWidth, DefaultDashLength, DefaultSpaceLength, DefaultStopOffset)},
            {StopLineType.SolidAndDashed, new SolidAndDashedStopLineStyle(DefaultColor, DefaultWidth, DefaultDashLength, DefaultSpaceLength, DefaultStopOffset)},
            {StopLineType.SharkTeeth, new SharkTeethStopLineStyle(DefaultColor, DefaultSharkBaseLength, DefaultSharkHeight, DefaultSharkSpaceLength) },
            {StopLineType.Pavement, new PavementStopLineStyle(Default3DWidth, Default3DHeigth) },
        };

        public StopLineStyle(Color32 color, float width) : base(color, width) { }

        public sealed override IStyleData Calculate(MarkupLine line, ITrajectory trajectory, MarkupLOD lod) => line is MarkupStopLine stopLine ? Calculate(stopLine, trajectory, lod) : new MarkupStyleParts();
        protected abstract IStyleData Calculate(MarkupStopLine stopLine, ITrajectory trajectory, MarkupLOD lod);

        public sealed override List<EditorItem> GetUIComponents(object editObject, UIComponent parent, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, isTemplate);
            if (editObject is MarkupStopLine line)
                GetUIComponents(line, components, parent, isTemplate);
            else if (isTemplate)
                GetUIComponents(null, components, parent, isTemplate);
            return components;
        }
        public virtual void GetUIComponents(MarkupStopLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false) { }

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

            [Description(nameof(Localize.LineStyle_StopPavement))]
            Pavement = StyleType.StopLinePavement,

            [Description(nameof(Localize.Style_FromClipboard))]
            [NotVisible]
            Buffer = StyleType.StopLineBuffer,
        }
    }
}
