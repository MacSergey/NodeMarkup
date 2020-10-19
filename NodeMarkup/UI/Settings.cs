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
using System.Reflection;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;
using System.Globalization;
using NodeMarkup.UI.Editors;
using UnityEngine.SocialPlatforms;
using static ColossalFramework.UI.UIDropDown;
using ColossalFramework.Globalization;
using ColossalFramework.PlatformServices;
using NodeMarkup.Tools;

namespace NodeMarkup.UI
{
    public static class Settings
    {
        public static string SettingsFile => $"{nameof(NodeMarkup)}{nameof(SettingsFile)}";

        public static SavedString WhatsNewVersion { get; } = new SavedString(nameof(WhatsNewVersion), SettingsFile, Mod.Version.PrevMinor().ToString(), true);
        public static SavedFloat RenderDistance { get; } = new SavedFloat(nameof(RenderDistance), SettingsFile, 300f, true);
        public static SavedBool ShowToolTip { get; } = new SavedBool(nameof(ShowToolTip), SettingsFile, true, true);
        public static SavedBool DeleteWarnings { get; } = new SavedBool(nameof(DeleteWarnings), SettingsFile, true, true);
        public static SavedInt DeleteWarningsType { get; } = new SavedInt(nameof(DeleteWarningsType), SettingsFile, 0, true);
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

        private static CustomUITabstrip TabStrip { get; set; }
        private static List<UIPanel> TabPanels { get; set; }

        static Settings()
        {
            if (GameSettings.FindSettingsFileByName(SettingsFile) == null)
                GameSettings.AddSettingsFile(new SettingsFile[] { new SettingsFile() { fileName = SettingsFile } });
        }

        public static void OnSettingsUI(UIHelperBase helper)
        {
            var mainPanel = (helper as UIHelper).self as UIScrollablePanel;
            mainPanel.autoLayoutPadding = new RectOffset(0, 0, 0, 25);
            CreateTabStrip(mainPanel);

            var generalTab = CreateTab(mainPanel, Localize.Settings_GeneralTab);
            generalTab.AddGroup(Mod.StaticFullName);
            AddLanguage(generalTab);
            AddGeneral(generalTab);
            AddGrouping(generalTab);
            AddNotifications(generalTab);

            var shortcutTab = CreateTab(mainPanel, Localize.Settings_ShortcutsAndModifiersTab);
            AddKeyMapping(shortcutTab);

            var backupTab = CreateTab(mainPanel, Localize.Settings_BackupTab);
            if (SceneManager.GetActiveScene().name is string scene && (scene != "MainMenu" && scene != "IntroScreen"))
                AddBackupMarking(backupTab);
            AddBackupTemplates(backupTab);

            var supportTab = CreateTab(mainPanel, Localize.Settings_SupportTab);
            AddSupport(supportTab);
            //AddAccess(supportTab);
        }
        private static void CreateTabStrip(UIScrollablePanel mainPanel)
        {
            TabPanels = new List<UIPanel>();

            TabStrip = mainPanel.AddUIComponent<CustomUITabstrip>();
            TabStrip.eventSelectedIndexChanged += TabStripSelectedIndexChanged;
            TabStrip.selectedIndex = -1;
        }
        private static UIHelper CreateTab(UIScrollablePanel mainPanel, string name)
        {
            TabStrip.AddTab(name, 1.25f);

            var tabPanel = mainPanel.AddUIComponent<UIPanel>();
            tabPanel.size = new Vector2(mainPanel.width - mainPanel.scrollPadding.horizontal, mainPanel.height - mainPanel.scrollPadding.vertical - 2 * mainPanel.autoLayoutPadding.vertical - TabStrip.height);
            tabPanel.isVisible = false;
            TabPanels.Add(tabPanel);

            var panel = tabPanel.AddUIComponent<UIScrollablePanel>();
            UIUtils.AddScrollbar(tabPanel, panel);
            panel.verticalScrollbar.eventVisibilityChanged += ScrollbarVisibilityChanged;

            panel.size = tabPanel.size;
            panel.relativePosition = Vector2.zero;
            panel.autoLayout = true;
            panel.autoLayoutDirection = LayoutDirection.Vertical;
            panel.clipChildren = true;
            panel.scrollWheelDirection = UIOrientation.Vertical;

            return new UIHelper(panel);

            void ScrollbarVisibilityChanged(UIComponent component, bool value)
            {
                panel.width = tabPanel.width - (panel.verticalScrollbar.isVisible ? panel.verticalScrollbar.width : 0);
            }
        }

        private static void TabStripSelectedIndexChanged(UIComponent component, int index)
        {
            if (index >= 0 && TabPanels.Count > index)
            {
                foreach (var tab in TabPanels)
                    tab.isVisible = false;

                TabPanels[index].isVisible = true;
            }
        }


        #region SUPPORT

        private static void AddSupport(UIHelperBase helper)
        {
            UIHelper group = helper.AddGroup() as UIHelper;
            AddWiki(group);
            AddTroubleshooting(group);
            AddDiscord(group);
        }
        private static void AddWiki(UIHelper helper) => AddButton(helper, "Wiki", () => Utilities.OpenUrl(Mod.WikiUrl));
        private static void AddDiscord(UIHelper helper) => AddButton(helper, "Discord", () => Utilities.OpenUrl(Mod.DiscordURL));
        private static void AddTroubleshooting(UIHelper helper) => AddButton(helper, Localize.Settings_Troubleshooting, () => Utilities.OpenUrl(Mod.TroubleshootingUrl));

        #endregion

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
            var keymappingsPanel = (helper.AddGroup(Localize.Settings_Shortcuts) as UIHelper).self as UIPanel;

            var keymappings = keymappingsPanel.gameObject.AddComponent<KeymappingsPanel>();
            keymappings.AddKeymapping(Localize.Settings_ShortcutActivateTool, NodeMarkupTool.ActivationShortcut);
            keymappings.AddKeymapping(Localize.Settings_ShortcutDeleteAllNodeLines, NodeMarkupTool.DeleteAllShortcut);
            keymappings.AddKeymapping(Localize.Settings_ShortcutResetPointsOffset, NodeMarkupTool.ResetOffsetsShortcut);
            keymappings.AddKeymapping(Localize.Settings_ShortcutAddNewLineRule, NodeMarkupTool.AddRuleShortcut);
            keymappings.AddKeymapping(Localize.Settings_ShortcutAddNewFiller, NodeMarkupTool.AddFillerShortcut);
            keymappings.AddKeymapping(Localize.Settings_ShortcutCopyMarking, NodeMarkupTool.CopyMarkingShortcut);
            keymappings.AddKeymapping(Localize.Settings_ShortcutPasteMarking, NodeMarkupTool.PasteMarkingShortcut);
            keymappings.AddKeymapping(Localize.Settings_ShortcutEditMarking, NodeMarkupTool.EditMarkingShortcut);

            var regularLinesPanel = (helper.AddGroup(Localize.Settings_RegularLinesModifier) as UIHelper).self as UIPanel;
            var regularLinesModifier = regularLinesPanel.gameObject.AddComponent<RegularLineModifierPanel>();
            regularLinesModifier.OnModifierChanged += (Style.StyleType style, StyleModifier value) => NodeMarkupTool.StylesModifier[style].value = (int)value;

            var stopLinesPanel = (helper.AddGroup(Localize.Settings_StopLinesModifier) as UIHelper).self as UIPanel;
            var stopLinesModifier = stopLinesPanel.gameObject.AddComponent<StopLineModifierPanel>();
            stopLinesModifier.OnModifierChanged += (Style.StyleType style, StyleModifier value) => NodeMarkupTool.StylesModifier[style].value = (int)value;

            var crosswalksPanel = (helper.AddGroup(Localize.Settings_CrosswalksModifier) as UIHelper).self as UIPanel;
            var crosswalksModifier = crosswalksPanel.gameObject.AddComponent<CrosswalkModifierPanel>();
            crosswalksModifier.OnModifierChanged += (Style.StyleType style, StyleModifier value) => NodeMarkupTool.StylesModifier[style].value = (int)value;

            var fillersPanel = (helper.AddGroup(Localize.Settings_FillersModifier) as UIHelper).self as UIPanel;
            var fillersModifier = fillersPanel.gameObject.AddComponent<FillerModifierPanel>();
            fillersModifier.OnModifierChanged += (Style.StyleType style, StyleModifier value) => NodeMarkupTool.StylesModifier[style].value = (int)value;
        }

        #endregion

        #region GENERAL
        private static void AddGeneral(UIHelperBase helper)
        {
            UIHelper group = helper.AddGroup(Localize.Settings_DisplayAndUsage) as UIHelper;

            AddDistanceSetting(group);
            AddShowToolTipsSetting(group);
            AddCheckboxPanel(group, Localize.Settings_ShowDeleteWarnings, DeleteWarnings, DeleteWarningsType, new string[] { Localize.Settings_ShowDeleteWarningsAlways, Localize.Settings_ShowDeleteWarningsOnlyDependences });
            AddQuickRuleSetup(group);
            }
        private static void AddGrouping(UIHelperBase helper)
        {
            UIHelper group = helper.AddGroup(Localize.Settings_Groupings) as UIHelper;

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

        #region ACCESS
        //private static void AddAccess(UIHelperBase helper)
        //{
        //    UIHelper group = helper.AddGroup(Localize.Settings_EarlyAccess) as UIHelper;
        //    if (group.self is UIComponent component)
        //        component.AddUIComponent<EarlyAccessPanel>();
        //}

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

        #region BACKUP
        private static void AddBackupMarking(UIHelperBase helper)
        {
            UIHelper group = helper.AddGroup(Localize.Settings_BackupMarking) as UIHelper;

            AddDeleteAll(group, Localize.Settings_DeleteMarkingButton, Localize.Settings_DeleteMarkingCaption, $"{Localize.Settings_DeleteMarkingMessage}\n{Localize.MessageBox_CantUndone}", () => MarkupManager.DeleteAll());
            AddDump(group, Localize.Settings_DumpMarkingButton, Localize.Settings_DumpMarkingCaption, Loader.DumpMarkingData);
            AddRestore<ImportMarkingMessageBox>(group, Localize.Settings_RestoreMarkingButton, Localize.Settings_RestoreMarkingCaption, $"{Localize.Settings_RestoreMarkingMessage}\n{Localize.MessageBox_CantUndone}");
        }
        private static void AddBackupTemplates(UIHelperBase helper)
        {
            UIHelper group = helper.AddGroup(Localize.Settings_BackupTemplates) as UIHelper;

            AddDeleteAll(group, Localize.Settings_DeleteTemplatesButton, Localize.Settings_DeleteTemplatesCaption, $"{Localize.Settings_DeleteTemplatesMessage}\n{Localize.MessageBox_CantUndone}", () => TemplateManager.DeleteAll());
            AddDump(group, Localize.Settings_DumpTemplatesButton, Localize.Settings_DumpTemplatesCaption, Loader.DumpTemplatesData);
            AddRestore<ImportTemplatesMessageBox>(group, Localize.Settings_RestoreTemplatesButton, Localize.Settings_RestoreTemplatesCaption, $"{Localize.Settings_RestoreTemplatesMessage}\n{Localize.MessageBox_CantUndone}");
        }

        private static void AddDeleteAll(UIHelper group, string buttonText, string caption, string message, Action process)
        {
            var button = AddButton(group, buttonText, Click, 600);
            button.textColor = Color.red;

            void Click()
            {
                var messageBox = MessageBoxBase.ShowModal<YesNoMessageBox>();
                messageBox.CaprionText = caption;
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
        private static void AddDump(UIHelper group, string buttonText, string caption, Dump dump)
        {
            AddButton(group, buttonText, Click, 600);

            void Click()
            {
                var result = dump(out string path);

                if (result)
                {
                    var messageBox = MessageBoxBase.ShowModal<TwoButtonMessageBox>();
                    messageBox.CaprionText = caption;
                    messageBox.MessageText = Localize.Settings_DumpMessageSuccess;
                    messageBox.Button1Text = Localize.Settings_CopyPathToClipboard;
                    messageBox.Button2Text = Localize.MessageBox_OK;
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
                    messageBox.CaprionText = caption;
                    messageBox.MessageText = Localize.Settings_DumpMessageFailed;
                }
            }
        }
        private static void AddRestore<Modal>(UIHelper group, string buttonText, string caption, string message)
            where Modal : ImportMessageBox
        {
            AddButton(group, buttonText, Click, 600);

            void Click()
            {
                var messageBox = MessageBoxBase.ShowModal<Modal>();
                messageBox.CaprionText = caption;
                messageBox.MessageText = message;

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

    public class LanguageDropDown : UIDropDown<string> 
    { 
        public LanguageDropDown()
        {
            SetSettingsStyle(new Vector2(300, 31));
        }
    }
}
