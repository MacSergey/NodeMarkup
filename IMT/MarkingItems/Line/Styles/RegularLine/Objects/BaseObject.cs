using ColossalFramework.UI;
using IMT.API;
using IMT.UI;
using IMT.Utilities;
using IMT.Utilities.API;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
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

        public PropertyEnumValue<DistributionType> Distribution { get; }

        public abstract bool CanElevate { get; }
        public abstract bool CanSlope { get; }

        public BaseObjectLineStyle(int probability, float? step, Vector2 angle, Vector2 tilt, Vector2? slope, Vector2 shift, Vector2 scale, Vector2 elevation, float offsetBefore, float offsetAfter, DistributionType distribution) : base(new Color32(), 0f)
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
            Distribution = new PropertyEnumValue<DistributionType>("PT", StyleChanged, distribution);
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

        public override void GetUIComponents(MarkingRegularLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);
            components.Add(AddPrefabProperty(parent, false));

            components.Add(AddProbabilityProperty(parent, true));
            components.Add(AddStepProperty(parent, false));
            components.Add(AddShiftProperty(parent, false));
            components.Add(AddAngleRangeProperty(parent, false));
            components.Add(AddScaleRangeProperty(parent, true));
            components.Add(AddOffsetProperty(parent, true));
            components.Add(AddDistributionProperty(parent, true));
            components.Add(AddElevationProperty(parent, false));
            components.Add(AddTiltRangeProperty(parent, true));
            components.Add(AddSlopeRangeProperty(parent, true));
        }

        protected abstract EditorItem AddPrefabProperty(UIComponent parent, bool canCollapse);

        protected IntPropertyPanel AddProbabilityProperty(UIComponent parent, bool canCollapse)
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
            probabilityProperty.CanCollapse = canCollapse;
            probabilityProperty.Init();
            probabilityProperty.Value = Probability;
            probabilityProperty.OnValueChanged += (value) => Probability.Value = value;

            return probabilityProperty;
        }

        protected FloatStaticAutoProperty AddStepProperty(UIComponent parent, bool canCollapse)
        {
            var stepProperty = ComponentPool.Get<FloatStaticAutoProperty>(parent, nameof(Step));
            stepProperty.Text = Localize.StyleOption_ObjectStep;
            stepProperty.Format = Localize.NumberFormat_Meter;
            stepProperty.UseWheel = true;
            stepProperty.WheelStep = 0.1f;
            stepProperty.WheelTip = Settings.ShowToolTip;
            stepProperty.CheckMin = true;
            stepProperty.MinValue = 0.1f;
            stepProperty.CanCollapse = canCollapse;
            stepProperty.Init();

            if (Step.HasValue)
                stepProperty.SetValue(Step.Value.Value);
            else
                stepProperty.SetAuto();

            stepProperty.OnValueChanged += (value) => Step.Value = value;
            stepProperty.OnAutoValue += () =>
            {
                Step.Value = null;

                if (IsValid)
                    stepProperty.Value = PrefabSize.x;
            };

            return stepProperty;
        }

        protected FloatStaticRangeProperty AddAngleRangeProperty(UIComponent parent, bool canCollapse)
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
            angleProperty.CanCollapse = canCollapse;
            angleProperty.Init();
            angleProperty.SetValues(Angle.Value.x, Angle.Value.y);
            angleProperty.OnValueChanged += (valueA, valueB) => Angle.Value = new Vector2(valueA, valueB);

            return angleProperty;
        }

        protected FloatStaticRangeProperty AddTiltRangeProperty(UIComponent parent, bool canCollapse)
        {
            var tiltProperty = ComponentPool.Get<FloatStaticRangeProperty>(parent, nameof(Tilt));
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
            tiltProperty.CanCollapse = canCollapse;
            tiltProperty.Init();
            tiltProperty.SetValues(Tilt.Value.x, Tilt.Value.y);
            tiltProperty.OnValueChanged += (valueA, valueB) => Tilt.Value = new Vector2(valueA, valueB);

            return tiltProperty;
        }

        protected FloatStaticRangeAutoProperty AddSlopeRangeProperty(UIComponent parent, bool canCollapse)
        {
            var slopeProperty = ComponentPool.Get<FloatStaticRangeAutoProperty>(parent, nameof(Slope));
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
            slopeProperty.CanCollapse = canCollapse;
            slopeProperty.Init();

            if (Slope.HasValue)
                slopeProperty.SetValues(Slope.Value.Value.x, Slope.Value.Value.y);
            else
                slopeProperty.SetAuto();

            slopeProperty.OnValueChanged += (valueA, valueB) => Slope.Value = new Vector2(valueA, valueB);
            slopeProperty.OnAutoValue += () => Slope.Value = null;

            return slopeProperty;
        }

        protected FloatStaticRangeProperty AddShiftProperty(UIComponent parent, bool canCollapse)
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
            shiftProperty.CanCollapse = canCollapse;
            shiftProperty.Init();
            shiftProperty.SetValues(Shift.Value.x, Shift.Value.y);
            shiftProperty.OnValueChanged += (valueA, valueB) => Shift.Value = new Vector2(valueA, valueB);

            return shiftProperty;
        }

        protected FloatStaticRangeProperty AddScaleRangeProperty(UIComponent parent, bool canCollapse)
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
            scaleProperty.CanCollapse = canCollapse;
            scaleProperty.Init();
            scaleProperty.SetValues(Scale.Value.x * 100f, Scale.Value.y * 100f);
            scaleProperty.OnValueChanged += (valueA, valueB) => Scale.Value = new Vector2(valueA, valueB) * 0.01f;

            return scaleProperty;
        }

        protected FloatStaticRangeProperty AddElevationProperty(UIComponent parent, bool canCollapse)
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
            elevationProperty.CanCollapse = canCollapse;
            elevationProperty.Init();
            elevationProperty.SetValues(Elevation.Value.x, Elevation.Value.y);
            elevationProperty.OnValueChanged += (valueA, valueB) => Elevation.Value = new Vector2(valueA, valueB);

            return elevationProperty;
        }
        protected Vector2PropertyPanel AddOffsetProperty(UIComponent parent, bool canCollapse)
        {
            var offsetProperty = ComponentPool.Get<Vector2PropertyPanel>(parent, nameof(Offset));
            offsetProperty.Text = Localize.StyleOption_Offset;
            offsetProperty.SetLabels(Localize.StyleOption_OffsetBeforeAbrv, Localize.StyleOption_OffsetAfterAbrv);
            offsetProperty.FieldsWidth = 50f;
            offsetProperty.Format = Localize.NumberFormat_Meter;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = new Vector2(0.1f, 0.1f);
            offsetProperty.WheelTip = Settings.ShowToolTip;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = Vector2.zero;
            offsetProperty.CanCollapse = canCollapse;
            offsetProperty.Init(0, 1);
            offsetProperty.Value = new Vector2(OffsetBefore, OffsetAfter);
            offsetProperty.OnValueChanged += (value) =>
            {
                OffsetBefore.Value = value.x;
                OffsetAfter.Value = value.y;
            };

            return offsetProperty;
        }
        protected DistributionTypePanel AddDistributionProperty(UIComponent parent, bool canCollapse)
        {
            var distributionProperty = ComponentPool.Get<DistributionTypePanel>(parent, nameof(Distribution));
            distributionProperty.Text = Localize.StyleOption_Distribution;
            distributionProperty.Selector.AutoButtonSize = false;
            distributionProperty.Selector.ButtonWidth = 57f;
            distributionProperty.Selector.atlas = IMTTextures.Atlas;
            distributionProperty.CanCollapse = canCollapse;
            distributionProperty.Init();
            distributionProperty.SelectedObject = Distribution;
            distributionProperty.OnSelectObjectChanged += (value) => Distribution.Value = value;

            return distributionProperty;
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
            Distribution.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
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
            Distribution.FromXml(config, DistributionType.FixedSpaceFreeEnd);

            if (invert)
            {
                var offsetBefore = OffsetBefore.Value;
                var offsetAfter = OffsetAfter.Value;
                OffsetBefore.Value = offsetAfter;
                OffsetAfter.Value = offsetBefore;
            }

            if (map.Invert ^ invert ^ typeChanged)
            {
                Shift.Value = -Shift.Value;
                var angleX = Angle.Value.x > 0 ? Angle.Value.x - 180 : Angle.Value.x + 180;
                var angleY = Angle.Value.y > 0 ? Angle.Value.y - 180 : Angle.Value.y + 180;
                Angle.Value = new Vector2(angleX, angleY);
            }
        }
    }
    public abstract class BaseObjectLineStyle<PrefabType, SelectPrefabType> : BaseObjectLineStyle
        where PrefabType : PrefabInfo
        where SelectPrefabType : SelectPrefabProperty<PrefabType>
    {
        public PropertyPrefabValue<PrefabType> Prefab { get; }
        protected override bool IsValid => IsValidPrefab(Prefab.Value);
        protected abstract string AssetPropertyName { get; }

        public BaseObjectLineStyle(PrefabType prefab, int probability, float? step, Vector2 angle, Vector2 tilt, Vector2? slope, Vector2 shift, Vector2 scale, Vector2 elevation, float offsetBefore, float offsetAfter, DistributionType distribution) : base(probability, step, angle, tilt, slope, shift, scale, elevation, offsetBefore, offsetAfter, distribution)
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

        protected override void CalculateImpl(MarkingRegularLine line, ITrajectory trajectory, MarkingLOD lod, Action<IStyleData> addData)
        {
            if (Prefab.Value is not PrefabType prefab)
                return;

            var shift = (Shift.Value.x + Shift.Value.y) * 0.5f;

            if (shift != 0)
            {
                trajectory = trajectory.Shift(shift, shift);
            }

            var length = trajectory.Length;
            if (OffsetBefore + OffsetAfter >= length)
                return;

            var startT = OffsetBefore == 0f ? 0f : trajectory.Travel(OffsetBefore);
            var endT = OffsetAfter == 0f ? 1f : 1f - trajectory.Invert().Travel(OffsetAfter);
            trajectory = trajectory.Cut(startT, endT);

            length = trajectory.Length;
            var stepValue = Step.HasValue ? Step.Value.Value : PrefabSize.x;

            MarkingPropItemData[] items;
            int startIndex;
            int count;
            float startOffset;

            switch (Distribution.Value)
            {
                case DistributionType.FixedSpaceFreeEnd:
                    {
                        startIndex = 0;
                        count = Mathf.CeilToInt(length / stepValue);
                        startOffset = (length - (count - 1) * stepValue) * 0.5f;
                        items = new MarkingPropItemData[count];
                        break;
                    }
                case DistributionType.FixedSpaceFixedEnd:
                    {
                        startIndex = 1;
                        count = Math.Max(Mathf.RoundToInt(length / stepValue - 1.5f), 0);
                        startOffset = (length - (count - 1) * stepValue) * 0.5f;
                        items = new MarkingPropItemData[count + 2];

                        CalculateItem(trajectory, 0f, prefab, ref items[0]);
                        CalculateItem(trajectory, 1f, prefab, ref items[items.Length - 1]);
                        break;
                    }
                case DistributionType.DynamicSpaceFreeEnd:
                    {
                        startIndex = 0;
                        count = Mathf.RoundToInt(length / stepValue);
                        stepValue = length / count;
                        startOffset = (length - (count - 1) * stepValue) * 0.5f;
                        items = new MarkingPropItemData[count];
                        break;
                    }
                case DistributionType.DynamicSpaceFixedEnd:
                    {
                        startIndex = 1;
                        count = Math.Max(Mathf.RoundToInt(length / stepValue) - 1, 0);
                        stepValue = length / (count + 1);
                        startOffset = stepValue;
                        items = new MarkingPropItemData[count + 2];

                        CalculateItem(trajectory, 0f, prefab, ref items[0]);
                        CalculateItem(trajectory, 1f, prefab, ref items[items.Length - 1]);
                        break;
                    }
                default:
                    return;
            }

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

                CalculateItem(trajectory, t, prefab, ref items[i + startIndex]);
            }

            CalculateParts(prefab, items, lod, addData);
        }
        private void CalculateItem(ITrajectory trajectory, float t, PrefabType prefab, ref MarkingPropItemData item)
        {
            item.Position = trajectory.Position(t);

            var randomShift = SimulationManager.instance.m_randomizer.UInt32((uint)((Shift.Value.y - Shift.Value.x) * 1000f)) * 0.001f;
            item.Position += trajectory.Tangent(t).Turn90(true).MakeFlatNormalized() * (randomShift - (Shift.Value.y - Shift.Value.x) * 0.5f);

            if (CanElevate)
            {
                var randomElevation = SimulationManager.instance.m_randomizer.UInt32((uint)((Elevation.Value.y - Elevation.Value.x) * 1000f)) * 0.001f;
                item.Position.y += Elevation.Value.x + randomElevation;
            }

            var minAngle = Mathf.Min(Angle.Value.x, Angle.Value.y);
            var maxAngle = Mathf.Max(Angle.Value.x, Angle.Value.y);
            var randomAngle = (float)SimulationManager.instance.m_randomizer.UInt32((uint)(maxAngle - minAngle));
            item.AbsoluteAngle = trajectory.Tangent(t).AbsoluteAngle();
            item.Angle = (minAngle + randomAngle) * Mathf.Deg2Rad;

            if (CanSlope)
            {
                var randomTilt = (float)SimulationManager.instance.m_randomizer.UInt32((uint)(Tilt.Value.y - Tilt.Value.x));
                item.Tilt += (Tilt.Value.x + randomTilt) * Mathf.Deg2Rad;

                if (Slope.HasValue)
                {
                    var slopeValue = Slope.Value.Value;
                    var randomSlope = (float)SimulationManager.instance.m_randomizer.UInt32((uint)(slopeValue.y - slopeValue.x));
                    item.Slope += (slopeValue.x + randomSlope) * Mathf.Deg2Rad;
                }
                else
                {
                    var direction = trajectory.Tangent(t);
                    var flatDirection = direction.MakeFlat();
                    item.Slope = Mathf.Sign(direction.y) * Vector3.Angle(flatDirection, direction) * Mathf.Deg2Rad;
                }
            }

            var randomScale = SimulationManager.instance.m_randomizer.UInt32((uint)((Scale.Value.y - Scale.Value.x) * 1000f)) * 0.001f;
            item.Scale = Scale.Value.x + randomScale;

            CalculateItem(prefab, ref item);
        }
        protected virtual void CalculateItem(PrefabType prefab, ref MarkingPropItemData item) { }
        protected abstract void CalculateParts(PrefabType prefab, MarkingPropItemData[] items, MarkingLOD lod, Action<IStyleData> addData);

        public override void GetUIComponents(MarkingRegularLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);
            PrefabChanged(parent, IsValid);
        }
        protected sealed override EditorItem AddPrefabProperty(UIComponent parent, bool canCollapse)
        {
            var prefabProperty = ComponentPool.Get<SelectPrefabType>(parent, nameof(Prefab));
            prefabProperty.Text = AssetPropertyName;
            prefabProperty.PrefabSelectPredicate = IsValidPrefab;
            prefabProperty.PrefabSortPredicate = GetSortPredicate();
            prefabProperty.CanCollapse = canCollapse;
            prefabProperty.Init(60f);
            prefabProperty.Prefab = Prefab;
            prefabProperty.OnValueChanged += (value) =>
            {
                Prefab.Value = value;
                PrefabChanged(parent, IsValid);

                if (!Step.HasValue)
                {
                    if (parent.Find(nameof(Step)) is FloatStaticAutoProperty stepProperty)
                        stepProperty.SimulateEnterValue(IsValid ? PrefabSize.x : DefaultObjectStep);
                }
            };

            return prefabProperty;
        }
        protected virtual void PrefabChanged(UIComponent parent, bool valid)
        {
            if (parent.Find(nameof(Probability)) is EditorPropertyPanel probability)
                probability.IsHidden = !valid;

            if (parent.Find(nameof(Step)) is EditorPropertyPanel step)
                step.IsHidden = !valid;

            if (parent.Find(nameof(Shift)) is EditorPropertyPanel shift)
                shift.IsHidden = !valid;

            if (parent.Find(nameof(Angle)) is EditorPropertyPanel angle)
                angle.IsHidden = !valid;

            if (parent.Find(nameof(Scale)) is EditorPropertyPanel scale)
                scale.IsHidden = !valid;

            if (parent.Find(nameof(Offset)) is EditorPropertyPanel offset)
                offset.IsHidden = !valid;

            if (parent.Find(nameof(Distribution)) is EditorPropertyPanel distribution)
                distribution.IsHidden = !valid;

            if (parent.Find(nameof(Elevation)) is EditorPropertyPanel elevation)
                elevation.IsHidden = !valid || !CanElevate;

            if (parent.Find(nameof(Tilt)) is EditorPropertyPanel tilt)
                tilt.IsHidden = !valid || !CanSlope;

            if (parent.Find(nameof(Slope)) is EditorPropertyPanel slope)
                slope.IsHidden = !valid || !CanSlope;
        }

        protected abstract bool IsValidPrefab(PrefabType info);
        protected abstract Func<PrefabType, string> GetSortPredicate();

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Prefab.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            Prefab.FromXml(config, null);
        }
    }

    public enum DistributionType
    {
        [Description(nameof(Localize.StyleOption_DistributionFixedFree))]
        [Sprite(nameof(IMTTextures.FixedFreeButtonIcon))]
        FixedSpaceFreeEnd,

        [Description(nameof(Localize.StyleOption_DistributionFixedFixed))]
        [Sprite(nameof(IMTTextures.FixedFixedButtonIcon))]
        FixedSpaceFixedEnd,

        [Description(nameof(Localize.StyleOption_DistributionDynamicFree))]
        [Sprite(nameof(IMTTextures.DynamicFreeButtonIcon))]
        DynamicSpaceFreeEnd,

        [Description(nameof(Localize.StyleOption_DistributionDynamicFixed))]
        [Sprite(nameof(IMTTextures.DynamicFixedButtonIcon))]
        DynamicSpaceFixedEnd,
    }
    public class DistributionTypePanel : EnumOncePropertyPanel<DistributionType, DistributionTypePanel.DistributionTypeSegmented>
    {
        protected override string GetDescription(DistributionType value) => value.Description();
        protected override bool IsEqual(DistributionType first, DistributionType second) => first == second;

        protected override void FillItems(Func<DistributionType, bool> selector)
        {
            Selector.StopLayout();
            foreach (var value in GetValues())
            {
                if (selector?.Invoke(value) != false)
                {
                    var sprite = value.Sprite();
                    if (string.IsNullOrEmpty(sprite))
                        Selector.AddItem(value, GetDescription(value));
                    else
                        Selector.AddItem(value, GetDescription(value), IMTTextures.Atlas, sprite);
                }
            }
            Selector.StartLayout();
        }

        public class DistributionTypeSegmented : UIOnceSegmented<DistributionType> { }
    }
}
