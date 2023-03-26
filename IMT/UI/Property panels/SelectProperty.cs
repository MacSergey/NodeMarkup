using IMT.Manager;
using ModsCommon.UI;

namespace IMT.UI.Editors
{
    public class RuleEdgeSelectPropertyPanel : SelectPropertyPanel<ILinePartEdge, RuleEdgeSelectPropertyPanel.RuleEdgeSelectButton>
    {
        protected override float Width => 230f;
        public override void SetStyle(ControlStyle style)
        {
            Selector.SelectorStyle = style.DropDown;
        }

        public class RuleEdgeSelectButton : SelectListPropertyButton<ILinePartEdge>
        {
            public EdgePosition Position { get; set; }
            protected override string NotSet => IMT.Localize.SelectPanel_NotSet;
            protected override bool IsEqual(ILinePartEdge first, ILinePartEdge second) => (first == null && second == null) || first?.Equals(second) == true;
        }
    }

    public class CrosswalkBorderSelectPropertyPanel : ResetableSelectPropertyPanel<MarkingRegularLine, CrosswalkBorderSelectPropertyPanel.CrosswalkBorderSelectButton>
    {
        protected override string ResetToolTip => IMT.Localize.CrosswalkStyle_ResetBorder;
        protected override float Width => 150f;
        public override void SetStyle(ControlStyle style)
        {
            Selector.SelectorStyle = style.DropDown;
        }

        public class CrosswalkBorderSelectButton : SelectListPropertyButton<MarkingRegularLine>
        {
            public BorderPosition Position { get; set; }
            protected override string NotSet => IMT.Localize.SelectPanel_NotSet;
            protected override bool IsEqual(MarkingRegularLine first, MarkingRegularLine second) => ReferenceEquals(first, second);
        }
    }
}
