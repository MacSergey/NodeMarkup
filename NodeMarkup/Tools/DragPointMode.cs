using ColossalFramework.Math;
using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.Tools
{
    public class DragPointToolMode : BaseToolMode
    {
        public override ToolModeType Type => ToolModeType.DragPoint;
        public MarkupPoint DragPoint { get; set; } = null;

        protected override void Reset(BaseToolMode prevMode)
        {
            DragPoint = prevMode is MakeLineToolMode makeLineMode ? makeLineMode.HoverPoint : null;
        }
        public override void OnMouseDrag(Event e)
        {
            var normal = DragPoint.Enter.CornerDir.Turn90(true);
            Line2.Intersect(DragPoint.Position.XZ(), (DragPoint.Position + DragPoint.Enter.CornerDir).XZ(), NodeMarkupTool.MouseWorldPosition.XZ(), (NodeMarkupTool.MouseWorldPosition + normal).XZ(), out float offsetChange, out _);
            DragPoint.Offset = (DragPoint.Offset + offsetChange * Mathf.Sin(DragPoint.Enter.CornerAndNormalAngle)).RoundToNearest(0.01f);
            Panel.EditPoint(DragPoint);
        }
        public override void OnMouseUp(Event e)
        {
            Panel.EditPoint(DragPoint);
            Tool.SetDefaultMode();
        }
        public override void OnPrimaryMouseClicked(Event e)
        {
            Panel.EditPoint(DragPoint);
            Tool.SetDefaultMode();
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (DragPoint.Type == MarkupPoint.PointType.Crosswalk)
                RenderEnterOverlay(cameraInfo, DragPoint.Enter, DragPoint.Direction * MarkupCrosswalkPoint.Shift, 4f);
            else
                RenderEnterOverlay(cameraInfo, DragPoint.Enter, Vector3.zero, 2f);

            NodeMarkupTool.RenderPointOverlay(cameraInfo, DragPoint);
        }

        private void RenderEnterOverlay(RenderManager.CameraInfo cameraInfo, Enter enter, Vector3 shift, float width)
        {
            if (enter.Position == null)
                return;

            var bezier = new Line3(enter.Position.Value - enter.CornerDir * enter.RoadHalfWidth + shift, enter.Position.Value + enter.CornerDir * enter.RoadHalfWidth + shift).GetBezier();
            NodeMarkupTool.RenderBezier(cameraInfo, Colors.White, bezier, width);
        }
    }
}
