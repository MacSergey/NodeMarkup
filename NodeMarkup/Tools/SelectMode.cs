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
            NodeSelection nodeSelection = HoverNode;
            SegmentSelection segmentSelection = HoverSegment;

            if (NodeMarkupTool.MouseRayValid)
            {
                if (!GetRayCast(ItemClass.Service.Road, ItemClass.SubService.None, ref nodeSelection, ref segmentSelection))
                    GetRayCast(ItemClass.Service.PublicTransport, ItemClass.SubService.PublicTransportPlane, ref nodeSelection, ref segmentSelection);
            }

            HoverNode = nodeSelection;
            HoverSegment = segmentSelection;
        }
        private bool GetRayCast(ItemClass.Service service, ItemClass.SubService subService, ref NodeSelection nodeSelection, ref SegmentSelection segmentSelection)
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

                    if (CheckNodeHover(segment.m_startNode, output.m_hitPos, ref nodeSelection))
                    {
                        segmentSelection = null;
                        return true;
                    }
                    else if (CheckNodeHover(segment.m_endNode, output.m_hitPos, ref nodeSelection))
                    {
                        segmentSelection = null;
                        return true;
                    }
                }

                nodeSelection = null;
                segmentSelection = new SegmentSelection(output.m_netSegment);
                return true;
            }
            else
            {
                nodeSelection = null;
                segmentSelection = null;
                return false;
            }
        }
        private bool CheckNodeHover(ushort nodeId, Vector3 hitPos, ref NodeSelection nodeSelection)
        {
            var selection = nodeSelection;
            if (selection?.Id != nodeId)
                selection = new NodeSelection(nodeId);

            if (selection.Contains(hitPos))
            {
                nodeSelection = selection;
                return true;
            }
            else
                return false;
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

    public abstract class Selection : IOverlay
    {
        protected static float OverlayWidth => 2f;

        public ushort Id { get; }
        protected Data[] Datas { get; }
        private IEnumerable<ITrajectory> BorderLines
        {
            get
            {
                for (var i = 0; i < Datas.Length; i += 1)
                {
                    yield return new StraightTrajectory(Datas[i].leftPos, Datas[i].rightPos);
                    var j = (i + 1) % Datas.Length;
                    yield return new BezierTrajectory(Datas[i].leftPos, Datas[i].leftDir, Datas[j].rightPos, Datas[j].rightDir);
                }
            }
        }
        public Selection(ushort id)
        {
            Id = id;
            Datas = Calculate().OrderBy(s => s.angle).ToArray();
        }
        protected abstract IEnumerable<Data> Calculate();
        public bool Contains(Vector3 position)
        {
            var node = Id.GetNode();

            if ((node.m_flags & NetNode.Flags.Middle) != 0 && (position - node.m_position).magnitude < node.Info.m_halfWidth)
                return true;
            else
            {
                var line = new StraightTrajectory(position, node.m_position);
                var contains = !BorderLines.Any(b => MarkupIntersect.CalculateSingle(line, b).IsIntersect);
                return contains;
            }
        }

        public abstract void Render(OverlayData overlayData);

        protected void Render(OverlayData overlayData, Data data1, Data data2)
        {
            var count = Math.Max(Mathf.CeilToInt(2 * data1.halfWidth / OverlayWidth), Mathf.CeilToInt(2 * data2.halfWidth / OverlayWidth));

            var step1 = data1.GetStep(count);
            var step2 = data2.GetStep(count);

            var ratio1 = data1.Ratio;
            var ratio2 = data2.Ratio;

            var cornerDir1 = data1.CornerDir;
            var cornerDir2 = data2.CornerDir;

            for (var l = 0; l < count; l += 1)
            {
                var bezier = new Bezier3()
                {
                    a = data1.leftPos + cornerDir1 * (OverlayWidth / 2 + l * step1) * ratio1,
                    b = data1.dir,
                    c = data2.dir,
                    d = data2.rightPos - cornerDir2 * (OverlayWidth / 2 + l * step2) * ratio2,
                };

                NetSegment.CalculateMiddlePoints(bezier.a, bezier.b, bezier.d, bezier.c, true, true, out bezier.b, out bezier.c);
                NodeMarkupTool.RenderBezier(bezier, overlayData);
            }
        }
        protected void Render(OverlayData overlayData, Data data)
        {
            var cornerDir = data.CornerDir * data.Ratio * (OverlayWidth / 2);
            var line = new StraightTrajectory(data.leftPos + cornerDir, data.rightPos - cornerDir);
            line.Render(overlayData);
        }

        protected struct Data
        {
            public float angle;
            public Vector3 rightPos;
            public Vector3 leftPos;
            public Vector3 rightDir;
            public Vector3 leftDir;
            public Vector3 dir;
            public float halfWidth;

            public float Ratio => (rightPos - leftPos).XZ().magnitude / (2 * halfWidth);
            public Vector3 CornerDir => (rightPos - leftPos).normalized;

            public float GetStep(int count) => (2 * halfWidth - OverlayWidth) / (count - 1);
        }
    }
    public class NodeSelection : Selection
    {
        public NodeSelection(ushort id) : base(id) { }

        protected override IEnumerable<Data> Calculate()
        {
            var node = Id.GetNode();

            foreach (var segmentId in node.SegmentsId())
            {
                var segment = segmentId.GetSegment();
                var isStart = segment.m_startNode == Id;
                var data = new Data()
                {
                    halfWidth = segment.Info.m_halfWidth.RoundToNearest(0.1f),
                    dir = (isStart ? -segment.m_startDirection : -segment.m_endDirection).normalized,
                };
                data.angle = (-data.dir).AbsoluteAngle();

                segment.CalculateCorner(segmentId, true, isStart, true, out data.leftPos, out data.leftDir, out _);
                segment.CalculateCorner(segmentId, true, isStart, false, out data.rightPos, out data.rightDir, out _);

                data.leftDir *= -1;
                data.rightDir *= -1;

                yield return data;
            }
        }

        public override void Render(OverlayData overlayData)
        {
            var node = Id.GetNode();

            if ((node.m_flags & NetNode.Flags.Middle) != 0)
            {
                overlayData.Width = node.Info.m_halfWidth * 2f;
                NodeMarkupTool.RenderCircle(node.m_position, overlayData);
            }
            else
            {
                overlayData.Width = OverlayWidth;
                overlayData.AlphaBlend = false;

                for (var i = 0; i < Datas.Length; i += 1)
                {
                    var data1 = Datas[i];
                    var data2 = Datas[(i + 1) % Datas.Length];

                    Render(overlayData, data1, data2);
                    Render(overlayData, data1);
                }
            }
        }
    }
    public class SegmentSelection : Selection
    {
        public SegmentSelection(ushort id) : base(id) { }

        protected override IEnumerable<Data> Calculate()
        {
            var segment = Id.GetSegment();

            var startData = new Data()
            {
                halfWidth = segment.Info.m_halfWidth.RoundToNearest(0.1f),
                dir = segment.m_startDirection.normalized,
                angle = segment.m_startDirection.AbsoluteAngle(),
            };

            segment.CalculateCorner(Id, true, true, true, out startData.leftPos, out startData.leftDir, out _);
            segment.CalculateCorner(Id, true, true, false, out startData.rightPos, out startData.rightDir, out _);

            yield return startData;

            var endData = new Data()
            {
                halfWidth = segment.Info.m_halfWidth.RoundToNearest(0.1f),
                dir = segment.m_endDirection.normalized,
                angle = segment.m_endDirection.AbsoluteAngle(),
            };

            segment.CalculateCorner(Id, true, false, true, out endData.leftPos, out endData.leftDir, out _);
            segment.CalculateCorner(Id, true, false, false, out endData.rightPos, out endData.rightDir, out _);

            yield return endData;
        }

        public override void Render(OverlayData overlayData)
        {
            overlayData.Width = OverlayWidth;
            overlayData.AlphaBlend = false;

            Render(overlayData, Datas[0], Datas[1]);
            Render(overlayData, Datas[0]);
            Render(overlayData, Datas[1]);
        }
    }
}
