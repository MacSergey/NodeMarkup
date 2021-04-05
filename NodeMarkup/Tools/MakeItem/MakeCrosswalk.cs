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

            if (!IsSelectPoint && !InputExtension.ShiftIsPressed)
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
                var pointPair = new MarkupPointPair(SelectPoint, HoverPoint);

                if (Tool.Markup.TryGetLine(pointPair, out MarkupLine line))
                    Tool.DeleteItem(line, OnDelete);
                else
                {
                    var style = Tool.GetStyleByModifier<CrosswalkStyle, CrosswalkStyle.CrosswalkType>(CrosswalkStyle.CrosswalkType.Zebra);
                    var newCrosswalkLine = Tool.Markup.AddCrosswalkLine(pointPair, style);
                    Panel.EditCrosswalk(newCrosswalkLine?.Crosswalk);
                }

                SelectPoint = null;
                SetTarget();
            }
        }
        private void OnDelete(MarkupLine line)
        {
            Tool.Markup.RemoveLine(line);
            Panel.DeleteCrosswalk((line as MarkupCrosswalkLine).Crosswalk);
            Panel.DeleteLine(line);
        }
        protected override IEnumerable<MarkupPoint> GetTarget(Enter enter, MarkupPoint ignore)
        {
            if (ignore != null && ignore.Enter != enter)
                yield break;

            var nodeEnter = (SegmentEnter)enter;
            var allow = nodeEnter.Crosswalks.Select(i => 1).ToArray();
            var bridge = new Dictionary<MarkupPoint, int>();
            foreach (var crosswalk in nodeEnter.Crosswalks)
                bridge.Add(crosswalk, bridge.Count);

            var isIgnore = ignore?.Enter == enter;
            var ignoreIdx = isIgnore ? bridge[ignore] : 0;

            var leftIdx = ignoreIdx;
            var rightIdx = ignoreIdx;

            foreach (var line in enter.Markup.Lines.Where(l => l.Type == MarkupLine.LineType.Crosswalk && l.Start.Enter == enter))
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
            var bezier = new Line3(SelectPoint.Position, HoverPoint.Position).GetBezier();
            var pointPair = new MarkupPointPair(SelectPoint, HoverPoint);
            var color = Tool.Markup.ExistLine(pointPair) ? Colors.Red : Colors.Green;

            bezier.RenderBezier(new OverlayData(cameraInfo) { Color = color, Width = MarkupCrosswalkPoint.Shift * 2, Cut = true });
        }
        private void RenderNotConnectCrosswalkLine(RenderManager.CameraInfo cameraInfo)
        {
            var dir = SingletonTool<NodeMarkupTool>.Instance.Ray.GetRayPosition(Markup.Position.y, out _) - SelectPoint.Position;
            var lenght = dir.magnitude;
            dir.Normalize();
            var bezier = new Line3(SelectPoint.Position, SelectPoint.Position + dir * Mathf.Max(lenght, 1f)).GetBezier();

            bezier.RenderBezier(new OverlayData(cameraInfo) { Color = Colors.White, Width = MarkupCrosswalkPoint.Shift * 2, Cut = true });
        }
    }
}
