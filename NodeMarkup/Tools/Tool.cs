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
using System.Xml.Linq;
using NodeMarkup.UI.Editors;

namespace NodeMarkup.Tools
{
    public class NodeMarkupTool : ToolBase
    {
        #region PROPERTIES

        #region STATIC
        public static SavedInputKey ActivationShortcut { get; } = new SavedInputKey(nameof(ActivationShortcut), UI.Settings.SettingsFile, SavedInputKey.Encode(KeyCode.L, true, false, false), true);
        public static SavedInputKey DeleteAllShortcut { get; } = new SavedInputKey(nameof(DeleteAllShortcut), UI.Settings.SettingsFile, SavedInputKey.Encode(KeyCode.D, true, true, false), true);
        public static SavedInputKey ResetOffsetsShortcut { get; } = new SavedInputKey(nameof(ResetOffsetsShortcut), UI.Settings.SettingsFile, SavedInputKey.Encode(KeyCode.R, true, true, false), true);
        public static SavedInputKey AddRuleShortcut { get; } = new SavedInputKey(nameof(AddRuleShortcut), UI.Settings.SettingsFile, SavedInputKey.Encode(KeyCode.A, true, true, false), true);
        public static SavedInputKey AddFillerShortcut { get; } = new SavedInputKey(nameof(AddFillerShortcut), UI.Settings.SettingsFile, SavedInputKey.Encode(KeyCode.F, true, true, false), true);
        public static SavedInputKey CopyMarkingShortcut { get; } = new SavedInputKey(nameof(CopyMarkingShortcut), UI.Settings.SettingsFile, SavedInputKey.Encode(KeyCode.C, true, true, false), true);
        public static SavedInputKey PasteMarkingShortcut { get; } = new SavedInputKey(nameof(PasteMarkingShortcut), UI.Settings.SettingsFile, SavedInputKey.Encode(KeyCode.V, true, true, false), true);
        public static SavedInputKey EditMarkingShortcut { get; } = new SavedInputKey(nameof(EditMarkingShortcut), UI.Settings.SettingsFile, SavedInputKey.Encode(KeyCode.E, true, true, false), true);

        public static bool AltIsPressed => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        public static bool ShiftIsPressed => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        public static bool CtrlIsPressed => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        public static bool OnlyAltIsPressed => AltIsPressed && !ShiftIsPressed && !CtrlIsPressed;
        public static bool OnlyShiftIsPressed => ShiftIsPressed && !AltIsPressed && !CtrlIsPressed;
        public static bool OnlyCtrlIsPressed => CtrlIsPressed && !AltIsPressed && !ShiftIsPressed;

        public static Dictionary<Style.StyleType, SavedInt> StylesModifier { get; } =
            Utilities.GetEnumValues<Style.StyleType>().ToDictionary(i => i, i => new SavedInt($"{nameof(StylesModifier)}{(int)(object)i}", UI.Settings.SettingsFile, (int)GetDefaultStylesModifier(i), true));

        public static Ray MouseRay { get; private set; }
        public static float MouseRayLength { get; private set; }
        public static bool MouseRayValid { get; private set; }
        public static Vector3 MousePosition { get; private set; }
        public static Vector3 MouseWorldPosition { get; private set; }
        public static Vector3 CameraDirection { get; private set; }

        #endregion

        public BaseToolMode Mode { get; private set; }
        private Dictionary<ToolModeType, BaseToolMode> ToolModes { get; set; } = new Dictionary<ToolModeType, BaseToolMode>();
        public Markup Markup { get; private set; }

        public static RenderManager RenderManager => Singleton<RenderManager>.instance;

        private NodeMarkupButton Button => NodeMarkupButton.Instance;
        private NodeMarkupPanel Panel => NodeMarkupPanel.Instance;
        private ToolBase PrevTool { get; set; }
        private UIComponent PauseMenu { get; } = UIView.library.Get("PauseMenu");
        public MarkupBuffer MarkupBuffer { get; private set; } = new MarkupBuffer();

        #endregion

        #region BASIC
        public static NodeMarkupTool Instance { get; set; }
        protected override void Awake()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(Awake)}");
            base.Awake();

            ToolModes = new Dictionary<ToolModeType, BaseToolMode>()
            {
                { ToolModeType.SelectNode, new SelectNodeToolMode() },
                { ToolModeType.MakeLine, new MakeLineToolMode() },
                { ToolModeType.MakeCrosswalk, new MakeCrosswalkToolMode() },
                { ToolModeType.MakeFiller, new MakeFillerToolMode() },
                { ToolModeType.DragPoint, new DragPointToolMode() },
                { ToolModeType.PasteEntersOrder, new PasteEntersOrderToolMode()},
                { ToolModeType.EditEntersOrder, new EditEntersOrderToolMode()},
                { ToolModeType.PointsOrder, new PointsOrderToolMode()},
            };

            NodeMarkupButton.CreateButton();
            NodeMarkupPanel.CreatePanel();

            Disable();
        }
        public static NodeMarkupTool Create()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(Create)}");
            GameObject nodeMarkupControl = ToolsModifierControl.toolController.gameObject;
            Instance = nodeMarkupControl.AddComponent<NodeMarkupTool>();
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
            else
                ToolsModifierControl.SetTool<DefaultTool>();

            PrevTool = null;
        }
        private void Reset()
        {
            Panel.Hide();
            //SetMarkup(null);
            SetMode(ToolModeType.SelectNode);
            cursorInfoLabel.isVisible = false;
            cursorInfoLabel.text = string.Empty;
        }

        public void ToggleTool()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(ToggleTool)}");
            enabled = !enabled;
        }
        public void Disable() => enabled = false;

        public void SetDefaultMode() => SetMode(ToolModeType.MakeLine);
        public void SetMode(ToolModeType mode) => SetMode(ToolModes[mode]);
        public void SetMode(BaseToolMode mode)
        {
            Mode?.End();
            var prevMode = Mode;
            Mode = mode;
            Mode?.Start(prevMode);

            if (Mode?.ShowPanel == true)
                Panel.Show();
            else
                Panel.Hide();
        }
        public void SetMarkup(Markup markup)
        {
            Markup = markup;
            Panel.SetNode(Markup);
        }
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

            var isToolTipEnable = UI.Settings.ShowToolTip || Mode.Type == ToolModeType.SelectNode;
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

            static float MathPos(float pos, float size, float screen) => pos + size > screen ? (screen - size < 0 ? 0 : screen - size) : Mathf.Max(pos, 0);
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

        private bool IsMouseDown { get; set; }
        private bool IsMouseMove { get; set; }
        protected override void OnToolGUI(Event e)
        {
            Mode.OnGUI(e);

            if (Mode.ProcessShortcuts(e))
                return;

            switch (e.type)
            {
                case EventType.MouseDown when MouseRayValid && e.button == 0:
                    IsMouseDown = true;
                    IsMouseMove = false;
                    Mode.OnMouseDown(e);
                    break;
                case EventType.MouseDrag when MouseRayValid:
                    IsMouseMove = true;
                    Mode.OnMouseDrag(e);
                    break;
                case EventType.MouseUp when MouseRayValid && e.button == 0:
                    if (IsMouseMove)
                        Mode.OnMouseUp(e);
                    else
                        Mode.OnPrimaryMouseClicked(e);
                    IsMouseDown = false;
                    break;
                case EventType.MouseUp when MouseRayValid && e.button == 1:
                    Mode.OnSecondaryMouseClicked();
                    break;
            }
        }

        public void DeleteAllMarking()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(DeleteAllMarking)}");

            if (UI.Settings.DeleteWarnings)
            {
                var messageBox = MessageBoxBase.ShowModal<YesNoMessageBox>();
                messageBox.CaprionText = Localize.Tool_ClearMarkingsCaption;
                messageBox.MessageText = string.Format($"{Localize.Tool_ClearMarkingsMessage}\n{Localize.MessageBox_CantUndone}", Markup.Id);
                messageBox.OnButton1Click = Delete;
            }
            else
                Delete();

            bool Delete()
            {
                Markup.Clear();
                Panel.UpdatePanel();
                return true;
            }
        }
        public void ResetAllOffsets()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(ResetAllOffsets)}");

            if (UI.Settings.DeleteWarnings)
            {
                var messageBox = MessageBoxBase.ShowModal<YesNoMessageBox>();
                messageBox.CaprionText = Localize.Tool_ResetOffsetsCaption;
                messageBox.MessageText = $"{string.Format(Localize.Tool_ResetOffsetsMessage, Markup.Id)}\n{Localize.MessageBox_CantUndone}";
                messageBox.OnButton1Click = Reset;
            }
            else
                Reset();

            bool Reset()
            {
                Markup.ResetOffsets();
                Panel.UpdatePanel();
                return true;
            }
        }
        public void DeleteItem(IDeletable item, Action onDelete)
        {
            if (UI.Settings.DeleteWarnings)
            {
                var dependences = item.GetDependences();
                if (dependences.Exist)
                {
                    ShowModal(GetDeleteDependences(dependences));
                    return;
                }
                else if (UI.Settings.DeleteWarningsType == 0)
                {
                    ShowModal(string.Empty);
                    return;
                }
            }

            onDelete();

            void ShowModal(string additional)
            {
                var messageBox = MessageBoxBase.ShowModal<YesNoMessageBox>();
                messageBox.CaprionText = string.Format(Localize.Tool_DeleteCaption, item.DeleteCaptionDescription);
                messageBox.MessageText = $"{string.Format(Localize.Tool_DeleteMessage, item.DeleteMessageDescription, item)}\n{Localize.MessageBox_CantUndone}\n\n{additional}";
                messageBox.OnButton1Click = () =>
                    {
                        onDelete();
                        return true;
                    };
            }
        }
        private string GetDeleteDependences(Dependences dependences)
        {
            var strings = dependences.Total.Where(i => i.Value > 0).Select(i => string.Format(i.Key.Description(), i.Value)).ToArray();
            return $"{Localize.Tool_DeleteDependence}\n{string.Join(", ", strings)}.";
        }

        public void CopyMarkup() => MarkupBuffer = new MarkupBuffer(Markup);
        public void CopyMarkupBackup() => MarkupBuffer = Markup.Backup;
        public void PasteMarkup()
        {
            if (UI.Settings.DeleteWarnings)
            {
                var messageBox = MessageBoxBase.ShowModal<YesNoMessageBox>();
                messageBox.CaprionText = Localize.Tool_PasteMarkingsCaption;
                messageBox.MessageText = $"{Localize.Tool_PasteMarkingsMessage}\n{Localize.MessageBox_CantUndone}";
                messageBox.OnButton1Click = Paste;
            }
            else
                Paste();

            bool Paste()
            {
                SetMode(ToolModeType.PasteEntersOrder);
                return true;
            }
        }
        public void EditMarkup()
        {
            CopyMarkup();
            SetMode(ToolModeType.EditEntersOrder);
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

        public static Style.StyleType GetStyle<StyleType>(StyleType defaultStyle)
           where StyleType : Enum
        {
            var modifier = Utilities.GetEnumValues<StyleModifier>()
                .FirstOrDefault(i => i.GetAttr<InputKeyAttribute, StyleModifier>() is InputKeyAttribute ik && ik.Control == CtrlIsPressed && ik.Shift == ShiftIsPressed && ik.Alt == AltIsPressed);

            foreach (var style in Utilities.GetEnumValues<StyleType>())
            {
                var general = (Style.StyleType)(object)style;
                if (StylesModifier.TryGetValue(general, out SavedInt saved) && (StyleModifier)saved.value == modifier)
                    return general;
            }
            return (Style.StyleType)(object)defaultStyle;
        }
        private static StyleModifier GetDefaultStylesModifier(Style.StyleType style)
        {
            switch (style)
            {
                case Style.StyleType.LineDashed: return StyleModifier.Without;
                case Style.StyleType.LineSolid: return StyleModifier.Shift;
                case Style.StyleType.LineDoubleDashed: return StyleModifier.Ctrl;
                case Style.StyleType.LineDoubleSolid: return StyleModifier.CtrlShift;
                case Style.StyleType.StopLineSolid: return StyleModifier.Without;
                case Style.StyleType.CrosswalkZebra: return StyleModifier.Without;
                case Style.StyleType.FillerStripe: return StyleModifier.Without;
                default: return StyleModifier.NotSet;
            }
        }
    }
    public class MarkupBuffer
    {
        public XElement Data { get; }
        public EnterData[] Enters { get; }
        public ObjectsMap Map { get; }
        public MarkupBuffer(Markup markup) : this(markup.ToXml(), markup.Enters.Select(e => e.Data).ToArray()) { }
        public MarkupBuffer() : this(new XElement("Markup"), new EnterData[0]) { }
        private MarkupBuffer(XElement data, EnterData[] enters)
        {
            Data = data;
            Enters = enters;
            Map = new ObjectsMap();
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
