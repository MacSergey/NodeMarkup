using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.UI.Editors
{
    public class PointsEditorPanel : EditorPanel
    {
        public override string PanelName { get; } = "Points";

        public PointsEditorPanel() : base(nameof(PointsEditorPanel))
        {

        }
    }
}
