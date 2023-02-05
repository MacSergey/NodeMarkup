using ColossalFramework.UI;
using IMT.Manager;
using IMT.UI;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace IMT.Manager
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
        public PropertyBoolValue TwoColors { get; }
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

        public LineStyle(Color32 color, float width, Vector2 cracks, Vector2 voids, float texture) : base(color, width, cracks, voids, texture) { }
        public LineStyle(Color32 color, float width) : base(color, width) { }

        public abstract void Calculate(MarkingLine line, ITrajectory trajectory, Action<IStyleData> addData);

        protected virtual float LodLength => 0.5f;
        protected virtual float LodWidth => 0.15f;
        public virtual bool CanOverlap => false;
        public virtual bool RenderOverlay => true;

        protected bool CheckDashedLod(MarkingLOD lod, float width, float length) => lod != MarkingLOD.LOD1 || width > LodWidth || length > LodLength;

        protected FloatPropertyPanel AddOffsetProperty(IDoubleLine doubleStyle, UIComponent parent, bool canCollapse)
        {
            var offsetProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(doubleStyle.Offset));
            offsetProperty.Text = Localize.StyleOption_OffsetBetween;
            offsetProperty.Format = Localize.NumberFormat_Meter;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.WheelTip = Settings.ShowToolTip;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = 0.05f;
            offsetProperty.CanCollapse = canCollapse;
            offsetProperty.Init();
            offsetProperty.Value = doubleStyle.Offset;
            offsetProperty.OnValueChanged += (value) => doubleStyle.Offset.Value = value;

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
            triangleProperty.OnValueChanged += (value) =>
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
            baseProperty.OnValueChanged += (value) => sharkTeethStyle.Base.Value = value;

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
            heightProperty.OnValueChanged += (value) => sharkTeethStyle.Height.Value = value;

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
            spaceProperty.OnValueChanged += (value) => sharkTeethStyle.Space.Value = value;

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
            elevationProperty.OnValueChanged += (value) => line3DStyle.Elevation.Value = value;

            return elevationProperty;
        }

        protected BoolListPropertyPanel AddUseSecondColorProperty(IDoubleLine doubleLine, UIComponent parent, bool canCollapse)
        {
            var useSecondColorProperty = ComponentPool.Get<BoolListPropertyPanel>(parent, nameof(doubleLine.TwoColors));
            useSecondColorProperty.Text = Localize.StyleOption_ColorCount;
            useSecondColorProperty.CanCollapse = canCollapse;
            useSecondColorProperty.Init(Localize.StyleOption_ColorCountOne, Localize.StyleOption_ColorCountTwo, false);
            useSecondColorProperty.SelectedObject = doubleLine.TwoColors;
            useSecondColorProperty.OnSelectObjectChanged += (value) =>
                {
                    doubleLine.TwoColors.Value = value;
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
            colorProperty.OnValueChanged += (color) => doubleLine.SecondColor.Value = color;

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

        public static float ZigZagStep => 2f;
        public static float ZigZagOffset => 1f;

        public LineStyle(Color32 color, float width, Vector2 cracks, Vector2 voids, float texture) : base(color, width, cracks, voids, texture) { }
        public LineStyle(Color32 color, float width) : base(color, width) { }

        public override LineStyle CopyStyle() => CopyLineStyle();
        public abstract StyleType CopyLineStyle();
    }
}
