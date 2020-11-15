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
    public abstract class PopupPanel : UIPanel
    {
        protected virtual Color32 Background => Color.black;
        public UIScrollablePanel Content { get; private set; }
        private float Padding => 2f;

        private float _width = 250f;
        public float Width
        {
            get => _width;
            set
            {
                _width = value;
                Init();
            }
        }
        public Vector2 MaxSize
        {
            get => Content.maximumSize;
            set => Content.maximumSize = value;
        }

        public PopupPanel()
        {
            isVisible = true;
            canFocus = true;
            isInteractive = true;
            color = Background;
            atlas = TextureUtil.Atlas;
            backgroundSprite = TextureUtil.FieldHovered;

            AddPanel();
        }

        private void AddPanel()
        {
            Content = AddUIComponent<UIScrollablePanel>();
            Content.autoLayout = true;
            Content.autoLayoutDirection = LayoutDirection.Vertical;
            Content.autoLayoutPadding = new RectOffset(0, 0, 0, 0);
            Content.clipChildren = true;
            Content.builtinKeyNavigation = true;
            Content.scrollWheelDirection = UIOrientation.Vertical;
            Content.maximumSize = new Vector2(500, 500);
            Content.relativePosition = new Vector2(Padding, Padding);
            this.AddScrollbar(Content);
        }
        public void Init()
        {
            FitContentChildren();
            ContentSizeChanged();
        }
        private void FitContentChildren()
        {
            Content.FitChildrenVertically();
            Content.width = Content.verticalScrollbar.isVisible ? Width - Content.verticalScrollbar.width : Width;
        }
        private void ContentSizeChanged(UIComponent component = null, Vector2 value = default)
        {
            if (Content != null)
            {
                size = new Vector2(Width + Padding * 2, Content.height + Padding * 2);

                foreach (var item in Content.components)
                    item.width = Content.width;
            }
        }
    }

    
}
