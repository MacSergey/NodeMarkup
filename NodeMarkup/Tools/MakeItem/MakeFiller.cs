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

        private bool IsHover => FillerPointsSelector.IsHoverPoint;
        private IFillerVertex Hover => FillerPointsSelector.HoverPoint;

        protected override void Reset(BaseToolMode prevMode)
        {
            Contour = new FillerContour(Tool.Markup);
            GetFillerPoints();
        }

        public override void OnToolUpdate()
        {
            if (DisableByAlt && !NodeMarkupTool.AltIsPressed && Contour.IsEmpty)
                Tool.SetDefaultMode();
            else
                FillerPointsSelector.OnUpdate();
        }
        public override string GetToolInfo()
        {
            if (IsHover)
                return HoverInfo();
            //return $"{HoverInfo()}\n({Hover})";
            else if (Contour.IsEmpty)
                return Localize.Tool_InfoFillerSelectStart;
            else
                return Localize.Tool_InfoFillerSelectNext;
        }
        private string HoverInfo()
        {
            if (Contour.IsEmpty)
                return Localize.Tool_InfoFillerClickStart;
            else if (Hover == Contour.First)
                return GetCreateToolTip<FillerStyle.FillerType>(Localize.Tool_InfoFillerClickEnd);
            else
                return Localize.Tool_InfoFillerClickNext;
        }

        public override void OnPrimaryMouseClicked(Event e)
        {
            if (IsHover)
            {
                if (Contour.Add(Hover))
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
            var color = IsHover && Hover.Equals(Contour.First) ? Colors.Green : Colors.Hover;
            Contour.Render(cameraInfo, color);
        }
        private void RenderFillerConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            if (Contour.IsEmpty)
                return;

            if (IsHover)
            {
                var linePart = Contour.GetFillerLine(Contour.Last, Hover);
                if (linePart.GetTrajectory(out ILineTrajectory trajectory))
                    trajectory.Render(cameraInfo, Colors.Green);
            }
            else
            {
                var bezier = new Line3(Contour.Last.Position, NodeMarkupTool.MouseWorldPosition).GetBezier();
                NodeMarkupTool.RenderBezier(cameraInfo, bezier, Colors.Hover);
            }
        }

        private void GetFillerPoints() => FillerPointsSelector = new PointsSelector<IFillerVertex>(Contour.GetNextСandidates(), Colors.Red);
    }
}
