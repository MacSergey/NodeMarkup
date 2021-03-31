using ColossalFramework;
using ColossalFramework.Importers;
using ColossalFramework.Math;
using ColossalFramework.UI;
using HarmonyLib;
using ICities;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.UI;
using NodeMarkup.UI.Panel;
using NodeMarkup.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Tools
{
    public class NodeMarkupTool : BaseTool<NodeMarkupTool, ToolModeType>
    {
        #region STATIC

        public static void Create() => Create<NodeMarkupTool>();

        public static NodeMarkupShortcut DeleteAllShortcut { get; } = new NodeMarkupShortcut(nameof(DeleteAllShortcut), nameof(Localize.Settings_ShortcutDeleteAllNodeLines), SavedInputKey.Encode(KeyCode.D, true, true, false), () => Instance.DeleteAllMarking());
        public static NodeMarkupShortcut ResetOffsetsShortcut { get; } = new NodeMarkupShortcut(nameof(ResetOffsetsShortcut), nameof(Localize.Settings_ShortcutResetPointsOffset), SavedInputKey.Encode(KeyCode.R, true, true, false), () => Instance.ResetAllOffsets());
        public static NodeMarkupShortcut AddFillerShortcut { get; } = new NodeMarkupShortcut(nameof(AddFillerShortcut), nameof(Localize.Settings_ShortcutAddNewFiller), SavedInputKey.Encode(KeyCode.F, true, true, false), () => Instance.StartCreateFiller());
        public static NodeMarkupShortcut CopyMarkingShortcut { get; } = new NodeMarkupShortcut(nameof(CopyMarkingShortcut), nameof(Localize.Settings_ShortcutCopyMarking), SavedInputKey.Encode(KeyCode.C, true, true, false), () => Instance.CopyMarkup());
        public static NodeMarkupShortcut PasteMarkingShortcut { get; } = new NodeMarkupShortcut(nameof(PasteMarkingShortcut), nameof(Localize.Settings_ShortcutPasteMarking), SavedInputKey.Encode(KeyCode.V, true, true, false), () => Instance.PasteMarkup());
        public static NodeMarkupShortcut EditMarkingShortcut { get; } = new NodeMarkupShortcut(nameof(EditMarkingShortcut), nameof(Localize.Settings_ShortcutEditMarking), SavedInputKey.Encode(KeyCode.E, true, true, false), () => Instance.EditMarkup());
        public static NodeMarkupShortcut CreateEdgeLinesShortcut { get; } = new NodeMarkupShortcut(nameof(CreateEdgeLinesShortcut), nameof(Localize.Settings_ShortcutCreateEdgeLines), SavedInputKey.Encode(KeyCode.W, true, true, false), () => Instance.CreateEdgeLines());
        public static NodeMarkupShortcut ActivationShortcut { get; } = new NodeMarkupShortcut(nameof(ActivationShortcut), nameof(Localize.Settings_ShortcutActivateTool), SavedInputKey.Encode(KeyCode.L, true, false, false));
        public static NodeMarkupShortcut AddRuleShortcut { get; } = new NodeMarkupShortcut(nameof(AddRuleShortcut), nameof(Localize.Settings_ShortcutAddNewLineRule), SavedInputKey.Encode(KeyCode.A, true, true, false));
        public static NodeMarkupShortcut SaveAsIntersectionTemplateShortcut { get; } = new NodeMarkupShortcut(nameof(SaveAsIntersectionTemplateShortcut), nameof(Localize.Settings_ShortcutSaveAsPreset), SavedInputKey.Encode(KeyCode.S, true, true, false), () => Instance.SaveAsIntersectionTemplate());
        public static NodeMarkupShortcut CutLinesByCrosswalksShortcut { get; } = new NodeMarkupShortcut(nameof(CutLinesByCrosswalksShortcut), nameof(Localize.Settings_ShortcutCutLinesByCrosswalks), SavedInputKey.Encode(KeyCode.T, true, true, false), () => Instance.CutByCrosswalks());
        public static NodeMarkupShortcut ApplyBetweenIntersectionsShortcut { get; } = new NodeMarkupShortcut(nameof(ApplyBetweenIntersectionsShortcut), nameof(Localize.Settings_ShortcutApplyBetweenIntersections), SavedInputKey.Encode(KeyCode.G, true, true, false), () => Instance.ApplyBetweenIntersections());
        public static NodeMarkupShortcut ApplyWholeStreetShortcut { get; } = new NodeMarkupShortcut(nameof(ApplyWholeStreetShortcut), nameof(Localize.Settings_ShortcutApplyWholeStreet), SavedInputKey.Encode(KeyCode.B, true, true, false), () => Instance.ApplyWholeStreet());

        public static IEnumerable<NodeMarkupShortcut> Shortcuts
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
                yield return SaveAsIntersectionTemplateShortcut;
                yield return CutLinesByCrosswalksShortcut;
                yield return ApplyBetweenIntersectionsShortcut;
                yield return ApplyWholeStreetShortcut;
            }
        }

        public static Dictionary<Style.StyleType, SavedInt> StylesModifier { get; } = EnumExtension.GetEnumValues<Style.StyleType>(v => v.IsItem()).ToDictionary(i => i, i => GetSavedStylesModifier(i));

        #endregion

        #region PROPERTIES

        protected override IToolMode DefaultMode => ToolModes[ToolModeType.Select];
        protected override bool ShowToolTip => (Settings.ShowToolTip || Mode.Type == ToolModeType.Select) && !Panel.IsHover;

        public Markup Markup { get; private set; }

        public NodeMarkupPanel Panel => NodeMarkupPanel.Instance;
        public IntersectionTemplate MarkupBuffer { get; private set; }
        public bool IsMarkupBufferEmpty => MarkupBuffer == null;
        private Dictionary<Style.StyleType, Style> StyleBuffer { get; } = new Dictionary<Style.StyleType, Style>();

        #endregion

        #region BASIC
        public new static NodeMarkupTool Instance
        {
            get => BaseTool.Instance as NodeMarkupTool;
            set => BaseTool.Instance = value;
        }
        protected override void InitProcess()
        {
            base.InitProcess();
            NodeMarkupPanel.CreatePanel();
        }
        protected override IEnumerable<IToolMode<ToolModeType>> GetModes()
        {
            yield return CreateToolMode<SelectToolMode>();
            yield return CreateToolMode<MakeLineToolMode>();
            yield return CreateToolMode<MakeCrosswalkToolMode>();
            yield return CreateToolMode<MakeFillerToolMode>();
            yield return CreateToolMode<DragPointToolMode>();
            yield return CreateToolMode<PasteEntersOrderToolMode>();
            yield return CreateToolMode<EditEntersOrderToolMode>();
            yield return CreateToolMode<ApplyIntersectionTemplateOrderToolMode>();
            yield return CreateToolMode<PointsOrderToolMode>();
        }
        public override void Enable() => Enable<NodeMarkupTool>();
        protected override void OnEnable()
        {
            base.OnEnable();
            Singleton<InfoManager>.instance.SetCurrentMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.Default);
        }
        public override void Escape()
        {
            if (!Mode.OnEscape() && !Panel.OnEscape())
                Disable();
        }
        public void SetDefaultMode() => SetMode(ToolModeType.MakeLine);
        public void SetMode(ToolModeType mode) => SetMode(ToolModes[mode]);
        protected override void SetModeNow(IToolMode mode)
        {
            base.SetModeNow(mode);
            Panel.Active = (Mode as NodeMarkupToolMode)?.ShowPanel == true;
        }
        public void SetMarkup(Markup markup)
        {
            Markup = markup;
            Panel.SetMarkup(Markup);
        }
        #endregion

        #region GUI

        private bool IsMouseMove { get; set; }
        protected override void OnToolGUI(Event e)
        {
            Mode.OnToolGUI(e);

            if (Shortcuts.Any(s => s.IsPressed(e)) || Panel?.OnShortcut(e) == true)
                return;

            switch (e.type)
            {
                case EventType.MouseDown when MouseRayValid && e.button == 0:
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
                    break;
                case EventType.MouseUp when MouseRayValid && e.button == 1:
                    Mode.OnSecondaryMouseClicked();
                    break;
            }
        }
        private void StartCreateFiller()
        {
            if (Markup is ISupportFillers)
            {
                SetMode(ToolModeType.MakeFiller);
                if (NextMode is MakeFillerToolMode fillerToolMode)
                    fillerToolMode.DisableByAlt = false;
            }
        }
        private void DeleteAllMarking()
        {
            Mod.Logger.Debug($"Delete all markings");

            var messageBox = MessageBoxBase.ShowModal<YesNoMessageBox>();
            messageBox.CaptionText = Localize.Tool_ClearMarkingsCaption;
            messageBox.MessageText = string.Format($"{Localize.Tool_ClearMarkingsMessage}\n{NodeMarkupMessageBox.CantUndone}", Markup.Id);
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
            Mod.Logger.Debug($"Reset all points offsets");

            if (Settings.DeleteWarnings)
            {
                var messageBox = MessageBoxBase.ShowModal<YesNoMessageBox>();
                messageBox.CaptionText = Localize.Tool_ResetOffsetsCaption;
                messageBox.MessageText = $"{string.Format(Localize.Tool_ResetOffsetsMessage, Markup.Id)}\n{NodeMarkupMessageBox.CantUndone}";
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
        public void DeleteItem<T>(T item, Action<T> onDelete)
            where T : IDeletable
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

            onDelete(item);

            void ShowModal(string additional)
            {
                var messageBox = MessageBoxBase.ShowModal<YesNoMessageBox>();
                messageBox.CaptionText = string.Format(Localize.Tool_DeleteCaption, item.DeleteCaptionDescription);
                messageBox.MessageText = $"{string.Format(Localize.Tool_DeleteMessage, item.DeleteMessageDescription, item)}\n{NodeMarkupMessageBox.CantUndone}\n\n{additional}";
                messageBox.OnButton1Click = () =>
                    {
                        onDelete(item);
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
            Mod.Logger.Debug($"Copy marking");
            MarkupBuffer = new IntersectionTemplate(Markup);
        }
        private void PasteMarkup()
        {
            Mod.Logger.Debug($"Paste marking");

            if (MarkupBuffer == null)
                return;

            if (Settings.DeleteWarnings && !Markup.IsEmpty)
            {
                var messageBox = MessageBoxBase.ShowModal<YesNoMessageBox>();
                messageBox.CaptionText = Localize.Tool_PasteMarkingsCaption;
                messageBox.MessageText = $"{Localize.Tool_PasteMarkingsMessage}\n{NodeMarkupMessageBox.ItWillReplace}\n{NodeMarkupMessageBox.CantUndone}";
                messageBox.OnButton1Click = Paste;
            }
            else
                Paste();

            bool Paste()
            {
                BaseOrderToolMode.IntersectionTemplate = MarkupBuffer;
                SetMode(ToolModeType.PasteEntersOrder);
                return true;
            }
        }
        private void EditMarkup()
        {
            Mod.Logger.Debug($"Edit marking order");

            BaseOrderToolMode.IntersectionTemplate = new IntersectionTemplate(Markup);
            SetMode(ToolModeType.EditEntersOrder);
        }
        public void ApplyIntersectionTemplate(IntersectionTemplate template)
        {
            Mod.Logger.Debug($"Apply intersection template");

            BaseOrderToolMode.IntersectionTemplate = template;
            SetMode(ToolModeType.ApplyIntersectionTemplateOrder);
        }
        private void CreateEdgeLines()
        {
            Mod.Logger.Debug($"Create edge lines");

            var lines = Markup.Enters.Select(e => Markup.AddRegularLine(new MarkupPointPair(e.LastPoint, e.Next.FirstPoint), null)).ToArray();
            foreach (var line in lines)
                Panel.AddLine(line);

            Panel.EditLine(lines.Last());
        }
        private void SaveAsIntersectionTemplate()
        {
            Mod.Logger.Debug($"Save as intersection template");

            StartCoroutine(MakeScreenshot(Callback));

            void Callback(Image image)
            {
                if (TemplateManager.IntersectionManager.AddTemplate(Markup, image, out IntersectionTemplate template))
                    Panel.EditIntersectionTemplate(template);
            }
        }
        private void CutByCrosswalks()
        {
            Mod.Logger.Debug($"Cut by crosswalk");

            foreach (var crosswalk in Markup.Crosswalks)
                Markup.CutLinesByCrosswalk(crosswalk);
        }
        private delegate ushort? SegmentGetter(ushort[] segmentIds, ushort beforeSegmentId);
        private void ApplyBetweenIntersections()
        {
            Mod.Logger.Debug($"Apply between intersections");

            if (Markup.Type != MarkupType.Segment)
                return;

            var segment = Markup.Id.GetSegment();
            if (Settings.DeleteWarnings)
            {
                var messageBox = MessageBoxBase.ShowModal<YesNoMessageBox>();
                messageBox.CaptionText = Localize.Tool_ApplyBetweenIntersectionsCaption;
                messageBox.MessageText = $"{Localize.Tool_ApplyBetweenIntersectionsMessage}\n{NodeMarkupMessageBox.ItWillReplace}\n{NodeMarkupMessageBox.CantUndone}";
                messageBox.OnButton1Click = Apply;
            }
            else
                Apply();


            bool Apply()
            {
                var config = Markup.ToXml();
                this.Apply(Markup.Id, segment.m_startNode, segment.m_endNode, segment.Info, config, SegmentGetter);
                this.Apply(Markup.Id, segment.m_endNode, segment.m_startNode, segment.Info, config, SegmentGetter);
                return true;
            }

            ushort? SegmentGetter(ushort[] segmentIds, ushort beforeSegmentId)
            {
                var id = segmentIds[segmentIds[0] == beforeSegmentId ? 1 : 0];
                var nextSegment = id.GetSegment();
                return nextSegment.Info == segment.Info ? id : null;
            }
        }
        private void ApplyWholeStreet()
        {
            Mod.Logger.Debug($"Apply to whole street");

            if (Markup.Type != MarkupType.Segment)
                return;

            var segment = Markup.Id.GetSegment();
            if (Settings.DeleteWarnings)
            {
                var streetName = Singleton<NetManager>.instance.GetSegmentName(Markup.Id);
                var messageBox = MessageBoxBase.ShowModal<YesNoMessageBox>();
                messageBox.CaptionText = Localize.Tool_ApplyWholeStreetCaption;
                messageBox.MessageText = $"{string.Format(Localize.Tool_ApplyWholeStreetMessage, streetName)}\n{NodeMarkupMessageBox.ItWillReplace}\n{NodeMarkupMessageBox.CantUndone}";
                messageBox.OnButton1Click = Apply;
            }
            else
                Apply();

            bool Apply()
            {
                var config = Markup.ToXml();
                this.Apply(Markup.Id, segment.m_startNode, segment.m_endNode, segment.Info, config, SegmentGetter);
                this.Apply(Markup.Id, segment.m_endNode, segment.m_startNode, segment.Info, config, SegmentGetter);
                return true;
            }

            ushort? SegmentGetter(ushort[] segmentIds, ushort beforeSegmentId)
            {
                foreach (var id in segmentIds)
                {
                    if (id == beforeSegmentId)
                        continue;

                    var nextSegment = id.GetSegment();
                    if (nextSegment.Info == segment.Info && nextSegment.m_nameSeed == segment.m_nameSeed)
                        return id;
                }
                return null;
            }
        }

        void Apply(ushort startSegmentId, ushort nearNodeId, ushort farNodeId, NetInfo info, XElement config, SegmentGetter segmentGetter)
        {
            var nodeId = (ushort?)nearNodeId;
            var segmentId = (ushort?)startSegmentId;
            while (true)
            {
                segmentId = ApplyToNode(nodeId.Value, segmentId.Value, nearNodeId, farNodeId, info, config, segmentGetter);
                if (segmentId == null || segmentId == startSegmentId)
                    return;

                nodeId = ApplyToSegment(segmentId.Value, nodeId.Value, nearNodeId, farNodeId, info, config);
                if (nodeId == null)
                    return;
            }
        }

        ushort? ApplyToNode(ushort nodeId, ushort beforeSegmentId, ushort nearNodeId, ushort farNodeId, NetInfo info, XElement config, SegmentGetter nextGetter)
        {
            var node = nodeId.GetNode();

            var nodeSegmentIds = node.SegmentsId().ToArray();
            var nextSegmentId = nextGetter(nodeSegmentIds, beforeSegmentId);

            if (nextSegmentId != null && nodeSegmentIds.Length == 2 && (node.m_flags & NetNode.Flags.Bend) != 0)
            {
                var map = new ObjectsMap();
                map.AddSegment(nearNodeId, beforeSegmentId);
                map.AddSegment(farNodeId, nextSegmentId.Value);
                var markup = MarkupManager.NodeManager.Get(nodeId);
                markup.Clear();
                markup.FromXml(Mod.Version, config, map);
            }

            return nextSegmentId;
        }

        ushort? ApplyToSegment(ushort segmentId, ushort beforeNodeId, ushort nearNodeId, ushort farNodeId, NetInfo info, XElement config)
        {
            var segment = segmentId.GetSegment();
            if (segment.Info != info)
                return null;

            var nextNodeId = segment.m_startNode == beforeNodeId ? segment.m_endNode : segment.m_startNode;

            var map = new ObjectsMap();
            map.AddNode(farNodeId, beforeNodeId);
            map.AddNode(nearNodeId, nextNodeId);
            var markup = MarkupManager.SegmentManager.Get(segmentId);
            markup.Clear();
            markup.FromXml(Mod.Version, config, map);

            return nextNodeId;
        }


        private int ScreenshotSize => 400;
        private IEnumerator MakeScreenshot(Action<Image> callback)
        {
            if (callback == null)
                yield break;

            Mod.Logger.Debug($"Make screenshot");

            var cameraController = ToolsModifierControl.cameraController;
            var camera = Camera.main;
            var backupMask = camera.cullingMask;
            var backupRect = camera.rect;
            var backupPosition = cameraController.m_currentPosition;
            var backupRotation = cameraController.m_currentAngle;
            var backupSize = cameraController.m_currentSize;

            var angle = GetCameraAngle();
            GetCameraPorition(angle, out Vector3 position, out float size);
            SetCameraPosition(position, new Vector2(0f, 90f), size);

            yield return new WaitForEndOfFrame();

            camera.transform.position = position + new Vector3(0, Math.Max(size * 1.1f, size + 5f) / 2 / Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad / 2), 0);
            camera.transform.rotation = Quaternion.Euler(90, (2 * Mathf.PI - angle - Vector3.forward.AbsoluteAngle()) * Mathf.Rad2Deg, 0);
            camera.cullingMask = LayerMask.GetMask("Road") | (3 << 24);
            camera.rect = new Rect(0f, 0f, 1f, 1f);

            bool smaaEnabled = false;
            var smaa = camera.GetComponent<SMAA>();
            if (smaa != null)
            {
                smaaEnabled = smaa.enabled;
                smaa.enabled = true;
            }

            var scale = ScreenshotSize * 4;

            camera.targetTexture = new RenderTexture(scale, scale, 24);
            var screenShot = new Texture2D(scale, scale, TextureFormat.RGB24, false);

            Singleton<RenderManager>.instance.UpdateCameraInfo();
            camera.Render();

            if (smaa != null)
                smaa.enabled = smaaEnabled;

            RenderTexture.active = camera.targetTexture;
            screenShot.ReadPixels(new Rect(0, 0, scale, scale), 0, 0);
            RenderTexture.active = null;
            Destroy(camera.targetTexture);

            SetCameraPosition(backupPosition, backupRotation, backupSize);
            camera.targetTexture = null;
            camera.cullingMask = backupMask;
            camera.rect = backupRect;

            var data = screenShot.GetPixels32();
            var image = new Image(scale, scale, TextureFormat.RGB24, data);
            image.Resize(ScreenshotSize, ScreenshotSize);

            callback(image);
        }
        private float GetCameraAngle()
        {
            var enters = Markup.Enters.ToArray();

            switch (enters.Length)
            {
                case 0: return 0;
                case 1: return enters[0].NormalAngle;
                default:
                    var sortEnters = enters.OrderBy(e => e.RoadHalfWidth).Reverse().ToArray();
                    var selectWidth = sortEnters[1].RoadHalfWidth * 0.9f;
                    var selectEnters = sortEnters.Where(e => e.RoadHalfWidth > selectWidth).ToArray();

                    var first = 0;
                    var second = 1;
                    var maxDelta = 0f;

                    for (var i = 0; i < selectEnters.Length; i += 1)
                    {
                        for (var j = i + 1; j < selectEnters.Length; j += 1)
                        {
                            var delte = Mathf.Abs(selectEnters[i].NormalAngle - selectEnters[j].NormalAngle);
                            if (delte > Mathf.PI)
                                delte = 2 * Mathf.PI - delte;
                            if (delte > maxDelta)
                            {
                                maxDelta = delte;
                                first = i;
                                second = j;
                            }
                        }
                    }

                    return (selectEnters[first].NormalAngle + selectEnters[second].NormalAngle) / 2;
            }
        }
        private void GetCameraPorition(float angle, out Vector3 position, out float size)
        {
            var points = Markup.Enters.SelectMany(e => new Vector3[] { e.FirstPointSide, e.LastPointSide }).ToArray();

            if (!points.Any())
            {
                position = Markup.Position;
                size = 10f;
                return;
            }

            var dir = angle.Direction();
            var normal = dir.Turn90(false);

            var rect = new Rect();
            foreach (var point in points)
            {
                Line2.Intersect(Markup.Position.XZ(), (Markup.Position + dir).XZ(), point.XZ(), (point + normal).XZ(), out float x, out _);
                Line2.Intersect(Markup.Position.XZ(), (Markup.Position + normal).XZ(), point.XZ(), (point + dir).XZ(), out float y, out _);

                Set(ref rect, x, y);
            }

            position = Markup.Position + dir * rect.center.x + normal * rect.center.y;
            size = Mathf.Max(rect.width, rect.height);

            static void Set(ref Rect rect, float x, float y)
            {
                if (x < rect.xMin)
                    rect.xMin = x;
                else if (x > rect.xMax)
                    rect.xMax = x;

                if (y < rect.yMin)
                    rect.yMin = y;
                else if (y > rect.yMax)
                    rect.yMax = y;
            }
        }
        private void SetCameraPosition(Vector3 position, Vector2 rotation, float size)
        {
            var cameraController = ToolsModifierControl.cameraController;
            cameraController.ClearTarget();
            cameraController.SetOverrideModeOff();
            cameraController.m_targetPosition = cameraController.m_currentPosition = position;
            cameraController.m_targetAngle = cameraController.m_currentAngle = rotation;
            cameraController.m_targetSize = cameraController.m_currentSize = size;
        }

        #endregion

        #region UTILITIES

        public TStyle GetStyleByModifier<TStyle, TStyleType>(TStyleType ifNotFound, bool allowNull = false)
            where TStyleType : Enum
            where TStyle : Style
        {
            var modifier = EnumExtension.GetEnumValues<StyleModifier>().FirstOrDefault(i => i.GetAttr<InputKeyAttribute, StyleModifier>() is InputKeyAttribute ik && ik.IsPressed);

            foreach (var style in EnumExtension.GetEnumValues<TStyleType>(i => true).Select(i => i.ToEnum<Style.StyleType, TStyleType>()))
            {
                if (StylesModifier.TryGetValue(style, out SavedInt saved) && (StyleModifier)saved.value == modifier)
                {
                    if ((style + 1).GetItem() == 0)
                    {
                        if (FromStyleBuffer<TStyle>(style.GetGroup(), out var bufferStyle))
                            return bufferStyle;
                    }
                    else if (TemplateManager.StyleManager.GetDefault<TStyle>(style) is TStyle defaultStyle)
                        return defaultStyle;
                    else if (allowNull)
                        return null;

                    break;
                }
            }

            return TemplateManager.StyleManager.GetDefault<TStyle>(ifNotFound.ToEnum<Style.StyleType, TStyleType>());
        }

        private static SavedInt GetSavedStylesModifier(Style.StyleType type) => new SavedInt($"{nameof(StylesModifier)}{type.ToInt()}", Settings.SettingsFile, (int)GetDefaultStylesModifier(type), true);
        private static StyleModifier GetDefaultStylesModifier(Style.StyleType style)
        {
            return style switch
            {
                Style.StyleType.LineDashed => StyleModifier.Without,
                Style.StyleType.LineSolid => StyleModifier.Shift,
                Style.StyleType.LineDoubleDashed => StyleModifier.Ctrl,
                Style.StyleType.LineDoubleSolid => StyleModifier.CtrlShift,
                Style.StyleType.EmptyLine => StyleModifier.Alt,
                Style.StyleType.LineBuffer => StyleModifier.CtrlAlt,

                Style.StyleType.StopLineSolid => StyleModifier.Without,
                Style.StyleType.StopLineBuffer => StyleModifier.CtrlAlt,

                Style.StyleType.CrosswalkZebra => StyleModifier.Without,
                Style.StyleType.CrosswalkBuffer => StyleModifier.CtrlAlt,

                Style.StyleType.FillerStripe => StyleModifier.Without,
                Style.StyleType.FillerBuffer => StyleModifier.CtrlAlt,

                _ => StyleModifier.NotSet,
            };
        }
        public string GetModifierToolTip<StyleType>(string text)
            where StyleType : Enum
        {
            var modifiers = GetStylesModifier<StyleType>().ToArray();
            return modifiers.Any() ? $"{text}:\n{string.Join("\n", modifiers)}" : text;
        }
        private IEnumerable<string> GetStylesModifier<StyleType>()
            where StyleType : Enum
        {
            foreach (var style in EnumExtension.GetEnumValues<StyleType>(i => true))
            {
                var general = (Style.StyleType)(object)style;
                var modifier = (StyleModifier)StylesModifier[general].value;
                if (modifier != StyleModifier.NotSet)
                    yield return $"{general.Description()} - {modifier.Description()}";
            }
        }

        public event Action<Style.StyleType> OnStyleToBuffer;
        public void ToStyleBuffer(Style.StyleType type, Style style)
        {
            var group = type.GetGroup();
            StyleBuffer[group] = style.Copy();
            OnStyleToBuffer?.Invoke(group);
        }
        public bool FromStyleBuffer<T>(Style.StyleType type, out T style)
            where T : Style
        {
            if (StyleBuffer.TryGetValue(type.GetGroup(), out var bufferStyle) && bufferStyle is T tStyle)
            {
                style = (T)tStyle.Copy();
                return true;
            }
            else
            {
                style = null;
                return false;
            }
        }
        public bool IsStyleInBuffer(Style.StyleType type) => StyleBuffer.ContainsKey(type.GetGroup());

        #endregion
    }
    public abstract class NodeMarkupToolMode : BaseToolMode, IToolMode<ToolModeType>, IToolModePanel
    {
        public abstract ToolModeType Type { get; }
        public virtual bool ShowPanel => true;
        protected new NodeMarkupTool Tool => NodeMarkupTool.Instance;
        protected NodeMarkupPanel Panel => NodeMarkupPanel.Instance;
        public Markup Markup => Tool.Markup;
    }
    public enum ToolModeType
    {
        None = 0,

        Select = 1,
        MakeLine = 2,
        MakeCrosswalk = 4,
        MakeFiller = 8,
        PanelAction = 16,
        PasteEntersOrder = 32,
        EditEntersOrder = 64,
        ApplyIntersectionTemplateOrder = 128,
        PointsOrder = 256,
        DragPoint = 512,

        MakeItem = MakeLine | MakeCrosswalk
    }
    public class ThreadingExtension : ThreadingExtensionBase
    {
        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            if (!UIView.HasModalInput() && !UIView.HasInputFocus() && NodeMarkupTool.ActivationShortcut.InputKey.IsKeyUp())
            {
                Mod.Logger.Debug($"On press shortcut");
                NodeMarkupTool.Instance.ToggleTool();
            }
        }
    }
}
