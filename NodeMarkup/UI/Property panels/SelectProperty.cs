using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class MarkupLineSelectPropertyPanel : SelectPropertyPanel<ILinePartEdge, MarkupLineSelectPropertyPanel>
    {
        protected override string NotSet => NodeMarkup.Localize.SelectPanel_NotSet;
        public EdgePosition Position { get; set; }
        protected override float Width => 230f;

        protected override bool IsEqual(ILinePartEdge first, ILinePartEdge second) => (first == null && second == null) || first?.Equals(second) == true;
    }

    public class MarkupCrosswalkSelectPropertyPanel : ResetableSelectPropertyPanel<MarkupRegularLine, MarkupCrosswalkSelectPropertyPanel>
    {
        protected override string NotSet => NodeMarkup.Localize.SelectPanel_NotSet;
        protected override string ResetToolTip => NodeMarkup.Localize.CrosswalkStyle_ResetBorder;

        public BorderPosition Position { get; set; }
        protected override float Width => 150f;

        protected override bool IsEqual(MarkupRegularLine first, MarkupRegularLine second) => ReferenceEquals(first, second);
    }
}
