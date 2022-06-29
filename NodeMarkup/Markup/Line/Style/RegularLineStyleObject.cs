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

        public BaseObjectLineStyle(float step, float angleA, float angleB, bool useRandomAngle, float shift, float scaleA, float scaleB, bool useRandomScale, float elevation, float offsetBefore, float offsetAfter) : base(new Color32(), 0f)
        {
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
            components.Add(AddPrefabProperty(parent));
            components.Add(AddStepProperty(parent));

            var useRandomAngle = AddRandomAngleProperty(parent);
            var angle = AddAngleProperty(parent);
            var angleRange = AddAngleRangeProperty(parent);
            components.Add(useRandomAngle);
            components.Add(angle);
            components.Add(angleRange);
            useRandomAngle.OnSelectObjectChanged += ChangeAngleOption;
            ChangeAngleOption(useRandomAngle.SelectedObject);

            components.Add(AddShiftProperty(parent));
            components.Add(AddElevationProperty(parent));

            var useRandomScale = AddRandomScaleProperty(parent);
            var scale = AddScaleProperty(parent);
            var scaleRange = AddScaleRangeProperty(parent);
            components.Add(useRandomScale);
            components.Add(scale);
            components.Add(scaleRange);
            useRandomScale.OnSelectObjectChanged += ChangeScaleOption;
            ChangeScaleOption(useRandomScale.SelectedObject);

            components.Add(AddOffsetBeforeProperty(parent));
            components.Add(AddOffsetAfterProperty(parent));

            void ChangeAngleOption(bool random)
            {
                if (random)
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

        protected abstract EditorItem AddPrefabProperty(UIComponent parent);
        protected FloatPropertyPanel AddStepProperty(UIComponent parent)
        {
            var stepProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(Step));
            stepProperty.Text = Localize.StyleOption_ObjectStep;
            stepProperty.Format = "{0}m";
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
        protected BoolListPropertyPanel AddRandomAngleProperty(UIComponent parent)
        {
            var useRandomAngleProperty = ComponentPool.GetBefore<BoolListPropertyPanel>(parent, nameof(UseRandomAngle));
            useRandomAngleProperty.Text = "Angle option";
            useRandomAngleProperty.Init("Static", "Range", false);
            useRandomAngleProperty.SelectedObject = UseRandomAngle;
            useRandomAngleProperty.OnSelectObjectChanged += (value) => UseRandomAngle.Value = value;

            return useRandomAngleProperty;
        }
        protected FloatPropertyPanel AddAngleProperty(UIComponent parent)
        {
            var angleProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(AngleA));
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
            angleProperty.Value = AngleA;
            angleProperty.OnValueChanged += (float value) =>
            {
                AngleA.Value = value;
                AngleB.Value = value;
            };

            return angleProperty;
        }
        protected FloatRangePropertyPanel AddAngleRangeProperty(UIComponent parent)
        {
            var angleProperty = ComponentPool.Get<FloatRangePropertyPanel>(parent, nameof(AngleA));
            angleProperty.Text = Localize.StyleOption_ObjectAngle;
            angleProperty.Format = Localize.NumberFormat_Degree;
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
            angleProperty.SetValues(AngleA, AngleB);
            angleProperty.OnValueChanged += (float valueA, float valueB) =>
                {
                    AngleA.Value = valueA;
                    AngleB.Value = valueB;
                };

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

        protected BoolListPropertyPanel AddRandomScaleProperty(UIComponent parent)
        {
            var useRandomScaleProperty = ComponentPool.GetBefore<BoolListPropertyPanel>(parent, nameof(UseRandomScale));
            useRandomScaleProperty.Text = "Scale option";
            useRandomScaleProperty.Init("Static", "Range", false);
            useRandomScaleProperty.SelectedObject = UseRandomScale;
            useRandomScaleProperty.OnSelectObjectChanged += (value) => UseRandomScale.Value = value;

            return useRandomScaleProperty;
        }
        protected FloatPropertyPanel AddScaleProperty(UIComponent parent)
        {
            var scaleProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(ScaleA));
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
            scaleProperty.Value = ScaleA * 100f;
            scaleProperty.OnValueChanged += (float value) =>
            {
                ScaleA.Value = value * 0.01f;
                ScaleB.Value = value * 0.01f;
            };

            return scaleProperty;
        }
        protected FloatRangePropertyPanel AddScaleRangeProperty(UIComponent parent)
        {
            var scaleProperty = ComponentPool.Get<FloatRangePropertyPanel>(parent, nameof(ScaleB));
            scaleProperty.Text = Localize.StyleOption_ObjectScale;
            scaleProperty.Format = Localize.NumberFormat_Percent;
            scaleProperty.FieldWidth = (100f - 5f) * 0.5f;
            scaleProperty.UseWheel = true;
            scaleProperty.WheelStep = 1f;
            scaleProperty.WheelTip = Settings.ShowToolTip;
            scaleProperty.CheckMin = true;
            scaleProperty.CheckMax = true;
            scaleProperty.MinValue = 1f;
            scaleProperty.MaxValue = 500f;
            scaleProperty.Init();
            scaleProperty.SetValues(ScaleA * 100f, ScaleB * 100f);
            scaleProperty.OnValueChanged += (float valueA, float valueB) =>
            {
                ScaleA.Value = valueA * 0.01f;
                ScaleB.Value = valueB * 0.01f;
            };

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
        public PropertyPrefabValue<PrefabType> Prefab { get; }

        public BaseObjectLineStyle(PrefabType prefab, float step, float angleA, float angleB, bool useRandomAngle, float shift, float scaleA, float scaleB, bool useRandomScale, float elevation, float offsetBefore, float offsetAfter) : base(step, angleA, angleB, useRandomAngle, shift, scaleA, scaleB, useRandomScale, elevation, offsetBefore, offsetAfter) 
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
            if (count == 1)
            {
                items[0].Position = trajectory.Position(0.5f);
                items[0].Position.y += Elevation;
                items[0].Angle = trajectory.Tangent(0.5f).AbsoluteAngle() + AngleA * Mathf.Deg2Rad;
                items[0].Scale = ScaleA;
            }
            else
            {
                var startOffset = (length - (count - 1) * Step) * 0.5f;
                for (int i = 0; i < count; i += 1)
                {
                    var distance = startOffset + Step * i;
                    var t = trajectory.Travel(distance);
                    items[i].Position = trajectory.Position(t);
                    items[i].Position.y += Elevation;

                    items[i].Angle = trajectory.Tangent(t).AbsoluteAngle();
                    if (UseRandomAngle)
                    {
                        var minAngle = Mathf.Min(AngleA, AngleB);
                        var maxAngle = Mathf.Max(AngleA, AngleB);
                        var randomAngle = (float)SimulationManager.instance.m_randomizer.UInt32((uint)(maxAngle - minAngle));
                        items[i].Angle += (minAngle + randomAngle) * Mathf.Deg2Rad;
                    }
                    else
                        items[i].Angle += AngleA * Mathf.Deg2Rad;

                    if (UseRandomScale)
                    {
                        var randomScale = (float)SimulationManager.instance.m_randomizer.UInt32((uint)((ScaleB - ScaleA) * 1000));
                        items[i].Scale = ScaleA + randomScale * 0.001f;
                    }
                    else
                        items[i].Scale = ScaleA;

                    CalculateItem(prefab, ref items[i]);
                }
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

        public PropLineStyle(PropInfo prop, ColorOptionEnum colorOption, Color32 color, float step, float angleA, float angleB, bool useRandomAngle, float shift, float scaleA, float scaleB, bool useRandomScale, float elevation, float offsetBefore, float offsetAfter) : base(prop, step, angleA, angleB, useRandomAngle, shift, scaleA, scaleB, useRandomScale, elevation, offsetBefore, offsetAfter) 
        {
            Color.Value = color;
            ColorOption = new PropertyEnumValue<ColorOptionEnum>("CO", StyleChanged, colorOption);
        }

        public override RegularLineStyle CopyLineStyle() => new PropLineStyle(Prefab.Value, ColorOption, Color, Step, AngleA, AngleB, UseRandomAngle, Shift, ScaleA, ScaleB, UseRandomScale, Elevation, OffsetBefore, OffsetAfter);

        protected override void CalculateItem(PropInfo prop, ref MarkupStylePropItem item)
        {
            switch(ColorOption.Value)
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

        public TreeLineStyle(TreeInfo tree, float step, float angleA, float angleB, bool useRandomAngle, float shift, float scaleA, float scaleB, bool useRandomScale, float elevation, float offsetBefore, float offsetAfter) : base(tree, step, angleA, angleB, useRandomAngle, shift, scaleA, scaleB, useRandomScale, elevation, offsetBefore, offsetAfter) { }

        public override RegularLineStyle CopyLineStyle() => new TreeLineStyle(Prefab.Value, Step, AngleA, AngleB, UseRandomAngle, Shift, ScaleA, ScaleB, UseRandomScale, Elevation, OffsetBefore, OffsetAfter);

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
