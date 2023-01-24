using ColossalFramework.Math;
using ColossalFramework.UI;
using IMT.Manager;
using ModsCommon;
using ModsCommon.Utilities;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;

namespace IMT.Tools
{
    public class DragPointToolMode : IntersectionMarkingToolMode
    {
        public override ToolModeType Type => ToolModeType.DragPoint;
        public MarkingEnterPoint DragPoint { get; set; } = null;

        protected override void Reset(IToolMode prevMode)
        {
            DragPoint = prevMode is MakeLineToolMode makeLineMode ? makeLineMode.HoverPoint as MarkingEnterPoint : null;
        }
        public override void OnToolGUI(Event e)
        {
            if (!Input.GetMouseButton(0))
                Exit();
        }
        public override void OnMouseDrag(Event e)
        {
            var normal = DragPoint.Enter.CornerDir.Turn90(true);
            var position = SingletonTool<IntersectionMarkingTool>.Instance.Ray.GetRayPosition(DragPoint.Position.y, out _);
            Line2.Intersect(XZ(DragPoint.MarkerPosition), XZ(DragPoint.MarkerPosition + DragPoint.Enter.CornerDir), XZ(position), XZ(position + normal), out float offsetChange, out _);
            DragPoint.Offset.Value = (DragPoint.Offset + offsetChange * Mathf.Sin(DragPoint.Enter.CornerAndNormalAngle)).RoundToNearest(Utility.OnlyShiftIsPressed ? 0.1f : 0.01f);
            Panel.SelectPoint(DragPoint);
        }
        public override void OnPrimaryMouseClicked(Event e) => Exit();
        public override void OnMouseUp(Event e) => Exit();
        private void Exit()
        {
            Panel.SelectPoint(DragPoint);
            Tool.SetDefaultMode();
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            DragPoint.Enter.Render(new OverlayData(cameraInfo) { Color = Colors.Hover, Width = 2f });
            DragPoint.Render(new OverlayData(cameraInfo));
        }
    }
}
