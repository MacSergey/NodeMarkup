using ColossalFramework.Math;
using ModsCommon;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeMarkup.Tools
{
    public class MakeCrosswalkToolMode : BaseMakeItemToolMode
    {
        public override ToolModeType Type => ToolModeType.MakeCrosswalk;

        public override string GetToolInfo()
        {
            if (IsSelectPoint)
                return IsHoverPoint ? base.GetToolInfo() : Localize.Tool_InfoSelectCrosswalkEndPoint;
            else
                return Localize.Tool_InfoSelectCrosswalkStartPoint;
        }

        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (!IsSelectPoint && !Utility.OnlyShiftIsPressed)
                Tool.SetDefaultMode();
        }
        public override void OnPrimaryMouseClicked(Event e)
        {
            if (!IsHoverPoint)
                return;

            if (!IsSelectPoint)
                base.OnPrimaryMouseClicked(e);
            else
            {
                var pointPair = new MarkingPointPair(SelectPoint, HoverPoint);

                if (Tool.Marking.TryGetLine(pointPair, out MarkingCrosswalkLine line))
                {
                    if (Utility.OnlyCtrlIsPressed)
                        Panel.SelectCrosswalk(line.Crosswalk);
                    else
                        Tool.DeleteItem(line, OnDelete);
                }
                else
                {
                    var style = Tool.GetStyleByModifier<CrosswalkStyle, CrosswalkStyle.CrosswalkType>(NetworkType.Road, LineType.Crosswalk, CrosswalkStyle.CrosswalkType.Zebra);
                    var newCrosswalkLine = Tool.Marking.AddCrosswalkLine(pointPair, style);
                    Panel.AddLine(newCrosswalkLine);
                    Panel.EditCrosswalk(newCrosswalkLine?.Crosswalk);
                }

                SelectPoint = null;
                SetTarget();
            }
        }
        private void OnDelete(MarkingLine line)
        {
            Panel.DeleteCrosswalk((line as MarkingCrosswalkLine).Crosswalk);
            Panel.DeleteLine(line);
            Tool.Marking.RemoveLine(line);
        }
        protected override IEnumerable<MarkingPoint> GetTarget(Entrance enter, MarkingPoint ignore)
        {
            if (ignore != null && ignore.Enter != enter)
                yield break;

            var nodeEnter = (SegmentEntrance)enter;
            var allow = nodeEnter.CrosswalkPoints.Select(i => 1).ToArray();
            var bridge = new Dictionary<MarkingPoint, int>();
            foreach (var crosswalk in nodeEnter.CrosswalkPoints)
                bridge.Add(crosswalk, bridge.Count);

            var isIgnore = ignore?.Enter == enter;
            var ignoreIdx = isIgnore ? bridge[ignore] : 0;

            var leftIdx = ignoreIdx;
            var rightIdx = ignoreIdx;

            foreach (var line in enter.Marking.Lines.Where(l => l.Type == LineType.Crosswalk && l.Start.Enter == enter))
            {
                var from = Math.Min(bridge[line.Start], bridge[line.End]);
                var to = Math.Max(bridge[line.Start], bridge[line.End]);
                allow[from] = 2;
                allow[to] = 2;
                for (var i = from + 1; i <= to - 1; i += 1)
                    allow[i] = 0;

                if (isIgnore && line.ContainsPoint(ignore))
                {
                    var otherIdx = bridge[line.PointPair.GetOther(ignore)];
                    if (otherIdx < ignoreIdx)
                        leftIdx = otherIdx;
                    else if (otherIdx > ignoreIdx)
                        rightIdx = otherIdx;
                }
            }

            if (isIgnore)
            {
                SetNotAllow(allow, leftIdx == ignoreIdx ? Find(allow, ignoreIdx, -1) : leftIdx, -1);
                SetNotAllow(allow, rightIdx == ignoreIdx ? Find(allow, ignoreIdx, 1) : rightIdx, 1);
                allow[ignoreIdx] = 0;
            }

            foreach (var point in bridge)
            {
                if (allow[point.Value] != 0)
                    yield return point.Key;
            }
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHoverPoint)
                HoverPoint.Render(new OverlayData(cameraInfo) { Color = Colors.Hover, Width = 0.5f });

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
            var bezier = new Line3(SelectPoint.MarkerPosition, HoverPoint.MarkerPosition).GetBezier();
            var pointPair = new MarkingPointPair(SelectPoint, HoverPoint);
            var color = Tool.Marking.ExistLine(pointPair) ? (Utility.OnlyCtrlIsPressed ? Colors.Yellow : Colors.Red) : Colors.Green;

            bezier.RenderBezier(new OverlayData(cameraInfo) { Color = color, Width = MarkingCrosswalkPoint.Shift * 2, Cut = true });
        }
        private void RenderNotConnectCrosswalkLine(RenderManager.CameraInfo cameraInfo)
        {
            var dir = SingletonTool<IntersectionMarkingTool>.Instance.Ray.GetRayPosition(Marking.Position.y, out _) - SelectPoint.MarkerPosition;
            var lenght = dir.magnitude;
            dir.Normalize();
            var bezier = new Line3(SelectPoint.MarkerPosition, SelectPoint.MarkerPosition + dir * Mathf.Max(lenght, 1f)).GetBezier();

            bezier.RenderBezier(new OverlayData(cameraInfo) { Color = Colors.White, Width = MarkingCrosswalkPoint.Shift * 2, Cut = true });
        }
    }
}
