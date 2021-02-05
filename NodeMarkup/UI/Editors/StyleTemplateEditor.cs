using ColossalFramework.UI;
using ModsCommon.UI;
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
    public class StyleTemplateEditor : BaseTemplateEditor<StyleTemplateItem, StyleTemplate, StyleTemplateIcon, StyleTemplateGroup, Style.StyleType, StyleTemplateHeaderPanel, EditStyleTemplateMode>
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

        protected override bool GroupingEnabled => Settings.GroupTemplates.value;

        private Style EditStyle { get; set; }
        private List<EditorItem> StyleProperties { get; set; } = new List<EditorItem>();

        protected override IEnumerable<StyleTemplate> GetTemplates() => TemplateManager.StyleManager.Templates.OrderBy(t => t.Name);
        protected override Style.StyleType SelectGroup(StyleTemplate editableItem)
            => Settings.GroupTemplatesType == 0 ? editableItem.Style.Type & Style.StyleType.GroupMask : editableItem.Style.Type;
        protected override string GroupName(Style.StyleType group)
            => Settings.GroupTemplatesType == 0 ? group.Description() : $"{(group & Style.StyleType.GroupMask).Description()}\n{group.Description()}";

        protected override void OnObjectSelect()
        {
            CopyStyle();
            base.OnObjectSelect();
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
                colorProperty.OnValueChanged += (Color32 c) => SelectItem.Refresh();

            return StyleProperties;
        }

        protected override void AddHeader()
        {
            base.AddHeader();
            HeaderPanel.OnSetAsDefault += ToggleAsDefault;
            HeaderPanel.OnDuplicate += Duplicate;
        }
        protected override void OnClear()
        {
            base.OnClear();
            StyleProperties.Clear();
        }
        private void AddStyleProperties() => StyleProperties = EditStyle.GetUIComponents(EditObject, PropertiesPanel, isTemplate: true);

        private void ToggleAsDefault()
        {
            TemplateManager.StyleManager.ToggleAsDefaultTemplate(EditObject);
            RefreshItems();
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

    public class StyleTemplateItem : EditableItem<StyleTemplate, StyleTemplateIcon>
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

            SetColors();
        }
    }
    public class StyleTemplateIcon : StyleIcon
    {
        public bool IsDefault { set => BorderColor = value ? new Color32(255, 215, 0, 255) : (Color32)Color.white; }
    }
    public class StyleTemplateGroup : EditableGroup<Style.StyleType, StyleTemplateItem, StyleTemplate, StyleTemplateIcon> { }
    public class EditStyleTemplateMode : EditTemplateMode<StyleTemplate> { }
}
