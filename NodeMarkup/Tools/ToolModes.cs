using ColossalFramework.Math;
using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using static ToolBase;

namespace NodeMarkup
{
    public abstract class BaseToolMode
    {
        public abstract ModeType Type { get; }

        protected NodeMarkupTool Tool => NodeMarkupTool.Instance;
        protected Markup Markup => Tool.Markup;
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

        protected string GetCreateToolTip<StyleType>(string text)
            where StyleType : Enum
        {
            var modifiers = GetStylesModifier<StyleType>().ToArray();
            return modifiers.Any() ? $"{text}:\n{string.Join("\n", modifiers)}" : text;
        }
        protected IEnumerable<string> GetStylesModifier<StyleType>()
            where StyleType : Enum
        {
            foreach (var style in Utilities.GetEnumValues<StyleType>())
            {
                var general = (Style.StyleType)(object)style;
                var modifier = (StyleModifier)NodeMarkupTool.StylesModifier[general].value;
                if (modifier != StyleModifier.NotSet)
                    yield return $"{general.Description()} - {modifier.Description()}";
            }
        }

        public enum ModeType
        {
            SelectNode,
            MakeLine,
            MakeCrosswalk,
            MakeFiller,
            PanelAction,
            PasteMarkup,
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
            if (IsHoverNode)
            {
                Tool.SetMarkup(MarkupManager.Get(HoverNodeId));
                Tool.SetDefaultMode();
            }
        }
        public override void OnSecondaryMouseClicked() => Tool.Disable();
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHoverNode)
            {
                var node = Utilities.GetNode(HoverNodeId);
                NodeMarkupTool.RenderCircle(cameraInfo, Colors.Orange, node.m_position, Mathf.Max(6f, node.Info.m_halfWidth * 2f));
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
            var exist = Tool.Markup.ExistConnection(pointPair);

            if (pointPair.IsStopLine)
                return exist ? Localize.Tool_InfoDeleteStopLine : GetCreateToolTip<StopLineStyle.StopLineType>(Localize.Tool_InfoCreateStopLine);
            else if (pointPair.IsCrosswalk)
                return exist ? Localize.Tool_InfoDeleteCrosswalk : GetCreateToolTip<CrosswalkStyle.CrosswalkType>(Localize.Tool_InfoCreateCrosswalk);
            else if (pointPair.IsNormal)
                return exist ? Localize.Tool_InfoDeleteNormalLine : GetCreateToolTip<RegularLineStyle.RegularLineType>(Localize.Tool_InfoCreateNormalLine);
            else
                return exist ? Localize.Tool_InfoDeleteLine : GetCreateToolTip<RegularLineStyle.RegularLineType>(Localize.Tool_InfoCreateLine);
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

            if (NodeMarkupTool.DeleteAllShortcut.IsPressed(e))
            {
                Tool.DeleteAllMarking();
                return true;
            }
            if (NodeMarkupTool.ResetOffsetsShortcut.IsPressed(e))
            {
                Tool.ResetAllOffsets();
                return true;
            }
            if (NodeMarkupTool.CopyMarkingShortcut.IsPressed(e))
            {
                Tool.CopyMarkup();
                return true;
            }
            if (NodeMarkupTool.PasteMarkingShortcut.IsPressed(e))
            {
                Tool.PasteMarkup();
                return true;
            }

            return Panel?.OnShortcut(e) == true;
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
            foreach (var enter in Tool.Markup.Enters)
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
                if (Tool.Mode is MakeFillerToolMode fillerToolMode)
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

                if (Tool.Markup.TryGetLine(pointPair, out MarkupLine line))
                    Tool.DeleteItem(line, () => Tool.Markup.RemoveConnect(line));
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
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHoverPoint)
                NodeMarkupTool.RenderPointOverlay(cameraInfo, HoverPoint, Colors.White, 0.5f);

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
            NodeMarkupTool.RenderBezier(cameraInfo, color, bezier);
        }
        private void RenderNormalConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            var pointPair = new MarkupPointPair(SelectPoint, HoverPoint);
            var color = Tool.Markup.ExistConnection(pointPair) ? Colors.Red : Colors.Blue;

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
            NodeMarkupTool.RenderBezier(cameraInfo, Colors.White, bezier);
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

                if (Tool.Markup.TryGetLine(pointPair, out MarkupLine line))
                    Tool.DeleteItem(line, () => Tool.Markup.RemoveConnect(line));
                else
                {
                    var newCrosswalkLine = Tool.Markup.AddConnection(pointPair, NodeMarkupTool.GetStyle(CrosswalkStyle.CrosswalkType.Zebra)) as MarkupCrosswalkLine;
                    Panel.EditCrosswalk(newCrosswalkLine?.Crosswalk);
                }

                SelectPoint = null;
                SetTarget();
            }
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHoverPoint)
                NodeMarkupTool.RenderPointOverlay(cameraInfo, HoverPoint, Colors.White, 0.5f);

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
            var color = Tool.Markup.ExistConnection(pointPair) ? Colors.Red : Colors.Green;

            NodeMarkupTool.RenderBezier(cameraInfo, color, bezier, MarkupCrosswalkPoint.Shift * 2, true);
        }
        private void RenderNotConnectCrosswalkLine(RenderManager.CameraInfo cameraInfo)
        {
            var dir = NodeMarkupTool.MouseWorldPosition - SelectPoint.Position;
            var lenght = dir.magnitude;
            dir.Normalize();
            var bezier = new Line3(SelectPoint.Position, SelectPoint.Position + dir * Mathf.Max(lenght, 1f)).GetBezier();

            NodeMarkupTool.RenderBezier(cameraInfo, Colors.White, bezier, MarkupCrosswalkPoint.Shift * 2, true);
        }
    }
    public class MakeFillerToolMode : BaseToolMode
    {
        public override ModeType Type => ModeType.MakeFiller;

        private FillerContour Contour { get; set; }
        private PointsSelector<IFillerVertex> FillerPointsSelector { get; set; }

        public bool DisableByAlt { get; set; }

        protected override void Reset()
        {
            Contour = new FillerContour(Tool.Markup);
            GetFillerPoints();
        }

        public override void OnUpdate() => FillerPointsSelector.OnUpdate();
        public override string GetToolInfo()
        {
            if (FillerPointsSelector.IsHoverPoint)
            {
                if (Contour.IsEmpty)
                    return Localize.Tool_InfoFillerClickStart;
                else if (FillerPointsSelector.HoverPoint == Contour.First)
                    return GetCreateToolTip<FillerStyle.FillerType>(Localize.Tool_InfoFillerClickEnd);
                else
                    return Localize.Tool_InfoFillerClickNext;
            }
            else if (Contour.IsEmpty)
                return Localize.Tool_InfoFillerSelectStart;
            else
                return Localize.Tool_InfoFillerSelectNext;
        }
        public override bool ProcessShortcuts(Event e)
        {
            if (DisableByAlt && !NodeMarkupTool.AltIsPressed && Contour.IsEmpty)
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
                if (Contour.Add(FillerPointsSelector.HoverPoint))
                {
                    var filler = new MarkupFiller(Contour, NodeMarkupTool.GetStyle(FillerStyle.FillerType.Stripe));
                    Tool.Markup.AddFiller(filler);
                    Panel.EditFiller(filler);
                    Tool.SetDefaultMode();
                    return;
                }
                DisableByAlt = false;
                GetFillerPoints();
            }
        }
        public override void OnSecondaryMouseClicked()
        {
            if (Contour.IsEmpty)
                Tool.SetDefaultMode();
            else
            {
                Contour.Remove();
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
            var color = FillerPointsSelector.IsHoverPoint && FillerPointsSelector.HoverPoint.Equals(Contour.First) ? Colors.Green : Colors.White;
            foreach (var trajectory in Contour.Trajectories)
                NodeMarkupTool.RenderTrajectory(cameraInfo, color, trajectory);
        }
        private void RenderFillerConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            if (Contour.IsEmpty)
                return;

            if (FillerPointsSelector.IsHoverPoint)
            {
                var linePart = Contour.GetFillerLine(Contour.Last, FillerPointsSelector.HoverPoint);
                if (linePart.GetTrajectory(out ILineTrajectory trajectory))
                    NodeMarkupTool.RenderTrajectory(cameraInfo, Colors.Green, trajectory);
            }
            else
            {
                var bezier = new Line3(Contour.Last.Position, NodeMarkupTool.MouseWorldPosition).GetBezier();
                NodeMarkupTool.RenderBezier(cameraInfo, Colors.White, bezier);
            }
        }

        private void GetFillerPoints() => FillerPointsSelector = new PointsSelector<IFillerVertex>(Contour.GetNextСandidates(), Colors.Red);
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
            NodeMarkupTool.RenderBezier(cameraInfo, Colors.White, bezier, width);
        }
    }
    public class PasteMarkupToolMode : BaseToolMode
    {
        public override ModeType Type => ModeType.PasteMarkup;
        public override void OnSecondaryMouseClicked() => Tool.SetDefaultMode();
        private MarkupBuffer Buffer => Tool.MarkupBuffer;
        private bool IsMirror { get; set; }

        private XElement Backup { get; set; }

        private GUIButton TurnLeft { get; }
        private GUIButton Flip { get; }
        private GUIButton TurnRight { get; }

        private Vector3 Centre { get; set; }
        private float Radius { get; set; }

        private Source[] Sources { get; set; }
        private Target[] Targets { get; set; }
        private Basket BasketItem { get; }

        private Source HoverSource { get; set; }
        private bool IsHoverSource => HoverSource != null;

        private Source SelectedSource { get; set; }
        private bool IsSelectedSource => SelectedSource != null;

        private Target HoverTarget { get; set; }
        private bool IsHoverTarget => HoverTarget != null;

        private Target[] VisibleTargets { get; set; }

        public static UITextureAtlas ButtonAtlas { get; } = GetButtonsIcons();
        private static UITextureAtlas GetButtonsIcons()
        {
            var spriteNames = new string[]
            {
                nameof(TurnLeft),
                nameof(Flip),
                nameof(TurnRight),
            };

            var atlas = TextureUtil.GetAtlas(nameof(PasteMarkupToolMode));
            if (atlas == UIView.GetAView().defaultAtlas)
                atlas = TextureUtil.CreateTextureAtlas("PasteButtons.png", nameof(PasteMarkupToolMode), 50, 50, spriteNames, new RectOffset(0, 0, 0, 0));

            return atlas;
        }
        public PasteMarkupToolMode()
        {
            TurnLeft = new GUIButton(1, 3, ButtonAtlas.texture, ButtonAtlas[nameof(TurnLeft)].region);
            TurnLeft.OnClick += () =>
            {
                Transform((t) => t == null ? null : Targets[t.Num.NextIndex(Markup.Enters.Count())]);
                Paste();
            };

            Flip = new GUIButton(2, 3, ButtonAtlas.texture, ButtonAtlas[nameof(Flip)].region);
            Flip.OnClick += () =>
            {
                Transform((t) => t == null ? null : Targets[Markup.Enters.Count() - t.Num - 1]);
                IsMirror = !IsMirror;
                Paste();
            };

            TurnRight = new GUIButton(3, 3, ButtonAtlas.texture, ButtonAtlas[nameof(TurnRight)].region);
            TurnRight.OnClick += () =>
            {
                Transform((t) => t == null ? null : Targets[t.Num.PrevIndex(Markup.Enters.Count())]);
                Paste();
            };

            BasketItem = new Basket(this);
        }
        protected override void Reset()
        {
            UpdateCentreAndRadius();

            Targets = Markup.Enters.Select((e, i) => new Target(this, e, i)).ToArray();
            Sources = Tool.MarkupBuffer.Enters.Select((e, i) => new Source(this, e, i)).ToArray();

            var min = Math.Min(Targets.Length, Sources.Length);
            for (var i = 0; i < min; i += 1)
                Sources[i].Target = Targets[i];

            IsMirror = false;

            HoverSource = null;
            SelectedSource = null;
            HoverTarget = null;
            VisibleTargets = Targets.ToArray();

            Backup = Markup.ToXml();
            Paste();
        }
        public override void OnMouseDown(Event e)
        {
            if (IsHoverSource)
            {
                SelectedSource = HoverSource;
                VisibleTargets = GetVisibleTargets(SelectedSource).ToArray();
            }
        }
        public override void OnPrimaryMouseClicked(Event e)
        {
            if (IsSelectedSource)
            {
                if (IsHoverTarget)
                {
                    foreach (var source in Sources)
                    {
                        if (source.Target == HoverTarget)
                            source.Target = null;
                    }

                    SelectedSource.Target = HoverTarget;
                }
                else
                    SelectedSource.Target = null;

                SelectedSource = null;
                VisibleTargets = Targets.ToArray();

                Paste();
            }
            else
            {
                var mouse = GetMouse();

                TurnLeft.CheckClick(mouse);
                Flip.CheckClick(mouse);
                TurnRight.CheckClick(mouse);
            }
        }
        private void Transform(Func<Target, Target> func)
        {
            for (var i = 0; i < Sources.Length; i += 1)
                Sources[i].Target = func(Sources[i].Target);
        }
        public override void OnUpdate()
        {
            BasketItem.Update();
            foreach (var source in Sources)
                source.Update();
            GetHoverSource();
            GetHoverTarget();
        }
        public void GetHoverSource()
        {
            if (NodeMarkupTool.MouseRayValid)
            {
                foreach (var source in Sources)
                {
                    if (source.IsHover(NodeMarkupTool.MouseRay))
                    {
                        HoverSource = source;
                        return;
                    }
                }
            }

            HoverSource = null;
        }
        public void GetHoverTarget()
        {
            if (NodeMarkupTool.MouseRayValid)
            {
                foreach (var target in VisibleTargets)
                {
                    if (target.IsHover(NodeMarkupTool.MouseRay))
                    {
                        HoverTarget = target;
                        return;
                    }
                }
            }

            HoverTarget = null;
        }
        public override string GetToolInfo()
        {
            if (IsSelectedSource)
                return Localize.Tool_InfoPasteDrop;
            else
            {
                var mouse = GetMouse();

                if (TurnLeft.CheckHover(mouse))
                    return Localize.Tool_InfoTurnСounterClockwise;
                else if (Flip.CheckHover(mouse))
                    return Localize.Tool_InfoChangeOrder;
                else if (TurnRight.CheckHover(mouse))
                    return Localize.Tool_InfoTurnClockwise;
                else
                    return Localize.Tool_InfoPasteDrag;
            }
        }
        public override void OnGUI(Event e)
        {
            var uiView = UIView.GetAView();
            var screenPos = uiView.WorldPointToGUI(Camera.main, Centre) * uiView.inputScale;

            TurnLeft.Update(screenPos);
            Flip.Update(screenPos);
            TurnRight.Update(screenPos);

            TurnLeft.OnGUI(e);
            Flip.OnGUI(e);
            TurnRight.OnGUI(e);

        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            NodeMarkupTool.RenderCircle(cameraInfo, Colors.White, Centre, Radius * 2);
            BasketItem.Render(cameraInfo);

            foreach (var target in Targets)
                target.Render(cameraInfo);

            if (IsHoverSource && !IsSelectedSource)
                HoverSource.RenderHover(cameraInfo);

            foreach (var source in Sources)
            {
                if (!IsSelectedSource || SelectedSource == source || (source.Target != null && source.Target != HoverTarget))
                    source.Render(cameraInfo);
            }
        }

        private void Paste()
        {
            Markup.Clear();
            var map = new ObjectsMap(IsMirror);

            foreach (var enter in Tool.MarkupBuffer.Enters)
                map[new ObjectId() { Segment = enter }] = new ObjectId() { Segment = 0 };

            foreach (var source in Sources)
            {
                if (source.Target != null)
                {
                    map[new ObjectId() { Segment = source.Enter }] = new ObjectId() { Segment = source.Target.Enter.Id };

                    if (IsMirror)
                        map.AddMirrorEnter(source.Target.Enter);
                }
            }

            Markup.FromXml(Mod.Version, Buffer.Data, map);
            Panel.UpdatePanel();
        }

        private void UpdateCentreAndRadius()
        {
            var points = Markup.Enters.Where(e => e.Position != null).SelectMany(e => new Vector3[] { e.LeftSide, e.RightSide }).ToArray();

            if (points.Length == 0)
            {
                Centre = Markup.Position;
                Radius = Markup.Radius;
                return;
            }

            var centre = Markup.Position;
            var radius = 1000f;

            for (var i = 0; i < points.Length; i += 1)
            {
                for (var j = i + 1; j < points.Length; j += 1)
                {
                    GetCircle2Points(points, i, j, ref centre, ref radius);

                    for (var k = j + 1; k < points.Length; k += 1)
                        GetCircle3Points(points, i, j, k, ref centre, ref radius);
                }
            }

            Centre = centre;
            Radius = radius + Target.Size / 2;
        }
        private void GetCircle2Points(Vector3[] points, int i, int j, ref Vector3 centre, ref float radius)
        {
            var newCentre = (points[i] + points[j]) / 2;
            var newRadius = (points[i] - points[j]).magnitude / 2;

            if (newRadius >= radius)
                return;

            if (AllPointsInCircle(points, newCentre, newRadius, i, j))
            {
                centre = newCentre;
                radius = newRadius;
            }
        }
        private void GetCircle3Points(Vector3[] points, int i, int j, int k, ref Vector3 centre, ref float radius)
        {
            var pos1 = (points[i] + points[j]) / 2;
            var pos2 = (points[j] + points[k]) / 2;

            var dir1 = (points[i] - points[j]).Turn90(true).normalized;
            var dir2 = (points[j] - points[k]).Turn90(true).normalized;

            Line2.Intersect(pos1.XZ(), (pos1 + dir1).XZ(), pos2.XZ(), (pos2 + dir2).XZ(), out float p, out _);
            var newCentre = pos1 + dir1 * p;
            var newRadius = (newCentre - points[i]).magnitude;

            if (newRadius >= radius)
                return;

            if (AllPointsInCircle(points, newCentre, newRadius, i, j, k))
            {
                centre = newCentre;
                radius = newRadius;
            }
        }
        private bool AllPointsInCircle(Vector3[] points, Vector3 centre, float radius, params int[] ignore)
        {
            for (var i = 0; i < points.Length; i += 1)
            {
                if (ignore.Any(j => j == i))
                    continue;

                if ((centre - points[i]).magnitude > radius)
                    return false;
            }

            return true;
        }
        private Bounds GetTargetPosition(Enter enter)
        {
            var dir = (enter.Position.Value - Markup.Position).normalized;
            var normal = dir.Turn90(true);

            Line2.Intersect(Centre.XZ(), (Centre + normal).XZ(), Markup.Position.XZ(), (Markup.Position + dir).XZ(), out float p, out _);
            var point = Centre + normal * p;
            var distance = Mathf.Sqrt(Mathf.Pow(Radius, 2) - Mathf.Pow(Math.Abs(p), 2));

            return new Bounds(point + dir * distance, Vector3.one * Target.Size);
        }
        private IEnumerable<Target> GetVisibleTargets(Source source)
        {
            var a = Get(s => s.PrevIndex(Sources.Length)) ?? Targets.First();
            var b = Get(s => s.NextIndex(Sources.Length)) ?? Targets.Last();

            yield return a;
            for (var target = Targets[a.Num.NextIndex(Targets.Length)]; target != b; target = Targets[target.Num.NextIndex(Targets.Length)])
                yield return target;
            if (b != a)
                yield return b;

            Target Get(Func<int, int> func)
            {
                var i = func(source.Num);
                for (; i != source.Num && Sources[i].Target == null; i = func(i)) { }
                return Sources[i].Target;
            }
        }
        private Vector2 GetMouse()
        {
            var uiView = UIView.GetAView();
            return uiView.ScreenPointToGUI(NodeMarkupTool.MousePosition / uiView.inputScale) * uiView.inputScale;
        }

        private class Target
        {
            public static float Size => 3f;

            private PasteMarkupToolMode ToolMode { get; }
            private Bounds Bounds { get; set; }
            public Vector3 Position
            {
                get => Bounds.center;
                private set => Bounds = new Bounds(value, Vector3.one * Size);
            }
            public Enter Enter { get; }
            public int Num { get; }

            public Target(PasteMarkupToolMode toolMode, Enter enter, int num)
            {
                ToolMode = toolMode;
                Enter = enter;
                Num = num;

                var dir = (Enter.Position.Value - ToolMode.Markup.Position).normalized;
                var normal = dir.Turn90(true);

                Line2.Intersect(ToolMode.Centre.XZ(), (ToolMode.Centre + normal).XZ(), ToolMode.Markup.Position.XZ(), (ToolMode.Markup.Position + dir).XZ(), out float p, out _);
                var point = ToolMode.Centre + normal * p;
                var distance = Mathf.Sqrt(Mathf.Pow(ToolMode.Radius, 2) - Mathf.Pow(Math.Abs(p), 2));
                Position = point + dir * distance;
            }

            public bool IsHover(Ray ray) => Bounds.IntersectRay(ray);

            public void Render(RenderManager.CameraInfo cameraInfo)
            {
                if (ToolMode.VisibleTargets.Contains(this))
                {
                    NodeMarkupTool.RenderCircle(cameraInfo, Colors.White, Position, Size, false);
                    if (ToolMode.IsSelectedSource)
                    {
                        if (ToolMode.HoverTarget == this && ToolMode.SelectedSource.Target != this)
                            NodeMarkupTool.RenderCircle(cameraInfo, Colors.Green, Position, Size + 0.43f);
                        else if (ToolMode.HoverTarget != this && ToolMode.SelectedSource.Target == this)
                            NodeMarkupTool.RenderCircle(cameraInfo, Colors.Red, Position, Size + 0.43f);
                    }
                }
                else
                    NodeMarkupTool.RenderCircle(cameraInfo, new Color32(192, 192, 192, 255), Position, Size, false);
            }
        }

        private class Source
        {
            public static float Size => 1.5f;

            private PasteMarkupToolMode ToolMode { get; }
            private Bounds Bounds { get; set; }
            public Vector3 Position
            {
                get => Bounds.center;
                set => Bounds = new Bounds(value, Vector3.one * Size);
            }
            public ushort Enter { get; }
            public int Num { get; }
            public Target Target { get; set; }

            public Source(PasteMarkupToolMode toolMode, ushort enter, int num)
            {
                ToolMode = toolMode;
                Enter = enter;
                Num = num;
            }
            public bool IsHover(Ray ray) => Bounds.IntersectRay(ray);

            public void Update()
            {
                if (Target == null)
                {
                    var i = ToolMode.Sources.Take(Num).Count(s => s.Target == null);
                    Position = ToolMode.BasketItem.Position + ToolMode.BasketItem.Direction * ((Target.Size * (i + 1) + Size * i - ToolMode.BasketItem.Width) / 2);
                }
                else
                    Position = Target.Position;
            }

            public void Render(RenderManager.CameraInfo cameraInfo)
            {
                var position = ToolMode.SelectedSource == this ? (ToolMode.IsHoverTarget ? ToolMode.HoverTarget.Position : NodeMarkupTool.MouseWorldPosition) : Position;
                NodeMarkupTool.RenderCircle(cameraInfo, Colors.GetOverlayColor(Num, 255), position, Size - 0.4f);
                NodeMarkupTool.RenderCircle(cameraInfo, Colors.GetOverlayColor(Num, 255), position, Size);

            }
            public void RenderHover(RenderManager.CameraInfo cameraInfo) => NodeMarkupTool.RenderCircle(cameraInfo, Colors.White, Position, Size - 0.9f);
        }
        private class Basket
        {
            private PasteMarkupToolMode ToolMode { get; }

            public Vector3 Position { get; private set; }
            public Vector3 Direction { get; private set; }
            public float Width { get; private set; }

            public int Count { get; set; }
            public bool IsEmpty => Count == 0;

            public Basket(PasteMarkupToolMode toolMode)
            {
                ToolMode = toolMode;
            }

            public void Update()
            {
                Count = ToolMode.Sources.Count(s => s.Target == null);

                if (!IsEmpty)
                {
                    var cameraDir = -NodeMarkupTool.CameraDirection;
                    cameraDir.y = 0;
                    cameraDir.Normalize();
                    Direction = cameraDir.Turn90(false);
                    Position = ToolMode.Centre + cameraDir * (ToolMode.Radius + 2 * Target.Size);
                    Width = (Target.Size * (Count + 1) + Source.Size * (Count - 1)) / 2;
                }
            }

            public void Render(RenderManager.CameraInfo cameraInfo)
            {
                if (!IsEmpty && !ToolMode.IsSelectedSource)
                {
                    var halfWidth = (Width - Target.Size) / 2;
                    var basket = new StraightTrajectory(Position - Direction * halfWidth, Position + Direction * halfWidth);
                    NodeMarkupTool.RenderTrajectory(cameraInfo, Colors.White, basket, Target.Size, alphaBlend: false);
                }
            }
        }
    }


}
