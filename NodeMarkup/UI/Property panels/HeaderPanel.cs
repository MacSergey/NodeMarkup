using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Linq;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public abstract class HeaderPanel : EditorItem
    {
        public static UITextureAtlas ButtonAtlas { get; } = GetStylesIcons();
        private static UITextureAtlas GetStylesIcons()
        {
            var spriteNames = new string[]
            {
                "Hovered",
                "_",
                "AddTemplate",
                "ApplyTemplate",
                "Copy",
                "Paste",
                "SetDefault",
                "UnsetDefault",
            };

            var atlas = TextureUtil.GetAtlas(nameof(ButtonAtlas));
            if (atlas == UIView.GetAView().defaultAtlas)
            {
                atlas = TextureUtil.CreateTextureAtlas("Buttons.png", nameof(ButtonAtlas), 25, 25, spriteNames, new RectOffset(2,2,2,2));
            }

            return atlas;
        }

        public event Action OnDelete;

        protected UIPanel Content { get; set; }
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
            Content = AddUIComponent<UIPanel>();
            Content.relativePosition = new Vector2(0, 0);
            Content.autoLayoutDirection = LayoutDirection.Horizontal;
            Content.autoLayoutPadding = new RectOffset(0, 5, 0, 0);
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

        protected UIButton AddButton(string sprite, string text = null, MouseEventHandler onClick = null)
        {
            var button = Content.AddUIComponent<UIButton>();
            button.hoveredBgSprite = "Hovered";
            button.pressedBgSprite = "Hovered";
            button.size = new Vector2(25, 25);
            button.atlas = ButtonAtlas;
            button.hoveredColor = Color.black;
            button.pressedColor = new Color32(32, 32, 32, 255);
            button.tooltip = text;
            if (onClick != null)
                button.eventClick += onClick;

            var panel = button.AddUIComponent<UIPanel>();
            panel.size = button.size;
            panel.atlas = button.atlas;
            panel.relativePosition = Vector2.zero;

            SetSprite(button, sprite);

            return button;
        }
        protected void SetSprite(UIButton button, string sprite) => (button.components.First() as UIPanel).backgroundSprite = sprite;
    }
    public class StyleHeaderPanel : HeaderPanel
    {
        public event Action OnSaveTemplate;
        public event Action<StyleTemplate> OnSelectTemplate;
        public event Action OnCopy;
        public event Action OnPaste;

        Style.StyleType StyleGroup { get; set; }
        TemplateSelectPanel Popup { get; set; }

        UIButton SaveTemplate { get; set; }
        UIButton ApplyTemplate { get; set; }
        UIButton Copy { get; set; }
        UIButton Paste { get; set; }

        public StyleHeaderPanel()
        {
            SaveTemplate = AddButton("AddTemplate", NodeMarkup.Localize.HeaderPanel_SaveAsTemplate, SaveTemplateClick);
            ApplyTemplate = AddButton("ApplyTemplate", NodeMarkup.Localize.HeaderPanel_ApplyTemplate, ApplyTemplateClick);
            Copy = AddButton("Copy", NodeMarkup.Localize.LineEditor_StyleCopy, CopyClick);
            Paste = AddButton("Paste", NodeMarkup.Localize.LineEditor_StylePaste, PasteClick);
        }

        public void Init(Style.StyleType styleGroup, bool isDeletable = true)
        {
            base.Init(35, isDeletable);

            StyleGroup = styleGroup & Style.StyleType.GroupMask;
        }
        protected override void OnVisibilityChanged()
        {
            base.OnVisibilityChanged();
            if(!isVisible)
                ClosePopup();
        }
        private void SaveTemplateClick(UIComponent component, UIMouseEventParameter eventParam) => OnSaveTemplate?.Invoke();
        private void ApplyTemplateClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (Popup == null)
                OpenPopup();
            else
                ClosePopup();
        }
        private void OnPopupLostFocus(UIComponent component, UIFocusEventParameter eventParam)
        {
            var uiView = Popup.GetUIView();
            var mouse = uiView.ScreenPointToGUI(Input.mousePosition / uiView.inputScale);
            var popupRect = new Rect(Popup.absolutePosition, Popup.size);
            var buttonRect = new Rect(ApplyTemplate.absolutePosition, ApplyTemplate.size);
            if (!popupRect.Contains(mouse) && !buttonRect.Contains(mouse))
                ClosePopup();
            else
                Popup.Focus();
        }
        private void OnTemplateSelect(StyleTemplate template)
        {
            OnSelectTemplate?.Invoke(template);
            ClosePopup();
        }
        private void OpenPopup()
        {
            var root = GetRootContainer();
            Popup = root.AddUIComponent<TemplateSelectPanel>();
            Popup.Init(StyleGroup);
            Popup.OnSelect += OnTemplateSelect;
            Popup.eventLostFocus += OnPopupLostFocus;
            SetPopupPosition();
            Popup.Focus();

            Popup.parent.eventPositionChanged += SetPopupPosition;
        }
        private void ClosePopup()
        {
            if (Popup != null)
            {
                Popup.parent.eventPositionChanged -= SetPopupPosition;

                Popup.OnSelect -= OnTemplateSelect;
                Popup.eventLostFocus -= OnPopupLostFocus;
                Popup.parent.RemoveUIComponent(Popup);
                Destroy(Popup.gameObject);
                Popup = null;
            }
        }
        private void SetPopupPosition(UIComponent component = null, Vector2 value = default)
        {
            if (Popup != null)
            {
                UIView uiView = Popup.GetUIView();
                var screen = uiView.GetScreenResolution();
                var position = ApplyTemplate.absolutePosition + new Vector3(0, ApplyTemplate.height);
                position.x = MathPos(position.x, Popup.width, screen.x);
                position.y = MathPos(position.y, Popup.height, screen.y);

                Popup.relativePosition = position - Popup.parent.absolutePosition;
            }

            float MathPos(float pos, float size, float screen) => pos + size > screen ? (screen - size < 0 ? 0 : screen - size) : Mathf.Max(pos, 0);
        }

        private void CopyClick(UIComponent component, UIMouseEventParameter eventParam) => OnCopy?.Invoke();
        private void PasteClick(UIComponent component, UIMouseEventParameter eventParam) => OnPaste?.Invoke();


        public class TemplateSelectPanel : UIPanel
        {
            public event Action<StyleTemplate> OnSelect;

            private static float MaxContentHeight { get; } = 200;
            protected UIScrollablePanel ScrollableContent { get; private set; }
            private float Padding => 2f;

            private float _width = 250f;
            public float Width
            {
                get => _width;
                set
                {
                    _width = value;
                    FitContentChildren();
                }
            }
            public Vector2 MaxSize
            {
                get => ScrollableContent.maximumSize;
                set => ScrollableContent.maximumSize = value;
            }

            public TemplateSelectPanel()
            {
                isVisible = true;
                canFocus = true;
                isInteractive = true;
                color = new Color32(58, 88, 104, 255);
                atlas = TextureUtil.InGameAtlas;
                backgroundSprite = "OptionsDropboxListbox";
            }
            public void Init(Style.StyleType styleGroup)
            {
                AddPanel();
                styleGroup &= Style.StyleType.GroupMask;
                Fill(styleGroup);
                ContentSizeChanged();

                ScrollableContent.eventSizeChanged += ContentSizeChanged;
            }

            private void AddPanel()
            {
                ScrollableContent = AddUIComponent<UIScrollablePanel>();
                ScrollableContent.autoLayout = true;
                ScrollableContent.autoLayoutDirection = LayoutDirection.Vertical;
                ScrollableContent.autoLayoutPadding = new RectOffset(0, 0, 0, 0);
                ScrollableContent.clipChildren = true;
                ScrollableContent.builtinKeyNavigation = true;
                ScrollableContent.scrollWheelDirection = UIOrientation.Vertical;
                ScrollableContent.maximumSize = new Vector2(250, 500);
                ScrollableContent.relativePosition = new Vector2(Padding, Padding);
                UIUtils.AddScrollbar(this, ScrollableContent);

                ScrollableContent.eventComponentAdded += (UIComponent container, UIComponent child) =>
                {
                    child.eventVisibilityChanged += (UIComponent component, bool value) => FitContentChildren();
                    child.eventSizeChanged += (UIComponent component, Vector2 value) => FitContentChildren();
                };

                FitContentChildren();
            }
            private void Fill(Style.StyleType styleGroup)
            {
                var templates = TemplateManager.GetTemplates(styleGroup).ToArray();
                if(!templates.Any())
                {
                    var emptyLabel = ScrollableContent.AddUIComponent<UILabel>();
                    emptyLabel.text = NodeMarkup.Localize.HeaderPanel_NoTemplates;
                    emptyLabel.textScale = 0.8f;
                    emptyLabel.autoSize = false;
                    emptyLabel.width = ScrollableContent.width;
                    emptyLabel.autoHeight = true;
                    emptyLabel.textAlignment = UIHorizontalAlignment.Center;
                    emptyLabel.padding = new RectOffset(0, 0, 5, 5);
                    return;
                }

                foreach (var template in templates)
                {
                    var item = ScrollableContent.AddUIComponent<TemplateItem>();
                    item.Init(true, false);
                    item.name = template.ToString();
                    item.Object = template;
                    item.eventClick += ItemClick;
                }
            }

            private void ItemClick(UIComponent component, UIMouseEventParameter eventParam)
            {
                if (component is TemplateItem item)
                    OnSelect?.Invoke(item.Object);
            }

            private void FitContentChildren()
            {
                ScrollableContent.FitChildrenVertically();
                ScrollableContent.width = ScrollableContent.verticalScrollbar.isVisible ? Width - ScrollableContent.verticalScrollbar.width : Width;
            }
            private void ContentSizeChanged(UIComponent component = null, Vector2 value = default)
            {
                if (ScrollableContent != null)
                {
                    size = ScrollableContent.size + new Vector2(Padding * 2, Padding * 2);
                    ScrollableContent.verticalScrollbar.relativePosition = ScrollableContent.relativePosition + new Vector3(ScrollableContent.width, 0);
                    ScrollableContent.verticalScrollbar.height = ScrollableContent.height;

                    foreach (var item in ScrollableContent.components)
                        item.width = ScrollableContent.width;
                }
            }
        }
    }

    public class TemplateHeaderPanel : HeaderPanel
    {
        public event Action OnSetAsDefault;

        UIButton SetAsDefaultButton { get; set; }

        public TemplateHeaderPanel()
        {
            SetAsDefaultButton = AddButton(string.Empty, onClick: SetAsDefaultClick);
        }
        public void Init(bool isDefault)
        {
            base.Init(isDeletable: false);

            SetSprite(SetAsDefaultButton, isDefault ? "UnsetDefault" : "SetDefault");
            SetAsDefaultButton.tooltip = isDefault ? NodeMarkup.Localize.HeaderPanel_UnsetAsDefault : NodeMarkup.Localize.HeaderPanel_SetAsDefault;
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            SetAsDefaultButton.relativePosition = new Vector2(5, (height - SetAsDefaultButton.height) / 2);
        }
        private void SetAsDefaultClick(UIComponent component, UIMouseEventParameter eventParam) => OnSetAsDefault?.Invoke();
    }

}
