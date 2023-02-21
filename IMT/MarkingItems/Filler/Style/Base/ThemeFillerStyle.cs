using IMT.API;
using IMT.UI;
using IMT.UI.Editors;
using IMT.Utilities;
using IMT.Utilities.API;
using ModsCommon.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace IMT.Manager
{
    public abstract class ThemeFillerStyle : CurbFillerStyle
    {
        public PropertyThemeValue Theme { get; }

        public ThemeFillerStyle(ThemeHelper.IThemeData theme, Vector2 offset, float elevation, Vector2 cornerRadius, Vector2 curbSize) : base(offset, elevation, cornerRadius, curbSize) 
        {
            Theme = new PropertyThemeValue("THM", StyleChanged, theme);
        }

        public override void CopyTo(BaseFillerStyle target)
        {
            base.CopyTo(target);
            if(target is ThemeFillerStyle themeTarger)
            {
                themeTarger.Theme.Value = Theme.Value;
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
            themeProperty.Theme = Theme.Value;
            themeProperty.OnValueChanged += (value) => Theme.Value = value;
        }
    }
}
