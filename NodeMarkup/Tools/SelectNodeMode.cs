using ColossalFramework.Math;
using NodeMarkup.Manager;
using NodeMarkup.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static ToolBase;

namespace NodeMarkup.Tools
{
    public class SelectNodeToolMode : BaseToolMode
    {
        public override ToolModeType Type => ToolModeType.SelectNode;
        public override bool ShowPanel => false;
        ushort HoverNodeId { get; set; } = 0;
        bool IsHoverNode => HoverNodeId != 0;
        int ClickCount { get; set; } = 0;

        bool JustFun { get; set; } = true;
        ushort HoverSegmentId { get; set; } = 0;
        bool IsHoverSegment => HoverSegmentId != 0;

        protected override void Reset(BaseToolMode prevMode)
        {
            HoverNodeId = 0;
        }

        public override void OnToolUpdate()
        {
            if (NodeMarkupTool.MouseRayValid)
            {
                RaycastInput input = new RaycastInput(NodeMarkupTool.MouseRay, Camera.main.farClipPlane)
                {
                    m_ignoreTerrain = true,
                    m_ignoreNodeFlags = NetNode.Flags.None,
                    m_ignoreSegmentFlags = JustFun ? NetSegment.Flags.None : NetSegment.Flags.All
                };
                input.m_netService.m_itemLayers = (ItemClass.Layer.Default | ItemClass.Layer.MetroTunnels);
                input.m_netService.m_service = ItemClass.Service.Road;

                if (NodeMarkupTool.RayCast(input, out RaycastOutput output))
                {
                    HoverNodeId = output.m_netNode;
                    HoverSegmentId = output.m_netSegment;
                    return;
                }
            }

            HoverNodeId = 0;
            HoverSegmentId = 0;
        }
        public override string GetToolInfo() => IsHoverNode ? string.Format(Localize.Tool_InfoHoverNode, HoverNodeId) : (IsHoverSegment ? $"Segment #{HoverSegmentId}\nClick to edit marking" : Localize.Tool_InfoNode);

        public override void OnMouseUp(Event e) => OnPrimaryMouseClicked(e);
        public override void OnPrimaryMouseClicked(Event e)
        {
            if (IsHoverNode)
            {
                var markup = MarkupManager.Get(HoverNodeId);
                Tool.SetMarkup(markup);

                if (markup.NeedSetOrder)
                {
                    var messageBox = MessageBoxBase.ShowModal<YesNoMessageBox>();
                    messageBox.CaprionText = Localize.Tool_RoadsWasChangedCaption;
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
            else if (IsHoverSegment)
            {
                ClickCount += 1;

                var messageBox = MessageBoxBase.ShowModal<OkMessageBox>();
                messageBox.CaprionText = "You missed";

                if (ClickCount < 30)
                {
                    if (ClickCount % 10 != 0)
                        messageBox.MessageText = "You missed by segment, try again";
                    else
                        messageBox.MessageText = $"You missed by segment, I didn't think that you couldn't do it {ClickCount} times in a row, try again";
                }
                else if (ClickCount == 30)
                {
                    messageBox.MessageText = "You missed by segment, To motivate you to aim better, after another 10 misses, I'll start removing one node marking at a time, try again";
                }
                else if (30 < ClickCount && ClickCount <= 40)
                {
                    messageBox.MessageText = $"You missed by segment. {40 - ClickCount} misses before the start removing node marking, try again";
                }
                else if (40 < ClickCount && ClickCount <= 50)
                {
                    messageBox.MessageText = $"You missed by segment. {ClickCount - 40} node markings removed, try again";
                }
                else
                {
                    messageBox.CaprionText = "It was joke";
                    messageBox.MessageText = $"It was joke, segment marking planing to implement in next version";
                    JustFun = false;
                }
            }
        }
        public override void OnSecondaryMouseClicked() => Tool.Disable();
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHoverNode)
            {
                var node = Utilities.GetNode(HoverNodeId);
                NodeMarkupTool.RenderCircle(cameraInfo, node.m_position, Colors.Orange, Mathf.Max(6f, node.Info.m_halfWidth * 2f));
            }
            if (IsHoverSegment)
            {
                var segment = Utilities.GetSegment(HoverSegmentId);
                var bezier = new Bezier3()
                {
                    a = Utilities.GetNode(segment.m_startNode).m_position,
                    d = Utilities.GetNode(segment.m_endNode).m_position,
                };
                NetSegment.CalculateMiddlePoints(bezier.a, segment.m_startDirection, bezier.d, segment.m_endDirection, true, true, out bezier.b, out bezier.c);
                NodeMarkupTool.RenderBezier(cameraInfo, bezier, Colors.Orange, segment.Info.m_halfWidth * 2);
            }
        }
    }
}
