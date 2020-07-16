﻿using ColossalFramework;
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
using ICities;
using ColossalFramework.PlatformServices;

namespace NodeMarkup
{
    public class NodeMarkupTool : ToolBase
    {
        public static SavedInputKey ActivationShortcut { get; } = new SavedInputKey(nameof(ActivationShortcut), UI.Settings.SettingsFile, SavedInputKey.Encode(KeyCode.L, true, false, false), true);
        public static SavedInputKey DeleteAllShortcut { get; } = new SavedInputKey(nameof(DeleteAllShortcut), UI.Settings.SettingsFile, SavedInputKey.Encode(KeyCode.D, true, true, false), true);
        public static SavedInputKey AddRuleShortcut { get; } = new SavedInputKey(nameof(AddRuleShortcut), UI.Settings.SettingsFile, SavedInputKey.Encode(KeyCode.A, true, true, false), true);

        private Mode ToolMode { get; set; } = Mode.SelectNode;

        private Ray _mouseRay;
        private float _mouseRayLength;
        private bool _mouseRayValid;
        private Vector3 _mousePosition;

        ushort HoverNodeId { get; set; } = 0;
        ushort SelectNodeId { get; set; } = 0;
        MarkupPoint HoverPoint { get; set; } = null;
        MarkupPoint SelectPoint { get; set; } = null;
        MarkupPoint DragPoint { get; set; } = null;

        bool IsHoverNode => HoverNodeId != 0;
        bool IsSelectNode => SelectNodeId != 0;
        bool IsHoverPoint => HoverPoint != null;
        bool IsSelectPoint => SelectPoint != null;

        Color32 HoverColor { get; } = new Color32(255, 136, 0, 224);

        public static RenderManager RenderManager => Singleton<RenderManager>.instance;

        NodeMarkupButton Button => NodeMarkupButton.Instace;
        NodeMarkupPanel Panel => NodeMarkupPanel.Instance;
        private ToolBase PrevTool { get; set; }
        UIComponent PauseMenu { get; } = UIView.library.Get("PauseMenu");


        #region BASIC
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
            base.Awake();

            NodeMarkupButton.CreateButton();
            NodeMarkupPanel.CreatePanel();

            DisableTool();
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
            NodeMarkupButton.RemoveButton();
            NodeMarkupPanel.RemovePanel();
            base.OnDestroy();
        }
        protected override void OnEnable()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(OnEnable)}");
            Button?.Activate();
            Reset();

            PrevTool = m_toolController.CurrentTool;

            base.OnEnable();

            Singleton<InfoManager>.instance.SetCurrentMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.Default);
        }
        protected override void OnDisable()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(OnDisable)}");
            Button?.Deactivate();
            Reset();

            if (m_toolController?.NextTool == null && PrevTool != null)
                PrevTool.enabled = true;

            PrevTool = null;
        }
        private void Reset()
        {
            HoverNodeId = 0;
            SelectNodeId = 0;
            HoverPoint = null;
            SelectPoint = null;
            DragPoint = null;
            ToolMode = Mode.SelectNode;
            cursorInfoLabel.isVisible = false;
            Panel?.EndPanelAction();
            Panel?.Hide();
        }

        public void ToggleTool()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(ToggleTool)}");
            enabled = !enabled;
        }
        public void DisableTool() => enabled = false;

        public void StartPanelAction(out bool isAccept)
        {
            if (ToolMode == Mode.ConnectLine)
            {
                ToolMode = Mode.PanelAction;
                isAccept = true;
            }
            else
                isAccept = false;
        }
        public void EndPanelAction()
        {
            if (ToolMode == Mode.PanelAction)
            {
                Panel.EndPanelAction();
                ToolMode = Mode.ConnectLine;
            }
        }

        #endregion

        #region UPDATE

        protected override void OnToolUpdate()
        {
            if (PauseMenu?.isVisible == true)
            {
                PrevTool = null;
                DisableTool();
                UIView.library.Hide("PauseMenu");
                return;
            }
            if ((RenderManager.CurrentCameraInfo.m_layerMask & (3 << 24)) == 0)
            {
                PrevTool = null;
                DisableTool();
                return;
            }

            _mousePosition = Input.mousePosition;
            _mouseRay = Camera.main.ScreenPointToRay(_mousePosition);
            _mouseRayLength = Camera.main.farClipPlane;
            _mouseRayValid = !UIView.IsInsideUI() && Cursor.visible;

            switch (ToolMode)
            {
                case Mode.SelectNode:
                    GetHoveredNode();
                    break;
                case Mode.ConnectLine:
                    GetHoverPoint();
                    break;
                case Mode.PanelAction:
                    Panel.OnUpdate();
                    break;
            }

            Info();

            base.OnToolUpdate();
        }

        private void GetHoveredNode()
        {
            if (_mouseRayValid)
            {
                RaycastInput input = new RaycastInput(_mouseRay, Camera.main.farClipPlane)
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
            if (_mouseRayValid)
            {
                var markup = MarkupManager.Get(SelectNodeId);
                foreach (var enter in markup.Enters)
                {
                    foreach (var point in enter.Points)
                    {
                        if (point.IsIntersect(_mouseRay) && (!IsSelectPoint || point != SelectPoint))
                        {
                            HoverPoint = point;
                            return;
                        }
                    }
                }
            }

            HoverPoint = null;
        }

        private void Info()
        {
            if (!UI.Settings.ShowToolTip)
            {
                cursorInfoLabel.isVisible = false;
                return;
            }

            var position = GetInfoPosition();

            if (position.x >= Panel.relativePosition.x &&
                position.x <= Panel.relativePosition.x + Panel.width &&
                position.y >= Panel.relativePosition.y &&
                position.y <= Panel.relativePosition.y + Panel.height
                )
            {
                cursorInfoLabel.isVisible = false;
                return;
            }

            switch (ToolMode)
            {
                case Mode.SelectNode when IsHoverNode:
                    ShowToolInfo(string.Format(Localize.Tool_InfoHoverNode, HoverNodeId), position);
                    break;
                case Mode.SelectNode:
                    ShowToolInfo(Localize.Tool_InfoNode, position);
                    break;
                case Mode.ConnectLine when IsSelectPoint && IsHoverPoint:
                    var markup = MarkupManager.Get(SelectNodeId);
                    var pointPair = new MarkupPointPair(SelectPoint, HoverPoint);
                    if (markup.ExistConnection(pointPair))
                        ShowToolInfo(pointPair.IsSomeEnter ? Localize.Tool_InfoDeleteStopLine : Localize.Tool_InfoDeleteLine, position);
                    else
                        ShowToolInfo(pointPair.IsSomeEnter ? Localize.Tool_InfoCreateStopLine : Localize.Tool_InfoCreateLine, position);
                    break;
                case Mode.ConnectLine when IsSelectPoint:
                    ShowToolInfo(Localize.Tool_InfoSelectEndPoint, position);
                    break;
                case Mode.ConnectLine:
                    ShowToolInfo(Localize.Tool_InfoSelectStartPoint, position);
                    break;
                case Mode.PanelAction when Panel.GetInfo() is string panelInfo && !string.IsNullOrEmpty(panelInfo):
                    ShowToolInfo(panelInfo, position);
                    break;
                default:
                    cursorInfoLabel.isVisible = false;
                    break;
            }
        }
        private void ShowToolInfo(string text, Vector3 relativePosition)
        {
            if (cursorInfoLabel == null)
                return;

            cursorInfoLabel.isVisible = true;
            cursorInfoLabel.text = text ?? string.Empty;

            UIView uIView = cursorInfoLabel.GetUIView();

            var cursorPosition = cursorInfoLabel.pivot.UpperLeftToTransform(cursorInfoLabel.size, cursorInfoLabel.arbitraryPivotOffset);
            relativePosition += new Vector3(cursorPosition.x, cursorPosition.y);

            var screenSize = fullscreenContainer?.size ?? uIView.GetScreenResolution();
            relativePosition.x = MathPos(relativePosition.x, cursorInfoLabel.width, screenSize.x);
            relativePosition.y = MathPos(relativePosition.y, cursorInfoLabel.height, screenSize.y);

            cursorInfoLabel.relativePosition = relativePosition;

            float MathPos(float pos, float size, float screen) => pos + size > screen ? (screen - size < 0 ? 0 : screen - size) : pos;
        }
        private Vector3 GetInfoPosition()
        {
            RaycastInput input = new RaycastInput(_mouseRay, _mouseRayLength)
            {
                m_ignoreTerrain = false,
                m_ignoreNodeFlags = NetNode.Flags.None
            };
            RayCast(input, out RaycastOutput output);

            UIView uIView = cursorInfoLabel.GetUIView();
            var screenPoint = Camera.main.WorldToScreenPoint(output.m_hitPos) / uIView.inputScale;
            var relativePosition = uIView.ScreenPointToGUI(screenPoint);

            return relativePosition;
        }


        #endregion

        #region GUI

        protected override void OnToolGUI(Event e)
        {
            switch (e.type)
            {
                case EventType.MouseDown when _mouseRayValid && e.button == 0:
                    OnMouseDown(e);
                    break;
                case EventType.MouseDrag when _mouseRayValid:
                    OnMouseDrag(e);
                    break;
                case EventType.MouseUp when _mouseRayValid && e.button == 0:
                    OnPrimaryMouseClicked(e);
                    break;
                case EventType.MouseUp when _mouseRayValid && e.button == 1:
                    OnSecondaryMouseClicked();
                    break;
                default:
                    if (DeleteAllShortcut.IsPressed(e))
                    {
                        DeleteAllLines();
                        e.Use();
                    }
                    else
                        Panel?.OnEvent(e);
                    break;
            }

            base.OnToolGUI(e);
        }
        private void OnMouseDown(Event e)
        {
            if (ToolMode == Mode.ConnectLine && !IsSelectPoint && IsHoverPoint && e.control)
            {
                ToolMode = Mode.DragPoint;
                DragPoint = HoverPoint;
            }
        }
        private void OnMouseDrag(Event e)
        {
            if (ToolMode == Mode.DragPoint)
            {
                OnPointDrag(DragPoint);
                Panel.EditPoint(DragPoint);
            }
        }
        private void OnPointDrag(MarkupPoint point)
        {
            RaycastInput input = new RaycastInput(_mouseRay, _mouseRayLength);
            RayCast(input, out RaycastOutput output);

            var normal = point.Enter.CornerDir.Turn90(true);

            Line2.Intersect(VectorUtils.XZ(point.Position), VectorUtils.XZ(point.Position + point.Enter.CornerDir), VectorUtils.XZ(output.m_hitPos), VectorUtils.XZ(output.m_hitPos + normal), out float offsetChange, out _);

            point.Offset = (point.Offset + offsetChange).RoundToNearest(0.01f);
        }

        private void OnPrimaryMouseClicked(Event e)
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(OnPrimaryMouseClicked)}");

            switch (ToolMode)
            {
                case Mode.SelectNode when IsHoverNode:
                    OnSelectNode();
                    break;
                case Mode.ConnectLine when IsHoverPoint && !IsSelectPoint:
                    OnSelectPoint(e);
                    break;
                case Mode.ConnectLine when IsHoverPoint && IsSelectPoint:
                    OnMakeLine(e);
                    break;
                case Mode.PanelAction:
                    OnPanelActionPrimaryClick(e);
                    break;
                case Mode.DragPoint:
                    ToolMode = Mode.ConnectLine;
                    break;
            }
        }
        private void OnSelectNode()
        {
            SelectNodeId = HoverNodeId;
            ToolMode = Mode.ConnectLine;
            Panel.SetNode(SelectNodeId);
        }
        private void OnSelectPoint(Event e)
        {
            if (e.shift)
                Panel.EditPoint(HoverPoint);
            else
                SelectPoint = HoverPoint;
        }
        private void OnMakeLine(Event e)
        {
            var markup = MarkupManager.Get(SelectNodeId);
            var pointPair = new MarkupPointPair(SelectPoint, HoverPoint);
            var lineType = pointPair.IsSomeEnter ? LineStyle.LineType.Stop : e.GetStyle();
            var newLine = markup.ToggleConnection(pointPair, lineType);
            Panel.EditLine(newLine);
            SelectPoint = null;
        }
        private void OnPanelActionPrimaryClick(Event e)
        {
            Panel.OnPrimaryMouseClicked(e, out bool isDone);
            if (isDone)
                ToolMode = Mode.ConnectLine;
        }
        private void OnSecondaryMouseClicked()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(OnSecondaryMouseClicked)}");

            switch (ToolMode)
            {
                case Mode.PanelAction:
                    OnPanelActionSecondaryClick();
                    break;
                case Mode.ConnectLine when IsSelectPoint:
                    OnUnselectPoint();
                    break;
                case Mode.ConnectLine when !IsSelectPoint:
                    OnUnselectNode();
                    break;
                case Mode.SelectNode:
                    DisableTool();
                    break;
            }
        }
        private void OnPanelActionSecondaryClick()
        {
            Panel.OnSecondaryMouseClicked(out bool isDone);
            if (isDone)
                ToolMode = Mode.ConnectLine;
        }
        private void OnUnselectPoint() => SelectPoint = null;
        private void OnUnselectNode()
        {
            ToolMode = Mode.SelectNode;
            SelectNodeId = 0;
            Panel?.Hide();
        }
        private void DeleteAllLines()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(DeleteAllLines)}");

            if (ToolMode == Mode.ConnectLine && !IsSelectPoint && MarkupManager.TryGetMarkup(SelectNodeId, out Markup markup))
            {
                if (UI.Settings.DeleteWarnings)
                {
                    var messageBox = MessageBox.ShowModal<YesNoMessageBox>();
                    messageBox.CaprionText = Localize.Tool_ClearMarkingsCaption;
                    messageBox.MessageText = string.Format(Localize.Tool_ClearMarkingsMessage, SelectNodeId);
                    messageBox.OnButton1Click = Delete;
                }
                else
                    Delete();

                bool Delete()
                {
                    markup.Clear();
                    Panel.UpdatePanel();
                    return true;
                }
            }
        }

        #endregion

        #region Overlay
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            switch (ToolMode)
            {
                case Mode.SelectNode when IsHoverNode:
                    var node = Utilities.GetNode(HoverNodeId);
                    RenderManager.OverlayEffect.DrawCircle(cameraInfo, HoverColor, node.m_position, Mathf.Max(6f, node.Info.m_halfWidth * 2f), -1f, 1280f, false, true);
                    break;
                case Mode.ConnectLine:
                    if (IsHoverPoint)
                        RenderManager.OverlayEffect.DrawCircle(cameraInfo, Color.white, HoverPoint.Position, 0.5f, -1f, 1280f, false, true);

                    //RenderNodeEnterPointsOverlay(cameraInfo, SelectPoint?.Enter);
                    RenderNodeEnterPointsOverlay(cameraInfo, SelectPoint);
                    RenderConnectLineOverlay(cameraInfo);
                    Panel.Render(cameraInfo);
                    break;
                case Mode.PanelAction:
                    Panel.Render(cameraInfo);
                    break;
                case Mode.DragPoint:
                    RenderEnterOverlay(cameraInfo, DragPoint.Enter);
                    RenderPointOverlay(cameraInfo, DragPoint);
                    break;
            }

            base.RenderOverlay(cameraInfo);
        }
        private void RenderNodeEnterPointsOverlay(RenderManager.CameraInfo cameraInfo, Enter ignore = null)
        {
            var markup = MarkupManager.Get(SelectNodeId);
            foreach (var enter in markup.Enters.Where(m => m != ignore))
            {
                foreach (var point in enter.Points)
                {
                    RenderPointOverlay(cameraInfo, point);
                }
            }
        }
        private void RenderNodeEnterPointsOverlay(RenderManager.CameraInfo cameraInfo, MarkupPoint ignore = null)
        {
            var markup = MarkupManager.Get(SelectNodeId);
            foreach (var enter in markup.Enters)
            {
                foreach (var point in enter.Points.Where(p => p != ignore))
                {
                    RenderPointOverlay(cameraInfo, point);
                }
            }
        }
        private void RenderEnterOverlay(RenderManager.CameraInfo cameraInfo, Enter enter)
        {
            var bezier = new Bezier3
            {
                a = enter.Position - enter.CornerDir * enter.RoadHalfWidth,
                d = enter.Position + enter.CornerDir * enter.RoadHalfWidth
            };
            NetSegment.CalculateMiddlePoints(bezier.a, enter.CornerDir, bezier.d, -enter.CornerDir, true, true, out bezier.b, out bezier.c);

            RenderManager.OverlayEffect.DrawBezier(cameraInfo, Color.white, bezier, 2f, 0f, 0f, -1f, 1280f, false, true);
        }
        private void RenderPointOverlay(RenderManager.CameraInfo cameraInfo, MarkupPoint point)
        {
            RenderManager.OverlayEffect.DrawCircle(cameraInfo, point.Color, point.Position, 1f, -1f, 1280f, false, true);
        }
        private void RenderConnectLineOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (!IsSelectPoint)
                return;

            var bezier = new Bezier3();
            Color color;

            if (IsHoverPoint)
            {
                var markup = MarkupManager.Get(SelectNodeId);
                var pointPair = new MarkupPointPair(SelectPoint, HoverPoint);
                color = markup.ExistConnection(pointPair) ? Color.red : Color.green;

                bezier.a = SelectPoint.Position;
                bezier.b = HoverPoint.Enter == SelectPoint.Enter ? HoverPoint.Position - SelectPoint.Position : SelectPoint.Direction;
                bezier.c = HoverPoint.Enter == SelectPoint.Enter ? SelectPoint.Position - HoverPoint.Position : HoverPoint.Direction;
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
            RenderManager.OverlayEffect.DrawBezier(cameraInfo, color, bezier, 0.5f, 0f, 0f, -1f, 1280f, false, true);
        }

        #endregion

        enum Mode
        {
            SelectNode,
            ConnectLine,
            PanelAction,
            DragPoint
        }
    }
    public class ThreadingExtension : ThreadingExtensionBase
    {
        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            if (!UIView.HasModalInput() && !UIView.HasInputFocus() && NodeMarkupTool.ActivationShortcut.IsKeyUp())
                NodeMarkupTool.Instance.ToggleTool();
        }
    }
}
