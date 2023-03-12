using ColossalFramework;
using ColossalFramework.UI;
using IMT.API;
using IMT.Manager;
using IMT.Tools;
using IMT.UI;
using IMT.UI.Panel;
using IMT.Utilities;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using ModsCommon.Settings;
using static ModsCommon.Settings.Helper;

namespace IMT
{
    public class Settings : BaseSettings<Mod>
    {
        #region PROPERTIES

        public static SavedFloat RenderDistance { get; } = new SavedFloat(nameof(RenderDistance), SettingsFile, 700f, true);
        public static SavedFloat LODDistance { get; } = new SavedFloat(nameof(LODDistance), SettingsFile, 300f, true);
        public static SavedFloat MeshLODDistance { get; } = new SavedFloat(nameof(MeshLODDistance), SettingsFile, 300f, true);
        public static SavedFloat PropLODDistance { get; } = new SavedFloat(nameof(PropLODDistance), SettingsFile, 500f, true);
        public static SavedFloat TreeLODDistance { get; } = new SavedFloat(nameof(TreeLODDistance), SettingsFile, 500f, true);
        public static SavedFloat NetworkLODDistance { get; } = new SavedFloat(nameof(NetworkLODDistance), SettingsFile, 500f, true);

        public static SavedBool LoadMarkingAssets { get; } = new SavedBool(nameof(LoadMarkingAssets), SettingsFile, true, true);
        public static SavedBool ApplyMarkingFromAssets { get; } = new SavedBool(nameof(ApplyMarkingFromAssets), SettingsFile, true, true);
        public static SavedBool RailUnderMarking { get; } = new SavedBool(nameof(RailUnderMarking), SettingsFile, true, true);
        public static SavedBool LevelCrossingUnderMarking { get; } = new SavedBool(nameof(LevelCrossingUnderMarking), SettingsFile, true, true);
        public static SavedBool AutoCollapseItemsPanel { get; } = new SavedBool(nameof(AutoCollapseItemsPanel), SettingsFile, true, true);
        public static SavedBool CollapseRules { get; } = new SavedBool(nameof(CollapseRules), SettingsFile, true, true);
        public static SavedBool ShowToolTip { get; } = new SavedBool(nameof(ShowToolTip), SettingsFile, true, true);
        public static SavedBool ShowPanelTip { get; } = new SavedBool(nameof(ShowPanelTip), SettingsFile, true, true);
        public static SavedBool DeleteWarnings { get; } = new SavedBool(nameof(DeleteWarnings), SettingsFile, true, true);
        public static SavedInt DeleteWarningsType { get; } = new SavedInt(nameof(DeleteWarningsType), SettingsFile, 0, true);
        public static SavedBool QuickRuleSetup { get; } = new SavedBool(nameof(QuickRuleSetup), SettingsFile, true, true);
        public static SavedBool QuickBorderSetup { get; } = new SavedBool(nameof(QuickBorderSetup), SettingsFile, true, true);
        public static SavedBool CutLineByCrosswalk { get; } = new SavedBool(nameof(CutLineByCrosswalk), SettingsFile, true, true);
        public static SavedBool CreateLaneEdgeLines { get; } = new SavedBool(nameof(CreateLaneEdgeLines), SettingsFile, false, true);
        public static SavedBool NotCutBordersByCrosswalk { get; } = new SavedBool(nameof(NotCutBordersByCrosswalk), SettingsFile, true, true);
        public static SavedBool HideStreetName { get; } = new SavedBool(nameof(HideStreetName), SettingsFile, true, true);
        public static SavedBool AutoApplyPasting { get; } = new SavedBool(nameof(AutoApplyPasting) + "V2", SettingsFile, false, true);
        public static SavedInt AutoApplyPastingType { get; } = new SavedInt(nameof(AutoApplyPastingType), SettingsFile, 1, true);
        public static SavedString Templates { get; } = new SavedString(nameof(Templates), SettingsFile, string.Empty, true);
        public static SavedString Intersections { get; } = new SavedString(nameof(Intersections), SettingsFile, string.Empty, true);
        public static SavedString Roads { get; } = new SavedString(nameof(Roads), SettingsFile, string.Empty, true);
        public static SavedString FavoritePrefabs { get; } = new SavedString(nameof(FavoritePrefabs), SettingsFile, string.Empty, true);

        public static SavedBool GroupPoints { get; } = new SavedBool(nameof(GroupPoints), SettingsFile, true, true);
        public static SavedBool GroupLines { get; } = new SavedBool(nameof(GroupLines), SettingsFile, false, true);
        public static SavedBool GroupTemplates { get; } = new SavedBool(nameof(GroupTemplates), SettingsFile, true, true);
        public static SavedBool GroupPresets { get; } = new SavedBool(nameof(GroupPresets), SettingsFile, true, true);
        public static SavedInt GroupTemplatesType { get; } = new SavedInt(nameof(GroupTemplatesType), SettingsFile, 0, true);
        public static SavedBool GroupPointsOverlay { get; } = new SavedBool(nameof(GroupPointsOverlay), SettingsFile, true, true);
        public static SavedInt GroupPointsOverlayType { get; } = new SavedInt(nameof(GroupPointsOverlayType), SettingsFile, 0, true);
        public static SavedInt SortTemplatesType { get; } = new SavedInt(nameof(SortTemplatesType), SettingsFile, 0, true);
        public static SavedInt SortPresetsType { get; } = new SavedInt(nameof(SortPresetsType), SettingsFile, 0, true);
        public static SavedInt SortApplyType { get; } = new SavedInt(nameof(SortApplyType), SettingsFile, 0, true);
        public static SavedBool DefaultTemlatesFirst { get; } = new SavedBool(nameof(DefaultTemlatesFirst), SettingsFile, true, true);
        public static SavedBool IlluminationAtNight { get; } = new SavedBool(nameof(IlluminationAtNight), SettingsFile, true, true);
        public static SavedInt IlluminationIntensity { get; } = new SavedInt(nameof(IlluminationIntensity), SettingsFile, 10, true);

        public static SavedInt ToggleUndergroundMode { get; } = new SavedInt(nameof(ToggleUndergroundMode), SettingsFile, 0, true);
        public static SavedBool HoldCtrlToMovePoint { get; } = new SavedBool(nameof(HoldCtrlToMovePoint), SettingsFile, true, true);

        protected UIComponent ShortcutsTab => GetTabContent(nameof(ShortcutsTab));
        protected UIComponent BackupTab => GetTabContent(nameof(BackupTab));
#if DEBUG
        protected UIComponent APITab => GetTabContent(nameof(APITab));
#endif

        public static bool IsUndergroundWithModifier => ToggleUndergroundMode == 0;
        public static string UndergroundModifier => LocalizeExtension.Shift;

        #endregion

        #region BASIC

        protected override IEnumerable<KeyValuePair<string, string>> AdditionalTabs
        {
            get
            {
                yield return new KeyValuePair<string, string>(nameof(ShortcutsTab), Localize.Settings_ShortcutsAndModifiersTab);
                yield return new KeyValuePair<string, string>(nameof(BackupTab), Localize.Settings_BackupTab);
#if DEBUG
                yield return new KeyValuePair<string, string>(nameof(APITab), "API");
#endif
            }
        }
        protected override void FillSettings()
        {
            base.FillSettings();

            AddLanguage(GeneralTab);
            AddGeneral(GeneralTab, out var undergroundOptions);
            AddGrouping(GeneralTab);
            AddSorting(GeneralTab);
            AddNotifications(GeneralTab);
            AddOther(GeneralTab);

            AddKeyMapping(ShortcutsTab, undergroundOptions);

            AddBackupMarking(BackupTab);
            AddBackupStyleTemplates(BackupTab);
            AddBackupIntersectionTemplates(BackupTab);
#if DEBUG
            AddDebug(DebugTab);
            AddAPI(APITab);
#endif
        }

        #endregion

        #region GENERAL

        #region DISPLAY&USAGE
        private void AddGeneral(UIComponent helper, out OptionPanelWithLabelData undergroundOptions)
        {
            var renderGroup = helper.AddOptionsGroup(Localize.Settings_Render);

            var renderDistance = renderGroup.AddFloatField(Localize.Settings_RenderDistance, RenderDistance, 0f);
            renderDistance.Control.Format = Localize.NumberFormat_Meter;
            var markingLOD = renderGroup.AddFloatField(Localize.Settings_LODDistanceMarking, LODDistance, 0f);
            markingLOD.Control.Format = Localize.NumberFormat_Meter;
            var meshLOD = renderGroup.AddFloatField(Localize.Settings_LODDistanceMesh, MeshLODDistance, 0f);
            meshLOD.Control.Format = Localize.NumberFormat_Meter;
            var networkLOD = renderGroup.AddFloatField(Localize.Settings_LODDistanceNetwork, NetworkLODDistance, 0f);
            networkLOD.Control.Format = Localize.NumberFormat_Meter;
            var propLOD = renderGroup.AddFloatField(Localize.Settings_LODDistanceProp, PropLODDistance, 0f);
            propLOD.Control.Format = Localize.NumberFormat_Meter;
            var treeLOD = renderGroup.AddFloatField(Localize.Settings_LODDistanceTree, TreeLODDistance, 0f);
            treeLOD.Control.Format = Localize.NumberFormat_Meter;


            var displayAndUsageGroup = helper.AddOptionsGroup(Localize.Settings_DisplayAndUsage);

            LabelSettingsItem label = null;
            displayAndUsageGroup.AddToggle(Localize.Settings_LoadMarkingAssets, LoadMarkingAssets);
            label = displayAndUsageGroup.AddLabel(Localize.Settings_ApplyAfterRestart.AddColor(new Color32(255, 215, 81, 255)), 0.8f);
            label.LabelItem.processMarkup = true;
            label.Borders = SettingsContentItem.Border.None;
            label.paddingTop = 0;

            var warning = $"{Localize.Settings_RailUnderMarkingWarning.AddColor(new Color32(255, 68, 68, 255))}\n{Localize.Settings_ApplyAfterRestart.AddColor(new Color32(255, 215, 81, 255))}";

            displayAndUsageGroup.AddToggle(Localize.Settings_ApplyMarkingsFromAssets, ApplyMarkingFromAssets);
            displayAndUsageGroup.AddToggle(Localize.Settings_RailUnderMarking, RailUnderMarking);
            label = displayAndUsageGroup.AddLabel(warning, 0.8f);
            label.LabelItem.processMarkup = true;
            label.Borders = SettingsContentItem.Border.None;
            label.paddingTop = 0;

            displayAndUsageGroup.AddToggle(Localize.Settings_LevelCrossingUnderMarking, LevelCrossingUnderMarking);
            label = displayAndUsageGroup.AddLabel(warning, 0.8f);
            label.LabelItem.processMarkup = true;
            label.Borders = SettingsContentItem.Border.None;
            label.paddingTop = 0;

            AddToolButton<IntersectionMarkingTool, NodeMarkingButton>(displayAndUsageGroup);

            undergroundOptions = displayAndUsageGroup.AddTogglePanel(Localize.Settings_ToggleUnderground, ToggleUndergroundMode, new string[] { string.Format(Localize.Settings_ToggleUndergroundHold, UndergroundModifier), string.Format(Localize.Settings_ToggleUndergroundButtons, IntersectionMarkingTool.EnterUndergroundShortcut, IntersectionMarkingTool.ExitUndergroundShortcut) });
            displayAndUsageGroup.AddToggle(string.Format(Localize.Setting_HoldToMovePoint, LocalizeExtension.Ctrl), HoldCtrlToMovePoint);
            var autoCollapse = displayAndUsageGroup.AddToggle(Localize.Settings_AutoCollapseItemsPanel, AutoCollapseItemsPanel);
            autoCollapse.Control.OnStateChanged += OnChanged;
            displayAndUsageGroup.AddToggle(Localize.Settings_CollapseRules, CollapseRules);
            displayAndUsageGroup.AddToggle(CommonLocalize.Settings_ShowTooltips, ShowToolTip);
            displayAndUsageGroup.AddToggle(Localize.Settings_ShowPaneltips, ShowPanelTip);
            displayAndUsageGroup.AddToggle(Localize.Settings_HideStreetName, HideStreetName);

            IntSettingsItem intensityField = null;
            var illuminationToggle = displayAndUsageGroup.AddToggle(Localize.Settings_IlluminationAtNight, IlluminationAtNight);
            illuminationToggle.Control.OnStateChanged += OnIlluminationChanged;
            intensityField = displayAndUsageGroup.AddIntField(Localize.Settings_IlluminationIntensity, IlluminationIntensity, 1, 30);
            OnIlluminationChanged(IlluminationAtNight);


            var gameplayGroup = helper.AddOptionsGroup(Localize.Settings_Gameplay);

            gameplayGroup.AddTogglePanel(Localize.Settings_ShowDeleteWarnings, DeleteWarnings, DeleteWarningsType, new string[] { Localize.Settings_ShowDeleteWarningsAlways, Localize.Settings_ShowDeleteWarningsOnlyDependences });
            gameplayGroup.AddToggle(Localize.Settings_QuickRuleSetup, QuickRuleSetup);
            gameplayGroup.AddToggle(Localize.Settings_CreateLaneEdgeLines, CreateLaneEdgeLines);
            gameplayGroup.AddToggle(Localize.Settings_QuickBorderSetup, QuickBorderSetup);
            gameplayGroup.AddToggle(Localize.Settings_CutLineByCrosswalk, CutLineByCrosswalk);
            gameplayGroup.AddToggle(Localize.Settings_DontCutBorderByCrosswalk, NotCutBordersByCrosswalk);
            gameplayGroup.AddTogglePanel(Localize.Settings_AutoApplyPasting, AutoApplyPasting, AutoApplyPastingType, new string[] { Localize.Settings_AutoApplyPastingDirectOnly, Localize.Settings_AutoApplyPastingDirectAndInvert });

            void OnIlluminationChanged(bool value) => intensityField.isVisible = value;
            static void OnChanged(bool value) => SingletonItem<IntersectionMarkingToolPanel>.Instance?.UpdatePanel();
        }
        private void AddGrouping(UIComponent helper)
        {
            var group = helper.AddOptionsGroup(Localize.Settings_Groupings);

            var groupPointToggle = group.AddToggle(Localize.Settings_GroupPoints, GroupPoints);
            groupPointToggle.Control.OnStateChanged += OnChanged;

            var groupLineToggle = group.AddToggle(Localize.Settings_GroupLines, GroupLines);
            groupLineToggle.Control.OnStateChanged += OnChanged;

            group.AddTogglePanel(Localize.Settings_GroupTemplates, GroupTemplates, GroupTemplatesType, new string[] { Localize.Settings_GroupTemplatesByType, Localize.Settings_GroupTemplatesByStyle }, () => OnChanged(false));

            var groupPresetToggle = group.AddToggle(Localize.Settings_GroupPresets, GroupPresets);
            groupPresetToggle.Control.OnStateChanged += OnChanged;

            group.AddTogglePanel(Localize.Settings_GroupPointsOverlay, GroupPointsOverlay, GroupPointsOverlayType, new string[] { Localize.Settings_GroupPointsArrangeCircle, Localize.Settings_GroupPointsArrangeLine });

            static void OnChanged(bool value) => SingletonItem<IntersectionMarkingToolPanel>.Instance?.UpdatePanel();
        }
        private void AddSorting(UIComponent helper)
        {
            var group = helper.AddOptionsGroup(Localize.Settings_Sortings);

            group.AddTogglePanel(Localize.Settings_SortPresetType, SortPresetsType, new string[] { Localize.Settings_SortPresetByRoadCount, Localize.Settings_SortPresetByNames }, OnChanged);
            group.AddTogglePanel(Localize.Settings_SortTemplateType, SortTemplatesType, new string[] { Localize.Settings_SortTemplateByAuthor, Localize.Settings_SortTemplateByType, Localize.Settings_SortTemplateByNames }, OnChanged);
            group.AddTogglePanel(Localize.Settings_SortApplyType, SortApplyType, new string[] { Localize.Settings_SortApplyByAuthor, Localize.Settings_SortApplyByType, Localize.Settings_SortApplyByNames });
            group.AddToggle(Localize.Settings_SortApplyDefaultFirst, DefaultTemlatesFirst);


            static void OnChanged() => SingletonItem<IntersectionMarkingToolPanel>.Instance?.UpdatePanel();
        }
        private void AddOther(UIComponent helper)
        {
            var group = helper.AddOptionsGroup(Localize.Setting_Others);
            var button = group.AddButtonPanel().AddButton(Localize.Settings_InvertChevrons, InvertChevrons, 400);

            static void InvertChevrons()
            {
                for (int i = 0; i < NetManager.MAX_NODE_COUNT; i += 1)
                {
                    if (SingletonManager<NodeMarkingManager>.Instance.TryGetMarking((ushort)i, out var marking))
                    {
                        foreach (var filler in marking.Fillers)
                        {
                            if (filler.Style.Value is ChevronFillerStyle chevron)
                                chevron.Invert.Value = !chevron.Invert;
                        }
                    }
                }

                for (int i = 0; i < NetManager.MAX_SEGMENT_COUNT; i += 1)
                {
                    if (SingletonManager<SegmentMarkingManager>.Instance.TryGetMarking((ushort)i, out var marking))
                    {
                        foreach (var filler in marking.Fillers)
                        {
                            if (filler.Style.Value is ChevronFillerStyle chevron)
                                chevron.Invert.Value = !chevron.Invert;
                        }
                    }
                }
            }
        }

        #endregion

        #endregion

        #region KEYMAPPING
        private void AddKeyMapping(UIComponent helper, OptionPanelWithLabelData undergroundOptions)
        {
            var group = helper.AddOptionsGroup(CommonLocalize.Settings_Shortcuts);

            group.AddKeyMappingButton(IntersectionMarkingTool.ActivationShortcut);
            foreach (var shortcut in IntersectionMarkingTool.ToolShortcuts)
            {
                var shortcutItem = group.AddKeyMappingButton(shortcut);
                shortcutItem.BindingChanged += OnBindingChanged;
            }

            void OnBindingChanged(Shortcut shortcut)
            {
                if (shortcut == IntersectionMarkingTool.EnterUndergroundShortcut || shortcut == IntersectionMarkingTool.ExitUndergroundShortcut)
                    undergroundOptions.checkBoxes[1].label.text = string.Format(Localize.Settings_ToggleUndergroundButtons, IntersectionMarkingTool.EnterUndergroundShortcut, IntersectionMarkingTool.ExitUndergroundShortcut);
            }

            AddModifier<RegularLineStyle.RegularLineType>(helper, Localize.Settings_RegularLinesModifier);
            AddModifier<StopLineStyle.StopLineType>(helper, Localize.Settings_StopLinesModifier);
            AddModifier<BaseCrosswalkStyle.CrosswalkType>(helper, Localize.Settings_CrosswalksModifier);
            AddModifier<BaseFillerStyle.FillerType>(helper, Localize.Settings_FillersModifier);
        }
        private void AddModifier<StyleType>(UIComponent helper, string title)
            where StyleType : Enum
        {
            var group = helper.AddOptionsGroup(title);

            var items = new Dictionary<Style.StyleType, StyleModifierSettingsItem>();
            foreach (var styleRaw in EnumExtension.GetEnumValues<StyleType>(v => true))
            {
                var style = styleRaw.ToEnum<Style.StyleType, StyleType>();
                var item = group.AddUIComponent<StyleModifierSettingsItem>();
                item.Label = style.Description();
                item.Style = style;
                item.OnModifierChanged += ModifierChanged;

                items[style] = item;
            }

            void ModifierChanged(Style.StyleType style, StyleModifier value)
            {
                if (value != StyleModifier.NotSet)
                {
                    foreach (var pair in items)
                    {
                        if (pair.Key != style && pair.Value.Value == value)
                            pair.Value.Value = StyleModifier.NotSet;
                    }
                }
            }
        }

        #endregion

        #region BACKUP
        private void AddBackupMarking(UIComponent helper)
        {
            if (!Utility.InGame)
                return;

            var group = helper.AddOptionsGroup(Localize.Settings_BackupMarking);

            AddDeleteAll(group, Localize.Settings_DeleteMarkingButton, Localize.Settings_DeleteMarkingCaption, $"{Localize.Settings_DeleteMarkingMessage}\n{IntersectionMarkingToolMessageBox.CantUndone}", () => MarkingManager.Clear());
            AddDump(group, Localize.Settings_DumpMarkingButton, Localize.Settings_DumpMarkingCaption, Loader.DumpMarkingData);
            AddRestore<ImportMarkingMessageBox>(group, Localize.Settings_RestoreMarkingButton, Localize.Settings_RestoreMarkingCaption, $"{Localize.Settings_RestoreMarkingMessage}\n{IntersectionMarkingToolMessageBox.CantUndone}");
        }
        private void AddBackupStyleTemplates(UIComponent helper)
        {
            var group = helper.AddOptionsGroup(Localize.Settings_BackupTemplates);

            AddDeleteAll(group, Localize.Settings_DeleteTemplatesButton, Localize.Settings_DeleteTemplatesCaption, $"{Localize.Settings_DeleteTemplatesMessage}\n{IntersectionMarkingToolMessageBox.CantUndone}", () => SingletonManager<StyleTemplateManager>.Instance.DeleteAll());
            AddDump(group, Localize.Settings_DumpTemplatesButton, Localize.Settings_DumpTemplatesCaption, Loader.DumpStyleTemplatesData);
            AddRestore<ImportStyleTemplatesMessageBox>(group, Localize.Settings_RestoreTemplatesButton, Localize.Settings_RestoreTemplatesCaption, $"{Localize.Settings_RestoreTemplatesMessage}\n{IntersectionMarkingToolMessageBox.CantUndone}");
        }

        private void AddBackupIntersectionTemplates(UIComponent helper)
        {
            var group = helper.AddOptionsGroup(Localize.Settings_BackupPresets);

            AddDeleteAll(group, Localize.Settings_DeletePresetsButton, Localize.Settings_DeletePresetsCaption, $"{Localize.Settings_DeletePresetsMessage}\n{IntersectionMarkingToolMessageBox.CantUndone}", () => SingletonManager<IntersectionTemplateManager>.Instance.DeleteAll());
            AddDump(group, Localize.Settings_DumpPresetsButton, Localize.Settings_DumpPresetsCaption, Loader.DumpIntersectionTemplatesData);
            AddRestore<ImportIntersectionTemplatesMessageBox>(group, Localize.Settings_RestorePresetsButton, Localize.Settings_RestorePresetsCaption, $"{Localize.Settings_RestorePresetsMessage}\n{IntersectionMarkingToolMessageBox.CantUndone}");
        }

        private void AddDeleteAll(UIComponent group, string buttonText, string caption, string message, Action process)
        {
            var button = group.AddButtonPanel().AddButton(buttonText, Click, 600);
            button.color = new Color32(179, 45, 45, 255);
            button.hoveredColor = new Color32(153, 38, 38, 255);
            button.pressedColor = new Color32(128, 32, 32, 255);
            button.focusedColor = button.color;

            void Click()
            {
                var messageBox = MessageBox.Show<YesNoMessageBox>();
                messageBox.CaptionText = caption;
                messageBox.MessageText = message;
                messageBox.OnButton1Click = Сonfirmed;
            }
            bool Сonfirmed()
            {
                process();
                return true;
            }
        }
        private delegate bool Dump(out string path);
        private void AddDump(UIComponent group, string buttonText, string caption, Dump dump)
        {
            group.AddButtonPanel().AddButton(buttonText, Click, 600);

            void Click()
            {
                var result = dump(out string path);

                if (result)
                {
                    var messageBox = MessageBox.Show<TwoButtonMessageBox>();
                    messageBox.CaptionText = caption;
                    messageBox.MessageText = Localize.Settings_DumpMessageSuccess;
                    messageBox.Button1Text = Localize.Settings_CopyPathToClipboard;
                    messageBox.Button2Text = CommonLocalize.MessageBox_OK;
                    messageBox.OnButton1Click = CopyToClipboard;
                    messageBox.SetButtonsRatio(2, 1);

                    bool CopyToClipboard()
                    {
                        Clipboard.text = path;
                        return false;
                    }
                }
                else
                {
                    var messageBox = MessageBox.Show<OkMessageBox>();
                    messageBox.CaptionText = caption;
                    messageBox.MessageText = Localize.Settings_DumpMessageFailed;
                }
            }
        }
        private void AddRestore<Modal>(UIComponent group, string buttonText, string caption, string message)
            where Modal : ImportMessageBox
        {
            group.AddButtonPanel().AddButton(buttonText, Click, 600);

            void Click()
            {
                var messageBox = MessageBox.Show<Modal>();
                messageBox.CaptionText = caption;
                messageBox.MessageText = message;

            }
        }

        #endregion

        #region DEBUG
#if DEBUG
        public static SavedBool ShowDebugProperties { get; } = new SavedBool(nameof(ShowDebugProperties), SettingsFile, false);
        public static SavedBool ShowNodeContour { get; } = new SavedBool(nameof(ShowNodeContour), string.Empty, false);
        public static SavedFloat IlluminationDelta { get; } = new SavedFloat(nameof(IlluminationDelta), SettingsFile, 1f, true);
        public static SavedInt ShowFillerTriangulation { get; } = new SavedInt(nameof(ShowFillerTriangulation), SettingsFile, 0, true);

        private void AddDebug(UIComponent helper)
        {
            var overlayGroup = helper.AddOptionsGroup("Selection overlay");

            Selection.AddAlphaBlendOverlay(overlayGroup);
            Selection.AddRenderOverlayCentre(overlayGroup);
            Selection.AddRenderOverlayBorders(overlayGroup);
            Selection.AddBorderOverlayWidth(overlayGroup);

            var groupOther = helper.AddOptionsGroup("Nodes");
            groupOther.AddToggle("Show debug properties", ShowDebugProperties);
            groupOther.AddToggle("Show node contour", ShowNodeContour);
            groupOther.AddFloatField("Delta", IlluminationDelta, 0f, 10f);

            groupOther.AddTogglePanel("Show filler triangulation", ShowFillerTriangulation, new string[] { "Dont show", "Original", "Splitted", "Both" });
        }

        private static IDataProviderV1 DataProvider { get; } = API.Helper.GetProvider("Test");
        public static SavedInt NodeId { get; } = new SavedInt(nameof(NodeId), SettingsFile, 1, true);
        public static SavedInt StartSegmentEnterId { get; } = new SavedInt(nameof(StartSegmentEnterId), SettingsFile, 1, true);
        public static SavedInt EndSegmentEnterId { get; } = new SavedInt(nameof(EndSegmentEnterId), SettingsFile, 1, true);
        public static SavedInt StartPointIndex { get; } = new SavedInt(nameof(StartPointIndex), SettingsFile, 1, true);
        public static SavedInt EndPointIndex { get; } = new SavedInt(nameof(EndPointIndex), SettingsFile, 1, true);
        public static SavedInt LineType { get; } = new SavedInt(nameof(LineType), SettingsFile, 0, true);
        public static SavedString LineStyle { get; } = new SavedString(nameof(LineStyle), SettingsFile, string.Empty, true);

        public static SavedString FillerPoints { get; } = new SavedString(nameof(FillerPoints), SettingsFile, string.Empty, true);
        public static SavedString FillerStyle { get; } = new SavedString(nameof(FillerStyle), SettingsFile, string.Empty, true);

        private LabelSettingsItem AddingLineResult { get; set; }
        private LabelSettingsItem AddingFillerResult { get; set; }

        private void AddAPI(UIComponent helper)
        {
            var lineGroup = helper.AddOptionsGroup("Add line to node");

            lineGroup.AddIntField("Node id", NodeId, 1, NetManager.MAX_NODE_COUNT);
            lineGroup.AddIntField("Start segment id", StartSegmentEnterId, 1, NetManager.MAX_SEGMENT_COUNT);
            lineGroup.AddIntField("End segment id", EndSegmentEnterId, 1, NetManager.MAX_SEGMENT_COUNT);
            lineGroup.AddIntField("Start point index", StartPointIndex, 1, 255);
            lineGroup.AddIntField("End point index", EndPointIndex, 1, 255);
            lineGroup.AddTogglePanel("Lane type", LineType, new string[] { "Regular", "Stop", "Normal", "Lane", "Crosswalk" });
            lineGroup.AddStringField("Style", LineStyle);

            var lineButtonItem = lineGroup.AddButtonPanel();
            lineButtonItem.AddButton("Create line", CreateLine, 150f);
            lineButtonItem.AddButton("Remove line", RemoveLine, 150f);
            lineButtonItem.AddButton("Exist line", ExistLine, 150f);
            AddingLineResult = lineGroup.AddLabel(string.Empty);


            var fillerGroup = helper.AddOptionsGroup("Add filler to node");
            fillerGroup.AddIntField("Node id", NodeId, 1, NetManager.MAX_NODE_COUNT);
            fillerGroup.AddStringField("Points", FillerPoints);
            fillerGroup.AddStringField("Style", FillerStyle);

            var fillerButtonItem = fillerGroup.AddButtonPanel();
            fillerButtonItem.AddButton("Create filler", CreateFiller, 150f);
            AddingFillerResult = fillerGroup.AddLabel(string.Empty);
        }

        private void CreateLine()
        {
            try
            {
                var provider = DataProvider;
                var nodeMarking = provider.GetOrCreateNodeMarking((ushort)NodeId.value);
                nodeMarking.TryGetEntrance((ushort)StartSegmentEnterId.value, out var startEnter);
                nodeMarking.TryGetEntrance((ushort)EndSegmentEnterId.value, out var endEnter);

                switch (LineType.value)
                {
                    case 0:
                        {
                            startEnter.GetEntrancePoint((byte)StartPointIndex.value, out var startPoint);
                            endEnter.GetEntrancePoint((byte)EndPointIndex.value, out var endPoint);
                            var style = provider.SolidLineStyle;
                            var line = nodeMarking.AddRegularLine(startPoint, endPoint, style);
                            AddingLineResult.Label = $"Line {line} was added";
                        }
                        break;
                    case 1:
                        {
                            startEnter.GetEntrancePoint((byte)StartPointIndex.value, out var startPoint);
                            endEnter.GetEntrancePoint((byte)EndPointIndex.value, out var endPoint);
                            var style = provider.SolidStopLineStyle;
                            var line = nodeMarking.AddStopLine(startPoint, endPoint, style);
                            AddingLineResult.Label = $"Line {line} was added";
                        }
                        break;
                    case 2:
                        {
                            startEnter.GetEntrancePoint((byte)StartPointIndex.value, out var startPoint);
                            endEnter.GetNormalPoint((byte)StartPointIndex.value, out var endPoint);
                            var style = provider.SolidLineStyle;
                            var line = nodeMarking.AddNormalLine(startPoint, endPoint, style);
                            AddingLineResult.Label = $"Line {line} was added";
                        }
                        break;
                    case 3:
                        {
                            startEnter.GetLanePoint((byte)StartPointIndex.value, out var startPoint);
                            endEnter.GetLanePoint((byte)EndPointIndex.value, out var endPoint);
                            var style = provider.PropLineStyle;
                            style.Prefab = PrefabCollection<PropInfo>.FindLoaded("Flowerpot 04");
                            var line = nodeMarking.AddLaneLine(startPoint, endPoint, style);
                            AddingLineResult.Label = $"Line {line} was added";
                        }
                        break;
                    case 4:
                        {
                            startEnter.GetCrosswalkPoint((byte)StartPointIndex.value, out var startPoint);
                            endEnter.GetCrosswalkPoint((byte)EndPointIndex.value, out var endPoint);
                            var style = provider.ZebraCrosswalkStyle;
                            var crosswalk = nodeMarking.AddCrosswalk(startPoint, endPoint, style);
                            AddingLineResult.Label = $"Crosswalk {crosswalk} was added";
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                AddingLineResult.Label = ex.Message;
            }
        }
        private void RemoveLine()
        {
            try
            {
                var provider = DataProvider;
                var nodeMarking = provider.GetOrCreateNodeMarking((ushort)NodeId.value);
                nodeMarking.TryGetEntrance((ushort)StartSegmentEnterId.value, out var startEnter);
                nodeMarking.TryGetEntrance((ushort)EndSegmentEnterId.value, out var endEnter);

                switch (LineType.value)
                {
                    case 0:
                        {
                            startEnter.GetEntrancePoint((byte)StartPointIndex.value, out var startPoint);
                            endEnter.GetEntrancePoint((byte)EndPointIndex.value, out var endPoint);
                            var removed = nodeMarking.RemoveRegularLine(startPoint, endPoint);
                            AddingLineResult.Label = removed ? "Line was removed" : "Line does not exist";
                        }
                        break;
                    case 1:
                        {
                            startEnter.GetEntrancePoint((byte)StartPointIndex.value, out var startPoint);
                            endEnter.GetEntrancePoint((byte)EndPointIndex.value, out var endPoint);
                            var removed = nodeMarking.RemoveStopLine(startPoint, endPoint);
                            AddingLineResult.Label = removed ? "Line was removed" : "Line does not exist";
                        }
                        break;
                    case 2:
                        {
                            startEnter.GetEntrancePoint((byte)StartPointIndex.value, out var startPoint);
                            endEnter.GetNormalPoint((byte)StartPointIndex.value, out var endPoint);
                            var removed = nodeMarking.RemoveNormalLine(startPoint, endPoint);
                            AddingLineResult.Label = removed ? "Line was removed" : "Line does not exist";
                        }
                        break;
                    case 3:
                        {
                            startEnter.GetLanePoint((byte)StartPointIndex.value, out var startPoint);
                            endEnter.GetLanePoint((byte)EndPointIndex.value, out var endPoint);
                            var removed = nodeMarking.RemoveLaneLine(startPoint, endPoint);
                            AddingLineResult.Label = removed ? "Line was removed" : "Line does not exist";
                        }
                        break;
                    case 4:
                        {
                            startEnter.GetCrosswalkPoint((byte)StartPointIndex.value, out var startPoint);
                            endEnter.GetCrosswalkPoint((byte)EndPointIndex.value, out var endPoint);
                            var removed = nodeMarking.RemoveCrosswalk(startPoint, endPoint);
                            AddingLineResult.Label = removed ? "Crosswalk was removed" : "Crosswalk does not exist";
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                AddingLineResult.Label = ex.Message;
            }
        }
        private void ExistLine()
        {
            try
            {
                var provider = DataProvider;
                var nodeMarking = provider.GetOrCreateNodeMarking((ushort)NodeId.value);
                nodeMarking.TryGetEntrance((ushort)StartSegmentEnterId.value, out var startEnter);
                nodeMarking.TryGetEntrance((ushort)EndSegmentEnterId.value, out var endEnter);

                switch (LineType.value)
                {
                    case 0:
                        {
                            startEnter.GetEntrancePoint((byte)StartPointIndex.value, out var startPoint);
                            endEnter.GetEntrancePoint((byte)EndPointIndex.value, out var endPoint);
                            var exist = nodeMarking.RegularLineExist(startPoint, endPoint);
                            AddingLineResult.Label = exist ? "Line exist" : "Line does not exist";
                        }
                        break;
                    case 1:
                        {
                            startEnter.GetEntrancePoint((byte)StartPointIndex.value, out var startPoint);
                            endEnter.GetEntrancePoint((byte)EndPointIndex.value, out var endPoint);
                            var exist = nodeMarking.StopLineExist(startPoint, endPoint);
                            AddingLineResult.Label = exist ? "Line exist" : "Line does not exist";
                        }
                        break;
                    case 2:
                        {
                            startEnter.GetEntrancePoint((byte)StartPointIndex.value, out var startPoint);
                            endEnter.GetNormalPoint((byte)EndPointIndex.value, out var endPoint);
                            var exist = nodeMarking.NormalLineExist(startPoint, endPoint);
                            AddingLineResult.Label = exist ? "Line exist" : "Line does not exist";
                        }
                        break;
                    case 3:
                        {
                            startEnter.GetLanePoint((byte)StartPointIndex.value, out var startPoint);
                            endEnter.GetLanePoint((byte)EndPointIndex.value, out var endPoint);
                            var exist = nodeMarking.LaneLineExist(startPoint, endPoint);
                            AddingLineResult.Label = exist ? "Line exist" : "Line does not exist";
                        }
                        break;
                    case 4:
                        {
                            startEnter.GetCrosswalkPoint((byte)StartPointIndex.value, out var startPoint);
                            endEnter.GetCrosswalkPoint((byte)EndPointIndex.value, out var endPoint);
                            var exist = nodeMarking.CrosswalkExist(startPoint, endPoint);
                            AddingLineResult.Label = exist ? "Line exist" : "Line does not exist";
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                AddingLineResult.Label = ex.Message;
            }
        }
        private void CreateFiller()
        {
            try
            {
                var provider = DataProvider;
                var nodeMarking = provider.GetOrCreateNodeMarking((ushort)NodeId.value);

                var points = new List<IEntrancePointData>();
                var raw = FillerPoints.value.Split(';');
                foreach (var str in raw)
                {
                    var pd = str.Split(':');
                    if (pd.Length == 2)
                    {
                        var enterId = ushort.Parse(pd[0]);
                        var index = byte.Parse(pd[1]);

                        nodeMarking.TryGetEntrance(enterId, out var enter);
                        enter.GetEntrancePoint(index, out var point);
                        points.Add(point);
                    }
                }
                points.Add(points[0]);
                var style = provider.SolidFillerStyle;
                style.Color = new Color32(255, 0, 0, 255);
                var filler = nodeMarking.AddFiller(points, style);
                AddingFillerResult.Label = $"Filler {filler} was added";
            }
            catch (Exception ex)
            {
                AddingFillerResult.Label = ex.Message;
            }
        }
#endif
        #endregion
    }
}
