using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static ToolBase;

namespace NodeMarkup
{
    public abstract class BaseToolMode
    {
        public abstract ModeType Type { get; }

        protected NodeMarkupTool Tool => NodeMarkupTool.Instance;
        protected NodeMarkupPanel Panel => NodeMarkupPanel.Instance;

        public virtual void Start() 
        {
            Reset();
        }
        public virtual void End() { }
        protected virtual void Reset() { }

        public virtual void OnUpdate() { }
        public virtual string GetToolInfo() => null;

        public virtual void OnGUI(Event e) { }
        public virtual bool ProcessShortcuts(Event e) => false;
        public virtual void OnMouseDown(Event e) { }
        public virtual void OnMouseDrag(Event e) { }
        public virtual void OnPrimaryMouseClicked(Event e) { }
        public virtual void OnSecondaryMouseClicked() { }
        public virtual void RenderOverlay(RenderManager.CameraInfo cameraInfo) { }

        public enum ModeType
        {
            SelectNode,
            MakeLine,
            MakeCrosswalk,
            MakeFiller,
            PanelAction,
            DragPoint,
        }
    }
    public class SelectNodeToolMode : BaseToolMode
    {
        public override ModeType Type => ModeType.SelectNode;
        ushort HoverNodeId { get; set; } = 0;
        bool IsHoverNode => HoverNodeId != 0;

        protected override void Reset()
        {
            HoverNodeId = 0;
        }

        public override void OnUpdate()
        {
            if (NodeMarkupTool.MouseRayValid)
            {
                RaycastInput input = new RaycastInput(NodeMarkupTool.MouseRay, Camera.main.farClipPlane)
                {
                    m_ignoreTerrain = true,
                    m_ignoreNodeFlags = NetNode.Flags.None,
                    m_ignoreSegmentFlags = NetSegment.Flags.All
                };
                input.m_netService.m_itemLayers = (ItemClass.Layer.Default | ItemClass.Layer.MetroTunnels);
                input.m_netService.m_service = ItemClass.Service.Road;

                if (NodeMarkupTool.RayCast(input, out RaycastOutput output))
                {
                    HoverNodeId = output.m_netNode;
                    return;
                }
            }

            HoverNodeId = 0;
        }
        public override string GetToolInfo() => IsHoverNode ? string.Format(Localize.Tool_InfoHoverNode, HoverNodeId) : Localize.Tool_InfoNode;

        public override void OnPrimaryMouseClicked(Event e)
        {
            Tool.SetMarkup(MarkupManager.Get(HoverNodeId));
            Tool.SetDefaultMode();
        }
        public override void OnSecondaryMouseClicked() => Tool.Disable();
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHoverNode)
            {
                var node = Utilities.GetNode(HoverNodeId);
                NodeMarkupTool.RenderCircle(cameraInfo, MarkupColors.Orange, node.m_position, Mathf.Max(6f, node.Info.m_halfWidth * 2f));
            }
        }
    }
    public abstract class MakeItemToolMode : BaseToolMode
    {
        List<MarkupPoint> TargetPoints { get; set; } = new List<MarkupPoint>();

        protected MarkupPoint HoverPoint { get; set; } = null;
        protected MarkupPoint SelectPoint { get; set; } = null;

        protected bool IsHoverPoint => HoverPoint != null;
        protected bool IsSelectPoint => SelectPoint != null;

        protected override void Reset()
        {
            HoverPoint = null;
            SelectPoint = null;
            TargetPoints.Clear();
        }

        public override void OnUpdate()
        {
            if (NodeMarkupTool.MouseRayValid)
            {
                foreach (var point in TargetPoints)
                {
                    if (point.IsHover(NodeMarkupTool.MouseRay))
                    {
                        HoverPoint = point;
                        return;
                    }
                }
            }

            if (IsSelectPoint && SelectPoint.Type == MarkupPoint.PointType.Enter)
            {
                var connectLine = NodeMarkupTool.MouseWorldPosition - SelectPoint.Position;
                if (connectLine.magnitude >= 2 && 135 <= Vector3.Angle(SelectPoint.Direction.XZ(), connectLine.XZ()) && SelectPoint.Enter.TryGetPoint(SelectPoint.Num, MarkupPoint.PointType.Normal, out MarkupPoint normalPoint))
                {
                    HoverPoint = normalPoint;
                    return;
                }
            }

            HoverPoint = null;
        }
        public override string GetToolInfo()
        {
            var pointPair = new MarkupPointPair(SelectPoint, HoverPoint);
            var exist = Tool.EditMarkup.ExistConnection(pointPair);

            if (pointPair.IsStopLine)
                return exist ? Localize.Tool_InfoDeleteStopLine : Localize.Tool_InfoCreateStopLine;
            else if (pointPair.IsCrosswalk)
                return exist ? Localize.Tool_InfoDeleteCrosswalk : Localize.Tool_InfoCreateCrosswalk;
            else if (pointPair.IsNormal)
                return exist ? Localize.Tool_InfoDeleteNormalLine : Localize.Tool_InfoCreateNormalLine;
            else
                return exist ? Localize.Tool_InfoDeleteLine : Localize.Tool_InfoCreateLine;
        }
        public override bool ProcessShortcuts(Event e)
        {
            if (NodeMarkupTool.AddFillerShortcut.IsPressed(e))
            {
                Tool.SetMode(ModeType.MakeFiller);
                if (Tool.Mode is MakeFillerToolMode fillerToolMode)
                    fillerToolMode.DisableByAlt = false;
                return true;
            }
            else if (NodeMarkupTool.DeleteAllShortcut.IsPressed(e))
            {
                Tool.DeleteAllLines();
                return true;
            }
            else if (Panel?.OnShortcut(e) == true)
                return true;
            else
                return false;
        }
        public override void OnPrimaryMouseClicked(Event e)
        {
            SelectPoint = HoverPoint;
            SetTarget(SelectPoint.Type, SelectPoint);
        }
        public override void OnSecondaryMouseClicked()
        {
            if (IsSelectPoint)
            {
                SelectPoint = null;
                SetTarget();
            }
            else
            {
                Tool.SetMode(ModeType.SelectNode);
                Tool.SetMarkup(null);
            }
        }

        #region SET TARGET

        protected void SetTarget(MarkupPoint.PointType pointType = MarkupPoint.PointType.Enter, MarkupPoint ignore = null)
        {
            TargetPoints.Clear();
            foreach (var enter in Tool.EditMarkup.Enters)
            {
                if ((pointType & MarkupPoint.PointType.Enter) == MarkupPoint.PointType.Enter)
                    SetEnterTarget(enter, ignore);

                if ((pointType & MarkupPoint.PointType.Crosswalk) == MarkupPoint.PointType.Crosswalk)
                    SetCrosswalkTarget(enter, ignore);
            }
        }
        private void SetEnterTarget(Enter enter, MarkupPoint ignore)
        {
            if (ignore == null || ignore.Enter != enter)
            {
                TargetPoints.AddRange(enter.Points.Cast<MarkupPoint>());
                return;
            }

            var allow = enter.Points.Select(i => 1).ToArray();
            var ignoreIdx = ignore.Num - 1;
            var leftIdx = ignoreIdx;
            var rightIdx = ignoreIdx;

            foreach (var line in enter.Markup.Lines.Where(l => l.Type == MarkupLine.LineType.Stop && l.Start.Enter == enter))
            {
                var from = Math.Min(line.Start.Num, line.End.Num) - 1;
                var to = Math.Max(line.Start.Num, line.End.Num) - 1;
                if (from < ignore.Num - 1 && ignore.Num - 1 < to)
                    return;
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

            foreach (var point in enter.Points)
            {
                if (allow[point.Num - 1] != 0)
                    TargetPoints.Add(point);
            }
        }
        private void SetCrosswalkTarget(Enter enter, MarkupPoint ignore)
        {
            if (ignore != null && ignore.Enter != enter)
                return;

            var allow = enter.Crosswalks.Select(i => 1).ToArray();
            var bridge = new Dictionary<MarkupPoint, int>();
            foreach (var crosswalk in enter.Crosswalks)
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
                    TargetPoints.Add(point.Key);
            }
        }
        private int Find(int[] allow, int idx, int sign)
        {
            do
                idx += sign;
            while (idx >= 0 && idx < allow.Length && allow[idx] != 2);

            return idx;
        }
        private void SetNotAllow(int[] allow, int idx, int sign)
        {
            idx += sign;
            while (idx >= 0 && idx < allow.Length)
            {
                allow[idx] = 0;
                idx += sign;
            }
        }

        #endregion

        protected void RenderPointsOverlay(RenderManager.CameraInfo cameraInfo)
        {
            foreach (var point in TargetPoints)
                NodeMarkupTool.RenderPointOverlay(cameraInfo, point);
        }
    }
    public class MakeLineToolMode : MakeItemToolMode
    {
        public override ModeType Type => ModeType.MakeLine;

        protected override void Reset()
        {
            base.Reset();
            SetTarget();
        }
        public override string GetToolInfo()
        {
            if (IsSelectPoint)
                return IsHoverPoint ? base.GetToolInfo() : Localize.Tool_InfoSelectLineEndPoint;
            else
                return Localize.Tool_InfoSelectLineStartPoint;
        }
        public override bool ProcessShortcuts(Event e)
        {
            if (IsSelectPoint)
                return false;
            else if (base.ProcessShortcuts(e))
                return true;
            else if (NodeMarkupTool.OnlyAltIsPressed)
            {
                Tool.SetMode(ModeType.MakeFiller);
                if(Tool.Mode is MakeFillerToolMode fillerToolMode)
                    fillerToolMode.DisableByAlt = true;
                return true;
            }
            else if (NodeMarkupTool.OnlyShiftIsPressed)
            {
                Tool.SetMode(ModeType.MakeCrosswalk);
                SetTarget(MarkupPoint.PointType.Crosswalk);
                return true;
            }
            else
                return false;
        }
        public override void OnMouseDown(Event e)
        {
            if (!IsSelectPoint && IsHoverPoint && NodeMarkupTool.CtrlIsPressed)
            {
                Tool.SetMode(ModeType.DragPoint);
                if (Tool.Mode is DragPointToolMode dragPointMode)
                    dragPointMode.DragPoint = HoverPoint;
            }
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

                var lineType = pointPair.IsStopLine ? e.GetStopStyle() : e.GetRegularStyle();
                var newLine = Tool.EditMarkup.ToggleConnection(pointPair, lineType);
                Panel.EditLine(newLine);

                SelectPoint = null;
                SetTarget();
            }
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHoverPoint)
                NodeMarkupTool.RenderPointOverlay(cameraInfo, HoverPoint, MarkupColors.White, 0.5f);

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
            var color = Tool.EditMarkup.ExistConnection(pointPair) ? MarkupColors.Red : MarkupColors.Green;

            NetSegment.CalculateMiddlePoints(bezier.a, bezier.b, bezier.d, bezier.c, true, true, out bezier.b, out bezier.c);
            NodeMarkupTool.RenderBezier(cameraInfo, color, bezier);
        }
        private void RenderNormalConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            var pointPair = new MarkupPointPair(SelectPoint, HoverPoint);
            var color = Tool.EditMarkup.ExistConnection(pointPair) ? MarkupColors.Red : MarkupColors.Blue;

            var lineBezier = new Bezier3()
            {
                a = SelectPoint.Position,
                b = HoverPoint.Position,
                c = SelectPoint.Position,
                d = HoverPoint.Position,
            };
            NodeMarkupTool.RenderBezier(cameraInfo, color, lineBezier);

            var normal = SelectPoint.Direction.Turn90(false);

            var normalBezier = new Bezier3
            {
                a = SelectPoint.Position + SelectPoint.Direction,
                d = SelectPoint.Position + normal
            };
            normalBezier.b = normalBezier.a + normal / 2;
            normalBezier.c = normalBezier.d + SelectPoint.Direction / 2;
            NodeMarkupTool.RenderBezier(cameraInfo, color, normalBezier, 2f, true);
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
            NodeMarkupTool.RenderBezier(cameraInfo, MarkupColors.White, bezier);
        }
    }
    public class MakeCrosswalkToolMode : MakeItemToolMode
    {
        public override ModeType Type => ModeType.MakeCrosswalk;

        protected override void Reset()
        {
            base.Reset();
            SetTarget(MarkupPoint.PointType.Crosswalk);
        }

        public override string GetToolInfo()
        {
            if (IsSelectPoint)
                return IsHoverPoint ? base.GetToolInfo() : Localize.Tool_InfoSelectCrosswalkEndPoint;
            else
                return Localize.Tool_InfoSelectCrosswalkStartPoint;
        }
        public override bool ProcessShortcuts(Event e)
        {
            if (IsSelectPoint)
                return false;
            else if (base.ProcessShortcuts(e))
                return true;
            else if (!NodeMarkupTool.ShiftIsPressed)
            {
                Tool.SetDefaultMode();
                SetTarget();
                return true;
            }
            else
                return false;
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

                var newCrosswalkLine = Tool.EditMarkup.ToggleConnection(pointPair, e.GetCrosswalkStyle()) as MarkupCrosswalkLine;
                Panel.EditCrosswalk(newCrosswalkLine?.Crosswalk);

                SelectPoint = null;
                SetTarget();
            }
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHoverPoint)
                NodeMarkupTool.RenderPointOverlay(cameraInfo, HoverPoint, MarkupColors.White, 0.5f);

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
            var color = Tool.EditMarkup.ExistConnection(pointPair) ? MarkupColors.Red : MarkupColors.Green;

            NodeMarkupTool.RenderBezier(cameraInfo, color, bezier, MarkupCrosswalkPoint.Shift * 2, true);
        }
        private void RenderNotConnectCrosswalkLine(RenderManager.CameraInfo cameraInfo)
        {
            var dir = NodeMarkupTool.MouseWorldPosition - SelectPoint.Position;
            var lenght = dir.magnitude;
            dir.Normalize();
            var bezier = new Line3(SelectPoint.Position, SelectPoint.Position + dir * Mathf.Max(lenght, 1f)).GetBezier();

            NodeMarkupTool.RenderBezier(cameraInfo, MarkupColors.White, bezier, MarkupCrosswalkPoint.Shift * 2, true);
        }
    }
    public class MakeFillerToolMode : BaseToolMode
    {
        public override ModeType Type => ModeType.MakeFiller;

        private MarkupFiller TempFiller { get; set; }
        private PointsSelector<IFillerVertex> FillerPointsSelector { get; set; }

        public bool DisableByAlt { get; set; }

        protected override void Reset()
        {
            TempFiller = new MarkupFiller(Tool.EditMarkup, Style.StyleType.FillerStripe);
            GetFillerPoints();
        }

        public override void OnUpdate() => FillerPointsSelector.OnUpdate();
        public override string GetToolInfo()
        {
            if (FillerPointsSelector.IsHoverPoint)
            {
                if (TempFiller.IsEmpty)
                    return Localize.Tool_InfoFillerClickStart;
                else if (FillerPointsSelector.HoverPoint == TempFiller.First)
                    return Localize.Tool_InfoFillerClickEnd;
                else
                    return Localize.Tool_InfoFillerClickNext;
            }
            else if (TempFiller.IsEmpty)
                return Localize.Tool_InfoFillerSelectStart;
            else
                return Localize.Tool_InfoFillerSelectNext;
        }
        public override bool ProcessShortcuts(Event e)
        {
            if (DisableByAlt && !NodeMarkupTool.AltIsPressed && TempFiller.IsEmpty)
            {
                Tool.SetDefaultMode();
                return true;
            }
            else
                return false;
        }
        public override void OnPrimaryMouseClicked(Event e)
        {
            if (FillerPointsSelector.IsHoverPoint)
            {
                if (TempFiller.Add(FillerPointsSelector.HoverPoint))
                {
                    Tool.EditMarkup.AddFiller(TempFiller);
                    Panel.EditFiller(TempFiller);
                    Tool.SetDefaultMode();
                    return;
                }
                DisableByAlt = false;
                GetFillerPoints();
            }
        }
        public override void OnSecondaryMouseClicked()
        {
            if (TempFiller.IsEmpty)
                Tool.SetDefaultMode();
            else
            {
                TempFiller.Remove();
                GetFillerPoints();
            }
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            RenderFillerLines(cameraInfo);
            RenderFillerConnectLine(cameraInfo);
            FillerPointsSelector.Render(cameraInfo);
        }

        private void RenderFillerLines(RenderManager.CameraInfo cameraInfo)
        {
            var color = FillerPointsSelector.IsHoverPoint && FillerPointsSelector.HoverPoint.Equals(TempFiller.First) ? MarkupColors.Green : MarkupColors.White;
            foreach (var trajectory in TempFiller.Trajectories)
                NodeMarkupTool.RenderTrajectory(cameraInfo, color, trajectory);
        }
        private void RenderFillerConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            if (TempFiller.IsEmpty)
                return;

            if (FillerPointsSelector.IsHoverPoint)
            {
                var linePart = TempFiller.GetFillerLine(TempFiller.Last, FillerPointsSelector.HoverPoint);
                if (linePart.GetTrajectory(out ILineTrajectory trajectory))
                    NodeMarkupTool.RenderTrajectory(cameraInfo, MarkupColors.Green, trajectory);
            }
            else
            {
                var bezier = new Line3(TempFiller.Last.Position, NodeMarkupTool.MouseWorldPosition).GetBezier();
                NodeMarkupTool.RenderBezier(cameraInfo, MarkupColors.White, bezier);
            }
        }

        private void GetFillerPoints() => FillerPointsSelector = new PointsSelector<IFillerVertex>(TempFiller.GetNextСandidates(), MarkupColors.Red);
    }
    public class DragPointToolMode : BaseToolMode
    {
        public override ModeType Type => ModeType.DragPoint;
        public MarkupPoint DragPoint { get; set; } = null;

        protected override void Reset()
        {
            DragPoint = null;
        }
        public override void OnMouseDrag(Event e)
        {
            var normal = DragPoint.Enter.CornerDir.Turn90(true);
            Line2.Intersect(DragPoint.Position.XZ(), (DragPoint.Position + DragPoint.Enter.CornerDir).XZ(), NodeMarkupTool.MouseWorldPosition.XZ(), (NodeMarkupTool.MouseWorldPosition + normal).XZ(), out float offsetChange, out _);
            DragPoint.Offset = (DragPoint.Offset + offsetChange * Mathf.Sin(DragPoint.Enter.CornerAndNormalAngle)).RoundToNearest(0.01f);
            Panel.EditPoint(DragPoint);
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
            NodeMarkupTool.RenderBezier(cameraInfo, MarkupColors.White, bezier, width);
        }
    }
}
