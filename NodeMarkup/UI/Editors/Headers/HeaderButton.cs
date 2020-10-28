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
    public abstract class HeaderButton : UIButton
    {
        public static string Hovered => nameof(Hovered);
        public static string AddTemplate => nameof(AddTemplate);
        public static string ApplyTemplate => nameof(ApplyTemplate);
        public static string Copy => nameof(Copy);
        public static string Paste => nameof(Paste);
        public static string Duplicate => nameof(Duplicate);
        public static string SetDefault => nameof(SetDefault);
        public static string UnsetDefault => nameof(UnsetDefault);
        public static string Package => nameof(Package);
        public static string Clear => nameof(Clear);
        public static string Edit => nameof(Edit);
        public static string Offset => nameof(Offset);
        public static string EdgeLines => nameof(EdgeLines);
        public static string Additionally => nameof(Additionally);

        public static UITextureAtlas ButtonAtlas { get; } = GetButtonsIcons();
        private static UITextureAtlas GetButtonsIcons()
        {
            var spriteNames = new string[]
            {
                Hovered,
                "_",
                AddTemplate,
                ApplyTemplate,
                Copy,
                Paste,
                Duplicate,
                SetDefault,
                UnsetDefault,
                Package,
                Clear,
                Edit,
                Offset,
                EdgeLines,
                Additionally,
            };

            var atlas = TextureUtil.GetAtlas(nameof(ButtonAtlas));
            if (atlas == UIView.GetAView().defaultAtlas)
                atlas = TextureUtil.CreateTextureAtlas("Buttons.png", nameof(ButtonAtlas), 25, 25, spriteNames, new RectOffset(2, 2, 2, 2));

            return atlas;
        }

        public UIPanel Panel { get; }
        protected virtual Color32 HoveredColor => Color.black;
        protected virtual Color32 PressedColor => new Color32(160, 160, 160, 255);

        public HeaderButton()
        {
            hoveredBgSprite = Hovered;
            pressedBgSprite = Hovered;
            size = new Vector2(25, 25);
            atlas = ButtonAtlas;
            hoveredColor = HoveredColor;
            pressedColor = PressedColor;
            clipChildren = true;
            textPadding = new RectOffset(30, 5, 5, 0);
            textScale = 0.8f;
            textHorizontalAlignment = UIHorizontalAlignment.Left;
            minimumSize = size;

            Panel = AddUIComponent<UIPanel>();
            Panel.size = size;
            Panel.atlas = atlas;
            Panel.relativePosition = Vector2.zero;
        }

        public void SetSprite(string sprite) => Panel.backgroundSprite = sprite;
    }
    public class SimpleHeaderButton : HeaderButton { }

    public abstract class HeaderPopupButton<PopupType> : HeaderButton
        where PopupType : PopupPanel
    {
        public PopupType Popup { get; private set; }

        protected override void OnClick(UIMouseEventParameter p)
        {
            if (Popup == null)
                OpenPopup();
            else
                ClosePopup();
        }
        protected override void OnVisibilityChanged()
        {
            base.OnVisibilityChanged();
            if (!isVisible)
                ClosePopup();
        }

        protected void OpenPopup()
        {
            var root = GetRootContainer();
            Popup = root.AddUIComponent<PopupType>();
            Popup.eventLostFocus += OnPopupLostFocus;
            Popup.Focus();

            OnOpenPopup();
            Popup.Init();

            SetPopupPosition();
            Popup.parent.eventPositionChanged += SetPopupPosition;
        }
        protected virtual void OnOpenPopup() { }
        public virtual void ClosePopup()
        {
            if (Popup != null)
            {
                Popup.parent.RemoveUIComponent(Popup);
                Destroy(Popup.gameObject);
                Popup = null;
            }
        }
        private void OnPopupLostFocus(UIComponent component, UIFocusEventParameter eventParam)
        {
            var uiView = Popup.GetUIView();
            var mouse = uiView.ScreenPointToGUI(Input.mousePosition / uiView.inputScale);
            var popupRect = new Rect(Popup.absolutePosition, Popup.size);
            var buttonRect = new Rect(absolutePosition, size);
            if (!popupRect.Contains(mouse) && !buttonRect.Contains(mouse))
                ClosePopup();
            else
                Popup.Focus();
        }
        private void SetPopupPosition(UIComponent component = null, Vector2 value = default)
        {
            if (Popup != null)
            {
                UIView uiView = Popup.GetUIView();
                var screen = uiView.GetScreenResolution();
                var position = absolutePosition + new Vector3(0, height);
                position.x = MathPos(position.x, Popup.width, screen.x);
                position.y = MathPos(position.y, Popup.height, screen.y);

                Popup.relativePosition = position - Popup.parent.absolutePosition;
            }

            static float MathPos(float pos, float size, float screen) => pos + size > screen ? (screen - size < 0 ? 0 : screen - size) : Mathf.Max(pos, 0);
        }
    }
}
