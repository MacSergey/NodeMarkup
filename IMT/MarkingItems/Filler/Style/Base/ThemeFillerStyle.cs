using IMT.API;
using IMT.UI;
using IMT.UI.Editors;
using IMT.Utilities;
using IMT.Utilities.API;
using ModsCommon.Utilities;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public abstract class ThemeFillerStyle : CurbFillerStyle
    {
        public PropertyThemeValue Theme { get; }
        protected abstract ThemeHelper.TextureType TextureType { get; }

        public ThemeFillerStyle(ThemeHelper.IThemeData pavementTheme, ThemeHelper.IThemeData theme, Vector2 offset, float elevation, Vector2 cornerRadius, Vector2 curbSize) : base(pavementTheme, offset, elevation, cornerRadius, curbSize)
        {
            Theme = new PropertyThemeValue("THM", StyleChanged, theme);
        }

        public override void CopyTo(BaseFillerStyle target)
        {
            base.CopyTo(target);
            if (target is ThemeFillerStyle themeTarger)
            {
                themeTarger.Theme.Value = Theme.Value;
            }
        }

        protected override bool GetCenterTexture(out DecalData.TextureData textureData, out Color color)
        {
            var theme = (Theme.Value is ThemeHelper.IThemeData themeData ? themeData : ThemeHelper.DefaultTheme).GetTexture(TextureType);
            textureData = new DecalData.TextureData(theme.texture, null, theme.tiling, 0f);
            color = UnityEngine.Color.white;
            return true;
        }
        protected override bool GetTopTexture(out FillerMeshData.TextureData textureData, out Color color)
        {
            var theme = (Theme.Value is ThemeHelper.IThemeData themeData ? themeData : ThemeHelper.DefaultTheme).GetTexture(TextureType);
            textureData = new FillerMeshData.TextureData(theme.texture, theme.tiling, 0f);
            color = UnityEngine.Color.white;
            return true;
        }

        protected override void GetUIComponents(MarkingFiller filler, EditorProvider provider)
        {
            base.GetUIComponents(filler, provider);
            provider.AddProperty(new PropertyInfo<SelectThemeProperty>(this, nameof(Theme), MainCategory, AddThemeProperty, RefreshThemeProperty));
        }
        protected void AddThemeProperty(SelectThemeProperty themeProperty, EditorProvider provider)
        {
            themeProperty.Label = Localize.StyleOption_Theme;
            themeProperty.Init(60f);
            themeProperty.RawName = Theme.RawName;
            themeProperty.TextureType = TextureType;
            themeProperty.Theme = Theme.Value;
            themeProperty.OnValueChanged += (value) => Theme.Value = value;
        }
        protected void RefreshThemeProperty(SelectThemeProperty themeProperty, EditorProvider provider)
        {
            themeProperty.IsHidden = ThemeHelper.ThemeCount == 0 && string.IsNullOrEmpty(Theme.RawName);
        }

        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            Theme.FromXml(config, null);
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            Theme.ToXml(config);
            return config;
        }
    }
}
