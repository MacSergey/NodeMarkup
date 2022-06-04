using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public abstract class BaseObjectLineStyle : RegularLineStyle
    {
        public override bool CanOverlap => true;

        public PropertyValue<string> Name { get; }
        public PropertyValue<float> Step { get; }
        public PropertyValue<float> Angle { get; }
        public PropertyValue<float> Shift { get; }
        public PropertyValue<float> Scale { get; }
        public PropertyValue<float> Elevation { get; }
        public PropertyValue<float> OffsetBefore { get; }
        public PropertyValue<float> OffsetAfter { get; }

        public BaseObjectLineStyle(string name, float step, float angle, float shift, float scale, float elevation, float offsetBefore, float offsetAfter) : base(DefaultColor, DefaultWidth)
        {
            Name = new PropertyStringValue("N", StyleChanged, name);
            Step = new PropertyStructValue<float>("S", StyleChanged, step);
            Angle = new PropertyStructValue<float>("A", StyleChanged, angle);
            Shift = new PropertyStructValue<float>("SF", StyleChanged, shift);
            Scale = new PropertyStructValue<float>("SC", StyleChanged, scale);
            Elevation = new PropertyStructValue<float>("E", StyleChanged, elevation);
            OffsetBefore = new PropertyStructValue<float>("OB", StyleChanged, offsetBefore);
            OffsetAfter = new PropertyStructValue<float>("OA", StyleChanged, offsetAfter);
        }

        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is BaseObjectLineStyle propTarget)
            {
                propTarget.Name.Value = Name;
                propTarget.Step.Value = Step;
                propTarget.Angle.Value = Angle;
                propTarget.Shift.Value = Shift;
                propTarget.Scale.Value = Scale;
                propTarget.Elevation.Value = Elevation;
                propTarget.OffsetBefore.Value = OffsetBefore;
                propTarget.OffsetAfter.Value = OffsetAfter;
            }
        }

        public override void GetUIComponents(MarkupRegularLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);
            components.Add(AddNameProperty(this, parent));
            components.Add(AddStepProperty(this, parent));
            components.Add(AddAngleProperty(this, parent));
            components.Add(AddShiftProperty(this, parent));
            components.Add(AddElevationProperty(this, parent));
            components.Add(AddScaleProperty(this, parent));
            components.Add(AddOffsetBeforeProperty(this, parent));
            components.Add(AddOffsetAfterProperty(this, parent));
        }

        protected StringPropertyPanel AddNameProperty(BaseObjectLineStyle propStyle, UIComponent parent)
        {
            var nameProperty = ComponentPool.Get<StringPropertyPanel>(parent, nameof(propStyle.Name));
            nameProperty.Text = Localize.StyleOption_ObjectName;
            nameProperty.FieldWidth = 230f;
            nameProperty.Init();
            nameProperty.Value = propStyle.Name;
            nameProperty.OnValueChanged += (string value) => propStyle.Name.Value = value;

            return nameProperty;
        }
        protected FloatPropertyPanel AddStepProperty(BaseObjectLineStyle propStyle, UIComponent parent)
        {
            var stepProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(propStyle.Step));
            stepProperty.Text = Localize.StyleOption_ObjectStep;
            stepProperty.UseWheel = true;
            stepProperty.WheelStep = 0.1f;
            stepProperty.WheelTip = Settings.ShowToolTip;
            stepProperty.CheckMin = true;
            stepProperty.MinValue = 0.1f;
            stepProperty.Init();
            stepProperty.Value = propStyle.Step;
            stepProperty.OnValueChanged += (float value) => propStyle.Step.Value = value;

            return stepProperty;
        }
        protected FloatPropertyPanel AddAngleProperty(BaseObjectLineStyle propStyle, UIComponent parent)
        {
            var angleProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(propStyle.Angle));
            angleProperty.Text = Localize.StyleOption_ObjectAngle;
            angleProperty.UseWheel = true;
            angleProperty.CyclicalValue = true;
            angleProperty.WheelStep = 1f;
            angleProperty.WheelTip = Settings.ShowToolTip;
            angleProperty.CheckMin = true;
            angleProperty.CheckMax = true;
            angleProperty.MinValue = -180;
            angleProperty.MaxValue = 180;
            angleProperty.Init();
            angleProperty.Value = propStyle.Angle;
            angleProperty.OnValueChanged += (float value) => propStyle.Angle.Value = value;

            return angleProperty;
        }
        protected FloatPropertyPanel AddShiftProperty(BaseObjectLineStyle propStyle, UIComponent parent)
        {
            var shiftProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(propStyle.Shift));
            shiftProperty.Text = Localize.StyleOption_ObjectShift;
            shiftProperty.UseWheel = true;
            shiftProperty.WheelStep = 0.1f;
            shiftProperty.WheelTip = Settings.ShowToolTip;
            shiftProperty.CheckMin = true;
            shiftProperty.CheckMax = true;
            shiftProperty.MinValue = -50;
            shiftProperty.MaxValue = 50;
            shiftProperty.Init();
            shiftProperty.Value = propStyle.Shift;
            shiftProperty.OnValueChanged += (float value) => propStyle.Shift.Value = value;

            return shiftProperty;
        }
        protected FloatPropertyPanel AddScaleProperty(BaseObjectLineStyle propStyle, UIComponent parent)
        {
            var scaleProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(propStyle.Scale));
            scaleProperty.Text = Localize.StyleOption_ObjectScale;
            scaleProperty.UseWheel = true;
            scaleProperty.WheelStep = 1f;
            scaleProperty.WheelTip = Settings.ShowToolTip;
            scaleProperty.CheckMin = true;
            scaleProperty.CheckMax = true;
            scaleProperty.MinValue = 1f;
            scaleProperty.MaxValue = 500f;
            scaleProperty.Init();
            scaleProperty.Value = propStyle.Scale * 100f;
            scaleProperty.OnValueChanged += (float value) => propStyle.Scale.Value = value / 100f;

            return scaleProperty;
        }
        protected FloatPropertyPanel AddElevationProperty(BaseObjectLineStyle propStyle, UIComponent parent)
        {
            var elevationProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(propStyle.Elevation));
            elevationProperty.Text = Localize.LineStyle_Elevation;
            elevationProperty.UseWheel = true;
            elevationProperty.WheelStep = 0.1f;
            elevationProperty.WheelTip = Settings.ShowToolTip;
            elevationProperty.CheckMin = true;
            elevationProperty.CheckMax = true;
            elevationProperty.MinValue = -10;
            elevationProperty.MaxValue = 10;
            elevationProperty.Init();
            elevationProperty.Value = propStyle.Elevation;
            elevationProperty.OnValueChanged += (float value) => propStyle.Elevation.Value = value;

            return elevationProperty;
        }
        protected FloatPropertyPanel AddOffsetBeforeProperty(BaseObjectLineStyle propStyle, UIComponent parent)
        {
            var offsetProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(propStyle.OffsetBefore));
            offsetProperty.Text = Localize.StyleOption_OffsetBefore;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.WheelTip = Settings.ShowToolTip;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = 0;
            offsetProperty.Init();
            offsetProperty.Value = propStyle.OffsetBefore;
            offsetProperty.OnValueChanged += (float value) => propStyle.OffsetBefore.Value = value;

            return offsetProperty;
        }
        protected FloatPropertyPanel AddOffsetAfterProperty(BaseObjectLineStyle propStyle, UIComponent parent)
        {
            var offsetProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(propStyle.OffsetAfter));
            offsetProperty.Text = Localize.StyleOption_OffsetAfter;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.WheelTip = Settings.ShowToolTip;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = 0;
            offsetProperty.Init();
            offsetProperty.Value = propStyle.OffsetAfter;
            offsetProperty.OnValueChanged += (float value) => propStyle.OffsetAfter.Value = value;

            return offsetProperty;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Name.ToXml(config);
            Step.ToXml(config);
            Angle.ToXml(config);
            Shift.ToXml(config);
            Scale.ToXml(config);
            Elevation.ToXml(config);
            OffsetBefore.ToXml(config);
            OffsetAfter.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Name.FromXml(config, string.Empty);
            Step.FromXml(config, DefaultObjectStep);
            Angle.FromXml(config, DefaultObjectAngle);
            Shift.FromXml(config, DefaultObjectShift);
            Scale.FromXml(config, DefaultObjectScale);
            Elevation.FromXml(config, DefaultObjectElevation);
            OffsetBefore.FromXml(config, DefaultObjectOffsetBefore);
            OffsetAfter.FromXml(config, DefaultObjectOffsetAfter);
        }
    }
    public abstract class BaseObjectLineStyle<PrefabType> : BaseObjectLineStyle
        where PrefabType : PrefabInfo
    {
        public BaseObjectLineStyle(string name, float step, float angle, float shift, float scale, float elevation, float offsetBefore, float offsetAfter) : base(name, step, angle, shift, scale, elevation, offsetBefore, offsetAfter) { }

        protected override IStyleData Calculate(MarkupRegularLine line, ITrajectory trajectory, MarkupLOD lod)
        {
            if (PrefabCollection<PrefabType>.FindLoaded(Name) is not PrefabType prefab)
                return new MarkupStyleParts();

            if (Shift != 0)
            {
                var startNormal = trajectory.StartDirection.Turn90(true);
                var endNormal = trajectory.EndDirection.Turn90(false);

                trajectory = new BezierTrajectory(trajectory.StartPosition + startNormal * Shift, trajectory.StartDirection, trajectory.EndPosition + endNormal * Shift, trajectory.EndDirection);
            }

            var length = trajectory.Length;
            if (OffsetBefore + OffsetAfter >= length)
                return new MarkupStyleParts();

            var startT = OffsetBefore == 0 ? 0f : trajectory.Travel(OffsetBefore);
            var endT = OffsetAfter == 0 ? 1f : 1f - trajectory.Invert().Travel(OffsetAfter);
            trajectory = trajectory.Cut(startT, endT);

            length = trajectory.Length;
            var count = Mathf.CeilToInt(length / Step);

            var items = new MarkupStylePropItem[count];
            if (count == 1)
            {
                items[0].position = trajectory.Position(0.5f);
                items[0].position.y += Elevation;
                items[0].angle = trajectory.Tangent(0.5f).AbsoluteAngle() + Angle * Mathf.Deg2Rad;
                items[0].scale = Scale;
            }
            else
            {
                var startOffset = (length - (count - 1) * Step) * 0.5f;
                for (int i = 0; i < count; i += 1)
                {
                    var distance = startOffset + Step * i;
                    var t = trajectory.Travel(distance);
                    items[i].position = trajectory.Position(t);
                    items[i].position.y += Elevation;
                    items[i].angle = trajectory.Tangent(t).AbsoluteAngle() + Angle * Mathf.Deg2Rad;
                    items[i].scale = Scale;
                }
            }

            return GetParts(prefab, items);
        }
        protected abstract IStyleData GetParts(PrefabType prefab, MarkupStylePropItem[] items);
    }
    public class PropLineStyle : BaseObjectLineStyle<PropInfo>
    {
        public override StyleType Type => StyleType.LineProp;
        public PropInfo Prop => PrefabCollection<PropInfo>.FindLoaded(Name);

        public PropLineStyle(string name, float step, float angle, float shift, float scale, float elevation, float offsetBefore, float offsetAfter) : base(name, step, angle, shift, scale, elevation, offsetBefore, offsetAfter) { }

        public override RegularLineStyle CopyLineStyle() => new PropLineStyle(Name, Step, Angle, Shift, Scale, Elevation, OffsetBefore, OffsetAfter);

        protected override IStyleData GetParts(PropInfo prop, MarkupStylePropItem[] items)
        {
            return new MarkupStyleProp(prop, items);
        }
    }
    public class TreeLineStyle : BaseObjectLineStyle<TreeInfo>
    {
        public override StyleType Type => StyleType.LineTree;
        public TreeInfo Tree => PrefabCollection<TreeInfo>.FindLoaded(Name);

        public TreeLineStyle(string name, float step, float angle, float shift, float scale, float elevation, float offsetBefore, float offsetAfter) : base(name, step, angle, shift, scale, elevation, offsetBefore, offsetAfter) { }

        public override RegularLineStyle CopyLineStyle() => new TreeLineStyle(Name, Step, Angle, Shift, Scale, Elevation, OffsetBefore, OffsetAfter);

        protected override IStyleData GetParts(TreeInfo tree, MarkupStylePropItem[] items)
        {
            return new MarkupStyleTree(tree, items);
        }
    }
}
