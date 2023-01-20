using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utilities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Emit;
using System.Xml.Linq;
using UnityEngine;
using static NodeMarkup.Manager.StopLineStyle;

namespace NodeMarkup.Manager
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

        protected static string Guide => nameof(Guide);

        private static Dictionary<FillerType, FillerStyle> Defaults { get; } = new Dictionary<FillerType, FillerStyle>()
        {
            {FillerType.Stripe, new StripeFillerStyle(DefaultColor, StripeDefaultWidth, DefaultOffset,DefaultAngle, DefaultStepStripe, DefaultOffset,  DefaultFollowGuides)},
            {FillerType.Grid, new GridFillerStyle(DefaultColor, DefaultWidth, DefaultAngle, DefaultStepGrid, DefaultOffset, DefaultOffset)},
            {FillerType.Solid, new SolidFillerStyle(DefaultColor, DefaultOffset, DefaultOffset)},
            {FillerType.Chevron, new ChevronFillerStyle(DefaultColor, StripeDefaultWidth, DefaultOffset, DefaultOffset, DefaultAngleBetween, DefaultStepStripe)},
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

        public virtual IEnumerable<IStyleData> Calculate(MarkingFiller filler)
        {
            var contours = GetContours(filler);

            foreach (var lod in EnumExtension.GetEnumValues<MarkupLOD>())
            {
                foreach (var data in CalculateImpl(filler, contours, lod))
                    yield return data;
            }
        }
        protected virtual List<List<FillerContour.Part>> GetContours(MarkingFiller filler)
        {
            var originalContour = filler.Contour.Parts.ToList();
            var contours = StyleHelper.SetOffset(originalContour, LineOffset, MedianOffset);
            return contours;
        }
        protected abstract IEnumerable<IStyleData> CalculateImpl(MarkingFiller filler, List<List<FillerContour.Part>> contours, MarkupLOD lod);

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

        protected FillerGuidePropertyPanel AddGuideProperty(IGuideFiller guideStyle, FillerContour contour, UIComponent parent, bool canCollapse)
        {
            var guideProperty = ComponentPool.Get<FillerGuidePropertyPanel>(parent, Guide);
            guideProperty.Text = Localize.StyleOption_Rails;
            guideProperty.CanCollapse = canCollapse;
            guideProperty.Init();
            guideProperty.LeftGuide = new FillerGuide(contour.GetCorrectIndex(guideStyle.LeftGuideA), contour.GetCorrectIndex(guideStyle.LeftGuideB));
            guideProperty.RightGuide = new FillerGuide(contour.GetCorrectIndex(guideStyle.RightGuideA), contour.GetCorrectIndex(guideStyle.RightGuideB));
            guideProperty.Follow = (guideStyle as IFollowGuideFiller)?.FollowGuides.Value;
            guideProperty.OnValueChanged += (bool follow, FillerGuide left, FillerGuide right) =>
            {
                if(guideStyle is IFollowGuideFiller followGuideStyle)
                    followGuideStyle.FollowGuides.Value = follow;

                guideStyle.LeftGuideA.Value = left.A;
                guideStyle.LeftGuideB.Value = left.B;
                guideStyle.RightGuideA.Value = right.A;
                guideStyle.RightGuideB.Value = right.B;
            };
            return guideProperty;
        }

        public enum FillerType
        {
            [Description(nameof(Localize.FillerStyle_Stripe))]
            Stripe = StyleType.FillerStripe,

            [Description(nameof(Localize.FillerStyle_Grid))]
            Grid = StyleType.FillerGrid,

            [Description(nameof(Localize.FillerStyle_Solid))]
            Solid = StyleType.FillerSolid,

            [Description(nameof(Localize.FillerStyle_Chevron))]
            Chevron = StyleType.FillerChevron,

            [Description(nameof(Localize.FillerStyle_Pavement))]
            Pavement = StyleType.FillerPavement,

            [Description(nameof(Localize.FillerStyle_Grass))]
            Grass = StyleType.FillerGrass,

            [Description(nameof(Localize.FillerStyle_Gravel))]
            Gravel = StyleType.FillerGravel,

            [Description(nameof(Localize.FillerStyle_Ruined))]
            Ruined = StyleType.FillerRuined,

            [Description(nameof(Localize.FillerStyle_Cliff))]
            Cliff = StyleType.FillerCliff,

            [Description(nameof(Localize.Style_FromClipboard))]
            [NotVisible]
            Buffer = StyleType.FillerBuffer,
        }
        public enum GuideType
        {
            Left,
            Right
        }
    }
}
