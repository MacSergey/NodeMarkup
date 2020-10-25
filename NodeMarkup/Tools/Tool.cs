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
using NodeMarkup.UI.Panel;
using System.Diagnostics;

namespace NodeMarkup.Tools
{
    public class NodeMarkupTool : ToolBase
    {
        #region PROPERTIES

        #region STATIC
        public static Shortcut DeleteAllShortcut { get; } = new Shortcut(nameof(DeleteAllShortcut), nameof(Localize.Settings_ShortcutDeleteAllNodeLines), SavedInputKey.Encode(KeyCode.D, true, true, false), () => Instance.DeleteAllMarking());
        public static Shortcut ResetOffsetsShortcut { get; } = new Shortcut(nameof(ResetOffsetsShortcut), nameof(Localize.Settings_ShortcutResetPointsOffset), SavedInputKey.Encode(KeyCode.R, true, true, false), () => Instance.ResetAllOffsets());
        public static Shortcut AddFillerShortcut { get; } = new Shortcut(nameof(AddFillerShortcut), nameof(Localize.Settings_ShortcutAddNewFiller), SavedInputKey.Encode(KeyCode.F, true, true, false), () => Instance.StartCreateFiller());
        public static Shortcut CopyMarkingShortcut { get; } = new Shortcut(nameof(CopyMarkingShortcut), nameof(Localize.Settings_ShortcutCopyMarking), SavedInputKey.Encode(KeyCode.C, true, true, false), () => Instance.CopyMarkup());
        public static Shortcut PasteMarkingShortcut { get; } = new Shortcut(nameof(PasteMarkingShortcut), nameof(Localize.Settings_ShortcutPasteMarking), SavedInputKey.Encode(KeyCode.V, true, true, false), () => Instance.PasteMarkup());
        public static Shortcut EditMarkingShortcut { get; } = new Shortcut(nameof(EditMarkingShortcut), nameof(Localize.Settings_ShortcutEditMarking), SavedInputKey.Encode(KeyCode.E, true, true, false), () => Instance.EditMarkup());
        public static Shortcut CreateEdgeLinesShortcut { get; } = new Shortcut(nameof(CreateEdgeLinesShortcut), nameof(Localize.Settings_ShortcutCreateEdgeLines), SavedInputKey.Encode(KeyCode.W, true, true, false), () => Instance.CreateEdgeLines());
        public static Shortcut ActivationShortcut { get; } = new Shortcut(nameof(ActivationShortcut), nameof(Localize.Settings_ShortcutActivateTool), SavedInputKey.Encode(KeyCode.L, true, false, false));
        public static Shortcut AddRuleShortcut { get; } = new Shortcut(nameof(AddRuleShortcut), nameof(Localize.Settings_ShortcutAddNewLineRule), SavedInputKey.Encode(KeyCode.A, true, true, false));

        public static IEnumerable<Shortcut> Shortcuts
        {
            get
            {
                yield return DeleteAllShortcut;
                yield return ResetOffsetsShortcut;
                yield return AddFillerShortcut;
                yield return CopyMarkingShortcut;
                yield return PasteMarkingShortcut;
                yield return EditMarkingShortcut;
                yield return CreateEdgeLinesShortcut;
            }
        }

        public static bool AltIsPressed => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        public static bool ShiftIsPressed => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        public static bool CtrlIsPressed => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        public static bool OnlyAltIsPressed => AltIsPressed && !ShiftIsPressed && !CtrlIsPressed;
        public static bool OnlyShiftIsPressed => ShiftIsPressed && !AltIsPressed && !CtrlIsPressed;
        public static bool OnlyCtrlIsPressed => CtrlIsPressed && !AltIsPressed && !ShiftIsPressed;

        public static Dictionary<Style.StyleType, SavedInt> StylesModifier { get; } =
            Utilities.GetEnumValues<Style.StyleType>().ToDictionary(i => i, i => new SavedInt($"{nameof(StylesModifier)}{(int)(object)i}", Settings.SettingsFile, (int)GetDefaultStylesModifier(i), true));

        public static Ray MouseRay { get; private set; }
        public static float MouseRayLength { get; private set; }
        public static bool MouseRayValid { get; private set; }
        public static Vector3 MousePosition { get; private set; }
        public static Vector3 MouseWorldPosition { get; private set; }
        public static Vector3 CameraDirection { get; private set; }

        #endregion

        public BaseToolMode Mode { get; private set; }
        public ToolModeType ModeType => Mode?.Type ?? ToolModeType.None;
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
            Logger.LogDebug($"Tool Created");
            return Instance;
        }
        public static void Remove()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(Remove)}");
            if (Instance != null)
            {
                Destroy(Instance);
                Instance = null;
                Logger.LogDebug($"Tool removed");
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
            SetMode(ToolModeType.SelectNode);
            cursorInfoLabel.isVisible = false;
            cursorInfoLabel.text = string.Empty;
        }

        public void ToggleTool()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(ToggleTool)}: {(!enabled ? "enable" : "disable")}");
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

            var isToolTipEnable = Settings.ShowToolTip || Mode.Type == ToolModeType.SelectNode;
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

            if (Shortcuts.Any(s => s.IsPressed(e)) || Panel?.OnShortcut(e) == true)
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
        private void StartCreateFiller()
        {
            SetMode(ToolModeType.MakeFiller);
            if (Mode is MakeFillerToolMode fillerToolMode)
                fillerToolMode.DisableByAlt = false;
        }
        private void DeleteAllMarking()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(DeleteAllMarking)}");

            var messageBox = MessageBoxBase.ShowModal<YesNoMessageBox>();
            messageBox.CaprionText = Localize.Tool_ClearMarkingsCaption;
            messageBox.MessageText = string.Format($"{Localize.Tool_ClearMarkingsMessage}\n{Localize.MessageBox_CantUndone}", Markup.Id);
            messageBox.OnButton1Click = Delete;

            bool Delete()
            {
                Markup.Clear();
                Panel.UpdatePanel();
                return true;
            }
        }
        private void ResetAllOffsets()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(ResetAllOffsets)}");

            if (Settings.DeleteWarnings)
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
            if (Settings.DeleteWarnings)
            {
                var dependences = item.GetDependences();
                if (dependences.Exist)
                {
                    ShowModal(GetDeleteDependences(dependences));
                    return;
                }
                else if (Settings.DeleteWarningsType == 0)
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

        private void CopyMarkup()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(CopyMarkup)}");
            MarkupBuffer = new MarkupBuffer(Markup);
        }
        public void CopyMarkupBackup() => MarkupBuffer = Markup.Backup;
        private void PasteMarkup()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(PasteMarkup)}");

            if (Settings.DeleteWarnings)
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
        private void EditMarkup()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(EditMarkup)}");

            CopyMarkup();
            SetMode(ToolModeType.EditEntersOrder);
        }
        private void CreateEdgeLines()
        {
            Logger.LogDebug($"{nameof(NodeMarkupTool)}.{nameof(CreateEdgeLines)}");

            foreach (var enter in Markup.Enters)
                Markup.AddConnection(new MarkupPointPair(enter.LastPoint, enter.Next.FirstPoint), Style.StyleType.EmptyLine);

            Panel.UpdatePanel();
        }

        #endregion

        #region OVERLAY

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            Mode.RenderOverlay(cameraInfo);
            base.RenderOverlay(cameraInfo);
        }

        private static float DefaultWidth => 0.2f;
        private static bool DefaultBlend => true;
        public static void RenderBezier(RenderManager.CameraInfo cameraInfo, Bezier3 bezier, Color? color = null, float? width = null, bool? alphaBlend = null, bool cut = false) =>
            RenderManager.OverlayEffect.DrawBezier(cameraInfo, color ?? Colors.White, bezier, width ?? DefaultWidth, cut ? (width ?? DefaultWidth) / 2 : 0f, cut ? (width ?? DefaultWidth) / 2 : 0f, -1f, 1280f, false, alphaBlend ?? DefaultBlend);
        public static void RenderCircle(RenderManager.CameraInfo cameraInfo, Vector3 position, Color? color = null, float? width = null, bool? alphaBlend = null) =>
            RenderManager.OverlayEffect.DrawCircle(cameraInfo, color ?? Colors.White, position, width ?? DefaultWidth, -1f, 1280f, false, alphaBlend ?? DefaultBlend);

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
            return style switch
            {
                Style.StyleType.LineDashed => StyleModifier.Without,
                Style.StyleType.LineSolid => StyleModifier.Shift,
                Style.StyleType.LineDoubleDashed => StyleModifier.Ctrl,
                Style.StyleType.LineDoubleSolid => StyleModifier.CtrlShift,
                Style.StyleType.StopLineSolid => StyleModifier.Without,
                Style.StyleType.CrosswalkZebra => StyleModifier.Without,
                Style.StyleType.FillerStripe => StyleModifier.Without,
                _ => StyleModifier.NotSet,
            };
        }

        public static void PressShortcut()
        {

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
            if (!UIView.HasModalInput() && !UIView.HasInputFocus() && NodeMarkupTool.ActivationShortcut.InputKey.IsKeyUp())
                NodeMarkupTool.Instance.ToggleTool();
        }
    }
}
