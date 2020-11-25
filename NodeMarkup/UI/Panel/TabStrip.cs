using ModsCommon.UI;
using ModsCommon.Utilities;
using IMT.UI.Editors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace IMT.UI.Panel
{
    public class PanelTabStrip : TabStrip<PanelTabStrip.PanelTab>
    {
        private static Color32 NormalColor { get; } = new Color32(107, 113, 115, 255);
        private static Color32 HoverColor { get; } = new Color32(143, 149, 150, 255);
        private static Color32 FocusColor { get; } = new Color32(177, 195, 94, 255);

        public PanelTabStrip() => isLocalized = true;

        protected override void OnLocalize()
        {
            foreach (var tab in Tabs)
                tab.text = tab.Editor.Name;

            ArrangeTabs();
        }

        protected override void SetStyle(PanelTab tabButton)
        {
            tabButton.atlas = TextureHelper.CommonAtlas;

            tabButton.normalBgSprite = TextureHelper.Tab;
            tabButton.focusedBgSprite = TextureHelper.Tab;
            tabButton.hoveredBgSprite = TextureHelper.Tab;
            tabButton.disabledBgSprite = TextureHelper.Tab;

            tabButton.color = NormalColor;
            tabButton.hoveredColor = HoverColor;
            tabButton.pressedColor = FocusColor;
            tabButton.focusedColor = FocusColor;
        }

        public void AddTab(Editor editor)
        {
            var tab = AddTabImpl(editor.Name);
            tab.Editor = editor;
        }
        public class PanelTab : Tab
        {
            public Editor Editor { get; set; }
        }
    }
}
