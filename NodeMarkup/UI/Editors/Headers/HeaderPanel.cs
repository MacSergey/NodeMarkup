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
    public abstract class HeaderPanel : EditorItem
    {
        public event Action OnDelete;

        protected HeaderContent Content { get; set; }
        protected UIButton DeleteButton { get; set; }

        public HeaderPanel()
        {
            AddDeleteButton();
            AddContent();
        }

        public virtual void Init(float height = defaultHeight, bool isDeletable = true)
        {
            base.Init(height);
            DeleteButton.enabled = isDeletable;
        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            Content.size = new Vector2(DeleteButton.enabled ? width - DeleteButton.width - 10 : width, height);
            Content.autoLayout = true;
            Content.autoLayout = false;
            DeleteButton.relativePosition = new Vector2(width - DeleteButton.width - 5, (height - DeleteButton.height) / 2);

            foreach (var item in Content.components)
                item.relativePosition = new Vector2(item.relativePosition.x, (Content.height - item.height) / 2);
        }

        private void AddContent()
        {
            Content = AddUIComponent<HeaderContent>();
            Content.relativePosition = new Vector2(0, 0);
        }

        private void AddDeleteButton()
        {
            DeleteButton = AddUIComponent<UIButton>();
            DeleteButton.atlas = TextureUtil.InGameAtlas;
            DeleteButton.normalBgSprite = "buttonclose";
            DeleteButton.hoveredBgSprite = "buttonclosehover";
            DeleteButton.pressedBgSprite = "buttonclosepressed";
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
    }

    public class StyleHeaderPanel : HeaderPanel
    {
        public event Action OnSaveTemplate;
        public event Action OnCopy;
        public event Action OnPaste;

        SimpleHeaderButton SaveTemplate { get;}
        ApplyTemplateHeaderButton ApplyTemplate { get; }
        SimpleHeaderButton Copy { get; }
        SimpleHeaderButton Paste { get; }

        public StyleHeaderPanel()
        {
            SaveTemplate = Content.AddButton(HeaderButton.AddTemplate, NodeMarkup.Localize.HeaderPanel_SaveAsTemplate, onClick: SaveTemplateClick);
            ApplyTemplate = Content.AddButton<ApplyTemplateHeaderButton>(HeaderButton.ApplyTemplate, NodeMarkup.Localize.HeaderPanel_ApplyTemplate);
            Copy = Content.AddButton(HeaderButton.Copy, NodeMarkup.Localize.HeaderPanel_StyleCopy, onClick: CopyClick);
            Paste = Content.AddButton(HeaderButton.Paste, NodeMarkup.Localize.HeaderPanel_StylePaste, onClick: PasteClick);
        }

        public void Init(Style.StyleType styleGroup, Action<StyleTemplate> onSelectTemplate, bool isDeletable = true)
        {
            base.Init(35, isDeletable);
            ApplyTemplate.Init(styleGroup, onSelectTemplate);
        }
        private void SaveTemplateClick(UIComponent component, UIMouseEventParameter eventParam) => OnSaveTemplate?.Invoke();
        private void CopyClick(UIComponent component, UIMouseEventParameter eventParam) => OnCopy?.Invoke();
        private void PasteClick(UIComponent component, UIMouseEventParameter eventParam) => OnPaste?.Invoke();
    }

    public class TemplateHeaderPanel : HeaderPanel
    {
        public event Action OnSetAsDefault;
        public event Action OnDuplicate;

        HeaderButton SetAsDefaultButton { get; }
        HeaderButton DuplicateButton { get; }

        public TemplateHeaderPanel()
        {
            SetAsDefaultButton = Content.AddButton(string.Empty, null, onClick: SetAsDefaultClick);
            DuplicateButton = Content.AddButton(HeaderButton.Duplicate, NodeMarkup.Localize.HeaderPanel_Duplicate, onClick: DuplicateClick);
        }
        public void Init(bool isDefault)
        {
            base.Init(isDeletable: false);

            SetAsDefaultButton.SetSprite(isDefault ? HeaderButton.UnsetDefault : HeaderButton.SetDefault);
            SetAsDefaultButton.tooltip = isDefault ? NodeMarkup.Localize.HeaderPanel_UnsetAsDefault : NodeMarkup.Localize.HeaderPanel_SetAsDefault;
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            SetAsDefaultButton.relativePosition = new Vector2(5, (height - SetAsDefaultButton.height) / 2);
        }
        private void SetAsDefaultClick(UIComponent component, UIMouseEventParameter eventParam) => OnSetAsDefault?.Invoke();
        private void DuplicateClick(UIComponent component, UIMouseEventParameter eventParam) => OnDuplicate?.Invoke();
    }

}
