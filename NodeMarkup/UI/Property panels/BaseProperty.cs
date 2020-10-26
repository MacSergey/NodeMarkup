using ColossalFramework.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
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
                atlas = TextureUtil.CreateTextureAtlas("TextFieldPanel.png", nameof(EditorItemAtlas), 32, 32, spriteNames, new RectOffset(4, 4, 4, 4), 2);

            return atlas;
        }
        public virtual void Init() => Init(defaultHeight);
        public virtual void DeInit() { }
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
            button.SetDefaultStyle();
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
            Control.autoLayoutDirection = LayoutDirection.Horizontal;
            Control.autoLayoutStart = LayoutStart.TopRight;
            Control.autoLayoutPadding = new RectOffset(5, 0, 0, 0);

            Control.eventSizeChanged += ControlSizeChanged;
        }
        public override void DeInit() => Text = string.Empty;
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            Label.relativePosition = new Vector2(0, (height - Label.height) / 2);
            Control.size = size;           
        }

        private void ControlSizeChanged(UIComponent component, Vector2 value) => RefreshContent();
        protected void RefreshContent()
        {
            Control.autoLayout = true;
            Control.autoLayout = false;

            foreach (var item in Control.components)
                item.relativePosition = new Vector2(item.relativePosition.x, (Control.size.y - item.size.y) / 2);
        }
    }
}
