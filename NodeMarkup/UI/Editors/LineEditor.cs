using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.UI.Editors
{
    public class LinesEditor : Editor<LineItem, MarkupLine, UIPanel>
    {
        public override string Name { get; } = "Lines";

        public LinesEditor()
        {

        }
        protected override void FillItems()
        {
            foreach (var line in Markup.Lines)
            {
                AddItem(line);
            }
        }
        protected override void ItemClick(LineItem item)
        {
            ClearSettings();

            foreach (var rule in item.Object.RawRules)
            {
                var rulePanel = SettingsPanel.AddUIComponent<RulePanel>();
                rulePanel.Init();
            }
        }
    }

    public class LineItem : EditableItem<MarkupLine, UIPanel> { }

    public class RulePanel : UIPanel
    {
        public RulePanel()
        {
            atlas = TextureUtil.GetAtlas("Ingame");
            backgroundSprite = "GenericPanel";
            autoLayout = true;
            autoFitChildrenVertically = true;
            autoLayoutDirection = LayoutDirection.Vertical;
            height = 100;
        }

        public void Init()
        {

        }
    }
}
