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
using ICities;
using ColossalFramework.PlatformServices;

namespace NodeMarkup
{
    public class NodeMarkupTool : ToolBase
    {
        public static SavedInputKey ActivationShortcut { get; } = new SavedInputKey(nameof(ActivationShortcut), UI.Settings.SettingsFile, SavedInputKey.Encode(KeyCode.L, true, false, false), true);
        public static SavedInputKey DeleteAllShortcut { get; } = new SavedInputKey(nameof(DeleteAllShortcut), UI.Settings.SettingsFile, SavedInputKey.Encode(KeyCode.D, true, true, false), true);
        public static SavedInputKey AddRuleShortcut { get; } = new SavedInputKey(nameof(AddRuleShortcut), UI.Settings.SettingsFile, SavedInputKey.Encode(KeyCode.A, true, true, false), true);
        public static SavedInputKey AddFillerShortcut { get; } = new SavedInputKey(nameof(AddFillerShortcut), UI.Settings.SettingsFile, SavedInputKey.Encode(KeyCode.F, true, true, false), true);
        public static bool AltIsPressed => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        public static bool ShiftIsPressed => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        public static bool CtrlIsPressed => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        private Mode ToolMode { get; set; } = Mode.SelectNode;

        public static Ray MouseRay { get; private set; }
        public static float MouseRayLength { get; private set; }
        public static bool MouseRayValid { get; private set; }
        public static Vector3 MousePosition { get; private set; }
        public static Vector3 MouseWorldPosition { get; private set; }

        Markup EditMarkup { get; set; }

        ushort HoverNodeId { get; set; } = 0;
        ushort SelectNodeId { get; set; } = 0;
        MarkupPoint HoverPoint { get; set; } = null;
        MarkupPoint SelectPoint { get; set; } = null;
        List<MarkupPoint> TargetPoints { get; set; } = new List<MarkupPoint>();
        MarkupPoint DragPoint { get; set; } = null;

        bool IsHoverNode => HoverNodeId != 0;
        bool IsSelectNode => SelectNodeId != 0;
        bool IsHoverPoint => HoverPoint != null;
        bool IsSelectPoint => SelectPoint != null;

        MarkupFiller TempFiller { get; set; }
        public List<IFillerVertex> FillerPoints { get; } = new List<IFillerVertex>();
        private IFillerVertex HoverFillerPoint { get; set; }
        private bool IsHoverFillerPoint => HoverFillerPoint != null;

        Color32 HoverColor { get; } = new Color32(255, 136, 0, 224);

        public static RenderManager RenderManager => Singleton<RenderManager>.instance;

        NodeMarkupButton Button => NodeMarkupButton.Instance;
        NodeMarkupPanel Panel => NodeMarkupPanel.Instance;
        private ToolBase PrevTool { get; set; }
        UIComponent PauseMenu { get; } = UIView.library.Get("PauseMenu");

        private bool DisableByAlt { get; set; }

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
            EditMarkup = null;
            HoverNodeId = 0;
            SelectNodeId = 0;
            HoverPoint = null;
            SelectPoint = null;
            TargetPoints.Clear();
            DragPoint = null;
            FillerPoints.Clear();
            HoverFillerPoint = null;
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

            MousePosition = Input.mousePosition;
            MouseRay = Camera.main.ScreenPointToRay(MousePosition);
            MouseRayLength = Camera.main.farClipPlane;
            MouseRayValid = !UIView.IsInsideUI() && Cursor.visible;
            RaycastInput input = new RaycastInput(MouseRay, MouseRayLength);
            RayCast(input, out RaycastOutput output);
            MouseWorldPosition = output.m_hitPos;

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
                case Mode.SelectFiller:
                    GetHoverFillerPoint();
                    break;
            }

            Info();

            base.OnToolUpdate();
        }

        private void GetHoveredNode()
        {
            if (MouseRayValid)
            {
                RaycastInput input = new RaycastInput(MouseRay, Camera.main.farClipPlane)
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
            if (MouseRayValid)
            {
                foreach (var point in TargetPoints)
                {
                    if (point.IsIntersect(MouseRay) && (!IsSelectPoint || point != SelectPoint))
                    {
                        HoverPoint = point;
                        return;
                    }
                }
            }

            if (IsSelectPoint)
            {
                var connectLine = MouseWorldPosition - SelectPoint.Position;
                if (connectLine.magnitude >= 5 && Vector3.Angle(SelectPoint.Direction, connectLine) <= 3 && SelectPoint.Enter.TryGetPoint(SelectPoint.Num, MarkupPoint.PointType.Normal, out MarkupPoint normalPoint))
                {
                    HoverPoint = normalPoint;
                    return;
                }
            }

            HoverPoint = null;
        }
        private void GetHoverFillerPoint()
        {
            if (MouseRayValid)
            {
                foreach (var supportPoint in FillerPoints)
                {
                    if (supportPoint.IsIntersect(MouseRay))
                    {
                        HoverFillerPoint = supportPoint;
                        return;
                    }
                }
            }

            HoverFillerPoint = null;
        }

        private void Info()
        {
            var position = GetInfoPosition();

            if (!UI.Settings.ShowToolTip || (Panel.isVisible && new Rect(Panel.relativePosition, Panel.size).Contains(position)))
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
                        ShowToolInfo(pointPair.IsSomeEnter ? (pointPair.IsNormal ? Localize.Tool_InfoDeleteNormalLine : Localize.Tool_InfoDeleteStopLine) : Localize.Tool_InfoDeleteLine, position);
                    else
                        ShowToolInfo(pointPair.IsSomeEnter ? (pointPair.IsNormal ? Localize.Tool_InfoCreateNormalLine : Localize.Tool_InfoCreateStopLine) : Localize.Tool_InfoCreateLine, position);
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
                case Mode.SelectFiller when IsHoverFillerPoint && TempFiller.IsEmpty:
                    ShowToolInfo(Localize.Tool_InfoFillerClickStart, position);
                    break;
                case Mode.SelectFiller when IsHoverFillerPoint && HoverFillerPoint == TempFiller.First:
                    ShowToolInfo(Localize.Tool_InfoFillerClickEnd, position);
                    break;
                case Mode.SelectFiller when IsHoverFillerPoint:
                    ShowToolInfo(Localize.Tool_InfoFillerClickNext, position);
                    break;
                case Mode.SelectFiller when TempFiller.IsEmpty:
                    ShowToolInfo(Localize.Tool_InfoFillerSelectStart, position);
                    break;
                case Mode.SelectFiller:
                    ShowToolInfo(Localize.Tool_InfoFillerSelectNext, position);
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

            relativePosition += new Vector3(25, 25);

            var screenSize = fullscreenContainer?.size ?? uIView.GetScreenResolution();
            relativePosition.x = MathPos(relativePosition.x, cursorInfoLabel.width, screenSize.x);
            relativePosition.y = MathPos(relativePosition.y, cursorInfoLabel.height, screenSize.y);

            cursorInfoLabel.relativePosition = relativePosition;

            float MathPos(float pos, float size, float screen) => pos + size > screen ? (screen - size < 0 ? 0 : screen - size) : Mathf.Max(pos, 0);
        }
        private Vector3 GetInfoPosition()
        {
            var uiView = cursorInfoLabel.GetUIView();
            var mouse = uiView.ScreenPointToGUI(MousePosition / uiView.inputScale);

            return mouse;
        }


        #endregion

        #region GUI

        protected override void OnToolGUI(Event e)
        {
            switch (e.type)
            {
                case EventType.MouseDown when MouseRayValid && e.button == 0:
                    OnMouseDown(e);
                    break;
                case EventType.MouseDrag when MouseRayValid:
                    OnMouseDrag(e);
                    break;
                case EventType.MouseUp when MouseRayValid && e.button == 0:
                    OnPrimaryMouseClicked(e);
                    break;
                case EventType.MouseUp when MouseRayValid && e.button == 1:
                    OnSecondaryMouseClicked();
                    break;
                default:
                    ProcessShortcuts(e);
                    break;
            }

            base.OnToolGUI(e);
        }
        private void OnMouseDown(Event e)
        {
            if (ToolMode == Mode.ConnectLine && !IsSelectPoint && IsHoverPoint && CtrlIsPressed)
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
            var normal = point.Enter.CornerDir.Turn90(true);

            Line2.Intersect(point.Position.XZ(), (point.Position + point.Enter.CornerDir).XZ(), MouseWorldPosition.XZ(), (MouseWorldPosition + normal).XZ(), out float offsetChange, out _);

            point.Offset = (point.Offset + offsetChange * Mathf.Sin(point.Enter.CornerAndNormalAngle)).RoundToNearest(0.01f);
        }
        private void ProcessShortcuts(Event e)
        {
            switch (ToolMode)
            {
                case Mode.ConnectLine when !IsSelectPoint && AltIsPressed:
                    DisableByAlt = true;
                    EnableSelectFiller();
                    break;
                case Mode.ConnectLine when !IsSelectPoint && AddFillerShortcut.IsPressed(e):
                    DisableByAlt = false;
                    EnableSelectFiller();
                    break;
                case Mode.ConnectLine when !IsSelectPoint && DeleteAllShortcut.IsPressed(e):
                    DeleteAllLines();
                    break;
                case Mode.ConnectLine:
                    Panel?.OnEvent(e);
                    break;
                case Mode.SelectFiller when DisableByAlt && !AltIsPressed && TempFiller.IsEmpty:
                    ToolMode = Mode.ConnectLine;
                    TempFiller = null;
                    break;
            }
        }
        private void EnableSelectFiller()
        {
            ToolMode = Mode.SelectFiller;
            TempFiller = new MarkupFiller(EditMarkup, Style.StyleType.FillerStripe);
            GetFillerPoints();
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
                case Mode.SelectFiller:
                    OnSelectFillerPoint(e);
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
            EditMarkup = MarkupManager.Get(SelectNodeId);

            ToolMode = Mode.ConnectLine;
            Panel.SetNode(SelectNodeId);
            SetTarget();
        }
        private void SetTarget(MarkupPoint.PointType pointType = MarkupPoint.PointType.Enter | MarkupPoint.PointType.Crosswalk, MarkupPoint ignore = null)
        {
            TargetPoints.Clear();
            foreach (var enter in EditMarkup.Enters)
            {
                if ((pointType & MarkupPoint.PointType.Enter) == MarkupPoint.PointType.Enter)
                    foreach (var point in enter.Points.Where(p => p != ignore))
                        TargetPoints.Add(point);

                if ((ignore == null || enter == ignore.Enter) && (pointType & MarkupPoint.PointType.Crosswalk) == MarkupPoint.PointType.Crosswalk)
                    foreach (var point in enter.Crosswalks.Where(p => p != ignore))
                        TargetPoints.Add(point);
            }
        }
        private void OnSelectPoint(Event e)
        {
            if (e.shift)
                Panel.EditPoint(HoverPoint);
            else
            {
                SelectPoint = HoverPoint;
                SetTarget(SelectPoint.Type, SelectPoint);
            }
        }
        private void OnMakeLine(Event e)
        {
            var pointPair = new MarkupPointPair(SelectPoint, HoverPoint);
            var lineType = pointPair.IsStopLine ? e.GetStopStyle() : pointPair.IsCrosswalk ? e.GetCrosswalkStyle() : e.GetSimpleStyle();
            var newLine = EditMarkup.ToggleConnection(pointPair, lineType);
            Panel.EditLine(newLine);
            SelectPoint = null;
            SetTarget();
        }
        private void OnSelectFillerPoint(Event e)
        {
            if (IsHoverFillerPoint)
            {
                if (TempFiller.Add(HoverFillerPoint))
                {
                    EditMarkup.AddFiller(TempFiller);
                    Panel.EditFiller(TempFiller);
                    ToolMode = Mode.ConnectLine;
                    return;
                }
                DisableByAlt = false;
                GetFillerPoints();
            }
        }

        private void OnPanelActionPrimaryClick(Event e)
        {
            Panel.OnPrimaryMouseClicked(e, out bool isDone);
            if (isDone)
            {
                Panel.EndPanelAction();
                ToolMode = Mode.ConnectLine;
            }

        }
        private void OnSecondaryMouseClicked()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(OnSecondaryMouseClicked)}");

            switch (ToolMode)
            {
                case Mode.PanelAction:
                    OnPanelActionSecondaryClick();
                    break;
                case Mode.SelectFiller:
                    OnUnselectFillerPoint();
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
            {
                Panel.EndPanelAction();
                ToolMode = Mode.ConnectLine;
            }
        }
        private void OnUnselectFillerPoint()
        {
            if (TempFiller.IsEmpty)
            {
                ToolMode = Mode.ConnectLine;
                TempFiller = null;
            }
            else
            {
                TempFiller.Remove();
                GetFillerPoints();
            }
        }
        private void OnUnselectPoint()
        {
            SelectPoint = null;
            SetTarget();
        }
        private void OnUnselectNode()
        {
            ToolMode = Mode.SelectNode;
            EditMarkup = null;
            SelectNodeId = 0;
            Panel?.Hide();
        }
        private void GetFillerPoints()
        {
            FillerPoints.Clear();
            FillerPoints.AddRange(TempFiller.GetNextСandidates());
        }
        private void DeleteAllLines()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(DeleteAllLines)}");

            if (ToolMode == Mode.ConnectLine && !IsSelectPoint && MarkupManager.TryGetMarkup(SelectNodeId, out Markup markup))
            {
                if (UI.Settings.DeleteWarnings)
                {
                    var messageBox = MessageBoxBase.ShowModal<YesNoMessageBox>();
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
                case Mode.SelectNode:
                    RenderSelectNodeMode(cameraInfo);
                    break;
                case Mode.ConnectLine:
                    RenderConnectLineMode(cameraInfo);
                    break;
                case Mode.PanelAction:
                    RenderPanelActionMode(cameraInfo);
                    break;
                case Mode.DragPoint:
                    RenderDragPointMode(cameraInfo);
                    break;
                case Mode.SelectFiller:
                    RenderSelectFillerMode(cameraInfo);
                    break;
            }

            base.RenderOverlay(cameraInfo);
        }
        private void RenderSelectNodeMode(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHoverNode)
            {
                var node = Utilities.GetNode(HoverNodeId);
                RenderCircle(cameraInfo, HoverColor, node.m_position, Mathf.Max(6f, node.Info.m_halfWidth * 2f));
            }
        }
        private void RenderConnectLineMode(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHoverPoint && HoverPoint.Type != MarkupPoint.PointType.Normal)
                RenderCircle(cameraInfo, Color.white, HoverPoint.Position, 0.5f);

            RenderPointsOverlay(cameraInfo);
            RenderConnectLineOverlay(cameraInfo);
            Panel.Render(cameraInfo);
        }
        private void RenderNodeEnterPointsOverlay(RenderManager.CameraInfo cameraInfo, Enter ignore = null)
        {
            foreach (var enter in EditMarkup.Enters.Where(m => m != ignore))
            {
                foreach (var point in enter.Points)
                {
                    RenderPointOverlay(cameraInfo, point);
                }
            }
        }
        private void RenderPointsOverlay(RenderManager.CameraInfo cameraInfo)
        {
            foreach (var point in TargetPoints)
                RenderPointOverlay(cameraInfo, point);
        }
        private void RenderEnterOverlay(RenderManager.CameraInfo cameraInfo, Enter enter)
        {
            if (enter.Position == null)
                return;

            var bezier = new Bezier3
            {
                a = enter.Position.Value - enter.CornerDir * enter.RoadHalfWidth,
                d = enter.Position.Value + enter.CornerDir * enter.RoadHalfWidth
            };
            NetSegment.CalculateMiddlePoints(bezier.a, enter.CornerDir, bezier.d, -enter.CornerDir, true, true, out bezier.b, out bezier.c);

            RenderBezier(cameraInfo, Color.white, bezier, 2f);
        }
        private void RenderPointOverlay(RenderManager.CameraInfo cameraInfo, MarkupPoint point) => RenderCircle(cameraInfo, point.Color, point.Position, 1f);
        private void RenderConnectLineOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (!IsSelectPoint)
                return;

            switch (IsHoverPoint)
            {
                case true when HoverPoint.Type != MarkupPoint.PointType.Normal:
                    RenderRegularConnectLine(cameraInfo);
                    break;
                case true:
                    RenderNormalConnectLine(cameraInfo);
                    break;
                case false:
                    RenderNotConnectLine(cameraInfo);
                    break;
            }
        }
        private void RenderRegularConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            var bezier = new Bezier3()
            {
                a = SelectPoint.Position,
                b = HoverPoint.Enter == SelectPoint.Enter ? HoverPoint.Position - SelectPoint.Position : SelectPoint.Direction,
                c = HoverPoint.Enter == SelectPoint.Enter ? SelectPoint.Position - HoverPoint.Position : HoverPoint.Direction,
                d = HoverPoint.Position,
            };

            var pointPair = new MarkupPointPair(SelectPoint, HoverPoint);
            var color = EditMarkup.ExistConnection(pointPair) ? Color.red : Color.green;

            NetSegment.CalculateMiddlePoints(bezier.a, bezier.b, bezier.d, bezier.c, true, true, out bezier.b, out bezier.c);
            RenderBezier(cameraInfo, color, bezier);
        }
        private void RenderNormalConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            var pointPair = new MarkupPointPair(SelectPoint, HoverPoint);
            var color = EditMarkup.ExistConnection(pointPair) ? Color.red : Color.blue;

            var lineBezier = new Bezier3()
            {
                a = SelectPoint.Position,
                b = HoverPoint.Position,
                c = SelectPoint.Position,
                d = HoverPoint.Position,
            };
            RenderBezier(cameraInfo, color, lineBezier);

            var normal = SelectPoint.Direction.Turn90(false);
            var p1Bezier = new Bezier3()
            {
                a = SelectPoint.Position + normal * 2,
                d = SelectPoint.Position + normal * 2 + SelectPoint.Direction * 2
            };
            p1Bezier.b = p1Bezier.d;
            p1Bezier.c = p1Bezier.a;
            RenderBezier(cameraInfo, color, p1Bezier, 0.2f);

            var p2Bezier = new Bezier3()
            {
                a = SelectPoint.Position + SelectPoint.Direction * 2,
                d = SelectPoint.Position + normal * 2 + SelectPoint.Direction * 2
            };
            p2Bezier.b = p2Bezier.d;
            p2Bezier.c = p2Bezier.a;
            RenderBezier(cameraInfo, color, p2Bezier, 0.2f);
        }
        private void RenderNotConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            var bezier = new Bezier3()
            {
                a = SelectPoint.Position,
                b = SelectPoint.Direction,
                c = SelectPoint.Direction.Turn90(true),
                d = MouseWorldPosition,
            };

            Line2.Intersect(VectorUtils.XZ(bezier.a), VectorUtils.XZ(bezier.a + bezier.b), VectorUtils.XZ(bezier.d), VectorUtils.XZ(bezier.d + bezier.c), out _, out float v);
            bezier.c = v >= 0 ? bezier.c : -bezier.c;

            NetSegment.CalculateMiddlePoints(bezier.a, bezier.b, bezier.d, bezier.c, true, true, out bezier.b, out bezier.c);
            RenderBezier(cameraInfo, Color.white, bezier);
        }


        private void RenderPanelActionMode(RenderManager.CameraInfo cameraInfo) => Panel.Render(cameraInfo);
        private void RenderDragPointMode(RenderManager.CameraInfo cameraInfo)
        {
            RenderEnterOverlay(cameraInfo, DragPoint.Enter);
            RenderPointOverlay(cameraInfo, DragPoint);
        }
        private void RenderSelectFillerMode(RenderManager.CameraInfo cameraInfo)
        {
            RenderFillerLines(cameraInfo);
            RenderFillerBounds(cameraInfo);
            RenderFillerConnectLine(cameraInfo);
            if (IsHoverFillerPoint)
                RenderCircle(cameraInfo, Color.white, HoverFillerPoint.Position, 1f);
        }
        private void RenderFillerLines(RenderManager.CameraInfo cameraInfo)
        {
            var color = IsHoverFillerPoint && HoverFillerPoint.Equals(TempFiller.First) ? Color.green : Color.white;
            foreach (var trajectory in TempFiller.Trajectories)
                RenderBezier(cameraInfo, color, trajectory);
        }
        private void RenderFillerBounds(RenderManager.CameraInfo cameraInfo)
        {
            foreach (var supportPoint in FillerPoints)
                RenderCircle(cameraInfo, Color.red, supportPoint.Position, 0.5f);
        }
        private void RenderFillerConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            if (TempFiller.IsEmpty)
                return;

            Bezier3 bezier;
            Color color;

            if (IsHoverFillerPoint)
            {
                var linePart = TempFiller.GetFillerLine(TempFiller.Last, HoverFillerPoint);
                if (!linePart.GetTrajectory(out bezier))
                    return;

                color = Color.green;
            }
            else
            {
                bezier.a = TempFiller.Last.Position;
                bezier.b = MouseWorldPosition;
                bezier.c = TempFiller.Last.Position;
                bezier.d = MouseWorldPosition;

                color = Color.white;
            }

            RenderBezier(cameraInfo, color, bezier);
        }

        private void RenderBezier(RenderManager.CameraInfo cameraInfo, Color color, Bezier3 bezier, float width = 0.5f) =>
            RenderManager.OverlayEffect.DrawBezier(cameraInfo, color, bezier, width, 0f, 0f, -1f, 1280f, false, true);
        private void RenderCircle(RenderManager.CameraInfo cameraInfo, Color color, Vector3 position, float width) =>
            RenderManager.OverlayEffect.DrawCircle(cameraInfo, color, position, width, -1f, 1280f, false, true);

        #endregion

        enum Mode
        {
            SelectNode,
            ConnectLine,
            SelectFiller,
            PanelAction,
            DragPoint
        }
        public static new bool RayCast(RaycastInput input, out RaycastOutput output) => ToolBase.RayCast(input, out output);
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
