using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.UI.Editors;
using UnityEngine;

namespace NodeMarkup.UI.Panel
{
    public class PanelTabStrip : TabStrip<PanelTabStrip.PanelTab>
    {
        private static Color32 NormalColor { get; } = new Color32(107, 113, 115, 255);
        private static Color32 HoverColor { get; } = new Color32(143, 149, 150, 255);
        private static Color32 PressedColor { get; } = new Color32(153, 159, 160, 255);
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
            tabButton.atlas = CommonTextures.Atlas;

            tabButton.normalBgSprite = CommonTextures.Tab;
            tabButton.focusedBgSprite = CommonTextures.Tab;
            tabButton.hoveredBgSprite = CommonTextures.Tab;
            tabButton.disabledBgSprite = CommonTextures.Tab;

            tabButton.color = NormalColor;
            tabButton.hoveredColor = HoverColor;
            tabButton.pressedColor = PressedColor;
            tabButton.focusedColor = FocusColor;
        }
        public void SetVisible(Marking markup)
        {
            foreach (var tab in Tabs)
                tab.isVisible = (markup.Support & tab.Editor.Support) != 0;
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
