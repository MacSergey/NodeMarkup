using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.Tools
{
    public class PasteMarkupPointsOrderToolMode : BasePasteMarkupToolMode<SourcePoint>
    {
        public override ToolModeType Type => ToolModeType.PasteMarkupPointOrder;

        protected override Func<int, SourcePoint, bool> AvailableTargetsGetter => (i, s) => i >= 0 && i < Sources.Length;

        protected override Target[] GetTargets(BaseToolMode prevMode) 
            => CheckPrev(prevMode, out PasteMarkupEntersOrderToolMode toolMode) && toolMode.HoverSource.Target is TargetEnter target ? target.Points : new TargetPoint[0];
        protected override SourcePoint[] GetSources(BaseToolMode prevMode)
            => CheckPrev(prevMode, out PasteMarkupEntersOrderToolMode toolMode) ? toolMode.HoverSource.Points : new SourcePoint[0];
        private bool CheckPrev(BaseToolMode prevMode, out PasteMarkupEntersOrderToolMode toolMode)
        {
            toolMode = prevMode as PasteMarkupEntersOrderToolMode;
            return toolMode != null && toolMode.IsHoverSource && toolMode.HoverSource.HasTarget;
        }

        public override void OnSecondaryMouseClicked()
        {
            Tool.SetMode(ToolModeType.PasteMarkupEnterOrder);
        }
    }
}
