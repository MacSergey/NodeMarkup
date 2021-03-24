using ModsCommon.UI;
using NodeMarkup.Manager;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class StyleTemplateEditor : BaseTemplateEditor<StyleTemplateItemsPanel, StyleTemplate, StyleTemplateHeaderPanel, EditStyleTemplateMode>
    {
        public override string Name => NodeMarkup.Localize.TemplateEditor_Templates;
        public override string EmptyMessage => string.Format(NodeMarkup.Localize.TemplateEditor_EmptyMessage, NodeMarkup.Localize.HeaderPanel_SaveAsTemplate);
        public override Type SupportType { get; } = typeof(ISupportStyleTemplate);
        protected override string IsAssetMessage => NodeMarkup.Localize.TemplateEditor_TemplateIsAsset;
        protected override string RewriteCaption => NodeMarkup.Localize.TemplateEditor_RewriteCaption;
        protected override string RewriteMessage => NodeMarkup.Localize.TemplateEditor_RewriteMessage;
        protected override string SaveChangesMessage => NodeMarkup.Localize.TemplateEditor_SaveChangesMessage;
        protected override string NameExistMessage => NodeMarkup.Localize.TemplateEditor_NameExistMessage;
        protected override string IsAssetWarningMessage => NodeMarkup.Localize.TemplateEditor_IsAssetWarningMessage;
        protected override string IsWorkshopWarningMessage => NodeMarkup.Localize.TemplateEditor_IsWorkshopWarningMessage;

        private Style EditStyle { get; set; }
        private List<EditorItem> StyleProperties { get; set; } = new List<EditorItem>();

        protected override IEnumerable<StyleTemplate> GetObjects() => TemplateManager.StyleManager.Templates;

        protected override void OnFillPropertiesPanel(StyleTemplate template)
        {
            CopyStyle();
            base.OnFillPropertiesPanel(template);
        }
        protected override void OnClear()
        {
            base.OnClear();
            StyleProperties.Clear();
        }

        private void CopyStyle()
        {
            EditStyle = EditObject.Style.Copy();
            EditStyle.OnStyleChanged = OnChanged;
        }
        protected override IEnumerable<EditorItem> AddAditionalProperties()
        {
            AddStyleProperties();
            if (StyleProperties.OfType<ColorPropertyPanel>().FirstOrDefault() is ColorPropertyPanel colorProperty)
                colorProperty.OnValueChanged += (Color32 c) => RefreshSelectedItem();

            return StyleProperties;
        }

        protected override void AddHeader()
        {
            base.AddHeader();
            HeaderPanel.OnSetAsDefault += ToggleAsDefault;
            HeaderPanel.OnDuplicate += Duplicate;
        }
        private void AddStyleProperties()
        {
            StyleProperties = EditStyle.GetUIComponents(EditObject, PropertiesPanel, true);
        }

        private void ToggleAsDefault()
        {
            TemplateManager.StyleManager.ToggleAsDefaultTemplate(EditObject);
            ItemsPanel.RefreshItems();
            HeaderPanel.Init(EditObject);
        }
        private void Duplicate()
        {
            if (TemplateManager.StyleManager.DuplicateTemplate(EditObject, out StyleTemplate duplicate))
                Panel.EditStyleTemplate(duplicate, false);
        }
        protected override void OnApplyChanges()
        {
            base.OnApplyChanges();
            EditObject.Style = EditStyle.Copy();
        }
        protected override void OnNotApplyChanges()
        {
            base.OnNotApplyChanges();
            CopyStyle();
        }
    }

    public class StyleTemplateItemsPanel : ItemsGroupPanel<StyleTemplateItem, StyleTemplate, StyleTemplateGroup, Style.StyleType>
    {
        public override bool GroupingEnable => Settings.GroupTemplates.value;
        public override int Compare(StyleTemplate x, StyleTemplate y) => x.Name.CompareTo(y.Name);
        public override int Compare(Style.StyleType x, Style.StyleType y) => x.CompareTo(y);

        protected override string GroupName(Style.StyleType group) => Settings.GroupTemplatesType == 0 ? group.Description() : $"{group.GetGroup().Description()}\n{group.Description()}";

        protected override Style.StyleType SelectGroup(StyleTemplate editObject) => Settings.GroupTemplatesType == 0 ? editObject.Style.Type.GetGroup() : editObject.Style.Type;
    }
    public class StyleTemplateItem : EditItem<StyleTemplate, StyleTemplateIcon>
    {
        public override bool ShowDelete => !Object.IsAsset;

        private bool IsDefault => Object?.IsDefault == true;
        public override Color32 NormalColor => IsDefault ? new Color32(255, 197, 0, 255) : base.NormalColor;
        public override Color32 HoveredColor => IsDefault ? new Color32(255, 207, 51, 255) : base.HoveredColor;
        public override Color32 PressedColor => IsDefault ? new Color32(255, 218, 72, 255) : base.PressedColor;
        public override Color32 FocusColor => IsDefault ? new Color32(255, 228, 92, 255) : base.FocusColor;

        public override void Refresh()
        {
            base.Refresh();
            Icon.Type = Object.Style.Type;
            Icon.StyleColor = Object.Style.Color;
            Label.wordWrap = !Object.IsAsset;

            SetColors();
        }
    }
    public class StyleTemplateIcon : StyleIcon
    {
        public bool IsDefault { set => BorderColor = value ? new Color32(255, 215, 0, 255) : (Color32)Color.white; }
    }
    public class StyleTemplateGroup : EditGroup<Style.StyleType, StyleTemplateItem, StyleTemplate> { }
    public class EditStyleTemplateMode : EditTemplateMode<StyleTemplate> { }
}
