using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.UI;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public abstract class BaseObjectLineStyle : RegularLineStyle
    {
        public override bool CanOverlap => true;

        public PropertyValue<int> Probability { get; }
        public PropertyValue<float> Step { get; }

        public PropertyVector2Value Angle { get; }
        public PropertyVector2Value Scale { get; }

        public PropertyValue<float> Shift { get; }
        public PropertyValue<float> Elevation { get; }
        public PropertyValue<float> OffsetBefore { get; }
        public PropertyValue<float> OffsetAfter { get; }

        public BaseObjectLineStyle(int probability, float step, Vector2 angle, float shift, Vector2 scale, float elevation, float offsetBefore, float offsetAfter) : base(new Color32(), 0f)
        {
            Probability = new PropertyStructValue<int>("P", StyleChanged, probability);
            Step = new PropertyStructValue<float>("S", StyleChanged, step);
            Angle = new PropertyVector2Value(StyleChanged, angle, "AA", "AB");
            Scale = new PropertyVector2Value(StyleChanged, scale, "SCA", "SCB");
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
                objectTarget.Probability.Value = Probability;
                objectTarget.Step.Value = Step;
                objectTarget.Angle.Value = Angle;
                objectTarget.Scale.Value = Scale;
                objectTarget.Shift.Value = Shift;
                objectTarget.Elevation.Value = Elevation;
                objectTarget.OffsetBefore.Value = OffsetBefore;
                objectTarget.OffsetAfter.Value = OffsetAfter;
            }
        }

        public override void GetUIComponents(MarkupRegularLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);
            components.Add(AddPrefabProperty(parent));

            var probabilityProperty = AddProbabilityProperty(parent);
            var stepProperty = AddStepProperty(parent);
            var angleProperty = AddAngleRangeProperty(parent);
            var shiftProperty = AddShiftProperty(parent);
            var elevationProperty = AddElevationProperty(parent);
            var scaleProperty = AddScaleRangeProperty(parent);
            var offsetBeforeProperty = AddOffsetBeforeProperty(parent);
            var offsetAfterProperty = AddOffsetAfterProperty(parent);

            components.Add(probabilityProperty);
            components.Add(stepProperty);
            components.Add(angleProperty);
            components.Add(shiftProperty);
            components.Add(elevationProperty);
            components.Add(scaleProperty);
            components.Add(offsetBeforeProperty);
            components.Add(offsetAfterProperty);
        }

        protected abstract EditorItem AddPrefabProperty(UIComponent parent);

        protected IntPropertyPanel AddProbabilityProperty(UIComponent parent)
        {
            var probabilityProperty = ComponentPool.Get<IntPropertyPanel>(parent, nameof(Probability));
            probabilityProperty.Text = Localize.StyleOption_ObjectProbability;
            probabilityProperty.Format = Localize.NumberFormat_Percent;
            probabilityProperty.UseWheel = true;
            probabilityProperty.WheelStep = 1;
            probabilityProperty.WheelTip = Settings.ShowToolTip;
            probabilityProperty.CheckMin = true;
            probabilityProperty.MinValue = 0;
            probabilityProperty.CheckMax = true;
            probabilityProperty.MaxValue = 100;
            probabilityProperty.Init();
            probabilityProperty.Value = Probability;
            probabilityProperty.OnValueChanged += (int value) => Probability.Value = value;

            return probabilityProperty;
        }

        protected FloatPropertyPanel AddStepProperty(UIComponent parent)
        {
            var stepProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(Step));
            stepProperty.Text = Localize.StyleOption_ObjectStep;
            stepProperty.Format = Localize.NumberFormat_Meter;
            stepProperty.UseWheel = true;
            stepProperty.WheelStep = 0.1f;
            stepProperty.WheelTip = Settings.ShowToolTip;
            stepProperty.CheckMin = true;
            stepProperty.MinValue = 0.1f;
            stepProperty.Init();
            stepProperty.Value = Step;
            stepProperty.OnValueChanged += (float value) => Step.Value = value;

            return stepProperty;
        }

        protected FloatStaticRangeProperty AddAngleRangeProperty(UIComponent parent)
        {
            var angleProperty = ComponentPool.Get<FloatStaticRangeProperty>(parent, nameof(Angle));
            angleProperty.Text = Localize.StyleOption_ObjectAngle;
            angleProperty.Format = Localize.NumberFormat_Degree;
            angleProperty.UseWheel = true;
            angleProperty.WheelStep = 1f;
            angleProperty.WheelTip = Settings.ShowToolTip;
            angleProperty.CheckMin = true;
            angleProperty.CheckMax = true;
            angleProperty.MinValue = -180;
            angleProperty.MaxValue = 180;
            angleProperty.AllowInvert = true;
            angleProperty.CyclicalValue = true;
            angleProperty.Init();
            angleProperty.SetValues(Angle.Value.x, Angle.Value.y);
            angleProperty.OnValueChanged += (float valueA, float valueB) => Angle.Value = new Vector2(valueA, valueB);

            return angleProperty;
        }

        protected FloatPropertyPanel AddShiftProperty(UIComponent parent)
        {
            var shiftProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(Shift));
            shiftProperty.Text = Localize.StyleOption_ObjectShift;
            shiftProperty.Format = Localize.NumberFormat_Meter;
            shiftProperty.UseWheel = true;
            shiftProperty.WheelStep = 0.1f;
            shiftProperty.WheelTip = Settings.ShowToolTip;
            shiftProperty.CheckMin = true;
            shiftProperty.CheckMax = true;
            shiftProperty.MinValue = -50;
            shiftProperty.MaxValue = 50;
            shiftProperty.Init();
            shiftProperty.Value = Shift;
            shiftProperty.OnValueChanged += (float value) => Shift.Value = value;

            return shiftProperty;
        }

        protected FloatStaticRangeProperty AddScaleRangeProperty(UIComponent parent)
        {
            var scaleProperty = ComponentPool.Get<FloatStaticRangeProperty>(parent, nameof(Scale));
            scaleProperty.Text = Localize.StyleOption_ObjectScale;
            scaleProperty.Format = Localize.NumberFormat_Percent;
            scaleProperty.UseWheel = true;
            scaleProperty.WheelStep = 1f;
            scaleProperty.WheelTip = Settings.ShowToolTip;
            scaleProperty.CheckMin = true;
            scaleProperty.CheckMax = true;
            scaleProperty.MinValue = 1f;
            scaleProperty.MaxValue = 500f;
            scaleProperty.Init();
            scaleProperty.SetValues(Scale.Value.x * 100f, Scale.Value.y * 100f);
            scaleProperty.OnValueChanged += (float valueA, float valueB) => Scale.Value = new Vector2(valueA, valueB) * 0.01f;

            return scaleProperty;
        }

        protected FloatPropertyPanel AddElevationProperty(UIComponent parent)
        {
            var elevationProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(Elevation));
            elevationProperty.Text = Localize.LineStyle_Elevation;
            elevationProperty.Format = Localize.NumberFormat_Meter;
            elevationProperty.UseWheel = true;
            elevationProperty.WheelStep = 0.1f;
            elevationProperty.WheelTip = Settings.ShowToolTip;
            elevationProperty.CheckMin = true;
            elevationProperty.CheckMax = true;
            elevationProperty.MinValue = -10;
            elevationProperty.MaxValue = 10;
            elevationProperty.Init();
            elevationProperty.Value = Elevation;
            elevationProperty.OnValueChanged += (float value) => Elevation.Value = value;

            return elevationProperty;
        }
        protected FloatPropertyPanel AddOffsetBeforeProperty(UIComponent parent)
        {
            var offsetProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(OffsetBefore));
            offsetProperty.Text = Localize.StyleOption_OffsetBefore;
            offsetProperty.Format = Localize.NumberFormat_Meter;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.WheelTip = Settings.ShowToolTip;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = 0;
            offsetProperty.Init();
            offsetProperty.Value = OffsetBefore;
            offsetProperty.OnValueChanged += (float value) => OffsetBefore.Value = value;

            return offsetProperty;
        }
        protected FloatPropertyPanel AddOffsetAfterProperty(UIComponent parent)
        {
            var offsetProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(OffsetAfter));
            offsetProperty.Text = Localize.StyleOption_OffsetAfter;
            offsetProperty.Format = Localize.NumberFormat_Meter;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.WheelTip = Settings.ShowToolTip;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = 0;
            offsetProperty.Init();
            offsetProperty.Value = OffsetAfter;
            offsetProperty.OnValueChanged += (float value) => OffsetAfter.Value = value;

            return offsetProperty;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Probability.ToXml(config);
            Step.ToXml(config);
            Angle.ToXml(config);
            Scale.ToXml(config);
            Shift.ToXml(config);
            Elevation.ToXml(config);
            OffsetBefore.ToXml(config);
            OffsetAfter.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Probability.FromXml(config, DefaultObjectProbability);
            Step.FromXml(config, DefaultObjectStep);
            Angle.FromXml(config, new Vector2(DefaultObjectAngle, DefaultObjectAngle));
            Scale.FromXml(config, new Vector2(DefaultObjectAngle, DefaultObjectAngle));
            Shift.FromXml(config, DefaultObjectShift);
            Elevation.FromXml(config, DefaultObjectElevation);
            OffsetBefore.FromXml(config, DefaultObjectOffsetBefore);
            OffsetAfter.FromXml(config, DefaultObjectOffsetAfter);
        }
    }
    public abstract class BaseObjectLineStyle<PrefabType> : BaseObjectLineStyle
        where PrefabType : PrefabInfo
    {
        public PropertyPrefabValue<PrefabType> Prefab { get; }

        public BaseObjectLineStyle(PrefabType prefab, int probability, float step, Vector2 angle, float shift, Vector2 scale, float elevation, float offsetBefore, float offsetAfter) : base(probability, step, angle, shift, scale, elevation, offsetBefore, offsetAfter)
        {
            Prefab = new PropertyPrefabValue<PrefabType>("PRF", StyleChanged, prefab);
        }

        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is BaseObjectLineStyle<PrefabType> objectTarget)
            {
                objectTarget.Prefab.Value = Prefab;
            }
        }

        protected override IStyleData Calculate(MarkupRegularLine line, ITrajectory trajectory, MarkupLOD lod)
        {
            if (Prefab.Value is not PrefabType prefab)
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

            var startOffset = (length - (count - 1) * Step) * 0.5f;
            for (int i = 0; i < count; i += 1)
            {
                if (SimulationManager.instance.m_randomizer.Int32(1, 100) > Probability)
                    continue;

                float t;
                if (count == 1)
                    t = 0.5f;
                else
                {
                    var distance = startOffset + Step * i;
                    t = trajectory.Travel(distance);
                }

                items[i].Position = trajectory.Position(t);
                items[i].Position.y += Elevation;
                items[i].Angle = trajectory.Tangent(t).AbsoluteAngle();

                var minAngle = Mathf.Min(Angle.Value.x, Angle.Value.y);
                var maxAngle = Mathf.Max(Angle.Value.x, Angle.Value.y);
                var randomAngle = (float)SimulationManager.instance.m_randomizer.UInt32((uint)(maxAngle - minAngle));
                items[i].Angle += (minAngle + randomAngle) * Mathf.Deg2Rad;

                var randomScale = (float)SimulationManager.instance.m_randomizer.UInt32((uint)((Scale.Value.y - Scale.Value.x) * 1000));
                items[i].Scale = Scale.Value.x + randomScale * 0.001f;

                CalculateItem(prefab, ref items[i]);
            }

            return GetParts(prefab, items);
        }
        protected virtual void CalculateItem(PrefabType prefab, ref MarkupStylePropItem item) { }
        protected abstract IStyleData GetParts(PrefabType prefab, MarkupStylePropItem[] items);

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Prefab.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Prefab.FromXml(config, null);
        }
    }
    public class PropLineStyle : BaseObjectLineStyle<PropInfo>
    {
        public static bool IsValidProp(PropInfo info) => info != null && !info.m_isMarker;
        public static new Color32 DefaultColor => new Color32();
        public static ColorOptionEnum DefaultColorOption => ColorOptionEnum.Random;

        public override StyleType Type => StyleType.LineProp;

        PropertyEnumValue<ColorOptionEnum> ColorOption { get; }

        public PropLineStyle(PropInfo prop, int probability, ColorOptionEnum colorOption, Color32 color, float step, Vector2 angle, float shift, Vector2 scale, float elevation, float offsetBefore, float offsetAfter) : base(prop, probability, step, angle, shift, scale, elevation, offsetBefore, offsetAfter)
        {
            Color.Value = color;
            ColorOption = new PropertyEnumValue<ColorOptionEnum>("CO", StyleChanged, colorOption);
        }

        public override RegularLineStyle CopyLineStyle() => new PropLineStyle(Prefab.Value, Probability, ColorOption, Color, Step, Angle, Shift, Scale, Elevation, OffsetBefore, OffsetAfter);

        protected override void CalculateItem(PropInfo prop, ref MarkupStylePropItem item)
        {
            switch (ColorOption.Value)
            {
                case ColorOptionEnum.Color1:
                    item.Color = prop.m_color0;
                    break;
                case ColorOptionEnum.Color2:
                    item.Color = prop.m_color1;
                    break;
                case ColorOptionEnum.Color3:
                    item.Color = prop.m_color2;
                    break;
                case ColorOptionEnum.Color4:
                    item.Color = prop.m_color3;
                    break;
                case ColorOptionEnum.Random:
                    item.Color = prop.GetColor(ref SimulationManager.instance.m_randomizer);
                    break;
                case ColorOptionEnum.Custom:
                    item.Color = Color;
                    break;
            }
        }
        protected override IStyleData GetParts(PropInfo prop, MarkupStylePropItem[] items)
        {
            return new MarkupStyleProp(prop, items);
        }
        public override void GetUIComponents(MarkupRegularLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);

            var colorOption = AddColorOptionProperty(parent);
            var color = AddColorProperty(parent);
            components.Add(colorOption);
            components.Add(color);

            colorOption.OnSelectObjectChanged += ColorOptionChanged;
            ColorOptionChanged(ColorOption);

            void ColorOptionChanged(ColorOptionEnum option)
            {
                color.isVisible = (option == ColorOptionEnum.Custom);
            }
        }
        protected sealed override EditorItem AddPrefabProperty(UIComponent parent)
        {
            var prefabProperty = ComponentPool.Get<SelectPropProperty>(parent, nameof(Prefab));
            prefabProperty.Text = Localize.StyleOption_AssetProp;
            prefabProperty.Selector = IsValidProp;
            prefabProperty.Init(60f);
            prefabProperty.Prefab = Prefab;
            prefabProperty.OnValueChanged += (PropInfo value) => Prefab.Value = value;

            return prefabProperty;
        }
        protected PropColorPropertyPanel AddColorOptionProperty(UIComponent parent)
        {
            var colorOptionProperty = ComponentPool.GetAfter<PropColorPropertyPanel>(parent, nameof(Prefab), nameof(ColorOption));
            colorOptionProperty.Text = Localize.StyleOption_ColorOption;
            colorOptionProperty.UseWheel = true;
            colorOptionProperty.Init();
            colorOptionProperty.SelectedObject = ColorOption;
            colorOptionProperty.OnSelectObjectChanged += (value) => ColorOption.Value = value;
            return colorOptionProperty;
        }
        protected ColorAdvancedPropertyPanel AddColorProperty(UIComponent parent)
        {
            var colorProperty = ComponentPool.GetAfter<ColorAdvancedPropertyPanel>(parent, nameof(ColorOption), nameof(Color));
            colorProperty.Text = Localize.StyleOption_Color;
            colorProperty.WheelTip = Settings.ShowToolTip;
            colorProperty.Init();
            colorProperty.Value = Color;
            colorProperty.OnValueChanged += (Color32 color) => Color.Value = color;

            return colorProperty;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            ColorOption.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            ColorOption.FromXml(config, DefaultColorOption);
        }

        public enum ColorOptionEnum
        {
            [Description(nameof(Localize.StyleOption_Color1))]
            Color1,

            [Description(nameof(Localize.StyleOption_Color2))]
            Color2,

            [Description(nameof(Localize.StyleOption_Color3))]
            Color3,

            [Description(nameof(Localize.StyleOption_Color4))]
            Color4,

            [Description(nameof(Localize.StyleOption_ColorRandom))]
            Random,

            [Description(nameof(Localize.StyleOption_ColorCustom))]
            Custom,
        }
    }

    public class TreeLineStyle : BaseObjectLineStyle<TreeInfo>
    {
        public override StyleType Type => StyleType.LineTree;

        public TreeLineStyle(TreeInfo tree, int probability, float step, Vector2 angle, float shift, Vector2 scale, float elevation, float offsetBefore, float offsetAfter) : base(tree, probability, step, angle, shift, scale, elevation, offsetBefore, offsetAfter) { }

        public override RegularLineStyle CopyLineStyle() => new TreeLineStyle(Prefab.Value,Probability, Step, Angle, Shift, Scale, Elevation, OffsetBefore, OffsetAfter);

        protected override IStyleData GetParts(TreeInfo tree, MarkupStylePropItem[] items)
        {
            return new MarkupStyleTree(tree, items);
        }
        protected sealed override EditorItem AddPrefabProperty(UIComponent parent)
        {
            var prefabProperty = ComponentPool.Get<SelectTreeProperty>(parent, nameof(Prefab));
            prefabProperty.Text = Localize.StyleOption_AssetTree;
            prefabProperty.Init(60f);
            prefabProperty.Prefab = Prefab;
            prefabProperty.OnValueChanged += (TreeInfo value) => Prefab.Value = value;

            return prefabProperty;
        }
    }
}
