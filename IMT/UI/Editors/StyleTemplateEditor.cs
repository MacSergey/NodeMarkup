using IMT.Manager;
using IMT.Utilities;
using ModsCommon;
using ModsCommon.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IMT.UI.Editors
{
    public class StyleTemplateEditor : BaseTemplateEditor<StyleTemplateItemsPanel, StyleTemplate, StyleTemplateHeaderPanel, EditStyleTemplateMode>
    {
        public override string Name => IMT.Localize.TemplateEditor_Templates;
        public override string EmptyMessage => string.Format(IMT.Localize.TemplateEditor_EmptyMessage, IMT.Localize.HeaderPanel_SaveAsTemplate);
        public override Marking.SupportType Support { get; } = Marking.SupportType.StyleTemplates;
        protected override string IsAssetMessage => IMT.Localize.TemplateEditor_TemplateIsAsset;
        protected override string RewriteCaption => IMT.Localize.TemplateEditor_RewriteCaption;
        protected override string RewriteMessage => IMT.Localize.TemplateEditor_RewriteMessage;
        protected override string SaveChangesMessage => IMT.Localize.TemplateEditor_SaveChangesMessage;
        protected override string NameExistMessage => IMT.Localize.TemplateEditor_NameExistMessage;
        protected override string IsAssetWarningMessage => IMT.Localize.TemplateEditor_IsAssetWarningMessage;
        protected override string IsWorkshopWarningMessage => IMT.Localize.TemplateEditor_IsWorkshopWarningMessage;

        private Style EditStyle { get; set; }
        private List<EditorItem> StyleProperties { get; set; } = new List<EditorItem>();

        protected override IEnumerable<StyleTemplate> GetObjects() => SingletonManager<StyleTemplateManager>.Instance.Templates;

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
            SingletonManager<StyleTemplateManager>.Instance.ToggleAsDefaultTemplate(EditObject);
            ItemsPanel.RefreshItems();
            HeaderPanel.Refresh();
        }
        private void Duplicate()
        {
            if (SingletonManager<StyleTemplateManager>.Instance.DuplicateTemplate(EditObject, out StyleTemplate duplicate))
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
        protected override bool SaveAsset(StyleTemplate template) => SingletonManager<StyleTemplateManager>.Instance.MakeAsset(template);
    }

    public class StyleTemplateItemsPanel : ItemsGroupPanel<StyleTemplateItem, StyleTemplate, StyleTemplateGroup, Style.StyleType>
    {
        public override bool GroupingEnable => Settings.GroupTemplates.value;
        public override int Compare(StyleTemplate x, StyleTemplate y)
        {
            var result = 0;

            if (Settings.SortTemplatesType == 0)
            {
                if ((result = SortByAuthor(x, y)) == 0)
                    if ((result = SortByType(x, y)) == 0)
                        result = SortByName(x, y);
            }
            else if (Settings.SortTemplatesType == 1)
            {
                if ((result = SortByType(x, y)) == 0)
                    result = SortByName(x, y);
            }
            else if (Settings.SortTemplatesType == 2)
            {
                if ((result = SortByName(x, y)) == 0)
                    result = SortByType(x, y);
            }

            return result;

            static int SortByAuthor(StyleTemplate x, StyleTemplate y) => (x.Asset?.Author ?? string.Empty).CompareTo(y.Asset?.Author ?? string.Empty);
            static int SortByType(StyleTemplate x, StyleTemplate y) => x.Style.Type.CompareTo(y.Style.Type);
            static int SortByName(StyleTemplate x, StyleTemplate y) => x.Name.CompareTo(y.Name);
        }
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
