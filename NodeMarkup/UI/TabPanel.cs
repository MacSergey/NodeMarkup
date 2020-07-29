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
            atlas = NodeMarkupTool.InGameAtlas;
            backgroundSprite = "";
        }
        public void AddTab<PanelType>(string name) where PanelType : Editor
        {
            var tabButton = AddTab(name);
            tabButton.autoSize = true;
            tabButton.textPadding = new RectOffset(10, 10, 1, 1);
            tabButton.textScale = 0.9f;
            tabButton.textHorizontalAlignment = UIHorizontalAlignment.Center;
            tabButton.verticalAlignment = UIVerticalAlignment.Middle;

            tabButton.atlas = NodeMarkupTool.InGameAtlas;

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
