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
        public static bool DefaultFollowRails => false;

        public static Dictionary<FillerType, FillerStyle> Defaults { get; } = new Dictionary<FillerType, FillerStyle>()
        {
            {FillerType.Stripe, new StripeFillerStyle(DefaultColor, StripeDefaultWidth, DefaultOffset,DefaultAngle, DefaultStepStripe, DefaultOffset,  DefaultFollowRails)},
            {FillerType.Grid, new GridFillerStyle(DefaultColor, DefaultWidth, DefaultAngle, DefaultStepGrid, DefaultOffset, DefaultOffset)},
            {FillerType.Solid, new SolidFillerStyle(DefaultColor, DefaultOffset)},
            {FillerType.Chevron, new ChevronFillerStyle(DefaultColor, StripeDefaultWidth, DefaultOffset, DefaultAngleBetween, DefaultStepStripe)},
            {FillerType.Pavement, new PavementFillerStyle(DefaultColor, DefaultWidth, DefaultOffset, DefaultElevation)},
            {FillerType.Grass, new GrassFillerStyle(DefaultColor, DefaultWidth, DefaultOffset, DefaultElevation)},
        };

        public PropertyValue<float> MedianOffset { get; }

        public FillerStyle(Color32 color, float width, float medianOffset) : base(color, width)
        {
            MedianOffset = GetMedianOffsetProperty(medianOffset);
        }

        public override void CopyTo(FillerStyle target)
        {
            base.CopyTo(target);
            if (target is IFillerStyle fillerTarget)
                fillerTarget.MedianOffset.Value = MedianOffset;
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
            if (!isTemplate && filler.IsMedian)
                components.Add(AddMedianOffsetProperty(parent));
        }

        public abstract IStyleData Calculate(MarkupFiller filler, MarkupLOD lod);

        public ITrajectory[] SetMedianOffset(MarkupFiller filler)
        {
            var lineParts = filler.Contour.RawParts.ToArray();
            var trajectories = filler.Contour.TrajectoriesRaw.ToArray();

            for (var i = 0; i < lineParts.Length; i += 1)
            {
                if (trajectories[i] == null)
                    continue;

                var line = lineParts[i].Line;
                if (line is MarkupEnterLine)
                    continue;

                var prevI = i == 0 ? lineParts.Length - 1 : i - 1;
                if (lineParts[prevI].Line is MarkupEnterLine && trajectories[prevI] != null)
                {
                    trajectories[i] = Shift(trajectories[i]);
                    trajectories[prevI] = new StraightTrajectory(trajectories[prevI].StartPosition, trajectories[i].StartPosition);
                }

                var nextI = i + 1 == lineParts.Length ? 0 : i + 1;
                if (lineParts[nextI].Line is MarkupEnterLine && trajectories[nextI] != null)
                {
                    trajectories[i] = Shift(trajectories[i].Invert()).Invert();
                    trajectories[nextI] = new StraightTrajectory(trajectories[i].EndPosition, trajectories[nextI].EndPosition);
                }

                ITrajectory Shift(ITrajectory trajectory)
                {
                    var newT = trajectory.Travel(0, MedianOffset);
                    return trajectory.Cut(newT, 1);
                }
            }

            return trajectories.Where(t => t != null).ToArray();
        }
        protected float GetOffset(Intersection intersect, float offset)
        {
            var sin = Mathf.Sin(intersect.Angle);
            return sin != 0 ? offset / sin : 1000f;
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

        private FloatPropertyPanel AddMedianOffsetProperty(UIComponent parent)
        {
            var offsetProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(MedianOffset));
            offsetProperty.Text = Localize.StyleOption_MedianOffset;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.WheelTip = Editor.WheelTip;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = 0f;
            offsetProperty.Init();
            offsetProperty.Value = MedianOffset;
            offsetProperty.OnValueChanged += (float value) => MedianOffset.Value = value;

            return offsetProperty;
        }
        protected FloatPropertyPanel AddAngleProperty(IRotateFiller rotateStyle, UIComponent parent)
        {
            var angleProperty = ComponentPool.GetBefore<FloatPropertyPanel>(parent, nameof(MedianOffset), nameof(rotateStyle.Angle));
            angleProperty.Text = Localize.StyleOption_Angle;
            angleProperty.UseWheel = true;
            angleProperty.WheelStep = 1f;
            angleProperty.WheelTip = Editor.WheelTip;
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
            var stepProperty = ComponentPool.GetBefore<FloatPropertyPanel>(parent, nameof(MedianOffset), nameof(periodicStyle.Step));
            stepProperty.Text = Localize.StyleOption_Step;
            stepProperty.UseWheel = true;
            stepProperty.WheelStep = 0.1f;
            stepProperty.WheelTip = Editor.WheelTip;
            stepProperty.CheckMin = true;
            stepProperty.MinValue = 1.5f;
            stepProperty.Init();
            stepProperty.Value = periodicStyle.Step;
            stepProperty.OnValueChanged += (float value) => periodicStyle.Step.Value = value;

            return stepProperty;
        }
        protected FloatPropertyPanel AddOffsetProperty(IOffsetFiller offsetStyle, UIComponent parent)
        {
            var offsetProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(offsetStyle.Offset));
            offsetProperty.Text = Localize.StyleOption_Offset;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.WheelTip = Editor.WheelTip;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = 0f;
            offsetProperty.Init();
            offsetProperty.Value = offsetStyle.Offset;
            offsetProperty.OnValueChanged += (float value) => offsetStyle.Offset.Value = value;

            return offsetProperty;
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

            [Description(nameof(Localize.Style_FromClipboard))]
            [NotVisible]
            Buffer = StyleType.FillerBuffer,
        }
    }
}
