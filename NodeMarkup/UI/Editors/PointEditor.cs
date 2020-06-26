using ColossalFramework.UI;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.UI.Editors
{
    public class PointsEditorPanel : Editor<PointItem>
    {
        public override string PanelName { get; } = "Points";

        public PointsEditorPanel() : base(nameof(PointsEditorPanel))
        {

        }

        public override void SetMarkup(Markup markup)
        {
            base.SetMarkup(markup);

            foreach (var enter in markup.Enters)
            {
                foreach (var point in enter.Points)
                {
                    var item = AddItem(point.ToString());
                    item.Point = point;
                    item.Icon.Color = point.Color;
                }
            }
        }
        protected override void ItemClick(PointItem item)
        {
            
        }
    }
    public class PointItem : EditableItem<ColorIcon>
    {
        public MarkupPoint Point { get; set; }
    }
}
