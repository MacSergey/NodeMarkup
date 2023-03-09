using IMT.Manager;
using IMT.UI.Editors;
using ModsCommon.UI;
using ModsCommon.Utilities;
using UnityEngine;

namespace IMT.UI.Panel
{
    public class PanelTabStrip : TabStrip<PanelTabStrip.PanelTab>
    {
        public PanelTabStrip()
        {
            isLocalized = true;

            this.CustomStyle();

            backgroundSprite = CommonTextures.Empty;

            color = disabledColor = TabColor = new Color32(66, 69, 71, 255);
            TabHoveredColor = new Color32(125, 134, 131, 255);
            TabPressedColor = new Color32(134, 143, 140, 255);
            TabFocusedColor = new Color32(155, 175, 86, 255);
            TabDisabledColor = new Color32(36, 38, 37, 255);
            TabFocusedDisabledColor = new Color32(111, 125, 61, 255);
        }

        protected override void OnLocalize()
        {
            foreach (var tab in Tabs)
                tab.text = tab.Editor.Name;

            ArrangeTabs();
        }
        public void SetVisible(Marking marking)
        {
            foreach (var tab in Tabs)
                tab.isVisible = (marking.Support & tab.Editor.Support) != 0;
        }

        public void AddTab(Editor editor)
        {
            var tab = AddTabImpl(editor.Name);
            tab.textPadding.top = 4;
            tab.Editor = editor;
        }
        public class PanelTab : Tab
        {
            public Editor Editor { get; set; }
        }
    }
}
