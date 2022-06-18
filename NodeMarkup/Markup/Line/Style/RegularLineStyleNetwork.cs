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
    public class NetworkLineStyle : RegularLineStyle, IAsymLine, I3DLine
    {
        public override StyleType Type => StyleType.LineNetwork;

        public override bool CanOverlap => true;

        public PropertyValue<string> Name { get; }
        public PropertyValue<float> Shift { get; }
        public PropertyValue<float> Elevation { get; }
        public PropertyValue<float> OffsetBefore { get; }
        public PropertyValue<float> OffsetAfter { get; }
        public PropertyValue<float> Scale { get; }
        public PropertyValue<int> RepeatDistance { get; }
        public PropertyBoolValue Invert { get; }

        public NetworkLineStyle(string name, float shift, float elevation, float scale, float offsetBefore, float offsetAfter, int repeatDistance, bool invert) : base(new Color32(), 0f)
        {
            Name = new PropertyStringValue("N", StyleChanged, name);
            Shift = new PropertyStructValue<float>("SF", StyleChanged, shift);
            Elevation = new PropertyStructValue<float>("E", StyleChanged, elevation);
            OffsetBefore = new PropertyStructValue<float>("OB", StyleChanged, offsetBefore);
            OffsetAfter = new PropertyStructValue<float>("OA", StyleChanged, offsetAfter);
            Scale = new PropertyStructValue<float>("SC", StyleChanged, scale);
            RepeatDistance = new PropertyStructValue<int>("RD", StyleChanged, repeatDistance);
            Invert = GetInvertProperty(invert);
        }

        public override RegularLineStyle CopyLineStyle() => new NetworkLineStyle(Name, Shift, Elevation, Scale, OffsetBefore, OffsetAfter, RepeatDistance, Invert);

        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is I3DLine line3DTarget)
                line3DTarget.Elevation.Value = Elevation;
            if (target is IAsymLine asymTarget)
                asymTarget.Invert.Value = Invert;
            if (target is NetworkLineStyle networkTarget)
            {
                networkTarget.Name.Value = Name;
                networkTarget.Shift.Value = Shift;
                networkTarget.OffsetBefore.Value = OffsetBefore;
                networkTarget.OffsetAfter.Value = OffsetAfter;
                networkTarget.Scale.Value = Scale;
                networkTarget.RepeatDistance.Value = RepeatDistance;
            }
        }
        protected bool GetProp(out NetInfo info)
        {
            if (PrefabCollection<NetInfo>.FindLoaded(Name) is NetInfo foundInfo && foundInfo.m_segments.Length != 0 && foundInfo.m_netAI is DecorationWallAI)
                info = foundInfo;
            else
                info = null;

            return info != null;
        }

        protected override IStyleData Calculate(MarkupRegularLine line, ITrajectory trajectory, MarkupLOD lod)
        {
            if (!GetProp(out NetInfo info))
                return new MarkupStyleParts();

            if (Invert)
                trajectory = trajectory.Invert();

            if (Shift != 0)
            {
                var startNormal = trajectory.StartDirection.Turn90(!Invert);
                var endNormal = trajectory.EndDirection.Turn90(Invert);

                trajectory = new BezierTrajectory(trajectory.StartPosition + startNormal * Shift, trajectory.StartDirection, trajectory.EndPosition + endNormal * Shift, trajectory.EndDirection);
            }

            var length = trajectory.Length;
            if (OffsetBefore + OffsetAfter >= length)
                return new MarkupStyleParts();

            var startT = OffsetBefore == 0f ? 0f : trajectory.Travel(OffsetBefore);
            var endT = OffsetAfter == 0f ? 1f : 1f - trajectory.Invert().Travel(OffsetAfter);
            trajectory = trajectory.Cut(startT, endT);

            var count = Mathf.CeilToInt(trajectory.Length / RepeatDistance);
            var trajectories = new ITrajectory[count];
            if (count == 1)
                trajectories[0] = trajectory;
            else
            {
                for (int i = 0; i < count; i += 1)
                    trajectories[i] = trajectory.Cut(1f / count * i, 1f / count * (i + 1));
            }

            return new MarkupStyleNetwork(info, trajectories, info.m_halfWidth * 2f, info.m_segmentLength, Scale, Elevation);
        }

        public override void GetUIComponents(MarkupRegularLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);

            var name = AddNameProperty(parent);

            components.Add(name);
            components.Add(AddShiftProperty(parent));
            var elevation = AddElevationProperty(parent);
            components.Add(elevation);
            components.Add(AddScaleProperty(parent));
            components.Add(AddRepeatDistanceProperty(parent));
            components.Add(AddOffsetBeforeProperty(parent));
            components.Add(AddOffsetAfterProperty(parent));
            components.Add(AddInvertProperty(this, parent));

            name.OnValueChanged += OnNameChanged;
            AfterNameChanged();

            void OnNameChanged(string value)
            {
                Name.Value = value;
                AfterNameChanged();
            };
            void AfterNameChanged()
            {
                if (GetProp(out var info))
                {
                    name.FieldTextColor = UnityEngine.Color.green;
                    elevation.isVisible = info.m_segments[0].m_segmentMaterial.shader.name != "Custom/Net/Fence";
                }
                else
                {
                    name.FieldTextColor = UnityEngine.Color.white;
                    elevation.isVisible = true;
                }
            }
        }

        protected StringPropertyPanel AddNameProperty(UIComponent parent)
        {
            var nameProperty = ComponentPool.Get<StringPropertyPanel>(parent, nameof(Name));
            nameProperty.Text = Localize.StyleOption_ObjectName;
            nameProperty.FieldWidth = 230f;
            nameProperty.Init();
            nameProperty.Value = Name;
            return nameProperty;
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
        protected FloatPropertyPanel AddScaleProperty(UIComponent parent)
        {
            var scaleProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(Scale));
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
            scaleProperty.OnValueChanged += (float value) => Scale.Value = value * 0.01f;

            return scaleProperty;
        }
        protected IntPropertyPanel AddRepeatDistanceProperty(UIComponent parent)
        {
            var repeatDistanceProperty = ComponentPool.Get<IntPropertyPanel>(parent, nameof(RepeatDistance));
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
            repeatDistanceProperty.OnValueChanged += (int value) => RepeatDistance.Value = value;

            return repeatDistanceProperty;
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
            Name.ToXml(config);
            Shift.ToXml(config);
            Elevation.ToXml(config);
            Scale.ToXml(config);
            RepeatDistance.ToXml(config);
            OffsetBefore.ToXml(config);
            OffsetAfter.ToXml(config);
            Invert.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Name.FromXml(config, string.Empty);
            Shift.FromXml(config, DefaultObjectShift);
            Elevation.FromXml(config, DefaultObjectElevation);
            Scale.FromXml(config, DefaultNetworkScale);
            RepeatDistance.FromXml(config, DefaultRepeatDistance);
            OffsetBefore.FromXml(config, DefaultObjectOffsetBefore);
            OffsetAfter.FromXml(config, DefaultObjectOffsetAfter);
            Invert.FromXml(config, false);
            Invert.Value ^= map.IsMirror ^ invert;
        }
    }
}
