using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class RuleEdgeSelectPropertyPanel : SelectListPropertyPanel<ILinePartEdge, RuleEdgeSelectPropertyPanel>
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

    public class FillerRailSelectPropertyPanel : SelectItemPropertyPanel<FillerRail, FillerRailSelectPropertyPanel>
    {
        protected override string NotSet => string.Empty;

        protected override float Width => 100f;

        public PeriodicFillerStyle.RailType RailType { get; private set; }
        public FillerRailSelectPropertyPanel OtherRail { get; set; }

        public void Init(PeriodicFillerStyle.RailType railType)
        {
            RailType = railType;
            base.Init();
        }
        public override void DeInit()
        {
            RailType = PeriodicFillerStyle.RailType.Left;
            base.DeInit();
        }
    }
}
