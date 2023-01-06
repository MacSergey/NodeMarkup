using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using ICities;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using NodeMarkup.UI;
using NodeMarkup.UI.Panel;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using static ModsCommon.SettingsHelper;

namespace NodeMarkup
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
        public static SavedBool CollapseOptions { get; } = new SavedBool(nameof(CollapseOptions), SettingsFile, true, true);
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
        public static SavedBool CtrlToMovePoint { get; } = new SavedBool(nameof(CtrlToMovePoint), SettingsFile, false, true);

        protected UIAdvancedHelper ShortcutsTab => GetTab(nameof(ShortcutsTab));
        protected UIAdvancedHelper BackupTab => GetTab(nameof(BackupTab));

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

            AddKeyMapping(ShortcutsTab, undergroundOptions);

            AddBackupMarking(BackupTab);
            AddBackupStyleTemplates(BackupTab);
            AddBackupIntersectionTemplates(BackupTab);
#if DEBUG
            AddDebug(DebugTab);
#endif
        }

        #endregion

        #region GENERAL

        #region DISPLAY&USAGE
        private void AddGeneral(UIAdvancedHelper helper, out OptionPanelWithLabelData undergroundOptions)
        {
            var renderGroup = helper.AddGroup(Localize.Settings_Render);

            AddFloatField(renderGroup, Localize.Settings_RenderDistance, RenderDistance, 700f, 0f);
            AddFloatField(renderGroup, Localize.Settings_LODDistanceMarking, LODDistance, 300f, 0f);
            AddFloatField(renderGroup, Localize.Settings_LODDistanceMesh, MeshLODDistance, 300f, 0f);
            AddFloatField(renderGroup, Localize.Settings_LODDistanceNetwork, NetworkLODDistance, 500f, 0f);
            AddFloatField(renderGroup, Localize.Settings_LODDistanceProp, PropLODDistance, 500f, 0f);
            AddFloatField(renderGroup, Localize.Settings_LODDistanceTree, TreeLODDistance, 500f, 0f);


            var displayAndUsageGroup = helper.AddGroup(Localize.Settings_DisplayAndUsage);

            AddCheckBox(displayAndUsageGroup, Localize.Settings_LoadMarkingAssets, LoadMarkingAssets);
            AddCheckBox(displayAndUsageGroup, Localize.Settings_ApplyMarkingsFromAssets, ApplyMarkingFromAssets);
            AddLabel(displayAndUsageGroup, Localize.Settings_ApplyAfterRestart, 0.8f, new Color32(255, 215, 81, 255), 25);
            AddCheckBox(displayAndUsageGroup, Localize.Settings_RailUnderMarking, RailUnderMarking);
            AddLabel(displayAndUsageGroup, Localize.Settings_RailUnderMarkingWarning, 0.8f, new Color32(255, 68, 68, 255), 25);
            AddLabel(displayAndUsageGroup, Localize.Settings_ApplyAfterRestart, 0.8f, new Color32(255, 215, 81, 255), 25);
            AddCheckBox(displayAndUsageGroup, Localize.Settings_LevelCrossingUnderMarking, LevelCrossingUnderMarking);
            AddLabel(displayAndUsageGroup, Localize.Settings_RailUnderMarkingWarning, 0.8f, new Color32(255, 68, 68, 255), 25);
            AddLabel(displayAndUsageGroup, Localize.Settings_ApplyAfterRestart, 0.8f, new Color32(255, 215, 81, 255), 25);
            AddToolButton<NodeMarkupTool, NodeMarkupButton>(displayAndUsageGroup);
            undergroundOptions = AddCheckboxPanel(displayAndUsageGroup, Localize.Settings_ToggleUnderground, ToggleUndergroundMode, new string[] { string.Format(Localize.Settings_ToggleUndergroundHold, UndergroundModifier), string.Format(Localize.Settings_ToggleUndergroundButtons, NodeMarkupTool.EnterUndergroundShortcut, NodeMarkupTool.ExitUndergroundShortcut) });
            AddCheckBox(displayAndUsageGroup, string.Format(Localize.Setting_HoldToMovePoint, LocalizeExtension.Ctrl), CtrlToMovePoint);
            AddCheckBox(displayAndUsageGroup, Localize.Settings_CollapseOptions, CollapseOptions);
            AddCheckBox(displayAndUsageGroup, CommonLocalize.Settings_ShowTooltips, ShowToolTip);
            AddCheckBox(displayAndUsageGroup, Localize.Settings_ShowPaneltips, ShowPanelTip);
            AddCheckBox(displayAndUsageGroup, Localize.Settings_HideStreetName, HideStreetName);

            UIPanel intensityField = null;
            AddCheckBox(displayAndUsageGroup, Localize.Settings_IlluminationAtNight, IlluminationAtNight, OnIlluminationChanged);
            intensityField = AddIntField(displayAndUsageGroup, Localize.Settings_IlluminationIntensity, IlluminationIntensity, 10, 1, 30, padding: 25);
            OnIlluminationChanged();


            var gameplayGroup = helper.AddGroup(Localize.Settings_Gameplay);

            AddCheckboxPanel(gameplayGroup, Localize.Settings_ShowDeleteWarnings, DeleteWarnings, DeleteWarningsType, new string[] { Localize.Settings_ShowDeleteWarningsAlways, Localize.Settings_ShowDeleteWarningsOnlyDependences });
            AddCheckBox(gameplayGroup, Localize.Settings_QuickRuleSetup, QuickRuleSetup);
            AddCheckBox(gameplayGroup, Localize.Settings_CreateLaneEdgeLines, CreateLaneEdgeLines);
            AddCheckBox(gameplayGroup, Localize.Settings_QuickBorderSetup, QuickBorderSetup);
            AddCheckBox(gameplayGroup, Localize.Settings_CutLineByCrosswalk, CutLineByCrosswalk);
            AddCheckBox(gameplayGroup, Localize.Settings_DontCutBorderByCrosswalk, NotCutBordersByCrosswalk);
            AddCheckboxPanel(gameplayGroup, Localize.Settings_AutoApplyPasting, AutoApplyPasting, AutoApplyPastingType, new string[] { Localize.Settings_AutoApplyPastingDirectOnly, Localize.Settings_AutoApplyPastingDirectAndInvert});

            void OnIlluminationChanged()
            {
                intensityField.isVisible = IlluminationAtNight;
            }
        }
        private void AddGrouping(UIAdvancedHelper helper)
        {
            var group = helper.AddGroup(Localize.Settings_Groupings);

            AddCheckBox(group, Localize.Settings_GroupPoints, GroupPoints, OnChanged);
            AddCheckBox(group, Localize.Settings_GroupLines, GroupLines, OnChanged);
            AddCheckboxPanel(group, Localize.Settings_GroupTemplates, GroupTemplates, GroupTemplatesType, new string[] { Localize.Settings_GroupTemplatesByType, Localize.Settings_GroupTemplatesByStyle }, OnChanged);
            AddCheckBox(group, Localize.Settings_GroupPresets, GroupPresets, OnChanged);
            AddCheckboxPanel(group, Localize.Settings_GroupPointsOverlay, GroupPointsOverlay, GroupPointsOverlayType, new string[] { Localize.Settings_GroupPointsArrangeCircle, Localize.Settings_GroupPointsArrangeLine });

            static void OnChanged() => SingletonItem<NodeMarkupPanel>.Instance?.UpdatePanel();
        }
        private void AddSorting(UIAdvancedHelper helper)
        {
            var group = helper.AddGroup(Localize.Settings_Sortings);

            AddCheckboxPanel(group, Localize.Settings_SortPresetType, SortPresetsType, new string[] { Localize.Settings_SortPresetByRoadCount, Localize.Settings_SortPresetByNames }, OnChanged);
            AddCheckboxPanel(group, Localize.Settings_SortTemplateType, SortTemplatesType, new string[] { Localize.Settings_SortTemplateByAuthor, Localize.Settings_SortTemplateByType, Localize.Settings_SortTemplateByNames }, OnChanged);
            AddCheckboxPanel(group, Localize.Settings_SortApplyType, SortApplyType, new string[] { Localize.Settings_SortApplyByAuthor, Localize.Settings_SortApplyByType, Localize.Settings_SortApplyByNames });
            AddCheckBox(group, Localize.Settings_SortApplyDefaultFirst, DefaultTemlatesFirst);


            static void OnChanged() => SingletonItem<NodeMarkupPanel>.Instance?.UpdatePanel();
        }

        #endregion

        #endregion

        #region KEYMAPPING
        private void AddKeyMapping(UIAdvancedHelper helper, OptionPanelWithLabelData undergroundOptions)
        {
            var group = helper.AddGroup(CommonLocalize.Settings_Shortcuts);
            var keymappings = AddKeyMappingPanel(group);
            keymappings.AddKeymapping(NodeMarkupTool.ActivationShortcut);
            foreach (var shortcut in NodeMarkupTool.ToolShortcuts)
                keymappings.AddKeymapping(shortcut);

            keymappings.BindingChanged += OnBindingChanged;
            void OnBindingChanged(Shortcut shortcut)
            {
                if (shortcut == NodeMarkupTool.EnterUndergroundShortcut || shortcut == NodeMarkupTool.ExitUndergroundShortcut)
                    undergroundOptions.checkBoxes[1].label.text = string.Format(Localize.Settings_ToggleUndergroundButtons, NodeMarkupTool.EnterUndergroundShortcut, NodeMarkupTool.ExitUndergroundShortcut);
            }

            AddModifier<RegularLineModifierPanel>(helper, Localize.Settings_RegularLinesModifier);
            AddModifier<StopLineModifierPanel>(helper, Localize.Settings_StopLinesModifier);
            AddModifier<CrosswalkModifierPanel>(helper, Localize.Settings_CrosswalksModifier);
            AddModifier<FillerModifierPanel>(helper, Localize.Settings_FillersModifier);
        }
        private void AddModifier<PanelType>(UIAdvancedHelper helper, string title)
            where PanelType : StyleModifierPanel
        {
            var panel = helper.AddGroup(title).self as UIPanel;
            var modifier = panel.gameObject.AddComponent<PanelType>();
            modifier.OnModifierChanged += ModifierChanged;

            static void ModifierChanged(Style.StyleType style, StyleModifier value) => NodeMarkupTool.StylesModifier[style].value = (int)value;
        }

        #endregion

        #region BACKUP
        private void AddBackupMarking(UIAdvancedHelper helper)
        {
            if (!Utility.InGame)
                return;

            var group = helper.AddGroup(Localize.Settings_BackupMarking);

            AddDeleteAll(group, Localize.Settings_DeleteMarkingButton, Localize.Settings_DeleteMarkingCaption, $"{Localize.Settings_DeleteMarkingMessage}\n{NodeMarkupMessageBox.CantUndone}", () => MarkupManager.Clear());
            AddDump(group, Localize.Settings_DumpMarkingButton, Localize.Settings_DumpMarkingCaption, Loader.DumpMarkingData);
            AddRestore<ImportMarkingMessageBox>(group, Localize.Settings_RestoreMarkingButton, Localize.Settings_RestoreMarkingCaption, $"{Localize.Settings_RestoreMarkingMessage}\n{NodeMarkupMessageBox.CantUndone}");
        }
        private void AddBackupStyleTemplates(UIAdvancedHelper helper)
        {
            var group = helper.AddGroup(Localize.Settings_BackupTemplates);

            AddDeleteAll(group, Localize.Settings_DeleteTemplatesButton, Localize.Settings_DeleteTemplatesCaption, $"{Localize.Settings_DeleteTemplatesMessage}\n{NodeMarkupMessageBox.CantUndone}", () => SingletonManager<StyleTemplateManager>.Instance.DeleteAll());
            AddDump(group, Localize.Settings_DumpTemplatesButton, Localize.Settings_DumpTemplatesCaption, Loader.DumpStyleTemplatesData);
            AddRestore<ImportStyleTemplatesMessageBox>(group, Localize.Settings_RestoreTemplatesButton, Localize.Settings_RestoreTemplatesCaption, $"{Localize.Settings_RestoreTemplatesMessage}\n{NodeMarkupMessageBox.CantUndone}");
        }

        private void AddBackupIntersectionTemplates(UIAdvancedHelper helper)
        {
            var group = helper.AddGroup(Localize.Settings_BackupPresets);

            AddDeleteAll(group, Localize.Settings_DeletePresetsButton, Localize.Settings_DeletePresetsCaption, $"{Localize.Settings_DeletePresetsMessage}\n{NodeMarkupMessageBox.CantUndone}", () => SingletonManager<IntersectionTemplateManager>.Instance.DeleteAll());
            AddDump(group, Localize.Settings_DumpPresetsButton, Localize.Settings_DumpPresetsCaption, Loader.DumpIntersectionTemplatesData);
            AddRestore<ImportIntersectionTemplatesMessageBox>(group, Localize.Settings_RestorePresetsButton, Localize.Settings_RestorePresetsCaption, $"{Localize.Settings_RestorePresetsMessage}\n{NodeMarkupMessageBox.CantUndone}");
        }

        private void AddDeleteAll(UIHelper group, string buttonText, string caption, string message, Action process)
        {
            var button = AddButton(group, buttonText, Click, 600);
            button.color = new Color32(255, 40, 40, 255);
            button.hoveredColor = new Color32(224, 40, 40, 255);
            button.pressedColor = new Color32(192, 40, 40, 255);
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
        private void AddDump(UIHelper group, string buttonText, string caption, Dump dump)
        {
            AddButton(group, buttonText, Click, 600);

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
        private void AddRestore<Modal>(UIHelper group, string buttonText, string caption, string message)
            where Modal : ImportMessageBox
        {
            AddButton(group, buttonText, Click, 600);

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
        public static SavedBool ShowNodeContour { get; } = new SavedBool(nameof(ShowNodeContour), string.Empty, false);
        public static SavedFloat IlluminationDelta { get; } = new SavedFloat(nameof(IlluminationDelta), SettingsFile, 1f, true);
        public static SavedInt ShowFillerTriangulation { get; } = new SavedInt(nameof(ShowFillerTriangulation), SettingsFile, 0, true);

        private void AddDebug(UIAdvancedHelper helper)
        {
            var overlayGroup = helper.AddGroup("Selection overlay");

            Selection.AddAlphaBlendOverlay(overlayGroup);
            Selection.AddRenderOverlayCentre(overlayGroup);
            Selection.AddRenderOverlayBorders(overlayGroup);
            Selection.AddBorderOverlayWidth(overlayGroup);

            var groupOther = helper.AddGroup("Nodes");
            AddCheckBox(groupOther, "Show node contour", ShowNodeContour);
            AddFloatField(groupOther, "Delta", IlluminationDelta, 1f);

            AddCheckboxPanel(groupOther, "Show filler triangulation", ShowFillerTriangulation, new string[] { "Dont show", "Original", "Splitted", "Both" });
        }
#endif
        #endregion
    }
}
