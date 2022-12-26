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
                tips.Add(string.Format(Localize.Tool_InfoStartDragPointMode, LocalizeExtension.Ctrl.AddInfoColor()));
                if ((Markup.Support & Markup.SupportType.Fillers) != 0)
                    tips.Add(string.Format(Localize.Tool_InfoStartCreateFiller, LocalizeExtension.Alt.AddInfoColor()));
                if ((Markup.Support & Markup.SupportType.Croswalks) != 0)
                    tips.Add(string.Format(Localize.Tool_InfoStartCreateCrosswalk, LocalizeExtension.Shift.AddInfoColor()));
            }
            else if (IsHoverPoint)
                tips.Add(base.GetToolInfo());
            else
            {
                if ((SelectPoint.Markup.SupportLines & LineType.Stop) == 0)
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
                if (Utility.OnlyAltIsPressed && (Markup.Support & Markup.SupportType.Fillers) != 0)
                {
                    Tool.SetMode(ToolModeType.MakeFiller);
                    if (Tool.NextMode is MakeFillerToolMode fillerToolMode)
                        fillerToolMode.DisableByAlt = true;
                }
                else if (Utility.OnlyShiftIsPressed && (Markup.Support & Markup.SupportType.Croswalks) != 0)
                    Tool.SetMode(ToolModeType.MakeCrosswalk);
            }
        }

        public override void OnMouseDown(Event e)
        {
            if (!IsSelectPoint && IsHoverPoint && Utility.CtrlIsPressed)
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
                {
                    if (Utility.OnlyCtrlIsPressed)
                        Panel.SelectLine(line);
                    else
                        Tool.DeleteItem(line, OnDelete);
                }
                else if (pointPair.IsStopLine)
                {
                    var style = Tool.GetStyleByModifier<StopLineStyle, StopLineStyle.StopLineType>(NetworkType.Road, LineType.Stop, StopLineStyle.StopLineType.Solid);
                    var newLine = Tool.Markup.AddStopLine(pointPair, style);
                    Panel.SelectLine(newLine);
                }
                else if (pointPair.IsLane)
                {
                    var style = Tool.GetStyleByModifier<RegularLineStyle, RegularLineStyle.RegularLineType>(pointPair.NetworkType, LineType.Lane, RegularLineStyle.RegularLineType.Prop, true);
                    var newLine = Tool.Markup.AddRegularLine(pointPair, style);
                    Panel.SelectLine(newLine);
                }
                else
                {
                    var style = Tool.GetStyleByModifier<RegularLineStyle, RegularLineStyle.RegularLineType>(pointPair.NetworkType, LineType.Regular, RegularLineStyle.RegularLineType.Dashed, true);
                    var newLine = Tool.Markup.AddRegularLine(pointPair, style);
                    Panel.SelectLine(newLine);
                }

                SelectPoint = null;
                SetTarget();
            }
        }
        private void OnDelete(MarkupLine line)
        {
            var fillers = Markup.GetLineFillers(line).ToArray();

            if (line is MarkupCrosswalkLine crosswalkLine)
                Panel.DeleteCrosswalk(crosswalkLine.Crosswalk);
            foreach (var filler in fillers)
                Panel.DeleteFiller(filler);

            Panel.DeleteLine(line);
            Tool.Markup.RemoveLine(line);
        }
        protected override IEnumerable<MarkupPoint> GetTarget(Enter enter, MarkupPoint ignore)
        {
            var allow = enter.Points.Select(i => 1).ToArray();

            if (ignore == null)
            {
                foreach (var point in enter.Points)
                    yield return point;
                if (Markup.EntersCount > 1)
                {
                    foreach (var point in enter.LanePoints)
                        yield return point;
                }
            }
            else if (ignore.Type == MarkupPoint.PointType.Enter)
            {
                if (ignore != null && ignore.Enter == enter)
                {
                    if ((Markup.SupportLines & LineType.Stop) == 0)
                        yield break;

                    var ignoreIdx = ignore.Index - 1;
                    var leftIdx = ignoreIdx;
                    var rightIdx = ignoreIdx;

                    foreach (var line in enter.Markup.Lines.Where(l => l.Type == LineType.Stop && l.Start.Enter == enter))
                    {
                        var from = Math.Min(line.Start.Index, line.End.Index) - 1;
                        var to = Math.Max(line.Start.Index, line.End.Index) - 1;
                        if (from < ignore.Index - 1 && ignore.Index - 1 < to)
                            yield break;

                        allow[from] = 2;
                        allow[to] = 2;

                        for (var i = from + 1; i <= to - 1; i += 1)
                            allow[i] = 0;

                        if (line.ContainsPoint(ignore))
                        {
                            var otherIdx = line.PointPair.GetOther(ignore).Index - 1;
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
                    if (allow[point.Index - 1] != 0)
                        yield return point;
                }
            }
            else if (ignore.Type == MarkupPoint.PointType.Lane)
            {
                if (enter != ignore.Enter)
                {
                    foreach (var point in enter.LanePoints)
                        yield return point;
                }
            }
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHoverPoint)
            {
                if (Utility.CtrlIsPressed)
                    HoverPoint.Render(new OverlayData(cameraInfo) { Width = 0.53f });
                else
                    HoverPoint.Render(new OverlayData(cameraInfo) { Color = Colors.Hover, Width = 0.53f });
            }

            RenderPointsOverlay(cameraInfo, !Utility.CtrlIsPressed);

            if (IsSelectPoint)
            {
                if (IsHoverPoint)
                {
                    if (SelectPoint.Type == MarkupPoint.PointType.Normal)
                        RenderNormalConnectLine(cameraInfo);
                    else if (SelectPoint.Type == MarkupPoint.PointType.Lane)
                        RenderLaneConnectionLine(cameraInfo);
                    else
                        RenderRegularConnectLine(cameraInfo);
                }
                else
                {
                    if (SelectPoint.Type == MarkupPoint.PointType.Lane)
                        RenderNotConnectedLane(cameraInfo);
                    else
                        RenderNotConnectLine(cameraInfo);
                }
            }

            Panel.Render(cameraInfo);
#if DEBUG
            if (Settings.ShowNodeContour && Tool.Markup is Manager.NodeMarkup markup)
            {
                foreach (var line in markup.Contour)
                    line.Render(new OverlayData(cameraInfo));
            }
#endif
        }

        private void RenderRegularConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            var bezier = new Bezier3()
            {
                a = SelectPoint.MarkerPosition,
                b = HoverPoint.Enter == SelectPoint.Enter ? HoverPoint.MarkerPosition - SelectPoint.MarkerPosition : SelectPoint.Direction,
                c = HoverPoint.Enter == SelectPoint.Enter ? SelectPoint.MarkerPosition - HoverPoint.MarkerPosition : HoverPoint.Direction,
                d = HoverPoint.MarkerPosition,
            };

            var pointPair = new MarkupPointPair(SelectPoint, HoverPoint);
            var color = Tool.Markup.ExistLine(pointPair) ? (Utility.OnlyCtrlIsPressed ? Colors.Yellow : Colors.Red) : Colors.Green;

            NetSegment.CalculateMiddlePoints(bezier.a, bezier.b, bezier.d, bezier.c, true, true, out bezier.b, out bezier.c);
            bezier.RenderBezier(new OverlayData(cameraInfo) { Color = color });
        }
        private void RenderNormalConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            var pointPair = new MarkupPointPair(SelectPoint, HoverPoint);
            var color = Tool.Markup.ExistLine(pointPair) ? (Utility.OnlyCtrlIsPressed ? Colors.Yellow : Colors.Red) : Colors.Purple;

            var lineBezier = new Bezier3()
            {
                a = SelectPoint.MarkerPosition,
                b = HoverPoint.MarkerPosition,
                c = SelectPoint.MarkerPosition,
                d = HoverPoint.MarkerPosition,
            };
            lineBezier.RenderBezier(new OverlayData(cameraInfo) { Color = color });

            var normal = SelectPoint.Direction.Turn90(false);

            var normalBezier = new Bezier3
            {
                a = SelectPoint.MarkerPosition + SelectPoint.Direction,
                d = SelectPoint.MarkerPosition + normal
            };
            normalBezier.b = normalBezier.a + normal / 2;
            normalBezier.c = normalBezier.d + SelectPoint.Direction / 2;
            normalBezier.RenderBezier(new OverlayData(cameraInfo) { Color = color, Width = 2f, Cut = true });
        }
        private void RenderLaneConnectionLine(RenderManager.CameraInfo cameraInfo)
        {
            if (SelectPoint is MarkupLanePoint pointA && HoverPoint is MarkupLanePoint pointB)
            {
                var trajectory = new BezierTrajectory(pointA.MarkerPosition, pointA.Direction, pointB.MarkerPosition, pointB.Direction);

                var halfWidthA = pointA.Width * 0.5f;
                var halfWidthB = pointB.Width * 0.5f;
                var startNormal = trajectory.StartDirection.MakeFlatNormalized().Turn90(true);
                var endNormal = trajectory.EndDirection.MakeFlatNormalized().Turn90(false);

                var trajectories = new List<ITrajectory>()
                {
                    new BezierTrajectory(trajectory.StartPosition + startNormal * halfWidthA, trajectory.StartDirection, trajectory.EndPosition + endNormal * halfWidthB, trajectory.EndDirection),
                    new StraightTrajectory(trajectory.EndPosition + endNormal * halfWidthB, trajectory.EndPosition - endNormal * halfWidthB),
                    new BezierTrajectory(trajectory.EndPosition - endNormal * halfWidthB, trajectory.EndDirection, trajectory.StartPosition - startNormal * halfWidthA, trajectory.StartDirection),
                    new StraightTrajectory(trajectory.StartPosition - startNormal * halfWidthA, trajectory.StartPosition + startNormal * halfWidthA),
                };

                var pointPair = new MarkupPointPair(pointA, pointB);
                var color = Tool.Markup.ExistLine(pointPair) ? (Utility.OnlyCtrlIsPressed ? Colors.Yellow : Colors.Red) : Colors.Green;

                var triangles = Triangulator.TriangulateSimple(trajectories, out var points);
                points.RenderArea(triangles, new OverlayData(cameraInfo) { Color = color, AlphaBlend = false });
            }
        }

        private void RenderNotConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            var endPosition = SingletonTool<NodeMarkupTool>.Instance.Ray.GetRayPosition(Markup.Position.y, out _);
            new BezierTrajectory(SelectPoint.MarkerPosition, SelectPoint.Direction, endPosition).Render(new OverlayData(cameraInfo) { Color = Colors.Hover });
        }
        private void RenderNotConnectedLane(RenderManager.CameraInfo cameraInfo)
        {
            if (SelectPoint is MarkupLanePoint lanePoint)
            {
                var halfWidth = lanePoint.Width * 0.5f;

                var endPosition = SingletonTool<NodeMarkupTool>.Instance.Ray.GetRayPosition(Markup.Position.y, out _);
                if ((lanePoint.MarkerPosition - endPosition).sqrMagnitude < 4f * halfWidth * halfWidth)
                {
                    var normal = (lanePoint.MarkerPosition - endPosition).MakeFlatNormalized().Turn90(true);
                    var area = new Quad3()
                    {
                        a = lanePoint.MarkerPosition + normal * halfWidth,
                        b = lanePoint.MarkerPosition - normal * halfWidth,
                        c = endPosition - normal * halfWidth,
                        d = endPosition + normal * halfWidth,
                    };

                    area.RenderQuad(new OverlayData(cameraInfo) { Color = Colors.Hover, AlphaBlend = false });
                }
                else
                {
                    var trajectory = new BezierTrajectory(lanePoint.MarkerPosition, lanePoint.Direction, endPosition);

                    var startNormal = trajectory.StartDirection.MakeFlatNormalized().Turn90(true);
                    var endNormal = trajectory.EndDirection.MakeFlatNormalized().Turn90(false);

                    var trajectories = new List<ITrajectory>()
                    {
                        new BezierTrajectory(trajectory.StartPosition + startNormal * halfWidth, trajectory.StartDirection, trajectory.EndPosition + endNormal * halfWidth, trajectory.EndDirection),
                        new StraightTrajectory(trajectory.EndPosition + endNormal * halfWidth, trajectory.EndPosition - endNormal * halfWidth),
                        new BezierTrajectory(trajectory.EndPosition - endNormal * halfWidth, trajectory.EndDirection, trajectory.StartPosition - startNormal * halfWidth, trajectory.StartDirection),
                        new StraightTrajectory(trajectory.StartPosition - startNormal * halfWidth, trajectory.StartPosition + startNormal * halfWidth),
                    };

                    var triangles = Triangulator.TriangulateSimple(trajectories, out var points);
                    points.RenderArea(triangles, new OverlayData(cameraInfo) { Color = Colors.Hover, AlphaBlend = false });
                }
            }
        }
    }
}
