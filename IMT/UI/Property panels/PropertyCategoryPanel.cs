using ColossalFramework.UI;
using IMT.Manager;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using UnityEngine;

namespace IMT.UI.Editors
{
    public class CategoryItem : UIAutoLayoutPanel, IReusable
    {
        bool IReusable.InCache { get; set; }
        public PropertyGroupPanel CategoryPanel { get; private set; }

        public CategoryItem()
        {
            autoLayoutDirection = LayoutDirection.Vertical;
            autoLayoutPadding = new RectOffset(3, 3, 3, 3);
            verticalSpacing = 3;
            autoFitChildrenVertically = true;
        }

        public TypePanel Init<TypePanel>(string name)
            where TypePanel : PropertyGroupPanel, IPropertyCategoryPanel
        {
            if (CategoryPanel != null)
                ComponentPool.Free(CategoryPanel);

            CategoryPanel = ComponentPool.Get<TypePanel>(this, name);
            return (TypePanel)CategoryPanel;
        }

        void IReusable.DeInit()
        {
            ComponentPool.Free(CategoryPanel);
            CategoryPanel = null;
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            if (CategoryPanel != null)
                CategoryPanel.width = width - autoLayoutPadding.horizontal;
        }
    }
    public interface IPropertyCategoryPanel
    {
        void Init(IPropertyCategoryInfo category, IPropertyContainer editor);
    }

    public abstract class BasePropertyCategoryPanel<TypeHeader> : PropertyGroupPanel, IPropertyCategoryPanel
        where TypeHeader : BaseCategoryHeaderPanel
    {
        protected override UITextureAtlas Atlas => IMTTextures.Atlas;
        protected override string BackgroundSprite => IMTTextures.ButtonWhiteBorder;

        protected IPropertyContainer Editor { get; private set; }
        protected TypeHeader Header { get; private set; }

        public bool? IsExpand
        {
            get
            {
                if (Editor.ExpandList.TryGetValue(Category.Name, out var isExpand))
                    return isExpand;
                else
                    return null;
            }
            set
            {
                if (value != null)
                {
                    Editor.ExpandList[Category.Name] = value.Value;
                    Header.IsExpand = value.Value;

                    foreach (var item in components)
                    {
                        if (item is not TypeHeader && item is EditorItem editorItem)
                            editorItem.IsCollapsed = !value.Value;
                    }
                }
            }
        }

        public IPropertyCategoryInfo Category { get; private set; }

        public BasePropertyCategoryPanel()
        {
            verticalSpacing = 3;
            padding = new RectOffset(0, 0, 2, 0);
            autoLayoutPadding = new RectOffset(2, 2, 0, 0);
        }

        public virtual void Init(IPropertyCategoryInfo category, IPropertyContainer editor)
        {
            Category = category;
            Editor = editor;

            Header = ComponentPool.Get<TypeHeader>(this, nameof(Header));
            Header.Category = category.Text;
            Header.Init(editor);
            Header.eventClick += HeaderClick;

            IsExpand = IsExpand ?? category.IsExpand;

            base.Init();
        }

        public override void DeInit()
        {
            base.DeInit();
            Header = null;
            Editor = null;
        }

        private void HeaderClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (!eventParam.used)
                IsExpand = !IsExpand;
        }
        protected override void OnComponentAdded(UIComponent child)
        {
            base.OnComponentAdded(child);

            if (child is not TypeHeader && child is EditorItem item)
                item.IsCollapsed = IsExpand != true;
        }
    }
    public class DefaultPropertyCategoryPanel : BasePropertyCategoryPanel<DefaultCategoryHeaderPanel> { }
    public class EffectPropertyCategoryPanel : BasePropertyCategoryPanel<EffectCategoryHeaderPanel>
    {
        private static EffectData Buffer { get; set; }

        public override void Init(IPropertyCategoryInfo category, IPropertyContainer editor)
        {
            base.Init(category, editor);

            Header.OnCopy += CopyEffects;
            Header.OnPaste += PasteEffects;
            Header.OnApplyAllRules += ApplyAllRules;
            Header.OnApplySameType += ApplySameType;
            Header.OnApplySameStyle += ApplySameStyle;
            Header.OnApplyAll += OnApplyAll;
        }

        private void CopyEffects()
        {
            switch (Editor.EditObject)
            {
                case MarkingLineRawRule editRule:
                    Buffer = editRule.Style.Value.Effects;
                    break;
                case MarkingCrosswalk editCrosswalk:
                    Buffer = editCrosswalk.Style.Value.Effects;
                    break;
                case MarkingFiller editFiller:
                    Buffer = editFiller.Style.Value.Effects;
                    break;
            }
        }
        private void PasteEffects()
        {
            switch (Editor.EditObject)
            {
                case MarkingLineRawRule editRule:
                    editRule.Style.Value.Effects = Buffer;
                    Editor.RefreshProperties();
                    break;
                case MarkingCrosswalk editCrosswalk:
                    editCrosswalk.Style.Value.Effects = Buffer;
                    Editor.RefreshProperties();
                    break;
                case MarkingFiller editFiller:
                    editFiller.Style.Value.Effects = Buffer;
                    Editor.RefreshProperties();
                    break;
            }
        }
        private void ApplyAllRules()
        {
            switch (Editor.EditObject)
            {
                case MarkingLineRawRule editRule:
                    foreach (var rule in editRule.Line.Rules)
                    {
                        if (rule != editRule)
                            editRule.Style.Value.CopyEffectsTo(rule.Style.Value);
                    }
                    Editor.RefreshProperties();
                    break;
            }
        }
        private void ApplySameStyle()
        {
            switch (Editor.EditObject)
            {
                case MarkingLineRawRule editRule:
                    foreach (var line in editRule.Line.Marking.Lines)
                    {
                        foreach (var rule in line.Rules)
                        {
                            if (rule != editRule && rule.Style.Value.Type == editRule.Style.Value.Type)
                                editRule.Style.Value.CopyEffectsTo(rule.Style.Value);
                        }
                    }
                    Editor.RefreshProperties();
                    break;
                case MarkingCrosswalk editCrosswalk:
                    foreach (var crosswalk in editCrosswalk.Marking.Crosswalks)
                    {
                        if (crosswalk != editCrosswalk && crosswalk.Style.Value.Type == editCrosswalk.Style.Value.Type)
                            editCrosswalk.Style.Value.CopyEffectsTo(crosswalk.Style.Value);
                    }
                    break;
                case MarkingFiller editFiller:
                    foreach (var filler in editFiller.Marking.Fillers)
                    {
                        if (filler != editFiller && filler.Style.Value.Type == editFiller.Style.Value.Type)
                            editFiller.Style.Value.CopyEffectsTo(filler.Style.Value);
                    }
                    break;
            }
        }
        private void ApplySameType()
        {
            switch (Editor.EditObject)
            {
                case MarkingLineRawRule editRule:
                    foreach (var line in editRule.Line.Marking.Lines)
                    {
                        foreach (var rule in line.Rules)
                        {
                            if (rule != editRule)
                                editRule.Style.Value.CopyEffectsTo(rule.Style.Value);
                        }
                    }
                    Editor.RefreshProperties();
                    break;
                case MarkingCrosswalk editCrosswalk:
                    foreach (var crosswalk in editCrosswalk.Marking.Crosswalks)
                    {
                        if (crosswalk != editCrosswalk)
                            editCrosswalk.Style.Value.CopyEffectsTo(crosswalk.Style.Value);
                    }
                    break;
                case MarkingFiller editFiller:
                    foreach (var filler in editFiller.Marking.Fillers)
                    {
                        if (filler != editFiller)
                            editFiller.Style.Value.CopyEffectsTo(filler.Style.Value);
                    }
                    break;
            }
        }
        private void OnApplyAll()
        {
            Style source;
            Marking marking;
            switch (Editor.EditObject)
            {
                case MarkingLineRawRule editRule:
                    source = editRule.Style.Value;
                    marking = editRule.Line.Marking;
                    break;
                case MarkingCrosswalk editCrosswalk:
                    source = editCrosswalk.Style.Value;
                    marking = editCrosswalk.Marking;
                    break;
                case MarkingFiller editFiller:
                    source = editFiller.Style.Value;
                    marking = editFiller.Marking;
                    break;
                default:
                    return;
            }

            foreach (var line in marking.Lines)
            {
                foreach (var rule in line.Rules)
                {
                    source.CopyEffectsTo(rule.Style.Value);
                }
            }
            foreach (var crosswalk in marking.Crosswalks)
            {
                source.CopyEffectsTo(crosswalk.Style.Value);
            }
            foreach (var filler in marking.Fillers)
            {
                source.CopyEffectsTo(filler.Style.Value);
            }

            Editor.RefreshProperties();
        }
    }

    public abstract class BaseCategoryHeaderPanel : BaseHeaderPanel<CategoryHeaderContent>
    {
        protected override float DefaultHeight => 24f;
        protected virtual Color32 Color => new Color32(177, 195, 94, 255);
        protected virtual string BackgroundSprite => "ButtonWhite";
        protected virtual UITextureAtlas Atlas => TextureHelper.InGameAtlas;

        protected CustomUIButton ExpandButton { get; set; }
        protected CustomUILabel NameLabel { get; set; }

        protected IPropertyEditor Editor { get; private set; }
        public string Category
        {
            get => NameLabel.text;
            set => NameLabel.text = value;
        }
        public bool IsExpand { set => ExpandButton.normalBgSprite = value ? IMTTextures.ListItemCollapse : IMTTextures.ListItemExpand; }

        public BaseCategoryHeaderPanel()
        {
            atlas = Atlas;
            backgroundSprite = BackgroundSprite;
            color = Color;
            padding = new RectOffset(3, 5, 0, 0);

            AddCollapseButton();
            AddLabel();
        }

        private void AddCollapseButton()
        {
            ExpandButton = AddUIComponent<CustomUIButton>();
            ExpandButton.atlas = IMTTextures.Atlas;
            ExpandButton.size = new Vector2(20, 20);
            ExpandButton.zOrder = 0;
        }
        private void AddLabel()
        {
            NameLabel = AddUIComponent<CustomUILabel>();
            NameLabel.textScale = 0.8f;
            NameLabel.autoSize = true;
            NameLabel.padding = new RectOffset(0, 0, 2, 0);
            NameLabel.zOrder = 1;
        }

        public void Init(IPropertyEditor editor)
        {
            Editor = editor;
            base.Init();
        }
        public override void Refresh()
        {
            Content.Refresh();

            autoLayout = true;
            autoLayout = false;

            SetSize();
        }
        protected override void SetSize()
        {
            ExpandButton.relativePosition = new Vector2(ExpandButton.relativePosition.x, (height - ExpandButton.height) * 0.5f);
            NameLabel.relativePosition = new Vector2(NameLabel.relativePosition.x, (height - NameLabel.height) * 0.5f);
            Content.height = height;
            Content.relativePosition = new Vector2(width - Content.width - padding.right, 0);
        }
        public override void DeInit()
        {
            Editor = null;
            IsExpand = false;
            Category = string.Empty;
        }
    }
    public class DefaultCategoryHeaderPanel : BaseCategoryHeaderPanel { }
    public class EffectCategoryHeaderPanel : BaseCategoryHeaderPanel
    {
        public event Action OnCopy;
        public event Action OnPaste;
        public event Action OnApplyAllRules;
        public event Action OnApplySameStyle;
        public event Action OnApplySameType;
        public event Action OnApplyAll;

        private HeaderButtonInfo<HeaderButton> Copy { get; set; }
        private HeaderButtonInfo<HeaderButton> Paste { get; set; }
        private HeaderButtonInfo<HeaderButton> ApplyAllRules { get; set; }
        private HeaderButtonInfo<HeaderButton> ApplySameStyle { get; set; }
        private HeaderButtonInfo<HeaderButton> ApplySameType { get; set; }
        private HeaderButtonInfo<HeaderButton> ApplyAll { get; set; }

        public EffectCategoryHeaderPanel()
        {
            Copy = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.CopyButtonIcon, IMT.Localize.HeaderPanel_CopyEffects, CopyClick);
            Content.AddButton(Copy);

            Paste = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.PasteButtonIcon, IMT.Localize.HeaderPanel_PasteEffects, PasteClick);
            Content.AddButton(Paste);

            ApplyAllRules = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.ApplyButtonIcon, IMT.Localize.HeaderPanel_ApplyAllRules, ApplyAllRulesClick);
            Content.AddButton(ApplyAllRules);

            ApplySameStyle = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, IMTTextures.Atlas, IMTTextures.CopyToSameButtonIcon, string.Empty, ApplySameStyleClick);
            Content.AddButton(ApplySameStyle);

            ApplySameType = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, IMTTextures.Atlas, IMTTextures.CopyToAllButtonIcon, string.Empty, ApplySameTypeClick);
            Content.AddButton(ApplySameType);

            ApplyAll = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, IMTTextures.Atlas, IMTTextures.ApplyAllButtonIcon, IMT.Localize.HeaderPanel_ApplyAll, ApplyAllClick);
            Content.AddButton(ApplyAll);
        }

        public override void DeInit()
        {
            base.DeInit();
            OnCopy = null;
            OnPaste = null;
            OnApplyAllRules = null;
            OnApplySameStyle = null;
            OnApplySameType = null;
            OnApplyAll = null;
        }

        public override void Refresh()
        {
            switch (Editor.EditObject)
            {
                case MarkingLineRawRule editRule:
                    {
                        ApplySameStyle.Visible = true;
                        ApplySameType.Visible = true;
                        ApplyAll.Visible = true;

                        ApplyAllRules.Visible = editRule.Line.IsSupportRules;
                        if (editRule.Line.Type == LineType.Stop)
                        {
                            ApplySameStyle.Text = string.Format(IMT.Localize.HeaderPanel_ApplyStopType, editRule.Style.Value.Type.Description());
                            ApplySameType.Text = IMT.Localize.HeaderPanel_ApplyStopAll;
                        }
                        else
                        {
                            ApplySameStyle.Text = string.Format(IMT.Localize.HeaderPanel_ApplyRegularType, editRule.Style.Value.Type.Description());
                            ApplySameType.Text = IMT.Localize.HeaderPanel_ApplyRegularAll;
                        }

                        var isEffect = editRule.Style.Value is IEffectStyle;
                        Copy.Visible = isEffect;
                        Paste.Visible = isEffect;
                    }
                    break;
                case MarkingCrosswalk editCrosswalk:
                    {
                        ApplySameStyle.Visible = true;
                        ApplySameType.Visible = true;
                        ApplyAll.Visible = true;

                        ApplyAllRules.Visible = false;
                        ApplySameStyle.Text = string.Format(IMT.Localize.HeaderPanel_ApplyCrosswalkType, editCrosswalk.Style.Value.Type.Description());
                        ApplySameType.Text = IMT.Localize.HeaderPanel_ApplyCrosswalkAll;

                        var isEffect = editCrosswalk.Style.Value is IEffectStyle;
                        Copy.Visible = isEffect;
                        Paste.Visible = isEffect;
                    }
                    break;
                case MarkingFiller editFiller:
                    {
                        ApplySameStyle.Visible = true;
                        ApplySameType.Visible = true;
                        ApplyAll.Visible = true;

                        ApplyAllRules.Visible = false;
                        ApplySameStyle.Text = string.Format(IMT.Localize.HeaderPanel_ApplyFillerType, editFiller.Style.Value.Type.Description());
                        ApplySameType.Text = IMT.Localize.HeaderPanel_ApplyFillerAll;

                        var isEffect = editFiller.Style.Value is IEffectStyle;
                        Copy.Visible = isEffect;
                        Paste.Visible = isEffect;
                    }
                    break;
                default:
                    {
                        Copy.Visible = false;
                        Paste.Visible = false;
                        ApplyAllRules.Visible = false;
                        ApplySameStyle.Visible = false;
                        ApplySameType.Visible = false;
                        ApplyAll.Visible = false;
                    }
                    break;
            }

            base.Refresh();
        }

        private void CopyClick() => OnCopy?.Invoke();
        private void PasteClick() => OnPaste?.Invoke();
        private void ApplyAllRulesClick() => OnApplyAllRules?.Invoke();
        private void ApplySameStyleClick() => OnApplySameStyle?.Invoke();
        private void ApplySameTypeClick() => OnApplySameType?.Invoke();
        private void ApplyAllClick() => OnApplyAll?.Invoke();
    }

    public class CategoryHeaderContent : BaseHeaderContent
    {
        protected override int MainButtonSize => 20;
        protected override int MainIconPadding => 0;

        protected override int AdditionalButtonSize => 24;
        protected override UITextureAtlas AdditionalButtonAtlas => IMTTextures.Atlas;
        protected override string AdditionalButtonSprite => IMTTextures.AdditionalButtonIcon;

        protected override Color32 ButtonHoveredColor => new Color32(32, 32, 32, 255);
        protected override Color32 ButtonPressedColor => Color.black;
        protected override Color32 AdditionalButtonHoveredColor => new Color32(112, 112, 112, 255);
        protected override Color32 AdditionalButtonPressedColor => new Color32(144, 144, 144, 255);

        protected override Color32 IconNormalColor => Color.white;
        protected override Color32 IconHoverColor => Color.white;
        protected override Color32 IconPressedColor => new Color32(224, 224, 224, 255);
        protected override Color32 IconDisabledColor => new Color32(144, 144, 144, 255);
    }
}
