using ColossalFramework;
using ColossalFramework.UI;
using NodeMarkup.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
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

            foreach (var segment in node.GetSegments())
            {
                var segmentDir = segment.m_startNode == hoverNodeId ? SegmentDir.Start : SegmentDir.End;
                var direction = segmentDir == SegmentDir.Start ? segment.m_startDirection : segment.m_endDirection;
                var invert = (segment.m_flags & NetSegment.Flags.Invert) == NetSegment.Flags.Invert;

                var cornerAngle = (segmentDir == SegmentDir.Start ^ invert) ? segment.m_cornerAngleStart : segment.m_cornerAngleEnd;
                var cornerDir = Vector3.right.TurnDeg(cornerAngle / 255f * 360f, false);

                var lineList = new MarkupLine[segment.Info.m_lanes.Length + 1];
                var lanes = segment.GetLanes().ToArray();
                var driveLane = 0;
                for (var i = 0; i < segment.Info.m_lanes.Length; i += 1)
                {
                    var sortI = segment.Info.m_sortedLanes[invert ? segment.Info.m_lanes.Length - i - 1 : i];
                    //var sortI = segment.Info.m_sortedLanes[i];
                    var lane = lanes[sortI];
                    var laneInfo = segment.Info.m_lanes[sortI];
                    if ((laneInfo.m_vehicleType & VehicleInfo.VehicleType.Car) != VehicleInfo.VehicleType.None && (laneInfo.m_laneType & NetInfo.LaneType.Parking) == NetInfo.LaneType.None)
                    {
                        lineList[driveLane].RightInfo = laneInfo;
                        lineList[driveLane].RightLane = lane;
                        lineList[driveLane + 1].LeftInfo = laneInfo;
                        lineList[driveLane + 1].LeftLane = lane;

                        driveLane += 1;
                    }
                }

                var drawLine = new List<MarkupLine>();
                foreach (var line in lineList)
                {
                    if (line.IsEmpty)
                        continue;
                    if (!line.NeedSeparate)
                        drawLine.Add(line);
                    else
                    {
                        drawLine.Add(line.RightMarkupLine);
                        drawLine.Add(line.LeftMarkupLine);
                    }
                }

                for(var i = 0; i < drawLine.Count; i += 1)
                {
                    var position = drawLine[i].GetPosition(segmentDir, cornerDir);
                    RenderManager.OverlayEffect.DrawCircle(cameraInfo, linePointColors[i % linePointColors.Length], position, 1f, -1f, 1280f, false, true);
                }
            }
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
    public struct MarkupLine
    {
        public NetLane RightLane;
        public NetInfo.Lane RightInfo;

        public NetLane LeftLane;
        public NetInfo.Lane LeftInfo;

        public bool IsRightEdge => LeftInfo == null;
        public bool IsLeftEdge => RightInfo == null;
        public bool IsEmpty => IsRightEdge && IsLeftEdge;
        public bool IsEdge => IsRightEdge ^ IsLeftEdge;

        private NetLane EdgeLane => IsRightEdge ? RightLane : LeftLane;
        private NetInfo.Lane EdgeLaneInfo => IsRightEdge ? RightInfo : LeftInfo;

        public MarkupLine LeftMarkupLine => new MarkupLine()
        {
            LeftLane = LeftLane,
            LeftInfo = LeftInfo
        };
        public MarkupLine RightMarkupLine => new MarkupLine()
        {
            RightLane = RightLane,
            RightInfo = RightInfo
        };
        private float CenterDelte => IsEdge ? 0f : (RightInfo?.m_position ?? 0) - (LeftInfo?.m_position ?? 0);
        private float SideDelta => CenterDelte - ((RightInfo?.m_width ?? 0) + (LeftInfo?.m_width ?? 0)) / 2;
        public bool NeedSeparate => !IsEdge && SideDelta >= (RightInfo.m_width + LeftInfo.m_width) / 4;

        public Vector3 GetPosition(SegmentDir segmentDir, Vector3 cornerDir)
        {
            var point = segmentDir == SegmentDir.Start ? 0f : 1f;

            if (!IsEdge)
            {
                var rightPos = RightLane.CalculatePosition(point);
                var leftPos = LeftLane.CalculatePosition(point);

                var part = (RightInfo.m_width + SideDelta) / 2 / CenterDelte;
                var pos = Vector3.Lerp(rightPos, leftPos, part);

                return pos;
            }
            else
            {
                var pos = EdgeLane.CalculatePosition(point);
                var lineShift = (IsRightEdge ? RightInfo.m_width : -LeftInfo.m_width) / 2;
                pos += cornerDir * lineShift;

                return pos;
            }
        }
    }
    public enum SegmentDir
    {
        Start,
        End
    }
}
