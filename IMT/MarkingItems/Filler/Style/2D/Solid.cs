using IMT.API;
using IMT.UI.Editors;
using IMT.Utilities;
using IMT.Utilities.API;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public class SolidFillerStyle : BaseFillerStyle, IColorStyle, IEffectStyle
    {
        public static float DefaultSolidWidth { get; } = 0.2f;

        public override StyleType Type => StyleType.FillerSolid;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;
        public bool KeepColor => true;

#if DEBUG
        public PropertyBoolValue Debug { get; }
        public PropertyValue<float> MinAngle { get; }
        public PropertyValue<float> MinLength { get; }
        public PropertyValue<float> MaxLength { get; }
        public PropertyValue<float> MaxHeight { get; }

        private new StyleHelper.SplitParams SplitParams => new StyleHelper.SplitParams()
        {
            minAngle = MinAngle,
            minLength = MinLength,
            maxLength = MaxLength,
            maxHeight = MaxHeight,
        };

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
                yield return nameof(Debug);
                yield return nameof(MinAngle);
                yield return nameof(MinLength);
                yield return nameof(MaxLength);
                yield return nameof(MaxHeight);
#endif
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                //yield return new StylePropertyDataProvider<float>(nameof(LineOffset), LineOffset);
                //yield return new StylePropertyDataProvider<float>(nameof(MedianOffset), MedianOffset);
                yield return new StylePropertyDataProvider<float>(nameof(Texture), Texture);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Cracks), Cracks);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Voids), Voids);
            }
        }

        public SolidFillerStyle(Color32 color, Vector2 cracks, Vector2 voids, float texture, Vector2 offset) : base(color, DefaultSolidWidth, cracks, voids, texture, offset)
        {
#if DEBUG
            Debug = new PropertyBoolValue(StyleChanged, false);
            var splitParams = BaseFillerStyle.SplitParams;
            MinAngle = new PropertyStructValue<float>(StyleChanged, splitParams.minAngle);
            MinLength = new PropertyStructValue<float>(StyleChanged, splitParams.minLength);
            MaxLength = new PropertyStructValue<float>(StyleChanged, splitParams.maxLength);
            MaxHeight = new PropertyStructValue<float>(StyleChanged, splitParams.maxHeight);
#endif
        }

        public override BaseFillerStyle CopyStyle() => new SolidFillerStyle(Color, Cracks, Voids, Texture, Offset);

        protected override void CalculateImpl(MarkingFiller filler, ContourGroup contours, MarkingLOD lod, Action<IStyleData> addData)
        {
            if ((SupportLOD & lod) != 0)
            {
                foreach (var contour in contours)
                {
                    var trajectories = contour.Select(c => c.trajectory).ToArray();
                    foreach (var data in DecalData.GetData(DecalData.DecalType.Filler, lod, trajectories, SplitParams, Color, DecalData.TextureData.Default, new DecalData.EffectData(this)
#if DEBUG
                                , Debug
#endif
                        ))
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
                provider.AddProperty(new PropertyInfo<BoolPropertyPanel>(this, nameof(Debug), DebugCategory, GetDebug));
                provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(MinAngle), DebugCategory, GetMinAngle));
                provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(MinLength), DebugCategory, GetMinLength));
                provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(MaxLength), DebugCategory, GetMaxLength));
                provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(MaxHeight), DebugCategory, GetMaxHeight));
            }
#endif
        }

#if DEBUG
        private void GetDebug(BoolPropertyPanel debugProperty, EditorProvider provider)
        {
            debugProperty.Label = "Debug";
            debugProperty.Init();
            debugProperty.Value = Debug;
            debugProperty.OnValueChanged += (value) => Debug.Value = value;
        }
        private void GetMinAngle(FloatPropertyPanel minAngleProperty, EditorProvider provider)
        {
            minAngleProperty.Label = "Min angle";
            minAngleProperty.CanCollapse = true;
            minAngleProperty.FieldRef.CheckMax = true;
            minAngleProperty.FieldRef.CheckMin = true;
            minAngleProperty.FieldRef.MinValue = 1f;
            minAngleProperty.FieldRef.MaxValue = 90f;
            minAngleProperty.FieldRef.WheelStep = 1f;
            minAngleProperty.FieldRef.UseWheel = true;
            minAngleProperty.Init();
            minAngleProperty.FieldRef.Value = MinAngle;
            minAngleProperty.OnValueChanged += (float value) => MinAngle.Value = value;
        }
        private void GetMinLength(FloatPropertyPanel minLengthProperty, EditorProvider provider)
        {
            minLengthProperty.Label = "Min length";
            minLengthProperty.CanCollapse = true;
            minLengthProperty.FieldRef.CheckMax = true;
            minLengthProperty.FieldRef.CheckMin = true;
            minLengthProperty.FieldRef.MinValue = 0.1f;
            minLengthProperty.FieldRef.MaxValue = 10f;
            minLengthProperty.FieldRef.WheelStep = 0.1f;
            minLengthProperty.FieldRef.UseWheel = true;
            minLengthProperty.Init();
            minLengthProperty.FieldRef.Value = MinLength;
            minLengthProperty.OnValueChanged += (float value) => MinLength.Value = value;
        }
        private void GetMaxLength(FloatPropertyPanel maxLengthProperty, EditorProvider provider)
        {
            maxLengthProperty.Label = "Max length";
            maxLengthProperty.CanCollapse = true;
            maxLengthProperty.FieldRef.CheckMax = true;
            maxLengthProperty.FieldRef.CheckMin = true;
            maxLengthProperty.FieldRef.MinValue = 1f;
            maxLengthProperty.FieldRef.MaxValue = 100f;
            maxLengthProperty.FieldRef.WheelStep = 0.1f;
            maxLengthProperty.FieldRef.UseWheel = true;
            maxLengthProperty.Init();
            maxLengthProperty.FieldRef.Value = MaxLength;
            maxLengthProperty.OnValueChanged += (float value) => MaxLength.Value = value;
        }
        private void GetMaxHeight(FloatPropertyPanel maxHeightProperty, EditorProvider provider)
        {
            maxHeightProperty.Label = "Max height";
            maxHeightProperty.CanCollapse = true;
            maxHeightProperty.FieldRef.CheckMax = true;
            maxHeightProperty.FieldRef.CheckMin = true;
            maxHeightProperty.FieldRef.MinValue = 1f;
            maxHeightProperty.FieldRef.MaxValue = 100f;
            maxHeightProperty.FieldRef.WheelStep = 0.1f;
            maxHeightProperty.FieldRef.UseWheel = true;
            maxHeightProperty.Init();
            maxHeightProperty.FieldRef.Value = MaxHeight;
            maxHeightProperty.OnValueChanged += (float value) => MaxHeight.Value = value;
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
