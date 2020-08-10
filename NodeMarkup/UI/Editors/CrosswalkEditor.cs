using ColossalFramework.UI;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class CrosswalkEditor : Editor<CrosswalkItem, MarkupCrosswalk, LineIcon>
    {
        private static CrosswalkStyle Buffer { get; set; }
        public override string Name => NodeMarkup.Localize.CrosswalkEditor_Crosswalks;
        public override string EmptyMessage => NodeMarkup.Localize.CrosswalkEditor_EmptyMessage;
        private List<UIComponent> StyleProperties { get; set; } = new List<UIComponent>();
        public StylePropertyPanel Style { get; private set; }

        public CrosswalkEditor()
        {
            SettingsPanel.autoLayoutPadding = new RectOffset(10, 10, 0, 0);
        }

        protected override void FillItems()
        {
            foreach (var line in Markup.Lines)
            {
                if (line is MarkupCrosswalk crosswalk)
                    AddItem(crosswalk);
            }
        }
        protected override void OnObjectSelect()
        {
            AddHeader();
            AddStyleTypeProperty();
            AddStyleProperties();
            if (StyleProperties.FirstOrDefault() is ColorPropertyPanel colorProperty)
                colorProperty.OnValueChanged += (Color32 c) => SelectItem.Refresh();
        }
        private void AddHeader()
        {
            var header = SettingsPanel.AddUIComponent<StyleHeaderPanel>();
            header.Init(EditObject.Rule.Style.Type, false);
            header.OnSaveTemplate += OnSaveTemplate;
            header.OnSelectTemplate += OnSelectTemplate;
            header.OnCopy += CopyStyle;
            header.OnPaste += PasteStyle;
        }
        private void AddStyleTypeProperty()
        {
            Style = SettingsPanel.AddUIComponent<CrosswalkPropertyPanel>();
            Style.Text = NodeMarkup.Localize.LineEditor_Style;
            Style.Init();
            Style.SelectedObject = EditObject.Rule.Style.Type;
            Style.OnSelectObjectChanged += StyleChanged;
        }
        private void AddStyleProperties() => StyleProperties = EditObject.Rule.Style.GetUIComponents(EditObject, SettingsPanel, isTemplate: true);
        protected override void OnObjectDelete(MarkupCrosswalk crosswalk) => Markup.RemoveConnect(crosswalk);

        private void OnSaveTemplate()
        {
            if (TemplateManager.AddTemplate(EditObject.Rule.Style, out StyleTemplate template))
                NodeMarkupPanel.EditTemplate(template);
        }
        private void ApplyStyle(CrosswalkStyle style)
        {
            if ((EditObject.Rule.Style.Type & Manager.Style.StyleType.GroupMask) != (style.Type & Manager.Style.StyleType.GroupMask))
                return;

            EditObject.Rule.Style = style.CopyCrosswalkStyle();
            Style.SelectedObject = EditObject.Rule.Style.Type;

            RefreshItem();
            ClearStyleProperties();
            AddStyleProperties();
        }
        private void OnSelectTemplate(StyleTemplate template)
        {
            if (template.Style is CrosswalkStyle style)
                ApplyStyle(style);
        }
        private void CopyStyle()
        {
            if (EarlyAccess.CheckFunctionAccess(NodeMarkup.Localize.EarlyAccess_Function_CopyStyle))
                Buffer = EditObject.Rule.Style.CopyCrosswalkStyle();
        }
        private void PasteStyle()
        {
            if (EarlyAccess.CheckFunctionAccess(NodeMarkup.Localize.EarlyAccess_Function_PasteStyle) && Buffer is CrosswalkStyle style)
                ApplyStyle(style);
        }

        private void StyleChanged(Style.StyleType style)
        {
            if (style == EditObject.Rule.Style.Type)
                return;

            var newStyle = TemplateManager.GetDefault<CrosswalkStyle>(style);
            EditObject.Rule.Style.CopyTo(newStyle);

            EditObject.Rule.Style = newStyle;

            RefreshItem();
            ClearStyleProperties();
            AddStyleProperties();
        }
        private void ClearStyleProperties()
        {
            foreach (var property in StyleProperties)
            {
                RemoveUIComponent(property);
                Destroy(property);
            }
        }
        public void RefreshItem() => SelectItem.Refresh();
    }

    public class CrosswalkItem : EditableItem<MarkupCrosswalk, StyleIcon>
    {
        public override void Init() => Init(true, true);

        public override string Description => NodeMarkup.Localize.CrossWalkEditor_ItemDescription;
        protected override void OnObjectSet() => SetIcon();
        public override void Refresh()
        {
            base.Refresh();
            SetIcon();
        }
        private void SetIcon()
        {
            Icon.Type = Object.Rule.Style.Type;
            Icon.StyleColor = Object.Rule.Style.Color;
        }
    }
}
