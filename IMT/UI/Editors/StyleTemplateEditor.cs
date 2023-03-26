using ColossalFramework.UI;
using IMT.Manager;
using IMT.Utilities;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace IMT.UI.Editors
{
    public class StyleTemplateEditor : BaseTemplateEditor<StyleTemplateItemsPanel, StyleTemplate, StyleTemplateHeaderPanel, EditStyleTemplateMode>, IPropertyContainer
    {
        public override string Name => IMT.Localize.TemplateEditor_Templates;
        public override string EmptyMessage => string.Format(IMT.Localize.TemplateEditor_EmptyMessage, IMT.Localize.HeaderPanel_SaveAsTemplate);
        public override Marking.SupportType Support => Marking.SupportType.StyleTemplates;
        protected override string IsAssetMessage => IMT.Localize.TemplateEditor_TemplateIsAsset;
        protected override string RewriteCaption => IMT.Localize.TemplateEditor_RewriteCaption;
        protected override string RewriteMessage => IMT.Localize.TemplateEditor_RewriteMessage;
        protected override string SaveChangesMessage => IMT.Localize.TemplateEditor_SaveChangesMessage;
        protected override string NameExistMessage => IMT.Localize.TemplateEditor_NameExistMessage;
        protected override string IsAssetWarningMessage => IMT.Localize.TemplateEditor_IsAssetWarningMessage;
        protected override string IsWorkshopWarningMessage => IMT.Localize.TemplateEditor_IsWorkshopWarningMessage;

        private Style EditStyle { get; set; }

        object IPropertyEditor.EditObject => EditObject;
        bool IPropertyEditor.IsTemplate => true;
        CustomUIPanel IPropertyContainer.MainPanel => PropertiesPanel;
        Style IPropertyContainer.Style => EditStyle;
        Dictionary<string, bool> IPropertyContainer.ExpandList { get; } = new Dictionary<string, bool>();

        Dictionary<string, IPropertyCategoryInfo> IPropertyContainer.CategoryInfos { get; } = new Dictionary<string, IPropertyCategoryInfo>();
        Dictionary<string, List<IPropertyInfo>> IPropertyContainer.PropertyInfos { get; } = new Dictionary<string, List<IPropertyInfo>>();
        Dictionary<string, CategoryItem> IPropertyContainer.CategoryItems { get; } = new Dictionary<string, CategoryItem>();
        List<BaseEditorPanel> IPropertyContainer.StyleProperties { get; } = new List<BaseEditorPanel>();

        protected override IEnumerable<StyleTemplate> GetObjects() => SingletonManager<StyleTemplateManager>.Instance.Templates;

        protected override void FillProperties()
        {
            CopyStyle();
            base.FillProperties();
            AddStyleType();
        }

        private void AddStyleType()
        {
            var styleProperty = ComponentPool.Get<StringPropertyPanel>(PropertiesPanel, "Style");
            styleProperty.SetStyle(PropertyStyle.Default);
            styleProperty.Label = IMT.Localize.Editor_Style;
            styleProperty.FieldWidth = 230;
            styleProperty.EnableControl = false;
            styleProperty.Init();
            styleProperty.Value = EditStyle.Type.Description();
        }

        void IPropertyEditor.RefreshProperties() => PropertyEditorHelper.RefreshProperties(this);

        protected override void AddAditionalProperties() => this.AddProperties();
        protected override void ClearAdditionalProperties() => this.ClearProperties();
        protected override void RefreshAdditionalProperties() => this.RefreshProperties();

        private void CopyStyle()
        {
            EditStyle = EditObject.Style.Copy();
            EditStyle.OnStyleChanged = OnChanged;
        }

        protected override void AddHeader()
        {
            base.AddHeader();
            HeaderPanel.OnSetAsDefault += ToggleAsDefault;
            HeaderPanel.OnDuplicate += Duplicate;
            HeaderPanel.OnApplySameStyle += ApplyStyleSameStyle;
            HeaderPanel.OnApplySameType += ApplyStyleSameType;
        }

        protected override void SetEditable(EditMode mode)
        {
            base.SetEditable(mode);

            foreach (var property in (this as IPropertyContainer).StyleProperties)
                property.EnableControl = EditMode;
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
        private void ApplyStyleSameStyle()
        {
            switch (EditStyle)
            {
                case RegularLineStyle regularStyle:
                    foreach (var line in Marking.Lines)
                    {
                        foreach (var rule in line.Rules)
                        {
                            if (rule.Style.Value.Type == regularStyle.Type)
                                rule.Style.Value = regularStyle.CopyStyle();
                        }
                    }
                    break;
                case StopLineStyle stopStyle:
                    foreach (var line in Marking.Lines)
                    {
                        foreach (var rule in line.Rules)
                        {
                            if (rule.Style.Value.Type == stopStyle.Type)
                                rule.Style.Value = stopStyle.CopyStyle();
                        }
                    }
                    break;
                case BaseCrosswalkStyle crosswalkStyle:
                    foreach (var crosswalk in Marking.Crosswalks)
                    {
                        if (crosswalk.Style.Value.Type == crosswalkStyle.Type)
                            crosswalk.Style.Value = crosswalkStyle.CopyStyle();
                    }
                    break;
                case BaseFillerStyle fillerStyle:
                    foreach (var filler in Marking.Fillers)
                    {
                        if (filler.Style.Value.Type == fillerStyle.Type)
                            filler.Style.Value = fillerStyle.CopyStyle();
                    }
                    break;
            }

            Panel.UpdatePanel();
        }
        private void ApplyStyleSameType()
        {
            switch (EditStyle)
            {
                case RegularLineStyle regularStyle:
                    foreach (var line in Marking.Lines)
                    {
                        if ((regularStyle.Type.GetLineType() & line.Type) == 0 || (regularStyle.Type.GetNetworkType() & line.PointPair.NetworkType) == 0)
                            continue;

                        foreach (var rule in line.Rules)
                            rule.Style.Value = regularStyle.CopyStyle();
                    }
                    break;
                case StopLineStyle stopStyle:
                    foreach (var line in Marking.Lines)
                    {
                        if ((stopStyle.Type.GetLineType() & line.Type) == 0 || (stopStyle.Type.GetNetworkType() & line.PointPair.NetworkType) == 0)
                            continue;

                        foreach (var rule in line.Rules)
                            rule.Style.Value = stopStyle.CopyStyle();
                    }
                    break;
                case BaseCrosswalkStyle crosswalkStyle:
                    foreach (var crosswalk in Marking.Crosswalks)
                        crosswalk.Style.Value = crosswalkStyle.CopyStyle();
                    break;
                case BaseFillerStyle fillerStyle:
                    foreach (var filler in Marking.Fillers)
                    {
                        filler.Style.Value = fillerStyle.CopyStyle();
                    }
                    break;
            }

            Panel.UpdatePanel();
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
    public class StyleTemplateItem : EditItem<StyleTemplate, StyleIcon>
    {
        public override bool ShowDelete => EditObject != null && !EditObject.IsAsset;
        private bool IsDefault => EditObject?.IsDefault == true;

        public override ModsCommon.UI.SpriteSet ForegroundSprites => !IsDefault ? base.ForegroundSprites : new ModsCommon.UI.SpriteSet()
        {
            normal = CommonTextures.BorderBig,
            hovered = CommonTextures.PanelSmall,
            pressed = CommonTextures.PanelSmall,
            focused = CommonTextures.BorderBig,
            disabled = CommonTextures.PanelSmall,
        };
        public override ModsCommon.UI.SpriteSet ForegroundSelectedSprites => !IsDefault ? base.ForegroundSelectedSprites : new ModsCommon.UI.SpriteSet(CommonTextures.PanelSmall);

        public override ColorSet ForegroundColors => !IsDefault ? base.ForegroundColors : new ColorSet()
        {
            normal = IMTColors.ItemFavoriteNormal,
            hovered = IMTColors.ItemFavoriteNormal,
            pressed = IMTColors.ItemFavoritePressed,
            focused = IMTColors.ItemFavoriteFocused,
            disabled = default,
        };
        public override ColorSet ForegroundSelectedColors => !IsDefault ? base.ForegroundSelectedColors : new ColorSet(IMTColors.ItemFavoriteFocused);

        public override ColorSet DefaultTextColor => !IsDefault ? base.DefaultTextColor : new ColorSet()
        {
            normal = Color.white,
            hovered = Color.black,
            pressed = Color.black,
            focused = Color.white,
            disabled = Color.white,
        };
        public override ColorSet DefaultSelTextColor => !IsDefault ? base.DefaultSelTextColor : new ColorSet(Color.white);

        public override void Refresh()
        {
            base.Refresh();
            Icon.Type = EditObject.Style.Type;
            Icon.StyleColor = EditObject.Style is IColorStyle ? EditObject.Style.Color : Color.white;
            WordWrap = !EditObject.IsAsset;

            SetStyle();
        }
    }

    public class StyleTemplateGroup : EditGroup<Style.StyleType, StyleTemplateItem, StyleTemplate> { }
    public class EditStyleTemplateMode : EditTemplateMode<StyleTemplate> { }
}
