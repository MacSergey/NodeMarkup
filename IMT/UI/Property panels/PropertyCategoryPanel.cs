using ColossalFramework.UI;
using IMT.Manager;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
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
        void Init(IPropertyCategoryInfo category, object editObject);
    }

    public abstract class BasePropertyCategoryPanel<TypeHeader> : PropertyGroupPanel, IPropertyCategoryPanel
        where TypeHeader : BaseCategoryHeaderPanel
    {
        private static Dictionary<string, bool> ExpandList { get; } = new Dictionary<string, bool>();
        protected override UITextureAtlas Atlas => IMTTextures.Atlas;
        protected override string BackgroundSprite => IMTTextures.ButtonWhiteBorder;

        protected object EditObject { get; private set; }
        protected TypeHeader Header { get; private set; }

        public bool? IsExpand
        {
            get
            {
                if (ExpandList.TryGetValue(Category.Name, out var isExpand))
                    return isExpand;
                else
                    return null;
            }
            set
            {
                if (value == null)
                {
                    ExpandList.Remove(Category.Name);
                }
                else
                {
                    ExpandList[Category.Name] = value.Value;
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

        public virtual void Init(IPropertyCategoryInfo category, object editObject)
        {
            Category = category;
            EditObject = editObject;

            Header = ComponentPool.Get<TypeHeader>(this, nameof(Header));
            Header.Init(editObject);
            Header.eventClick += HeaderClick;
            Header.Category = category.Text;

            IsExpand ??= category.IsExpand;

            base.Init();
        }

        public override void DeInit()
        {
            base.DeInit();
            Header = null;
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
        public override void Init(IPropertyCategoryInfo category, object editObject)
        {
            base.Init(category, editObject);

            Header.OnApplySameType += ApplySameType;
            Header.OnApplySameStyle += ApplySameStyle;
            Header.OnApplyAll += OnApplyAll;

            Header.Init(editObject);
        }

        private void ApplySameStyle()
        {
            switch (EditObject)
            {
                case MarkingLineRawRule editRule:
                    foreach (var rule in editRule.Line.Rules)
                    {
                        if (rule != editRule && rule.Style.Value.Type == editRule.Style.Value.Type)
                            editRule.Style.Value.CopyEffectsTo(rule.Style.Value);
                    }
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
            switch (EditObject)
            {
                case MarkingLineRawRule editRule:
                    foreach (var rule in editRule.Line.Rules)
                    {
                        if (rule != editRule)
                            editRule.Style.Value.CopyEffectsTo(rule.Style.Value);
                    }
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
            switch (EditObject)
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

        protected object EditObject { get; private set; }
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

        public void Init(object editObject)
        {
            EditObject = editObject;
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
            ExpandButton.relativePosition = new Vector2(ExpandButton.relativePosition.x, (height - ExpandButton.height) / 2);
            NameLabel.relativePosition = new Vector2(NameLabel.relativePosition.x, (height - NameLabel.height) / 2);
            Content.height = height;
            Content.relativePosition = new Vector2(width - Content.width - padding.right, 0);
        }
        public override void DeInit()
        {
            EditObject = null;
            IsExpand = false;
            Category = string.Empty;
        }
    }
    public class DefaultCategoryHeaderPanel : BaseCategoryHeaderPanel { }
    public class EffectCategoryHeaderPanel : BaseCategoryHeaderPanel
    {
        private static EffectData Buffer { get; set; }

        public event Action OnApplySameStyle;
        public event Action OnApplySameType;
        public event Action OnApplyAll;

        private HeaderButtonInfo<HeaderButton> Copy { get; set; }
        private HeaderButtonInfo<HeaderButton> Paste { get; set; }
        private HeaderButtonInfo<HeaderButton> ApplySameStyle { get; set; }
        private HeaderButtonInfo<HeaderButton> ApplySameType { get; set; }
        private HeaderButtonInfo<HeaderButton> ApplyAll { get; set; }

        public EffectCategoryHeaderPanel()
        {
            Copy = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.CopyButtonIcon, "Copy effects", CopyEffects);
            Content.AddButton(Copy);

            Paste = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.PasteButtonIcon, "Paste effects", PasteEffects);
            Content.AddButton(Paste);

            ApplySameStyle = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, IMTTextures.Atlas, IMTTextures.CopyHeaderButton, "Apply to same style", ApplySameStyleClick);
            Content.AddButton(ApplySameStyle);

            ApplySameType = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, IMTTextures.Atlas, IMTTextures.CopyHeaderButton, "Apply to same type", ApplySameTypeClick);
            Content.AddButton(ApplySameType);

            ApplyAll = new HeaderButtonInfo<HeaderButton>(HeaderButtonState.Additional, IMTTextures.Atlas, IMTTextures.CopyHeaderButton, "Apply to all items", ApplyAllClick);
            Content.AddButton(ApplyAll);
        }

        public override void DeInit()
        {
            base.DeInit();
            OnApplySameStyle = null;
            OnApplySameType = null;
            OnApplyAll = null;
        }

        public override void Refresh()
        {
            switch (EditObject)
            {
                case MarkingLineRawRule editRule:
                    {
                        ApplySameStyle.Text = $"Apply to all \"{editRule.Style.Value.Type.Description()}\" lines";
                        ApplySameType.Text = $"Apply to all lines";

                        var isEffect = editRule.Style.Value is IEffectStyle;
                        Copy.Visible = isEffect;
                        Paste.Visible = isEffect;
                    }
                    break;
                case MarkingCrosswalk editCrosswalk:
                    {
                        ApplySameStyle.Text = $"Apply to all \"{editCrosswalk.Style.Value.Type.Description()}\" crosswalks";
                        ApplySameType.Text = $"Apply to all crosswalks";

                        var isEffect = editCrosswalk.Style.Value is IEffectStyle;
                        Copy.Visible = isEffect;
                        Paste.Visible = isEffect;
                    }
                    break;
                case MarkingFiller editFiller:
                    {
                        ApplySameStyle.Text = $"Apply to all \"{editFiller.Style.Value.Type.Description()}\" fillers";
                        ApplySameType.Text = $"Apply to all fillers";

                        var isEffect = editFiller.Style.Value is IEffectStyle;
                        Copy.Visible = isEffect;
                        Paste.Visible = isEffect;
                    }
                    break;
            }

            base.Refresh();
        }

        private void CopyEffects()
        {
            switch (EditObject)
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
            switch (EditObject)
            {
                case MarkingLineRawRule editRule:
                    editRule.Style.Value.Effects = Buffer;
                    break;
                case MarkingCrosswalk editCrosswalk:
                    editCrosswalk.Style.Value.Effects = Buffer;
                    break;
                case MarkingFiller editFiller:
                    editFiller.Style.Value.Effects = Buffer;
                    break;
            }
        }
        private void ApplySameStyleClick() => OnApplySameStyle?.Invoke();
        private void ApplySameTypeClick() => OnApplySameType?.Invoke();
        private void ApplyAllClick() => OnApplyAll?.Invoke();
    }

    public class CategoryHeaderContent : BaseHeaderContent
    {
        protected override int MainButtonSize => 20;
        protected override int MainIconPadding => 0;

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
