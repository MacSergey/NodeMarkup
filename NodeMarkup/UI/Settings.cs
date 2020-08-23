using ColossalFramework;
using ColossalFramework.UI;
using ICities;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Globalization;
using NodeMarkup.UI.Editors;
using static ColossalFramework.UI.UIDropDown;
using ColossalFramework.Globalization;

namespace NodeMarkup.UI
{
    public static class Settings
    {
        public static string SettingsFile => $"{nameof(NodeMarkup)}{nameof(SettingsFile)}";

        public static SavedString WhatsNewVersion { get; } = new SavedString(nameof(WhatsNewVersion), SettingsFile, Mod.Version.PrevMinor().ToString(), true);
        public static SavedFloat RenderDistance { get; } = new SavedFloat(nameof(RenderDistance), SettingsFile, 300f, true);
        public static SavedBool ShowToolTip { get; } = new SavedBool(nameof(ShowToolTip), SettingsFile, true, true);
        public static SavedBool DeleteWarnings { get; } = new SavedBool(nameof(DeleteWarnings), SettingsFile, true, true);
        public static SavedBool QuickRuleSetup { get; } = new SavedBool(nameof(QuickRuleSetup), SettingsFile, true, true);
        public static SavedBool ShowWhatsNew { get; } = new SavedBool(nameof(ShowWhatsNew), SettingsFile, true, true);
        public static SavedBool ShowOnlyMajor { get; } = new SavedBool(nameof(ShowOnlyMajor), SettingsFile, false, true);
        public static SavedString Templates { get; } = new SavedString(nameof(Templates), SettingsFile, string.Empty, true);
        public static SavedBool BetaWarning { get; } = new SavedBool(nameof(BetaWarning), SettingsFile, true, true);
        public static SavedString Locale { get; } = new SavedString(nameof(Locale), SettingsFile, string.Empty, true);
        public static SavedBool GroupLines { get; } = new SavedBool(nameof(GroupLines), SettingsFile, false, true);
        public static SavedBool GroupTemplates { get; } = new SavedBool(nameof(GroupTemplates), SettingsFile, true, true);
        public static SavedInt GroupTemplatesType { get; } = new SavedInt(nameof(GroupTemplatesType), SettingsFile, 0, true);
        public static SavedBool GroupPoints { get; } = new SavedBool(nameof(GroupPoints), SettingsFile, true, true);
        public static SavedInt GroupPointsType { get; } = new SavedInt(nameof(GroupPointsType), SettingsFile, 0, true);

        static Settings()
        {
            if (GameSettings.FindSettingsFileByName(SettingsFile) == null)
                GameSettings.AddSettingsFile(new SettingsFile[] { new SettingsFile() { fileName = SettingsFile } });
        }

        public static void OnSettingsUI(UIHelperBase helper)
        {
            AddSupport(helper);
            AddLanguage(helper);
            AddKeyMapping(helper);
            AddGeneral(helper);
            AddNotifications(helper);
            AddOther(helper);
        }
        private static void AddSupport(UIHelperBase helper)
        {
            UIHelper group = helper.AddGroup(Localize.Mod_Support) as UIHelper;
            AddWiki(group);
            AddTroubleshooting(group);
            AddDiscord(group);
        }
        private static void AddWiki(UIHelper helper) => AddButton(helper, "Wiki", () => Utilities.OpenUrl(Mod.WikiUrl));
        private static void AddDiscord(UIHelper helper) => AddButton(helper, "Discord", () => Utilities.OpenUrl(Mod.DiscordURL));
        private static void AddTroubleshooting(UIHelper helper) => AddButton(helper, Localize.Settings_Troubleshooting, () => Utilities.OpenUrl(Mod.TroubleshootingUrl));

        #region LANGUAGE

        private static void AddLanguage(UIHelperBase helper)
        {
            UIHelper group = helper.AddGroup(Localize.Settings_Language) as UIHelper;
            AddLanguageList(group);
        }
        private static void AddLanguageList(UIHelper group)
        {
            var locales = GetSupportLanguages().ToArray();
            var dropDown = (group.self as UIComponent).AddUIComponent<LanguageDropDown>();

            dropDown.atlas = TextureUtil.InGameAtlas;
            dropDown.size = new Vector2(400, 38);
            dropDown.listBackground = "OptionsDropboxListbox";
            dropDown.itemHeight = 24;
            dropDown.itemHover = "ListItemHover";
            dropDown.itemHighlight = "ListItemHighlight";
            dropDown.normalBgSprite = "OptionsDropbox";
            dropDown.hoveredBgSprite = "OptionsDropboxHovered";
            dropDown.focusedBgSprite = "OptionsDropboxFocused";
            dropDown.autoListWidth = true;
            dropDown.listHeight = 700;
            dropDown.listPosition = PopupListPosition.Below;
            dropDown.clampListToScreen = false;
            dropDown.foregroundSpriteMode = UIForegroundSpriteMode.Stretch;
            dropDown.popupColor = Color.white;
            dropDown.popupTextColor = new Color32(170, 170, 170, 255);
            dropDown.textScale = 1.25f;
            dropDown.textFieldPadding = new RectOffset(14, 40, 7, 0);
            dropDown.popupColor = Color.white;
            dropDown.popupTextColor = new Color32(170, 170, 170, 255);
            dropDown.verticalAlignment = UIVerticalAlignment.Middle;
            dropDown.horizontalAlignment = UIHorizontalAlignment.Left;
            dropDown.itemPadding = new RectOffset(14, 14, 0, 0);
            dropDown.triggerButton = dropDown;

            dropDown.AddItem(string.Empty, Localize.Mod_LocaleGame);

            foreach (var locale in locales)
            {
                var localizeString = $"Mod_Locale_{locale}";
                var localeText = Localize.ResourceManager.GetString(localizeString, Localize.Culture);
                if (Localize.Culture.Name.ToLower() != locale)
                    localeText += $" ({Localize.ResourceManager.GetString(localizeString, new CultureInfo(locale))})";

                dropDown.AddItem(locale, localeText);
            }

            dropDown.SelectedObject = Locale.value;

            dropDown.eventSelectedIndexChanged += IndexChanged;

            void IndexChanged(UIComponent component, int value)
            {
                var locale = dropDown.SelectedObject;
                Locale.value = locale;
                LocaleManager.ForceReload();
            }
        }

        private static string[] GetSupportLanguages()
        {
            var languages = new HashSet<string> { "en" };

            var resourceAssembly = $"{Assembly.GetExecutingAssembly().GetName().Name}.resources";

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var assemblyName = assembly.GetName();
                if (assemblyName.Name == resourceAssembly)
                    languages.Add(assemblyName.CultureInfo.Name.ToLower());
            }

            return languages.OrderBy(l => l).ToArray();
        }

        #endregion

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
            AddGroupLines(group);
            AddCheckboxPanel(group, Localize.Settings_GroupTemplates, GroupTemplates, GroupTemplatesType, new string[] { Localize.Settings_GroupTemplatesByType, Localize.Settings_GroupTemplatesByStyle });
            AddCheckboxPanel(group, Localize.Settings_GroupPoints, GroupPoints, GroupPointsType, new string[] { Localize.Settings_GroupPointsArrangeCircle, Localize.Settings_GroupPointsArrangeLine });
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
            var quickRuleSetupCheckBox = group.AddCheckbox(Localize.Settings_QuickRuleSetup, QuickRuleSetup, OnQuickRuleSetupChanged) as UICheckBox;

            void OnQuickRuleSetupChanged(bool request) => QuickRuleSetup.value = request;
        }
        private static void AddGroupLines(UIHelper group)
        {
            var groupLinesCheckBox = group.AddCheckbox(Localize.Settings_GroupLines, GroupLines, OnGroupLinesChanged) as UICheckBox;

            void OnGroupLinesChanged(bool groupLines) => GroupLines.value = groupLines;
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
            var button = AddButton(group, Localize.Settings_DeleteMarkingButton, Click, 600);
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
            AddButton(group, Localize.Settings_DumpMarkingButton, Click, 600);

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
            AddButton(group, Localize.Settings_ImportMarkingButton, Click, 600);

            void Click()
            {
                var messageBox = MessageBoxBase.ShowModal<ImportMessageBox>();
                messageBox.CaprionText = Localize.Settings_ImportMarkingCaption;
                messageBox.MessageText = Localize.Settings_ImportMarkingMessage;

            }
        }
        #endregion

        private static void AddCheckboxPanel(UIHelper group, string mainLabel, SavedBool mainSaved, SavedInt optionsSaved, string[] labels)
        {
            var inProcess = false;
            var checkBoxes = new UICheckBox[labels.Length];
            var optionsPanel = default(UIPanel);

            var mainCheckBox = group.AddCheckbox(mainLabel, mainSaved, OnMainChanged) as UICheckBox;

            optionsPanel = (group.self as UIComponent).AddUIComponent<UIPanel>();
            optionsPanel.autoLayout = true;
            optionsPanel.autoLayoutDirection = LayoutDirection.Vertical;
            optionsPanel.autoFitChildrenHorizontally = true;
            optionsPanel.autoFitChildrenVertically = true;
            optionsPanel.autoLayoutPadding = new RectOffset(25, 0, 0, 5);
            var panelHelper = new UIHelper(optionsPanel);

            for (var i = 0; i < checkBoxes.Length; i += 1)
            {
                var index = i;
                checkBoxes[i] = panelHelper.AddCheckbox(labels[i], optionsSaved == i, (value) => Set(index, value)) as UICheckBox;
            }

            SetVisible();

            void OnMainChanged(bool value)
            {
                mainSaved.value = value;
                SetVisible();
            }
            void SetVisible() => optionsPanel.isVisible = mainSaved;
            void Set(int index, bool value)
            {
                if (!inProcess)
                {
                    inProcess = true;
                    optionsSaved.value = index;
                    for (var i = 0; i < checkBoxes.Length; i += 1)
                        checkBoxes[i].isChecked = optionsSaved == i;
                    inProcess = false;
                }
            }
        }
        private static UIButton AddButton(UIHelper group, string text, OnButtonClicked click, float width = 400)
        {
            var button = group.AddButton(text, click) as UIButton;
            button.autoSize = false;
            button.textHorizontalAlignment = UIHorizontalAlignment.Center;
            button.width = width;

            return button;
        }
    }

    public class LanguageDropDown : CustomUIDropDown<string> { }
}
