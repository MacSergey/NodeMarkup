using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.Tools
{
    public abstract class BaseMakeItemToolMode : BaseToolMode
    {
        List<MarkupPoint> TargetPoints { get; set; } = new List<MarkupPoint>();

        public MarkupPoint HoverPoint { get; protected set; } = null;
        public MarkupPoint SelectPoint { get; protected set; } = null;

        protected bool IsHoverPoint => HoverPoint != null;
        protected bool IsSelectPoint => SelectPoint != null;

        protected override void Reset(BaseToolMode prevMode)
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
                Tool.SetMode(ToolModeType.MakeFiller);
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
            if (NodeMarkupTool.EditMarkingShortcut.IsPressed(e))
            {
                Tool.EditMarkup();
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
                Tool.SetMarkup(null);
                Tool.SetMode(ToolModeType.SelectNode);
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
}
