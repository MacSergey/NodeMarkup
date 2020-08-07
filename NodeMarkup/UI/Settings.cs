using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Diagnostics;
using ColossalFramework.Threading;
using System.Threading;

namespace NodeMarkup.UI
{
    public static class Settings
    {
        public static string SettingsFile => $"{nameof(NodeMarkup)}{nameof(SettingsFile)}";

        public static SavedString WhatsNewVersion { get; } = new SavedString(nameof(WhatsNewVersion), SettingsFile, Mod.Version, true);
        public static SavedFloat RenderDistance { get; } = new SavedFloat(nameof(RenderDistance), SettingsFile, 300f, true);
        public static SavedBool ShowToolTip { get; } = new SavedBool(nameof(ShowToolTip), SettingsFile, true, true);
        public static SavedBool DeleteWarnings { get; } = new SavedBool(nameof(DeleteWarnings), SettingsFile, true, true);
        public static SavedBool QuickRuleSetup { get; } = new SavedBool(nameof(QuickRuleSetup), SettingsFile, true, true);
        public static SavedBool ShowWhatsNew { get; } = new SavedBool(nameof(ShowWhatsNew), SettingsFile, true, true);
        public static SavedBool ShowOnlyMajor { get; } = new SavedBool(nameof(ShowOnlyMajor), SettingsFile, false, true);
        public static SavedString Templates { get; } = new SavedString(nameof(Templates), SettingsFile, string.Empty, true);
        //public static SavedString AccessKey { get; } = new SavedString(nameof(AccessKey), SettingsFile, string.Empty, true);

        static Settings()
        {
            if (GameSettings.FindSettingsFileByName(SettingsFile) == null)
                GameSettings.AddSettingsFile(new SettingsFile[] { new SettingsFile() { fileName = SettingsFile } });
        }

        public static void OnSettingsUI(UIHelperBase helper)
        {
            AddKeyMapping(helper);
            AddGeneral(helper);
            AddAccess(helper);
            AddNotifications(helper);
            AddOther(helper);
        }

        #region KEYMAPPING
        private static void AddKeyMapping(UIHelperBase helper)
        {
            UIHelper group = helper.AddGroup(Localize.Settings_Shortcuts) as UIHelper;
            UIPanel panel = group.self as UIPanel;

            var keymappings = panel.gameObject.AddComponent<KeymappingsPanel>();
            keymappings.AddKeymapping(Localize.Settings_ActivateTool, NodeMarkupTool.ActivationShortcut);
            keymappings.AddKeymapping(Localize.Settings_DeleteAllNodeLines, NodeMarkupTool.DeleteAllShortcut);
            keymappings.AddKeymapping(Localize.Settings_AddNewLineRule, NodeMarkupTool.AddRuleShortcut);
            keymappings.AddKeymapping(Localize.Settings_AddNewFiller, NodeMarkupTool.AddFillerShortcut);
        }
        #endregion

        #region GENERAL
        private static void AddGeneral(UIHelperBase helper)
        {
            UIHelper group = helper.AddGroup(Localize.Settings_General) as UIHelper;

            AddDistanceSetting(group);
            AddShowToolTipsSetting(group);
            AddDeleteRequest(group);
            AddQuickRuleSetup(group);

        }
        private static void AddDistanceSetting(UIHelper group)
        {
            UITextField distanceField = null;
            distanceField = group.AddTextfield(Localize.Settings_RenderDistance, RenderDistance.ToString(), OnDistanceChanged, OnDistanceSubmitted) as UITextField;

            void OnDistanceChanged(string distance) { }
            void OnDistanceSubmitted(string text)
            {
                if (float.TryParse(text, out float distance))
                {
                    if (distance < 0)
                        distance = 300;

                    RenderDistance.value = distance;
                    distanceField.text = distance.ToString();
                }
                else
                {
                    distanceField.text = RenderDistance.ToString();
                }
            }
        }
        private static void AddShowToolTipsSetting(UIHelper group)
        {
            var showCheckBox = group.AddCheckbox(Localize.Settings_ShowTooltips, ShowToolTip, OnShowToolTipsChanged) as UICheckBox;

            void OnShowToolTipsChanged(bool show) => ShowToolTip.value = show;
        }
        private static void AddDeleteRequest(UIHelper group)
        {
            var requestCheckBox = group.AddCheckbox(Localize.Settings_ShowDeleteWarnings, DeleteWarnings, OnDeleteRequestChanged) as UICheckBox;

            void OnDeleteRequestChanged(bool request) => DeleteWarnings.value = request;
        }
        private static void AddQuickRuleSetup(UIHelper group)
        {
            var quickRuleSetupCheckBox = group.AddCheckbox(Localize.Settings_QuickRuleSetup, QuickRuleSetup, OnQuickRuleSetuptChanged) as UICheckBox;

            void OnQuickRuleSetuptChanged(bool request) => QuickRuleSetup.value = request;
        }
        #endregion

        #region ACCESS
        private static void AddAccess(UIHelperBase helper)
        {
            UIHelper group = helper.AddGroup("Early access") as UIHelper;
            if (group.self is UIComponent component)
                component.AddUIComponent<EarlyAccessPanel>();
        }

        #endregion

        #region NOTIFICATIONS
        private static void AddNotifications(UIHelperBase helper)
        {
            UIHelper group = helper.AddGroup(Localize.Settings_Notifications) as UIHelper;

            AddShowWhatsNew(group);
            AddShowOnlyMajor(group);
        }
        private static void AddShowWhatsNew(UIHelper group)
        {
            var showWhatsNewCheckBox = group.AddCheckbox(Localize.Settings_ShowWhatsNew, ShowWhatsNew, OnShowWhatsNewChanged) as UICheckBox;

            void OnShowWhatsNewChanged(bool request) => ShowWhatsNew.value = request;
        }
        private static void AddShowOnlyMajor(UIHelper group)
        {
            var showOnlyMajorCheckBox = group.AddCheckbox(Localize.Settings_ShowOnlyMajor, ShowOnlyMajor, OnShowOnlyMajorChanged) as UICheckBox;

            void OnShowOnlyMajorChanged(bool request) => ShowOnlyMajor.value = request;
        }
        #endregion

        #region OTHER
        private static void AddOther(UIHelperBase helper)
        {
            if (SceneManager.GetActiveScene().name is string scene && (scene == "MainMenu" || scene == "IntroScreen"))
                return;

            UIHelper group = helper.AddGroup(Localize.Settings_Other) as UIHelper;

            AddDeleteAll(group);
            AddDump(group);
            AddImport(group);
        }
        private static void AddDeleteAll(UIHelper group)
        {
            var button = group.AddButton(Localize.Settings_DeleteMarkingButton, Click) as UIButton;
            button.textColor = Color.red;

            void Click()
            {
                var messageBox = MessageBoxBase.ShowModal<YesNoMessageBox>();
                messageBox.CaprionText = Localize.Settings_DeleteMarkingCaption;
                messageBox.MessageText = Localize.Settings_DeleteMarkingMessage;
                messageBox.OnButton1Click = Сonfirmed;
            }
            bool Сonfirmed()
            {
                MarkupManager.DeleteAll();
                return true;
            }
        }
        private static void AddDump(UIHelper group)
        {
            var button = group.AddButton(Localize.Settings_DumpMarkingButton, Click) as UIButton;

            void Click()
            {
                var result = Serializer.OnDumpData(out string path);

                if (result)
                {
                    var messageBox = MessageBoxBase.ShowModal<TwoButtonMessageBox>();
                    messageBox.CaprionText = Localize.Settings_DumpMarkingCaption;
                    messageBox.MessageText = Localize.Settings_DumpMarkingMessageSuccess;
                    messageBox.Button1Text = Localize.Settings_DumpMarkingButton1;
                    messageBox.Button2Text = Localize.Settings_DumpMarkingButton2;
                    messageBox.OnButton1Click = CopyToClipboard;

                    bool CopyToClipboard()
                    {
                        Clipboard.text = path;
                        return false;
                    }
                }
                else
                {
                    var messageBox = MessageBoxBase.ShowModal<OkMessageBox>();
                    messageBox.CaprionText = Localize.Settings_DumpMarkingCaption;
                    messageBox.MessageText = Localize.Settings_DumpMarkingMessageFailed;
                }
            }
        }
        private static void AddImport(UIHelper group)
        {
            var button = group.AddButton(Localize.Settings_ImportMarkingButton, Click) as UIButton;

            void Click()
            {
                var messageBox = MessageBoxBase.ShowModal<ImportMessageBox>();
                messageBox.CaprionText = Localize.Settings_ImportMarkingCaption;
                messageBox.MessageText = Localize.Settings_ImportMarkingMessage;

            }
        }
        #endregion
    }
}
