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
using ColossalFramework;
using ModsCommon;

namespace NodeMarkup.Tools
{
    public class SelectToolMode : NodeMarkupToolMode
    {
        public override ToolModeType Type => ToolModeType.Select;
        public override bool ShowPanel => false;

        private NodeSelection HoverNode { get; set; } = null;
        private bool IsHoverNode => HoverNode != null;

        private SegmentSelection HoverSegment { get; set; } = null;
        private bool IsHoverSegment => HoverSegment != null;

        protected override void Reset(BaseToolMode prevMode)
        {
            HoverNode = null;
            HoverSegment = null;
        }

        public override void OnToolUpdate()
        {
            NodeSelection nodeSelection = null;
            SegmentSelection segmentSelection = null;

            if (NodeMarkupTool.MouseRayValid)
            {
                if (IsHoverNode && HoverNode.Contains(NodeMarkupTool.Ray, out _))
                    nodeSelection = HoverNode;
                else if (IsHoverSegment && HoverSegment.Contains(NodeMarkupTool.Ray, out _))
                    segmentSelection = HoverSegment;
                else
                    RayCast(out nodeSelection, out segmentSelection);
            }

            HoverNode = nodeSelection;
            HoverSegment = segmentSelection;
        }

        private void RayCast(out NodeSelection nodeSelection, out SegmentSelection segmentSelection)
        {
            var hitPos = NodeMarkupTool.MouseWorldPosition;
            var gridMinX = Max(hitPos.x);
            var gridMinZ = Max(hitPos.z);
            var gridMaxX = Min(hitPos.x);
            var gridMaxZ = Min(hitPos.z);
            var segmentBuffer = Singleton<NetManager>.instance.m_segments.m_buffer;
            var checkedNodes = new HashSet<ushort>();

            var priority = 1f;

            nodeSelection = null;
            segmentSelection = null;

            var onlyNodes = InputExtension.OnlyCtrlIsPressed;
            var onlySegments = InputExtension.OnlyShiftIsPressed;

            for (int i = gridMinZ; i <= gridMaxZ; i++)
            {
                for (int j = gridMinX; j <= gridMaxX; j++)
                {
                    var segmentId = NetManager.instance.m_segmentGrid[i * 270 + j];
                    int count = 0;

                    while (segmentId != 0u && count < 36864)
                    {
                        if (CheckSegment(segmentId))
                        {
                            var segment = segmentId.GetSegment();
                            float t;

                            if (!onlySegments && RayCastNode(checkedNodes, segment.m_startNode, out NodeSelection startSelection, out t) && t < priority)
                            {
                                nodeSelection = startSelection;
                                segmentSelection = null;
                                priority = t;
                            }
                            else if (!onlySegments && RayCastNode(checkedNodes, segment.m_endNode, out NodeSelection endSelection, out t) && t < priority)
                            {
                                nodeSelection = endSelection;
                                segmentSelection = null;
                                priority = t;
                            }
                            else if (!onlyNodes && RayCastSegments(segmentId, out SegmentSelection selection, out t) && t < priority)
                            {
                                segmentSelection = selection;
                                nodeSelection = null;
                                priority = t;
                            }
                        }

                        segmentId = segmentBuffer[segmentId].m_nextGridSegment;
                    }
                }
            }

            static int Max(float value) => Mathf.Max((int)((value - 16f) / 64f + 135f) - 1, 0);
            static int Min(float value) => Mathf.Min((int)((value + 16f) / 64f + 135f) + 1, 269);
        }
        bool RayCastNode(HashSet<ushort> checkedNodes, ushort nodeId, out NodeSelection selection, out float t)
        {
            if (!checkedNodes.Contains(nodeId))
            {
                checkedNodes.Add(nodeId);
                selection = new NodeSelection(nodeId);
                return selection.Contains(NodeMarkupTool.Ray, out t);
            }
            else
            {
                selection = null;
                t = 0f;
                return false;
            }
        }
        bool RayCastSegments(ushort segmentId, out SegmentSelection selection, out float t)
        {
            selection = new SegmentSelection(segmentId);
            return selection.Contains(NodeMarkupTool.Ray, out t);
        }


        private bool CheckSegment(ushort segmentId)
        {
            var segment = segmentId.GetSegment();
            var connect = segment.Info.GetConnectionClass();

            if ((segment.m_flags & NetSegment.Flags.Created) == 0)
                return false;

            if ((connect.m_layer & ItemClass.Layer.Default) == 0)
                return false;

            if (connect.m_service != ItemClass.Service.Road && (connect.m_service != ItemClass.Service.PublicTransport || connect.m_subService != ItemClass.SubService.PublicTransportPlane))
                return false;

            return true;
        }


        public override string GetToolInfo() => IsHoverNode ? string.Format(Localize.Tool_InfoHoverNode, HoverNode.Id) : (IsHoverSegment ? string.Format(Localize.Tool_InfoHoverSegment, HoverSegment.Id) : Localize.Tool_SelectInfo);

        public override void OnMouseUp(Event e) => OnPrimaryMouseClicked(e);
        public override void OnPrimaryMouseClicked(Event e)
        {
            var markup = default(Markup);
            if (IsHoverNode)
                markup = MarkupManager.NodeManager.Get(HoverNode.Id);
            else if (IsHoverSegment)
                markup = MarkupManager.SegmentManager.Get(HoverSegment.Id);
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
                HoverNode.Render(new OverlayData(cameraInfo) { Color = Colors.Orange });
            else if (IsHoverSegment)
                HoverSegment.Render(new OverlayData(cameraInfo) { Color = Colors.Purple });
        }
    }
}
