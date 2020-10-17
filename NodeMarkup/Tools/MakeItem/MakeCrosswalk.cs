using ColossalFramework.Math;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.Tools
{
    public class MakeCrosswalkToolMode : BaseMakeItemToolMode
    {
        public override ToolModeType Type => ToolModeType.MakeCrosswalk;

        protected override void Reset(BaseToolMode prevMode)
        {
            base.Reset(prevMode);
            SetTarget(MarkupPoint.PointType.Crosswalk);
        }

        public override string GetToolInfo()
        {
            if (IsSelectPoint)
                return IsHoverPoint ? base.GetToolInfo() : Localize.Tool_InfoSelectCrosswalkEndPoint;
            else
                return Localize.Tool_InfoSelectCrosswalkStartPoint;
        }
        public override bool ProcessShortcuts(Event e)
        {
            if (IsSelectPoint)
                return false;
            else if (base.ProcessShortcuts(e))
                return true;
            else if (!NodeMarkupTool.ShiftIsPressed)
            {
                Tool.SetDefaultMode();
                SetTarget();
                return true;
            }
            else
                return false;
        }
        public override void OnMouseUp(Event e) => OnPrimaryMouseClicked(e);
        public override void OnPrimaryMouseClicked(Event e)
        {
            if (!IsHoverPoint)
                return;

            if (!IsSelectPoint)
                base.OnPrimaryMouseClicked(e);
            else
            {
                var pointPair = new MarkupPointPair(SelectPoint, HoverPoint);

                if (Tool.Markup.TryGetLine(pointPair, out MarkupLine line))
                    Tool.DeleteItem(line, () => Tool.Markup.RemoveConnect(line));
                else
                {
                    var newCrosswalkLine = Tool.Markup.AddConnection(pointPair, NodeMarkupTool.GetStyle(CrosswalkStyle.CrosswalkType.Zebra)) as MarkupCrosswalkLine;
                    Panel.EditCrosswalk(newCrosswalkLine?.Crosswalk);
                }

                SelectPoint = null;
                SetTarget();
            }
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHoverPoint)
                NodeMarkupTool.RenderPointOverlay(cameraInfo, HoverPoint, Colors.White, 0.5f);

            RenderPointsOverlay(cameraInfo);

            if (IsSelectPoint)
            {
                if (IsHoverPoint)
                    RenderConnectCrosswalkLine(cameraInfo);
                else
                    RenderNotConnectCrosswalkLine(cameraInfo);
            }
        }

        private void RenderConnectCrosswalkLine(RenderManager.CameraInfo cameraInfo)
        {
            var bezier = new Line3(SelectPoint.Position, HoverPoint.Position).GetBezier();
            var pointPair = new MarkupPointPair(SelectPoint, HoverPoint);
            var color = Tool.Markup.ExistConnection(pointPair) ? Colors.Red : Colors.Green;

            NodeMarkupTool.RenderBezier(cameraInfo, color, bezier, MarkupCrosswalkPoint.Shift * 2, true);
        }
        private void RenderNotConnectCrosswalkLine(RenderManager.CameraInfo cameraInfo)
        {
            var dir = NodeMarkupTool.MouseWorldPosition - SelectPoint.Position;
            var lenght = dir.magnitude;
            dir.Normalize();
            var bezier = new Line3(SelectPoint.Position, SelectPoint.Position + dir * Mathf.Max(lenght, 1f)).GetBezier();

            NodeMarkupTool.RenderBezier(cameraInfo, Colors.White, bezier, MarkupCrosswalkPoint.Shift * 2, true);
        }
    }
}
