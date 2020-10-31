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
    public class StyleTemplateEditor : BaseTemplateEditor<TemplateItem, StyleTemplate, TemplateIcon, TemplateGroup, Style.StyleType, StyleTemplateHeaderPanel>
    {
        public override string Name => NodeMarkup.Localize.TemplateEditor_Templates;
        public override string EmptyMessage => string.Format(NodeMarkup.Localize.TemplateEditor_EmptyMessage, NodeMarkup.Localize.HeaderPanel_SaveAsTemplate);
        protected override bool GroupingEnabled => Settings.GroupTemplates.value;

        private List<UIComponent> StyleProperties { get; set; } = new List<UIComponent>();

        protected override IEnumerable<StyleTemplate> GetTemplates() => TemplateManager.StyleManager.Templates.OrderBy(t => t.Style.Type);
        protected override Style.StyleType SelectGroup(StyleTemplate editableItem)
            => Settings.GroupTemplatesType == 0 ? editableItem.Style.Type & Style.StyleType.GroupMask : editableItem.Style.Type;
        protected override string GroupName(Style.StyleType group)
            => Settings.GroupTemplatesType == 0 ? group.Description() : $"{(group & Style.StyleType.GroupMask).Description()}\n{group.Description()}";

        protected override void AddAditional()
        {
            AddStyleProperties();
            if (StyleProperties.FirstOrDefault() is ColorPropertyPanel colorProperty)
                colorProperty.OnValueChanged += (Color32 c) => SelectItem.Refresh();
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
        private void AddStyleProperties() => StyleProperties = EditObject.Style.GetUIComponents(EditObject, PropertiesPanel, isTemplate: true);

        private void ToggleAsDefault()
        {
            TemplateManager.StyleManager.ToggleAsDefaultTemplate(EditObject);
            RefreshItems();
            HeaderPanel.Init(EditObject);
        }
        private void Duplicate()
        {
            if (TemplateManager.StyleManager.DuplicateTemplate(EditObject, out StyleTemplate duplicate))
                NodeMarkupPanel.EditTemplate(duplicate);
        }
    }

    public class TemplateItem : EditableItem<StyleTemplate, TemplateIcon>
    {
        public override bool ShowDelete => !Object.IsAsset;

        private bool IsDefault => Object.IsDefault;
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
    public class TemplateIcon : StyleIcon
    {
        public bool IsDefault { set => BorderColor = value ? new Color32(255, 215, 0, 255) : (Color32)Color.white; }
    }
    public class TemplateGroup : EditableGroup<Style.StyleType, TemplateItem, StyleTemplate, TemplateIcon> { }
}
