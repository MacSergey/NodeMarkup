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

        public BaseToolMode Mode { get; private set; }
        private Dictionary<BaseToolMode.ModeType, BaseToolMode> ToolModes { get; set; } = new Dictionary<BaseToolMode.ModeType, BaseToolMode>();
        public Markup EditMarkup { get; private set; }

        public static RenderManager RenderManager => Singleton<RenderManager>.instance;

        NodeMarkupButton Button => NodeMarkupButton.Instance;
        NodeMarkupPanel Panel => NodeMarkupPanel.Instance;
        private ToolBase PrevTool { get; set; }
        UIComponent PauseMenu { get; } = UIView.library.Get("PauseMenu");

        #endregion

        #region BASIC
        public static NodeMarkupTool Instance { get; set; }
        protected override void Awake()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(Awake)}");
            base.Awake();

            NodeMarkupButton.CreateButton();
            NodeMarkupPanel.CreatePanel();

            Disable();
        }
        public static NodeMarkupTool Create()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(Create)}");
            GameObject nodeMarkupControl = ToolsModifierControl.toolController.gameObject;
            Instance = nodeMarkupControl.AddComponent<NodeMarkupTool>();

            Instance.ToolModes = new Dictionary<BaseToolMode.ModeType, BaseToolMode>()
            {
                { BaseToolMode.ModeType.SelectNode, new SelectNodeToolMode() },
                { BaseToolMode.ModeType.MakeLine, new MakeLineToolMode() },
                { BaseToolMode.ModeType.MakeCrosswalk, new MakeCrosswalkToolMode() },
                { BaseToolMode.ModeType.MakeFiller, new MakeFillerToolMode() },
                { BaseToolMode.ModeType.DragPoint, new DragPointToolMode() },
            };

            return Instance;
        }
        public static void Remove()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(Remove)}");
            if (Instance != null)
            {
                Destroy(Instance);
                Instance = null;
            }
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
            SetMarkup(null);
            SetMode(BaseToolMode.ModeType.SelectNode);
            cursorInfoLabel.isVisible = false;
            cursorInfoLabel.text = string.Empty;
            //Panel?.EndPanelAction();
        }

        public void ToggleTool()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(ToggleTool)}");
            enabled = !enabled;
        }
        public void Disable() => enabled = false;

        public void SetDefaultMode() => SetMode(BaseToolMode.ModeType.MakeLine);
        public void SetMode(BaseToolMode.ModeType mode) => SetMode(ToolModes[mode]);
        public void SetMode(BaseToolMode mode)
        {
            Mode?.End();
            Mode = mode;
            Mode.Start();
        }
        public void SetMarkup(Markup markup)
        {
            EditMarkup = markup;
            Panel.SetNode(EditMarkup);
        }

        //public void StartPanelAction(out bool isAccept)
        //{
        //    if (Mode.Type == BaseToolMode.ModeType.MakeLine)
        //    {
        //        SetMode(BaseToolMode.ModeType.PanelAction);
        //        isAccept = true;
        //    }
        //    else
        //        isAccept = false;
        //}
        //public void EndPanelAction()
        //{
        //    if (Mode.Type == BaseToolMode.ModeType.PanelAction)
        //    {
        //        Panel.EndPanelAction();
        //        SetMode(BaseToolMode.ModeType.MakeLine);
        //    }
        //}

        #endregion

        #region UPDATE

        protected override void OnToolUpdate()
        {
            if (PauseMenu?.isVisible == true)
            {
                PrevTool = null;
                Disable();
                UIView.library.Hide("PauseMenu");
                return;
            }
            if ((RenderManager.CurrentCameraInfo.m_layerMask & (3 << 24)) == 0)
            {
                PrevTool = null;
                Disable();
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

            Mode.OnUpdate();
            Info();

            base.OnToolUpdate();
        }

        #region INFO

        private void Info()
        {
            var position = GetInfoPosition();

            var isToolTipEnable = UI.Settings.ShowToolTip || Mode.Type == BaseToolMode.ModeType.SelectNode;
            var isPanelHover = Panel.isVisible && new Rect(Panel.relativePosition, Panel.size).Contains(position);
            var isHasText = Mode.GetToolInfo() is string info && !string.IsNullOrEmpty(info);

            if (isToolTipEnable && !isPanelHover && isHasText)
                ShowToolInfo(Mode.GetToolInfo(), position);
            else
                cursorInfoLabel.isVisible = false;
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
            if (Mode.ProcessShortcuts(e))
                return;

            switch (e.type)
            {
                case EventType.MouseDown when MouseRayValid && e.button == 0:
                    Mode.OnMouseDown(e);
                    break;
                case EventType.MouseDrag when MouseRayValid:
                    Mode.OnMouseDrag(e);
                    break;
                case EventType.MouseUp when MouseRayValid && e.button == 0:
                    Mode.OnPrimaryMouseClicked(e);
                    break;
                case EventType.MouseUp when MouseRayValid && e.button == 1:
                    Mode.OnSecondaryMouseClicked();
                    break;
            }
        }

        public void DeleteAllLines()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(DeleteAllLines)}");

            if (UI.Settings.DeleteWarnings)
            {
                var messageBox = MessageBoxBase.ShowModal<YesNoMessageBox>();
                messageBox.CaprionText = Localize.Tool_ClearMarkingsCaption;
                messageBox.MessageText = string.Format(Localize.Tool_ClearMarkingsMessage, EditMarkup.Id);
                messageBox.OnButton1Click = Delete;
            }
            else
                Delete();

            bool Delete()
            {
                EditMarkup.Clear();
                Panel.UpdatePanel();
                return true;
            }
        }

        #endregion

        #region OVERLAY

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            Mode.RenderOverlay(cameraInfo);
            base.RenderOverlay(cameraInfo);
        }

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

        #endregion

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
