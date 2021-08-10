using ColossalFramework.Math;
using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Utilities;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;

namespace NodeMarkup.Tools
{
    public class DragPointToolMode : NodeMarkupToolMode
    {
        public override ToolModeType Type => ToolModeType.DragPoint;
        public MarkupEnterPoint DragPoint { get; set; } = null;

        protected override void Reset(IToolMode prevMode)
        {
            DragPoint = prevMode is MakeLineToolMode makeLineMode ? makeLineMode.HoverPoint as MarkupEnterPoint : null;
        }
        public override void OnMouseDrag(Event e)
        {
            var normal = DragPoint.Enter.CornerDir.Turn90(true);
            var position = SingletonTool<NodeMarkupTool>.Instance.Ray.GetRayPosition(Markup.Position.y, out _);
            Line2.Intersect(XZ(DragPoint.Position), XZ(DragPoint.Position + DragPoint.Enter.CornerDir), XZ(position), XZ(position + normal), out float offsetChange, out _);
            DragPoint.Offset.Value = (DragPoint.Offset + offsetChange * Mathf.Sin(DragPoint.Enter.CornerAndNormalAngle)).RoundToNearest(0.01f);
            Panel.SelectPoint(DragPoint);
        }
        public override void OnPrimaryMouseClicked(Event e)
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
