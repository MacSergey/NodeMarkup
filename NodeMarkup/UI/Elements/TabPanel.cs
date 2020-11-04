using ColossalFramework.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI
{
    public class CustomUITabstrip : UITabstrip
    {
        public CustomUITabstrip()
        {
            atlas = TextureUtil.InGameAtlas;
            backgroundSprite = "";
        }
        public void AddTab(string name, float textScale = 0.85f)
        {
            var tabButton = base.AddTab(name);
            tabButton.autoSize = true;
            tabButton.textPadding = new RectOffset(5, 5, 2, 2);
            tabButton.textScale = textScale;
            tabButton.textHorizontalAlignment = UIHorizontalAlignment.Center;
            tabButton.verticalAlignment = UIVerticalAlignment.Middle;
            tabButton.eventIsEnabledChanged += TabButtonIsEnabledChanged;

            SetStyle(tabButton);

            tabButton.Invalidate();

            FitChildrenVertically();
        }

        private void TabButtonIsEnabledChanged(UIComponent component, bool value)
        {
            if (!component.isEnabled)
            {
                var button = component as UIButton;
                button.disabledColor = button.state == UIButton.ButtonState.Focused ? button.focusedColor : button.color;
            }
        }

        protected virtual void SetStyle(UIButton tabButton)
        {
            tabButton.atlas = TextureUtil.InGameAtlas;

            tabButton.normalBgSprite = "SubBarButtonBase";
            tabButton.focusedBgSprite = "SubBarButtonBaseFocused";
            tabButton.hoveredBgSprite = "SubBarButtonBaseHovered";
        }
        protected override void OnSizeChanged() => FitChildrenVertically();
    }

    public class PanelTabStrip : CustomUITabstrip
    {
        private static Color32 NormalColor { get; } = new Color32(107, 113, 115, 255);
        private static Color32 HoverColor { get; } = new Color32(143, 149, 150, 255);
        private static Color32 FocusColor { get; } = new Color32(177, 195, 94, 255);

        protected override void SetStyle(UIButton tabButton)
        {
            tabButton.atlas = TextureUtil.Atlas;

            tabButton.normalBgSprite = TextureUtil.Tab;
            tabButton.focusedBgSprite = TextureUtil.Tab;
            tabButton.hoveredBgSprite = TextureUtil.Tab;
            tabButton.disabledBgSprite = TextureUtil.Tab;

            tabButton.color = NormalColor;
            tabButton.hoveredColor = HoverColor;
            tabButton.focusedColor = FocusColor;
        }
    }
}
