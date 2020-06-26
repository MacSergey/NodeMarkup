using ColossalFramework.UI;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.UI.Editors
{
    public class LinesEditorPanel : Editor<LineItem>
    {
        public override string PanelName { get; } = "Lines";

        public LinesEditorPanel() : base(nameof(LinesEditorPanel))
        {

        }
        public override void SetMarkup(Markup markup)
        {
            base.SetMarkup(markup);

            foreach(var line in markup.Lines)
            {
                var item = AddItem(line.PointPair.ToString());
                item.Line = line;
            }
        }
        protected override void ItemClick(LineItem item)
        {
            
        }
    }

    public class LineItem : EditableItem<UIPanel>
    {
        public MarkupLine Line { get; set; }
    }
}
