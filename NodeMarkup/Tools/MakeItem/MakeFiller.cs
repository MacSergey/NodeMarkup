using ColossalFramework.Math;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.UI;
using NodeMarkup.Utilities;
using UnityEngine;

namespace NodeMarkup.Tools
{
    public class MakeFillerToolMode : BaseToolMode
    {
        public override ToolModeType Type => ToolModeType.MakeFiller;

        private FillerContour Contour { get; set; }
        private PointsSelector<IFillerVertex> FillerPointsSelector { get; set; }

        public bool DisableByAlt { get; set; }

        private bool IsHover => FillerPointsSelector.IsHoverPoint;
        private IFillerVertex Hover => FillerPointsSelector.HoverPoint;

        protected override void Reset(BaseToolMode prevMode)
        {
            Contour = new FillerContour(Tool.Markup);
            GetFillerPoints();
        }

        public override void OnToolUpdate()
        {
            if (DisableByAlt && !InputExtension.AltIsPressed && Contour.IsEmpty)
                Tool.SetDefaultMode();
            else
                FillerPointsSelector.OnUpdate();
        }
        public override string GetToolInfo()
        {
            if (IsHover)
                return HoverInfo();
            else if (Contour.IsEmpty)
                return Localize.Tool_InfoFillerSelectStart;
            else
                return Localize.Tool_InfoFillerSelectNext;
        }
        private string HoverInfo()
        {
            if (Contour.IsEmpty)
                return Localize.Tool_InfoFillerClickStart;
            else if (Hover.Equals(Contour.First))
                return Tool.GetModifierToolTip<FillerStyle.FillerType>(Localize.Tool_InfoFillerClickEnd);
            else
                return Localize.Tool_InfoFillerClickNext;
        }

        public override void OnPrimaryMouseClicked(Event e)
        {
            if (IsHover)
            {
                if (Contour.Add(Hover))
                {
                    var style = Tool.GetStyleByModifier<FillerStyle, FillerStyle.FillerType>(FillerStyle.FillerType.Stripe);
                    var filler = new MarkupFiller(Contour, style);
                    Tool.Markup.AddFiller(filler);
                    Panel.EditFiller(filler);
                    Tool.SetDefaultMode();
                    return;
                }
                DisableByAlt = false;
                GetFillerPoints();
            }
        }
        public override void OnSecondaryMouseClicked()
        {
            if (Contour.IsEmpty)
                Tool.SetDefaultMode();
            else
            {
                Contour.Remove();
                GetFillerPoints();
            }
        }
        public override bool OnEscape()
        {
            Tool.SetDefaultMode();
            return true;
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            RenderFillerLines(cameraInfo);
            RenderFillerConnectLine(cameraInfo);
            FillerPointsSelector.Render(cameraInfo);
        }

        private void RenderFillerLines(RenderManager.CameraInfo cameraInfo)
        {
            var color = IsHover && Hover.Equals(Contour.First) ? Colors.Green : Colors.Hover;
            Contour.Render(new OverlayData(cameraInfo) { Color = color });
        }
        private void RenderFillerConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            if (Contour.IsEmpty)
                return;

            if (IsHover)
            {
                var linePart = Contour.GetFillerLine(Contour.Last, Hover);
                if (linePart.GetTrajectory(out ITrajectory trajectory))
                    trajectory.Render(new OverlayData(cameraInfo) { Color = Colors.Green });
            }
            else
            {
                var bezier = new Line3(Contour.Last.Position, NodeMarkupTool.MouseWorldPosition).GetBezier();
                NodeMarkupTool.RenderBezier(bezier, new OverlayData(cameraInfo) { Color = Colors.Hover });
            }
        }

        private void GetFillerPoints() => FillerPointsSelector = new PointsSelector<IFillerVertex>(Contour.GetNextСandidates(), Colors.Red);
    }
}
