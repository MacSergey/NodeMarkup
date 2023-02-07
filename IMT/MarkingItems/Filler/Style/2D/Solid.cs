using ColossalFramework.DataBinding;
using ColossalFramework.Math;
using ColossalFramework.UI;
using IMT.API;
using IMT.UI.Editors;
using IMT.Utilities;
using IMT.Utilities.API;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using static IMT.Manager.StyleHelper;

namespace IMT.Manager
{
    public class SolidFillerStyle : FillerStyle, IColorStyle, IEffectStyle
    {
        public static float DefaultSolidWidth { get; } = 0.2f;

        public override StyleType Type => StyleType.FillerSolid;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;

#if DEBUG
        public new PropertyValue<float> MinAngle { get; }
        public new PropertyValue<float> MinLength { get; }
        public new PropertyValue<float> MaxLength { get; }
#endif

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Offset);
                yield return nameof(Texture);
                yield return nameof(Cracks);
                yield return nameof(Voids);
#if DEBUG
                yield return nameof(MinAngle);
                yield return nameof(MinLength);
                yield return nameof(MaxLength);
#endif
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<float>(nameof(LineOffset), LineOffset);
                yield return new StylePropertyDataProvider<float>(nameof(MedianOffset), MedianOffset);
            }
        }

        public SolidFillerStyle(Color32 color, Vector2 cracks, Vector2 voids, float texture, float lineOffset, float medianOffset) : base(color, DefaultSolidWidth, cracks, voids, texture, lineOffset, medianOffset)
        {
#if DEBUG
            MinAngle = new PropertyStructValue<float>(StyleChanged, FillerStyle.MinAngle);
            MinLength = new PropertyStructValue<float>(StyleChanged, FillerStyle.MinLength);
            MaxLength = new PropertyStructValue<float>(StyleChanged, FillerStyle.MaxLength);
#endif
        }

        public override FillerStyle CopyStyle() => new SolidFillerStyle(Color, Cracks, Voids, Texture, LineOffset, DefaultOffset);

        protected override void CalculateImpl(MarkingFiller filler, ContourGroup contours, MarkingLOD lod, Action<IStyleData> addData)
        {
            if ((SupportLOD & lod) != 0)
            {
                foreach (var contour in contours)
                {
                    var trajectories = contour.Select(c => c.trajectory).ToArray();
                    foreach (var data in DecalData.GetData(this, lod, trajectories, MinAngle, MinLength, MaxLength, Color))
                    {
                        addData(data);
                    }
                }
            }
        }

        protected override void GetUIComponents(MarkingFiller filler, EditorProvider provider)
        {
            base.GetUIComponents(filler, provider);
#if DEBUG
            if (!provider.isTemplate && Settings.ShowDebugProperties)
            {
                provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(MinAngle), true, GetMinAngle));
                provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(MinLength), true, GetMinLength));
                provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(MaxLength), true, GetMaxLength));
            }
#endif
        }

#if DEBUG
        private void GetMinAngle(FloatPropertyPanel minAngleProperty, EditorProvider provider)
        {
            minAngleProperty.Text = "Min angle";
            minAngleProperty.CanCollapse = true;
            minAngleProperty.CheckMax = true;
            minAngleProperty.CheckMin = true;
            minAngleProperty.MinValue = 1f;
            minAngleProperty.MaxValue = 90f;
            minAngleProperty.WheelStep = 1f;
            minAngleProperty.UseWheel = true;
            minAngleProperty.Init();
            minAngleProperty.Value = MinAngle;
            minAngleProperty.OnValueChanged += (float value) => MinAngle.Value = value;
        }
        private void GetMinLength(FloatPropertyPanel minLengthProperty, EditorProvider provider)
        {
            minLengthProperty.Text = "Min length";
            minLengthProperty.CanCollapse = true;
            minLengthProperty.CheckMax = true;
            minLengthProperty.CheckMin = true;
            minLengthProperty.MinValue = 0.1f;
            minLengthProperty.MaxValue = 10f;
            minLengthProperty.WheelStep = 0.1f;
            minLengthProperty.UseWheel = true;
            minLengthProperty.Init();
            minLengthProperty.Value = MinLength;
            minLengthProperty.OnValueChanged += (float value) => MinLength.Value = value;
        }
        private void GetMaxLength(FloatPropertyPanel maxLengthProperty, EditorProvider provider)
        {
            maxLengthProperty.Text = "Max length";
            maxLengthProperty.CanCollapse = true;
            maxLengthProperty.CheckMax = true;
            maxLengthProperty.CheckMin = true;
            maxLengthProperty.MinValue = 1f;
            maxLengthProperty.MaxValue = 100f;
            maxLengthProperty.WheelStep = 0.1f;
            maxLengthProperty.UseWheel = true;
            maxLengthProperty.Init();
            maxLengthProperty.Value = MaxLength;
            maxLengthProperty.OnValueChanged += (float value) => MaxLength.Value = value;
        }
#endif
        public override XElement ToXml()
        {
            var config = base.ToXml();
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
        }
    }
}
