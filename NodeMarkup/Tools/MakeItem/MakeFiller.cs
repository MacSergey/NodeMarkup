using ColossalFramework.Math;
using NodeMarkup.Manager;
using NodeMarkup.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.Tools
{
    public class MakeFillerToolMode : BaseToolMode
    {
        public override ToolModeType Type => ToolModeType.MakeFiller;

        private FillerContour Contour { get; set; }
        private PointsSelector<IFillerVertex> FillerPointsSelector { get; set; }

        public bool DisableByAlt { get; set; }

        protected override void Reset(BaseToolMode prevMode)
        {
            Contour = new FillerContour(Tool.Markup);
            GetFillerPoints();
        }

        public override void OnUpdate() => FillerPointsSelector.OnUpdate();
        public override string GetToolInfo()
        {
            if (FillerPointsSelector.IsHoverPoint)
            {
                if (Contour.IsEmpty)
                    return Localize.Tool_InfoFillerClickStart;
                else if (FillerPointsSelector.HoverPoint == Contour.First)
                    return GetCreateToolTip<FillerStyle.FillerType>(Localize.Tool_InfoFillerClickEnd);
                else
                    return Localize.Tool_InfoFillerClickNext;
            }
            else if (Contour.IsEmpty)
                return Localize.Tool_InfoFillerSelectStart;
            else
                return Localize.Tool_InfoFillerSelectNext;
        }
        public override bool ProcessShortcuts(Event e)
        {
            if (DisableByAlt && !NodeMarkupTool.AltIsPressed && Contour.IsEmpty)
            {
                Tool.SetDefaultMode();
                return true;
            }
            else
                return false;
        }
        public override void OnPrimaryMouseClicked(Event e)
        {
            if (FillerPointsSelector.IsHoverPoint)
            {
                if (Contour.Add(FillerPointsSelector.HoverPoint))
                {
                    var filler = new MarkupFiller(Contour, NodeMarkupTool.GetStyle(FillerStyle.FillerType.Stripe));
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
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            RenderFillerLines(cameraInfo);
            RenderFillerConnectLine(cameraInfo);
            FillerPointsSelector.Render(cameraInfo);
        }

        private void RenderFillerLines(RenderManager.CameraInfo cameraInfo)
        {
            var color = FillerPointsSelector.IsHoverPoint && FillerPointsSelector.HoverPoint.Equals(Contour.First) ? Colors.Green : Colors.White;
            foreach (var trajectory in Contour.Trajectories)
                NodeMarkupTool.RenderTrajectory(cameraInfo, color, trajectory);
        }
        private void RenderFillerConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            if (Contour.IsEmpty)
                return;

            if (FillerPointsSelector.IsHoverPoint)
            {
                var linePart = Contour.GetFillerLine(Contour.Last, FillerPointsSelector.HoverPoint);
                if (linePart.GetTrajectory(out ILineTrajectory trajectory))
                    NodeMarkupTool.RenderTrajectory(cameraInfo, Colors.Green, trajectory);
            }
            else
            {
                var bezier = new Line3(Contour.Last.Position, NodeMarkupTool.MouseWorldPosition).GetBezier();
                NodeMarkupTool.RenderBezier(cameraInfo, Colors.White, bezier);
            }
        }

        private void GetFillerPoints() => FillerPointsSelector = new PointsSelector<IFillerVertex>(Contour.GetNextСandidates(), Colors.Red);
    }
}
