using ColossalFramework.UI;
using IMT.UI;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public interface IFillerStyle : IStyle
    {
        PropertyValue<float> LineOffset { get; }
        PropertyValue<float> MedianOffset { get; }
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

        protected static Vector2 DefaultEffect => new Vector2(0f, 1f);

        protected static string Guide => nameof(Guide);

        private static Dictionary<FillerType, FillerStyle> Defaults { get; } = new Dictionary<FillerType, FillerStyle>()
        {
            {FillerType.Stripe, new StripeFillerStyle(DefaultColor, StripeDefaultWidth, DefaultOffset,DefaultAngle, DefaultStepStripe, DefaultOffset, DefaultEffect, DefaultEffect,  DefaultFollowGuides)},
            {FillerType.Grid, new GridFillerStyle(DefaultColor, DefaultWidth, DefaultAngle, DefaultStepGrid, DefaultOffset, DefaultOffset, DefaultEffect, DefaultEffect)},
            {FillerType.Solid, new SolidFillerStyle(DefaultColor, DefaultOffset, DefaultOffset, DefaultEffect, DefaultEffect)},
            {FillerType.Chevron, new ChevronFillerStyle(DefaultColor, StripeDefaultWidth, DefaultOffset, DefaultOffset, DefaultAngleBetween, DefaultStepStripe, DefaultEffect, DefaultEffect)},
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

        public PropertyValue<float> MedianOffset { get; }
        public PropertyValue<float> LineOffset { get; }

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

        public sealed override List<EditorItem> GetUIComponents(object editObject, UIComponent parent, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, isTemplate);
            if (editObject is MarkingFiller filler)
                GetUIComponents(filler, components, parent, isTemplate);
            else if (isTemplate)
                GetUIComponents(null, components, parent, isTemplate);
            return components;
        }
        public virtual void GetUIComponents(MarkingFiller filler, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            if (!isTemplate)
            {
                if (!filler.IsMedian)
                    components.Add(AddLineOffsetProperty(parent, false));
                else
                    components.Add(AddMedianOffsetProperty(parent, false));
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

        protected FloatPropertyPanel AddLineOffsetProperty(UIComponent parent, bool canCollapse)
        {
            var offsetProperty = ComponentPool.Get<FloatPropertyPanel>(parent, "Offset");
            offsetProperty.Text = Localize.StyleOption_Offset;
            offsetProperty.Format = Localize.NumberFormat_Meter;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.WheelTip = Settings.ShowToolTip;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = 0f;
            offsetProperty.CanCollapse = canCollapse;
            offsetProperty.Init();
            offsetProperty.Value = LineOffset;
            offsetProperty.OnValueChanged += (float value) => LineOffset.Value = value;

            return offsetProperty;
        }
        private Vector2PropertyPanel AddMedianOffsetProperty(UIComponent parent, bool canCollapse)
        {
            var offsetProperty = ComponentPool.Get<Vector2PropertyPanel>(parent, "Offset");
            offsetProperty.Text = Localize.StyleOption_Offset;
            offsetProperty.FieldsWidth = 50f;
            offsetProperty.SetLabels(Localize.StyleOption_LineOffsetAbrv, Localize.StyleOption_MedianOffsetAbrv);
            offsetProperty.Format = Localize.NumberFormat_Meter;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = new Vector2(0.1f, 0.1f);
            offsetProperty.WheelTip = Settings.ShowToolTip;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = new Vector2(0f, 0f);
            offsetProperty.CanCollapse = canCollapse;
            offsetProperty.Init(0, 1);
            offsetProperty.Value = new Vector2(LineOffset, MedianOffset);
            offsetProperty.OnValueChanged += (Vector2 value) =>
            {
                LineOffset.Value = value.x;
                MedianOffset.Value = value.y;
            };

            return offsetProperty;
        }
        protected FloatPropertyPanel AddAngleProperty(IRotateFiller rotateStyle, UIComponent parent, bool canCollapse)
        {
            var angleProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(rotateStyle.Angle));
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
            angleProperty.CanCollapse = canCollapse;
            angleProperty.Init();
            angleProperty.Value = rotateStyle.Angle;
            angleProperty.OnValueChanged += (float value) => rotateStyle.Angle.Value = value;

            return angleProperty;
        }
        protected FloatPropertyPanel AddStepProperty(IPeriodicFiller periodicStyle, UIComponent parent, bool canCollapse)
        {
            var stepProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(periodicStyle.Step));
            stepProperty.Text = Localize.StyleOption_Step;
            stepProperty.UseWheel = true;
            stepProperty.WheelStep = 0.1f;
            stepProperty.WheelTip = Settings.ShowToolTip;
            stepProperty.CheckMin = true;
            stepProperty.MinValue = 1.5f;
            stepProperty.CanCollapse = canCollapse;
            stepProperty.Init();
            stepProperty.Value = periodicStyle.Step;
            stepProperty.OnValueChanged += (float value) => periodicStyle.Step.Value = value;

            return stepProperty;
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
