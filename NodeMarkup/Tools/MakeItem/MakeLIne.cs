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
    public class MakeLineToolMode : BaseMakeItemToolMode
    {
        public override ToolModeType Type => ToolModeType.MakeLine;

        public override string GetToolInfo()
        {
            var tips = new List<string>();

            if (!IsSelectPoint)
            {
                tips.Add(Localize.Tool_InfoSelectLineStartPoint);
                tips.Add(Localize.Tool_InfoStartDragPointMode);
                if (Markup is ISupportFillers)
                    tips.Add(Localize.Tool_InfoStartCreateFiller);
                if (Markup is ISupportCrosswalks)
                    tips.Add(Localize.Tool_InfoStartCreateCrosswalk);
            }
            else if (IsHoverPoint)
                tips.Add(base.GetToolInfo());
            else
            {
                if ((SelectPoint.Markup.SupportLines & MarkupLine.LineType.Stop) == 0)
                    tips.Add(Localize.Tool_InfoSelectLineEndPoint);
                else
                    tips.Add(Localize.Tool_InfoSelectLineEndPointStop);

                if ((SelectPoint.Enter.SupportPoints & MarkupPoint.PointType.Normal) != 0)
                    tips.Add(Localize.Tool_InfoSelectLineEndPointNormal);
            }

            return string.Join("\n", tips.ToArray());
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (IsSelectPoint)
                return;

            if (!Tool.Panel.IsHover)
            {
                if (Utilites.OnlyAltIsPressed && Markup is ISupportFillers)
                {
                    Tool.SetMode(ToolModeType.MakeFiller);
                    if (Tool.NextMode is MakeFillerToolMode fillerToolMode)
                        fillerToolMode.DisableByAlt = true;
                }
                else if (Utilites.OnlyShiftIsPressed && Markup is ISupportCrosswalks)
                    Tool.SetMode(ToolModeType.MakeCrosswalk);
            }
        }

        public override void OnMouseDown(Event e)
        {
            if (!IsSelectPoint && IsHoverPoint && Utilites.CtrlIsPressed)
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
                    Tool.DeleteItem(line, OnDelete);
                else if (pointPair.IsStopLine)
                {
                    var style = Tool.GetStyleByModifier<StopLineStyle, StopLineStyle.StopLineType>(StopLineStyle.StopLineType.Solid);
                    var newLine = Tool.Markup.AddStopLine(pointPair, style);
                    Panel.EditLine(newLine);
                }
                else
                {
                    var style = Tool.GetStyleByModifier<RegularLineStyle, RegularLineStyle.RegularLineType>(RegularLineStyle.RegularLineType.Dashed, true);
                    var newLine = Tool.Markup.AddRegularLine(pointPair, style);
                    Panel.EditLine(newLine);
                }

                SelectPoint = null;
                SetTarget();
            }
        }
        private void OnDelete(MarkupLine line)
        {
            Tool.Markup.RemoveLine(line);
            Panel.DeleteLine(line);
        }
        protected override IEnumerable<MarkupPoint> GetTarget(Enter enter, MarkupPoint ignore)
        {
            var allow = enter.Points.Select(i => 1).ToArray();

            if (ignore != null && ignore.Enter == enter)
            {
                if ((Markup.SupportLines & MarkupLine.LineType.Stop) == 0)
                    yield break;

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
            {
                if (Utilites.CtrlIsPressed)
                    HoverPoint.Render(new OverlayData(cameraInfo) { Width = 0.53f });
                else
                    HoverPoint.Render(new OverlayData(cameraInfo) { Color = Colors.Hover, Width = 0.53f });
            }

            RenderPointsOverlay(cameraInfo, !Utilites.CtrlIsPressed);

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
            var color = Tool.Markup.ExistLine(pointPair) ? Colors.Red : Colors.Green;

            NetSegment.CalculateMiddlePoints(bezier.a, bezier.b, bezier.d, bezier.c, true, true, out bezier.b, out bezier.c);
            bezier.RenderBezier(new OverlayData(cameraInfo) { Color = color });
        }
        private void RenderNormalConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            var pointPair = new MarkupPointPair(SelectPoint, HoverPoint);
            var color = Tool.Markup.ExistLine(pointPair) ? Colors.Red : Colors.Purple;

            var lineBezier = new Bezier3()
            {
                a = SelectPoint.Position,
                b = HoverPoint.Position,
                c = SelectPoint.Position,
                d = HoverPoint.Position,
            };
            lineBezier.RenderBezier(new OverlayData(cameraInfo) { Color = color });

            var normal = SelectPoint.Direction.Turn90(false);

            var normalBezier = new Bezier3
            {
                a = SelectPoint.Position + SelectPoint.Direction,
                d = SelectPoint.Position + normal
            };
            normalBezier.b = normalBezier.a + normal / 2;
            normalBezier.c = normalBezier.d + SelectPoint.Direction / 2;
            normalBezier.RenderBezier(new OverlayData(cameraInfo) { Color = color, Width = 2f, Cut = true });
        }
        private void RenderNotConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            var endPosition = SingletonTool<NodeMarkupTool>.Instance.Ray.GetRayPosition(Markup.Position.y, out _);
            new BezierTrajectory(SelectPoint.Position, SelectPoint.Direction, endPosition).Render(new OverlayData(cameraInfo) { Color = Colors.Hover });
        }
    }
}
