using IMT.UI;
using IMT.UI.Editors;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
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

        protected void AddOffsetProperty(FloatPropertyPanel offsetProperty, EditorProvider provider)
        {
            if (this is IDoubleLine doubleStyle)
            {
                offsetProperty.Text = Localize.StyleOption_OffsetBetween;
                offsetProperty.Format = Localize.NumberFormat_Meter;
                offsetProperty.UseWheel = true;
                offsetProperty.WheelStep = 0.1f;
                offsetProperty.WheelTip = Settings.ShowToolTip;
                offsetProperty.CheckMin = true;
                offsetProperty.MinValue = 0.05f;
                offsetProperty.Init();
                offsetProperty.Value = doubleStyle.Offset;
                offsetProperty.OnValueChanged += (value) => doubleStyle.Offset.Value = value;
            }
            else
                throw new NotSupportedException();
        }
        protected void AddTriangleProperty(Vector2PropertyPanel triangleProperty, EditorProvider provider)
        {
            if (this is ISharkLine sharkTeethStyle)
            {
                triangleProperty.Text = Localize.StyleOption_Triangle;
                triangleProperty.FieldsWidth = 50f;
                triangleProperty.SetLabels(Localize.StyleOption_SharkToothBaseAbrv, Localize.StyleOption_SharkToothHeightAbrv);
                triangleProperty.Format = Localize.NumberFormat_Meter;
                triangleProperty.UseWheel = true;
                triangleProperty.WheelStep = new Vector2(0.1f, 0.1f);
                triangleProperty.WheelTip = Settings.ShowToolTip;
                triangleProperty.CheckMin = true;
                triangleProperty.MinValue = new Vector2(0.3f, 0.3f);
                triangleProperty.Init(0, 1);
                triangleProperty.Value = new Vector2(sharkTeethStyle.Base, sharkTeethStyle.Height);
                triangleProperty.OnValueChanged += (value) =>
                {
                    sharkTeethStyle.Base.Value = value.x;
                    sharkTeethStyle.Height.Value = value.y;
                };
            }
            else
                throw new NotSupportedException();
        }
        protected void AddSpaceProperty(FloatPropertyPanel spaceProperty, EditorProvider provider)
        {
            if (this is ISharkLine sharkTeethStyle)
            {
                spaceProperty.Text = Localize.StyleOption_SharkToothSpace;
                spaceProperty.Format = Localize.NumberFormat_Meter;
                spaceProperty.UseWheel = true;
                spaceProperty.WheelStep = 0.1f;
                spaceProperty.WheelTip = Settings.ShowToolTip;
                spaceProperty.CheckMin = true;
                spaceProperty.MinValue = 0.1f;
                spaceProperty.Init();
                spaceProperty.Value = sharkTeethStyle.Space;
                spaceProperty.OnValueChanged += (value) => sharkTeethStyle.Space.Value = value;
            }
            else
                throw new NotSupportedException();
        }
        protected void AddAlignmentProperty(LineAlignmentPropertyPanel alignmentProperty, EditorProvider provider)
        {
            if (this is IDoubleAlignmentLine alignmentStyle)
            {
                alignmentProperty.Text = Localize.StyleOption_Alignment;
                alignmentProperty.Init();
                alignmentProperty.SelectedObject = alignmentStyle.Alignment;
                alignmentProperty.OnSelectObjectChanged += (value) => alignmentStyle.Alignment.Value = value;
            }
            else
                throw new NotSupportedException();
        }
        protected void AddElevationProperty(FloatPropertyPanel elevationProperty, EditorProvider provider)
        {
            if (this is I3DLine line3DStyle)
            {
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
                elevationProperty.OnValueChanged += (value) => line3DStyle.Elevation.Value = value;
            }
            else
                throw new NotSupportedException();
        }

        protected void AddUseSecondColorProperty(BoolListPropertyPanel useSecondColorProperty, EditorProvider provider)
        {
            if (this is IDoubleLine doubleLine)
            {
                useSecondColorProperty.Text = Localize.StyleOption_ColorCount;
                useSecondColorProperty.Init(Localize.StyleOption_ColorCountOne, Localize.StyleOption_ColorCountTwo, false);
                useSecondColorProperty.SelectedObject = doubleLine.TwoColors;
                useSecondColorProperty.OnSelectObjectChanged += (value) =>
                    {
                        doubleLine.TwoColors.Value = value;
                        provider.Refresh();
                    };
            }
            else
                throw new NotSupportedException();
        }

        protected void AddSecondColorProperty(ColorAdvancedPropertyPanel colorProperty, EditorProvider provider)
        {
            if (this is IDoubleLine doubleLine)
            {
                colorProperty.Text = Localize.StyleOption_Color;
                colorProperty.WheelTip = Settings.ShowToolTip;
                colorProperty.Init((GetDefault() as IDoubleLine)?.SecondColor);
                colorProperty.Value = doubleLine.SecondColor;
                colorProperty.OnValueChanged += (color) => doubleLine.SecondColor.Value = color;
            }
            else
                throw new NotSupportedException();
        }
        protected void RefreshSecondColorProperty(ColorAdvancedPropertyPanel colorProperty, EditorProvider provider)
        {
            if (this is IDoubleLine doubleLine)
            {
                colorProperty.IsHidden = !doubleLine.TwoColors;
                colorProperty.Text = doubleLine.TwoColors ? Localize.StyleOption_SecondColor : Localize.StyleOption_Color;
            }
        }

        protected override void RefreshColorProperty(ColorAdvancedPropertyPanel colorProperty, EditorProvider provider)
        {
            if (this is IDoubleLine doubleLine)
            {
                colorProperty.Text = doubleLine.TwoColors ? Localize.StyleOption_MainColor : Localize.StyleOption_Color;
            }
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
