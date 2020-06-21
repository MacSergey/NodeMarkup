using ColossalFramework;
using ColossalFramework.UI;
using NodeMarkup.UI;
using NodeMarkup.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace NodeMarkup
{
    public class NodeMarkupTool : ToolBase
    {
        ushort hoverNodeId = 0;
        ushort selectNodeId = 0;
        Color32 hoverColor = new Color32(51, 181, 229, 224);
        Color32[] linePointColors = new Color32[]
        {
            new Color32(204, 0, 0, 224),
            new Color32(0, 204, 0, 224),
            new Color32(0, 0, 204, 224),
            new Color32(204, 0, 255, 224),
            new Color32(255, 204, 0, 224),
            new Color32(0, 255, 204, 224),
            new Color32(204, 255, 0, 224),
            new Color32(0, 204, 255, 224),
            new Color32(255, 0, 204, 224),
        };

        bool IsHover => hoverNodeId != 0;
        bool IsSelect => selectNodeId != 0;

        private NetManager NetManager => Singleton<NetManager>.instance;
        private RenderManager RenderManager => Singleton<RenderManager>.instance;

        public ToolBase CurrentTool => ToolsModifierControl.toolController?.CurrentTool;
        public bool ToolEnabled => CurrentTool == this;
        NodeMarkupButton Button => NodeMarkupButton.Instace;

        public static NodeMarkupTool Instance
        {
            get
            {
                GameObject toolModControl = ToolsModifierControl.toolController?.gameObject;
                return toolModControl?.GetComponent<NodeMarkupTool>();
            }
        }
        protected override void Awake()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(Awake)}");
            NodeMarkupButton.CreateButton();

            base.Awake();
        }

        public static NodeMarkupTool Create()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(Create)}");
            GameObject nodeMarkupControl = ToolsModifierControl.toolController.gameObject;
            var tool = nodeMarkupControl.AddComponent<NodeMarkupTool>();
            return tool;
        }
        public static void Remove()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(Remove)}");
            var tool = Instance;
            if (tool != null)
                Destroy(tool);
        }
        protected override void OnDestroy()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(OnDestroy)}");
            Button?.Hide();
            Destroy(Button);
            base.OnDestroy();
        }
        protected override void OnEnable()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(OnEnable)}");
            base.OnEnable();
            Button?.Activate();
            hoverNodeId = 0;
        }
        protected override void OnDisable()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(OnDisable)}");
            base.OnDisable();
            Button?.Deactivate();
            hoverNodeId = 0;
        }

        protected override void OnToolUpdate()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(OnToolUpdate)}");
            GetHovered();
        }
        private void GetHovered()
        {
            if (!UIView.IsInsideUI() && Cursor.visible)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastInput input = new RaycastInput(ray, Camera.main.farClipPlane)
                {
                    m_ignoreTerrain = true,
                    m_ignoreNodeFlags = NetNode.Flags.None,
                    m_ignoreSegmentFlags = NetSegment.Flags.All
                };
                input.m_netService.m_itemLayers = (ItemClass.Layer.Default | ItemClass.Layer.MetroTunnels);
                input.m_netService.m_service = ItemClass.Service.Road;

                if (RayCast(input, out RaycastOutput output))
                {
                    hoverNodeId = output.m_netNode;
                    return;
                }
            }

            hoverNodeId = 0;
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(RenderOverlay)}");

            base.RenderOverlay(cameraInfo);

            if (hoverNodeId == 0)
                return;

            var node = Utilities.GetNode(hoverNodeId);
            RenderManager.OverlayEffect.DrawCircle(cameraInfo, hoverColor, node.m_position, Mathf.Max(6f, node.Info.m_halfWidth * 2f), -1f, 1280f, false, true);

            var nodeMarkup = new NodeMarkup(hoverNodeId);
            foreach(var enter in nodeMarkup)
            {
                var points = enter.ToArray();
                for (var i = 0; i < points.Length; i += 1)
                {
                    RenderManager.OverlayEffect.DrawCircle(cameraInfo, linePointColors[i % linePointColors.Length], points[i].Position, 1f, -1f, 1280f, false, true);
                }
            }

            //foreach (var segment in node.Segments())
            //{
            //    var segmentDir = segment.m_startNode == hoverNodeId ? SegmentDir.Start : SegmentDir.End;
            //    var direction = segmentDir == SegmentDir.Start ? segment.m_startDirection : segment.m_endDirection;
            //    var segmentInvert = (segment.m_flags & NetSegment.Flags.Invert) == NetSegment.Flags.Invert;
            //    var laneInvert = segmentDir == SegmentDir.Start ^ segmentInvert;

            //    var cornerAngle = (segmentDir == SegmentDir.Start) ? segment.m_cornerAngleStart : segment.m_cornerAngleEnd;
            //    var cornerDir = Vector3.right.TurnDeg(cornerAngle / 255f * 360f, false) * (laneInvert ? 1 : -1);

            //    var lineList = new MarkupLine[segment.Info.m_lanes.Length + 1];
            //    var lanes = segment.GetLanes().ToArray();
            //    var driveLane = 0;
            //    for (var i = 0; i < segment.Info.m_lanes.Length; i += 1)
            //    {
            //        var sortI = segment.Info.m_sortedLanes[!laneInvert ? segment.Info.m_lanes.Length - i - 1 : i];
            //        //var sortI = segment.Info.m_sortedLanes[i];
            //        var lane = lanes[sortI];
            //        var laneInfo = segment.Info.m_lanes[sortI];
            //        if ((laneInfo.m_vehicleType & VehicleInfo.VehicleType.Car) != VehicleInfo.VehicleType.None && (laneInfo.m_laneType & NetInfo.LaneType.Parking) == NetInfo.LaneType.None)
            //        {
            //            lineList[driveLane].RightInfo = laneInfo;
            //            lineList[driveLane].RightLane = lane;
            //            lineList[driveLane + 1].LeftInfo = laneInfo;
            //            lineList[driveLane + 1].LeftLane = lane;

            //            driveLane += 1;
            //        }
            //    }

            //    var drawLine = new List<MarkupLine>();
            //    foreach (var line in lineList)
            //    {
            //        if (line.IsEmpty)
            //            continue;
            //        if (!line.NeedSeparate)
            //            drawLine.Add(line);
            //        else
            //        {
            //            drawLine.Add(line.RightMarkupLine);
            //            drawLine.Add(line.LeftMarkupLine);
            //        }
            //    }

            //    for (var i = 0; i < drawLine.Count; i += 1)
            //    {
            //        var position = drawLine[i].GetPosition(segmentDir, cornerDir);
            //        RenderManager.OverlayEffect.DrawCircle(cameraInfo, linePointColors[i % linePointColors.Length], position, 1f, -1f, 1280f, false, true);
            //    }
            //}
        }
        public void ToggleTool()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(ToggleTool)}");
            if (!ToolEnabled)
                EnableTool();
            else
                DisableTool();
        }

        public void EnableTool()
        {
            ToolsModifierControl.toolController.CurrentTool = this;
        }

        public void DisableTool()
        {
            if (CurrentTool == this)
                ToolsModifierControl.SetTool<DefaultTool>();
        }
    }
    //public struct MarkupLine
    //{
    //    public NetLane RightLane;
    //    public NetInfo.Lane RightInfo;

    //    public NetLane LeftLane;
    //    public NetInfo.Lane LeftInfo;

    //    public bool IsRightEdge => LeftInfo == null;
    //    public bool IsLeftEdge => RightInfo == null;
    //    public bool IsEmpty => IsRightEdge && IsLeftEdge;
    //    public bool IsEdge => IsRightEdge ^ IsLeftEdge;

    //    private NetLane EdgeLane => IsRightEdge ? RightLane : LeftLane;
    //    private NetInfo.Lane EdgeLaneInfo => IsRightEdge ? RightInfo : LeftInfo;

    //    public MarkupLine LeftMarkupLine => new MarkupLine()
    //    {
    //        LeftLane = LeftLane,
    //        LeftInfo = LeftInfo
    //    };
    //    public MarkupLine RightMarkupLine => new MarkupLine()
    //    {
    //        RightLane = RightLane,
    //        RightInfo = RightInfo
    //    };
    //    private float CenterDelte => IsEdge ? 0f : (RightInfo?.m_position ?? 0) - (LeftInfo?.m_position ?? 0);
    //    private float SideDelta => CenterDelte - ((RightInfo?.m_width ?? 0) + (LeftInfo?.m_width ?? 0)) / 2;
    //    public bool NeedSeparate => !IsEdge && SideDelta >= (RightInfo.m_width + LeftInfo.m_width) / 4;

    //    public Vector3 GetPosition(SegmentDir segmentDir, Vector3 cornerDir)
    //    {
    //        var point = segmentDir == SegmentDir.Start ? 0f : 1f;

    //        if (!IsEdge)
    //        {
    //            var rightPos = RightLane.CalculatePosition(point);
    //            var leftPos = LeftLane.CalculatePosition(point);

    //            var part = (RightInfo.m_width + SideDelta) / 2 / CenterDelte;
    //            var pos = Vector3.Lerp(rightPos, leftPos, part);

    //            return pos;
    //        }
    //        else
    //        {
    //            var pos = EdgeLane.CalculatePosition(point);
    //            var lineShift = (IsRightEdge ? RightInfo.m_width : -LeftInfo.m_width) / 2;
    //            pos += cornerDir * lineShift;

    //            return pos;
    //        }
    //    }
    //}
    public enum SegmentDir
    {
        Start,
        End
    }

    public class NodeMarkup : IEnumerable<SegmentEnter>
    {
        public ushort NodeId { get; }
        Dictionary<ushort, SegmentEnter> Enters { get; } = new Dictionary<ushort, SegmentEnter>();

        public NodeMarkup(ushort nodeId)
        {
            NodeId = nodeId;

            var node = Utilities.GetNode(NodeId);
            foreach (var segmentId in node.SegmentsId())
            {
                var enter = new SegmentEnter(NodeId, segmentId);
                Enters[segmentId] = enter;
            }
        }

        public IEnumerator<SegmentEnter> GetEnumerator() => Enters.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public class SegmentEnter : IEnumerable<MarkupPoint>
    {
        public ushort SegmentId { get; }
        public NetSegment Segment { get; }
        public bool IsStartSide { get; }
        public bool IsLaneInvert => IsStartSide ^ Segment.IsInvert();
        List<MarkupPoint> Points { get; } = new List<MarkupPoint>();
        public Vector3 CornerDir { get; private set; }

        public SegmentEnter(ushort nodeId, ushort segmentId)
        {
            SegmentId = segmentId;
            Segment = Utilities.GetSegment(SegmentId);
            IsStartSide = Segment.m_startNode == nodeId;

            Update();

            CreatePoints();
        }
        private void CreatePoints()
        {
            var info = Segment.Info;
            var lanes = Segment.GetLanesId().ToArray();
            var driveLanesIdxs = info.m_sortedLanes.Where(s => Utilities.IsDriveLane(info.m_lanes[s]));
            if (!IsLaneInvert)
                driveLanesIdxs = driveLanesIdxs.Reverse();

            var driveLanes = driveLanesIdxs.Select(d => new SegmentLane(lanes[d], info.m_lanes[d])).ToArray();

            var markupLines = new SegmentMarkupLine[driveLanes.Length + 1];

            for(int i = 0; i < markupLines.Length; i += 1)
            {
                var left = i - 1 >= 0 ? driveLanes[i - 1] : null;
                var right = i < driveLanes.Length ? driveLanes[i] : null;
                var markupLine = new SegmentMarkupLine(this, left, right);
                markupLines[i] = markupLine;
            }

            foreach(var markupLine in markupLines)
            {
                var points = markupLine.GetMarkupPoints();
                Points.AddRange(points);
            }
        }

        public void Update()
        {
            var cornerAngle = IsStartSide ? Segment.m_cornerAngleStart : Segment.m_cornerAngleEnd;
            CornerDir = Vector3.right.TurnDeg(cornerAngle / 255f * 360f, false).normalized * (IsLaneInvert ? 1 : -1);
        }

        public MarkupPoint this[int index] => Points[index];

        public IEnumerator<MarkupPoint> GetEnumerator() => Points.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public class SegmentLane
    {
        public uint LaneId { get; }
        public NetInfo.Lane Info { get; }
        public NetLane NetLane => Utilities.GetLane(LaneId);
        public float Position => Info.m_position;
        public float HalfWidth => Info.m_width / 2;
        public float LeftSidePos => Position - HalfWidth;
        public float RightSidePos => Position + HalfWidth;

        public SegmentLane(uint laneId, NetInfo.Lane info)
        {
            LaneId = laneId;
            Info = info;
        }
    }
    public class SegmentMarkupLine
    {
        public SegmentEnter SegmentEnter { get; }

        SegmentLane LeftLane { get; }
        SegmentLane RightLane { get; }
        float Point => SegmentEnter.IsStartSide ? 0f : 1f;

        public bool IsRightEdge => LeftLane == null;
        public bool IsLeftEdge => RightLane == null;
        public bool IsEdge => IsRightEdge ^ IsLeftEdge;
        public bool NeedSplit => !IsEdge && SideDelta >= (RightLane.HalfWidth + LeftLane.HalfWidth) / 2;

        public float CenterDelte => IsEdge ? 0f : RightLane.Position - LeftLane.Position;
        public float SideDelta => IsEdge ? 0f : RightLane.LeftSidePos - LeftLane.RightSidePos;
        public float HalfSideDelta => SideDelta / 2;

        public SegmentMarkupLine(SegmentEnter segmentEnter, SegmentLane leftLane, SegmentLane rightLane)
        {
            SegmentEnter = segmentEnter;
            LeftLane = leftLane;
            RightLane = rightLane;
        }

        public MarkupPoint[] GetMarkupPoints()
        {
            if (IsEdge)
            {
                var point = new MarkupPoint(this, IsRightEdge ? MarkupPoint.Type.RightEdge : MarkupPoint.Type.LeftEdge);
                return new MarkupPoint[] { point };
            }
            else if (NeedSplit)
            {
                var pointLeft = new MarkupPoint(this, MarkupPoint.Type.LeftEdge);
                var pointRight = new MarkupPoint(this, MarkupPoint.Type.RightEdge);
                return new MarkupPoint[] { pointLeft, pointRight };
            }
            else
            {
                var point = new MarkupPoint(this, MarkupPoint.Type.Between);
                return new MarkupPoint[] { point };
            }
        }

        public void GetPositionAndDirection(MarkupPoint.Type pointType, out Vector3 position, out Vector3 direction)
        {
            if ((pointType & MarkupPoint.Type.Between) != MarkupPoint.Type.None)
                GetMiddlePosition(out position, out direction);

            else if ((pointType & MarkupPoint.Type.Edge) != MarkupPoint.Type.None)
                GetEdgePosition(pointType, out position, out direction);

            else
                throw new Exception();
        }
        void GetMiddlePosition(out Vector3 position, out Vector3 direction)
        {
            RightLane.NetLane.CalculatePositionAndDirection(Point, out Vector3 rightPos, out Vector3 rightDir);
            LeftLane.NetLane.CalculatePositionAndDirection(Point, out Vector3 leftPos, out Vector3 leftDir);

            var part = (RightLane.HalfWidth + HalfSideDelta) / CenterDelte;
            position = Vector3.Lerp(rightPos, leftPos, part);
            direction = (rightDir + leftDir) / 2;
        }
        void GetEdgePosition(MarkupPoint.Type pointType, out Vector3 position, out Vector3 direction)
        {
            float lineShift;
            switch (pointType)
            {
                case MarkupPoint.Type.LeftEdge:
                    LeftLane.NetLane.CalculatePositionAndDirection(Point, out position, out direction);
                    lineShift = -LeftLane.HalfWidth;
                    break;
                case MarkupPoint.Type.RightEdge:
                    RightLane.NetLane.CalculatePositionAndDirection(Point, out position, out direction);
                    lineShift = RightLane.HalfWidth;
                    break;
                default:
                    throw new Exception();
            }
            direction = SegmentEnter.IsStartSide ? -direction : direction;

            var angle = Vector3.Angle(direction, SegmentEnter.CornerDir);
            angle = (angle > 90 ? 180 - angle : angle);
            lineShift /= Mathf.Sin(angle * Mathf.Deg2Rad);

            direction.Normalize();
            position += SegmentEnter.CornerDir * lineShift;
        }
    }

    public class MarkupPoint
    {
        public Vector3 Position { get; private set; }
        public Vector3 Direction { get; private set; }
        public Type PointType { get; private set; }

        SegmentMarkupLine MarkupLine { get; }

        public MarkupPoint(SegmentMarkupLine markupLine, Type pointType)
        {
            MarkupLine = markupLine;
            PointType = pointType;

            Update();
        }

        public void Update()
        {
            MarkupLine.GetPositionAndDirection(PointType, out Vector3 position, out Vector3 direction);
            Position = position;
            Direction = direction;
        }

        public enum Type
        {
            None = 0,
            Edge = 1,
            LeftEdge = 2 + Edge,
            RightEdge = 4 + Edge,
            Between = 8,
            BetweenSomeDir = 16 + Between,
            BetweenDiffDir = 32 + Between,
        }
    }
}
