using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI
{
    public class UITabPanel : UIPanel
    {
        private UITabstrip TabStrip { get; set; }
        List<UIPanel> TabPanels { get; } = new List<UIPanel>();

        private Vector2 TabPanelSize => new Vector2(width, height - TabStrip.height);
        private Vector2 TabPanelPosition => new Vector2(0, TabStrip.height);
        public UITabPanel()
        {
            TabStrip = AddUIComponent<UITabstrip>();
            TabStrip.anchor = UIAnchorStyle.Top;
            TabStrip.eventSelectedIndexChanged += TabStripSelectedIndexChanged;
        }
        public PanelType AddTab<PanelType>(string name) where PanelType : UIPanel
        {
            var tabButton = TabStrip.AddTab(name);
            tabButton.autoSize = true;
            tabButton.textPadding = new RectOffset(10, 10, 1, 1);
            tabButton.textHorizontalAlignment = UIHorizontalAlignment.Center;
            tabButton.verticalAlignment = UIVerticalAlignment.Middle;

            tabButton.normalBgSprite = "SubBarButtonBase";
            tabButton.disabledBgSprite = "SubBarButtonBaseDisabled";
            tabButton.focusedBgSprite = "SubBarButtonBaseFocused";
            tabButton.hoveredBgSprite = "SubBarButtonBaseHovered";
            tabButton.pressedBgSprite = "SubBarButtonBasePressed";

            tabButton.Invalidate();

            TabStrip.FitChildrenVertically();

            var tabPanel = AddUIComponent<PanelType>();
            tabPanel.isVisible = false;
            tabPanel.size = TabPanelSize;
            tabPanel.relativePosition = TabPanelPosition;

            TabPanels.Add(tabPanel);

            return tabPanel;
        }

        protected override void OnSizeChanged()
        {
            TabStrip.width = width;
            TabStrip.FitChildrenVertically();

            foreach (var tab in TabPanels)
            {
                tab.size = TabPanelSize;
            }
        }
        private void TabStripSelectedIndexChanged(UIComponent component, int index)
        {
            foreach (var tab in TabPanels)
            {
                tab.isVisible = false;
            }
            if (TabPanels.Count > index)
                TabPanels[index].isVisible = true;
        }
    }
}
