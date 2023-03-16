using IMT.Manager;
using IMT.UI.Editors;
using ModsCommon.UI;
using ModsCommon.Utilities;
using UnityEngine;

namespace IMT.UI.Panel
{
    public class PanelTabStrip : TabStrip<PanelTabStrip.PanelTab>
    {
        private bool available = true;
        public bool Available
        {
            get => available;
            set
            {
                if (value != available)
                {
                    available = value;
                    Blur.isVisible = !available;
                }
            }
        }
        private BlurEffect Blur { get; set; }

        public PanelTabStrip()
        {
            isLocalized = true;

            this.DefaultStyle();

            backgroundSprite = CommonTextures.Empty;

            color = disabledColor = TabColor = new Color32(60, 64, 66, 255);
            TabHoveredColor = new Color32(132, 141, 145, 255);
            TabPressedColor = new Color32(104, 111, 115, 255);
            TabFocusedColor = new Color32(155, 175, 86, 255);
            TabDisabledColor = new Color32(36, 36, 36, 255);
            TabFocusedDisabledColor = new Color32(111, 125, 61, 255);

            Blur = AddUIComponent<BlurEffect>();
            Blur.relativePosition = Vector3.zero;
            Blur.size = size;
            Blur.isVisible = false;
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

            Blur.zOrder = int.MaxValue;
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            Blur.size = size;
        }

        public class PanelTab : Tab
        {
            public Editor Editor { get; set; }
        }
    }
}
