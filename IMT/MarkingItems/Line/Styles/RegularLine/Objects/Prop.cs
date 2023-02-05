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
    public class PropLineStyle : BaseObjectLineStyle<PropInfo, SelectPropProperty>
    {
        public static new Color32 DefaultColor => new Color32();
        public static ColorOptionEnum DefaultColorOption => ColorOptionEnum.Random;

        public override StyleType Type => StyleType.LineProp;
        public override MarkingLOD SupportLOD => MarkingLOD.NoLOD;
        protected override Vector3 PrefabSize => IsValid ? Prefab.Value.m_generatedInfo.m_size : Vector3.zero;
        protected override string AssetPropertyName => Localize.StyleOption_AssetProp;

        public override bool CanElevate => Prefab.Value is PropInfo prop && !prop.m_isDecal;
        public override bool CanSlope => Prefab.Value is PropInfo prop && !prop.m_isDecal;

        PropertyEnumValue<ColorOptionEnum> ColorOption { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Prefab);
                yield return nameof(ColorOption);
                yield return nameof(Color);
                yield return nameof(Distribution);
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
                yield return new StylePropertyDataProvider<Vector2>(nameof(Angle), Angle);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Tilt), Tilt);
                yield return new StylePropertyDataProvider<Vector2?>(nameof(Slope), Slope);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Shift), Shift);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Elevation), Elevation);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Scale), Scale);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetBefore), OffsetBefore);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetAfter), OffsetAfter);
            }
        }

        public PropLineStyle(PropInfo prop, int probability, ColorOptionEnum colorOption, Color32 color, float? step, Vector2 angle, Vector2 tilt, Vector2? slope, Vector2 shift, Vector2 scale, Vector2 elevation, float offsetBefore, float offsetAfter, DistributionType distribution) : base(prop, probability, step, angle, tilt, slope, shift, scale, elevation, offsetBefore, offsetAfter, distribution)
        {
            Color.Value = color;
            ColorOption = new PropertyEnumValue<ColorOptionEnum>("CO", StyleChanged, colorOption);
        }

        public override RegularLineStyle CopyLineStyle() => new PropLineStyle(Prefab.Value, Probability, ColorOption, Color, Step, Angle, Tilt, Slope, Shift, Scale, Elevation, OffsetBefore, OffsetAfter, Distribution);

        protected override void CalculateItem(PropInfo prop, ref MarkingPropItemData item)
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
        protected override void CalculateParts(PropInfo prop, MarkingPropItemData[] items, MarkingLOD lod, Action<IStyleData> addData)
        {
            addData(new MarkingPropData(prop, items));
        }

        public override void GetUIComponents(MarkingRegularLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            components.Add(AddColorOptionProperty(parent, true));
            components.Add(AddColorProperty(parent, true));
            base.GetUIComponents(line, components, parent, isTemplate);
        }
        protected override void PrefabChanged(UIComponent parent, bool valid)
        {
            base.PrefabChanged(parent, valid);

            if (parent.Find(nameof(ColorOption)) is EditorPropertyPanel colorOption)
                colorOption.IsHidden = !valid;

            ColorOptionChanged(parent, valid, ColorOption);
        }
        protected void ColorOptionChanged(UIComponent parent, bool valid, ColorOptionEnum value)
        {
            if (parent.Find(nameof(Color)) is EditorPropertyPanel color)
                color.IsHidden = !valid || !(value == ColorOptionEnum.Custom);
        }

        protected PropColorPropertyPanel AddColorOptionProperty(UIComponent parent, bool canCollapse)
        {
            var colorOptionProperty = ComponentPool.Get<PropColorPropertyPanel>(parent, nameof(ColorOption));
            colorOptionProperty.Text = Localize.StyleOption_ColorOption;
            colorOptionProperty.UseWheel = true;
            colorOptionProperty.CanCollapse = canCollapse;
            colorOptionProperty.Init();
            colorOptionProperty.SelectedObject = ColorOption;
            colorOptionProperty.OnSelectObjectChanged += (value) =>
            {
                ColorOption.Value = value;
                ColorOptionChanged(parent, IsValid, value);
            };
            return colorOptionProperty;
        }
        protected ColorAdvancedPropertyPanel AddColorProperty(UIComponent parent, bool canCollapse)
        {
            var colorProperty = ComponentPool.Get<ColorAdvancedPropertyPanel>(parent, nameof(Color));
            colorProperty.Text = Localize.StyleOption_Color;
            colorProperty.WheelTip = Settings.ShowToolTip;
            colorProperty.CanCollapse = canCollapse;
            colorProperty.Init(GetDefault()?.Color);
            colorProperty.Value = Color;
            colorProperty.OnValueChanged += (color) => Color.Value = color;

            return colorProperty;
        }

        protected override bool IsValidPrefab(PropInfo info) => info != null && !info.m_isMarker;
        protected override Func<PropInfo, string> GetSortPredicate() => Utilities.Utilities.GetPrefabName;

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
