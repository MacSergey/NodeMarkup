using ColossalFramework.Math;
using ColossalFramework.UI;
using IMT.API;
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

namespace IMT.Manager
{
    public class SolidFillerStyle : FillerStyle, IColorStyle, ITexture
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
                yield return nameof(Scratches);
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

        public SolidFillerStyle(Color32 color, float lineOffset, float medianOffset, Vector2 scratches, Vector2 voids) : base(color, DefaultSolidWidth, lineOffset, medianOffset, scratches, voids)
        {
#if DEBUG
            MinAngle = new PropertyStructValue<float>(StyleChanged, FillerStyle.MinAngle);
            MinLength = new PropertyStructValue<float>(StyleChanged, FillerStyle.MinLength);
            MaxLength = new PropertyStructValue<float>(StyleChanged, FillerStyle.MaxLength);
#endif
        }

        public override FillerStyle CopyStyle() => new SolidFillerStyle(Color, LineOffset, DefaultOffset, Scratches, Voids);

        protected override void CalculateImpl(MarkingFiller filler, ContourGroup contours, MarkingLOD lod, Action<IStyleData> addData)
        {
            if ((SupportLOD & lod) != 0)
            {
                foreach (var contour in contours)
                {
                    var trajectories = contour.Select(c => c.trajectory).ToArray();
                    foreach (var data in DecalData.GetData(lod, trajectories, MinAngle, MinLength, MaxLength, Color, Vector2.one, ScratchDensity, ScratchTiling, VoidDensity, VoidTiling))
                    {
                        addData(data);
                    }
                }
            }
        }

        public override void GetUIComponents(MarkingFiller filler, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(filler, components, parent, isTemplate);
#if DEBUG
            components.Add(GetMinAngle(parent));
            components.Add(GetMinLength(parent));
            components.Add(GetMaxLength(parent));
#endif
        }
#if DEBUG
        private FloatPropertyPanel GetMinAngle(UIComponent parent)
        {
            var minAngleProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(MinAngle));
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
            return minAngleProperty;
        }
        private FloatPropertyPanel GetMinLength(UIComponent parent)
        {
            var minLengthProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(MinLength));
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
            return minLengthProperty;
        }
        private FloatPropertyPanel GetMaxLength(UIComponent parent)
        {
            var maxLengthProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(MaxLength));
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
            return maxLengthProperty;
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
