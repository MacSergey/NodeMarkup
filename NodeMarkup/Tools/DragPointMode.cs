using ColossalFramework.Math;
using ColossalFramework.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Utilities;
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
        public MarkupEnterPoint DragPoint { get; set; } = null;

        protected override void Reset(BaseToolMode prevMode)
        {
            DragPoint = prevMode is MakeLineToolMode makeLineMode ? makeLineMode.HoverPoint as MarkupEnterPoint : null;
        }
        public override void OnMouseDrag(Event e)
        {
            var normal = DragPoint.Enter.CornerDir.Turn90(true);
            Line2.Intersect(DragPoint.Position.XZ(), (DragPoint.Position + DragPoint.Enter.CornerDir).XZ(), NodeMarkupTool.MouseWorldPosition.XZ(), (NodeMarkupTool.MouseWorldPosition + normal).XZ(), out float offsetChange, out _);
            DragPoint.Offset.Value = (DragPoint.Offset + offsetChange * Mathf.Sin(DragPoint.Enter.CornerAndNormalAngle)).RoundToNearest(0.01f);
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
            DragPoint.Enter.Render(new OverlayData(cameraInfo) { Color = Colors.Hover, Width = 2f });
            DragPoint.Render(new OverlayData(cameraInfo));
        }
    }
}
