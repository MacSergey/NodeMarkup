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
    public interface IFillerStyle { }
    public abstract class FillerStyle : Style, IFillerStyle
    {
        public static float DefaultAngle { get; } = 0f;
        public static float DefaultStep { get; } = 1f;
        public static float DefaultOffset { get; } = 0f;

        public static StrokeFillerStyle DefaultStroke => new StrokeFillerStyle(DefaultColor, DefaultWidth, DefaultAngle, DefaultStep, DefaultOffset);

        public static FillerStyle GetDefault(FillerType type)
        {
            switch (type)
            {
                case FillerType.Stroke: return DefaultStroke;
                default: return null;
            }
        }
        public FillerStyle(Color32 color, float width) : base(color, width) { }
        public override Style Copy() => CopyFillerStyle();
        public abstract FillerStyle CopyFillerStyle();
        public abstract IEnumerable<MarkupStyleDash> Calculate(MarkupFiller filler);

        protected static UIComponent AddStepProperty(IStrokeFiller strokeStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var stepProperty = parent.AddUIComponent<FloatPropertyPanel>();
            stepProperty.Text = "Step";
            stepProperty.UseWheel = true;
            stepProperty.WheelStep = 0.1f;
            stepProperty.CheckMin = true;
            stepProperty.MinValue = 0.05f;
            stepProperty.Init();
            stepProperty.Value = strokeStyle.Step;
            stepProperty.OnValueChanged += (float value) => strokeStyle.Step = value;
            AddOnHoverLeave(stepProperty, onHover, onLeave);
            return stepProperty;
        }
        protected static UIComponent AddAngleProperty(IStrokeFiller strokeStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var angleProperty = parent.AddUIComponent<FloatPropertyPanel>();
            angleProperty.Text = "Angle";
            angleProperty.UseWheel = true;
            angleProperty.WheelStep = 1f;
            angleProperty.CheckMin = true;
            angleProperty.MinValue = -90;
            angleProperty.CheckMax = true;
            angleProperty.MaxValue = 90;
            angleProperty.Init();
            angleProperty.Value = strokeStyle.Angle;
            angleProperty.OnValueChanged += (float value) => strokeStyle.Angle = value;
            AddOnHoverLeave(angleProperty, onHover, onLeave);
            return angleProperty;
        }
        protected static UIComponent AddOffsetProperty(IStrokeFiller strokeStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var offsetProperty = parent.AddUIComponent<FloatPropertyPanel>();
            offsetProperty.Text = "Offset";
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = 0f;
            offsetProperty.Init();
            offsetProperty.Value = strokeStyle.Offset;
            offsetProperty.OnValueChanged += (float value) => strokeStyle.Offset = value;
            AddOnHoverLeave(offsetProperty, onHover, onLeave);
            return offsetProperty;
        }

        public enum FillerType
        {
            [Description("LineStyle_Solid")]
            Stroke = StyleType.FillerStroke,
        }
    }
}
