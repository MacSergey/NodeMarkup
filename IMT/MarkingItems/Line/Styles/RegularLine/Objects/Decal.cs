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
    public class DecalLineStyle : BaseObjectStyle<PropInfo, SelectPropProperty>, IEffectStyle
    {
        public override StyleType Type => StyleType.LineDecal;
        public override MarkingLOD SupportLOD => MarkingLOD.NoLOD;
        protected override string AssetPropertyName => Localize.StyleOption_AssetDecal;
        protected override Vector3 PrefabSize => IsValid ? Prefab.Value.m_generatedInfo.m_size : Vector3.zero;
        private Vector2 DefaultSize => IsValid ? Prefab.Value.m_material.GetVector("_DecalSize").XZ() : Vector2.zero;


        public PropertyNullableStructValue<Color32, PropertyColorValue> DecalColor { get; }
        public PropertyNullableStructValue<Vector2, PropertyVector2Value> Size { get; }
        public PropertyStructValue<float> Height { get; }
        public PropertyVector2Value Tiling { get; }


        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Prefab);
                yield return nameof(DecalColor);
                yield return nameof(Size);
                yield return nameof(Tiling);
                yield return nameof(Height);
                yield return nameof(EnableCount);
                yield return nameof(Distribution);
                yield return nameof(FixedEnd);
                yield return nameof(Probability);
                yield return nameof(Step);
                yield return nameof(Angle);
                yield return nameof(Shift);
                yield return nameof(Offset);
                yield return nameof(Texture);
                yield return nameof(Cracks);
                yield return nameof(Voids);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<PropInfo>(nameof(Prefab), Prefab);
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<DistributionType>(nameof(Distribution), Distribution);
                yield return new StylePropertyDataProvider<int>(nameof(Probability), Probability);
                yield return new StylePropertyDataProvider<float?>(nameof(Step), Step);
                yield return new StylePropertyDataProvider<Vector2?>(nameof(Angle), Angle);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Shift), Shift);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetBefore), OffsetBefore);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetAfter), OffsetAfter);
            }
        }

        public DecalLineStyle(PropInfo prefab, int probability, float? step, Vector2? angle, Spread angleSpread, Vector2 shift, Spread shiftSpread, float offsetBefore, float offsetAfter, DistributionType distribution, FixedEndType fixedEnd, int minCount, int maxCount, Color32? color, Vector2? size, Vector2 tiling, float height) : base(prefab, probability, step, angle, angleSpread, shift, shiftSpread, offsetBefore, offsetAfter, distribution, fixedEnd, minCount, maxCount)
        {
            DecalColor = new PropertyNullableStructValue<Color32, PropertyColorValue>(new PropertyColorValue("DC", null), "DC", StyleChanged, color);
            Size = new PropertyNullableStructValue<Vector2, PropertyVector2Value>(new PropertyVector2Value(null, labelX: "SZX", labelY: "SZY"), "SZ", StyleChanged, size);
            Tiling = new PropertyVector2Value(StyleChanged, tiling, "TX", "TY");
            Height = new PropertyStructValue<float>("H", StyleChanged, height);
        }

        public override RegularLineStyle CopyLineStyle() => new DecalLineStyle(Prefab, Probability, Step, Angle, AngleSpread, Shift, ShiftSpread, OffsetBefore, OffsetAfter, Distribution, FixedEnd, MinCount, MaxCount, DecalColor, Size, Tiling, Height);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is DecalLineStyle decalTarget)
            {
                decalTarget.DecalColor.Value = DecalColor;
                decalTarget.Size.Value = Size;
                decalTarget.Height.Value = Height;
                decalTarget.Tiling.Value = Tiling;
            }
        }

        protected override void AddData(PropInfo decal, MarkingObjectItemData[] items, MarkingLOD lod, Action<IStyleData> addData)
        {
            var mainTexture = decal.m_material.mainTexture as Texture2D;
            var alphaTexture = decal.m_material.GetTexture("_ACIMap") as Texture2D;
            var size = Size.HasValue ? Size.Value.Value : DefaultSize;
            var textureData = new DecalData.TextureData(mainTexture, alphaTexture, Tiling, 0f);
            var color = DecalColor.Value ?? decal.m_color0;
            foreach (var item in items)
            {
                addData(new DecalData(MaterialType.Text, lod, item.position, item.absoluteAngle + item.angle, new Vector3(size.x, Height, size.y), color, textureData, new DecalData.EffectData(this)));
            }
        }

        protected override void GetUIComponents(MarkingRegularLine line, EditorProvider provider)
        {
            base.GetUIComponents(line, provider);

            provider.AddProperty(new PropertyInfo<IMTColorPropertyPanel>(this, nameof(DecalColor), MainCategory, AddDecalColorProperty, RefreshDecalColorProperty));
            provider.AddProperty(new PropertyInfo<Vector2PropertyPanel>(this, nameof(Size), MainCategory, AddSizeProperty, RefreshSizeProperty));
            provider.AddProperty(new PropertyInfo<Vector2PropertyPanel>(this, nameof(Tiling), MainCategory, AddTilingProperty, RefreshTilingProperty));
            provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Height), MainCategory, AddHeightProperty, RefreshHeightProperty));
        }

        protected override void OnPrefabValueChanged(PropInfo oldDecal, PropInfo newDecal)
        {
            base.OnPrefabValueChanged(oldDecal, newDecal);

            if (newDecal != null && (oldDecal == null || !DecalColor.HasValue || DecalColor.Value.Value == oldDecal.m_color0))
                DecalColor.Value = newDecal.m_color0;
        }

        private void AddDecalColorProperty(IMTColorPropertyPanel colorProperty, EditorProvider provider)
        {
            colorProperty.Label = Localize.StyleOption_Color;
            colorProperty.WheelTip = Settings.ShowToolTip;
            colorProperty.Init(Prefab.Value?.m_color0);
            colorProperty.OnValueChanged += (Color32 color) => DecalColor.Value = color;
        }
        private void RefreshDecalColorProperty(IMTColorPropertyPanel colorProperty, EditorProvider provider)
        {
            colorProperty.Value = DecalColor.Value ?? Prefab.Value?.m_color0 ?? new Color32(127, 127, 127, 255);
            colorProperty.IsHidden = !IsValid;

            if (Prefab.Value != null)
                colorProperty.DefaultColor = Prefab.Value.m_color0;
        }

        private void AddSizeProperty(Vector2PropertyPanel sizeProperty, EditorProvider provider)
        {
            sizeProperty.Label = Localize.StyleOption_Size;
            sizeProperty.SetLabels(Localize.StyleOption_Width, Localize.StyleOption_Length);
            sizeProperty.FieldsWidth = 50f;
            sizeProperty.Format = Localize.NumberFormat_Meter;
            sizeProperty.UseWheel = true;
            sizeProperty.WheelStep = new Vector2(0.1f, 0.1f);
            sizeProperty.WheelTip = Settings.ShowToolTip;
            sizeProperty.CheckMin = true;
            sizeProperty.MinValue = Vector2.zero;
            sizeProperty.Init(0, 1);
            sizeProperty.OnValueChanged += (value) => Size.Value = value;
        }
        private void RefreshSizeProperty(Vector2PropertyPanel tilingProperty, EditorProvider provider)
        {
            tilingProperty.IsHidden = !IsValid;
            tilingProperty.Value = Size.Value ?? DefaultSize;
        }

        private void AddTilingProperty(Vector2PropertyPanel tilingProperty, EditorProvider provider)
        {
            tilingProperty.Label = Localize.StyleOption_Tiling;
            tilingProperty.SetLabels(Localize.StyleOption_Width, Localize.StyleOption_Length);
            tilingProperty.FieldsWidth = 50f;
            //tilingProperty.Format = Localize.NumberFormat_Meter;
            tilingProperty.UseWheel = true;
            tilingProperty.WheelStep = Vector2.one;
            tilingProperty.WheelTip = Settings.ShowToolTip;
            tilingProperty.CheckMin = true;
            tilingProperty.MinValue = new Vector2(0.1f, 0.1f);
            tilingProperty.Init(0, 1);
            tilingProperty.Value = Tiling;
            tilingProperty.OnValueChanged += (value) => Tiling.Value = value;
        }
        private void RefreshTilingProperty(Vector2PropertyPanel tilingProperty, EditorProvider provider)
        {
            tilingProperty.IsHidden = !IsValid;
        }

        private void AddHeightProperty(FloatPropertyPanel heightProperty, EditorProvider provider)
        {
            heightProperty.Label = Localize.StyleOption_SlopeTolerance;
            heightProperty.Format = Localize.NumberFormat_Meter;
            heightProperty.UseWheel = true;
            heightProperty.WheelStep = 0.1f;
            heightProperty.WheelTip = Settings.ShowToolTip;
            heightProperty.CheckMin = true;
            heightProperty.MinValue = 0.1f;
            heightProperty.CheckMax = true;
            heightProperty.MaxValue = 100f;
            heightProperty.Init();
            heightProperty.Value = Height * 0.5f;
            heightProperty.OnValueChanged += (value) => Height.Value = value * 2f;
        }
        private void RefreshHeightProperty(FloatPropertyPanel heightProperty, EditorProvider provider)
        {
            heightProperty.IsHidden = !IsValid;
        }

        protected override void RefreshTextureProperty(FloatPropertyPanel textureProperty, EditorProvider provider)
        {
            textureProperty.IsHidden = !IsValid;
            base.RefreshTextureProperty(textureProperty, provider);
        }
        protected override void RefreshCracksProperty(Vector2PropertyPanel cracksProperty, EditorProvider provider)
        {
            cracksProperty.IsHidden = !IsValid;
            base.RefreshCracksProperty(cracksProperty, provider);
        }
        protected override void RefreshVoidsProperty(Vector2PropertyPanel voidProperty, EditorProvider provider)
        {
            voidProperty.IsHidden = !IsValid;
            base.RefreshVoidsProperty(voidProperty, provider);
        }

        protected override int SortPredicate(PropInfo objA, PropInfo objB) => Utilities.Utilities.GetPrefabName(objA).CompareTo(Utilities.Utilities.GetPrefabName(objB));
        public override bool IsValidPrefab(PropInfo info) => info != null && info.m_isDecal && !info.m_isMarker;
        public override void GetUsedAssets(HashSet<string> networks, HashSet<string> props, HashSet<string> trees)
        {
            props.Add(Prefab.RawName);
        }

        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            DecalColor.FromXml(config, Prefab.Value?.m_color0);
            Size.FromXml(config, null);
            Tiling.FromXml(config, Vector2.one);
            Height.FromXml(config, DecalData.DefaultHeight);
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            DecalColor.ToXml(config);
            Size.ToXml(config);
            Tiling.ToXml(config);
            Height.ToXml(config);
            return config;
        }
    }
}
