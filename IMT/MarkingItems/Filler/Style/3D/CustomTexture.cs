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
    public class CustomTextureFillerStyle : CurbFillerStyle
    {
        public override StyleType Type => StyleType.FillerTexture;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;

        private bool IsValid => IsValidDecal(Decal.Value);

        public PropertyPrefabValue<PropInfo> Decal { get; }
        public PropertyNullableStructValue<Color32, PropertyColorValue> DecalColor { get; }
        public PropertyVector2Value Tiling { get; }
        public PropertyStructValue<float> Angle { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Decal);
                yield return nameof(DecalColor);
                yield return nameof(Tiling);
                yield return nameof(Angle);
                yield return nameof(Elevation);
                yield return nameof(LineCornerRadius);
                yield return nameof(Offset);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<float>(nameof(Elevation), Elevation);
                //yield return new StylePropertyDataProvider<float>(nameof(LineCornerRadius), LineCornerRadius);
                //yield return new StylePropertyDataProvider<float>(nameof(MedianCornerRadius), MedianCornerRadius);
                //yield return new StylePropertyDataProvider<float>(nameof(LineOffset), LineOffset);
                //yield return new StylePropertyDataProvider<float>(nameof(MedianOffset), MedianOffset);
            }
        }

        public CustomTextureFillerStyle(PropInfo decal, Color32? color, Vector2 offset, float elevation, Vector2 cornerRadius, Vector2 curbSize, Vector2 tiling, float angle) : base(offset, elevation, cornerRadius, curbSize)
        {
            Decal = new PropertyPrefabValue<PropInfo>("DCL", StyleChanged, decal);
            DecalColor = new PropertyNullableStructValue<Color32, PropertyColorValue>(new PropertyColorValue("DC", null), "DC", StyleChanged, color);
            Tiling = new PropertyVector2Value(StyleChanged, tiling, "TLX", "TLY");
            Angle = new PropertyStructValue<float>("A", StyleChanged, angle);
        }

        protected bool IsValidDecal(PropInfo info) => info != null && !info.m_isMarker && info.m_isDecal;
        public override BaseFillerStyle CopyStyle() => new CustomTextureFillerStyle(Decal, DecalColor, Offset, Elevation, CornerRadius, CurbSize, Tiling, Angle);

        protected override bool GetCenterTexture(out DecalData.TextureData textureData, out Color color)
        {
            if (Decal.Value is PropInfo decal)
            {
                var mainTexture = decal.m_material.mainTexture as Texture2D;
                var size = decal.m_material.GetVector("_DecalSize");
                var tiling = new Vector2(1f / (Tiling.Value.x * size.x), 1f / (Tiling.Value.y * size.z));
                var angle = Angle * Mathf.Deg2Rad;
                color = DecalColor.Value ?? decal.m_color0;
                textureData = new DecalData.TextureData(mainTexture, null, tiling, angle);
                return true;
            }
            else
            {
                color = default;
                textureData = default;
                return false;
            }
        }

        protected override void GetUIComponents(MarkingFiller filler, EditorProvider provider)
        {
            base.GetUIComponents(filler, provider);
            provider.AddProperty(new PropertyInfo<SelectPropProperty>(this, nameof(Decal), MainCategory, AddDecalProperty));
            provider.AddProperty(new PropertyInfo<ColorAdvancedPropertyPanel>(this, nameof(DecalColor), MainCategory, AddDecalColorProperty, RefreshDecalColorProperty));
            provider.AddProperty(new PropertyInfo<FloatSingleDoubleProperty>(this, nameof(Tiling), MainCategory, AddTilingProperty, RefreshTilingProperty));
            provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Angle), MainCategory, AddAngleProperty, RefreshAngleProperty));
        }

        protected void AddDecalProperty(SelectPropProperty decalProperty, EditorProvider provider)
        {
            decalProperty.Text = Localize.StyleOption_AssetDecal;
            decalProperty.PrefabSelectPredicate = IsValidDecal;
            decalProperty.PrefabSortPredicate = Utilities.Utilities.GetPrefabName;
            decalProperty.Init(60f);
            decalProperty.Prefab = Decal;
            decalProperty.RawName = Decal.RawName;
            decalProperty.OnValueChanged += (value) =>
            {
                var oldDecal = Decal.Value;
                Decal.Value = value;
                if ((oldDecal == null || DecalColor.Value == null || DecalColor.Value != oldDecal.m_color0) && value != null)
                    DecalColor.Value = value.m_color0;

                provider.Refresh();
            };
        }

        private void AddDecalColorProperty(ColorAdvancedPropertyPanel colorProperty, EditorProvider provider)
        {
            colorProperty.Text = Localize.StyleOption_Color;
            colorProperty.WheelTip = Settings.ShowToolTip;
            colorProperty.Init(Decal.Value?.m_color0);
            colorProperty.Value = DecalColor.Value ?? Decal.Value?.m_color0 ?? new Color32(127, 127, 127, 255);
            colorProperty.OnValueChanged += (Color32 color) => DecalColor.Value = color;
        }
        private void RefreshDecalColorProperty(ColorAdvancedPropertyPanel colorProperty, EditorProvider provider)
        {
            colorProperty.IsHidden = !IsValid;

            if (Decal.Value != null)
                colorProperty.DefaultColor = Decal.Value.m_color0;
        }

        private void AddTilingProperty(FloatSingleDoubleProperty tilingProperty, EditorProvider provider)
        {
            tilingProperty.Text = Localize.StyleOption_Scale;
            tilingProperty.Format = Localize.NumberFormat_Percent;
            tilingProperty.FieldWidth = 100f;
            tilingProperty.CheckMax = true;
            tilingProperty.CheckMin = true;
            tilingProperty.MinValue = 10f;
            tilingProperty.MaxValue = 1000f;
            tilingProperty.WheelStep = 10f;
            tilingProperty.UseWheel = true;
            tilingProperty.Init
                (new OptionData(Localize.StyleOption_ScaleLock, IMTTextures.Atlas, IMTTextures.LockButtonIcon),
                new OptionData(Localize.StyleOption_ScaleUnlock, IMTTextures.Atlas, IMTTextures.UnlockButtonIcon));
            tilingProperty.OnValueChanged += (x, y) => Tiling.Value = new Vector2(x * 0.01f, y * 0.01f);
        }
        protected virtual void RefreshTilingProperty(FloatSingleDoubleProperty tilingProperty, EditorProvider provider)
        {
            tilingProperty.IsHidden = !IsValid;
            tilingProperty.SetValues(Tiling.Value.x * 100f, Tiling.Value.y * 100f);
        }

        private new void AddAngleProperty(FloatPropertyPanel angleProperty, EditorProvider provider)
        {
            angleProperty.Text = Localize.StyleOption_ObjectAngle;
            angleProperty.Format = Localize.NumberFormat_Degree;
            angleProperty.UseWheel = true;
            angleProperty.WheelStep = 1f;
            angleProperty.WheelTip = Settings.ShowToolTip;
            angleProperty.CheckMin = true;
            angleProperty.CheckMax = true;
            angleProperty.MinValue = -180;
            angleProperty.MaxValue = 180;
            angleProperty.CyclicalValue = true;
            angleProperty.Init();
            angleProperty.Value = Angle;
            angleProperty.OnValueChanged += (value) => Angle.Value = value;
        }
        private void RefreshAngleProperty(FloatPropertyPanel angleProperty, EditorProvider provider)
        {
            angleProperty.IsHidden = !IsValid;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Decal.ToXml(config);
            DecalColor.ToXml(config);
            Tiling.ToXml(config);
            Angle.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            Decal.FromXml(config, null);
            DecalColor.FromXml(config, Decal.Value?.m_color0);
            Tiling.FromXml(config, Vector2.one);
            Angle.FromXml(config, 0f);
        }

        public override void GetUsedAssets(HashSet<string> networks, HashSet<string> props, HashSet<string> trees)
        {
            props.Add(Decal.RawName);
        }
    }
}
