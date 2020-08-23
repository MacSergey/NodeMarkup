using ColossalFramework.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utils;
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
        public void AddTab<PanelType>(string name) where PanelType : Editor
        {
            var tabButton = AddTab(name);
            tabButton.autoSize = true;
            tabButton.textPadding = new RectOffset(5, 5, 2, 2);
            tabButton.textScale = 0.85f;
            tabButton.textHorizontalAlignment = UIHorizontalAlignment.Center;
            tabButton.verticalAlignment = UIVerticalAlignment.Middle;

            tabButton.atlas = TextureUtil.InGameAtlas;

            tabButton.normalBgSprite = "SubBarButtonBase";
            tabButton.disabledBgSprite = "SubBarButtonBaseDisabled";
            tabButton.focusedBgSprite = "SubBarButtonBaseFocused";
            tabButton.hoveredBgSprite = "SubBarButtonBaseHovered";
            tabButton.pressedBgSprite = "SubBarButtonBasePressed";

            tabButton.Invalidate();

            FitChildrenVertically();
        }
        protected override void OnSizeChanged() => FitChildrenVertically();
    }
}
