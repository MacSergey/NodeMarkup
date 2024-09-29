using IMT.API;
using IMT.UI;
using IMT.UI.Editors;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public class DecalFillerStyle : BaseFillerStyle
    {
        public override StyleType Type => StyleType.FillerDecal;
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
                yield return nameof(Offset);
#if DEBUG

#endif
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield break;
            }
        }

        public DecalFillerStyle(PropInfo decal, Color32? color, Vector2 offset, Vector2 tiling, float angle) : base(default, default, offset)
        {
            Decal = new PropertyPrefabValue<PropInfo>("DCL", StyleChanged, decal);
            DecalColor = new PropertyNullableStructValue<Color32, PropertyColorValue>(new PropertyColorValue("DC", null), "DC", StyleChanged, color);
            Tiling = new PropertyVector2Value(StyleChanged, tiling, "TLX", "TLY");
            Angle = new PropertyStructValue<float>("A", StyleChanged, angle);
        }

        protected bool IsValidDecal(PropInfo info) => info != null && !info.m_isMarker && info.m_isDecal;
        public override void CopyTo(BaseFillerStyle target)
        {
            base.CopyTo(target);
            if (target is DecalFillerStyle decalTarget)
            {
                decalTarget.Decal.Value = Decal;
                decalTarget.DecalColor.Value = DecalColor;
                decalTarget.Angle.Value = Angle;
                decalTarget.Tiling.Value = Tiling;
            }
        }
        public override BaseFillerStyle CopyStyle() => new DecalFillerStyle(Decal, DecalColor, Offset, Tiling, Angle);

        protected override void CalculateImpl(MarkingFiller filler, ContourGroup contours, MarkingLOD lod, Action<IStyleData> addData)
        {
            if (Decal.Value is not PropInfo decal)
                return;

            if ((SupportLOD & lod) != 0)
            {
                var mainTexture = decal.m_material.mainTexture as Texture2D;
                var alphaTexture = decal.m_material.GetTexture("_ACIMap") as Texture2D;
                var size = decal.m_material.GetVector("_DecalSize");
                var tiling = new Vector2(1f / (Tiling.Value.x * size.x), 1f / (Tiling.Value.y * size.z));
                var angle = Angle * Mathf.Deg2Rad;
                var color = DecalColor.Value ?? decal.m_color0;
                var textureData = new DecalData.TextureData(mainTexture, alphaTexture, tiling, angle);

                foreach (var contour in contours)
                {
                    var trajectories = contour.Select(c => c.trajectory).ToArray();
                    var datas = DecalData.GetData(DecalData.DecalType.Filler, lod, trajectories, SplitParams, color, textureData, DecalData.EffectData.Default);
                    foreach (var data in datas)
                    {
                        addData(data);
                    }
                }
            }
        }

        protected override void GetUIComponents(MarkingFiller filler, EditorProvider provider)
        {
            base.GetUIComponents(filler, provider);
            provider.AddProperty(new PropertyInfo<SelectPropProperty>(this, nameof(Decal), MainCategory, AddDecalProperty));
            provider.AddProperty(new PropertyInfo<IMTColorPropertyPanel>(this, nameof(DecalColor), MainCategory, AddDecalColorProperty, RefreshDecalColorProperty));
            provider.AddProperty(new PropertyInfo<FloatSingleDoubleProperty>(this, nameof(Tiling), MainCategory, AddTilingProperty, RefreshTilingProperty));
            provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Angle), MainCategory, AddAngleProperty, RefreshAngleProperty));
        }

        protected void AddDecalProperty(SelectPropProperty decalProperty, EditorProvider provider)
        {
            decalProperty.Label = Localize.StyleOption_AssetDecal;
            decalProperty.Selector = IsValidDecal;
            decalProperty.Comparer = new Utilities.Utilities.PropComparer();
            decalProperty.Init();
            decalProperty.Prefab = Decal;
            decalProperty.RawName = Decal.RawName;
            decalProperty.UseWheel = true;
            decalProperty.WheelTip = true;
            decalProperty.OnValueChanged += (value) =>
            {
                var oldDecal = Decal.Value;
                Decal.Value = value;
                if (value != null && (oldDecal == null || !DecalColor.HasValue || DecalColor.Value.Value == oldDecal.m_color0))
                    DecalColor.Value = value.m_color0;

                provider.Refresh();
            };
        }

        private void AddDecalColorProperty(IMTColorPropertyPanel colorProperty, EditorProvider provider)
        {
            colorProperty.Label = Localize.StyleOption_Color;
            colorProperty.WheelTip = Settings.ShowToolTip;
            colorProperty.Init(Decal.Value?.m_color0);
            colorProperty.OnValueChanged += (Color32 color) => DecalColor.Value = color;
        }
        private void RefreshDecalColorProperty(IMTColorPropertyPanel colorProperty, EditorProvider provider)
        {
            colorProperty.Value = DecalColor.Value ?? Decal.Value?.m_color0 ?? new Color32(127, 127, 127, 255);
            colorProperty.IsHidden = !IsValid;

            if (Decal.Value != null)
                colorProperty.DefaultColor = Decal.Value.m_color0;
        }

        private void AddTilingProperty(FloatSingleDoubleProperty tilingProperty, EditorProvider provider)
        {
            tilingProperty.Label = Localize.StyleOption_Scale;
            tilingProperty.RangeRef.Format = Localize.NumberFormat_Percent;
            tilingProperty.RangeRef.FieldWidth = 100f;
            tilingProperty.RangeRef.CheckMax = true;
            tilingProperty.RangeRef.CheckMin = true;
            tilingProperty.RangeRef.MinValue = 10f;
            tilingProperty.RangeRef.MaxValue = 1000f;
            tilingProperty.RangeRef.WheelStep = 10f;
            tilingProperty.RangeRef.UseWheel = true;
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
            angleProperty.Label = Localize.StyleOption_ObjectAngle;
            angleProperty.FieldRef.Format = Localize.NumberFormat_Degree;
            angleProperty.FieldRef.UseWheel = true;
            angleProperty.FieldRef.WheelStep = 1f;
            angleProperty.FieldRef.WheelTip = Settings.ShowToolTip;
            angleProperty.FieldRef.CheckMin = true;
            angleProperty.FieldRef.CheckMax = true;
            angleProperty.FieldRef.MinValue = -180;
            angleProperty.FieldRef.MaxValue = 180;
            angleProperty.FieldRef.CyclicalValue = true;
            angleProperty.Init();
            angleProperty.FieldRef.Value = Angle;
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
