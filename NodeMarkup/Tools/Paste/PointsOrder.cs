using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.Tools
{
    public class PasteMarkupPointsOrderToolMode : BasePasteMarkupToolMode<SourcePoint, TargetPoint>
    {
        public override ToolModeType Type => ToolModeType.PasteMarkupPointOrder;

        protected override Func<int, SourcePoint, bool> AvailableTargetsGetter => (i, s) => i >= 0 && i < Sources.Length;

        protected override TargetPoint[] GetTargets(BaseToolMode prevMode) 
            => CheckPrev(prevMode, out PasteMarkupEntersOrderToolMode entersOrderToolMode) ? entersOrderToolMode.HoverSource.Target.Points : new TargetPoint[0];
        protected override SourcePoint[] GetSources(BaseToolMode prevMode)
            => CheckPrev(prevMode, out PasteMarkupEntersOrderToolMode entersOrderToolMode) ? entersOrderToolMode.HoverSource.Points : new SourcePoint[0];
        private bool CheckPrev(BaseToolMode prevMode, out PasteMarkupEntersOrderToolMode entersOrderToolMode)
        {
            entersOrderToolMode = prevMode as PasteMarkupEntersOrderToolMode;
            return entersOrderToolMode != null && entersOrderToolMode.IsHoverSource && entersOrderToolMode.HoverSource.HasTarget;
        }

        public override void OnSecondaryMouseClicked()
        {
            Tool.SetMode(ToolModeType.PasteMarkupEnterOrder);
        }
    }
}
