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
        public static SavedBool LoadMarkingAssets { get; } = new SavedBool(nameof(LoadMarkingAssets), SettingsFile, true, true);
        public static SavedBool RailUnderMarking { get; } = new SavedBool(nameof(RailUnderMarking), SettingsFile, true, true);
        public static SavedBool ShowToolTip { get; } = new SavedBool(nameof(ShowToolTip), SettingsFile, true, true);
        public static SavedBool ShowPanelTip { get; } = new SavedBool(nameof(ShowPanelTip), SettingsFile, true, true);
        public static SavedBool DeleteWarnings { get; } = new SavedBool(nameof(DeleteWarnings), SettingsFile, true, true);
        public static SavedInt DeleteWarningsType { get; } = new SavedInt(nameof(DeleteWarningsType), SettingsFile, 0, true);
        public static SavedBool QuickRuleSetup { get; } = new SavedBool(nameof(QuickRuleSetup), SettingsFile, true, true);
        public static SavedBool QuickBorderSetup { get; } = new SavedBool(nameof(QuickBorderSetup), SettingsFile, true, true);
        public static SavedBool CutLineByCrosswalk { get; } = new SavedBool(nameof(CutLineByCrosswalk), SettingsFile, true, true);
        public static SavedBool NotCutBordersByCrosswalk { get; } = new SavedBool(nameof(NotCutBordersByCrosswalk), SettingsFile, true, true);
        public static SavedBool HideStreetName { get; } = new SavedBool(nameof(HideStreetName), SettingsFile, true, true);
        public static SavedString Templates { get; } = new SavedString(nameof(Templates), SettingsFile, string.Empty, true);
        public static SavedString Intersections { get; } = new SavedString(nameof(Intersections), SettingsFile, string.Empty, true);

        public static SavedBool GroupPoints { get; } = new SavedBool(nameof(GroupPoints), SettingsFile, true, true);
        public static SavedBool GroupLines { get; } = new SavedBool(nameof(GroupLines), SettingsFile, false, true);
        public static SavedBool GroupTemplates { get; } = new SavedBool(nameof(GroupTemplates), SettingsFile, true, true);
        public static SavedInt GroupTemplatesType { get; } = new SavedInt(nameof(GroupTemplatesType), SettingsFile, 0, true);
        public static SavedBool GroupPointsOverlay { get; } = new SavedBool(nameof(GroupPointsOverlay), SettingsFile, true, true);
        public static SavedInt GroupPointsOverlayType { get; } = new SavedInt(nameof(GroupPointsOverlayType), SettingsFile, 0, true);

        protected UIAdvancedHelper ShortcutsTab => GetTab(nameof(ShortcutsTab));
        protected UIAdvancedHelper BackupTab => GetTab(nameof(BackupTab));

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
            AddGeneral(GeneralTab);
            AddGrouping(GeneralTab);
            AddNotifications(GeneralTab);

            AddKeyMapping(ShortcutsTab);

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
        private void AddGeneral(UIAdvancedHelper helper)
        {
            var group = helper.AddGroup(Localize.Settings_DisplayAndUsage);

            AddFloatField(group, Localize.Settings_RenderDistance, RenderDistance, 700f, 0f);
            AddFloatField(group, Localize.Settings_LODDistance, LODDistance, 300f, 0f);
            AddCheckBox(group, Localize.Settings_LoadMarkingAssets, LoadMarkingAssets);
            AddLabel(group, Localize.Settings_ApplyAfterRestart, 0.8f, Color.yellow, 25);
            AddCheckBox(group, Localize.Settings_RailUnderMarking, RailUnderMarking);
            AddLabel(group, Localize.Settings_RailUnderMarkingWarning, 0.8f, Color.red, 25);
            AddLabel(group, Localize.Settings_ApplyAfterRestart, 0.8f, Color.yellow, 25);
            AddCheckBox(group, CommonLocalize.Settings_ShowTooltips, ShowToolTip);
            AddCheckBox(group, Localize.Settings_ShowPaneltips, ShowPanelTip);
            AddCheckboxPanel(group, Localize.Settings_ShowDeleteWarnings, DeleteWarnings, DeleteWarningsType, new string[] { Localize.Settings_ShowDeleteWarningsAlways, Localize.Settings_ShowDeleteWarningsOnlyDependences });
            AddCheckBox(group, Localize.Settings_QuickRuleSetup, QuickRuleSetup);
            AddCheckBox(group, Localize.Settings_QuickBorderSetup, QuickBorderSetup);
            AddCheckBox(group, Localize.Settings_CutLineByCrosswalk, CutLineByCrosswalk);
            AddCheckBox(group, Localize.Settings_DontCutBorderByCrosswalk, NotCutBordersByCrosswalk);
            AddCheckBox(group, Localize.Settings_HideStreetName, HideStreetName);
        }
        private void AddGrouping(UIAdvancedHelper helper)
        {
            var group = helper.AddGroup(Localize.Settings_Groupings);

            AddCheckBox(group, Localize.Settings_GroupPoints, GroupPoints, OnChanged);
            AddCheckBox(group, Localize.Settings_GroupLines, GroupLines, OnChanged);
            AddCheckboxPanel(group, Localize.Settings_GroupTemplates, GroupTemplates, GroupTemplatesType, new string[] { Localize.Settings_GroupTemplatesByType, Localize.Settings_GroupTemplatesByStyle }, OnChanged);
            AddCheckboxPanel(group, Localize.Settings_GroupPointsOverlay, GroupPointsOverlay, GroupPointsOverlayType, new string[] { Localize.Settings_GroupPointsArrangeCircle, Localize.Settings_GroupPointsArrangeLine });

            static void OnChanged() => SingletonItem<NodeMarkupPanel>.Instance.UpdatePanel();
        }

        #endregion

        #endregion

        #region KEYMAPPING
        private void AddKeyMapping(UIAdvancedHelper helper)
        {
            var group = helper.AddGroup(CommonLocalize.Settings_Shortcuts);
            var keymappings = AddKeyMappingPanel(group);
            keymappings.AddKeymapping(NodeMarkupTool.ActivationShortcut);
            foreach (var shortcut in NodeMarkupTool.ToolShortcuts)
                keymappings.AddKeymapping(shortcut);

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
            button.textColor = Color.red;

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

        private void AddDebug(UIAdvancedHelper helper)
        {
            var group = helper.AddGroup("Selection overlay");

            AddCheckBox(group, "Alpha blend overlay", Selection.AlphaBlendOverlay);
            AddCheckBox(group, "Render overlay center", Selection.RenderOverlayCentre);
            AddCheckBox(group, "Render overlay borders", Selection.RenderOverlayBorders);
            AddFloatField(group, "Overlay width", Selection.OverlayWidth, 3f, 1f);

            var groupOther = helper.AddGroup("Nodes");
            AddCheckBox(groupOther, "Show node contour", ShowNodeContour);
        }
#endif
        #endregion
    }
}
