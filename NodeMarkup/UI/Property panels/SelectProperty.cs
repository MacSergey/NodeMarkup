using ModsCommon.UI;
using NodeMarkup.Manager;

namespace NodeMarkup.UI.Editors
{
    public class RuleEdgeSelectPropertyPanel : SelectPropertyPanel<ILinePartEdge, RuleEdgeSelectPropertyPanel.RuleEdgeSelectButton>
    {
        protected override float Width => 230f;

        public class RuleEdgeSelectButton : SelectListPropertyButton<ILinePartEdge>
        {
            public EdgePosition Position { get; set; }
            protected override string NotSet => NodeMarkup.Localize.SelectPanel_NotSet;
            protected override bool IsEqual(ILinePartEdge first, ILinePartEdge second) => (first == null && second == null) || first?.Equals(second) == true;
        }
    }

    public class CrosswalkBorderSelectPropertyPanel : ResetableSelectPropertyPanel<MarkingRegularLine, CrosswalkBorderSelectPropertyPanel.CrosswalkBorderSelectButton>
    {
        protected override string ResetToolTip => NodeMarkup.Localize.CrosswalkStyle_ResetBorder;
        protected override float Width => 150f;

        public class CrosswalkBorderSelectButton : SelectListPropertyButton<MarkingRegularLine>
        {
            public BorderPosition Position { get; set; }
            protected override string NotSet => NodeMarkup.Localize.SelectPanel_NotSet;
            protected override bool IsEqual(MarkingRegularLine first, MarkingRegularLine second) => ReferenceEquals(first, second);
        }
    }
}
