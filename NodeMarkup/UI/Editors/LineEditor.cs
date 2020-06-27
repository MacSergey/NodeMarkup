using ColossalFramework.UI;
using NodeMarkup.Manager;
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
        protected override void Fill()
        {
            foreach (var line in Markup.Lines)
            {
                AddItem(line);
            }
        }
        protected override void ItemClick(LineItem item)
        {
            
        }
    }

    public class LineItem : EditableItem<MarkupLine, UIPanel> { }
}
