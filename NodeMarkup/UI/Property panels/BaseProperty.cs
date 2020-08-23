using ColossalFramework.UI;
using NodeMarkup.Utils;
using UnityEngine;

namespace NodeMarkup.UI
{
    public abstract class EditorItem : UIPanel
    {
        protected const float defaultHeight = 30f;

        public static UITextureAtlas EditorItemAtlas { get; } = GetAtlas();
        private static UITextureAtlas GetAtlas()
        {
            var spriteNames = new string[]
            {
                "TextFieldPanel",
                "TextFieldPanelHovered",
                "TextFieldPanelFocus",
                "EmptySprite"
            };

            var atlas = TextureUtil.GetAtlas(nameof(EditorItemAtlas));
            if (atlas == UIView.GetAView().defaultAtlas)
            {
                atlas = TextureUtil.CreateTextureAtlas("TextFieldPanel.png", nameof(EditorItemAtlas), 32, 32, spriteNames, new RectOffset(4, 4, 4, 4), 2);
            }

            return atlas;
        }
        public virtual void Init() => Init(defaultHeight);
        public void Init(float height)
        {
            if (parent is UIScrollablePanel scrollablePanel)
                width = scrollablePanel.width - scrollablePanel.autoLayoutPadding.horizontal;
            else if (parent is UIPanel panel)
                width = panel.width - panel.autoLayoutPadding.horizontal;
            else
                width = parent.width;

            this.height = height;
        }

        protected UIButton AddButton(UIComponent parent)
        {
            var button = parent.AddUIComponent<UIButton>();
            button.atlas = TextureUtil.InGameAtlas;
            button.normalBgSprite = "ButtonWhite";
            button.disabledBgSprite = "ButtonWhite";
            button.hoveredBgSprite = "ButtonWhite";
            button.pressedBgSprite = "ButtonWhite";
            button.color = Color.white;
            button.hoveredColor = new Color32(224, 224, 224, 255);
            button.pressedColor = new Color32(192, 192, 192, 255);
            button.textColor = button.hoveredTextColor = button.focusedTextColor = Color.black;
            button.pressedTextColor = Color.white;

            return button;
        }
    }
    public abstract class EditorPropertyPanel : EditorItem
    {
        private UILabel Label { get; set; }
        protected UIPanel Control { get; set; }

        public string Text
        {
            get => Label.text;
            set => Label.text = value;
        }

        public EditorPropertyPanel()
        {
            Label = AddUIComponent<UILabel>();
            Label.textScale = 0.8f;

            Control = AddUIComponent<UIPanel>();
            Control.autoLayout = true;
            Control.autoLayoutDirection = LayoutDirection.Horizontal;
            Control.autoLayoutStart = LayoutStart.TopRight;
            Control.autoLayoutPadding = new RectOffset(5, 0, 0, 0);
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            Label.relativePosition = new Vector2(0, (height - Label.height) / 2);
            Control.size = size;
            Control.autoLayout = true;
            Control.autoLayout = false;

            foreach (var item in Control.components)
            {
                item.relativePosition = new Vector3(item.relativePosition.x, (Control.size.y - item.size.y) / 2);
            }
        }
    }
}
