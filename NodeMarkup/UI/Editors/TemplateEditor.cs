using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class TemplateEditor : GroupedEditor<TemplateItem, StyleTemplate, TemplateIcon, TemplateGroup, Style.StyleType>
    {
        public override string Name => NodeMarkup.Localize.TemplateEditor_Templates;
        public override string EmptyMessage => string.Format(NodeMarkup.Localize.TemplateEditor_EmptyMessage, NodeMarkup.Localize.HeaderPanel_SaveAsTemplate);
        protected override bool GroupingEnabled => Settings.GroupTemplates.value;

        private List<UIComponent> StyleProperties { get; set; } = new List<UIComponent>();
        private StringPropertyPanel NameProperty { get; set; }
        private TemplateHeaderPanel HeaderPanel { get; set; }

        public TemplateEditor()
        {
            SettingsPanel.autoLayoutPadding = new RectOffset(10, 10, 0, 0);
        }

        protected override void FillItems()
        {
            foreach (var templates in TemplateManager.Templates.OrderBy(t => t.Style.Type))
                AddItem(templates);
        }

        protected override void OnObjectSelect()
        {
            AddHeader();
            AddTemplateName();
            AddStyleProperties();
            if (StyleProperties.FirstOrDefault() is ColorPropertyPanel colorProperty)
                colorProperty.OnValueChanged += (Color32 c) => SelectItem.Refresh();
        }
        protected override Style.StyleType SelectGroup(StyleTemplate editableItem) => editableItem.Style.Type & Style.StyleType.GroupMask;
        protected override string GroupName(Style.StyleType group) => Utilities.EnumDescription(group);

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
        private void AddStyleProperties() => StyleProperties = EditObject.Style.GetUIComponents(EditObject, SettingsPanel, isTemplate: true);

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
        protected override void OnObjectDelete(StyleTemplate template) => TemplateManager.DeleteTemplate(template);
    }

    public class TemplateItem : EditableItem<StyleTemplate, TemplateIcon>
    {
        public override string Description => NodeMarkup.Localize.TemplateEditor_ItemDescription;

        public override void Init() => Init(true, true);

        protected override void OnObjectSet() => SetIcon();
        public override void Refresh()
        {
            base.Refresh();
            SetIcon();
        }
        private void SetIcon()
        {
            Icon.Type = Object.Style.Type;
            Icon.StyleColor = Object.Style.Color;
            Icon.IsDefault = Object.IsDefault();
        }
    }
    public class TemplateIcon : StyleIcon
    {
        public bool IsDefault { set => BorderColor = value ? new Color32(255, 215, 0, 255) : (Color32)Color.white; }
    }
    public class TemplateGroup : EditableGroup<Style.StyleType, TemplateItem, StyleTemplate, TemplateIcon> { }
}
