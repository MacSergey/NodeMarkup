using IMT.Manager;
using ModsCommon;
using ModsCommon.Utilities;
using System.Collections.Generic;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;

namespace IMT.Tools
{
    public abstract class BaseMakeItemToolMode : IntersectionMarkingToolMode
    {
        protected List<MarkingPoint> TargetPoints { get; set; } = new List<MarkingPoint>();

        public MarkingPoint HoverPoint { get; protected set; } = null;
        public MarkingPoint SelectPoint { get; protected set; } = null;

        protected bool IsHoverPoint => HoverPoint != null;
        protected bool IsSelectPoint => SelectPoint != null;

        protected override void Reset(IToolMode prevMode)
        {
            HoverPoint = null;
            SelectPoint = null;
            SetTarget();
        }

        public override void OnToolUpdate()
        {
            if (SingletonTool<IntersectionMarkingTool>.Instance.MouseRayValid)
            {
                foreach (var point in TargetPoints)
                {
                    if (point.IsHover(SingletonTool<IntersectionMarkingTool>.Instance.MouseRay))
                    {
                        HoverPoint = point;
                        return;
                    }
                }
            }

            if (IsSelectPoint && SelectPoint.Type == MarkingPoint.PointType.Enter && (SelectPoint.Enter.SupportPoints & MarkingPoint.PointType.Normal) != 0)
            {
                var connectLine = SingletonTool<IntersectionMarkingTool>.Instance.Ray.GetRayPosition(Marking.Position.y, out _) - SelectPoint.MarkerPosition;
                if (connectLine.magnitude >= 2 && 135 <= Vector3.Angle(XZ(SelectPoint.Direction), XZ(connectLine)) && SelectPoint.Enter.TryGetPoint(SelectPoint.Index, MarkingPoint.PointType.Normal, out MarkingPoint normalPoint))
                {
                    HoverPoint = normalPoint;
                    return;
                }
            }

            HoverPoint = null;
        }
        public override string GetToolInfo()
        {
            var pointPair = new MarkingPointPair(SelectPoint, HoverPoint);
            var exist = Tool.Marking.ExistLine(pointPair);

            if (pointPair.IsStopLine)
                return exist ? $"{Localize.Tool_InfoDeleteStopLine}\n{string.Format(Localize.Tool_InfoSelectLine, LocalizeExtension.Ctrl.AddInfoColor())}" : Tool.GetModifierToolTip<StopLineStyle.StopLineType>(Localize.Tool_InfoCreateStopLine, pointPair.NetworkType, pointPair.LineType);
            else if (pointPair.IsCrosswalk)
                return exist ? $"{Localize.Tool_InfoDeleteCrosswalk}\n{string.Format(Localize.Tool_InfoSelectCrosswalk, LocalizeExtension.Ctrl.AddInfoColor())}" : Tool.GetModifierToolTip<BaseCrosswalkStyle.CrosswalkType>(Localize.Tool_InfoCreateCrosswalk, pointPair.NetworkType, pointPair.LineType);
            else if (pointPair.IsNormal)
                return exist ? $"{Localize.Tool_InfoDeleteNormalLine}\n{string.Format(Localize.Tool_InfoSelectLine, LocalizeExtension.Ctrl.AddInfoColor())}" : Tool.GetModifierToolTip<RegularLineStyle.RegularLineType>(Localize.Tool_InfoCreateNormalLine, pointPair.NetworkType, pointPair.LineType);
            else if (pointPair.IsLane)
                return exist ? $"{Localize.Tool_InfoDeleteLaneLine}\n{string.Format(Localize.Tool_InfoSelectLane, LocalizeExtension.Ctrl.AddInfoColor())}" : Tool.GetModifierToolTip<RegularLineStyle.RegularLineType>(Localize.Tool_InfoCreateLaneLine, pointPair.NetworkType, pointPair.LineType);
            else
                return exist ? $"{Localize.Tool_InfoDeleteLine}\n{string.Format(Localize.Tool_InfoSelectLine, LocalizeExtension.Ctrl.AddInfoColor())}" : Tool.GetModifierToolTip<RegularLineStyle.RegularLineType>(Localize.Tool_InfoCreateLine, pointPair.NetworkType, pointPair.LineType);
        }

        public override void OnPrimaryMouseClicked(Event e)
        {
            SelectPoint = HoverPoint;
            SetTarget(SelectPoint);
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
                Tool.SetMarking(null);
                Tool.SetMode(ToolModeType.Select);
            }
        }
        public override bool OnEscape()
        {
            if (IsSelectPoint)
            {
                SelectPoint = null;
                SetTarget();
                return true;
            }
            else
                return false;
        }

        #region SET TARGET

        protected void SetTarget(MarkingPoint ignore = null)
        {
            TargetPoints.Clear();
            foreach (var enter in Tool.Marking.Enters)
                TargetPoints.AddRange(GetTarget(enter, ignore));
        }
        protected abstract IEnumerable<MarkingPoint> GetTarget(Entrance enter, MarkingPoint ignore);
        protected int Find(int[] allow, int idx, int sign)
        {
            do
                idx += sign;
            while (idx >= 0 && idx < allow.Length && allow[idx] != 2);

            return idx;
        }
        protected void SetNotAllow(int[] allow, int idx, int sign)
        {
            idx += sign;
            while (idx >= 0 && idx < allow.Length)
            {
                allow[idx] = 0;
                idx += sign;
            }
        }

        #endregion

        protected void RenderPointsOverlay(RenderManager.CameraInfo cameraInfo, bool splitPoint = true)
        {
            foreach (var point in TargetPoints)
                point.Render(new OverlayData(cameraInfo) { SplitPoint = splitPoint });
        }
    }
}
