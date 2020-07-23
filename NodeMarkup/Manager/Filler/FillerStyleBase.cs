using ColossalFramework.Math;
using ColossalFramework.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public interface IFillerStyle : IStyle { }
    public abstract class FillerStyle : Style, IFillerStyle
    {
        public static float DefaultAngle { get; } = 0f;
        public static float DefaultStep { get; } = 6f;
        public static float DefaultOffset { get; } = 0f;

        public static StripeFillerStyle DefaultStripe => new StripeFillerStyle(DefaultColor, DefaultWidth, DefaultAngle, DefaultStep, DefaultOffset);
        public static GridFillerStyle DefaultGrid => new GridFillerStyle(DefaultColor, DefaultWidth, DefaultAngle, DefaultStep, DefaultOffset);

        public static FillerStyle GetDefault(FillerType type)
        {
            switch (type)
            {
                case FillerType.Stripe: return DefaultStripe;
                case FillerType.Grid: return DefaultGrid;
                default: return null;
            }
        }
        public FillerStyle(Color32 color, float width) : base(color, width) { }
        public override Style Copy() => CopyFillerStyle();
        public abstract FillerStyle CopyFillerStyle();
        public abstract IEnumerable<MarkupStyleDash> Calculate(MarkupFiller filler);

        protected static UIComponent AddStepProperty(ISimpleFiller stripeStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var stepProperty = parent.AddUIComponent<FloatPropertyPanel>();
            stepProperty.Text = Localize.Filler_Step;
            stepProperty.UseWheel = true;
            stepProperty.WheelStep = 0.1f;
            stepProperty.CheckMin = true;
            stepProperty.MinValue = 1f;
            stepProperty.Init();
            stepProperty.Value = stripeStyle.Step;
            stepProperty.OnValueChanged += (float value) => stripeStyle.Step = value;
            AddOnHoverLeave(stepProperty, onHover, onLeave);
            return stepProperty;
        }
        protected static UIComponent AddAngleProperty(ISimpleFiller stripeStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var angleProperty = parent.AddUIComponent<FloatPropertyPanel>();
            angleProperty.Text = Localize.Filler_Angle;
            angleProperty.UseWheel = true;
            angleProperty.WheelStep = 1f;
            angleProperty.CheckMin = true;
            angleProperty.MinValue = -90;
            angleProperty.CheckMax = true;
            angleProperty.MaxValue = 90;
            angleProperty.Init();
            angleProperty.Value = stripeStyle.Angle;
            angleProperty.OnValueChanged += (float value) => stripeStyle.Angle = value;
            AddOnHoverLeave(angleProperty, onHover, onLeave);
            return angleProperty;
        }
        protected static UIComponent AddOffsetProperty(ISimpleFiller stripeStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var offsetProperty = parent.AddUIComponent<FloatPropertyPanel>();
            offsetProperty.Text = Localize.Filler_Offset;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = 0f;
            offsetProperty.Init();
            offsetProperty.Value = stripeStyle.Offset;
            offsetProperty.OnValueChanged += (float value) => stripeStyle.Offset = value;
            AddOnHoverLeave(offsetProperty, onHover, onLeave);
            return offsetProperty;
        }

        public enum FillerType
        {
            [Description(nameof(Localize.FillerStyle_Stripe))]
            Stripe = StyleType.FillerStripe,

            [Description(nameof(Localize.FillerStyle_Grid))]
            Grid = StyleType.FillerGrid,
        }
    }
}
