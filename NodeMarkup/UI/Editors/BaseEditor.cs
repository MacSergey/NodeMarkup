using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.UI.Editors
{
    public abstract class EditorPanel : UIPanel
    {
        public abstract string PanelName { get; }
        protected UIScrollablePanel ItemsPanel { get; private set; }
        protected UIPanel SettingsPanel { get; private set; }

        public EditorPanel(string name)
        {
            autoLayout = true;
            autoLayoutDirection = LayoutDirection.Horizontal;

            backgroundSprite = "GenericPanel";

            var lable = AddUIComponent<UILabel>();
            lable.text = name;
        }
        private void AddPanels()
        {
            ItemsPanel = AddUIComponent<UIScrollablePanel>();
            SettingsPanel = AddUIComponent<UIPanel>();

            eventSizeChanged += ((component, size) =>
            {
                ItemsPanel.width = size.x / 10 * 3;
                SettingsPanel.width = size.x / 10 * 7;
                ItemsPanel.height = size.y;
                SettingsPanel.height = size.y;
            });
        }
        public virtual void SetNode()
        {

        }
    }
}
