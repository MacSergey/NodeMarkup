using ColossalFramework.UI;
using IMT.UI;
using IMT.UI.Editors;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.ComponentModel;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public interface IPrefabStyle<PrefabType>
        where PrefabType : PrefabInfo
    {
        public PropertyPrefabValue<PrefabType> Prefab { get; }
        public bool IsValidPrefab(PrefabType prefab);
    }
    public interface IObjectStyle
    {
        PropertyNullableStructValue<float, PropertyStructValue<float>> Step { get; }
        PropertyStructValue<int> Probability { get; }
        PropertyNullableStructValue<Vector2, PropertyVector2Value> Angle { get; }
        PropertyEnumValue<Spread> AngleSpread { get; }
        PropertyVector2Value Shift { get; }
        PropertyEnumValue<Spread> ShiftSpread { get; }
        PropertyStructValue<float> OffsetBefore { get; }
        PropertyStructValue<float> OffsetAfter { get; }

        PropertyEnumValue<DistributionType> Distribution { get; }
        PropertyEnumValue<FixedEndType> FixedEnd { get; }

        PropertyStructValue<int> MinCount { get; }
        PropertyStructValue<int> MaxCount { get; }
    }
    public interface I3DObject
    {
        PropertyVector2Value Tilt { get; }
        PropertyEnumValue<Spread> TiltSpread { get; }
        PropertyNullableStructValue<Vector2, PropertyVector2Value> Slope { get; }
        PropertyEnumValue<Spread> SlopeSpread { get; }
        PropertyVector2Value Scale { get; }
        PropertyEnumValue<Spread> ScaleSpread { get; }
        PropertyVector2Value Elevation { get; }
        PropertyEnumValue<Spread> ElevationSpread { get; }
    }

    public abstract class BaseObjectStyle<PrefabType, SelectPrefabType> : RegularLineStyle, IObjectStyle, IPrefabStyle<PrefabType>
        where PrefabType : PrefabInfo
        where SelectPrefabType : EditorPropertyPanel, ISelectPrefabProperty<PrefabType>
    {
        public override bool CanOverlap => true;

        public PropertyPrefabValue<PrefabType> Prefab { get; }
        public PropertyNullableStructValue<float, PropertyStructValue<float>> Step { get; }

        public PropertyStructValue<int> Probability { get; }
        public PropertyNullableStructValue<Vector2, PropertyVector2Value> Angle { get; }
        public PropertyEnumValue<Spread> AngleSpread { get; }
        public PropertyVector2Value Shift { get; }
        public PropertyEnumValue<Spread> ShiftSpread { get; }
        public PropertyStructValue<float> OffsetBefore { get; }
        public PropertyStructValue<float> OffsetAfter { get; }

        public PropertyEnumValue<DistributionType> Distribution { get; }
        public PropertyEnumValue<FixedEndType> FixedEnd { get; }

        public PropertyStructValue<int> MinCount { get; }
        public PropertyStructValue<int> MaxCount { get; }
        protected bool EnableCount => MinCount != -1 && MaxCount != -1;

        protected virtual bool IsValid => IsValidPrefab(Prefab.Value);
        protected abstract Vector3 PrefabSize { get; }
        protected abstract string AssetPropertyName { get; }

        public BaseObjectStyle(PrefabType prefab, int probability, float? step, Vector2? angle, Spread angleSpread, Vector2 shift, Spread shiftSpread, float offsetBefore, float offsetAfter, DistributionType distribution, FixedEndType fixedEnd, int minCount, int maxCount) : base(default, default)
        {
            Prefab = new PropertyPrefabValue<PrefabType>("PRF", StyleChanged, prefab);
            Step = new PropertyNullableStructValue<float, PropertyStructValue<float>>(new PropertyStructValue<float>("S", null), "S", StyleChanged, step);
            Probability = new PropertyStructValue<int>("P", StyleChanged, probability);
            Angle = new PropertyNullableStructValue<Vector2, PropertyVector2Value>(new PropertyVector2Value(null, labelX: "AA", labelY: "AB"), "A", StyleChanged, angle);
            AngleSpread = new PropertyEnumValue<Spread>("ASP", StyleChanged, angleSpread);
            Shift = new PropertyVector2Value(StyleChanged, shift, "SFA", "SFB");
            ShiftSpread = new PropertyEnumValue<Spread>("SFSP", StyleChanged, shiftSpread);
            OffsetBefore = new PropertyStructValue<float>("OB", StyleChanged, offsetBefore);
            OffsetAfter = new PropertyStructValue<float>("OA", StyleChanged, offsetAfter);
            Distribution = new PropertyEnumValue<DistributionType>("PT", StyleChanged, distribution);
            FixedEnd = new PropertyEnumValue<FixedEndType>("FE", StyleChanged, fixedEnd);
            MinCount = new PropertyStructValue<int>("MNC", StyleChanged, minCount);
            MaxCount = new PropertyStructValue<int>("MXC", StyleChanged, maxCount);
        }

        public abstract bool IsValidPrefab(PrefabType info);
        protected abstract int SortPredicate(PrefabType objA, PrefabType objB);

        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is IPrefabStyle<PrefabType> prefabTarget)
            {
                if (prefabTarget.IsValidPrefab(Prefab))
                    prefabTarget.Prefab.Value = Prefab;
            }
            if (target is IObjectStyle objectTarget)
            {
                objectTarget.Probability.Value = Probability;
                objectTarget.Step.Value = Step;
                objectTarget.Angle.Value = Angle;
                objectTarget.AngleSpread.Value = AngleSpread;
                objectTarget.Shift.Value = Shift;
                objectTarget.ShiftSpread.Value = ShiftSpread;
                objectTarget.OffsetBefore.Value = OffsetBefore;
                objectTarget.OffsetAfter.Value = OffsetAfter;
                objectTarget.Distribution.Value = Distribution;
                objectTarget.FixedEnd.Value = FixedEnd;
                objectTarget.MinCount.Value = MinCount;
                objectTarget.MaxCount.Value = MaxCount;
            }
        }

        protected override void CalculateImpl(MarkingRegularLine line, ITrajectory trajectory, MarkingLOD lod, Action<IStyleData> addData)
        {
            if (Prefab.Value is not PrefabType prefab)
                return;

            var middleShift = (Shift.Value.x + Shift.Value.y) * 0.5f;
            if (middleShift != 0)
                trajectory = trajectory.Shift(middleShift, middleShift);

            var length = trajectory.Length;
            if (OffsetBefore + OffsetAfter >= length)
                return;

            var startT = OffsetBefore == 0f ? 0f : trajectory.Travel(OffsetBefore);
            var endT = OffsetAfter == 0f ? 1f : 1f - trajectory.Invert().Travel(OffsetAfter);
            trajectory = trajectory.Cut(startT, endT);

            length = trajectory.Length;
            var stepValue = Step.HasValue ? Step.Value.Value : PrefabSize.x;

            MarkingObjectItemData[] items;
            int startIndex;
            int count;
            float startOffset;

            switch (Distribution.Value)
            {
                case DistributionType.FixedSpaceFreeEnd:
                    {
                        startIndex = 0;
                        count = Mathf.CeilToInt(length / stepValue);
                        if (EnableCount)
                            count = Mathf.Clamp(count, MinCount, MaxCount);
                        startOffset = (length - (count - 1) * stepValue) * 0.5f;
                        items = new MarkingObjectItemData[count];
                        break;
                    }
                case DistributionType.FixedSpaceFixedEnd:
                    {
                        startIndex = 1;
                        count = Math.Max(Mathf.RoundToInt(length / stepValue - 1.5f), 0);
                        if (EnableCount)
                            count = Mathf.Clamp(count, MinCount, MaxCount);
                        startOffset = (length - (count - 1) * stepValue) * 0.5f;
                        items = new MarkingObjectItemData[count + 2];

                        if (FixedEnd.Value == FixedEndType.Both || FixedEnd.Value == FixedEndType.Start)
                            CalculateItem(trajectory, 0f, 0f, prefab, ref items[0]);
                        if (FixedEnd.Value == FixedEndType.Both || FixedEnd.Value == FixedEndType.End)
                            CalculateItem(trajectory, 1f, 1f, prefab, ref items[items.Length - 1]);
                        break;
                    }
                case DistributionType.DynamicSpaceFreeEnd:
                    {
                        startIndex = 0;
                        count = Mathf.RoundToInt(length / stepValue);
                        if (EnableCount)
                            count = Mathf.Clamp(count, MinCount, MaxCount);
                        stepValue = length / count;
                        startOffset = (length - (count - 1) * stepValue) * 0.5f;
                        items = new MarkingObjectItemData[count];
                        break;
                    }
                case DistributionType.DynamicSpaceFixedEnd:
                    {
                        startIndex = 1;
                        count = Math.Max(Mathf.RoundToInt(length / stepValue) - 1, 0);
                        if (EnableCount)
                            count = Mathf.Clamp(count, MinCount, MaxCount);
                        stepValue = length / (count + 1);
                        startOffset = stepValue;
                        items = new MarkingObjectItemData[count + 2];

                        if (FixedEnd.Value == FixedEndType.Both || FixedEnd.Value == FixedEndType.Start)
                            CalculateItem(trajectory, 0f, 0f, prefab, ref items[0]);
                        if (FixedEnd.Value == FixedEndType.Both || FixedEnd.Value == FixedEndType.End)
                            CalculateItem(trajectory, 1f, 1f, prefab, ref items[items.Length - 1]);
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
                float p;
                if (count == 1)
                {
                    t = 0.5f;
                    p = 0.5f;
                }
                else
                {
                    var distance = startOffset + stepValue * i;
                    t = trajectory.Travel(distance);
                    p = distance / length;
                }

                CalculateItem(trajectory, t, p, prefab, ref items[i + startIndex]);
            }

            AddData(prefab, items, lod, addData);
        }
        protected virtual void CalculateItem(ITrajectory trajectory, float t, float p, PrefabType prefab, ref MarkingObjectItemData item)
        {
            item.position = trajectory.Position(t);

            var shiftMiddle = (Shift.Value.y - Shift.Value.x) * 0.5f;
            var shift = ShiftSpread.Value switch
            { 
                Spread.Random => SimulationManager.instance.m_randomizer.UInt32((uint)(Mathf.Abs(Shift.Value.y - Shift.Value.x) * 1000f)) * 0.001f - shiftMiddle,
                Spread.Slope => Mathf.Lerp(Shift.Value.x, Shift.Value.y, p),
                _ => 0f
            };
            item.position += trajectory.Tangent(t).Turn90(true).MakeFlatNormalized() * shift;

  
            item.absoluteAngle = trajectory.Tangent(t).AbsoluteAngle();
            item.angle = AngleSpread.Value switch
            {
                Spread.Random => GetRandomAngle(),
                Spread.Slope when Angle.HasValue => Mathf.Lerp(Angle.Value.Value.x, Angle.Value.Value.y, p),
                _ => 0f,
            } * Mathf.Deg2Rad;

            float GetRandomAngle()
            {
                var xAngle = Angle.HasValue ? Angle.Value.Value.x : -180f;
                var yAngle = Angle.HasValue ? Angle.Value.Value.y : 180f;

                return Mathf.Min(xAngle, yAngle) + SimulationManager.instance.m_randomizer.UInt32((uint)Mathf.Abs(yAngle - xAngle));
            }
        }
        protected abstract void AddData(PrefabType prefab, MarkingObjectItemData[] items, MarkingLOD lod, Action<IStyleData> addData);

        protected override void GetUIComponents(MarkingRegularLine line, EditorProvider provider)
        {
            base.GetUIComponents(line, provider);
            provider.AddProperty(new PropertyInfo<SelectPrefabType>(this, nameof(Prefab), MainCategory, AddPrefabProperty));
            provider.AddProperty(new PropertyInfo<IntPropertyPanel>(this, nameof(Probability), AdditionalCategory, AddProbabilityProperty, RefreshProbabilityProperty));
            provider.AddProperty(new PropertyInfo<FloatStaticAutoProperty>(this, nameof(Step), MainCategory, AddStepProperty, RefreshStepProperty));
            provider.AddProperty(new PropertyInfo<FloatStaticRangeProperty>(this, nameof(Shift), MainCategory, AddShiftProperty, RefreshShiftProperty));
            provider.AddProperty(new PropertyInfo<FloatStaticRangeRandomProperty>(this, nameof(Angle), MainCategory, AddAngleRangeProperty, RefreshAngleRangeProperty));
            provider.AddProperty(new PropertyInfo<Vector2PropertyPanel>(this, nameof(Offset), AdditionalCategory, AddOffsetProperty, RefreshOffsetProperty));
            provider.AddProperty(new PropertyInfo<DistributionTypePanel>(this, nameof(Distribution), AdditionalCategory, AddDistributionProperty, RefreshDistributionProperty));
            provider.AddProperty(new PropertyInfo<FixedEndTypePanel>(this, nameof(FixedEnd), AdditionalCategory, AddFixedEndProperty, RefreshFixedEndProperty));
            provider.AddProperty(new PropertyInfo<MinMaxProperty>(this, nameof(EnableCount), AdditionalCategory, AddMinMaxCountProperty, RefreshMinMaxCountProperty));
        }

        protected void AddPrefabProperty(SelectPrefabType prefabProperty, EditorProvider provider)
        {
            prefabProperty.Label = AssetPropertyName;
            prefabProperty.SelectPredicate = IsValidPrefab;
            prefabProperty.SortPredicate = SortPredicate;
            prefabProperty.Init();
            prefabProperty.Prefab = Prefab;
            prefabProperty.RawName = Prefab.RawName;
            prefabProperty.OnValueChanged += (newPrefab) =>
                {
                    var oldPrefab = Prefab.Value;
                    Prefab.Value = newPrefab;
                    OnPrefabValueChanged(oldPrefab, newPrefab);
                    provider.Refresh();
                };
        }
        protected virtual void OnPrefabValueChanged(PrefabType oldPrefab, PrefabType newPrefab)
        {
            if (!Step.HasValue)
                Step.Value = IsValid ? PrefabSize.x : DefaultObjectStep;
        }
        private void AddProbabilityProperty(IntPropertyPanel probabilityProperty, EditorProvider provider)
        {
            probabilityProperty.Label = Localize.StyleOption_ObjectProbability;
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
            probabilityProperty.OnValueChanged += (value) => Probability.Value = value;
        }
        private void RefreshProbabilityProperty(IntPropertyPanel probabilityProperty, EditorProvider provider)
        {
            probabilityProperty.IsHidden = !IsValid;
        }

        private void AddStepProperty(FloatStaticAutoProperty stepProperty, EditorProvider provider)
        {
            stepProperty.Label = Localize.StyleOption_ObjectStep;
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

            stepProperty.OnValueChanged += (value) => Step.Value = value;
            stepProperty.OnAutoValue += () =>
            {
                Step.Value = null;

                if (IsValid)
                    stepProperty.Value = PrefabSize.x;
            };
        }
        private void RefreshStepProperty(FloatStaticAutoProperty stepProperty, EditorProvider provider)
        {
            stepProperty.IsHidden = !IsValid;
            if (Step.HasValue)
                stepProperty.SetValue(Step.Value.Value);
            else
                stepProperty.SetAuto();
        }

        private void AddAngleRangeProperty(FloatStaticRangeRandomProperty angleProperty, EditorProvider provider)
        {
            angleProperty.Label = Localize.StyleOption_ObjectAngle;
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

            if (Angle.HasValue)
                angleProperty.SetValues(Angle.Value.Value.x, Angle.Value.Value.y);
            else
                angleProperty.SetRandom();

            angleProperty.SetSpread(AngleSpread.Value);

            angleProperty.OnValueChanged += (valueA, valueB) => Angle.Value = new Vector2(valueA, valueB);
            angleProperty.OnRandomValue += () => Angle.Value = null;
            angleProperty.OnSpreadChanged += value => AngleSpread.Value = value;
        }
        private void RefreshAngleRangeProperty(FloatStaticRangeRandomProperty angleProperty, EditorProvider provider)
        {
            angleProperty.IsHidden = !IsValid;
        }

        private void AddShiftProperty(FloatStaticRangeProperty shiftProperty, EditorProvider provider)
        {
            shiftProperty.Label = Localize.StyleOption_ObjectShift;
            shiftProperty.Format = Localize.NumberFormat_Meter;
            shiftProperty.UseWheel = true;
            shiftProperty.WheelStep = 0.1f;
            shiftProperty.WheelTip = Settings.ShowToolTip;
            shiftProperty.CheckMin = true;
            shiftProperty.CheckMax = true;
            shiftProperty.MinValue = -50;
            shiftProperty.MaxValue = 50;
            shiftProperty.AllowInvert = true;
            shiftProperty.CyclicalValue = false;
            shiftProperty.Init();
            shiftProperty.SetValues(Shift.Value.x, Shift.Value.y);
            shiftProperty.SetSpread(ShiftSpread.Value);
            shiftProperty.OnValueChanged += (valueA, valueB) => Shift.Value = new Vector2(valueA, valueB);
            shiftProperty.OnSpreadChanged += value => ShiftSpread.Value = value;
        }
        private void RefreshShiftProperty(FloatStaticRangeProperty shiftProperty, EditorProvider provider)
        {
            shiftProperty.IsHidden = !IsValid;
        }

        private void AddOffsetProperty(Vector2PropertyPanel offsetProperty, EditorProvider provider)
        {
            offsetProperty.Label = Localize.StyleOption_Offset;
            offsetProperty.SetLabels(Localize.StyleOption_OffsetBeforeAbrv, Localize.StyleOption_OffsetAfterAbrv);
            offsetProperty.FieldsWidth = 50f;
            offsetProperty.Format = Localize.NumberFormat_Meter;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = new Vector2(0.1f, 0.1f);
            offsetProperty.WheelTip = Settings.ShowToolTip;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = Vector2.zero;
            offsetProperty.Init(0, 1);
            offsetProperty.Value = new Vector2(OffsetBefore, OffsetAfter);
            offsetProperty.OnValueChanged += (value) =>
            {
                OffsetBefore.Value = value.x;
                OffsetAfter.Value = value.y;
            };
        }
        private void RefreshOffsetProperty(Vector2PropertyPanel offsetProperty, EditorProvider provider)
        {
            offsetProperty.IsHidden = !IsValid;
        }

        private void AddDistributionProperty(DistributionTypePanel distributionProperty, EditorProvider provider)
        {
            distributionProperty.Label = Localize.StyleOption_Distribution;
            distributionProperty.Selector.AutoButtonSize = false;
            distributionProperty.Selector.ButtonWidth = 57f;
            distributionProperty.Selector.Atlas = IMTTextures.Atlas;
            distributionProperty.Init();
            distributionProperty.SelectedObject = Distribution;
            distributionProperty.OnSelectObjectChanged += (value) =>
            {
                Distribution.Value = value;
                provider.Refresh();
            };
        }
        private void RefreshDistributionProperty(DistributionTypePanel distributionProperty, EditorProvider provider)
        {
            distributionProperty.IsHidden = !IsValid;
        }

        private void AddFixedEndProperty(FixedEndTypePanel fixedEndProperty, EditorProvider provider)
        {
            fixedEndProperty.Label = Localize.StyleOption_FixedEnd;
            fixedEndProperty.Selector.AutoButtonSize = true;
            fixedEndProperty.Selector.Atlas = IMTTextures.Atlas;
            fixedEndProperty.Init();
            fixedEndProperty.SelectedObject = FixedEnd;
            fixedEndProperty.OnSelectObjectChanged += (value) => FixedEnd.Value = value;
        }
        private void RefreshFixedEndProperty(FixedEndTypePanel fixedEndProperty, EditorProvider provider)
        {
            fixedEndProperty.IsHidden = !IsValid || Distribution.Value == DistributionType.DynamicSpaceFreeEnd || Distribution.Value == DistributionType.FixedSpaceFreeEnd;
        }

        private void AddMinMaxCountProperty(MinMaxProperty minMaxProperty, EditorProvider provider)
        {
            minMaxProperty.Label = Localize.StyleOption_ObjectLimits;
            minMaxProperty.MinRange = 0;
            minMaxProperty.MaxRange = 1000;
            minMaxProperty.MinValue = MinCount;
            minMaxProperty.MaxValue = MaxCount;
            minMaxProperty.EnableCount = EnableCount;
            minMaxProperty.UseWheel = true;
            minMaxProperty.WheelStep = 1;
            minMaxProperty.WheelTip = Settings.ShowToolTip;
            minMaxProperty.Init();
            minMaxProperty.OnValueChanged += (enable, min, max) =>
            {
                if (enable)
                {
                    MinCount.Value = min;
                    MaxCount.Value = max;
                }
                else
                {
                    MinCount.Value = -1;
                    MaxCount.Value = -1;
                }
            };
        }
        private void RefreshMinMaxCountProperty(MinMaxProperty minMaxProperty, EditorProvider provider)
        {
            minMaxProperty.IsHidden = !IsValid;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Prefab.ToXml(config);
            Probability.ToXml(config);
            Step.ToXml(config);
            Angle.ToXml(config);
            AngleSpread.ToXml(config);
            Shift.ToXml(config);
            ShiftSpread.ToXml(config);
            OffsetBefore.ToXml(config);
            OffsetAfter.ToXml(config);
            Distribution.ToXml(config);
            FixedEnd.ToXml(config);
            MinCount.ToXml(config);
            MaxCount.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            Prefab.FromXml(config, null);
            Probability.FromXml(config, DefaultObjectProbability);
            Step.FromXml(config, DefaultObjectStep);
            Angle.FromXml(config, new Vector2(DefaultObjectAngle, DefaultObjectAngle));
            AngleSpread.FromXml(config, DefaultObjectSpread);
            Shift.FromXml(config, new Vector2(DefaultObjectShift, DefaultObjectShift));
            ShiftSpread.FromXml(config, DefaultObjectSpread);
            if (config.TryGetAttrValue<float>("SF", out var shift))
                Shift.Value = new Vector2(shift, shift);
            OffsetBefore.FromXml(config, DefaultObjectOffsetBefore);
            OffsetAfter.FromXml(config, DefaultObjectOffsetAfter);
            Distribution.FromXml(config, DistributionType.FixedSpaceFreeEnd);
            FixedEnd.FromXml(config, FixedEndType.Both);
            MinCount.FromXml(config, -1);
            MaxCount.FromXml(config, -1);

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
                if (Angle.Value.HasValue)
                {
                    var angle = Angle.Value.Value;
                    var angleX = angle.x > 0 ? angle.x - 180 : angle.x + 180;
                    var angleY = angle.y > 0 ? angle.y - 180 : angle.y + 180;
                    Angle.Value = new Vector2(angleX, angleY);
                }
            }
        }
    }
    public abstract class BaseObject3DObjectStyle<PrefabType, SelectPrefabType> : BaseObjectStyle<PrefabType, SelectPrefabType>, I3DObject
        where PrefabType : PrefabInfo
        where SelectPrefabType : EditorPropertyPanel, ISelectPrefabProperty<PrefabType>
    {
        public PropertyVector2Value Tilt { get; }
        public PropertyEnumValue<Spread> TiltSpread { get; }
        public PropertyNullableStructValue<Vector2, PropertyVector2Value> Slope { get; }
        public PropertyEnumValue<Spread> SlopeSpread { get; }
        public PropertyVector2Value Scale { get; }
        public PropertyEnumValue<Spread> ScaleSpread { get; }
        public PropertyVector2Value Elevation { get; }
        public PropertyEnumValue<Spread> ElevationSpread { get; }

        public BaseObject3DObjectStyle(PrefabType prefab, int probability, float? step, Vector2? angle, Spread angleSpread, Vector2 shift, Spread shiftSpread, float offsetBefore, float offsetAfter, DistributionType distribution, FixedEndType fixedEnd, int minCount, int maxCount, Vector2 tilt, Spread tiltSpread, Vector2? slope, Spread slopeSpread, Vector2 scale, Spread scaleSpread, Vector2 elevation, Spread elevationSpread) : base(prefab, probability, step, angle, angleSpread, shift, shiftSpread, offsetBefore, offsetAfter, distribution, fixedEnd, minCount, maxCount)
        {
            Scale = new PropertyVector2Value(StyleChanged, scale, "SCA", "SCB");
            ScaleSpread = new PropertyEnumValue<Spread>("SCSP", StyleChanged, scaleSpread);
            Tilt = new PropertyVector2Value(StyleChanged, tilt, "TLA", "TLB");
            TiltSpread = new PropertyEnumValue<Spread>("TLSP", StyleChanged, tiltSpread);
            Slope = new PropertyNullableStructValue<Vector2, PropertyVector2Value>(new PropertyVector2Value(null, labelX: "SLA", labelY: "SLB"), "SL", StyleChanged, slope);
            SlopeSpread = new PropertyEnumValue<Spread>("SLSP", StyleChanged, slopeSpread);
            Elevation = new PropertyVector2Value(StyleChanged, elevation, "EA", "EB");
            ElevationSpread = new PropertyEnumValue<Spread>("ESP", StyleChanged, elevationSpread);
        }

        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);

            if (target is I3DObject target3DObject)
            {
                target3DObject.Tilt.Value = Tilt;
                target3DObject.TiltSpread.Value = TiltSpread;
                target3DObject.Slope.Value = Slope;
                target3DObject.SlopeSpread.Value = SlopeSpread;
                target3DObject.Scale.Value = Scale;
                target3DObject.ScaleSpread.Value = ScaleSpread;
                target3DObject.Elevation.Value = Elevation;
                target3DObject.ElevationSpread.Value = ElevationSpread;
            }
        }

        protected override void CalculateItem(ITrajectory trajectory, float t, float p, PrefabType prefab, ref MarkingObjectItemData item)
        {
            base.CalculateItem(trajectory, t, p, prefab, ref item);

            item.position.y += ElevationSpread.Value switch
            {
                Spread.Random => Mathf.Min(Elevation.Value.x, Elevation.Value.y) + SimulationManager.instance.m_randomizer.UInt32((uint)(Mathf.Abs(Elevation.Value.y - Elevation.Value.x) * 1000f)) * 0.001f,
                Spread.Slope => Mathf.Lerp(Elevation.Value.x, Elevation.Value.y, p),
                _ => 0f,
            };

            item.tilt = TiltSpread.Value switch
            {
                Spread.Random => Mathf.Min(Tilt.Value.x, Tilt.Value.y) + SimulationManager.instance.m_randomizer.UInt32((uint)Mathf.Abs(Tilt.Value.y - Tilt.Value.x)),
                Spread.Slope => Mathf.Lerp(Tilt.Value.x, Tilt.Value.y, p),
                _ => 0f,
            } * Mathf.Deg2Rad;

            if (Slope.HasValue)
            {
                var slopeValue = Slope.Value.Value;
                item.slope = SlopeSpread.Value switch
                {
                    Spread.Random => Mathf.Min(slopeValue.x + slopeValue.y) + SimulationManager.instance.m_randomizer.UInt32((uint)Mathf.Abs(slopeValue.y - slopeValue.x)),
                    Spread.Slope => Mathf.Lerp(slopeValue.x, slopeValue.y, p),
                    _ => 0f,
                } * Mathf.Deg2Rad;
            }
            else
            {
                var direction = trajectory.Tangent(t);
                var flatDirection = direction.MakeFlat();
                item.slope = Mathf.Sign(direction.y) * Vector3.Angle(flatDirection, direction) * Mathf.Deg2Rad;
            }

            item.scale = ScaleSpread.Value switch
            { 
                Spread.Random => Mathf.Min(Scale.Value.x, Scale.Value.y) + SimulationManager.instance.m_randomizer.UInt32((uint)(Mathf.Abs(Scale.Value.y - Scale.Value.x) * 1000f)) * 0.001f,
                Spread.Slope => Mathf.Lerp(Scale.Value.x, Scale.Value.y, p),
                _ => 0f,
            };
        }

        protected override void GetUIComponents(MarkingRegularLine line, EditorProvider provider)
        {
            base.GetUIComponents(line, provider);

            provider.AddProperty(new PropertyInfo<FloatStaticRangeProperty>(this, nameof(Scale), AdditionalCategory, AddScaleRangeProperty, RefreshScaleRangeProperty));
            provider.AddProperty(new PropertyInfo<FloatStaticRangeProperty>(this, nameof(Elevation), MainCategory, AddElevationProperty, RefreshElevationProperty));
            provider.AddProperty(new PropertyInfo<FloatStaticRangeProperty>(this, nameof(Tilt), AdditionalCategory, AddTiltRangeProperty, RefreshTiltRangeProperty));
            provider.AddProperty(new PropertyInfo<FloatStaticRangeAutoProperty>(this, nameof(Slope), AdditionalCategory, AddSlopeRangeProperty, RefreshSlopeRangeProperty));
        }
        private void AddTiltRangeProperty(FloatStaticRangeProperty tiltProperty, EditorProvider provider)
        {
            tiltProperty.Label = Localize.StyleOption_Tilt;
            tiltProperty.Format = Localize.NumberFormat_Degree;
            tiltProperty.UseWheel = true;
            tiltProperty.WheelStep = 1f;
            tiltProperty.WheelTip = Settings.ShowToolTip;
            tiltProperty.CheckMin = true;
            tiltProperty.CheckMax = true;
            tiltProperty.MinValue = -90;
            tiltProperty.MaxValue = 90;
            tiltProperty.AllowInvert = true;
            tiltProperty.CyclicalValue = false;
            tiltProperty.Init();
            tiltProperty.SetValues(Tilt.Value.x, Tilt.Value.y);
            tiltProperty.SetSpread(TiltSpread.Value);
            tiltProperty.OnValueChanged += (valueA, valueB) => Tilt.Value = new Vector2(valueA, valueB);
            tiltProperty.OnSpreadChanged += value => TiltSpread.Value = value;
        }
        private void RefreshTiltRangeProperty(FloatStaticRangeProperty tiltProperty, EditorProvider provider)
        {
            tiltProperty.IsHidden = !IsValid;
        }

        private void AddSlopeRangeProperty(FloatStaticRangeAutoProperty slopeProperty, EditorProvider provider)
        {
            slopeProperty.Label = Localize.StyleOption_Slope;
            slopeProperty.Format = Localize.NumberFormat_Degree;
            slopeProperty.UseWheel = true;
            slopeProperty.WheelStep = 1f;
            slopeProperty.WheelTip = Settings.ShowToolTip;
            slopeProperty.CheckMin = true;
            slopeProperty.CheckMax = true;
            slopeProperty.MinValue = -90;
            slopeProperty.MaxValue = 90;
            slopeProperty.AllowInvert = true;
            slopeProperty.CyclicalValue = false;
            slopeProperty.Init();

            if (Slope.HasValue)
            {
                slopeProperty.SetValues(Slope.Value.Value.x, Slope.Value.Value.y);
                slopeProperty.SetSpread(SlopeSpread.Value);
            }
            else
                slopeProperty.SetAuto();

            slopeProperty.OnValueChanged += (valueA, valueB) => Slope.Value = new Vector2(valueA, valueB);
            slopeProperty.OnAutoValue += () => Slope.Value = null;
            slopeProperty.OnSpreadChanged += value => SlopeSpread.Value = value;
        }
        private void RefreshSlopeRangeProperty(FloatStaticRangeAutoProperty slopeProperty, EditorProvider provider)
        {
            slopeProperty.IsHidden = !IsValid;
        }

        private void AddScaleRangeProperty(FloatStaticRangeProperty scaleProperty, EditorProvider provider)
        {
            scaleProperty.Label = Localize.StyleOption_ObjectScale;
            scaleProperty.Format = Localize.NumberFormat_Percent;
            scaleProperty.UseWheel = true;
            scaleProperty.WheelStep = 1f;
            scaleProperty.WheelTip = Settings.ShowToolTip;
            scaleProperty.CheckMin = true;
            scaleProperty.CheckMax = true;
            scaleProperty.MinValue = 1f;
            scaleProperty.MaxValue = 500f;
            scaleProperty.AllowInvert = true;
            scaleProperty.CyclicalValue = false;
            scaleProperty.Init();
            scaleProperty.SetValues(Scale.Value.x * 100f, Scale.Value.y * 100f);
            scaleProperty.SetSpread(ScaleSpread.Value);
            scaleProperty.OnValueChanged += (valueA, valueB) => Scale.Value = new Vector2(valueA, valueB) * 0.01f;
            scaleProperty.OnSpreadChanged += value => ScaleSpread.Value = value;
        }
        private void RefreshScaleRangeProperty(FloatStaticRangeProperty scaleProperty, EditorProvider provider)
        {
            scaleProperty.IsHidden = !IsValid;
        }

        private void AddElevationProperty(FloatStaticRangeProperty elevationProperty, EditorProvider provider)
        {
            elevationProperty.Label = Localize.LineStyle_Elevation;
            elevationProperty.Format = Localize.NumberFormat_Meter;
            elevationProperty.UseWheel = true;
            elevationProperty.WheelStep = 0.1f;
            elevationProperty.WheelTip = Settings.ShowToolTip;
            elevationProperty.CheckMin = true;
            elevationProperty.CheckMax = true;
            elevationProperty.MinValue = -100;
            elevationProperty.MaxValue = 100;
            elevationProperty.AllowInvert = true;
            elevationProperty.CyclicalValue = false;
            elevationProperty.Init();
            elevationProperty.SetValues(Elevation.Value.x, Elevation.Value.y);
            elevationProperty.SetSpread(ElevationSpread.Value);
            elevationProperty.OnValueChanged += (valueA, valueB) => Elevation.Value = new Vector2(valueA, valueB);
            elevationProperty.OnSpreadChanged += value => ElevationSpread.Value = value;
        }
        private void RefreshElevationProperty(FloatStaticRangeProperty elevationProperty, EditorProvider provider)
        {
            elevationProperty.IsHidden = !IsValid;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Tilt.ToXml(config);
            Slope.ToXml(config);
            Scale.ToXml(config);
            Elevation.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            Tilt.FromXml(config, new Vector2(DefaultObjectAngle, DefaultObjectAngle));
            Slope.FromXml(config, new Vector2(DefaultObjectAngle, DefaultObjectAngle));
            Scale.FromXml(config, new Vector2(DefaultObjectAngle, DefaultObjectAngle));
            Elevation.FromXml(config, new Vector2(DefaultObjectElevation, DefaultObjectElevation));
            if (config.TryGetAttrValue<float>("E", out var elevation))
                Elevation.Value = new Vector2(elevation, elevation);
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
            Selector.PauseLayout(() =>
            {
                foreach (var value in GetValues())
                {
                    if (selector?.Invoke(value) != false)
                    {
                        var sprite = value.Sprite();
                        if (string.IsNullOrEmpty(sprite))
                            Selector.AddItem(value, new OptionData(GetDescription(value)));
                        else
                            Selector.AddItem(value, new OptionData(GetDescription(value), IMTTextures.Atlas, sprite));
                    }
                }
            });
        }
        public override void SetStyle(ControlStyle style)
        {
            Selector.SegmentedStyle = style.Segmented;
        }

        public class DistributionTypeSegmented : UIOnceSegmented<DistributionType> { }
    }

    public enum FixedEndType
    {
        [Description(nameof(Localize.StyleOption_FixedEndBoth))]
        Both,

        [Description(nameof(Localize.StyleOption_FixedEndStart))]
        Start,

        [Description(nameof(Localize.StyleOption_FixedEndEnd))]
        End,
    }

    public enum Spread
    {
        [Description(nameof(Localize.StyleOption_ObjectSpreadRandom))]
        Random,

        [Description(nameof(Localize.StyleOption_ObjectSpreadSlope))]
        Slope,
    }

    public class FixedEndTypePanel : EnumOncePropertyPanel<FixedEndType, FixedEndTypePanel.FixedEndTypeSegmented>
    {
        protected override string GetDescription(FixedEndType value) => value.Description();
        protected override bool IsEqual(FixedEndType first, FixedEndType second) => first == second;
        public override void SetStyle(ControlStyle style)
        {
            Selector.SegmentedStyle = style.Segmented;
        }

        public class FixedEndTypeSegmented : UIOnceSegmented<FixedEndType> { }
    }
}
