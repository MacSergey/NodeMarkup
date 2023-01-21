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
                tips.Add(Settings.HoldCtrlToMovePoint ? string.Format(Localize.Tool_InfoStartDragPointMode, LocalizeExtension.Ctrl.AddInfoColor()) : Localize.Tool_InfoDragPointMode);
                if ((Marking.Support & Marking.SupportType.Fillers) != 0)
                    tips.Add(string.Format(Localize.Tool_InfoStartCreateFiller, LocalizeExtension.Alt.AddInfoColor()));
                if ((Marking.Support & Marking.SupportType.Croswalks) != 0)
                    tips.Add(string.Format(Localize.Tool_InfoStartCreateCrosswalk, LocalizeExtension.Shift.AddInfoColor()));
            }
            else if (!IsHoverPoint)
            {
                if (SelectPoint.Type == MarkingPoint.PointType.Lane)
                {
                    tips.Add(Localize.Tool_InfoSelectLaneEndPoint);
                }
                else
                {
                    if ((SelectPoint.Marking.SupportLines & LineType.Stop) == 0)
                        tips.Add(Localize.Tool_InfoSelectLineEndPoint);
                    else
                        tips.Add(Localize.Tool_InfoSelectLineEndPointStop);

                    if ((SelectPoint.Enter.SupportPoints & MarkingPoint.PointType.Normal) != 0)
                        tips.Add(Localize.Tool_InfoSelectLineEndPointNormal);
                }
            }
            else
                tips.Add(base.GetToolInfo());

            return string.Join("\n", tips.ToArray());
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (IsSelectPoint)
                return;

            if (!Tool.Panel.IsHover)
            {
                if (Utility.OnlyAltIsPressed && (Marking.Support & Marking.SupportType.Fillers) != 0)
                {
                    Tool.SetMode(ToolModeType.MakeFiller);
                    if (Tool.NextMode is MakeFillerToolMode fillerToolMode)
                        fillerToolMode.DisableByAlt = true;
                }
                else if (Utility.OnlyShiftIsPressed && (Marking.Support & Marking.SupportType.Croswalks) != 0)
                    Tool.SetMode(ToolModeType.MakeCrosswalk);
            }
        }

        public override void OnMouseDrag(Event e)
        {
            if ((!Settings.HoldCtrlToMovePoint || Utility.OnlyCtrlIsPressed) && !IsSelectPoint && IsHoverPoint && HoverPoint.Type == MarkingPoint.PointType.Enter)
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
                var pointPair = new MarkingPointPair(SelectPoint, HoverPoint);

                if (Tool.Marking.TryGetLine(pointPair, out MarkingLine line))
                {
                    if (Utility.OnlyCtrlIsPressed)
                        Panel.SelectLine(line);
                    else
                        Tool.DeleteItem(line, OnDelete);
                }
                else if (pointPair.IsStopLine)
                {
                    var style = Tool.GetStyleByModifier<StopLineStyle, StopLineStyle.StopLineType>(NetworkType.Road, LineType.Stop, StopLineStyle.StopLineType.Solid);
                    var newLine = Tool.Marking.AddStopLine(pointPair, style);
                    Panel.SelectLine(newLine);
                }
                else if (pointPair.IsLane)
                {
                    var style = Tool.GetStyleByModifier<RegularLineStyle, RegularLineStyle.RegularLineType>(pointPair.NetworkType, LineType.Lane, RegularLineStyle.RegularLineType.Prop, true);
                    var newLine = Tool.Marking.AddLaneLine(pointPair, style);
                    Panel.SelectLine(newLine);

                    if (Settings.CreateLaneEdgeLines && pointPair.First is MarkingLanePoint lanePointS && pointPair.Second is MarkingLanePoint lanePointE)
                    {
                        lanePointS.Source.GetPoints(out var leftPointS, out var rightPointS);
                        lanePointE.Source.GetPoints(out var leftPointE, out var rightPointE);

                        var pairA = new MarkingPointPair(leftPointS, rightPointE);
                        if (!Marking.TryGetLine(pairA, out MarkingLine lineA))
                        {
                            lineA = Marking.AddRegularLine(pairA, null);
                            Panel.AddLine(lineA);
                        }

                        var pairB = new MarkingPointPair(leftPointE, rightPointS);
                        if (!Marking.TryGetLine(pairB, out MarkingLine lineB))
                        {
                            lineB = Marking.AddRegularLine(pairB, null);
                            Panel.AddLine(lineB);
                        }
                    }
                }
                else
                {
                    var style = Tool.GetStyleByModifier<RegularLineStyle, RegularLineStyle.RegularLineType>(pointPair.NetworkType, LineType.Regular, RegularLineStyle.RegularLineType.Dashed, true);
                    var newLine = Tool.Marking.AddRegularLine(pointPair, style);
                    Panel.SelectLine(newLine);
                }

                SelectPoint = null;
                SetTarget();
            }
        }
        private void OnDelete(MarkingLine line)
        {
            var fillers = Marking.GetLineFillers(line).ToArray();

            if (line is MarkingCrosswalkLine crosswalkLine)
                Panel.DeleteCrosswalk(crosswalkLine.Crosswalk);
            foreach (var filler in fillers)
                Panel.DeleteFiller(filler);

            Panel.DeleteLine(line);
            Tool.Marking.RemoveLine(line);
        }
        protected override IEnumerable<MarkingPoint> GetTarget(Entrance enter, MarkingPoint ignore)
        {
            var allow = enter.EnterPoints.Select(i => 1).ToArray();

            if (ignore == null)
            {
                foreach (var point in enter.EnterPoints)
                    yield return point;
                if (Marking.EntersCount > 1)
                {
                    foreach (var point in enter.LanePoints)
                        yield return point;
                }
            }
            else if (ignore.Type == MarkingPoint.PointType.Enter)
            {
                if (ignore != null && ignore.Enter == enter)
                {
                    if ((Marking.SupportLines & LineType.Stop) == 0)
                        yield break;

                    var ignoreIdx = ignore.Index - 1;
                    var leftIdx = ignoreIdx;
                    var rightIdx = ignoreIdx;

                    foreach (var line in enter.Marking.Lines.Where(l => l.Type == LineType.Stop && l.Start.Enter == enter))
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

                foreach (var point in enter.EnterPoints)
                {
                    if (allow[point.Index - 1] != 0)
                        yield return point;
                }
            }
            else if (ignore.Type == MarkingPoint.PointType.Lane)
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
            Panel.Render(cameraInfo);

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
                    if (SelectPoint.Type == MarkingPoint.PointType.Normal)
                        RenderNormalConnectLine(cameraInfo);
                    else if (SelectPoint.Type == MarkingPoint.PointType.Lane)
                        RenderLaneConnectionLine(cameraInfo);
                    else
                        RenderRegularConnectLine(cameraInfo);
                }
                else
                {
                    if (SelectPoint.Type == MarkingPoint.PointType.Lane)
                        RenderNotConnectedLane(cameraInfo);
                    else
                        RenderNotConnectLine(cameraInfo);
                }
            }
#if DEBUG
            if (Settings.ShowNodeContour && Tool.Marking is NodeMarking marking)
            {
                foreach (var line in marking.Contour)
                    line.Render(new OverlayData(cameraInfo));
            }
#endif
        }

        private void RenderRegularConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            var startPos = SelectPoint.MarkerPosition;
            var endPos = HoverPoint.MarkerPosition;
            var startDir = HoverPoint.Enter == SelectPoint.Enter ? HoverPoint.MarkerPosition - SelectPoint.MarkerPosition : SelectPoint.Direction;
            var endDir = HoverPoint.Enter == SelectPoint.Enter ? SelectPoint.MarkerPosition - HoverPoint.MarkerPosition : HoverPoint.Direction;
            var smoothStart = SelectPoint.Enter.IsSmooth;
            var smoothEnd = HoverPoint.Enter.IsSmooth;
            var bezier = new BezierTrajectory(startPos, startDir, endPos, endDir, true, smoothStart, smoothEnd);

            var pointPair = new MarkingPointPair(SelectPoint, HoverPoint);
            var color = Tool.Marking.ExistLine(pointPair) ? (Utility.OnlyCtrlIsPressed ? Colors.Yellow : Colors.Red) : Colors.Green;

            bezier.Render(new OverlayData(cameraInfo) { Color = color });
        }
        private void RenderNormalConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            var pointPair = new MarkingPointPair(SelectPoint, HoverPoint);
            var color = Tool.Marking.ExistLine(pointPair) ? (Utility.OnlyCtrlIsPressed ? Colors.Yellow : Colors.Red) : Colors.Purple;

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
            if (SelectPoint is MarkingLanePoint pointA && HoverPoint is MarkingLanePoint pointB)
            {
                var trajectories = new List<ITrajectory>()
                {
                    new StraightTrajectory(pointA.SourcePointA.Position, pointA.SourcePointB.Position),
                    new BezierTrajectory(pointA.SourcePointB.Position, pointA.SourcePointB.Direction, pointB.SourcePointA.Position, pointB.SourcePointA.Direction, false, pointA.Enter.IsSmooth, pointB.Enter.IsSmooth),
                    new StraightTrajectory(pointB.SourcePointA.Position, pointB.SourcePointB.Position),
                    new BezierTrajectory(pointB.SourcePointB.Position, pointB.SourcePointB.Direction, pointA.SourcePointA.Position, pointA.SourcePointA.Direction, false, pointB.Enter.IsSmooth, pointA.Enter.IsSmooth),
                };

                var pointPair = new MarkingPointPair(pointA, pointB);
                var color = Tool.Marking.ExistLine(pointPair) ? (Utility.OnlyCtrlIsPressed ? Colors.Yellow : Colors.Red) : Colors.Green;

                var triangles = Triangulator.TriangulateSimple(trajectories, out var points, minAngle: 5, maxLength: 10f);
                points.RenderArea(triangles, new OverlayData(cameraInfo) { Color = color, AlphaBlend = false });
            }
        }

        private void RenderNotConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            Vector3 endPosition;
            if (Marking is SegmentMarking segmentMarking)
            {
                segmentMarking.Trajectory.GetHitPosition(SingletonTool<IntersectionMarkingTool>.Instance.Ray, out _, out _, out endPosition);
                endPosition = SingletonTool<IntersectionMarkingTool>.Instance.Ray.GetRayPosition(endPosition.y, out _);
            }
            else
                endPosition = SingletonTool<IntersectionMarkingTool>.Instance.Ray.GetRayPosition(Marking.Position.y, out _);

            new BezierTrajectory(SelectPoint.MarkerPosition, SelectPoint.Direction, endPosition).Render(new OverlayData(cameraInfo) { Color = Colors.Hover });
        }
        private void RenderNotConnectedLane(RenderManager.CameraInfo cameraInfo)
        {
            if (SelectPoint is MarkingLanePoint lanePoint)
            {
                var halfWidth = lanePoint.Width * lanePoint.Enter.TranformCoef * 0.5f;

                Vector3 endPosition;
                if (Marking is SegmentMarking segmentMarking)
                {
                    segmentMarking.Trajectory.GetHitPosition(SingletonTool<IntersectionMarkingTool>.Instance.Ray, out _, out _, out endPosition);
                    endPosition = SingletonTool<IntersectionMarkingTool>.Instance.Ray.GetRayPosition(endPosition.y, out _);
                }
                else
                    endPosition = SingletonTool<IntersectionMarkingTool>.Instance.Ray.GetRayPosition(Marking.Position.y, out _);

                if ((lanePoint.Position - endPosition).sqrMagnitude < 4f * halfWidth * halfWidth)
                {
                    var normal = (lanePoint.Position - endPosition).MakeFlatNormalized().Turn90(true);
                    var area = new Quad3()
                    {
                        a = lanePoint.Position + normal * halfWidth,
                        b = lanePoint.Position - normal * halfWidth,
                        c = endPosition - normal * halfWidth,
                        d = endPosition + normal * halfWidth,
                    };

                    area.RenderQuad(new OverlayData(cameraInfo) { Color = Colors.Hover, AlphaBlend = false });
                }
                else
                {
                    var trajectory = new BezierTrajectory(lanePoint.MarkerPosition, lanePoint.Direction, endPosition);

                    var normal = trajectory.EndDirection.MakeFlatNormalized().Turn90(false);
                    var pointA = lanePoint.Marking.Type == MarkingType.Node ? lanePoint.SourcePointA : lanePoint.SourcePointB;
                    var pointB = lanePoint.Marking.Type == MarkingType.Node ? lanePoint.SourcePointB : lanePoint.SourcePointA;

                    var trajectories = new List<ITrajectory>()
                    { 
                        new BezierTrajectory(pointA.Position, pointA.Direction, trajectory.EndPosition + normal * halfWidth, trajectory.EndDirection, false, true, true),
                        new StraightTrajectory(trajectory.EndPosition + normal * halfWidth, trajectory.EndPosition - normal * halfWidth),
                        new BezierTrajectory(trajectory.EndPosition - normal * halfWidth, trajectory.EndDirection, pointB.Position, pointB.Direction, false, true, true),
                        new StraightTrajectory(pointB.Position, pointA.Position),
                    };

                    var triangles = Triangulator.TriangulateSimple(trajectories, out var points, minAngle: 5, maxLength: 10f);
                    points.RenderArea(triangles, new OverlayData(cameraInfo) { Color = Colors.Hover, AlphaBlend = false });
                }
            }
        }
    }
}
