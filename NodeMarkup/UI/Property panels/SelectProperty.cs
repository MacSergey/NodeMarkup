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
    public class RuleEdgeSelectPropertyPanel : SelectPropertyPanel<ILinePartEdge, RuleEdgeSelectPropertyPanel>
    {
        protected override string NotSet => NodeMarkup.Localize.SelectPanel_NotSet;

        public EdgePosition Position { get; set; }
        protected override float Width => 230f;

        protected override bool IsEqual(ILinePartEdge first, ILinePartEdge second) => (first == null && second == null) || first?.Equals(second) == true;
    }

    public class CrosswalkBorderSelectPropertyPanel : ResetableSelectPropertyPanel<MarkupRegularLine, CrosswalkBorderSelectPropertyPanel>
    {
        protected override string NotSet => NodeMarkup.Localize.SelectPanel_NotSet;
        protected override string ResetToolTip => NodeMarkup.Localize.CrosswalkStyle_ResetBorder;

        public BorderPosition Position { get; set; }
        protected override float Width => 150f;

        protected override bool IsEqual(MarkupRegularLine first, MarkupRegularLine second) => ReferenceEquals(first, second);
    }

    public class FillerRailSelectPropertyPanel : ResetableSelectPropertyPanel<object, FillerRailSelectPropertyPanel>
    {
        protected override string ResetToolTip => throw new NotImplementedException();
        protected override string NotSet => throw new NotImplementedException();

        protected override float Width => 150f;

        protected override bool IsEqual(object first, object second) => ReferenceEquals(first, second);
    }
}
