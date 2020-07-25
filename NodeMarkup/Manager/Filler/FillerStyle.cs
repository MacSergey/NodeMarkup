using ColossalFramework.Math;
using ColossalFramework.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public interface ISimpleFiller : IFillerStyle, IWidthStyle
    {
        float Angle { get; set; }
        float Step { get; set; }
        float Offset { get; set; }
    }

    public abstract class SimpleFillerStyle : FillerStyle, ISimpleFiller
    {
        float _angle;
        float _step;
        float _offset;

        public float Angle
        {
            get => _angle;
            set
            {
                _angle = value;
                StyleChanged();
            }
        }
        public float Step
        {
            get => _step;
            set
            {
                _step = value;
                StyleChanged();
            }
        }
        public float Offset
        {
            get => _offset;
            set
            {
                _offset = value;
                StyleChanged();
            }
        }

        public SimpleFillerStyle(Color32 color, float width, float angle, float step, float offset, float medianOffset) : base(color, width, medianOffset)
        {
            Angle = angle;
            Step = step;
            Offset = offset;
        }

        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is ISimpleFiller simpleTarget)
            {
                simpleTarget.Angle = Angle;
                simpleTarget.Step = Step;
                simpleTarget.Offset = Offset;
            }
        }

        public override List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            if (!isTemplate)
                components.Add(AddAngleProperty(this, parent, onHover, onLeave));
            components.Add(AddStepProperty(this, parent, onHover, onLeave));
            components.Add(AddOffsetProperty(this, parent, onHover, onLeave));
            return components;
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute("A", Angle));
            config.Add(new XAttribute("S", Step));
            config.Add(new XAttribute("O", Offset));
            return config;
        }
        public override void FromXml(XElement config)
        {
            base.FromXml(config);
            Angle = config.GetAttrValue("A", DefaultAngle);
            Step = config.GetAttrValue("S", DefaultStep);
            Offset = config.GetAttrValue("O", DefaultOffset);
        }
    }

    public class StripeFillerStyle : SimpleFillerStyle, ISimpleFiller
    {
        public override StyleType Type => StyleType.FillerStripe;

        public StripeFillerStyle(Color32 color, float width, float angle, float step, float offset, float medianOffset) : base(color, width, angle, step, offset, medianOffset) { }
        protected override IEnumerable<MarkupStyleDash> GetDashes(Bezier3[] parts, Rect rect, float height) => GetDashes(parts, Angle, rect, height, Width, Step, Offset);

        public override FillerStyle CopyFillerStyle() => new StripeFillerStyle(Color, Width, DefaultAngle, Step, Offset, DefaultOffset);
    }
    public class GridFillerStyle : SimpleFillerStyle, ISimpleFiller
    {
        public override StyleType Type => StyleType.FillerGrid;

        public GridFillerStyle(Color32 color, float width, float angle, float step, float offset, float medianOffset) : base(color, width, angle, step, offset, medianOffset) { }

        public override FillerStyle CopyFillerStyle() => new GridFillerStyle(Color, Width, DefaultAngle, Step, Offset, DefaultOffset);

        protected override IEnumerable<MarkupStyleDash> GetDashes(Bezier3[] parts, Rect rect, float height)
        {
            foreach (var dash in GetDashes(parts, Angle, rect, height, Width, Step, Offset))
                yield return dash;
            foreach (var dash in GetDashes(parts, Angle < 0 ? Angle + 90 : Angle - 90, rect, height, Width, Step, Offset))
                yield return dash;
        }
    }
    public class SolidFillerStyle : FillerStyle
    {
        public static float DefaultSolidWidth { get; } = 0.2f;

        public override StyleType Type => StyleType.FillerSolid;

        public SolidFillerStyle(Color32 color, float medianOffset) : base(color, DefaultSolidWidth, medianOffset) { }
        protected override IEnumerable<MarkupStyleDash> GetDashes(Bezier3[] parts, Rect rect, float height) => GetDashes(parts, 0f, rect, height, DefaultSolidWidth, 1, 0);

        public override FillerStyle CopyFillerStyle() => new SolidFillerStyle(Color, DefaultOffset);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
        }

        public override List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = new List<UIComponent>();
            components.Add(AddColorProperty(parent));
            if (!isTemplate && editObject is MarkupFiller filler && filler.IsMedian)
                components.Add(AddMedianOffsetProperty(this, parent, onHover, onLeave));

            return components;
        }
    }
}
