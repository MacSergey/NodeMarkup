using ColossalFramework.DataBinding;
using ColossalFramework.UI;
using IMT.UI;
using IMT.UI.Editors;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using UnityEngine;
using static IMT.Manager.StyleHelper;

namespace IMT.Manager
{
    public interface IFillerStyle : IStyle
    {
        PropertyValue<float> LineOffset { get; }
        PropertyValue<float> MedianOffset { get; }
    }
    public interface IPeriodicFiller : IFillerStyle
    {
        PropertyValue<float> Step { get; }
    }
    public interface IOffsetFiller : IFillerStyle
    {
        PropertyValue<float> Offset { get; }
    }
    public interface IRotateFiller : IFillerStyle
    {
        PropertyValue<float> Angle { get; }
    }
    public interface IGuideFiller : IFillerStyle
    {
        PropertyValue<int> LeftGuideA { get; }
        PropertyValue<int> LeftGuideB { get; }
        PropertyValue<int> RightGuideA { get; }
        PropertyValue<int> RightGuideB { get; }
    }
    public interface IFollowGuideFiller : IGuideFiller
    {
        PropertyValue<bool> FollowGuides { get; }
    }
    public abstract class FillerStyle : Style<FillerStyle>, IFillerStyle
    {
        public static float DefaultAngle => 0f;
        public static float DefaultStepStripe => 3f;
        public static float DefaultStepGrid => 6f;
        public static float DefaultOffset => 0f;
        public static float StripeDefaultWidth => 0.5f;
        public static float DefaultAngleBetween => 90f;
        public static float DefaultElevation => 0.3f;
        public static float DefaultCornerRadius => 0f;
        public static float DefaultCurbSize => 0f;
        public static bool DefaultFollowGuides => false;

        protected static float MinAngle => 5f;
        protected static float MinLength => 1f;
        protected static float MaxLength => 10f;

        protected static string Guide => nameof(Guide);

        private static Dictionary<FillerType, FillerStyle> Defaults { get; } = new Dictionary<FillerType, FillerStyle>()
        {
            {FillerType.Stripe, new StripeFillerStyle(DefaultColor, StripeDefaultWidth, DefaultEffect, DefaultEffect, DefaultTexture, DefaultOffset,DefaultAngle, DefaultStepStripe, DefaultOffset,  DefaultFollowGuides)},
            {FillerType.Grid, new GridFillerStyle(DefaultColor, DefaultWidth, DefaultEffect, DefaultEffect, DefaultTexture, DefaultAngle, DefaultStepGrid, DefaultOffset, DefaultOffset)},
            {FillerType.Solid, new SolidFillerStyle(DefaultColor, DefaultEffect, DefaultEffect, DefaultTexture, DefaultOffset, DefaultOffset)},
            {FillerType.Chevron, new ChevronFillerStyle(DefaultColor, StripeDefaultWidth, DefaultEffect, DefaultEffect, DefaultTexture, DefaultOffset, DefaultOffset, DefaultAngleBetween, DefaultStepStripe)},
            {FillerType.Pavement, new PavementFillerStyle(DefaultColor, DefaultWidth, DefaultOffset, DefaultOffset, DefaultElevation, DefaultCornerRadius, DefaultCornerRadius)},
            {FillerType.Grass, new GrassFillerStyle(DefaultColor, DefaultWidth, DefaultOffset, DefaultOffset, DefaultElevation, DefaultCornerRadius, DefaultCornerRadius, DefaultCurbSize, DefaultCurbSize)},
            {FillerType.Gravel, new GravelFillerStyle(DefaultColor, DefaultWidth, DefaultOffset, DefaultOffset, DefaultElevation, DefaultCornerRadius, DefaultCornerRadius, DefaultCurbSize, DefaultCurbSize)},
            {FillerType.Ruined, new RuinedFillerStyle(DefaultColor, DefaultWidth, DefaultOffset, DefaultOffset, DefaultElevation, DefaultCornerRadius, DefaultCornerRadius, DefaultCurbSize, DefaultCurbSize)},
            {FillerType.Cliff, new CliffFillerStyle(DefaultColor, DefaultWidth, DefaultOffset, DefaultOffset, DefaultElevation, DefaultCornerRadius, DefaultCornerRadius, DefaultCurbSize, DefaultCurbSize)},
        };
        public static FillerStyle GetDefault(FillerType type)
        {
            return Defaults.TryGetValue(type, out var style) ? style.CopyStyle() : null;
        }

        protected override float WidthWheelStep => 0.1f;
        protected override float WidthMinValue => 0.1f;

        public PropertyValue<float> MedianOffset { get; }
        public PropertyValue<float> LineOffset { get; }

        public FillerStyle(Color32 color, float width, Vector2 cracks, Vector2 voids, float texture, float lineOffset, float medianOffset) : base(color, width, cracks, voids, texture)
        {
            MedianOffset = GetMedianOffsetProperty(medianOffset);
            LineOffset = GetLineOffsetProperty(lineOffset);
        }
        public FillerStyle(Color32 color, float width, float lineOffset, float medianOffset) : base(color, width)
        {
            MedianOffset = GetMedianOffsetProperty(medianOffset);
            LineOffset = GetLineOffsetProperty(lineOffset);
        }

        public override void CopyTo(FillerStyle target)
        {
            base.CopyTo(target);
            if (target is IFillerStyle fillerTarget)
            {
                fillerTarget.MedianOffset.Value = MedianOffset;
                fillerTarget.LineOffset.Value = LineOffset;
            }
        }

        public sealed override void GetUIComponents(EditorProvider provider)
        {
            base.GetUIComponents(provider);
            if (provider.editor.EditObject is MarkingFiller filler)
                GetUIComponents(filler, provider);
        }
        protected virtual void GetUIComponents(MarkingFiller filler, EditorProvider provider)
        {
            if (!provider.isTemplate)
            {
                if (!filler.IsMedian)
                    provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Offset), MainCategory, AddLineOffsetProperty));
                else
                    provider.AddProperty(new PropertyInfo<Vector2PropertyPanel>(this, nameof(Offset), MainCategory, AddMedianOffsetProperty));
            }
        }

        public virtual void Calculate(MarkingFiller filler, Action<IStyleData> addData)
        {
            var contours = GetContours(filler);
            foreach (var lod in EnumExtension.GetEnumValues<MarkingLOD>())
            {
                CalculateImpl(filler, contours, lod, addData);
            }
        }
        protected virtual ContourGroup GetContours(MarkingFiller filler)
        {
            var originalContour = filler.Contour.Edges;
            var contourSets = originalContour.SetOffset(LineOffset, MedianOffset);
            return contourSets;
        }
        protected abstract void CalculateImpl(MarkingFiller filler, ContourGroup contourSets, MarkingLOD lod, Action<IStyleData> addData);

        public virtual void Render(MarkingFiller filler, OverlayData data) { }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            LineOffset.ToXml(config);
            MedianOffset.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            LineOffset.FromXml(config, DefaultOffset);
            MedianOffset.FromXml(config, DefaultOffset);
        }

        protected void AddLineOffsetProperty(FloatPropertyPanel offsetProperty, EditorProvider provider)
        {
            offsetProperty.Text = Localize.StyleOption_Offset;
            offsetProperty.Format = Localize.NumberFormat_Meter;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.WheelTip = Settings.ShowToolTip;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = 0f;
            offsetProperty.Init();
            offsetProperty.Value = LineOffset;
            offsetProperty.OnValueChanged += (float value) => LineOffset.Value = value;
        }
        private void AddMedianOffsetProperty(Vector2PropertyPanel offsetProperty, EditorProvider provider)
        {
            offsetProperty.Text = Localize.StyleOption_Offset;
            offsetProperty.FieldsWidth = 50f;
            offsetProperty.SetLabels(Localize.StyleOption_LineOffsetAbrv, Localize.StyleOption_MedianOffsetAbrv);
            offsetProperty.Format = Localize.NumberFormat_Meter;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = new Vector2(0.1f, 0.1f);
            offsetProperty.WheelTip = Settings.ShowToolTip;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = new Vector2(0f, 0f);
            offsetProperty.Init(0, 1);
            offsetProperty.Value = new Vector2(LineOffset, MedianOffset);
            offsetProperty.OnValueChanged += (Vector2 value) =>
            {
                LineOffset.Value = value.x;
                MedianOffset.Value = value.y;
            };
        }
        protected void AddAngleProperty(FloatPropertyPanel angleProperty, EditorProvider provider)
        {
            if (this is IRotateFiller rotateStyle)
            {
                angleProperty.Text = Localize.StyleOption_Angle;
                angleProperty.Format = Localize.NumberFormat_Degree;
                angleProperty.UseWheel = true;
                angleProperty.WheelStep = 1f;
                angleProperty.WheelTip = Settings.ShowToolTip;
                angleProperty.CheckMin = true;
                angleProperty.MinValue = -90;
                angleProperty.CheckMax = true;
                angleProperty.MaxValue = 90;
                angleProperty.CyclicalValue = true;
                angleProperty.Init();
                angleProperty.Value = rotateStyle.Angle;
                angleProperty.OnValueChanged += (float value) => rotateStyle.Angle.Value = value;
            }
            else
                throw new NotSupportedException();
        }

        public enum FillerType
        {
            [Description(nameof(Localize.FillerStyle_Stripe))]
            [Order(0)]
            Stripe = StyleType.FillerStripe,

            [Description(nameof(Localize.FillerStyle_Grid))]
            [Order(2)]
            Grid = StyleType.FillerGrid,

            [Description(nameof(Localize.FillerStyle_Solid))]
            [Order(3)]
            Solid = StyleType.FillerSolid,

            [Description(nameof(Localize.FillerStyle_Chevron))]
            [Order(1)]
            Chevron = StyleType.FillerChevron,

            [Description(nameof(Localize.FillerStyle_Pavement))]
            [Order(4)]
            Pavement = StyleType.FillerPavement,

            [Description(nameof(Localize.FillerStyle_Grass))]
            [Order(5)]
            Grass = StyleType.FillerGrass,

            [Description(nameof(Localize.FillerStyle_Gravel))]
            [Order(6)]
            Gravel = StyleType.FillerGravel,

            [Description(nameof(Localize.FillerStyle_Ruined))]
            [Order(7)]
            Ruined = StyleType.FillerRuined,

            [Description(nameof(Localize.FillerStyle_Cliff))]
            [Order(8)]
            Cliff = StyleType.FillerCliff,

            [Description(nameof(Localize.Style_FromClipboard))]
            [NotVisible]
            Buffer = StyleType.FillerBuffer,
        }
    }
}
