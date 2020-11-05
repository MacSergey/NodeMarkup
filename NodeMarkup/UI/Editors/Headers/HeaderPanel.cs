using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public abstract class HeaderPanel : EditorItem, IReusable
    {
        public event Action OnDelete;
        protected override float DefaultHeight => 35;

        protected HeaderContent Content { get; set; }
        protected UIButton DeleteButton { get; set; }

        public HeaderPanel()
        {
            AddDeleteButton();
            AddContent();
        }

        public virtual void Init(float? height = null, bool isDeletable = true)
        {
            base.Init(height);
            DeleteButton.enabled = isDeletable;
        }
        public override void DeInit()
        {
            base.DeInit();
            OnDelete = null;
        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            Content.size = new Vector2(DeleteButton.enabled ? width - DeleteButton.width - 10 : width, height);
            DeleteButton.relativePosition = new Vector2(width - DeleteButton.width - 5, (height - DeleteButton.height) / 2);
        }

        private void AddContent()
        {
            Content = AddUIComponent<HeaderContent>();
            Content.relativePosition = new Vector2(0, 0);
        }

        private void AddDeleteButton()
        {
            DeleteButton = AddUIComponent<UIButton>();
            DeleteButton.atlas = TextureUtil.Atlas;
            DeleteButton.normalBgSprite = TextureUtil.DeleteNormal;
            DeleteButton.hoveredBgSprite = TextureUtil.DeleteHover;
            DeleteButton.pressedBgSprite = TextureUtil.DeletePressed;
            DeleteButton.size = new Vector2(20, 20);
            DeleteButton.eventClick += DeleteClick;
        }
        private void DeleteClick(UIComponent component, UIMouseEventParameter eventParam) => OnDelete?.Invoke();

    }
    public class HeaderContent : UIPanel
    {
        public HeaderContent()
        {
            autoLayoutDirection = LayoutDirection.Horizontal;
            autoLayoutPadding = new RectOffset(0, 5, 0, 0);
        }
        public SimpleHeaderButton AddButton(string sprite, string text, bool showText = false, MouseEventHandler onClick = null)
            => AddButton<SimpleHeaderButton>(this, sprite, text, showText, onClick);
        public SimpleHeaderButton AddButton(UIComponent parent, string sprite, string text, bool showText = false, MouseEventHandler onClick = null)
            => AddButton<SimpleHeaderButton>(parent, sprite, text, showText, onClick);

        public ButtonType AddButton<ButtonType>(string sprite, string text, bool showText = false, MouseEventHandler onClick = null) where ButtonType : HeaderButton
            => AddButton<ButtonType>(this, sprite, text, showText, onClick);
        public ButtonType AddButton<ButtonType>(UIComponent parent, string sprite, string text, bool showText = false, MouseEventHandler onClick = null)
            where ButtonType : HeaderButton
        {
            var button = parent.AddUIComponent<ButtonType>();
            if(showText)
                button.text = text ?? string.Empty;
            else
                button.tooltip = text;
            button.SetSprite(sprite);

            if (onClick != null)
                button.eventClick += onClick;
            return button;
        }

        protected override void OnComponentAdded(UIComponent child)
        {
            base.OnComponentAdded(child);
            child.eventVisibilityChanged += ChildVisibilityChanged;
            child.eventSizeChanged += ChildSizeChanged;
        }
        protected override void OnComponentRemoved(UIComponent child)
        {
            base.OnComponentRemoved(child);
            child.eventVisibilityChanged -= ChildVisibilityChanged;
            child.eventSizeChanged -= ChildSizeChanged;
        }

        private void ChildVisibilityChanged(UIComponent component, bool value) => PlaceChildren();
        private void ChildSizeChanged(UIComponent component, Vector2 value) => PlaceChildren();

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            PlaceChildren();
        }

        public void PlaceChildren()
        {
            autoLayout = true;
            autoLayout = false;

            foreach (var item in components)
                item.relativePosition = new Vector2(item.relativePosition.x, (height - item.height) / 2);
        }
    }

    public class StyleHeaderPanel : HeaderPanel
    {
        public event Action OnSaveTemplate;
        public event Action OnCopy;
        public event Action OnPaste;

        ApplyTemplateHeaderButton ApplyTemplate { get; }

        public StyleHeaderPanel()
        {
            Content.AddButton(TextureUtil.AddTemplate, NodeMarkup.Localize.HeaderPanel_SaveAsTemplate, onClick: SaveTemplateClick);
            ApplyTemplate = Content.AddButton<ApplyTemplateHeaderButton>(TextureUtil.ApplyTemplate, NodeMarkup.Localize.HeaderPanel_ApplyTemplate);
            Content.AddButton(TextureUtil.Copy, NodeMarkup.Localize.HeaderPanel_StyleCopy, onClick: CopyClick);
            Content.AddButton(TextureUtil.Paste, NodeMarkup.Localize.HeaderPanel_StylePaste, onClick: PasteClick);
        }

        public void Init(Style.StyleType styleGroup, Action<StyleTemplate> onSelectTemplate, bool isDeletable = true)
        {
            base.Init(isDeletable: isDeletable);
            ApplyTemplate.Init(styleGroup, onSelectTemplate);
        }
        public override void DeInit()
        {
            base.DeInit();

            OnSaveTemplate = null;
            OnCopy = null;
            OnPaste = null;

            ApplyTemplate.DeInit();
        }
        private void SaveTemplateClick(UIComponent component, UIMouseEventParameter eventParam) => OnSaveTemplate?.Invoke();
        private void CopyClick(UIComponent component, UIMouseEventParameter eventParam) => OnCopy?.Invoke();
        private void PasteClick(UIComponent component, UIMouseEventParameter eventParam) => OnPaste?.Invoke();
    }
    public class CrosswalkHeaderPanel : StyleHeaderPanel
    {
        public event Action OnCut;

        public CrosswalkHeaderPanel()
        {
            Content.AddButton(TextureUtil.Cut, NodeMarkup.Localize.HeaderPanel_CutLinesByCrosswalk, onClick: CutClick);
        }
        public override void DeInit()
        {
            base.DeInit();

            OnCut = null;
        }

        private void CutClick(UIComponent component, UIMouseEventParameter eventParam) => OnCut?.Invoke();
    }

    public abstract class TemplateHeaderPanel<TemplateType> : HeaderPanel
        where TemplateType : Template
    {
        public event Action OnSaveAsset;
        public event Action OnEdit;
        public event Action OnApply;
        public event Action OnNotApply;
        HeaderButton SaveAsAsset { get; set; }
        HeaderButton Edit { get; set; }
        HeaderButton Apply { get; set; }
        HeaderButton NotApply { get; set; }

        private bool IsAsset { get; set; }
        private bool IsWorkshop { get; set; }

        public virtual bool EditMode
        {
            set
            {
                SaveAsAsset.isVisible = !IsAsset && !value;
                Edit.isVisible = (!IsAsset || !IsWorkshop) && !value;
                Apply.isVisible = NotApply.isVisible = value;
            }
        }

        public TemplateHeaderPanel()
        {
            AddButtons();
        }
        protected virtual void AddButtons()
        {
            Edit = Content.AddButton(TextureUtil.Edit, NodeMarkup.Localize.HeaderPanel_Edit, onClick: EditClick);
            SaveAsAsset = Content.AddButton(TextureUtil.Package, NodeMarkup.Localize.HeaderPanel_SaveAsAsset, onClick: SaveAssetClick);
            Apply = Content.AddButton(TextureUtil.Apply, NodeMarkup.Localize.HeaderPanel_Apply, onClick: ApplyClick);
            NotApply = Content.AddButton(TextureUtil.NotApply, NodeMarkup.Localize.HeaderPanel_NotApply, onClick: NotApplyClick);
        }

        public virtual void Init(TemplateType template)
        {
            base.Init(isDeletable: false);

            IsAsset = template.IsAsset;
            IsWorkshop = !IsAsset || template.Asset.IsWorkshop;

            EditMode = false;
        }
        public override void DeInit()
        {
            base.DeInit();
            OnSaveAsset = null;
            OnEdit = null;
            OnApply = null;
            OnNotApply = null;
        }
        private void SaveAssetClick(UIComponent component, UIMouseEventParameter eventParam) => OnSaveAsset?.Invoke();
        private void EditClick(UIComponent component, UIMouseEventParameter eventParam) => OnEdit?.Invoke();
        private void ApplyClick(UIComponent component, UIMouseEventParameter eventParam) => OnApply?.Invoke();
        private void NotApplyClick(UIComponent component, UIMouseEventParameter eventParam) => OnNotApply?.Invoke();
    }

    public class StyleTemplateHeaderPanel : TemplateHeaderPanel<StyleTemplate>
    {
        public event Action OnSetAsDefault;
        public event Action OnDuplicate;

        HeaderButton SetAsDefaultButton { get; set; }
        HeaderButton Duplicate { get; set; }

        public override bool EditMode 
        {
            set
            {
                base.EditMode = value;
                SetAsDefaultButton.isVisible = Duplicate.isVisible = !value;
            }
        }

        protected override void AddButtons()
        {
            SetAsDefaultButton = Content.AddButton(string.Empty, null, onClick: SetAsDefaultClick);
            Duplicate = Content.AddButton(TextureUtil.Duplicate, NodeMarkup.Localize.HeaderPanel_Duplicate, onClick: DuplicateClick);

            base.AddButtons();
        }
        public override void Init(StyleTemplate template)
        {
            base.Init(template);

            SetAsDefaultButton.SetSprite(template.IsDefault ? TextureUtil.UnsetDefault : TextureUtil.SetDefault);
            SetAsDefaultButton.tooltip = template.IsDefault ? NodeMarkup.Localize.HeaderPanel_UnsetAsDefault : NodeMarkup.Localize.HeaderPanel_SetAsDefault;
        }
        public override void DeInit()
        {
            base.DeInit();

            OnSetAsDefault = null;
            OnDuplicate = null;
        }

        private void SetAsDefaultClick(UIComponent component, UIMouseEventParameter eventParam) => OnSetAsDefault?.Invoke();
        private void DuplicateClick(UIComponent component, UIMouseEventParameter eventParam) => OnDuplicate?.Invoke();
    }
    public class IntersectionTemplateHeaderPanel : TemplateHeaderPanel<IntersectionTemplate> { }
}
