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
        public PropertyBoolValue ColorCount { get; }
        public PropertyColorValue SecondColor { get; }
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
        PropertyValue<float> OffsetBetween { get; }
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
        protected static string Triangle => string.Empty;

        public LineStyle(Color32 color, float width) : base(color, width) { }

        public abstract IStyleData Calculate(MarkupLine line, ITrajectory trajectory, MarkupLOD lod);

        protected virtual float LodLength => 0.5f;
        protected virtual float LodWidth => 0.15f;
        public virtual bool CanOverlap => false;

        protected bool CheckDashedLod(MarkupLOD lod, float width, float length) => lod != MarkupLOD.LOD1 || width > LodWidth || length > LodLength;

        protected FloatPropertyPanel AddOffsetProperty(IDoubleLine doubleStyle, UIComponent parent, bool canCollapse)
        {
            var offsetProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(doubleStyle.Offset));
            offsetProperty.Text = Localize.StyleOption_Offset;
            offsetProperty.Format = Localize.NumberFormat_Meter;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.WheelTip = Settings.ShowToolTip;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = 0.05f;
            offsetProperty.CanCollapse = canCollapse;
            offsetProperty.Init();
            offsetProperty.Value = doubleStyle.Offset;
            offsetProperty.OnValueChanged += (float value) => doubleStyle.Offset.Value = value;

            return offsetProperty;
        }
        protected Vector2PropertyPanel AddTriangleProperty(ISharkLine sharkTeethStyle, UIComponent parent, bool canCollapse)
        {
            var triangleProperty = ComponentPool.Get<Vector2PropertyPanel>(parent, nameof(Triangle));
            triangleProperty.Text = Localize.StyleOption_Triangle;
            triangleProperty.FieldsWidth = 50f;
            triangleProperty.SetLabels(Localize.StyleOption_SharkToothBaseAbrv, Localize.StyleOption_SharkToothHeightAbrv);
            triangleProperty.Format = Localize.NumberFormat_Meter;
            triangleProperty.UseWheel = true;
            triangleProperty.WheelStep = new Vector2(0.1f, 0.1f);
            triangleProperty.WheelTip = Settings.ShowToolTip;
            triangleProperty.CheckMin = true;
            triangleProperty.MinValue = new Vector2(0.3f, 0.3f);
            triangleProperty.CanCollapse = canCollapse;
            triangleProperty.Init(0, 1);
            triangleProperty.Value = new Vector2(sharkTeethStyle.Base, sharkTeethStyle.Height);
            triangleProperty.OnValueChanged += (Vector2 value) =>
            {
                sharkTeethStyle.Base.Value = value.x;
                sharkTeethStyle.Height.Value = value.y;
            };

            return triangleProperty;
        }
        protected FloatPropertyPanel AddBaseProperty(ISharkLine sharkTeethStyle, UIComponent parent, bool canCollapse)
        {
            var baseProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(sharkTeethStyle.Base));
            baseProperty.Text = Localize.StyleOption_SharkToothBase;
            baseProperty.Format = Localize.NumberFormat_Meter;
            baseProperty.UseWheel = true;
            baseProperty.WheelStep = 0.1f;
            baseProperty.WheelTip = Settings.ShowToolTip;
            baseProperty.CheckMin = true;
            baseProperty.MinValue = 0.3f;
            baseProperty.CanCollapse = canCollapse;
            baseProperty.Init();
            baseProperty.Value = sharkTeethStyle.Base;
            baseProperty.OnValueChanged += (float value) => sharkTeethStyle.Base.Value = value;

            return baseProperty;
        }
        protected FloatPropertyPanel AddHeightProperty(ISharkLine sharkTeethStyle, UIComponent parent, bool canCollapse)
        {
            var heightProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(sharkTeethStyle.Height));
            heightProperty.Text = Localize.StyleOption_SharkToothHeight;
            heightProperty.Format = Localize.NumberFormat_Meter;
            heightProperty.UseWheel = true;
            heightProperty.WheelStep = 0.1f;
            heightProperty.WheelTip = Settings.ShowToolTip;
            heightProperty.CheckMin = true;
            heightProperty.MinValue = 0.3f;
            heightProperty.CanCollapse = canCollapse;
            heightProperty.Init();
            heightProperty.Value = sharkTeethStyle.Height;
            heightProperty.OnValueChanged += (float value) => sharkTeethStyle.Height.Value = value;

            return heightProperty;
        }
        protected FloatPropertyPanel AddSpaceProperty(ISharkLine sharkTeethStyle, UIComponent parent, bool canCollapse)
        {
            var spaceProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(sharkTeethStyle.Space));
            spaceProperty.Text = Localize.StyleOption_SharkToothSpace;
            spaceProperty.Format = Localize.NumberFormat_Meter;
            spaceProperty.UseWheel = true;
            spaceProperty.WheelStep = 0.1f;
            spaceProperty.WheelTip = Settings.ShowToolTip;
            spaceProperty.CheckMin = true;
            spaceProperty.MinValue = 0.1f;
            spaceProperty.CanCollapse = canCollapse;
            spaceProperty.Init();
            spaceProperty.Value = sharkTeethStyle.Space;
            spaceProperty.OnValueChanged += (float value) => sharkTeethStyle.Space.Value = value;

            return spaceProperty;
        }
        protected LineAlignmentPropertyPanel AddAlignmentProperty(IDoubleAlignmentLine alignmentStyle, UIComponent parent, bool canCollapse)
        {
            var alignmentProperty = ComponentPool.Get<LineAlignmentPropertyPanel>(parent, nameof(alignmentStyle.Alignment));
            alignmentProperty.Text = Localize.StyleOption_Alignment;
            alignmentProperty.CanCollapse = canCollapse;
            alignmentProperty.Init();
            alignmentProperty.SelectedObject = alignmentStyle.Alignment;
            alignmentProperty.OnSelectObjectChanged += (value) => alignmentStyle.Alignment.Value = value;
            return alignmentProperty;
        }
        protected FloatPropertyPanel AddElevationProperty(I3DLine line3DStyle, UIComponent parent, bool canCollapse)
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
            elevationProperty.CanCollapse = canCollapse;
            elevationProperty.Init();
            elevationProperty.Value = line3DStyle.Elevation;
            elevationProperty.OnValueChanged += (float value) => line3DStyle.Elevation.Value = value;

            return elevationProperty;
        }

        protected BoolListPropertyPanel AddUseSecondColorProperty(IDoubleLine doubleLine, UIComponent parent, bool canCollapse)
        {
            var useSecondColorProperty = ComponentPool.Get<BoolListPropertyPanel>(parent, nameof(doubleLine.ColorCount));
            useSecondColorProperty.Text = Localize.StyleOption_ColorCount;
            useSecondColorProperty.CanCollapse = canCollapse;
            useSecondColorProperty.Init(Localize.StyleOption_ColorCountOne, Localize.StyleOption_ColorCountTwo, false);
            useSecondColorProperty.SelectedObject = doubleLine.ColorCount;
            useSecondColorProperty.OnSelectObjectChanged += (value) =>
                {
                    doubleLine.ColorCount.Value = value;
                    UseSecondColorChanged(doubleLine, parent, value);
                };

            return useSecondColorProperty;
        }
        protected void UseSecondColorChanged(IDoubleLine doubleLine, UIComponent parent, bool value)
        {
            if (parent.Find<ColorAdvancedPropertyPanel>(nameof(Color)) is ColorAdvancedPropertyPanel mainColorProperty)
            {
                mainColorProperty.Text = value ? Localize.StyleOption_MainColor : Localize.StyleOption_Color;
            }

            if (parent.Find<ColorAdvancedPropertyPanel>(nameof(doubleLine.SecondColor)) is ColorAdvancedPropertyPanel secondColorProperty)
            {
                secondColorProperty.IsHidden = !value;
                secondColorProperty.Text = value ? Localize.StyleOption_SecondColor : Localize.StyleOption_Color;
            }
        }

        protected ColorAdvancedPropertyPanel AddSecondColorProperty(IDoubleLine doubleLine, UIComponent parent, bool canCollapse)
        {
            var colorProperty = ComponentPool.Get<ColorAdvancedPropertyPanel>(parent, nameof(doubleLine.SecondColor));
            colorProperty.Text = Localize.StyleOption_Color;
            colorProperty.WheelTip = Settings.ShowToolTip;
            colorProperty.CanCollapse = canCollapse;
            colorProperty.Init((GetDefault() as IDoubleLine)?.SecondColor);
            colorProperty.Value = doubleLine.SecondColor;
            colorProperty.OnValueChanged += (Color32 color) => doubleLine.SecondColor.Value = color;

            return colorProperty;
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

        public static int DefaultObjectProbability => 100;
        public static float DefaultObjectStep => 5f;
        public static float DefaultObjectAngle => 0f;
        public static float DefaultObjectShift => 0f;
        public static float DefaultObjectScale => 1f;
        public static float DefaultObjectElevation => 0f;
        public static float DefaultObjectOffsetBefore => 0f;
        public static float DefaultObjectOffsetAfter => 0f;

        public static float DefaultNetworkScale => 1f;
        public static int DefaultRepeatDistance => 64;

        public static float DefaultTextScale => 5f;

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
            {RegularLineType.DoubleSolid, new DoubleSolidLineStyle(DefaultColor, DefaultColor, false, DefaultWidth, DefaultDoubleOffset)},
            {RegularLineType.DoubleDashed, new DoubleDashedLineStyle(DefaultColor, DefaultColor, false, DefaultWidth, DefaultDashLength, DefaultSpaceLength, DefaultDoubleOffset)},
            {RegularLineType.DoubleDashedAsym, new DoubleDashedAsymLineStyle(DefaultColor, DefaultColor, false, DefaultWidth, DefaultDashLength, DefaultSpaceLength, DefaultSpaceLength * 2f, DefaultDoubleOffset)},
            {RegularLineType.SolidAndDashed, new SolidAndDashedLineStyle(DefaultColor, DefaultColor, false, DefaultWidth, DefaultDashLength, DefaultSpaceLength, DefaultDoubleOffset)},
            {RegularLineType.SharkTeeth, new SharkTeethLineStyle(DefaultColor, DefaultSharkBaseLength, DefaultSharkHeight, DefaultSharkSpaceLength, DefaultSharkAngle) },
            {RegularLineType.Pavement, new PavementLineStyle(Default3DWidth, Default3DHeigth) },
            {RegularLineType.Prop, new PropLineStyle(null, DefaultObjectProbability, PropLineStyle.DefaultColorOption, PropLineStyle.DefaultColor, DefaultObjectStep, new Vector2(DefaultObjectAngle, DefaultObjectAngle), new Vector2(DefaultObjectAngle, DefaultObjectAngle), new Vector2(DefaultObjectAngle, DefaultObjectAngle), new Vector2(DefaultObjectShift,DefaultObjectShift), new Vector2(DefaultObjectScale, DefaultObjectScale), new Vector2(DefaultObjectElevation,DefaultObjectElevation), DefaultObjectOffsetBefore, DefaultObjectOffsetAfter) },
            {RegularLineType.Tree, new TreeLineStyle(null, DefaultObjectProbability, DefaultObjectStep, new Vector2(DefaultObjectAngle, DefaultObjectAngle), new Vector2(DefaultObjectAngle, DefaultObjectAngle), new Vector2(DefaultObjectAngle, DefaultObjectAngle), new Vector2(DefaultObjectShift,DefaultObjectShift), new Vector2(DefaultObjectScale, DefaultObjectScale), new Vector2(DefaultObjectElevation,DefaultObjectElevation), DefaultObjectOffsetBefore, DefaultObjectOffsetAfter) },
            {RegularLineType.Text, new RegularLineStyleText(DefaultColor, string.Empty, DefaultTextScale, DefaultObjectAngle, DefaultObjectShift, RegularLineStyleText.TextDirection.Horizontal, Vector2.zero)},
            {RegularLineType.Network, new NetworkLineStyle(null, DefaultObjectShift, DefaultObjectElevation, DefaultNetworkScale, DefaultObjectOffsetBefore, DefaultObjectOffsetAfter, DefaultRepeatDistance, false) },
        };

        public RegularLineStyle(Color32 color, float width) : base(color, width) { }

        public sealed override IStyleData Calculate(MarkupLine line, ITrajectory trajectory, MarkupLOD lod)
        {
            if ((SupportLOD & lod) != 0 && line is MarkupRegularLine regularLine)
                return CalculateImpl(regularLine, trajectory, lod);
            else
                return new MarkupPartGroupData(lod);
        }
        protected abstract IStyleData CalculateImpl(MarkupRegularLine line, ITrajectory trajectory, MarkupLOD lod);

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
            [NetworkType(NetworkType.Path | NetworkType.Road | NetworkType.Taxiway)]
            [Order(0)]
            Solid = StyleType.LineSolid,

            [Description(nameof(Localize.LineStyle_Dashed))]
            [NetworkType(NetworkType.Path | NetworkType.Road | NetworkType.Taxiway)]
            [Order(1)]
            Dashed = StyleType.LineDashed,

            [Description(nameof(Localize.LineStyle_DoubleSolid))]
            [NetworkType(NetworkType.Path | NetworkType.Road | NetworkType.Taxiway)]
            [Order(2)]
            DoubleSolid = StyleType.LineDoubleSolid,

            [Description(nameof(Localize.LineStyle_DoubleDashed))]
            [NetworkType(NetworkType.Path | NetworkType.Road | NetworkType.Taxiway)]
            [Order(3)]
            DoubleDashed = StyleType.LineDoubleDashed,

            [Description(nameof(Localize.LineStyle_SolidAndDashed))]
            [NetworkType(NetworkType.Path | NetworkType.Road | NetworkType.Taxiway)]
            [Order(5)]
            SolidAndDashed = StyleType.LineSolidAndDashed,

            [Description(nameof(Localize.LineStyle_SharkTeeth))]
            [NetworkType(NetworkType.Path | NetworkType.Road | NetworkType.Taxiway)]
            [Order(6)]
            SharkTeeth = StyleType.LineSharkTeeth,

            [Description(nameof(Localize.LineStyle_DoubleDashedAsym))]
            [NetworkType(NetworkType.Path | NetworkType.Road | NetworkType.Taxiway)]
            [Order(4)]
            DoubleDashedAsym = StyleType.LineDoubleDashedAsym,

            [Description(nameof(Localize.LineStyle_Pavement))]
            [NetworkType(NetworkType.All)]
            [Order(7)]
            Pavement = StyleType.LinePavement,

            [Description(nameof(Localize.LineStyle_Prop))]
            [NetworkType(NetworkType.All)]
            [Order(8)]
            Prop = StyleType.LineProp,

            [Description(nameof(Localize.LineStyle_Tree))]
            [NetworkType(NetworkType.All)]
            [Order(9)]
            Tree = StyleType.LineTree,

            [Description("Text")]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            Text = StyleType.LineText,

            [Description(nameof(Localize.LineStyle_Network))]
            [NetworkType(NetworkType.All)]
            [Order(10)]
            Network = StyleType.LineNetwork,

            [Description(nameof(Localize.LineStyle_Empty))]
            [NetworkType(NetworkType.All)]
            [NotVisible]
            Empty = StyleType.EmptyLine,

            [Description(nameof(Localize.Style_FromClipboard))]
            [NetworkType(NetworkType.All)]
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
            {StopLineType.DoubleSolid, new DoubleSolidStopLineStyle(DefaultColor, DefaultColor, false, DefaultStopWidth, DefaultStopOffset)},
            {StopLineType.DoubleDashed, new DoubleDashedStopLineStyle(DefaultColor, DefaultColor, false, DefaultStopWidth, DefaultDashLength, DefaultSpaceLength, DefaultStopOffset)},
            {StopLineType.SolidAndDashed, new SolidAndDashedStopLineStyle(DefaultColor, DefaultColor, false, DefaultWidth, DefaultDashLength, DefaultSpaceLength, DefaultStopOffset)},
            {StopLineType.SharkTeeth, new SharkTeethStopLineStyle(DefaultColor, DefaultSharkBaseLength, DefaultSharkHeight, DefaultSharkSpaceLength) },
            {StopLineType.Pavement, new PavementStopLineStyle(Default3DWidth, Default3DHeigth) },
        };

        public StopLineStyle(Color32 color, float width) : base(color, width) { }

        public sealed override IStyleData Calculate(MarkupLine line, ITrajectory trajectory, MarkupLOD lod)
        {
            if ((SupportLOD & lod) != 0 && line is MarkupStopLine stopLine)
                return CalculateImpl(stopLine, trajectory, lod);
            else
                return new MarkupPartGroupData(lod);
        }
        protected abstract IStyleData CalculateImpl(MarkupStopLine stopLine, ITrajectory trajectory, MarkupLOD lod);

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
