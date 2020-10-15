using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.Tools
{
    public class PointsOrderToolMode : BaseOrderToolMode<SourcePoint>
    {
        public override ToolModeType Type => ToolModeType.PasteMarkupPointOrder;

        public SourceEnter SourceEnter { get; private set; }
        public TargetEnter TargetEnter { get; private set; }
        protected override void Reset(BaseToolMode prevMode)
        {
            var toolMode = prevMode as EntersOrderToolMode;
            SourceEnter = toolMode != null && toolMode.IsHoverSource && toolMode.HoverSource.HasTarget ? toolMode.HoverSource : null;
            TargetEnter = SourceEnter?.Target as TargetEnter;

            base.Reset(prevMode);
        }

        protected override Target<SourcePoint>[] GetTargets(BaseToolMode prevMode) => TargetEnter?.Points ?? new TargetPoint[0];
        protected override SourcePoint[] GetSources(BaseToolMode prevMode) => SourceEnter?.Points ?? new SourcePoint[0];

        public override void OnSecondaryMouseClicked()
        {
            Tool.SetMode(ToolModeType.PasteMarkupEnterOrder);
        }
        protected override Target<SourcePoint>[] GetAvailableTargets(SourcePoint source)
        {
            var borders = new PointsBorders(this, source);
            var avalibleTargets = borders.GetTargets(this, Targets).ToArray();
            return avalibleTargets;
        }
        protected override Basket<SourcePoint>[] GetBaskets()
        {
            var sourcesBorders = Sources.Where(s => !(s.Target is TargetPoint)).ToDictionary(s => s, s => new PointsBorders(this, s));
            var baskets = sourcesBorders.GroupBy(b => b.Value, b => b.Key, PointsBorders.Comparer).Select(g => new PointsBasket(this, g.Key, g)).ToArray();
            return baskets;
        }
    }
}
