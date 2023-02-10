using ColossalFramework;
using ColossalFramework.Importers;
using ColossalFramework.Math;
using ColossalFramework.UI;
using HarmonyLib;
using IMT.Manager;
using IMT.UI;
using IMT.UI.Panel;
using IMT.Utilities;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;

namespace IMT.Tools
{
    public class IntersectionMarkingTool : BaseTool<Mod, IntersectionMarkingTool, ToolModeType>
    {
        #region STATIC

        public static IntersectionMarkingToolShortcut ActivationShortcut { get; } = new IntersectionMarkingToolShortcut(nameof(ActivationShortcut), nameof(CommonLocalize.Settings_ShortcutActivateTool), SavedInputKey.Encode(KeyCode.L, true, false, false));
        public static IntersectionMarkingToolShortcut SelectionStepOverShortcut { get; } = new IntersectionMarkingToolShortcut(nameof(SelectionStepOverShortcut), nameof(CommonLocalize.Settings_ShortcutSelectionStepOver), SavedInputKey.Encode(KeyCode.Space, true, false, false), () => SingletonTool<IntersectionMarkingTool>.Instance.SelectionStepOver(), ToolModeType.Select);

        public static IntersectionMarkingToolShortcut EnterUndergroundShortcut { get; } = new IntersectionMarkingToolShortcut(nameof(EnterUndergroundShortcut), nameof(Localize.Settings_ShortcutEnterUnderground), SavedInputKey.Encode(KeyCode.PageDown, false, false, false), () => (SingletonTool<IntersectionMarkingTool>.Instance.Mode as SelectToolMode)?.ChangeUnderground(true), ToolModeType.Select);
        public static IntersectionMarkingToolShortcut ExitUndergroundShortcut { get; } = new IntersectionMarkingToolShortcut(nameof(ExitUndergroundShortcut), nameof(Localize.Settings_ShortcutExitUnderground), SavedInputKey.Encode(KeyCode.PageUp, false, false, false), () => (SingletonTool<IntersectionMarkingTool>.Instance.Mode as SelectToolMode)?.ChangeUnderground(false), ToolModeType.Select);

        public static IntersectionMarkingToolShortcut AddRuleShortcut { get; } = new IntersectionMarkingToolShortcut(nameof(AddRuleShortcut), nameof(Localize.Settings_ShortcutAddNewLineRule), SavedInputKey.Encode(KeyCode.A, true, true, false), () =>
        {
            if (SingletonItem<IntersectionMarkingToolPanel>.Instance.CurrentEditor is UI.Editors.LinesEditor linesEditor)
                linesEditor.AddRuleShortcut();
        });
        public static IntersectionMarkingToolShortcut DeleteAllShortcut { get; } = new IntersectionMarkingToolShortcut(nameof(DeleteAllShortcut), nameof(Localize.Settings_ShortcutDeleteAllNodeLines), SavedInputKey.Encode(KeyCode.D, true, true, false), () => SingletonTool<IntersectionMarkingTool>.Instance.DeleteAllMarking());
        public static IntersectionMarkingToolShortcut ResetOffsetsShortcut { get; } = new IntersectionMarkingToolShortcut(nameof(ResetOffsetsShortcut), nameof(Localize.Settings_ShortcutResetPointsOffset), SavedInputKey.Encode(KeyCode.R, true, true, false), () => SingletonTool<IntersectionMarkingTool>.Instance.ResetAllOffsets());
        public static IntersectionMarkingToolShortcut AddFillerShortcut { get; } = new IntersectionMarkingToolShortcut(nameof(AddFillerShortcut), nameof(Localize.Settings_ShortcutAddNewFiller), SavedInputKey.Encode(KeyCode.F, true, true, false), () => SingletonTool<IntersectionMarkingTool>.Instance.StartCreateFiller());
        public static IntersectionMarkingToolShortcut CopyMarkingShortcut { get; } = new IntersectionMarkingToolShortcut(nameof(CopyMarkingShortcut), nameof(Localize.Settings_ShortcutCopyMarking), SavedInputKey.Encode(KeyCode.C, true, true, false), () => SingletonTool<IntersectionMarkingTool>.Instance.CopyMarking());
        public static IntersectionMarkingToolShortcut PasteMarkingShortcut { get; } = new IntersectionMarkingToolShortcut(nameof(PasteMarkingShortcut), nameof(Localize.Settings_ShortcutPasteMarking), SavedInputKey.Encode(KeyCode.V, true, true, false), () => SingletonTool<IntersectionMarkingTool>.Instance.PasteMarking());
        public static IntersectionMarkingToolShortcut EditMarkingShortcut { get; } = new IntersectionMarkingToolShortcut(nameof(EditMarkingShortcut), nameof(Localize.Settings_ShortcutEditMarking), SavedInputKey.Encode(KeyCode.E, true, true, false), () => SingletonTool<IntersectionMarkingTool>.Instance.EditMarking());
        public static IntersectionMarkingToolShortcut CreateEdgeLinesShortcut { get; } = new IntersectionMarkingToolShortcut(nameof(CreateEdgeLinesShortcut), nameof(Localize.Settings_ShortcutCreateEdgeLines), SavedInputKey.Encode(KeyCode.W, true, true, false), () => SingletonTool<IntersectionMarkingTool>.Instance.CreateEdgeLines());
        public static IntersectionMarkingToolShortcut SaveAsIntersectionTemplateShortcut { get; } = new IntersectionMarkingToolShortcut(nameof(SaveAsIntersectionTemplateShortcut), nameof(Localize.Settings_ShortcutSaveAsPreset), SavedInputKey.Encode(KeyCode.S, true, true, false), () => SingletonTool<IntersectionMarkingTool>.Instance.SaveAsIntersectionTemplate());
        public static IntersectionMarkingToolShortcut CutLinesByCrosswalksShortcut { get; } = new IntersectionMarkingToolShortcut(nameof(CutLinesByCrosswalksShortcut), nameof(Localize.Settings_ShortcutCutLinesByCrosswalks), SavedInputKey.Encode(KeyCode.T, true, true, false), () => SingletonTool<IntersectionMarkingTool>.Instance.CutByCrosswalks());
        public static IntersectionMarkingToolShortcut ApplyBetweenIntersectionsShortcut { get; } = new IntersectionMarkingToolShortcut(nameof(ApplyBetweenIntersectionsShortcut), nameof(Localize.Settings_ShortcutApplyBetweenIntersections), SavedInputKey.Encode(KeyCode.G, true, true, false), () => SingletonTool<IntersectionMarkingTool>.Instance.ApplyBetweenIntersections());
        public static IntersectionMarkingToolShortcut ApplyWholeStreetShortcut { get; } = new IntersectionMarkingToolShortcut(nameof(ApplyWholeStreetShortcut), nameof(Localize.Settings_ShortcutApplyWholeStreet), SavedInputKey.Encode(KeyCode.B, true, true, false), () => SingletonTool<IntersectionMarkingTool>.Instance.ApplyWholeStreet());

        public static IEnumerable<Shortcut> ToolShortcuts
        {
            get
            {
                yield return SelectionStepOverShortcut;
                yield return EnterUndergroundShortcut;
                yield return ExitUndergroundShortcut;
                yield return AddRuleShortcut;
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
        public override IEnumerable<Shortcut> Shortcuts
        {
            get
            {
                foreach (var shortcut in ToolShortcuts)
                    yield return shortcut;

                if (Mode is IShortcutMode mode)
                {
                    foreach (var shortcut in mode.Shortcuts)
                        yield return shortcut;
                }
            }
        }

        public static Dictionary<Style.StyleType, SavedInt> StylesModifier { get; } = EnumExtension.GetEnumValues<Style.StyleType>(v => v.IsItem()).ToDictionary(i => i, i => GetSavedStylesModifier(i));

        #endregion

        #region PROPERTIES

        protected override IToolMode DefaultMode => ToolModes[ToolModeType.Select];
        protected override bool ShowToolTip => base.ShowToolTip && (Settings.ShowToolTip || Mode.Type == ToolModeType.Select);
        public override Shortcut Activation => ActivationShortcut;

        public Marking Marking { get; private set; }
        public bool IsUnderground => Marking?.IsUnderground ?? false;

        public IntersectionMarkingToolPanel Panel => SingletonItem<IntersectionMarkingToolPanel>.Instance;
        public IntersectionTemplate MarkingBuffer { get; private set; }
        public bool IsMarkingBufferEmpty => MarkingBuffer == null;
        private Dictionary<Style.StyleType, Style> StyleBuffer { get; } = new Dictionary<Style.StyleType, Style>();

        protected override UITextureAtlas UUIAtlas => IMTTextures.Atlas;
        protected override string UUINormalSprite => IMTTextures.UUIButtonNormal;
        protected override string UUIHoveredSprite => IMTTextures.UUIButtonHovered;
        protected override string UUIPressedSprite => IMTTextures.UUIButtonPressed;
        protected override string UUIDisabledSprite => /*IMTTextures.UUIDisabled;*/string.Empty;

        #endregion

        #region BASIC

        protected override void InitProcess()
        {
            base.InitProcess();
            IntersectionMarkingToolPanel.CreatePanel();
        }
        protected override IEnumerable<IToolMode<ToolModeType>> GetModes()
        {
            yield return CreateToolMode<SelectToolMode>();
            yield return CreateToolMode<MakeLineToolMode>();
            yield return CreateToolMode<MakeCrosswalkToolMode>();
            yield return CreateToolMode<MakeFillerToolMode>();
            yield return CreateToolMode<DragPointToolMode>();
            yield return CreateToolMode<PasteMarkingToolMode>();
            yield return CreateToolMode<ApplyPresetToolMode>();
            yield return CreateToolMode<ApplyAllPresetToolMode>();
            yield return CreateToolMode<EditEntersOrderToolMode>();
            yield return CreateToolMode<LinkPresetToolMode>();
            yield return CreateToolMode<PointsOrderToolMode>();
        }
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
        protected override void SetModeNow(IToolMode mode)
        {
            base.SetModeNow(mode);
            Panel.Active = (Mode as IntersectionMarkingToolMode)?.ShowPanel == true;
        }
        public void SetMarking(Marking marking)
        {
            Marking = marking;
            Panel.SetMarking(Marking);
        }
        #endregion

        #region GUI

        private void StartCreateFiller()
        {
            if ((Marking.Support & Marking.SupportType.Fillers) != 0)
            {
                SetMode(ToolModeType.MakeFiller);
                if (NextMode is MakeFillerToolMode fillerToolMode)
                    fillerToolMode.DisableByAlt = false;
            }
        }
        private void SelectionStepOver()
        {
            if (Mode is SelectToolMode selectMode)
                selectMode.IgnoreSelected();
        }
        protected override bool CheckInfoMode(InfoManager.InfoMode mode, InfoManager.SubInfoMode subInfo) => (mode == InfoManager.InfoMode.None || mode == InfoManager.InfoMode.Underground) && subInfo == InfoManager.SubInfoMode.Default;

        private void DeleteAllMarking()
        {
            SingletonMod<Mod>.Logger.Debug($"Delete all markings");

            var messageBox = MessageBox.Show<YesNoMessageBox>();
            messageBox.CaptionText = Localize.Tool_ClearMarkingsCaption;
            messageBox.MessageText = string.Format($"{Localize.Tool_ClearMarkingsMessage}\n{IntersectionMarkingToolMessageBox.CantUndone}", Marking.Id);
            messageBox.OnButton1Click = Delete;

            bool Delete()
            {
                Marking.Clear();
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
                var messageBox = MessageBox.Show<YesNoMessageBox>();
                messageBox.CaptionText = string.Format(Localize.Tool_DeleteCaption, item.DeleteCaptionDescription);
                messageBox.MessageText = $"{string.Format(Localize.Tool_DeleteMessage, item.DeleteMessageDescription, item)}\n{IntersectionMarkingToolMessageBox.CantUndone}\n\n{additional}";
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

        private void ResetAllOffsets()
        {
            SingletonMod<Mod>.Logger.Debug($"Reset all points offsets");

            if (Settings.DeleteWarnings)
            {
                var messageBox = MessageBox.Show<YesNoMessageBox>();
                messageBox.CaptionText = Localize.Tool_ResetOffsetsCaption;
                messageBox.MessageText = $"{string.Format(Localize.Tool_ResetOffsetsMessage, Marking.Id)}\n{IntersectionMarkingToolMessageBox.CantUndone}";
                messageBox.OnButton1Click = Reset;
            }
            else
                Reset();

            bool Reset()
            {
                Marking.ResetOffsets();
                Panel.UpdatePanel();
                return true;
            }
        }
        private void CopyMarking()
        {
            SingletonMod<Mod>.Logger.Debug($"Copy marking");
            MarkingBuffer = new IntersectionTemplate(Marking);
            Panel?.RefreshHeader();
        }
        private void PasteMarking()
        {
            SingletonMod<Mod>.Logger.Debug($"Paste marking");

            if (MarkingBuffer == null)
                return;

            if (Settings.DeleteWarnings && !Marking.IsEmpty)
            {
                var messageBox = MessageBox.Show<YesNoMessageBox>();
                messageBox.CaptionText = Localize.Tool_PasteMarkingsCaption;
                messageBox.MessageText = $"{Localize.Tool_PasteMarkingsMessage}\n{IntersectionMarkingToolMessageBox.ItWillReplace}\n{IntersectionMarkingToolMessageBox.CantUndone}";
                messageBox.OnButton1Click = Paste;
            }
            else
                Paste();

            bool Paste()
            {
                ObjectsMap map = GetMap(MarkingBuffer);

                if (map != null)
                {
                    Marking.Clear();
                    Marking.FromXml(SingletonMod<Mod>.Version, MarkingBuffer.Data, map);
                    Panel.UpdatePanel();
                }
                else
                {
                    BaseOrderToolMode.IntersectionTemplate = MarkingBuffer;
                    SetMode(ToolModeType.PasteMarking);
                }

                return true;
            }
        }
        private void EditMarking()
        {
            SingletonMod<Mod>.Logger.Debug($"Edit marking order");

            BaseOrderToolMode.IntersectionTemplate = new IntersectionTemplate(Marking);
            SetMode(ToolModeType.EditEntersOrder);
        }
        public void LinkPreset(IntersectionTemplate template, string roadName)
        {
            SingletonMod<Mod>.Logger.Debug($"Link preset");

            if (ToolModes[ToolModeType.LinkPreset] is LinkPresetToolMode linkMode)
            {
                BaseOrderToolMode.IntersectionTemplate = template;
                linkMode.RoadName = roadName;
            }

            SetMode(ToolModeType.LinkPreset);
        }

        private void CreateEdgeLines()
        {
            SingletonMod<Mod>.Logger.Debug($"Create edge lines");

            foreach (var enter in Marking.Enters)
            {
                var pair = new MarkingPointPair(enter.LastPoint, enter.Next.FirstPoint);
                if (!Marking.TryGetLine(pair, out MarkingLine line))
                {
                    line = Marking.AddRegularLine(pair, null);
                    Panel.AddLine(line);
                }

                if (line != null)
                    Panel.SelectLine(line);
            }
        }
        private void SaveAsIntersectionTemplate()
        {
            SingletonMod<Mod>.Logger.Debug($"Save as intersection template");

            StartCoroutine(MakeScreenshot(Callback));

            void Callback(Image image)
            {
                if (SingletonManager<IntersectionTemplateManager>.Instance.AddTemplate(Marking, image, out IntersectionTemplate template))
                    Panel.EditIntersectionTemplate(template);
            }
        }
        private void CutByCrosswalks()
        {
            SingletonMod<Mod>.Logger.Debug($"Cut by crosswalk");

            foreach (var crosswalk in Marking.Crosswalks)
                Marking.CutLinesByCrosswalk(crosswalk);
        }

        #region APPLY

        public void ApplyIntersectionTemplate(IntersectionTemplate template)
        {
            SingletonMod<Mod>.Logger.Debug($"Apply intersection template");

            ObjectsMap map = GetMap(template);

            if (map != null)
            {
                Marking.Clear();
                Marking.FromXml(SingletonMod<Mod>.Version, template.Data, map);
                Panel.UpdatePanel();
            }
            else
            {
                BaseOrderToolMode.IntersectionTemplate = template;
                SetMode(ToolModeType.ApplyPreset);
            }
        }
        public void ApplyAllIntersectionTemplate(IntersectionTemplate template)
        {
            if (Marking.Type == MarkingType.Segment)
            {
                var segmentId = Marking.Id;
                ref var sourceSegment = ref segmentId.GetSegment();
                if (sourceSegment.Info != null)
                {
                    if (SingletonManager<RoadTemplateManager>.Instance.TryGetPreset(sourceSegment.Info.name, out var presetId, out var flip, out var invert) && SingletonManager<IntersectionTemplateManager>.Instance.TryGetTemplate(presetId, out var preset))
                    {
                        ApplyPresetToAsset(sourceSegment.Info, preset, flip, invert);
                    }
                    else
                    {
                        BaseOrderToolMode.IntersectionTemplate = template;
                        SetMode(ToolModeType.ApplyAllPreset);
                    }
                }
            }
        }
        public void ApplyPresetToAsset(NetInfo info, IntersectionTemplate preset, bool flip, bool invert)
        {
            SingletonMod<Mod>.Logger.Debug($"Apply intersection template to all segments of asset {info.name}");

            for (ushort i = 0; i < NetManager.MAX_SEGMENT_COUNT; i += 1)
            {
                ref var segment = ref i.GetSegment();

                if ((segment.m_flags & NetSegment.Flags.Created) == 0)
                    continue;

                if (segment.Info != info)
                    continue;

                ApplyLinkedMarking(preset, i, segment.m_startNode, segment.m_endNode, flip, invert);
            }
        }
        private ObjectsMap GetMap(IntersectionTemplate source)
        {
            ObjectsMap map = null;
            if (Settings.AutoApplyPasting && Marking.EntersCount == source.Enters.Length)
            {
                var targetPoints = Marking.Enters.Select(e => e.PointCount).ToArray();
                var sourcePoints = source.Enters.Select(e => e.PointCount).ToArray();
                var invertedPoints = source.Enters.Select(e => e.PointCount).Reverse().ToArray();

                var direct = MatchMarkings(sourcePoints, targetPoints);
                var invert = MatchMarkings(invertedPoints, targetPoints);

                if (direct.Length == 1 && invert.Length == 0)
                {
                    map = new ObjectsMap();
                    var targetEnters = Marking.Enters.ToArray();
                    var sourceEnters = source.Enters;

                    for (int indexTarget = 0; indexTarget < targetPoints.Length; indexTarget += 1)
                    {
                        var indexSource = indexTarget.NextIndex(sourcePoints.Length, direct[0]);
                        switch (Marking.Type)
                        {
                            case MarkingType.Node:
                                map.AddSegment(sourceEnters[indexSource].Id, targetEnters[indexTarget].Id);
                                break;
                            case MarkingType.Segment:
                                map.AddNode(sourceEnters[indexSource].Id, targetEnters[indexTarget].Id);
                                break;
                        }
                    }
                }
                else if (Settings.AutoApplyPastingType == 1 && direct.Length == 0 && invert.Length == 1)
                {
                    map = new ObjectsMap();
                    var targetEnters = Marking.Enters.ToArray();
                    var sourceEnters = source.Enters.Reverse().ToArray();

                    for (int indexTarget = 0; indexTarget < targetPoints.Length; indexTarget += 1)
                    {
                        var indexSource = indexTarget.NextIndex(sourcePoints.Length, invert[0]);
                        switch (Marking.Type)
                        {
                            case MarkingType.Node:
                                map.AddSegment(sourceEnters[indexSource].Id, targetEnters[indexTarget].Id);
                                break;
                            case MarkingType.Segment:
                                map.AddNode(sourceEnters[indexSource].Id, targetEnters[indexTarget].Id);
                                break;
                        }

                        for (var pointI = 0; pointI <= targetEnters[indexTarget].PointCount; pointI += 1)
                        {
                            map.AddPoint(targetEnters[indexTarget].Id, (byte)(pointI + 1), (byte)(targetEnters[indexTarget].PointCount - pointI));
                        }
                    }
                }
            }
            return map;
        }
        public static void ApplyDefaultMarking(NetInfo info, ushort segmentId, ushort startNode, ushort endNode)
        {
            if (SingletonManager<RoadTemplateManager>.Instance.TryGetPreset(info.name, out var presetId, out var flip, out var invert) && SingletonManager<IntersectionTemplateManager>.Instance.TryGetTemplate(presetId, out var preset) && ApplyLinkedMarking(preset, segmentId, startNode, endNode, flip, invert))
                return;

            if (Settings.ApplyMarkingFromAssets && SingletonItem<NetworkAssetDataExtension>.Instance.OnPlaceAsset(info, segmentId, startNode, endNode))
                return;

            if (SegmentMarkingManager.RemovedMarking is IntersectionTemplate removed && ApplyRemovedMarking(removed, segmentId, startNode, endNode))
                return;
        }
        private static bool ApplyRemovedMarking(IntersectionTemplate removed, ushort segmentId, ushort startNode, ushort endNode)
        {
            var firstNode = removed.Enters[0].Id;
            var secondNode = removed.Enters[1].Id;

            if (startNode == firstNode && endNode == secondNode || startNode == secondNode && endNode == firstNode)
            {
                var marking = SingletonManager<SegmentMarkingManager>.Instance[segmentId];
                var map = new ObjectsMap();
                map.AddNode(startNode, endNode);
                map.AddNode(endNode, startNode);
                marking.FromXml(SingletonMod<Mod>.Version, removed.Data, map);
                return true;
            }

            return false;
        }
        private static bool ApplyLinkedMarking(IntersectionTemplate preset, ushort segmentId, ushort startNode, ushort endNode, bool flip, bool invert)
        {
            var marking = SingletonManager<SegmentMarkingManager>.Instance[segmentId];
            ref var segment = ref segmentId.GetSegment();
            flip ^= (segment.m_flags & NetSegment.Flags.Invert) != 0;

            var map = new ObjectsMap(invert);
            map.AddNode(preset.Enters[0].Id, !flip ? startNode : endNode);
            map.AddNode(preset.Enters[1].Id, !flip ? endNode : startNode);
            if (invert)
            {
                foreach (var enter in marking.Enters)
                    map.AddInvertEnter(enter);
            }

            marking.Clear();
            marking.FromXml(SingletonMod<Mod>.Version, preset.Data, map);
            return true;
        }

        private int[] MatchMarkings(int[] source, int[] target)
        {
            var matches = new List<int>();

            var length = Math.Min(source.Length, target.Length);
            for (int shift = 0; shift < length; shift += 1)
            {
                bool match = true;
                for (int indexTarget = 0; indexTarget < length; indexTarget += 1)
                {
                    var indexSource = indexTarget.NextIndex(source.Length, shift);
                    if (target[indexTarget] != source[indexSource])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                    matches.Add(shift);
            }

            return matches.ToArray();
        }

        private delegate ushort? SegmentGetter(ushort[] segmentIds, ushort beforeSegmentId);
        private void ApplyBetweenIntersections()
        {
            SingletonMod<Mod>.Logger.Debug($"Apply between intersections");

            if (Marking.Type != MarkingType.Segment)
                return;

            ref var segment = ref Marking.Id.GetSegment();
            var startNode = segment.m_startNode;
            var endNode = segment.m_endNode;
            var info = segment.Info;

            if (Settings.DeleteWarnings)
            {
                var messageBox = MessageBox.Show<YesNoMessageBox>();
                messageBox.CaptionText = Localize.Tool_ApplyBetweenIntersectionsCaption;
                messageBox.MessageText = $"{Localize.Tool_ApplyBetweenIntersectionsMessage}\n{IntersectionMarkingToolMessageBox.ItWillReplace}\n{IntersectionMarkingToolMessageBox.CantUndone}";
                messageBox.OnButton1Click = Apply;
            }
            else
                Apply();


            bool Apply()
            {
                var config = Marking.ToXml();
                this.Apply(Marking.Id, startNode, endNode, info, config, SegmentGetter);
                this.Apply(Marking.Id, endNode, startNode, info, config, SegmentGetter);
                return true;
            }

            ushort? SegmentGetter(ushort[] segmentIds, ushort beforeSegmentId)
            {
                if (segmentIds.Length != 2)
                    return null;
                else
                {
                    var id = segmentIds[segmentIds[0] == beforeSegmentId ? 1 : 0];
                    var nextSegment = id.GetSegment();
                    return nextSegment.Info == info ? id : null;
                }
            }
        }
        private void ApplyWholeStreet()
        {
            SingletonMod<Mod>.Logger.Debug($"Apply to whole street");

            if (Marking.Type != MarkingType.Segment)
                return;

            ref var segment = ref Marking.Id.GetSegment();
            var startNode = segment.m_startNode;
            var endNode = segment.m_endNode;
            var info = segment.Info;
            var nameSeed = segment.m_nameSeed;

            if (Settings.DeleteWarnings)
            {
                var streetName = Singleton<NetManager>.instance.GetSegmentName(Marking.Id);
                var messageBox = MessageBox.Show<YesNoMessageBox>();
                messageBox.CaptionText = Localize.Tool_ApplyWholeStreetCaption;
                messageBox.MessageText = $"{string.Format(Localize.Tool_ApplyWholeStreetMessage, streetName)}\n{IntersectionMarkingToolMessageBox.ItWillReplace}\n{IntersectionMarkingToolMessageBox.CantUndone}";
                messageBox.OnButton1Click = Apply;
            }
            else
                Apply();

            bool Apply()
            {
                var config = Marking.ToXml();
                this.Apply(Marking.Id, startNode, endNode, info, config, SegmentGetter);
                this.Apply(Marking.Id, endNode, startNode, info, config, SegmentGetter);
                return true;
            }

            ushort? SegmentGetter(ushort[] segmentIds, ushort beforeSegmentId)
            {
                foreach (var id in segmentIds)
                {
                    if (id == beforeSegmentId)
                        continue;

                    ref var nextSegment = ref id.GetSegment();
                    if (nextSegment.Info == info && nextSegment.m_nameSeed == nameSeed)
                        return id;
                }
                return null;
            }
        }
        void Apply(ushort startSegmentId, ushort nearNodeId, ushort farNodeId, NetInfo info, XElement config, SegmentGetter segmentGetter)
        {
            var nodeId = (ushort?)nearNodeId;
            var segmentId = (ushort?)startSegmentId;

            var nodes = new HashSet<ushort>();
            var segments = new HashSet<ushort>() { startSegmentId };

            while (nodes.Count < NetManager.MAX_NODE_COUNT && segments.Count < NetManager.MAX_SEGMENT_COUNT)
            {
                segmentId = ApplyToNode(nodeId.Value, segmentId.Value, nearNodeId, farNodeId, config, segmentGetter);
                if (segmentId != null && !segments.Contains(segmentId.Value))
                    segments.Add(segmentId.Value);
                else
                    break;

                nodeId = ApplyToSegment(segmentId.Value, nodeId.Value, nearNodeId, farNodeId, info, config);
                if (nodeId != null && !nodes.Contains(nodeId.Value))
                    nodes.Add(nodeId.Value);
                else
                    break;
            }
        }
        ushort? ApplyToNode(ushort nodeId, ushort beforeSegmentId, ushort nearNodeId, ushort farNodeId, XElement config, SegmentGetter nextGetter)
        {
            ref var node = ref nodeId.GetNode();

            var nodeSegmentIds = node.SegmentIds().ToArray();
            var nextSegmentId = nextGetter(nodeSegmentIds, beforeSegmentId);

            if (nextSegmentId != null && nodeSegmentIds.Length == 2 && (node.m_flags & (NetNode.Flags.Bend | NetNode.Flags.Middle)) != 0)
            {
                var map = new ObjectsMap();
                map.AddSegment(nearNodeId, beforeSegmentId);
                map.AddSegment(farNodeId, nextSegmentId.Value);
                var marking = SingletonManager<NodeMarkingManager>.Instance[nodeId];
                marking.Clear();
                marking.FromXml(SingletonMod<Mod>.Version, config, map);
            }

            return nextSegmentId;
        }
        ushort? ApplyToSegment(ushort segmentId, ushort beforeNodeId, ushort nearNodeId, ushort farNodeId, NetInfo info, XElement config)
        {
            ref var segment = ref segmentId.GetSegment();
            if (segment.Info != info)
                return null;

            var nextNodeId = segment.m_startNode == beforeNodeId ? segment.m_endNode : segment.m_startNode;

            var map = new ObjectsMap();
            map.AddNode(farNodeId, beforeNodeId);
            map.AddNode(nearNodeId, nextNodeId);
            var marking = SingletonManager<SegmentMarkingManager>.Instance[segmentId];
            marking.Clear();
            marking.FromXml(SingletonMod<Mod>.Version, config, map);

            return nextNodeId;
        }

        #endregion

        #region SCREENSHOT

        private int ScreenshotSize => 400;

        private IEnumerator MakeScreenshot(Action<Image> callback)
        {
            if (callback == null)
                yield break;

            SingletonMod<Mod>.Logger.Debug($"Make screenshot");

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
            var enters = Marking.Enters.ToArray();

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
            var points = Marking.Enters.SelectMany(e => new Vector3[] { e.FirstPointSide, e.LastPointSide }).ToArray();

            if (!points.Any())
            {
                position = Marking.Position;
                size = 10f;
                return;
            }

            var dir = angle.Direction();
            var normal = dir.Turn90(false);

            var rect = new Rect();
            foreach (var point in points)
            {
                Line2.Intersect(XZ(Marking.Position), XZ(Marking.Position + dir), XZ(point), XZ(point + normal), out float x, out _);
                Line2.Intersect(XZ(Marking.Position), XZ(Marking.Position + normal), XZ(point), XZ(point + dir), out float y, out _);

                Set(ref rect, x, y);
            }

            position = Marking.Position + dir * rect.center.x + normal * rect.center.y;
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

        #endregion

        #region UTILITIES

        public TStyle GetStyleByModifier<TStyle, TStyleType>(NetworkType networkType, LineType lineType, TStyleType ifNotFound, bool allowNull = false)
            where TStyleType : Enum
            where TStyle : Style
        {
            var modifier = EnumExtension.GetEnumValues<StyleModifier>().FirstOrDefault(i => i.GetAttr<InputKeyAttribute, StyleModifier>() is InputKeyAttribute ik && ik.IsPressed);

            foreach (var style in EnumExtension.GetEnumValues<TStyleType>(i => true).Select(i => i.ToEnum<Style.StyleType, TStyleType>()))
            {
                if ((style.GetNetworkType() & networkType) == 0 || (style.GetLineType() & lineType) == 0)
                    continue;

                if (StylesModifier.TryGetValue(style, out SavedInt saved) && (StyleModifier)saved.value == modifier)
                {
                    if ((style + 1).GetItem() == 0)
                    {
                        if (FromStyleBuffer<TStyle>(style.GetGroup(), out var bufferStyle))
                            return bufferStyle;
                    }
                    else if (SingletonManager<StyleTemplateManager>.Instance.GetDefault<TStyle>(style) is TStyle defaultStyle)
                        return defaultStyle;
                    else if (allowNull)
                        return null;

                    break;
                }
            }

            {
                var defaultStyle = ifNotFound.ToEnum<Style.StyleType, TStyleType>();
                if ((defaultStyle.GetNetworkType() & networkType) == 0 && (defaultStyle.GetLineType() & lineType) == 0)
                {
                    foreach (var style in EnumExtension.GetEnumValues<TStyleType>(i => true).Select(i => i.ToEnum<Style.StyleType, TStyleType>()))
                    {
                        if ((style.GetNetworkType() & networkType) != 0 && (style.GetLineType() & lineType) != 0)
                        {
                            defaultStyle = style;
                            break;
                        }
                    }
                }
                return SingletonManager<StyleTemplateManager>.Instance.GetDefault<TStyle>(defaultStyle);
            }
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
        public string GetModifierToolTip<StyleType>(string text, NetworkType networkType, LineType lineType)
            where StyleType : Enum
        {
            var modifiers = GetStylesModifier<StyleType>(networkType, lineType).ToArray();
            return modifiers.Any() ? $"{text}:\n{string.Join("\n", modifiers)}" : text;
        }
        private IEnumerable<string> GetStylesModifier<StyleType>(NetworkType networkType, LineType lineType)
            where StyleType : Enum
        {
            foreach (var style in EnumExtension.GetEnumValues<StyleType>(i => true))
            {
                if ((style.GetNetworkType() & networkType) == 0 && (style.GetLineType() & lineType) == 0)
                    continue;

                var general = (Style.StyleType)(object)style;
                var modifier = (StyleModifier)StylesModifier[general].value;
                if (modifier != StyleModifier.NotSet)
                    yield return $"{general.Description()} - {modifier.Description().AddInfoColor()}";
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
    public abstract class IntersectionMarkingToolMode : BaseToolMode<IntersectionMarkingTool>, IToolMode<ToolModeType>, IToolModePanel
    {
        public abstract ToolModeType Type { get; }
        public virtual bool ShowPanel => true;
        protected IntersectionMarkingToolPanel Panel => SingletonItem<IntersectionMarkingToolPanel>.Instance;
        public Marking Marking => Tool.Marking;
        protected bool IsUnderground => Tool.IsUnderground;

        public override void RenderGeometry(RenderManager.CameraInfo cameraInfo)
        {
            if (Settings.IlluminationAtNight)
            {
                var lightSystem = Singleton<RenderManager>.instance.lightSystem;
                var intensity = Settings.IlluminationIntensity * MathUtils.SmoothStep(0.9f, 0.1f, lightSystem.DayLightIntensity);
                if (intensity > 0.001f)
                {
#if DEBUG
                    var position = Marking.CenterPosition + Vector3.up * Marking.CenterRadius * Settings.IlluminationDelta;
#else
                    var position = Marking.CenterPosition + Vector3.up * Marking.CenterRadius * 1.41f;
#endif
                    lightSystem.DrawLight(LightType.Spot, position, Vector3.down, Vector3.zero, Color.white, intensity, Marking.CenterRadius * 2f, 90f, 1f, false);
                }
            }
        }
    }
    public enum ToolModeType
    {
        None = 0,

        Select = 1 << 0,
        MakeLine = 1 << 1,
        MakeCrosswalk = 1 << 2,
        MakeFiller = 1 << 3,
        PanelAction = 1 << 4,
        PasteMarking = 1 << 5,
        ApplyPreset = 1 << 6,
        ApplyAllPreset = 1 << 7,
        EditEntersOrder = 1 << 8,
        LinkPreset = 1 << 9,
        PointsOrder = 1 << 10,
        DragPoint = 1 << 11,

        MakeItem = MakeLine | MakeCrosswalk,
        Order = ApplyPreset | EditEntersOrder | LinkPreset | PointsOrder,
    }
    public interface IShortcutMode
    {
        public IEnumerable<Shortcut> Shortcuts { get; }
    }
    public class IntersectionMarkingToolShortcut : ToolShortcut<Mod, IntersectionMarkingTool, ToolModeType>
    {
        public IntersectionMarkingToolShortcut(string name, string labelKey, InputKey key, Action action = null, ToolModeType modeType = ToolModeType.MakeItem) : base(name, labelKey, key, action, modeType) { }
    }
    public class IntersectionMarkingToolThreadingExtension : BaseUUIThreadingExtension<IntersectionMarkingTool> { }
    public class IntersectionMarkingToolLoadingExtension : BaseUUIToolLoadingExtension<IntersectionMarkingTool> { }

}
