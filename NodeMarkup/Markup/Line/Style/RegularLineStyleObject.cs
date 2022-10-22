using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.UI;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public abstract class BaseObjectLineStyle : RegularLineStyle
    {
        public override bool CanOverlap => true;

        protected abstract bool IsValid { get; }
        protected abstract Vector3 PrefabSize { get; }

        public PropertyStructValue<int> Probability { get; }
        public PropertyNullableStructValue<float, PropertyStructValue<float>> Step { get; }

        public PropertyVector2Value Angle { get; }
        public PropertyVector2Value Tilt { get; }
        public PropertyNullableStructValue<Vector2, PropertyVector2Value> Slope { get; }
        public PropertyVector2Value Scale { get; }

        public PropertyVector2Value Shift { get; }
        public PropertyVector2Value Elevation { get; }
        public PropertyStructValue<float> OffsetBefore { get; }
        public PropertyStructValue<float> OffsetAfter { get; }

        public BaseObjectLineStyle(int probability, float? step, Vector2 angle, Vector2 tilt, Vector2? slope, Vector2 shift, Vector2 scale, Vector2 elevation, float offsetBefore, float offsetAfter) : base(new Color32(), 0f)
        {
            Probability = new PropertyStructValue<int>("P", StyleChanged, probability);
            Step = new PropertyNullableStructValue<float, PropertyStructValue<float>>(new PropertyStructValue<float>("S", null), "S", StyleChanged, step);
            Angle = new PropertyVector2Value(StyleChanged, angle, "AA", "AB");
            Tilt = new PropertyVector2Value(StyleChanged, tilt, "TLA", "TLB");
            Slope = new PropertyNullableStructValue<Vector2, PropertyVector2Value>(new PropertyVector2Value(null, labelX: "SLA", labelY: "SLB"), "SL", StyleChanged, slope);
            Scale = new PropertyVector2Value(StyleChanged, scale, "SCA", "SCB");
            Shift = new PropertyVector2Value(StyleChanged, shift, "SFA", "SFB");
            Elevation = new PropertyVector2Value(StyleChanged, elevation, "EA", "EB");
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
                objectTarget.Tilt.Value = Tilt;
                objectTarget.Slope.Value = Slope;
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
            var tiltProperty = AddTiltRangeProperty(parent);
            var slopeProperty = AddSlopeRangeProperty(parent);
            var shiftProperty = AddShiftProperty(parent);
            var elevationProperty = AddElevationProperty(parent);
            var scaleProperty = AddScaleRangeProperty(parent);
            var offsetBeforeProperty = AddOffsetBeforeProperty(parent);
            var offsetAfterProperty = AddOffsetAfterProperty(parent);

            components.Add(probabilityProperty);
            components.Add(stepProperty);
            components.Add(angleProperty);
            components.Add(tiltProperty);
            components.Add(slopeProperty);
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

        protected FloatStaticAutoProperty AddStepProperty(UIComponent parent)
        {
            var stepProperty = ComponentPool.Get<FloatStaticAutoProperty>(parent, nameof(Step));
            stepProperty.Text = Localize.StyleOption_ObjectStep;
            stepProperty.Format = Localize.NumberFormat_Meter;
            stepProperty.UseWheel = true;
            stepProperty.WheelStep = 0.1f;
            stepProperty.WheelTip = Settings.ShowToolTip;
            stepProperty.CheckMin = true;
            stepProperty.MinValue = 0.1f;
            stepProperty.Init();

            if (Step.HasValue)
                stepProperty.SetValue(Step.Value.Value);
            else
                stepProperty.SetAuto();

            stepProperty.OnValueChanged += (float value) => Step.Value = value;
            stepProperty.OnAutoValue += () =>
            {
                Step.Value = null;

                if (IsValid)
                    stepProperty.Value = PrefabSize.x;
            };

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

        protected FloatStaticRangeProperty AddTiltRangeProperty(UIComponent parent)
        {
            var tiltProperty = ComponentPool.GetAfter<FloatStaticRangeProperty>(parent, nameof(Angle), nameof(Tilt));
            tiltProperty.Text = Localize.StyleOption_Tilt;
            tiltProperty.Format = Localize.NumberFormat_Degree;
            tiltProperty.UseWheel = true;
            tiltProperty.WheelStep = 1f;
            tiltProperty.WheelTip = Settings.ShowToolTip;
            tiltProperty.CheckMin = true;
            tiltProperty.CheckMax = true;
            tiltProperty.MinValue = -90;
            tiltProperty.MaxValue = 90;
            tiltProperty.AllowInvert = false;
            tiltProperty.CyclicalValue = false;
            tiltProperty.Init();
            tiltProperty.SetValues(Tilt.Value.x, Tilt.Value.y);
            tiltProperty.OnValueChanged += (float valueA, float valueB) => Tilt.Value = new Vector2(valueA, valueB);

            return tiltProperty;
        }

        protected FloatStaticRangeAutoProperty AddSlopeRangeProperty(UIComponent parent)
        {
            var slopeProperty = ComponentPool.GetAfter<FloatStaticRangeAutoProperty>(parent, nameof(Tilt), nameof(Slope));
            slopeProperty.Text = Localize.StyleOption_Slope;
            slopeProperty.Format = Localize.NumberFormat_Degree;
            slopeProperty.UseWheel = true;
            slopeProperty.WheelStep = 1f;
            slopeProperty.WheelTip = Settings.ShowToolTip;
            slopeProperty.CheckMin = true;
            slopeProperty.CheckMax = true;
            slopeProperty.MinValue = -90;
            slopeProperty.MaxValue = 90;
            slopeProperty.AllowInvert = false;
            slopeProperty.CyclicalValue = false;
            slopeProperty.Init();

            if (Slope.HasValue)
                slopeProperty.SetValues(Slope.Value.Value.x, Slope.Value.Value.y);
            else
                slopeProperty.SetAuto();

            slopeProperty.OnValueChanged += (float valueA, float valueB) => Slope.Value = new Vector2(valueA, valueB);
            slopeProperty.OnAutoValue += () => Slope.Value = null;

            return slopeProperty;
        }

        protected FloatStaticRangeProperty AddShiftProperty(UIComponent parent)
        {
            var shiftProperty = ComponentPool.Get<FloatStaticRangeProperty>(parent, nameof(Shift));
            shiftProperty.Text = Localize.StyleOption_ObjectShift;
            shiftProperty.Format = Localize.NumberFormat_Meter;
            shiftProperty.UseWheel = true;
            shiftProperty.WheelStep = 0.1f;
            shiftProperty.WheelTip = Settings.ShowToolTip;
            shiftProperty.CheckMin = true;
            shiftProperty.CheckMax = true;
            shiftProperty.MinValue = -50;
            shiftProperty.MaxValue = 50;
            shiftProperty.AllowInvert = false;
            shiftProperty.CyclicalValue = false;
            shiftProperty.Init();
            shiftProperty.SetValues(Shift.Value.x, Shift.Value.y);
            shiftProperty.OnValueChanged += (float valueA, float valueB) => Shift.Value = new Vector2(valueA, valueB);

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
            scaleProperty.AllowInvert = false;
            scaleProperty.CyclicalValue = false;
            scaleProperty.Init();
            scaleProperty.SetValues(Scale.Value.x * 100f, Scale.Value.y * 100f);
            scaleProperty.OnValueChanged += (float valueA, float valueB) => Scale.Value = new Vector2(valueA, valueB) * 0.01f;

            return scaleProperty;
        }

        protected FloatStaticRangeProperty AddElevationProperty(UIComponent parent)
        {
            var elevationProperty = ComponentPool.Get<FloatStaticRangeProperty>(parent, nameof(Elevation));
            elevationProperty.Text = Localize.LineStyle_Elevation;
            elevationProperty.Format = Localize.NumberFormat_Meter;
            elevationProperty.UseWheel = true;
            elevationProperty.WheelStep = 0.1f;
            elevationProperty.WheelTip = Settings.ShowToolTip;
            elevationProperty.CheckMin = true;
            elevationProperty.CheckMax = true;
            elevationProperty.MinValue = -10;
            elevationProperty.MaxValue = 10;
            elevationProperty.AllowInvert = false;
            elevationProperty.CyclicalValue = false;
            elevationProperty.Init();
            elevationProperty.SetValues(Elevation.Value.x, Elevation.Value.y);
            elevationProperty.OnValueChanged += (float valueA, float valueB) => Elevation.Value = new Vector2(valueA, valueB);

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
            Tilt.ToXml(config);
            Slope.ToXml(config);
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
            Tilt.FromXml(config, new Vector2(DefaultObjectAngle, DefaultObjectAngle));
            Slope.FromXml(config, new Vector2(DefaultObjectAngle, DefaultObjectAngle));
            Scale.FromXml(config, new Vector2(DefaultObjectAngle, DefaultObjectAngle));
            Shift.FromXml(config, new Vector2(DefaultObjectShift, DefaultObjectShift));
            if (config.TryGetAttrValue<float>("SF", out var shift))
                Shift.Value = new Vector2(shift, shift);
            Elevation.FromXml(config, new Vector2(DefaultObjectElevation, DefaultObjectElevation));
            if (config.TryGetAttrValue<float>("E", out var elevation))
                Elevation.Value = new Vector2(elevation, elevation);
            OffsetBefore.FromXml(config, DefaultObjectOffsetBefore);
            OffsetAfter.FromXml(config, DefaultObjectOffsetAfter);
        }
    }
    public abstract class BaseObjectLineStyle<PrefabType, SelectPrefabType> : BaseObjectLineStyle
        where PrefabType : PrefabInfo
        where SelectPrefabType : SelectPrefabProperty<PrefabType>
    {
        public PropertyPrefabValue<PrefabType> Prefab { get; }
        protected override bool IsValid => IsValidPrefab(Prefab.Value);

        public BaseObjectLineStyle(PrefabType prefab, int probability, float? step, Vector2 angle, Vector2 tilt, Vector2? slope, Vector2 shift, Vector2 scale, Vector2 elevation, float offsetBefore, float offsetAfter) : base(probability, step, angle, tilt, slope, shift, scale, elevation, offsetBefore, offsetAfter)
        {
            Prefab = new PropertyPrefabValue<PrefabType>("PRF", StyleChanged, prefab);
        }

        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is BaseObjectLineStyle<PrefabType, SelectPrefabType> objectTarget)
            {
                objectTarget.Prefab.Value = Prefab;
            }
        }

        protected override IStyleData CalculateImpl(MarkupRegularLine line, ITrajectory trajectory, MarkupLOD lod)
        {
            if (Prefab.Value is not PrefabType prefab)
                return new MarkupPartGroupData(lod);

            var shift = (Shift.Value.x + Shift.Value.y) * 0.5f;

            if (shift != 0)
            {
                var startNormal = trajectory.StartDirection.Turn90(true);
                var endNormal = trajectory.EndDirection.Turn90(false);

                trajectory = new BezierTrajectory(trajectory.StartPosition + startNormal * shift, trajectory.StartDirection, trajectory.EndPosition + endNormal * shift, trajectory.EndDirection);
            }

            var length = trajectory.Length;
            if (OffsetBefore + OffsetAfter >= length)
                return new MarkupPartGroupData(lod);

            var startT = OffsetBefore == 0f ? 0f : trajectory.Travel(OffsetBefore);
            var endT = OffsetAfter == 0f ? 1f : 1f - trajectory.Invert().Travel(OffsetAfter);
            trajectory = trajectory.Cut(startT, endT);

            length = trajectory.Length;
            var stepValue = Step.HasValue ? Step.Value.Value : PrefabSize.x;
            var count = Mathf.CeilToInt(length / stepValue);

            var items = new MarkupPropItemData[count];

            var startOffset = (length - (count - 1) * stepValue) * 0.5f;
            for (int i = 0; i < count; i += 1)
            {
                if (SimulationManager.instance.m_randomizer.Int32(1, 100) > Probability)
                    continue;

                float t;
                if (count == 1)
                    t = 0.5f;
                else
                {
                    var distance = startOffset + stepValue * i;
                    t = trajectory.Travel(distance);
                }

                items[i].Position = trajectory.Position(t);

                var randomShift = SimulationManager.instance.m_randomizer.UInt32((uint)((Shift.Value.y - Shift.Value.x) * 1000f)) * 0.001f;
                items[i].Position += trajectory.Tangent(t).Turn90(true).MakeFlatNormalized() * (randomShift - (Shift.Value.y - Shift.Value.x) * 0.5f);

                var randomElevation = SimulationManager.instance.m_randomizer.UInt32((uint)((Elevation.Value.y - Elevation.Value.x) * 1000f)) * 0.001f;
                items[i].Position.y += Elevation.Value.x + randomElevation;

                var minAngle = Mathf.Min(Angle.Value.x, Angle.Value.y);
                var maxAngle = Mathf.Max(Angle.Value.x, Angle.Value.y);
                var randomAngle = (float)SimulationManager.instance.m_randomizer.UInt32((uint)(maxAngle - minAngle));
                items[i].Angle = trajectory.Tangent(t).AbsoluteAngle() + (minAngle + randomAngle) * Mathf.Deg2Rad;

                var randomTilt = (float)SimulationManager.instance.m_randomizer.UInt32((uint)(Tilt.Value.y - Tilt.Value.x));
                items[i].Tilt += (Tilt.Value.x + randomTilt) * Mathf.Deg2Rad;

                if (Slope.HasValue)
                {
                    var slopeValue = Slope.Value.Value;
                    var randomSlope = (float)SimulationManager.instance.m_randomizer.UInt32((uint)(slopeValue.y - slopeValue.x));
                    items[i].Slope += (slopeValue.x + randomSlope) * Mathf.Deg2Rad;
                }
                else
                {
                    var direction = trajectory.Tangent(t);
                    var flatDirection = direction.MakeFlat();
                    items[i].Slope = Vector3.Angle(flatDirection, direction) * Mathf.Deg2Rad;
                }

                var randomScale = SimulationManager.instance.m_randomizer.UInt32((uint)((Scale.Value.y - Scale.Value.x) * 1000f)) * 0.001f;
                items[i].Scale = Scale.Value.x + randomScale;

                CalculateItem(prefab, ref items[i]);
            }

            return GetParts(prefab, items);
        }
        protected virtual void CalculateItem(PrefabType prefab, ref MarkupPropItemData item) { }
        protected abstract IStyleData GetParts(PrefabType prefab, MarkupPropItemData[] items);

        protected sealed override EditorItem AddPrefabProperty(UIComponent parent)
        {
            var prefabProperty = ComponentPool.Get<SelectPrefabType>(parent, nameof(Prefab));
            prefabProperty.Text = Localize.StyleOption_AssetProp;
            prefabProperty.PrefabPredicate = IsValidPrefab;
            prefabProperty.Init(60f);
            prefabProperty.Prefab = Prefab;
            prefabProperty.OnValueChanged += (PrefabType value) =>
            {
                Prefab.Value = value;
                if (!Step.HasValue)
                {
                    if (parent.Find(nameof(Step)) is FloatStaticAutoProperty stepProperty)
                        stepProperty.Value = IsValid ? PrefabSize.x : DefaultObjectStep;

                    StyleChanged();
                }
            };

            return prefabProperty;
        }

        protected abstract bool IsValidPrefab(PrefabType info);

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
    public class PropLineStyle : BaseObjectLineStyle<PropInfo, SelectPropProperty>
    {
        public static new Color32 DefaultColor => new Color32();
        public static ColorOptionEnum DefaultColorOption => ColorOptionEnum.Random;

        public override StyleType Type => StyleType.LineProp;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;
        protected override Vector3 PrefabSize => IsValid ? Prefab.Value.m_generatedInfo.m_size : Vector3.zero;

        PropertyEnumValue<ColorOptionEnum> ColorOption { get; }

        public PropLineStyle(PropInfo prop, int probability, ColorOptionEnum colorOption, Color32 color, float? step, Vector2 angle, Vector2 tilt, Vector2? slope, Vector2 shift, Vector2 scale, Vector2 elevation, float offsetBefore, float offsetAfter) : base(prop, probability, step, angle, tilt, slope, shift, scale, elevation, offsetBefore, offsetAfter)
        {
            Color.Value = color;
            ColorOption = new PropertyEnumValue<ColorOptionEnum>("CO", StyleChanged, colorOption);
        }

        public override RegularLineStyle CopyLineStyle() => new PropLineStyle(Prefab.Value, Probability, ColorOption, Color, Step, Angle, Tilt, Slope, Shift, Scale, Elevation, OffsetBefore, OffsetAfter);

        protected override void CalculateItem(PropInfo prop, ref MarkupPropItemData item)
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
        protected override IStyleData GetParts(PropInfo prop, MarkupPropItemData[] items)
        {
            return new MarkupPropData(prop, items);
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
            colorProperty.Init(GetDefault()?.Color);
            colorProperty.Value = Color;
            colorProperty.OnValueChanged += (Color32 color) => Color.Value = color;

            return colorProperty;
        }

        protected override bool IsValidPrefab(PropInfo info) => info != null && !info.m_isMarker;

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

    public class TreeLineStyle : BaseObjectLineStyle<TreeInfo, SelectTreeProperty>
    {
        public override StyleType Type => StyleType.LineTree;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;
        protected override Vector3 PrefabSize => IsValid ? Prefab.Value.m_generatedInfo.m_size : Vector3.zero;

        public TreeLineStyle(TreeInfo tree, int probability, float? step, Vector2 angle, Vector2 tilt, Vector2? slope, Vector2 shift, Vector2 scale, Vector2 elevation, float offsetBefore, float offsetAfter) : base(tree, probability, step, angle, tilt, slope, shift, scale, elevation, offsetBefore, offsetAfter) { }

        public override RegularLineStyle CopyLineStyle() => new TreeLineStyle(Prefab.Value, Probability, Step, Angle, Tilt, Slope, Shift, Scale, Elevation, OffsetBefore, OffsetAfter);

        protected override IStyleData GetParts(TreeInfo tree, MarkupPropItemData[] items)
        {
            return new MarkupTreeData(tree, items);
        }

        protected override bool IsValidPrefab(TreeInfo info) => info != null;
    }
}
