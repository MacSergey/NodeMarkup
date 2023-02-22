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

        public ThemeFillerStyle(ThemeHelper.IThemeData theme, Vector2 offset, float elevation, Vector2 cornerRadius, Vector2 curbSize) : base(offset, elevation, cornerRadius, curbSize)
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

        protected override FillerMeshData.TextureData GetTopTexture()
        {
            if (Theme.Value is ThemeHelper.IThemeData themeData)
            {         
                var theme = themeData.GetTexture(TextureType);
                var textureData = new FillerMeshData.TextureData(theme.texture, UnityEngine.Color.white, theme.tiling, 0f);
                return textureData;
            }
            else
            {
                var theme = ThemeHelper.DefaultTheme.GetTexture(TextureType);
                var textureData = new FillerMeshData.TextureData(theme.texture, UnityEngine.Color.white, theme.tiling, 0f);
                return textureData;
            }
        }

        protected override void GetUIComponents(MarkingFiller filler, EditorProvider provider)
        {
            base.GetUIComponents(filler, provider);
            provider.AddProperty(new PropertyInfo<SelectThemeProperty>(this, nameof(Theme), MainCategory, AddThemeProperty));
        }
        protected void AddThemeProperty(SelectThemeProperty themeProperty, EditorProvider provider)
        {
            themeProperty.Text = "Theme";
            themeProperty.Init(60f);
            themeProperty.RawName = Theme.RawName;
            themeProperty.TextureType = TextureType;
            themeProperty.Theme = Theme.Value;
            themeProperty.OnValueChanged += (value) => Theme.Value = value;
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
