using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utilities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

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
        public static bool DefaultFollowRails => false;

        public static Dictionary<FillerType, FillerStyle> Defaults { get; } = new Dictionary<FillerType, FillerStyle>()
        {
            {FillerType.Stripe, new StripeFillerStyle(DefaultColor, StripeDefaultWidth, DefaultOffset,DefaultAngle, DefaultStepStripe, DefaultOffset,  DefaultFollowRails)},
            {FillerType.Grid, new GridFillerStyle(DefaultColor, DefaultWidth, DefaultAngle, DefaultStepGrid, DefaultOffset, DefaultOffset)},
            {FillerType.Solid, new SolidFillerStyle(DefaultColor, DefaultOffset, DefaultOffset)},
            {FillerType.Chevron, new ChevronFillerStyle(DefaultColor, StripeDefaultWidth, DefaultOffset, DefaultOffset, DefaultAngleBetween, DefaultStepStripe)},
            {FillerType.Pavement, new PavementFillerStyle(DefaultColor, DefaultWidth, DefaultOffset, DefaultOffset, DefaultElevation, DefaultCornerRadius, DefaultCornerRadius)},
            {FillerType.Grass, new GrassFillerStyle(DefaultColor, DefaultWidth, DefaultOffset, DefaultOffset, DefaultElevation, DefaultCornerRadius, DefaultCornerRadius, DefaultCurbSize, DefaultCurbSize)},
            {FillerType.Gravel, new GravelFillerStyle(DefaultColor, DefaultWidth, DefaultOffset, DefaultOffset, DefaultElevation, DefaultCornerRadius, DefaultCornerRadius, DefaultCurbSize, DefaultCurbSize)},
            {FillerType.Ruined, new RuinedFillerStyle(DefaultColor, DefaultWidth, DefaultOffset, DefaultOffset, DefaultElevation, DefaultCornerRadius, DefaultCornerRadius, DefaultCurbSize, DefaultCurbSize)},
            {FillerType.Cliff, new CliffFillerStyle(DefaultColor, DefaultWidth, DefaultOffset, DefaultOffset, DefaultElevation, DefaultCornerRadius, DefaultCornerRadius, DefaultCurbSize, DefaultCurbSize)},
        };

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
            if (editObject is MarkupFiller filler)
                GetUIComponents(filler, components, parent, isTemplate);
            else if (isTemplate)
                GetUIComponents(null, components, parent, isTemplate);
            return components;
        }
        public virtual void GetUIComponents(MarkupFiller filler, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            if (!isTemplate)
            {
                components.Add(AddLineOffsetProperty(parent));
                if (filler.IsMedian)
                    components.Add(AddMedianOffsetProperty(parent));
            }
        }

        public virtual LodDictionaryArray<IStyleData> Calculate(MarkupFiller filler)
        {
            var contours = GetContours(filler);
            var data = new LodDictionaryArray<IStyleData>();

            foreach (var lod in EnumExtension.GetEnumValues<MarkupLOD>())
                data[lod] = Calculate(filler, contours, lod).ToArray();

            return data;
        }
        protected virtual List<List<FillerContour.Part>> GetContours(MarkupFiller filler)
        {
            var originalContour = filler.Contour.Parts.ToList();
            var contours = GetOffsetContours(new List<List<FillerContour.Part>>() { originalContour }, LineOffset, MedianOffset);
            return contours;
        }
        public abstract IEnumerable<IStyleData> Calculate(MarkupFiller filler, List<List<FillerContour.Part>> contours, MarkupLOD lod);
        protected List<List<FillerContour.Part>> GetOffsetContours(List<List<FillerContour.Part>> contours, float lineOffset, float medianOffset)
        {
            var offsetContours = new List<List<FillerContour.Part>>();

            foreach (var contour in contours)
            {
                var offseted = StyleHelper.SetOffset(contour, lineOffset, medianOffset);
                offsetContours.AddRange(offseted);
            }

            return offsetContours;
        }

        public virtual void Render(MarkupFiller filler, OverlayData data) { }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            MedianOffset.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            MedianOffset.FromXml(config, DefaultOffset);
        }

        protected FloatPropertyPanel AddLineOffsetProperty(UIComponent parent)
        {
            var offsetProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(LineOffset));
            offsetProperty.Text = Localize.StyleOption_LineOffset;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.WheelTip = Settings.ShowToolTip;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = 0f;
            offsetProperty.Init();
            offsetProperty.Value = LineOffset;
            offsetProperty.OnValueChanged += (float value) => LineOffset.Value = value;

            return offsetProperty;
        }
        private FloatPropertyPanel AddMedianOffsetProperty(UIComponent parent)
        {
            var offsetProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(MedianOffset));
            offsetProperty.Text = Localize.StyleOption_MedianOffset;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.WheelTip = Settings.ShowToolTip;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = 0f;
            offsetProperty.Init();
            offsetProperty.Value = MedianOffset;
            offsetProperty.OnValueChanged += (float value) => MedianOffset.Value = value;

            return offsetProperty;
        }
        protected FloatPropertyPanel AddAngleProperty(IRotateFiller rotateStyle, UIComponent parent)
        {
            var angleProperty = ComponentPool.GetBefore<FloatPropertyPanel>(parent, nameof(LineOffset), nameof(rotateStyle.Angle));
            angleProperty.Text = Localize.StyleOption_Angle;
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

            return angleProperty;
        }
        protected FloatPropertyPanel AddStepProperty(IPeriodicFiller periodicStyle, UIComponent parent)
        {
            var stepProperty = ComponentPool.GetBefore<FloatPropertyPanel>(parent, nameof(LineOffset), nameof(periodicStyle.Step));
            stepProperty.Text = Localize.StyleOption_Step;
            stepProperty.UseWheel = true;
            stepProperty.WheelStep = 0.1f;
            stepProperty.WheelTip = Settings.ShowToolTip;
            stepProperty.CheckMin = true;
            stepProperty.MinValue = 1.5f;
            stepProperty.Init();
            stepProperty.Value = periodicStyle.Step;
            stepProperty.OnValueChanged += (float value) => periodicStyle.Step.Value = value;

            return stepProperty;
        }
        protected BoolListPropertyPanel AddFollowRailsProperty(IFollowRailFiller followRailStyle, UIComponent parent)
        {
            var followRailsProperty = ComponentPool.Get<BoolListPropertyPanel>(parent, nameof(followRailStyle.FollowRails));
            followRailsProperty.Text = Localize.StyleOption_FollowRails;
            followRailsProperty.Init(Localize.StyleOption_No, Localize.StyleOption_Yes);
            followRailsProperty.SelectedObject = followRailStyle.FollowRails;
            followRailsProperty.OnSelectObjectChanged += (bool value) => followRailStyle.FollowRails.Value = value;
            return followRailsProperty;
        }
        protected void AddRailProperty(IRailFiller railStyle, FillerContour contour, UIComponent parent, out FillerRailSelectPropertyPanel leftRailProperty, out FillerRailSelectPropertyPanel rightRailProperty)
        {
            leftRailProperty = AddRailProperty(contour, parent, "LeftRail", railStyle.LeftRailA, railStyle.LeftRailB, RailType.Left, Localize.StyleOption_LeftRail);
            rightRailProperty = AddRailProperty(contour, parent, "RightRail", railStyle.RightRailA, railStyle.RightRailB, RailType.Right, Localize.StyleOption_RightRail);

            leftRailProperty.OtherRail = rightRailProperty;
            rightRailProperty.OtherRail = leftRailProperty;
        }
        private FillerRailSelectPropertyPanel AddRailProperty(FillerContour contour, UIComponent parent, string name, PropertyValue<int> railA, PropertyValue<int> railB, RailType railType, string label)
        {
            var rail = new FillerRail(contour.GetCorrectIndex(railA), contour.GetCorrectIndex(railB));
            var railProperty = ComponentPool.Get<FillerRailSelectPropertyPanel>(parent, name);
            railProperty.Text = label;
            railProperty.Init(railType);
            railProperty.Value = rail;
            railProperty.OnValueChanged += RailPropertyChanged;
            return railProperty;

            void RailPropertyChanged(FillerRail rail)
            {
                railA.Value = rail.A;
                railB.Value = rail.B;
            }
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
        public enum RailType
        {
            Left,
            Right
        }
    }
}
