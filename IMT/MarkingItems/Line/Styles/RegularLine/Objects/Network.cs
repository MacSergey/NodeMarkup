using IMT.API;
using IMT.UI;
using IMT.UI.Editors;
using IMT.Utilities;
using IMT.Utilities.API;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public class NetworkLineStyle : RegularLineStyle, IAsymLine, I3DLine
    {
        public static bool IsValidNetwork(NetInfo info) => info != null && info.m_segments.Length != 0 && info.m_netAI is DecorationWallAI;
        public override StyleType Type => StyleType.LineNetwork;
        public override MarkingLOD SupportLOD => MarkingLOD.NoLOD;

        public override bool CanOverlap => true;
        private bool IsValid => IsValidNetwork(Prefab.Value);

        public PropertyPrefabValue<NetInfo> Prefab { get; }
        public PropertyNullableStructValue<Color32, PropertyColorValue> NetworkColor { get; }
        public PropertyVector2Value Shift { get; }
        public PropertyValue<float> Elevation { get; }
        public PropertyValue<float> OffsetBefore { get; }
        public PropertyValue<float> OffsetAfter { get; }
        public PropertyValue<float> Scale { get; }
        public PropertyValue<int> RepeatDistance { get; }
        public PropertyBoolValue Invert { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Prefab);
                yield return nameof(NetworkColor);
                yield return nameof(Shift);
                yield return nameof(Elevation);
                yield return nameof(Scale);
                yield return nameof(RepeatDistance);
                yield return nameof(Offset);
                yield return nameof(Invert);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<NetInfo>(nameof(Prefab), Prefab);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Shift), Shift);
                yield return new StylePropertyDataProvider<float>(nameof(Elevation), Elevation);
                yield return new StylePropertyDataProvider<float>(nameof(Scale), Scale);
                yield return new StylePropertyDataProvider<int>(nameof(RepeatDistance), RepeatDistance);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetBefore), OffsetBefore);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetAfter), OffsetAfter);
                yield return new StylePropertyDataProvider<bool>(nameof(Invert), Invert);
            }
        }

        public NetworkLineStyle(NetInfo prefab, Color32? color, Vector2 shift, float elevation, float scale, float offsetBefore, float offsetAfter, int repeatDistance, bool invert) : base(default, 0f)
        {
            Prefab = new PropertyPrefabValue<NetInfo>("PRF", StyleChanged, prefab);
            NetworkColor = new PropertyNullableStructValue<Color32, PropertyColorValue>(new PropertyColorValue("NC", null), "NC", StyleChanged, color);
            Shift = new PropertyVector2Value(StyleChanged, shift, "SFA", "SFB");
            Elevation = new PropertyStructValue<float>("E", StyleChanged, elevation);
            OffsetBefore = new PropertyStructValue<float>("OB", StyleChanged, offsetBefore);
            OffsetAfter = new PropertyStructValue<float>("OA", StyleChanged, offsetAfter);
            Scale = new PropertyStructValue<float>("SC", StyleChanged, scale);
            RepeatDistance = new PropertyStructValue<int>("RD", StyleChanged, repeatDistance);
            Invert = GetInvertProperty(invert);
        }

        public override RegularLineStyle CopyLineStyle() => new NetworkLineStyle(Prefab, NetworkColor, Shift, Elevation, Scale, OffsetBefore, OffsetAfter, RepeatDistance, Invert);

        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is I3DLine line3DTarget)
                line3DTarget.Elevation.Value = Elevation;
            if (target is IAsymLine asymTarget)
                asymTarget.Invert.Value = Invert;
            if (target is NetworkLineStyle networkTarget)
            {
                networkTarget.Prefab.Value = Prefab;
                networkTarget.NetworkColor.Value = NetworkColor;
                networkTarget.Shift.Value = Shift;
                networkTarget.OffsetBefore.Value = OffsetBefore;
                networkTarget.OffsetAfter.Value = OffsetAfter;
                networkTarget.Scale.Value = Scale;
                networkTarget.RepeatDistance.Value = RepeatDistance;
            }
        }
        protected override void CalculateImpl(MarkingRegularLine line, ITrajectory trajectory, MarkingLOD lod, Action<IStyleData> addData)
        {
            if (!IsValid)
                return;

            var shift = Shift.Value;
            if (shift != Vector2.zero)
            {
                trajectory = trajectory.Shift(shift.x, shift.y);
            }

            var length = trajectory.Length;
            if (OffsetBefore + OffsetAfter >= length)
                return;

            var startT = OffsetBefore == 0f ? 0f : trajectory.Travel(OffsetBefore);
            var endT = OffsetAfter == 0f ? 1f : 1f - trajectory.Invert().Travel(OffsetAfter);
            if (startT != 0 || endT != 1)
                trajectory = trajectory.Cut(startT, endT);

            if (Invert)
                trajectory = trajectory.Invert();

            var count = Mathf.CeilToInt(trajectory.Length / RepeatDistance);
            var trajectories = new ITrajectory[count];
            if (count == 1)
                trajectories[0] = trajectory;
            else
            {
                for (int i = 0; i < count; i += 1)
                    trajectories[i] = trajectory.Cut(1f / count * i, 1f / count * (i + 1));
            }

            addData(new MarkingNetworkData(Prefab, trajectories, Prefab.Value.m_halfWidth * 2f, Prefab.Value.m_segmentLength, Scale, Elevation, NetworkColor.Value ?? Prefab.Value.m_color));
        }

        protected override void GetUIComponents(MarkingRegularLine line, EditorProvider provider)
        {
            base.GetUIComponents(line, provider);

            provider.AddProperty(new PropertyInfo<SelectNetworkProperty>(this, nameof(Prefab), MainCategory, AddPrefabProperty));
            provider.AddProperty(new PropertyInfo<ColorAdvancedPropertyPanel>(this, nameof(NetworkColor), AdditionalCategory, AddNetworkColorProperty, RefreshNetworkColorProperty));
            provider.AddProperty(new PropertyInfo<FloatSingleDoubleInvertedProperty>(this, nameof(Shift), MainCategory, AddShiftProperty, RefreshShiftProperty));
            provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Elevation), MainCategory, AddElevationProperty, RefreshElevationProperty));
            provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Scale), AdditionalCategory, AddScaleProperty, RefreshScaleProperty));
            provider.AddProperty(new PropertyInfo<IntPropertyPanel>(this, nameof(RepeatDistance), AdditionalCategory, AddRepeatDistanceProperty, RefreshRepeatDistanceProperty));
            provider.AddProperty(new PropertyInfo<Vector2PropertyPanel>(this, nameof(Offset), AdditionalCategory, AddOffsetProperty, RefreshOffsetProperty));
            provider.AddProperty(new PropertyInfo<ButtonPanel>(this, nameof(Invert), MainCategory, AddInvertProperty, RefreshInvertProperty));
        }

        private void AddPrefabProperty(SelectNetworkProperty prefabProperty, EditorProvider provider)
        {
            prefabProperty.Text = Localize.StyleOption_AssetNetwork;
            prefabProperty.PrefabSelectPredicate = IsValidNetwork;
            prefabProperty.PrefabSortPredicate = Utilities.Utilities.GetPrefabName;
            prefabProperty.Init(60f);
            prefabProperty.Prefab = Prefab;
            prefabProperty.OnValueChanged += (value) =>
            {
                var oldPrefab = Prefab.Value;
                Prefab.Value = value;
                if ((oldPrefab == null || NetworkColor.Value == null || NetworkColor.Value != oldPrefab.m_color) && value != null)
                    NetworkColor.Value = value.m_color;

                provider.Refresh();
            };
        }

        private void AddNetworkColorProperty(ColorAdvancedPropertyPanel colorProperty, EditorProvider provider)
        {
            colorProperty.Text = Localize.StyleOption_Color;
            colorProperty.WheelTip = Settings.ShowToolTip;
            colorProperty.Init(Prefab.Value?.m_color);
            colorProperty.Value = NetworkColor.Value ?? Prefab.Value?.m_color ?? new Color32(127, 127, 127, 255);
            colorProperty.OnValueChanged += (Color32 color) => NetworkColor.Value = color;
        }
        private void RefreshNetworkColorProperty(ColorAdvancedPropertyPanel colorProperty, EditorProvider provider)
        {
            colorProperty.IsHidden = !IsValid;

            if (Prefab.Value != null)
                colorProperty.DefaultColor = Prefab.Value.m_color;
        }

        private void AddShiftProperty(FloatSingleDoubleInvertedProperty shiftProperty, EditorProvider provider)
        {
            shiftProperty.Text = Localize.StyleOption_ObjectShift;
            shiftProperty.FieldWidth = 100f;
            shiftProperty.Format = Localize.NumberFormat_Meter;
            shiftProperty.UseWheel = true;
            shiftProperty.WheelStep = 0.1f;
            shiftProperty.WheelTip = Settings.ShowToolTip;
            shiftProperty.CheckMin = true;
            shiftProperty.CheckMax = true;
            shiftProperty.MinValue = -50;
            shiftProperty.MaxValue = 50;
            shiftProperty.Init();
            shiftProperty.SetValues(Shift.Value.x, Shift.Value.y);
            shiftProperty.OnValueChanged += (valueA, valueB) => Shift.Value = new Vector2(valueA, valueB);
        }
        private void RefreshShiftProperty(FloatSingleDoubleInvertedProperty shiftProperty, EditorProvider provider)
        {
            shiftProperty.IsHidden = !IsValid;
        }

        new private void AddElevationProperty(FloatPropertyPanel elevationProperty, EditorProvider provider)
        {
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
            elevationProperty.OnValueChanged += (value) => Elevation.Value = value;
        }
        private void RefreshElevationProperty(FloatPropertyPanel elevationProperty, EditorProvider provider)
        {
            elevationProperty.IsHidden = !IsValid || Prefab.Value.m_segments[0].m_segmentMaterial.shader.name == "Custom/Net/Fence";
        }

        private void AddScaleProperty(FloatPropertyPanel scaleProperty, EditorProvider provider)
        {
            scaleProperty.Text = Localize.StyleOption_NetWidthScale;
            scaleProperty.Format = Localize.NumberFormat_Percent;
            scaleProperty.UseWheel = true;
            scaleProperty.WheelStep = 10f;
            scaleProperty.WheelTip = Settings.ShowToolTip;
            scaleProperty.CheckMin = true;
            scaleProperty.CheckMax = true;
            scaleProperty.MinValue = 1f;
            scaleProperty.MaxValue = 1000f;
            scaleProperty.Init();
            scaleProperty.Value = Scale.Value * 100f;
            scaleProperty.OnValueChanged += (value) => Scale.Value = value * 0.01f;
        }
        private void RefreshScaleProperty(FloatPropertyPanel scaleProperty, EditorProvider provider)
        {
            scaleProperty.IsHidden = !IsValid;
        }

        private void AddRepeatDistanceProperty(IntPropertyPanel repeatDistanceProperty, EditorProvider provider)
        {
            repeatDistanceProperty.Text = Localize.StyleOption_NetRepeatDistance;
            repeatDistanceProperty.Format = Localize.NumberFormat_Meter;
            repeatDistanceProperty.UseWheel = true;
            repeatDistanceProperty.WheelStep = 1;
            repeatDistanceProperty.WheelTip = Settings.ShowToolTip;
            repeatDistanceProperty.CheckMin = true;
            repeatDistanceProperty.CheckMax = true;
            repeatDistanceProperty.MinValue = 1;
            repeatDistanceProperty.MaxValue = 100;
            repeatDistanceProperty.Init();
            repeatDistanceProperty.Value = RepeatDistance.Value;
            repeatDistanceProperty.OnValueChanged += (value) => RepeatDistance.Value = value;
        }
        private void RefreshRepeatDistanceProperty(IntPropertyPanel repeatDistanceProperty, EditorProvider provider)
        {
            repeatDistanceProperty.IsHidden = !IsValid;
        }

        private void AddOffsetProperty(Vector2PropertyPanel offsetProperty, EditorProvider provider)
        {
            offsetProperty.Text = Localize.StyleOption_Offset;
            offsetProperty.FieldsWidth = 50f;
            offsetProperty.SetLabels(Localize.StyleOption_OffsetBeforeAbrv, Localize.StyleOption_OffsetAfterAbrv);
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

        new private void AddInvertProperty(ButtonPanel buttonsPanel, EditorProvider provider)
        {
            buttonsPanel.Text = Localize.StyleOption_InvertNetwork;
            buttonsPanel.Init();
            buttonsPanel.OnButtonClick += () =>
            {
                Invert.Value = !Invert;
            };
        }
        new private void RefreshInvertProperty(ButtonPanel buttonsPanel, EditorProvider provider)
        {
            buttonsPanel.IsHidden = !IsValid;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Prefab.ToXml(config);
            NetworkColor.ToXml(config);
            Shift.ToXml(config);
            Elevation.ToXml(config);
            Scale.ToXml(config);
            RepeatDistance.ToXml(config);
            OffsetBefore.ToXml(config);
            OffsetAfter.ToXml(config);
            Invert.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            Prefab.FromXml(config, null);
            NetworkColor.FromXml(config, Prefab.Value?.m_color);
            Shift.FromXml(config, new Vector2(DefaultObjectShift, DefaultObjectShift));
            if (config.TryGetAttrValue<float>("SF", out var shift))
                Shift.Value = new Vector2(shift, shift);
            Elevation.FromXml(config, DefaultObjectElevation);
            Scale.FromXml(config, DefaultNetworkScale);
            RepeatDistance.FromXml(config, DefaultRepeatDistance);
            OffsetBefore.FromXml(config, DefaultObjectOffsetBefore);
            OffsetAfter.FromXml(config, DefaultObjectOffsetAfter);
            Invert.FromXml(config, false);

            if (invert)
            {
                var offsetBefore = OffsetBefore.Value;
                var offsetAfter = OffsetAfter.Value;
                OffsetBefore.Value = offsetAfter;
                OffsetAfter.Value = offsetBefore;
            }

            if (map.Invert ^ invert ^ typeChanged)
            {
                Invert.Value = !Invert.Value;
                Shift.Value = -Shift.Value;
            }
        }
    }
}
