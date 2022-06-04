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

        public PropertyBoolValue UseRandomAngle { get; }
        public PropertyValue<float> AngleA { get; }
        public PropertyValue<float> AngleB { get; }

        public PropertyBoolValue UseRandomScale { get; }
        public PropertyValue<float> ScaleA { get; }
        public PropertyValue<float> ScaleB { get; }

        public PropertyValue<float> Shift { get; }
        public PropertyValue<float> Elevation { get; }
        public PropertyValue<float> OffsetBefore { get; }
        public PropertyValue<float> OffsetAfter { get; }

        public BaseObjectLineStyle(string name, float step, float angleA, float angleB, bool useRandomAngle, float shift, float scaleA, float scaleB, bool useRandomScale, float elevation, float offsetBefore, float offsetAfter) : base(DefaultColor, DefaultWidth)
        {
            Name = new PropertyStringValue("N", StyleChanged, name);
            Step = new PropertyStructValue<float>("S", StyleChanged, step);

            UseRandomAngle = new PropertyBoolValue("URA", StyleChanged, useRandomAngle);
            AngleA = new PropertyStructValue<float>("AA", StyleChanged, angleA);
            AngleB = new PropertyStructValue<float>("AB", StyleChanged, angleB);

            UseRandomScale = new PropertyBoolValue("URSC", StyleChanged, useRandomScale);
            ScaleA = new PropertyStructValue<float>("SCA", StyleChanged, scaleA);
            ScaleB = new PropertyStructValue<float>("SCB", StyleChanged, scaleB);

            Shift = new PropertyStructValue<float>("SF", StyleChanged, shift);
            Elevation = new PropertyStructValue<float>("E", StyleChanged, elevation);
            OffsetBefore = new PropertyStructValue<float>("OB", StyleChanged, offsetBefore);
            OffsetAfter = new PropertyStructValue<float>("OA", StyleChanged, offsetAfter);
        }

        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is BaseObjectLineStyle objectTarget)
            {
                objectTarget.Name.Value = Name;
                objectTarget.Step.Value = Step;

                objectTarget.UseRandomAngle.Value = UseRandomAngle;
                objectTarget.AngleA.Value = AngleA;
                objectTarget.AngleB.Value = AngleB;

                objectTarget.UseRandomScale.Value = UseRandomScale;
                objectTarget.ScaleA.Value = ScaleA;
                objectTarget.ScaleB.Value = ScaleB;

                objectTarget.Shift.Value = Shift;
                objectTarget.Elevation.Value = Elevation;
                objectTarget.OffsetBefore.Value = OffsetBefore;
                objectTarget.OffsetAfter.Value = OffsetAfter;
            }
        }

        public override void GetUIComponents(MarkupRegularLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);
            components.Add(AddNameProperty(this, parent));
            components.Add(AddStepProperty(this, parent));

            var useRandomAngle = AddRandomAngleProperty(parent);
            var angle = AddAngleProperty(this, parent);
            var angleRange = AddAngleRangeProperty(this, parent);
            components.Add(useRandomAngle);
            components.Add(angle);
            components.Add(angleRange);
            useRandomAngle.OnSelectObjectChanged += ChangeAngleOption;
            ChangeAngleOption(useRandomAngle.SelectedObject);

            components.Add(AddShiftProperty(this, parent));
            components.Add(AddElevationProperty(this, parent));

            var useRandomScale = AddRandomScaleProperty(parent);
            var scale = AddScaleProperty(this, parent);
            var scaleRange = AddScaleRangeProperty(this, parent);
            components.Add(useRandomScale);
            components.Add(angle);
            components.Add(angleRange);
            useRandomScale.OnSelectObjectChanged += ChangeScaleOption;
            ChangeScaleOption(useRandomScale.SelectedObject);

            components.Add(AddOffsetBeforeProperty(this, parent));
            components.Add(AddOffsetAfterProperty(this, parent));

            void ChangeAngleOption(bool random)
            {
                if(random)
                {
                    angle.isVisible = false;
                    angleRange.isVisible = true;
                    angleRange.SetValues(AngleA, AngleB);
                }
                else
                {
                    angle.isVisible = true;
                    angleRange.isVisible = false;
                    angle.Value = AngleA;
                }

            }

            void ChangeScaleOption(bool random)
            {
                if (random)
                {
                    scale.isVisible = false;
                    scaleRange.isVisible = true;
                    scaleRange.SetValues(ScaleA * 100f, ScaleB * 100f);
                }
                else
                {
                    scale.isVisible = true;
                    scaleRange.isVisible = false;
                    scale.Value = ScaleA * 100f;
                }
            }
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
            stepProperty.Format = "{0}m";
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
        protected BoolListPropertyPanel AddRandomAngleProperty(UIComponent parent)
        {
            var useRandomAngleProperty = ComponentPool.GetBefore<BoolListPropertyPanel>(parent, nameof(UseRandomAngle));
            useRandomAngleProperty.Text = "Angle option";
            useRandomAngleProperty.Init("Static", "Range", false);
            useRandomAngleProperty.SelectedObject = UseRandomAngle;
            useRandomAngleProperty.OnSelectObjectChanged += (value) => UseRandomAngle.Value = value;

            return useRandomAngleProperty;
        }
        protected FloatPropertyPanel AddAngleProperty(BaseObjectLineStyle propStyle, UIComponent parent)
        {
            var angleProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(propStyle.AngleA));
            angleProperty.Text = Localize.StyleOption_ObjectAngle;
            angleProperty.Format = "{0}°";
            angleProperty.UseWheel = true;
            angleProperty.CyclicalValue = true;
            angleProperty.WheelStep = 1f;
            angleProperty.WheelTip = Settings.ShowToolTip;
            angleProperty.CheckMin = true;
            angleProperty.CheckMax = true;
            angleProperty.MinValue = -180;
            angleProperty.MaxValue = 180;
            angleProperty.Init();
            angleProperty.Value = propStyle.AngleA;
            angleProperty.OnValueChanged += (float value) =>
            {
                propStyle.AngleA.Value = value;
                propStyle.AngleB.Value = value;
            };

            return angleProperty;
        }
        protected FloatRangePropertyPanel AddAngleRangeProperty(BaseObjectLineStyle propStyle, UIComponent parent)
        {
            var angleProperty = ComponentPool.Get<FloatRangePropertyPanel>(parent, nameof(propStyle.AngleA));
            angleProperty.Text = Localize.StyleOption_ObjectAngle;
            angleProperty.Format = "{0}°";
            angleProperty.FieldWidth = (100f - 5f) * 0.5f;
            angleProperty.UseWheel = true;
            angleProperty.WheelStep = 1f;
            angleProperty.WheelTip = Settings.ShowToolTip;
            angleProperty.CheckMin = true;
            angleProperty.CheckMax = true;
            angleProperty.MinValue = -180;
            angleProperty.MaxValue = 180;
            angleProperty.AllowInvert = true;
            angleProperty.Init();
            angleProperty.SetValues(propStyle.AngleA, propStyle.AngleB);
            angleProperty.OnValueChanged += (float valueA, float valueB) =>
                {
                    propStyle.AngleA.Value = valueA;
                    propStyle.AngleB.Value = valueB;
                };

            return angleProperty;
        }

        protected FloatPropertyPanel AddShiftProperty(BaseObjectLineStyle propStyle, UIComponent parent)
        {
            var shiftProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(propStyle.Shift));
            shiftProperty.Text = Localize.StyleOption_ObjectShift;
            shiftProperty.Format = "{0}m";
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

        protected BoolListPropertyPanel AddRandomScaleProperty(UIComponent parent)
        {
            var useRandomScaleProperty = ComponentPool.GetBefore<BoolListPropertyPanel>(parent, nameof(UseRandomScale));
            useRandomScaleProperty.Text = "Scale option";
            useRandomScaleProperty.Init("Static", "Range", false);
            useRandomScaleProperty.SelectedObject = UseRandomScale;
            useRandomScaleProperty.OnSelectObjectChanged += (value) => UseRandomScale.Value = value;

            return useRandomScaleProperty;
        }
        protected FloatPropertyPanel AddScaleProperty(BaseObjectLineStyle propStyle, UIComponent parent)
        {
            var scaleProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(propStyle.ScaleA));
            scaleProperty.Text = Localize.StyleOption_ObjectScale;
            scaleProperty.Format = "{0}%";
            scaleProperty.UseWheel = true;
            scaleProperty.WheelStep = 1f;
            scaleProperty.WheelTip = Settings.ShowToolTip;
            scaleProperty.CheckMin = true;
            scaleProperty.CheckMax = true;
            scaleProperty.MinValue = 1f;
            scaleProperty.MaxValue = 500f;
            scaleProperty.Init();
            scaleProperty.Value = propStyle.ScaleA * 100f;
            scaleProperty.OnValueChanged += (float value) =>
            {
                propStyle.ScaleA.Value = value * 0.01f;
                propStyle.ScaleB.Value = value * 0.01f;
            };

            return scaleProperty;
        }
        protected FloatRangePropertyPanel AddScaleRangeProperty(BaseObjectLineStyle propStyle, UIComponent parent)
        {
            var scaleProperty = ComponentPool.Get<FloatRangePropertyPanel>(parent, nameof(propStyle.ScaleB));
            scaleProperty.Text = Localize.StyleOption_ObjectScale;
            scaleProperty.Format = "{0}%";
            scaleProperty.FieldWidth = (100f - 5f) * 0.5f;
            scaleProperty.UseWheel = true;
            scaleProperty.WheelStep = 1f;
            scaleProperty.WheelTip = Settings.ShowToolTip;
            scaleProperty.CheckMin = true;
            scaleProperty.CheckMax = true;
            scaleProperty.MinValue = 1f;
            scaleProperty.MaxValue = 500f;
            scaleProperty.Init();
            scaleProperty.SetValues(propStyle.ScaleA * 100f, propStyle.ScaleB * 100f);
            scaleProperty.OnValueChanged += (float valueA, float valueB) =>
            {
                propStyle.ScaleA.Value = valueA * 0.01f;
                propStyle.ScaleB.Value = valueB * 0.01f;
            };

            return scaleProperty;
        }

        protected FloatPropertyPanel AddElevationProperty(BaseObjectLineStyle propStyle, UIComponent parent)
        {
            var elevationProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(propStyle.Elevation));
            elevationProperty.Text = Localize.LineStyle_Elevation;
            elevationProperty.Format = "{0}m";
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
            offsetProperty.Format = "{0}m";
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
            offsetProperty.Format = "{0}m";
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

            UseRandomAngle.ToXml(config);
            AngleA.ToXml(config);
            AngleB.ToXml(config);

            UseRandomScale.ToXml(config);
            ScaleA.ToXml(config);
            ScaleB.ToXml(config);

            Shift.ToXml(config);
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

            UseRandomAngle.FromXml(config, false);
            AngleA.FromXml(config, DefaultObjectAngle);
            AngleB.FromXml(config, DefaultObjectAngle);

            UseRandomScale.FromXml(config, false);
            ScaleA.FromXml(config, DefaultObjectScale);
            ScaleB.FromXml(config, DefaultObjectScale);

            Shift.FromXml(config, DefaultObjectShift);
            Elevation.FromXml(config, DefaultObjectElevation);
            OffsetBefore.FromXml(config, DefaultObjectOffsetBefore);
            OffsetAfter.FromXml(config, DefaultObjectOffsetAfter);
        }
    }
    public abstract class BaseObjectLineStyle<PrefabType> : BaseObjectLineStyle
        where PrefabType : PrefabInfo
    {
        public BaseObjectLineStyle(string name, float step, float angleA, float angleB, bool useRandomAngle, float shift, float scaleA, float scaleB, bool useRandomScale, float elevation, float offsetBefore, float offsetAfter) : base(name, step, angleA, angleB, useRandomAngle, shift, scaleA, scaleB, useRandomScale, elevation, offsetBefore, offsetAfter) { }

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

            var startT = OffsetBefore == 0f ? 0f : trajectory.Travel(OffsetBefore);
            var endT = OffsetAfter == 0f ? 1f : 1f - trajectory.Invert().Travel(OffsetAfter);
            trajectory = trajectory.Cut(startT, endT);

            length = trajectory.Length;
            var count = Mathf.CeilToInt(length / Step);

            var items = new MarkupStylePropItem[count];
            if (count == 1)
            {
                items[0].position = trajectory.Position(0.5f);
                items[0].position.y += Elevation;
                items[0].angle = trajectory.Tangent(0.5f).AbsoluteAngle() + AngleA * Mathf.Deg2Rad;
                items[0].scale = ScaleA;
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

                    items[i].angle = trajectory.Tangent(t).AbsoluteAngle();
                    if (UseRandomAngle)
                    {
                        var minAngle = Mathf.Min(AngleA, AngleB);
                        var maxAngle = Mathf.Max(AngleA, AngleB);
                        var randomAngle = (float)SimulationManager.instance.m_randomizer.UInt32((uint)(maxAngle - minAngle));
                        items[i].angle += (minAngle + randomAngle) * Mathf.Deg2Rad;
                    }
                    else
                        items[i].angle += AngleA * Mathf.Deg2Rad;

                    if (UseRandomScale)
                    {
                        var randomScale = (float)SimulationManager.instance.m_randomizer.UInt32((uint)((ScaleB - ScaleA) * 1000));
                        items[i].scale = ScaleA + randomScale * 0.001f;
                    }
                    else
                        items[i].scale = ScaleA;
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

        public PropLineStyle(string name, float step, float angleA, float angleB, bool useRandomAngle, float shift, float scaleA, float scaleB, bool useRandomScale, float elevation, float offsetBefore, float offsetAfter) : base(name, step, angleA, angleB, useRandomAngle, shift, scaleA, scaleB, useRandomScale, elevation, offsetBefore, offsetAfter) { }

        public override RegularLineStyle CopyLineStyle() => new PropLineStyle(Name, Step, AngleA, AngleB, UseRandomAngle, Shift, ScaleA, ScaleB, UseRandomScale, Elevation, OffsetBefore, OffsetAfter);

        protected override IStyleData GetParts(PropInfo prop, MarkupStylePropItem[] items)
        {
            return new MarkupStyleProp(prop, items);
        }
    }
    public class TreeLineStyle : BaseObjectLineStyle<TreeInfo>
    {
        public override StyleType Type => StyleType.LineTree;
        public TreeInfo Tree => PrefabCollection<TreeInfo>.FindLoaded(Name);

        public TreeLineStyle(string name, float step, float angleA, float angleB, bool useRandomAngle, float shift, float scaleA, float scaleB, bool useRandomScale, float elevation, float offsetBefore, float offsetAfter) : base(name, step, angleA, angleB, useRandomAngle, shift, scaleA, scaleB, useRandomScale, elevation, offsetBefore, offsetAfter) { }

        public override RegularLineStyle CopyLineStyle() => new TreeLineStyle(Name, Step, AngleA, AngleB, UseRandomAngle, Shift, ScaleA, ScaleB, UseRandomScale, Elevation, OffsetBefore, OffsetAfter);

        protected override IStyleData GetParts(TreeInfo tree, MarkupStylePropItem[] items)
        {
            return new MarkupStyleTree(tree, items);
        }
    }
}
