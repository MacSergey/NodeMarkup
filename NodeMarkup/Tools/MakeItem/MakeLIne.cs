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
    public class MakeLineToolMode : BaseMakeItemToolMode
    {
        public override ToolModeType Type => ToolModeType.MakeLine;

        public override string GetToolInfo()
        {
            if (IsSelectPoint)
                return IsHoverPoint ? base.GetToolInfo() : Localize.Tool_InfoSelectLineEndPoint;
            else
                return $"{Localize.Tool_InfoSelectLineStartPoint}\n{Localize.Tool_InfoStartDragPointMode}\n{Localize.Tool_InfoStartCreateFiller}\n{Localize.Tool_InfoStartCreateCrosswalk}";
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (IsSelectPoint)
                return;
            else if (NodeMarkupTool.OnlyAltIsPressed)
            {
                Tool.SetMode(ToolModeType.MakeFiller);
                if (Tool.NextMode is MakeFillerToolMode fillerToolMode)
                    fillerToolMode.DisableByAlt = true;
            }
            else if (NodeMarkupTool.OnlyShiftIsPressed)
                Tool.SetMode(ToolModeType.MakeCrosswalk);
        }

        public override void OnMouseDown(Event e)
        {
            if (!IsSelectPoint && IsHoverPoint && NodeMarkupTool.CtrlIsPressed)
                Tool.SetMode(ToolModeType.DragPoint);
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
                    Tool.DeleteItem(line, () =>
                    {
                        Tool.Markup.RemoveConnect(line);
                        Panel.UpdatePanel();
                    });
                else
                {
                    var lineType = pointPair.IsStopLine ? NodeMarkupTool.GetStyle(StopLineStyle.StopLineType.Solid) : NodeMarkupTool.GetStyle(RegularLineStyle.RegularLineType.Dashed);
                    var newLine = Tool.Markup.AddConnection(pointPair, lineType);
                    Panel.EditLine(newLine);
                }

                SelectPoint = null;
                SetTarget();
            }
        }
        protected override IEnumerable<MarkupPoint> GetTarget(Enter enter, MarkupPoint ignore)
        {
            var allow = enter.Points.Select(i => 1).ToArray();

            if (ignore != null && ignore.Enter == enter)
            {
                var ignoreIdx = ignore.Num - 1;
                var leftIdx = ignoreIdx;
                var rightIdx = ignoreIdx;

                foreach (var line in enter.Markup.Lines.Where(l => l.Type == MarkupLine.LineType.Stop && l.Start.Enter == enter))
                {
                    var from = Math.Min(line.Start.Num, line.End.Num) - 1;
                    var to = Math.Max(line.Start.Num, line.End.Num) - 1;
                    if (from < ignore.Num - 1 && ignore.Num - 1 < to)
                        yield break;

                    allow[from] = 2;
                    allow[to] = 2;

                    for (var i = from + 1; i <= to - 1; i += 1)
                        allow[i] = 0;

                    if (line.ContainsPoint(ignore))
                    {
                        var otherIdx = line.PointPair.GetOther(ignore).Num - 1;
                        if (otherIdx < ignoreIdx)
                            leftIdx = otherIdx;
                        else if (otherIdx > ignoreIdx)
                            rightIdx = otherIdx;
                    }
                }

                SetNotAllow(allow, leftIdx == ignoreIdx ? Find(allow, ignoreIdx, -1) : leftIdx, -1);
                SetNotAllow(allow, rightIdx == ignoreIdx ? Find(allow, ignoreIdx, 1) : rightIdx, 1);
                allow[ignoreIdx] = 0;
            }

            foreach (var point in enter.Points)
            {
                if (allow[point.Num - 1] != 0)
                    yield return point;
            }
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHoverPoint)
                HoverPoint.Render(cameraInfo, Colors.Hover, 0.5f);

            RenderPointsOverlay(cameraInfo);

            if (IsSelectPoint)
            {
                switch (IsHoverPoint)
                {
                    case true when HoverPoint.Type == MarkupPoint.PointType.Normal:
                        RenderNormalConnectLine(cameraInfo);
                        break;
                    case true:
                        RenderRegularConnectLine(cameraInfo);
                        break;
                    case false:
                        RenderNotConnectLine(cameraInfo);
                        break;
                }
            }

            Panel.Render(cameraInfo);
        }

        private void RenderRegularConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            var bezier = new Bezier3()
            {
                a = SelectPoint.Position,
                b = HoverPoint.Enter == SelectPoint.Enter ? HoverPoint.Position - SelectPoint.Position : SelectPoint.Direction,
                c = HoverPoint.Enter == SelectPoint.Enter ? SelectPoint.Position - HoverPoint.Position : HoverPoint.Direction,
                d = HoverPoint.Position,
            };

            var pointPair = new MarkupPointPair(SelectPoint, HoverPoint);
            var color = Tool.Markup.ExistConnection(pointPair) ? Colors.Red : Colors.Green;

            NetSegment.CalculateMiddlePoints(bezier.a, bezier.b, bezier.d, bezier.c, true, true, out bezier.b, out bezier.c);
            NodeMarkupTool.RenderBezier(cameraInfo, bezier, color);
        }
        private void RenderNormalConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            var pointPair = new MarkupPointPair(SelectPoint, HoverPoint);
            var color = Tool.Markup.ExistConnection(pointPair) ? Colors.Red : Colors.Purple;

            var lineBezier = new Bezier3()
            {
                a = SelectPoint.Position,
                b = HoverPoint.Position,
                c = SelectPoint.Position,
                d = HoverPoint.Position,
            };
            NodeMarkupTool.RenderBezier(cameraInfo, lineBezier, color);

            var normal = SelectPoint.Direction.Turn90(false);

            var normalBezier = new Bezier3
            {
                a = SelectPoint.Position + SelectPoint.Direction,
                d = SelectPoint.Position + normal
            };
            normalBezier.b = normalBezier.a + normal / 2;
            normalBezier.c = normalBezier.d + SelectPoint.Direction / 2;
            NodeMarkupTool.RenderBezier(cameraInfo, normalBezier, color, 2f, cut: true);
        }
        private void RenderNotConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            var bezier = new Bezier3()
            {
                a = SelectPoint.Position,
                b = SelectPoint.Direction,
                c = SelectPoint.Direction.Turn90(true),
                d = NodeMarkupTool.MouseWorldPosition,
            };

            Line2.Intersect(VectorUtils.XZ(bezier.a), VectorUtils.XZ(bezier.a + bezier.b), VectorUtils.XZ(bezier.d), VectorUtils.XZ(bezier.d + bezier.c), out _, out float v);
            bezier.c = v >= 0 ? bezier.c : -bezier.c;

            NetSegment.CalculateMiddlePoints(bezier.a, bezier.b, bezier.d, bezier.c, true, true, out bezier.b, out bezier.c);
            NodeMarkupTool.RenderBezier(cameraInfo, bezier, Colors.Hover);
        }
    }
}
