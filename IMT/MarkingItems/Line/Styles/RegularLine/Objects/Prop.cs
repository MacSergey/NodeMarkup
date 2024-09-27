using IMT.API;
using IMT.UI;
using IMT.UI.Editors;
using IMT.Utilities;
using IMT.Utilities.API;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public class PropLineStyle : BaseObject3DObjectStyle<PropInfo, SelectPropProperty>
    {
        public static new Color32 DefaultColor => new Color32();
        public static ColorOptionEnum DefaultColorOption => ColorOptionEnum.Random;

        public override StyleType Type => StyleType.LineProp;
        public override MarkingLOD SupportLOD => MarkingLOD.NoLOD;
        protected override Vector3 PrefabSize => IsValid ? Prefab.Value.m_generatedInfo.m_size : Vector3.zero;
        protected bool IsDecal => IsValid && Prefab.Value.m_isDecal;
        protected override string AssetPropertyName => Localize.StyleOption_AssetProp;

        PropertyEnumValue<ColorOptionEnum> ColorOption { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Prefab);
                yield return nameof(ColorOption);
                yield return nameof(Color);
                yield return nameof(EnableCount);
                yield return nameof(Distribution);
                yield return nameof(FixedEnd);
                yield return nameof(Probability);
                yield return nameof(Step);
                yield return nameof(Angle);
                yield return nameof(Tilt);
                yield return nameof(Slope);
                yield return nameof(Shift);
                yield return nameof(Elevation);
                yield return nameof(Scale);
                yield return nameof(Offset);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<PropInfo>(nameof(Prefab), Prefab);
                yield return new StylePropertyDataProvider<ColorOptionEnum>(nameof(ColorOption), ColorOption);
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<DistributionType>(nameof(Distribution), Distribution);
                yield return new StylePropertyDataProvider<int>(nameof(Probability), Probability);
                yield return new StylePropertyDataProvider<float?>(nameof(Step), Step);
                yield return new StylePropertyDataProvider<Vector2?>(nameof(Angle), Angle);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Tilt), Tilt);
                yield return new StylePropertyDataProvider<Vector2?>(nameof(Slope), Slope);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Shift), Shift);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Elevation), Elevation);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Scale), Scale);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetBefore), OffsetBefore);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetAfter), OffsetAfter);
            }
        }

        public PropLineStyle(PropInfo prop, int probability, ColorOptionEnum colorOption, Color32 color, float? step, Vector2? angle, Spread angleSpread, Vector2 shift, Spread shiftSpread, float offsetBefore, float offsetAfter, DistributionType distribution, FixedEndType fixedEnd, int minCount, int maxCount, Vector2 tilt, Spread tiltSpread, Vector2? slope, Spread slopeSpread, Vector2 scale, Spread scaleSpread, Vector2 elevation, Spread elevationSpread) : base(prop, probability, step, angle, angleSpread, shift, shiftSpread, offsetBefore, offsetAfter, distribution, fixedEnd, minCount, maxCount, tilt, tiltSpread, slope, slopeSpread, scale, scaleSpread, elevation, elevationSpread)
        {
            Color.Value = color;
            ColorOption = new PropertyEnumValue<ColorOptionEnum>("CO", StyleChanged, colorOption);
        }

        public override RegularLineStyle CopyLineStyle() => new PropLineStyle(Prefab.Value, Probability, ColorOption, Color, Step, Angle, AngleSpread, Shift, ShiftSpread, OffsetBefore, OffsetAfter, Distribution, FixedEnd, MinCount, MaxCount, Tilt, TiltSpread, Slope, SlopeSpread, Scale, ScaleSpread, Elevation, ElevationSpread);

        protected override void CalculateItem(ITrajectory trajectory, float t, float p, PropInfo prop, ref MarkingObjectItemData item)
        {
            base.CalculateItem(trajectory, t, p, prop, ref item);

            switch (ColorOption.Value)
            {
                case ColorOptionEnum.Color1:
                    item.color = prop.m_color0;
                    break;
                case ColorOptionEnum.Color2:
                    item.color = prop.m_color1;
                    break;
                case ColorOptionEnum.Color3:
                    item.color = prop.m_color2;
                    break;
                case ColorOptionEnum.Color4:
                    item.color = prop.m_color3;
                    break;
                case ColorOptionEnum.Random:
                    item.color = prop.GetColor(ref SimulationManager.instance.m_randomizer);
                    break;
                case ColorOptionEnum.Custom:
                    item.color = Color;
                    break;
            }
        }
        protected override void AddData(PropInfo prop, MarkingObjectItemData[] items, MarkingLOD lod, Action<IStyleData> addData)
        {
            addData(new MarkingPropData(prop, items));
        }

        protected override void GetUIComponents(MarkingRegularLine line, EditorProvider provider)
        {
            base.GetUIComponents(line, provider);
            provider.AddProperty(new PropertyInfo<PropColorPropertyPanel>(this, nameof(ColorOption), AdditionalCategory, AddColorOptionProperty, RefreshColorOptionProperty));
            provider.AddProperty(new PropertyInfo<IMTColorPropertyPanel>(this, nameof(Color), AdditionalCategory, AddColorProperty, RefreshColorProperty));
        }

        private void AddColorOptionProperty(PropColorPropertyPanel colorOptionProperty, EditorProvider provider)
        {
            colorOptionProperty.Label = Localize.StyleOption_ColorOption;
            colorOptionProperty.UseWheel = true;
            colorOptionProperty.Init();
            colorOptionProperty.SelectedObject = ColorOption;
            colorOptionProperty.OnSelectObjectChanged += (value) =>
            {
                ColorOption.Value = value;
                provider.Refresh();
            };
        }
        private void RefreshColorOptionProperty(PropColorPropertyPanel colorOptionProperty, EditorProvider provider)
        {
            colorOptionProperty.IsHidden = !IsValid;
        }

        private void AddColorProperty(IMTColorPropertyPanel colorProperty, EditorProvider provider)
        {
            colorProperty.Label = Localize.StyleOption_Color;
            colorProperty.WheelTip = Settings.ShowToolTip;
            colorProperty.Init(GetDefault()?.Color);
            colorProperty.Value = Color;
            colorProperty.OnValueChanged += (color) => Color.Value = color;
        }
        private new void RefreshColorProperty(IMTColorPropertyPanel colorProperty, EditorProvider provider)
        {
            colorProperty.IsHidden = !IsValid || !(ColorOption == ColorOptionEnum.Custom);
        }

        public override bool IsValidPrefab(PropInfo info) => info != null && !info.m_isMarker;
        protected override int SortPredicate(PropInfo objA, PropInfo objB) => Utilities.Utilities.GetPrefabName(objA).CompareTo(Utilities.Utilities.GetPrefabName(objB));

        public override XElement ToXml()
        {
            var config = base.ToXml();
            ColorOption.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            ColorOption.FromXml(config, DefaultColorOption);
        }
        public override void GetUsedAssets(HashSet<string> networks, HashSet<string> props, HashSet<string> trees)
        {
            props.Add(Prefab.RawName);
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
}
