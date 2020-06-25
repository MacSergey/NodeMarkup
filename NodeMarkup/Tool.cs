using ColossalFramework;
using ColossalFramework.Math;
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
using NodeMarkup.Manager;

namespace NodeMarkup
{
    public class NodeMarkupTool : ToolBase
    {
        private Mode ToolMode { get; set; } = Mode.SelectNode;

        private Ray _mouseRay;
        private float _mouseRayLength;
        private bool _mouseRayValid;
        private Vector3 _mousePosition;

        ushort HoverNodeId { get; set; } = 0;
        ushort SelectNodeId { get; set; } = 0;
        MarkupPoint HoverPoint { get; set; } = null;
        MarkupPoint SelectPoint { get; set; } = null;

        bool IsHoverNode => HoverNodeId != 0;
        bool IsSelectNode => SelectNodeId != 0;
        bool IsHoverPoint => HoverPoint != null;
        bool IsSelectPoint => SelectPoint != null;

        Color32 hoverColor = new Color32(51, 181, 229, 224);
        Color32 whiteColor = new Color32(255, 255, 255, 128);
        Color32[] LinePointColors { get; } = new Color32[]
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

        private NetManager NetManager => Singleton<NetManager>.instance;
        private RenderManager RenderManager => Singleton<RenderManager>.instance;

        public ToolBase CurrentTool => ToolsModifierControl.toolController?.CurrentTool;
        public bool ToolEnabled => CurrentTool == this;

        Button Button => Button.Instace;
        NodeMarkupPanel Panel => NodeMarkupPanel.Instance;

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
            Button.CreateButton();
            NodeMarkupPanel.CreatePanel();

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
            Panel?.Hide();
            Destroy(Panel);
            base.OnDestroy();
        }
        protected override void OnEnable()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(OnEnable)}");
            base.OnEnable();
            Button?.Activate();
            Panel?.Hide();
            Reset();
        }
        protected override void OnDisable()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(OnDisable)}");
            base.OnDisable();
            Button?.Deactivate();
            Panel?.Hide();
            Reset();
        }
        private void Reset()
        {
            HoverNodeId = 0;
            SelectNodeId = 0;
            ToolMode = Mode.SelectNode;
        }

        protected override void OnToolUpdate()
        {
            //Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(OnToolUpdate)}");

            switch (ToolMode)
            {
                case Mode.SelectNode:
                    GetHoveredNode();
                    break;
                case Mode.ConnectLine:
                    GetHoverPoint();
                    break;
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

        private void GetHoveredNode()
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
                    HoverNodeId = output.m_netNode;
                    return;
                }
            }

            HoverNodeId = 0;
        }
        private void GetHoverPoint()
        {
            if (!UIView.IsInsideUI() && Cursor.visible)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                var markup = NodeMarkupManager.Get(SelectNodeId);
                foreach (var enter in markup.Enters)
                {
                    foreach (var point in enter.Points)
                    {
                        if (point.IsIntersect(ray) && (!IsSelectPoint || point.Enter != SelectPoint.Enter))
                        {
                            HoverPoint = point;
                            return;
                        }
                    }
                }
            }

            HoverPoint = null;
        }
        protected override void OnToolGUI(Event e)
        {
            base.OnToolGUI(e);
            if (e.type == EventType.MouseUp && _mouseRayValid)
            {
                if (e.button == 0)
                    OnPrimaryMouseClicked();
                else if (e.button == 1)
                    OnSecondaryMouseClicked();
            }
        }
        protected override void OnToolLateUpdate()
        {
            base.OnToolUpdate();
            _mousePosition = Input.mousePosition;
            _mouseRay = Camera.main.ScreenPointToRay(_mousePosition);
            _mouseRayLength = Camera.main.farClipPlane;
            _mouseRayValid = !UIView.IsInsideUI() && Cursor.visible;
        }
        private void OnPrimaryMouseClicked()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(OnPrimaryMouseClicked)}");

            switch (ToolMode)
            {
                case Mode.SelectNode when IsHoverNode:
                    SelectNodeId = HoverNodeId;
                    ToolMode = Mode.ConnectLine;
                    Panel.SetNode(SelectNodeId);
                    break;
                case Mode.ConnectLine when IsHoverPoint && !IsSelectPoint:
                    SelectPoint = HoverPoint;
                    break;
                case Mode.ConnectLine when IsHoverPoint && IsSelectPoint:
                    var markup = NodeMarkupManager.Get(SelectNodeId);
                    markup.ToggleConnection(new MarkupPointPair(SelectPoint, HoverPoint));
                    SelectPoint = null;
                    break;
            }
        }
        private void OnSecondaryMouseClicked()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(OnSecondaryMouseClicked)}");

            switch (ToolMode)
            {
                case Mode.ConnectLine when IsSelectPoint:
                    SelectPoint = null;
                    break;
                case Mode.ConnectLine when !IsSelectPoint:
                    ToolMode = Mode.SelectNode;
                    SelectNodeId = 0;
                    Panel?.Hide();
                    break;
                case Mode.SelectNode:
                    DisableTool();
                    break;
            }
        }


        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            //Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(RenderOverlay)}");

            base.RenderOverlay(cameraInfo);

            switch (ToolMode)
            {
                case Mode.SelectNode when IsHoverNode:
                    var node = Utilities.GetNode(HoverNodeId);
                    RenderManager.OverlayEffect.DrawCircle(cameraInfo, hoverColor, node.m_position, Mathf.Max(6f, node.Info.m_halfWidth * 2f), -1f, 1280f, false, true);
                    break;
                case Mode.ConnectLine:
                    if (IsHoverPoint)
                        RenderManager.OverlayEffect.DrawCircle(cameraInfo, Color.white, HoverPoint.Position, 0.5f, -1f, 1280f, false, true);

                    RenderPointOverlay(cameraInfo, SelectPoint?.Enter);
                    RenderConnectLineOverlay(cameraInfo);
                    break;
            }
        }
        private void RenderPointOverlay(RenderManager.CameraInfo cameraInfo, SegmentEnter ignore = null)
        {
            var markup = NodeMarkupManager.Get(SelectNodeId);
            foreach (var enter in markup.Enters.Where(m => m != ignore))
            {
                
                for (var i = 0; i < enter.Points.Length; i += 1)
                {
                    RenderManager.OverlayEffect.DrawCircle(cameraInfo, LinePointColors[i % LinePointColors.Length], enter.Points[i].Position, 1f, -1f, 1280f, false, true);
                }
            }
        }
        private void RenderConnectLineOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (!IsSelectPoint)
                return;

            var bezier = new Bezier3();
            Color color;

            if (IsHoverPoint)
            {
                var markup = NodeMarkupManager.Get(SelectNodeId);
                var pointPair = new MarkupPointPair(SelectPoint, HoverPoint);
                color = markup.ExistConnection(pointPair) ? Color.red : Color.green;

                bezier.a = SelectPoint.Position;
                bezier.b = SelectPoint.Direction;
                bezier.c = HoverPoint.Direction;
                bezier.d = HoverPoint.Position;
            }
            else
            {
                color = Color.white;

                RaycastInput input = new RaycastInput(_mouseRay, _mouseRayLength);
                RayCast(input, out RaycastOutput output);

                bezier.a = SelectPoint.Position;
                bezier.b = SelectPoint.Direction;
                bezier.c = SelectPoint.Direction.Turn90(true);
                bezier.d = output.m_hitPos;

                Line2.Intersect(VectorUtils.XZ(bezier.a), VectorUtils.XZ(bezier.a + bezier.b), VectorUtils.XZ(bezier.d), VectorUtils.XZ(bezier.d + bezier.c), out _, out float v);
                bezier.c = v >= 0 ? bezier.c : -bezier.c;
            }

            NetSegment.CalculateMiddlePoints(bezier.a, bezier.b, bezier.d, bezier.c, true, true, out bezier.b, out bezier.c);
            RenderManager.OverlayEffect.DrawBezier(cameraInfo, color, bezier, 0.5f, 0.5f, 0.5f, -1f, 1280f, false, true);
        }

        enum Mode
        {
            SelectNode,
            ConnectLine
        }
    }
}
