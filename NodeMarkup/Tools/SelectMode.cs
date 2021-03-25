using ColossalFramework.Math;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.UI;
using NodeMarkup.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ToolBase;

namespace NodeMarkup.Tools
{
    public class SelectToolMode : BaseToolMode
    {
        public override ToolModeType Type => ToolModeType.Select;
        public override bool ShowPanel => false;

        private ushort HoverNodeId { get; set; } = 0;
        private bool IsHoverNode => HoverNodeId != 0;

        private ushort HoverSegmentId { get; set; } = 0;
        private bool IsHoverSegment => HoverSegmentId != 0;

        private NodeBorder Borders { get; set; }

        protected override void Reset(BaseToolMode prevMode)
        {
            HoverNodeId = 0;
            HoverSegmentId = 0;
            Borders = new NodeBorder(0);
        }

        public override void OnToolUpdate()
        {
            ushort nodeId = 0;
            ushort segmentId = 0;

            if (NodeMarkupTool.MouseRayValid)
            {
                if (!GetRayCast(ItemClass.Service.Road, ItemClass.SubService.None, ref nodeId, ref segmentId))
                    GetRayCast(ItemClass.Service.PublicTransport, ItemClass.SubService.PublicTransportPlane, ref nodeId, ref segmentId);
            }

            HoverNodeId = nodeId;
            HoverSegmentId = segmentId;
        }
        private bool GetRayCast(ItemClass.Service service, ItemClass.SubService subService, ref ushort nodeId, ref ushort segmentId)
        {
            RaycastInput input = new RaycastInput(NodeMarkupTool.MouseRay, Camera.main.farClipPlane)
            {
                m_ignoreTerrain = true,
                m_ignoreNodeFlags = NetNode.Flags.All,
                m_ignoreSegmentFlags = NetSegment.Flags.None,
            };
            input.m_netService.m_itemLayers = (ItemClass.Layer.Default | ItemClass.Layer.MetroTunnels);
            input.m_netService.m_service = service;
            input.m_netService.m_subService = subService;

            if (NodeMarkupTool.RayCast(input, out RaycastOutput output))
            {
                if (!InputExtension.ShiftIsPressed)
                {
                    var segment = output.m_netSegment.GetSegment();

                    if (CheckNodeHover(segment.m_startNode, output.m_hitPos))
                    {
                        nodeId = segment.m_startNode;
                        return true;
                    }
                    else if (CheckNodeHover(segment.m_endNode, output.m_hitPos))
                    {
                        nodeId = segment.m_endNode;
                        return true;
                    }
                }

                segmentId = output.m_netSegment;
                return true;
            }
            else
                return false;
        }
        private bool CheckNodeHover(ushort nodeId, Vector3 hitPos)
        {
            if(Borders.NodeId != nodeId)
                Borders = new NodeBorder(nodeId);

            return Borders.Contains(hitPos);
        }


        public override string GetToolInfo() => IsHoverNode ? string.Format(Localize.Tool_InfoHoverNode, HoverNodeId) : (IsHoverSegment ? string.Format(Localize.Tool_InfoHoverSegment, HoverSegmentId) : Localize.Tool_SelectInfo);

        public override void OnMouseUp(Event e) => OnPrimaryMouseClicked(e);
        public override void OnPrimaryMouseClicked(Event e)
        {
            var markup = default(Markup);
            if (IsHoverNode)
                markup = MarkupManager.NodeManager.Get(HoverNodeId);
            else if (IsHoverSegment)
                markup = MarkupManager.SegmentManager.Get(HoverSegmentId);
            else
                return;

            Mod.Logger.Debug($"Select marking {markup}");
            Tool.SetMarkup(markup);

            if (markup.NeedSetOrder)
            {
                var messageBox = MessageBoxBase.ShowModal<YesNoMessageBox>();
                messageBox.CaptionText = Localize.Tool_RoadsWasChangedCaption;
                messageBox.MessageText = Localize.Tool_RoadsWasChangedMessage;
                messageBox.OnButton1Click = OnYes;
                messageBox.OnButton2Click = OnNo;
            }
            else
                OnNo();

            bool OnYes()
            {
                BaseOrderToolMode.IntersectionTemplate = markup.Backup;
                Tool.SetMode(ToolModeType.EditEntersOrder);
                markup.NeedSetOrder = false;
                return true;
            }
            bool OnNo()
            {
                Tool.SetDefaultMode();
                markup.NeedSetOrder = false;
                return true;
            }
        }
        public override void OnSecondaryMouseClicked() => Tool.Disable();
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHoverNode)
                RenderNodeOverlay(cameraInfo);
            else if (IsHoverSegment)
                RenderSegmentOverlay(cameraInfo);
        }
        private void RenderNodeOverlay(RenderManager.CameraInfo cameraInfo)
        {
            //var node = HoverNodeId.GetNode();
            //NodeMarkupTool.RenderCircle(node.m_position, new OverlayData(cameraInfo) { Color = Colors.Orange, Width = Mathf.Max(6f, node.Info.m_halfWidth * 2f) });

            Borders.Render(new OverlayData(cameraInfo) { Color = Colors.Orange });
        }
        private void RenderSegmentOverlay(RenderManager.CameraInfo cameraInfo)
        {
            var segment = HoverSegmentId.GetSegment();
            var bezier = new Bezier3()
            {
                a = segment.m_startNode.GetNode().m_position,
                d = segment.m_endNode.GetNode().m_position,
            };
            NetSegment.CalculateMiddlePoints(bezier.a, segment.m_startDirection, bezier.d, segment.m_endDirection, true, true, out bezier.b, out bezier.c);
            NodeMarkupTool.RenderBezier(bezier, new OverlayData(cameraInfo) { Color = Colors.Orange, Width = segment.Info.m_halfWidth * 2 });
        }
    }

    public class NodeBorder : IOverlay
    {
        public ushort NodeId { get; }
        private SegmentData[] SegmentDatas { get; }
        private IEnumerable<ITrajectory> BorderLines
        {
            get
            {
                for(var i = 0; i < SegmentDatas.Length; i += 1)
                {
                    yield return new StraightTrajectory(SegmentDatas[i].leftPos, SegmentDatas[i].rightPos);
                    var j = (i + 1) % SegmentDatas.Length;
                    yield return new BezierTrajectory(SegmentDatas[i].leftPos, -SegmentDatas[i].leftDir, SegmentDatas[j].rightPos, -SegmentDatas[j].rightDir);
                }
            }
        }
        public NodeBorder(ushort nodeId)
        {
            NodeId = nodeId;
            SegmentDatas = CalculateSegment().OrderBy(s => s.angle).ToArray();
        }
        private IEnumerable<SegmentData> CalculateSegment()
        {
            var node = NodeId.GetNode();

            foreach (var segmentId in node.SegmentsId())
            {
                var segment = segmentId.GetSegment();
                var data = new SegmentData()
                {
                    id = segmentId,
                    isStart = segment.m_startNode == NodeId,
                    width = segment.Info.m_halfWidth * 2,
                };
                data.angle = (data.isStart ? segment.m_startDirection : segment.m_endDirection).AbsoluteAngle();

                segment.CalculateCorner(segmentId, true, data.isStart, true, out data.leftPos, out data.leftDir, out _);
                segment.CalculateCorner(segmentId, true, data.isStart, false, out data.rightPos, out data.rightDir, out _);

                yield return data;
            }
        }
        public bool Contains(Vector3 position)
        {
            var node = NodeId.GetNode();
            var line = new StraightTrajectory(position, node.m_position);

            var contains = !BorderLines.Any(b => MarkupIntersect.CalculateSingle(line, b).IsIntersect);
            return contains;
        }

        public void Render(OverlayData data)
        {
            foreach (var borderLine in BorderLines)
                borderLine.Render(data);
        }

        private struct SegmentData
        {
            public ushort id;
            public bool isStart;
            public float angle;
            public Vector3 rightPos;
            public Vector3 leftPos;
            public Vector3 rightDir;
            public Vector3 leftDir;
            public float width;
        }
    }
}
