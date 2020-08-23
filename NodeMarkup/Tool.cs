using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using NodeMarkup.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NodeMarkup.Manager;
using ICities;

namespace NodeMarkup
{
    public class NodeMarkupTool : ToolBase
    {
        #region PROPERTIES

        #region STATIC
        public static SavedInputKey ActivationShortcut { get; } = new SavedInputKey(nameof(ActivationShortcut), UI.Settings.SettingsFile, SavedInputKey.Encode(KeyCode.L, true, false, false), true);
        public static SavedInputKey DeleteAllShortcut { get; } = new SavedInputKey(nameof(DeleteAllShortcut), UI.Settings.SettingsFile, SavedInputKey.Encode(KeyCode.D, true, true, false), true);
        public static SavedInputKey AddRuleShortcut { get; } = new SavedInputKey(nameof(AddRuleShortcut), UI.Settings.SettingsFile, SavedInputKey.Encode(KeyCode.A, true, true, false), true);
        public static SavedInputKey AddFillerShortcut { get; } = new SavedInputKey(nameof(AddFillerShortcut), UI.Settings.SettingsFile, SavedInputKey.Encode(KeyCode.F, true, true, false), true);
        public static bool AltIsPressed => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        public static bool ShiftIsPressed => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        public static bool CtrlIsPressed => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        public static bool OnlyAltIsPressed => AltIsPressed && !ShiftIsPressed && !CtrlIsPressed;
        public static bool OnlyShiftIsPressed => ShiftIsPressed && !AltIsPressed && !CtrlIsPressed;
        public static bool OnlyCtrlIsPressed => CtrlIsPressed && !AltIsPressed && !ShiftIsPressed;

        public static Ray MouseRay { get; private set; }
        public static float MouseRayLength { get; private set; }
        public static bool MouseRayValid { get; private set; }
        public static Vector3 MousePosition { get; private set; }
        public static Vector3 MouseWorldPosition { get; private set; }
        public static Vector3 CameraDirection { get; private set; }

        #endregion

        private Mode ToolMode { get; set; } = Mode.Node;
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
        private PointsSelector<IFillerVertex> FillerPointsSelector { get; set; }

        public static RenderManager RenderManager => Singleton<RenderManager>.instance;

        NodeMarkupButton Button => NodeMarkupButton.Instance;
        NodeMarkupPanel Panel => NodeMarkupPanel.Instance;
        private ToolBase PrevTool { get; set; }
        UIComponent PauseMenu { get; } = UIView.library.Get("PauseMenu");

        private bool DisableByAlt { get; set; }

        #endregion

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
            FillerPointsSelector = null;
            ToolMode = Mode.Node;
            cursorInfoLabel.isVisible = false;
            cursorInfoLabel.text = string.Empty;
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
            if (ToolMode == Mode.Line)
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
                ToolMode = Mode.Line;
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
            CameraDirection = Vector3.forward.TurnDeg(Camera.main.transform.eulerAngles.y, true);

            switch (ToolMode)
            {
                case Mode.Node:
                    GetHoveredNode();
                    break;
                case Mode.Line:
                case Mode.Crosswalk:
                    GetHoverPoint();
                    break;
                case Mode.PanelAction:
                    Panel.OnUpdate();
                    break;
                case Mode.Filler:
                    FillerPointsSelector.OnUpdate();
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
                    if (point.IsHover(MouseRay))
                    {
                        HoverPoint = point;
                        return;
                    }
                }
            }

            if (IsSelectPoint && SelectPoint.Type == MarkupPoint.PointType.Enter)
            {
                var connectLine = MouseWorldPosition - SelectPoint.Position;
                if (connectLine.magnitude >= 2 && 135 <= Vector3.Angle(SelectPoint.Direction.XZ(), connectLine.XZ()) && SelectPoint.Enter.TryGetPoint(SelectPoint.Num, MarkupPoint.PointType.Normal, out MarkupPoint normalPoint))
                {
                    HoverPoint = normalPoint;
                    return;
                }
            }

            HoverPoint = null;
        }

        #region INFO

        private void Info()
        {
            var position = GetInfoPosition();

            if ((!UI.Settings.ShowToolTip && ToolMode != Mode.Node) || (Panel.isVisible && new Rect(Panel.relativePosition, Panel.size).Contains(position)))
            {
                cursorInfoLabel.isVisible = false;
                return;
            }

            switch (ToolMode)
            {
                case Mode.Node when IsHoverNode:
                    ShowToolInfo(string.Format(Localize.Tool_InfoHoverNode, HoverNodeId), position);
                    break;
                case Mode.Node:
                    ShowToolInfo(Localize.Tool_InfoNode, position);
                    break;
                case Mode.Line when IsSelectPoint && IsHoverPoint:
                case Mode.Crosswalk when IsSelectPoint && IsHoverPoint:
                    var markup = MarkupManager.Get(SelectNodeId);
                    var pointPair = new MarkupPointPair(SelectPoint, HoverPoint);
                    var exist = markup.ExistConnection(pointPair);

                    if (pointPair.IsStopLine)
                        ShowToolInfo(exist ? Localize.Tool_InfoDeleteStopLine : Localize.Tool_InfoCreateStopLine, position);
                    else if (pointPair.IsCrosswalk)
                        ShowToolInfo(exist ? Localize.Tool_InfoDeleteCrosswalk : Localize.Tool_InfoCreateCrosswalk, position);
                    else if (pointPair.IsNormal)
                        ShowToolInfo(exist ? Localize.Tool_InfoDeleteNormalLine : Localize.Tool_InfoCreateNormalLine, position);
                    else
                        ShowToolInfo(exist ? Localize.Tool_InfoDeleteLine : Localize.Tool_InfoCreateLine, position);
                    break;
                case Mode.Line when IsSelectPoint:
                    ShowToolInfo(Localize.Tool_InfoSelectLineEndPoint, position);
                    break;
                case Mode.Crosswalk when IsSelectPoint:
                    ShowToolInfo(Localize.Tool_InfoSelectCrosswalkEndPoint, position);
                    break;
                case Mode.Line:
                    ShowToolInfo(Localize.Tool_InfoSelectLineStartPoint, position);
                    break;
                case Mode.Crosswalk:
                    ShowToolInfo(Localize.Tool_InfoSelectCrosswalkStartPoint, position);
                    break;
                case Mode.PanelAction when Panel.GetInfo() is string panelInfo && !string.IsNullOrEmpty(panelInfo):
                    ShowToolInfo(panelInfo, position);
                    break;
                case Mode.Filler when FillerPointsSelector.IsHoverPoint && TempFiller.IsEmpty:
                    ShowToolInfo(Localize.Tool_InfoFillerClickStart, position);
                    break;
                case Mode.Filler when FillerPointsSelector.IsHoverPoint && FillerPointsSelector.HoverPoint == TempFiller.First:
                    ShowToolInfo(Localize.Tool_InfoFillerClickEnd, position);
                    break;
                case Mode.Filler when FillerPointsSelector.IsHoverPoint:
                    ShowToolInfo(Localize.Tool_InfoFillerClickNext, position);
                    break;
                case Mode.Filler when TempFiller.IsEmpty:
                    ShowToolInfo(Localize.Tool_InfoFillerSelectStart, position);
                    break;
                case Mode.Filler:
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

        #endregion

        #region GUI

        protected override void OnToolGUI(Event e)
        {
            if (ProcessShortcuts(e))
                return;

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
            }
        }
        private void GetFillerPoints() => FillerPointsSelector = new PointsSelector<IFillerVertex>(TempFiller.GetNextСandidates(), MarkupColors.Red);

        #region MOUSE DOWN
        private void OnMouseDown(Event e)
        {
            if (ToolMode == Mode.Line && !IsSelectPoint && IsHoverPoint && CtrlIsPressed)
            {
                ToolMode = Mode.DragPoint;
                DragPoint = HoverPoint;
            }
        }
        #endregion

        #region MOUSE DRAG
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
        #endregion

        #region PROCESS SHORTCUTS
        private bool ProcessShortcuts(Event e)
        {
            if((ToolMode == Mode.Line || ToolMode == Mode.Crosswalk) && !IsSelectPoint)
            {
                if(AddFillerShortcut.IsPressed(e))
                {
                    DisableByAlt = false;
                    EnableSelectFiller();
                    return true;
                }
                if(DeleteAllShortcut.IsPressed(e))
                {
                    DeleteAllLines();
                    return true;
                }
                if(Panel?.OnShortcut(e) == true)
                    return true;
            }

            switch (ToolMode)
            {
                case Mode.Line when !IsSelectPoint && OnlyAltIsPressed:
                    DisableByAlt = true;
                    EnableSelectFiller();
                    return true;
                case Mode.Line when !IsSelectPoint && OnlyShiftIsPressed:
                    EnableCrosswalk();
                    return true;
                case Mode.Crosswalk when !IsSelectPoint && !ShiftIsPressed:
                    DisableCrosswalk();
                    return true;
                case Mode.Filler when DisableByAlt && !AltIsPressed && TempFiller.IsEmpty:
                    ToolMode = Mode.Line;
                    TempFiller = null;
                    return true;
            }

            return false;
        }
        private void EnableSelectFiller()
        {
            ToolMode = Mode.Filler;
            TempFiller = new MarkupFiller(EditMarkup, Style.StyleType.FillerStripe);
            GetFillerPoints();
        }
        private void DeleteAllLines()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(DeleteAllLines)}");

            if (ToolMode == Mode.Line && !IsSelectPoint && MarkupManager.TryGetMarkup(SelectNodeId, out Markup markup))
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
        private void EnableCrosswalk()
        {
            ToolMode = Mode.Crosswalk;
            SetTarget(MarkupPoint.PointType.Crosswalk);
        }
        private void DisableCrosswalk()
        {
            ToolMode = Mode.Line;
            SetTarget();
        }

        #endregion

        #region PRIMARY CLICKED

        private void OnPrimaryMouseClicked(Event e)
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(OnPrimaryMouseClicked)}");

            switch (ToolMode)
            {
                case Mode.Node when IsHoverNode:
                    OnSelectNode();
                    break;
                case Mode.Line when IsHoverPoint && !IsSelectPoint:
                case Mode.Crosswalk when IsHoverPoint && !IsSelectPoint:
                    OnSelectPoint(e);
                    break;
                case Mode.Line when IsHoverPoint && IsSelectPoint:
                    OnMakeLine(e);
                    break;
                case Mode.Crosswalk when IsHoverPoint && IsSelectPoint:
                    OnMakeCrosswalk(e);
                    break;
                case Mode.Filler:
                    OnSelectFillerPoint(e);
                    break;
                case Mode.PanelAction:
                    OnPanelActionPrimaryClick(e);
                    break;
                case Mode.DragPoint:
                    Panel.EditPoint(DragPoint);
                    ToolMode = Mode.Line;
                    break;
            }
        }

        #region SET TARGET

        private void SetTarget(MarkupPoint.PointType pointType = MarkupPoint.PointType.Enter, MarkupPoint ignore = null)
        {
            TargetPoints.Clear();
            foreach (var enter in EditMarkup.Enters)
            {
                if ((pointType & MarkupPoint.PointType.Enter) == MarkupPoint.PointType.Enter)
                    SetEnterTarget(enter, ignore);

                if ((pointType & MarkupPoint.PointType.Crosswalk) == MarkupPoint.PointType.Crosswalk)
                    SetCrosswalkTarget(enter, ignore);
            }
        }
        private void SetEnterTarget(Enter enter, MarkupPoint ignore)
        {
            if (ignore == null || ignore.Enter != enter)
            {
                TargetPoints.AddRange(enter.Points.Cast<MarkupPoint>());
                return;
            }

            var allow = enter.Points.Select(i => 1).ToArray();
            var ignoreIdx = ignore.Num - 1;
            var leftIdx = ignoreIdx;
            var rightIdx = ignoreIdx;

            foreach (var line in enter.Markup.Lines.Where(l => l.Type == MarkupLine.LineType.Stop && l.Start.Enter == enter))
            {
                var from = Math.Min(line.Start.Num, line.End.Num) - 1;
                var to = Math.Max(line.Start.Num, line.End.Num) - 1;
                if (from < ignore.Num - 1 && ignore.Num - 1 < to)
                    return;
                allow[from] = 2;
                allow[to] = 2;

                for (var i = from + 1; i <= to - 1; i += 1)
                    allow[i] = 0;

                if (line.ContainsPoint(ignore))
                {
                    var otherIdx = line.PointPair.GetOther(ignore).Num - 1;
                    if (otherIdx < ignoreIdx)
                        leftIdx = otherIdx;
                    else if (otherIdx > ignoreIdx)
                        rightIdx = otherIdx;
                }
            }

            SetNotAllow(allow, leftIdx == ignoreIdx ? Find(allow, ignoreIdx, -1) : leftIdx, -1);
            SetNotAllow(allow, rightIdx == ignoreIdx ? Find(allow, ignoreIdx, 1) : rightIdx, 1);
            allow[ignoreIdx] = 0;

            foreach (var point in enter.Points)
            {
                if (allow[point.Num - 1] != 0)
                    TargetPoints.Add(point);
            }
        }
        private void SetCrosswalkTarget(Enter enter, MarkupPoint ignore)
        {
            if (ignore != null && ignore.Enter != enter)
                return;

            var allow = enter.Crosswalks.Select(i => 1).ToArray();
            var bridge = new Dictionary<MarkupPoint, int>();
            foreach (var crosswalk in enter.Crosswalks)
                bridge.Add(crosswalk, bridge.Count);

            var isIgnore = ignore?.Enter == enter;
            var ignoreIdx = isIgnore ? bridge[ignore] : 0;

            var leftIdx = ignoreIdx;
            var rightIdx = ignoreIdx;

            foreach (var line in enter.Markup.Lines.Where(l => l.Type == MarkupLine.LineType.Crosswalk && l.Start.Enter == enter))
            {
                var from = Math.Min(bridge[line.Start], bridge[line.End]);
                var to = Math.Max(bridge[line.Start], bridge[line.End]);
                allow[from] = 2;
                allow[to] = 2;
                for (var i = from + 1; i <= to - 1; i += 1)
                    allow[i] = 0;

                if (isIgnore && line.ContainsPoint(ignore))
                {
                    var otherIdx = bridge[line.PointPair.GetOther(ignore)];
                    if (otherIdx < ignoreIdx)
                        leftIdx = otherIdx;
                    else if (otherIdx > ignoreIdx)
                        rightIdx = otherIdx;
                }
            }

            if (isIgnore)
            {
                SetNotAllow(allow, leftIdx == ignoreIdx ? Find(allow, ignoreIdx, -1) : leftIdx, -1);
                SetNotAllow(allow, rightIdx == ignoreIdx ? Find(allow, ignoreIdx, 1) : rightIdx, 1);
                allow[ignoreIdx] = 0;
            }

            foreach (var point in bridge)
            {
                if (allow[point.Value] != 0)
                    TargetPoints.Add(point.Key);
            }
        }
        private int Find(int[] allow, int idx, int sign)
        {
            do
                idx += sign;
            while (idx >= 0 && idx < allow.Length && allow[idx] != 2);

            return idx;
        }
        private void SetNotAllow(int[] allow, int idx, int sign)
        {
            idx += sign;
            while (idx >= 0 && idx < allow.Length)
            {
                allow[idx] = 0;
                idx += sign;
            }
        }

        #endregion

        private void OnSelectNode()
        {
            SelectNodeId = HoverNodeId;
            EditMarkup = MarkupManager.Get(SelectNodeId);

            ToolMode = Mode.Line;
            Panel.SetNode(SelectNodeId);
            SetTarget();
        }

        private void OnSelectPoint(Event e)
        {
            SelectPoint = HoverPoint;
            SetTarget(SelectPoint.Type, SelectPoint);
        }

        private void OnMakeLine(Event e)
        {
            var pointPair = new MarkupPointPair(SelectPoint, HoverPoint);

            var lineType = pointPair.IsStopLine ? e.GetStopStyle() : e.GetRegularStyle();
            var newLine = EditMarkup.ToggleConnection(pointPair, lineType);
            Panel.EditLine(newLine);

            SelectPoint = null;
            SetTarget();
        }
        private void OnMakeCrosswalk(Event e)
        {
            var pointPair = new MarkupPointPair(SelectPoint, HoverPoint);

            var newCrosswalkLine = EditMarkup.ToggleConnection(pointPair, e.GetCrosswalkStyle()) as MarkupCrosswalkLine;
            Panel.EditCrosswalk(newCrosswalkLine?.Crosswalk);

            SelectPoint = null;
            SetTarget();
        }

        private void OnSelectFillerPoint(Event e)
        {
            if (FillerPointsSelector.IsHoverPoint)
            {
                if (TempFiller.Add(FillerPointsSelector.HoverPoint))
                {
                    EditMarkup.AddFiller(TempFiller);
                    Panel.EditFiller(TempFiller);
                    ToolMode = Mode.Line;
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
                ToolMode = Mode.Line;
            }

        }

        #endregion

        #region SECONDARY CLICKED

        private void OnSecondaryMouseClicked()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(OnSecondaryMouseClicked)}");

            switch (ToolMode)
            {
                case Mode.PanelAction:
                    OnPanelActionSecondaryClick();
                    break;
                case Mode.Filler:
                    OnUnselectFillerPoint();
                    break;
                case Mode.Crosswalk when IsSelectPoint:

                case Mode.Line when IsSelectPoint:
                    OnUnselectPoint();
                    break;
                case Mode.Line when !IsSelectPoint:
                    OnUnselectNode();
                    break;
                case Mode.Node:
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
                ToolMode = Mode.Line;
            }
        }
        private void OnUnselectFillerPoint()
        {
            if (TempFiller.IsEmpty)
            {
                ToolMode = Mode.Line;
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
        private void OnUnselectCrosswalkPoint()
        {
            ToolMode = Mode.Line;
            SelectPoint = null;
            SetTarget();
        }
        private void OnUnselectNode()
        {
            ToolMode = Mode.Node;
            EditMarkup = null;
            SelectNodeId = 0;
            Panel?.Hide();
        }
        #endregion

        #endregion

        #region OVERLAY

        public static void RenderTrajectory(RenderManager.CameraInfo cameraInfo, Color color, ILineTrajectory trajectory, float width = 0.2f, bool cut = false, bool alphaBlend = true)
        {
            switch (trajectory)
            {
                case BezierTrajectory bezierTrajectory:
                    RenderBezier(cameraInfo, color, bezierTrajectory.Trajectory, width, cut, alphaBlend);
                    break;
                case StraightTrajectory straightTrajectory:
                    RenderBezier(cameraInfo, color, straightTrajectory.Trajectory.GetBezier(), width, cut, alphaBlend);
                    break;
            }
        }
        public static void RenderBezier(RenderManager.CameraInfo cameraInfo, Color color, Bezier3 bezier, float width = 0.2f, bool cut = false, bool alphaBlend = true) =>
            RenderManager.OverlayEffect.DrawBezier(cameraInfo, color, bezier, width, cut ? width / 2 : 0f, cut ? width / 2 : 0f, -1f, 1280f, false, alphaBlend);
        public static void RenderCircle(RenderManager.CameraInfo cameraInfo, Color color, Vector3 position, float width, bool alphaBlend = true) =>
            RenderManager.OverlayEffect.DrawCircle(cameraInfo, color, position, width, -1f, 1280f, false, alphaBlend);

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            switch (ToolMode)
            {
                case Mode.Node:
                    RenderSelectNodeMode(cameraInfo);
                    break;
                case Mode.Line:
                    RenderLineMode(cameraInfo);
                    break;
                case Mode.Crosswalk:
                    RenderCrosswalkMode(cameraInfo);
                    break;
                case Mode.PanelAction:
                    RenderPanelActionMode(cameraInfo);
                    break;
                case Mode.DragPoint:
                    RenderDragPointMode(cameraInfo);
                    break;
                case Mode.Filler:
                    RenderSelectFillerMode(cameraInfo);
                    break;
            }

            base.RenderOverlay(cameraInfo);
        }

        private void RenderPointsOverlay(RenderManager.CameraInfo cameraInfo)
        {
            foreach (var point in TargetPoints)
                RenderPointOverlay(cameraInfo, point);
        }
        public static void RenderPointOverlay(RenderManager.CameraInfo cameraInfo, MarkupPoint point) => RenderPointOverlay(cameraInfo, point, point.Color, 1f);
        public static void RenderPointOverlay(RenderManager.CameraInfo cameraInfo, MarkupPoint point, Color color, float width)
        {
            if (point.Type == MarkupPoint.PointType.Crosswalk)
            {
                var dir = point.Enter.CornerDir.Turn90(true) * MarkupCrosswalkPoint.Shift;
                var bezier = new Line3(point.Position - dir, point.Position + dir).GetBezier();
                RenderBezier(cameraInfo, color, bezier, width);
            }
            else
                RenderCircle(cameraInfo, color, point.Position, width);
        }
        private void RenderEnterOverlay(RenderManager.CameraInfo cameraInfo, Enter enter, Vector3 shift, float width)
        {
            if (enter.Position == null)
                return;

            var bezier = new Line3(enter.Position.Value - enter.CornerDir * enter.RoadHalfWidth + shift, enter.Position.Value + enter.CornerDir * enter.RoadHalfWidth + shift).GetBezier();
            RenderBezier(cameraInfo, MarkupColors.White, bezier, width);
        }

        #region SELECT NODE

        private void RenderSelectNodeMode(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHoverNode)
            {
                var node = Utilities.GetNode(HoverNodeId);
                RenderCircle(cameraInfo, MarkupColors.Orange, node.m_position, Mathf.Max(6f, node.Info.m_halfWidth * 2f));
            }
        }

        #endregion

        #region LINE

        private void RenderLineMode(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHoverPoint)
                RenderPointOverlay(cameraInfo, HoverPoint, MarkupColors.White, 0.5f);

            RenderPointsOverlay(cameraInfo);

            if (IsSelectPoint)
            {
                switch (IsHoverPoint)
                {
                    case true when HoverPoint.Type == MarkupPoint.PointType.Normal:
                        RenderNormalConnectLine(cameraInfo);
                        break;
                    case true:
                        RenderRegularConnectLine(cameraInfo);
                        break;
                    case false:
                        RenderNotConnectLine(cameraInfo);
                        break;
                }
            }

            Panel.Render(cameraInfo);
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
            var color = EditMarkup.ExistConnection(pointPair) ? MarkupColors.Red : MarkupColors.Green;

            NetSegment.CalculateMiddlePoints(bezier.a, bezier.b, bezier.d, bezier.c, true, true, out bezier.b, out bezier.c);
            RenderBezier(cameraInfo, color, bezier);
        }
        private void RenderNormalConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            var pointPair = new MarkupPointPair(SelectPoint, HoverPoint);
            var color = EditMarkup.ExistConnection(pointPair) ? MarkupColors.Red : MarkupColors.Blue;

            var lineBezier = new Bezier3()
            {
                a = SelectPoint.Position,
                b = HoverPoint.Position,
                c = SelectPoint.Position,
                d = HoverPoint.Position,
            };
            RenderBezier(cameraInfo, color, lineBezier);

            var normal = SelectPoint.Direction.Turn90(false);

            var normalBezier = new Bezier3
            {
                a = SelectPoint.Position + SelectPoint.Direction,
                d = SelectPoint.Position + normal
            };
            normalBezier.b = normalBezier.a + normal / 2;
            normalBezier.c = normalBezier.d + SelectPoint.Direction / 2;
            RenderBezier(cameraInfo, color, normalBezier, 2f, true);
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
            RenderBezier(cameraInfo, MarkupColors.White, bezier);
        }

        #endregion

        #region CROSSWALK

        private void RenderCrosswalkMode(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHoverPoint)
                RenderPointOverlay(cameraInfo, HoverPoint, MarkupColors.White, 0.5f);

            RenderPointsOverlay(cameraInfo);

            if (IsSelectPoint)
            {
                if (IsHoverPoint)
                    RenderConnectCrosswalkLine(cameraInfo);
                else
                    RenderNotConnectCrosswalkLine(cameraInfo);
            }
        }
        private void RenderConnectCrosswalkLine(RenderManager.CameraInfo cameraInfo)
        {
            var bezier = new Line3(SelectPoint.Position, HoverPoint.Position).GetBezier();
            var pointPair = new MarkupPointPair(SelectPoint, HoverPoint);
            var color = EditMarkup.ExistConnection(pointPair) ? MarkupColors.Red : MarkupColors.Green;

            RenderBezier(cameraInfo, color, bezier, MarkupCrosswalkPoint.Shift * 2, true);
        }
        private void RenderNotConnectCrosswalkLine(RenderManager.CameraInfo cameraInfo)
        {
            var dir = MouseWorldPosition - SelectPoint.Position;
            var lenght = dir.magnitude;
            dir.Normalize();
            var bezier = new Line3(SelectPoint.Position, SelectPoint.Position + dir * Mathf.Max(lenght, 1f)).GetBezier();

            RenderBezier(cameraInfo, MarkupColors.White, bezier, MarkupCrosswalkPoint.Shift * 2, true);
        }

        #endregion

        #region PANEL ACTION
        private void RenderPanelActionMode(RenderManager.CameraInfo cameraInfo) => Panel.Render(cameraInfo);
        #endregion

        #region DRAG POINT
        private void RenderDragPointMode(RenderManager.CameraInfo cameraInfo)
        {
            if (DragPoint.Type == MarkupPoint.PointType.Crosswalk)
                RenderEnterOverlay(cameraInfo, DragPoint.Enter, DragPoint.Direction * MarkupCrosswalkPoint.Shift, 4f);
            else
                RenderEnterOverlay(cameraInfo, DragPoint.Enter, Vector3.zero, 2f);

            RenderPointOverlay(cameraInfo, DragPoint);
        }
        #endregion

        #region FILLER

        private void RenderSelectFillerMode(RenderManager.CameraInfo cameraInfo)
        {
            RenderFillerLines(cameraInfo);
            RenderFillerConnectLine(cameraInfo);
            FillerPointsSelector.Render(cameraInfo);
        }
        private void RenderFillerLines(RenderManager.CameraInfo cameraInfo)
        {
            var color = FillerPointsSelector.IsHoverPoint && FillerPointsSelector.HoverPoint.Equals(TempFiller.First) ? MarkupColors.Green : MarkupColors.White;
            foreach (var trajectory in TempFiller.Trajectories)
                RenderTrajectory(cameraInfo, color, trajectory);
        }
        private void RenderFillerConnectLine(RenderManager.CameraInfo cameraInfo)
        {
            if (TempFiller.IsEmpty)
                return;

            if (FillerPointsSelector.IsHoverPoint)
            {
                var linePart = TempFiller.GetFillerLine(TempFiller.Last, FillerPointsSelector.HoverPoint);
                if (linePart.GetTrajectory(out ILineTrajectory trajectory))
                    RenderTrajectory(cameraInfo, MarkupColors.Green, trajectory);
            }
            else
            {
                var bezier = new Line3(TempFiller.Last.Position, MouseWorldPosition).GetBezier();
                RenderBezier(cameraInfo, MarkupColors.White, bezier);
            }
        }

        #endregion

        #endregion

        enum Mode
        {
            Node,
            Line,
            Filler,
            PanelAction,
            DragPoint,
            Crosswalk
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
