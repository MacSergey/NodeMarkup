using ColossalFramework.UI;
using IMT.Manager;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Linq;
using UnityEngine;
using LayoutStart = ModsCommon.UI.LayoutStart;
using static ModsCommon.UI.ComponentStyle;

namespace IMT.UI.Editors
{
    public class CategoryItem : CustomUIPanel, IReusable
    {
        bool IReusable.InCache { get; set; }
        Transform IReusable.CachedTransform { get => m_CachedTransform; set => m_CachedTransform = value; }

        public PropertyGroupPanel CategoryPanel { get; private set; }

        public CategoryItem()
        {
            autoLayout = AutoLayout.Vertical;
            autoChildrenVertically = AutoLayoutChildren.Fit;
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
                CategoryPanel.width = width - Padding.horizontal;
        }
    }
    public interface IPropertyCategoryPanel
    {
        void Init(IPropertyCategoryInfo category, IPropertyContainer editor);
    }

    public abstract class BasePropertyCategoryPanel<TypeHeader> : PropertyGroupPanel, IPropertyCategoryPanel
        where TypeHeader : BaseCategoryHeaderPanel
    {
        protected IPropertyContainer Editor { get; private set; }
        protected TypeHeader Header { get; private set; }
        private static HeaderStyle HeaderStyle { get; } = new HeaderStyle()
        {
            MainBgColors = new ColorSet(default, DarkPrimaryColor10, Color.black, default, default),
            MainIconColors = new ColorSet(Color.white, Color.white, DarkPrimaryColor90, Color.white, DarkPrimaryColor10),
            AdditionalBgColors = new ColorSet(default, DarkPrimaryColor45, DarkPrimaryColor45, default, default),
            AdditionalIconColors = new ColorSet(Color.white, Color.white, DarkPrimaryColor90, Color.white, DarkPrimaryColor55),
        };

        public bool? IsExpand
        {
            get
            {
                if (Editor.CategoryExpandList.TryGetValue(Category.Name, out var isExpand))
                    return isExpand;
                else
                    return null;
            }
            set
            {
                if (value != null)
                {
                    PauseLayout(() =>
                    {
                        Editor.CategoryExpandList[Category.Name] = value.Value;
                        Header.IsExpand = value.Value;
                        PaddingBottom = value.Value ? 0 : 5;

                        foreach (var item in components)
                        {
                            if (item is BaseEditorPanel property)
                                property.IsCollapsed = !value.Value;
                        }

                        SetBorder();
                    });
                }
            }
        }

        public IPropertyCategoryInfo Category { get; private set; }

        public BasePropertyCategoryPanel() : base()
        {
            ForegroundSprite = string.Empty;
            PaddingBottom = 3;
        }

        public virtual void Init(IPropertyCategoryInfo category, IPropertyContainer editor)
        {
            Category = category;
            Editor = editor;

            Header = ComponentPool.Get<TypeHeader>(this, nameof(Header));
            Header.ContentStyle = HeaderStyle;
            Header.Category = category.Text;
            Header.Init(editor);
            Header.eventClick += HeaderClick;

            IsExpand = (IsExpand ?? category.IsExpand);

            BackgroundSprite = string.Empty;

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
            if (child is EditorPropertyPanel property)
                property.IsCollapsed = IsExpand != true;

            base.OnComponentAdded(child);
        }

        protected override void SetBorder()
        {
            var properties = components.OfType<EditorPropertyPanel>().Where(p => p.isVisible).ToArray();
            for (int i = 0; i < properties.Length; i += 1)
            {
                properties[i].Borders = i == 0 ? PropertyBorder.None : PropertyBorder.Top;
            }
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
        protected override float DefaultHeight => 26f;
        protected virtual Color32 DefaultColor => new Color32(155, 175, 86, 255);
        protected virtual string DefaultForegroundSprite => CommonTextures.PanelBig;
        protected virtual UITextureAtlas DefaultAtlas => CommonTextures.Atlas;

        protected CustomUIButton ExpandButton { get; set; }
        protected CustomUILabel NameLabel { get; set; }

        protected IPropertyEditor Editor { get; private set; }
        public string Category
        {
            get => NameLabel.text;
            set => NameLabel.text = value;
        }
        public bool IsExpand { set => ExpandButton.IconSprites = value ? CommonTextures.ArrowDown : CommonTextures.ArrowRight; }

        public BaseCategoryHeaderPanel()
        {
            Atlas = DefaultAtlas;
            ForegroundSprite = DefaultForegroundSprite;
            FgColors = DefaultColor;
            Padding = new RectOffset(8, 8, 0, 0);
            SpritePadding = new RectOffset(5, 5, 0, 0);
        }

        protected override void Fill()
        {
            base.Fill();

            ExpandButton = AddUIComponent<CustomUIButton>();
            ExpandButton.Atlas = CommonTextures.Atlas;
            ExpandButton.IconColors = Color.black;
            ExpandButton.ScaleFactor = 0.6f;
            ExpandButton.size = new Vector2(20, 20);
            ExpandButton.zOrder = 0;

            NameLabel = AddUIComponent<CustomUILabel>();
            NameLabel.textScale = 0.8f;
            NameLabel.AutoSize = AutoSize.All;
            NameLabel.Padding = new RectOffset(0, 0, 2, 0);
            NameLabel.zOrder = 1;
        }
        protected override void FillContent()
        {
            Content.AutoLayoutStart = LayoutStart.TopRight;
        }

        public void Init(IPropertyEditor editor)
        {
            Editor = editor;
            base.Init();
        }
        public override void Refresh()
        {
            Content.Refresh();
            SetSize();
        }
        protected override void SetSize()
        {
            Content.size = new Vector2(width - Content.relativePosition.x - PaddingRight, height);
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

        protected override void FillContent()
        {
            base.FillContent();

            Copy = new HeaderButtonInfo<HeaderButton>("Copy", HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.CopyButtonIcon, IMT.Localize.HeaderPanel_CopyEffects, CopyClick);
            Content.AddButton(Copy);

            Paste = new HeaderButtonInfo<HeaderButton>("Paste", HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.PasteButtonIcon, IMT.Localize.HeaderPanel_PasteEffects, PasteClick);
            Content.AddButton(Paste);

            ApplyAllRules = new HeaderButtonInfo<HeaderButton>("Apply to all rules", HeaderButtonState.Main, IMTTextures.Atlas, IMTTextures.ApplyButtonIcon, IMT.Localize.HeaderPanel_ApplyAllRules, ApplyAllRulesClick);
            Content.AddButton(ApplyAllRules);

            ApplySameStyle = new HeaderButtonInfo<HeaderButton>("Appy the same style", HeaderButtonState.Additional, IMTTextures.Atlas, IMTTextures.CopyToSameButtonIcon, string.Empty, ApplySameStyleClick);
            Content.AddButton(ApplySameStyle);

            ApplySameType = new HeaderButtonInfo<HeaderButton>("Apply to the same type", HeaderButtonState.Additional, IMTTextures.Atlas, IMTTextures.CopyToAllButtonIcon, string.Empty, ApplySameTypeClick);
            Content.AddButton(ApplySameType);

            ApplyAll = new HeaderButtonInfo<HeaderButton>("Apply to all", HeaderButtonState.Additional, IMTTextures.Atlas, IMTTextures.ApplyAllButtonIcon, IMT.Localize.HeaderPanel_ApplyAll, ApplyAllClick);
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
    }
}
