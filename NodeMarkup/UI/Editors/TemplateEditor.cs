using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class TemplateEditor : Editor<TemplateItem, StyleTemplate, DefaultTemplateIcon>
    {
        public override string Name => NodeMarkup.Localize.TemplateEditor_Templates;
        private List<UIComponent> StyleProperties { get; } = new List<UIComponent>();
        private StringPropertyPanel NameProperty { get; set; }
        private TemplateHeaderPanel HeaderPanel { get; set; }

        public TemplateEditor()
        {
            SettingsPanel.autoLayoutPadding = new RectOffset(10, 10, 0, 0);
        }

        protected override void FillItems()
        {
            foreach (var templates in TemplateManager.Templates.OrderBy(t => t.Style.Type))
            {
                AddItem(templates);
            }
        }

        protected override void OnObjectSelect()
        {
            AddHeader();
            AddTemplateName();
            AddStyleProperties();
        }
        private void AddHeader()
        {
            HeaderPanel = SettingsPanel.AddUIComponent<TemplateHeaderPanel>();
            HeaderPanel.Init(EditObject.IsDefault());
            HeaderPanel.OnSetAsDefault += ToggleAsDefault;
        }
        private void AddTemplateName()
        {
            NameProperty = SettingsPanel.AddUIComponent<StringPropertyPanel>();
            NameProperty.Text = NodeMarkup.Localize.TemplateEditor_Name;
            NameProperty.FieldWidth = 230;
            NameProperty.UseWheel = false;
            NameProperty.Init();
            NameProperty.Value = EditObject.Name;
            NameProperty.OnValueChanged += NameSubmitted;
        }
        private void AddStyleProperties() => StyleProperties.AddRange(EditObject.Style.GetUIComponents(SettingsPanel));
        
        private void NameSubmitted(string value)
        {
            EditObject.Name = value;
            NameProperty.Value = EditObject.Name;
            SelectItem.Refresh();
        }

        private void ToggleAsDefault()
        {
            TemplateManager.ToggleAsDefaultTemplate(EditObject);
            AsDefaultRefresh();
        }
        private void AsDefaultRefresh()
        {
            RefreshItems();
            HeaderPanel.Init(EditObject.IsDefault());
        }

        protected override void OnObjectDelete(StyleTemplate template)
        {
            TemplateManager.DeleteTemplate(template);
        }
    }

    public class TemplateItem : EditableItem<StyleTemplate, DefaultTemplateIcon>
    {
        public override string Description => NodeMarkup.Localize.TemplateEditor_ItemDescription;

        public TemplateItem() : base(true, true) { }

        protected override void OnObjectSet() => SetIsDefault();
        public override void Refresh()
        {
            base.Refresh();
            SetIsDefault();
        }
        private void SetIsDefault() => Icon.IsDefault = Object.IsDefault();
    }
    public class DefaultTemplateIcon : UIPanel
    {
        public bool IsDefault { set => isVisible = value; }
        public DefaultTemplateIcon()
        {
            atlas = NodeMarkupPanel.InGameAtlas;
            backgroundSprite = "ParkLevelStar";
        }
    }
}
