using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.UI.Editors
{
    public class LinesEditorPanel : EditorPanel
    {
        public override string PanelName { get; } = "Lines";

        public LinesEditorPanel() : base(nameof(LinesEditorPanel))
        {

        }
    }
}
