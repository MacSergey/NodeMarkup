using ColossalFramework.Math;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.UI;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ToolBase;
using ColossalFramework.UI;

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
            if (Borders.NodeId != nodeId)
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
            NodeMarkupTool.RenderBezier(bezier, new OverlayData(cameraInfo) { Color = Colors.Orange, Width = segment.Info.m_halfWidth * 2, Cut = true });
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
                for (var i = 0; i < SegmentDatas.Length; i += 1)
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
                    halfWidth = segment.Info.m_halfWidth.RoundToNearest(0.1f),
                };
                data.dir = (data.isStart ? segment.m_startDirection : segment.m_endDirection).normalized;
                data.angle = data.dir.AbsoluteAngle();

                segment.CalculateCorner(segmentId, true, data.isStart, true, out data.leftPos, out data.leftDir, out _);
                segment.CalculateCorner(segmentId, true, data.isStart, false, out data.rightPos, out data.rightDir, out _);

                //var t = (segment.Info.m_pavementWidth / segment.Info.m_halfWidth) / 2;
                //var line = new StraightTrajectory(leftPos, rightPos).Cut(t, 1 - t);
                //data.leftPos = line.StartPosition;
                //data.rightPos = line.EndPosition;

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
            data.Cut = true;
            //data.AlphaBlend = false;

            for (var i = 0; i < SegmentDatas.Length; i += 1)
                RenderCurve(data, i);

            //if (SegmentDatas.Length > 2)
            //{
            //    for (var i = 0; i < SegmentDatas.Length; i += 1)
            //        RenderStraight(data, i);
            //}
        }
        private void RenderCurve(OverlayData overlayData, int i)
        {
            var data1 = SegmentDatas[i];
            var data2 = SegmentDatas[(i + 1) % SegmentDatas.Length];
            var width1 = (data1.rightPos - data1.leftPos).XZ().magnitude * 0.5f;
            var width2 = (data2.leftPos - data2.rightPos).XZ().magnitude * 0.5f;
            var cornerDir1 = (data1.rightPos - data1.leftPos).normalized;
            var cornerDir2 = (data2.leftPos - data2.rightPos).normalized;

            var bezierWidth = Mathf.Min(width1, width2);
            var count = Math.Max(Mathf.CeilToInt(width1 / bezierWidth), Mathf.CeilToInt(width2 / bezierWidth));
            var step1 = (width1 - bezierWidth) / (count - 1);
            var step2 = (width2 - bezierWidth) / (count - 1);


            for (var l = 0; l < count; l += 1)
            {
                var bezier = new Bezier3()
                {
                    a = data1.leftPos + cornerDir1 * (bezierWidth / 2 + l * step1),
                    b = cornerDir1.Turn90(true).normalized,
                    c = cornerDir2.Turn90(false).normalized,
                    d = data2.rightPos + cornerDir2 * (bezierWidth / 2 + l * step2),
                };

                NetSegment.CalculateMiddlePoints(bezier.a, bezier.b, bezier.d, bezier.c, true, true, out bezier.b, out bezier.c);

                overlayData.Width = bezierWidth;
                NodeMarkupTool.RenderBezier(bezier, overlayData);
            }
        }
        private void RenderStraight(OverlayData overlayData, int i)
        {
            var dataR = SegmentDatas[(i + SegmentDatas.Length - 1) % SegmentDatas.Length];
            var data = SegmentDatas[i];
            var dataL = SegmentDatas[(i + 1) % SegmentDatas.Length];

            var posR = (dataR.leftPos + dataR.rightPos) / 2;
            var posL = (dataL.leftPos + dataL.rightPos) / 2;

            var cornerLine = new StraightTrajectory(data.leftPos, data.rightPos, false);
            var dir = cornerLine.Direction.Turn90(true).normalized;

            var leftNormal = new StraightTrajectory(posL, posL - dir, false);
            var rightNormal = new StraightTrajectory(posR, posR - dir, false);
            var intersectLeftNormal = MarkupIntersect.CalculateSingle(cornerLine, leftNormal);
            var intersectRightNormal = MarkupIntersect.CalculateSingle(cornerLine, rightNormal);

            var leftT = Mathf.Clamp(intersectLeftNormal.FirstT, 0f, 1f);
            var rightT = Mathf.Clamp(intersectRightNormal.FirstT, 0f, 1f);

            cornerLine = cornerLine.Cut(leftT, rightT, false);


            var leftLine = new StraightTrajectory(posL, posL - dataL.dir, false);
            var rigthLine = new StraightTrajectory(posR, posR - dataR.dir, false);
            var intersectLeft = MarkupIntersect.CalculateSingle(new StraightTrajectory(cornerLine.StartPosition, cornerLine.StartPosition + dir, false), leftLine);
            var intersectRight = MarkupIntersect.CalculateSingle(new StraightTrajectory(cornerLine.EndPosition, cornerLine.EndPosition + dir, false), rigthLine);

            var length = Mathf.Min(intersectLeft.FirstT, intersectRight.FirstT);
            if (length > 0)
            {
                var pos = (cornerLine.StartPosition + cornerLine.EndPosition) / 2;
                overlayData.Width = cornerLine.Length;
                new StraightTrajectory(pos, pos + dir * length).Render(overlayData);
            }

            //{
            //    var pos = (data.leftPos + data.rightPos) / 2;
            //    new StraightTrajectory(pos, pos + dir * 3).Render(new OverlayData(overlayData.CameraInfo) { Color = overlayData.Color });

            //    leftNormal.Cut(0, intersectLeftNormal.SecondT).Render(new OverlayData(overlayData.CameraInfo) { Color = Colors.Red });
            //    rightNormal.Cut(0, intersectRightNormal.SecondT).Render(new OverlayData(overlayData.CameraInfo) { Color = Colors.Blue });

            //    NodeMarkupTool.RenderCircle(data.leftPos, new OverlayData(overlayData.CameraInfo) { Color = Colors.Red, Width = 1f });
            //    NodeMarkupTool.RenderCircle(data.rightPos, new OverlayData(overlayData.CameraInfo) { Color = Colors.Blue, Width = 1f });
            //}
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
            public Vector3 dir;
            public float halfWidth;
        }
    }
}
