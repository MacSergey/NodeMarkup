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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NodeMarkup
{
    public static class Settings
    {
        #region PROPERTIES

        public static string SettingsFile => $"{nameof(NodeMarkup)}{nameof(SettingsFile)}";

        public static SavedString WhatsNewVersion { get; } = new SavedString(nameof(WhatsNewVersion), SettingsFile, SingletonMod<Mod>.Version.PrevMinor(SingletonMod<Mod>.Versions).ToString(), true);
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
        public static SavedBool ShowWhatsNew { get; } = new SavedBool(nameof(ShowWhatsNew), SettingsFile, true, true);
        public static SavedBool ShowOnlyMajor { get; } = new SavedBool(nameof(ShowOnlyMajor), SettingsFile, false, true);
        public static SavedString Templates { get; } = new SavedString(nameof(Templates), SettingsFile, string.Empty, true);
        public static SavedString Intersections { get; } = new SavedString(nameof(Intersections), SettingsFile, string.Empty, true);
        public static SavedBool BetaWarning { get; } = new SavedBool(nameof(BetaWarning), SettingsFile, true, true);
        public static SavedString Locale { get; } = new SavedString(nameof(Locale), SettingsFile, string.Empty, true);

        public static SavedBool GroupPoints { get; } = new SavedBool(nameof(GroupPoints), SettingsFile, true, true);
        public static SavedBool GroupLines { get; } = new SavedBool(nameof(GroupLines), SettingsFile, false, true);
        public static SavedBool GroupTemplates { get; } = new SavedBool(nameof(GroupTemplates), SettingsFile, true, true);
        public static SavedInt GroupTemplatesType { get; } = new SavedInt(nameof(GroupTemplatesType), SettingsFile, 0, true);
        public static SavedBool GroupPointsOverlay { get; } = new SavedBool(nameof(GroupPointsOverlay), SettingsFile, true, true);
        public static SavedInt GroupPointsOverlayType { get; } = new SavedInt(nameof(GroupPointsOverlayType), SettingsFile, 0, true);
#if DEBUG
        public static SavedBool AlphaBlendOverlay { get; } = new SavedBool(nameof(AlphaBlendOverlay), SettingsFile, true, true);
        public static SavedFloat BorderOverlayWidth { get; } = new SavedFloat(nameof(BorderOverlayWidth), SettingsFile, 3f, true);
        public static SavedBool RenderOverlayCentre { get; } = new SavedBool(nameof(RenderOverlayCentre), SettingsFile, false, true);
        public static SavedBool RenderOverlayBorders { get; } = new SavedBool(nameof(RenderOverlayBorders), SettingsFile, false, true);
#endif
        private static TabStrip TabStrip { get; set; }
        private static List<CustomUIPanel> TabPanels { get; set; }

        #endregion

        #region BASIC

        static Settings()
        {
            if (GameSettings.FindSettingsFileByName(SettingsFile) == null)
                GameSettings.AddSettingsFile(new SettingsFile[] { new SettingsFile() { fileName = SettingsFile } });
        }

        public static void OnSettingsUI(UIHelperBase helper)
        {
            var scrollable = (helper as UIHelper).self as UIScrollablePanel;
            var mainPanel = scrollable.parent as UIPanel;

            foreach (var components in mainPanel.components)
                components.isVisible = false;

            mainPanel.autoLayout = true;
            mainPanel.autoLayoutDirection = LayoutDirection.Vertical;
            mainPanel.autoLayoutPadding = new RectOffset(0, 0, 0, 15);
            CreateTabStrip(mainPanel);

            var generalTab = CreateTab(mainPanel, Localize.Settings_GeneralTab);
            generalTab.AddGroup(SingletonMod<Mod>.Instance.Name);
            AddLanguage(generalTab);
            AddGeneral(generalTab);
            AddGrouping(generalTab);
            AddNotifications(generalTab);

            var shortcutTab = CreateTab(mainPanel, Localize.Settings_ShortcutsAndModifiersTab);
            AddKeyMapping(shortcutTab);

            var backupTab = CreateTab(mainPanel, Localize.Settings_BackupTab);
            AddBackupMarking(backupTab);
            AddBackupStyleTemplates(backupTab);
            AddBackupIntersectionTemplates(backupTab);

            var supportTab = CreateTab(mainPanel, Localize.Settings_SupportTab);
            AddSupport(supportTab);

#if DEBUG
            var debugTab = CreateTab(mainPanel, "Debug");
            AddDebug(debugTab);
#endif

            TabStrip.SelectedTab = 0;
        }
        private static void CreateTabStrip(UIPanel mainPanel)
        {
            TabPanels = new List<CustomUIPanel>();

            TabStrip = mainPanel.AddUIComponent<TabStrip>();
            TabStrip.SelectedTabChanged += OnSelectedTabChanged;
            TabStrip.SelectedTab = -1;
            TabStrip.width = mainPanel.width - mainPanel.autoLayoutPadding.horizontal;
            TabStrip.eventSizeChanged += (UIComponent component, Vector2 value) => TabStripSizeChanged(mainPanel);
        }

        private static void TabStripSizeChanged(UIPanel mainPanel)
        {
            foreach (var tab in TabPanels)
                SetTabSize(tab, mainPanel);
        }

        private static UIHelper CreateTab(UIPanel mainPanel, string name)
        {
            TabStrip.AddTab(name, 1.25f);

            var tabPanel = mainPanel.AddUIComponent<AdvancedScrollablePanel>();
            tabPanel.Content.autoLayoutPadding = new RectOffset(8, 8, 0, 0);
            SetTabSize(tabPanel, mainPanel);
            tabPanel.isVisible = false;
            TabPanels.Add(tabPanel);

            return new UIHelper(tabPanel.Content);
        }
        private static void SetTabSize(UIPanel panel, UIPanel mainPanel)
        {
            panel.size = new Vector2(mainPanel.width, mainPanel.height - mainPanel.autoLayoutPadding.vertical - TabStrip.height);
        }

        private static void OnSelectedTabChanged(int index)
        {
            if (index >= 0 && TabPanels.Count > index)
            {
                foreach (var tab in TabPanels)
                    tab.isVisible = false;

                TabPanels[index].isVisible = true;
            }
        }

        #endregion

        #region GENERAL

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
                SingletonMod<Mod>.Instance.LocaleChanged();
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

        #region DISPLAY&USAGE
        private static void AddGeneral(UIHelperBase helper)
        {
            UIHelper group = helper.AddGroup(Localize.Settings_DisplayAndUsage) as UIHelper;

            AddFloatField(group, Localize.Settings_RenderDistance, RenderDistance, 700f, 0f);
            AddFloatField(group, Localize.Settings_LODDistance, LODDistance, 300f, 0f);
            AddCheckBox(group, Localize.Settings_LoadMarkingAssets, LoadMarkingAssets);
            group.AddLabel(Localize.Settings_ApplyAfterRestart, 0.8f, Color.yellow, 25);
            AddCheckBox(group, Localize.Settings_RailUnderMarking, RailUnderMarking);
            group.AddLabel(Localize.Settings_RailUnderMarkingWarning, 0.8f, Color.red, 25);
            group.AddLabel(Localize.Settings_ApplyAfterRestart, 0.8f, Color.yellow, 25);
            AddCheckBox(group, Localize.Settings_ShowTooltips, ShowToolTip);
            AddCheckBox(group, Localize.Settings_ShowPaneltips, ShowPanelTip);
            AddCheckboxPanel(group, Localize.Settings_ShowDeleteWarnings, DeleteWarnings, DeleteWarningsType, new string[] { Localize.Settings_ShowDeleteWarningsAlways, Localize.Settings_ShowDeleteWarningsOnlyDependences });
            AddCheckBox(group, Localize.Settings_QuickRuleSetup, QuickRuleSetup);
            AddCheckBox(group, Localize.Settings_QuickBorderSetup, QuickBorderSetup);
            AddCheckBox(group, Localize.Settings_CutLineByCrosswalk, CutLineByCrosswalk);
            AddCheckBox(group, Localize.Settings_DontCutBorderByCrosswalk, NotCutBordersByCrosswalk);
        }
        private static void AddGrouping(UIHelperBase helper)
        {
            UIHelper group = helper.AddGroup(Localize.Settings_Groupings) as UIHelper;

            AddCheckBox(group, Localize.Settings_GroupPoints, GroupPoints, OnChanged);
            AddCheckBox(group, Localize.Settings_GroupLines, GroupLines, OnChanged);
            AddCheckboxPanel(group, Localize.Settings_GroupTemplates, GroupTemplates, GroupTemplatesType, new string[] { Localize.Settings_GroupTemplatesByType, Localize.Settings_GroupTemplatesByStyle }, OnChanged);
            AddCheckboxPanel(group, Localize.Settings_GroupPointsOverlay, GroupPointsOverlay, GroupPointsOverlayType, new string[] { Localize.Settings_GroupPointsArrangeCircle, Localize.Settings_GroupPointsArrangeLine });

            static void OnChanged() => NodeMarkupPanel.Instance.UpdatePanel();
        }
        private static void UpdateAllMarkings()
        {
            MarkupManager.NodeManager.AddAllToUpdate();
            MarkupManager.SegmentManager.AddAllToUpdate();
        }

        #endregion

        #region NOTIFICATIONS

        private static void AddNotifications(UIHelperBase helper)
        {
            UIHelper group = helper.AddGroup(Localize.Settings_Notifications) as UIHelper;

            AddCheckBox(group, Localize.Settings_ShowWhatsNew, ShowWhatsNew);
            AddCheckBox(group, Localize.Settings_ShowOnlyMajor, ShowOnlyMajor);
        }

        #endregion

        #endregion

        #region KEYMAPPING
        private static void AddKeyMapping(UIHelperBase helper)
        {
            var keymappingsPanel = (helper.AddGroup(Localize.Settings_Shortcuts) as UIHelper).self as UIPanel;

            var keymappings = keymappingsPanel.gameObject.AddComponent<KeymappingsPanel>();
            keymappings.AddKeymapping(NodeMarkupTool.ActivationShortcut);
            keymappings.AddKeymapping(NodeMarkupTool.AddRuleShortcut);
            foreach (var shortcut in NodeMarkupTool.Shortcuts)
                keymappings.AddKeymapping(shortcut);

            AddModifier<RegularLineModifierPanel>(helper, Localize.Settings_RegularLinesModifier);
            AddModifier<StopLineModifierPanel>(helper, Localize.Settings_StopLinesModifier);
            AddModifier<CrosswalkModifierPanel>(helper, Localize.Settings_CrosswalksModifier);
            AddModifier<FillerModifierPanel>(helper, Localize.Settings_FillersModifier);
        }
        private static void AddModifier<PanelType>(UIHelperBase helper, string title)
            where PanelType : StyleModifierPanel
        {
            var panel = (helper.AddGroup(title) as UIHelper).self as UIPanel;
            var modifier = panel.gameObject.AddComponent<PanelType>();
            modifier.OnModifierChanged += ModifierChanged;

            static void ModifierChanged(Style.StyleType style, StyleModifier value) => NodeMarkupTool.StylesModifier[style].value = (int)value;
        }

        #endregion

        #region BACKUP
        private static void AddBackupMarking(UIHelperBase helper)
        {
            if (!ItemsExtension.InGame)
                return;

            UIHelper group = helper.AddGroup(Localize.Settings_BackupMarking) as UIHelper;

            AddDeleteAll(group, Localize.Settings_DeleteMarkingButton, Localize.Settings_DeleteMarkingCaption, $"{Localize.Settings_DeleteMarkingMessage}\n{NodeMarkupMessageBox.CantUndone}", () => MarkupManager.Clear());
            AddDump(group, Localize.Settings_DumpMarkingButton, Localize.Settings_DumpMarkingCaption, Loader.DumpMarkingData);
            AddRestore<ImportMarkingMessageBox>(group, Localize.Settings_RestoreMarkingButton, Localize.Settings_RestoreMarkingCaption, $"{Localize.Settings_RestoreMarkingMessage}\n{NodeMarkupMessageBox.CantUndone}");
        }
        private static void AddBackupStyleTemplates(UIHelperBase helper)
        {
            UIHelper group = helper.AddGroup(Localize.Settings_BackupTemplates) as UIHelper;

            AddDeleteAll(group, Localize.Settings_DeleteTemplatesButton, Localize.Settings_DeleteTemplatesCaption, $"{Localize.Settings_DeleteTemplatesMessage}\n{NodeMarkupMessageBox.CantUndone}", () => TemplateManager.StyleManager.DeleteAll());
            AddDump(group, Localize.Settings_DumpTemplatesButton, Localize.Settings_DumpTemplatesCaption, Loader.DumpStyleTemplatesData);
            AddRestore<ImportStyleTemplatesMessageBox>(group, Localize.Settings_RestoreTemplatesButton, Localize.Settings_RestoreTemplatesCaption, $"{Localize.Settings_RestoreTemplatesMessage}\n{NodeMarkupMessageBox.CantUndone}");
        }

        private static void AddBackupIntersectionTemplates(UIHelperBase helper)
        {
            UIHelper group = helper.AddGroup(Localize.Settings_BackupPresets) as UIHelper;

            AddDeleteAll(group, Localize.Settings_DeletePresetsButton, Localize.Settings_DeletePresetsCaption, $"{Localize.Settings_DeletePresetsMessage}\n{NodeMarkupMessageBox.CantUndone}", () => TemplateManager.IntersectionManager.DeleteAll());
            AddDump(group, Localize.Settings_DumpPresetsButton, Localize.Settings_DumpPresetsCaption, Loader.DumpIntersectionTemplatesData);
            AddRestore<ImportIntersectionTemplatesMessageBox>(group, Localize.Settings_RestorePresetsButton, Localize.Settings_RestorePresetsCaption, $"{Localize.Settings_RestorePresetsMessage}\n{NodeMarkupMessageBox.CantUndone}");
        }

        private static void AddDeleteAll(UIHelper group, string buttonText, string caption, string message, Action process)
        {
            var button = AddButton(group, buttonText, Click, 600);
            button.textColor = Color.red;

            void Click()
            {
                var messageBox = MessageBoxBase.ShowModal<YesNoMessageBox>();
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
        private static void AddDump(UIHelper group, string buttonText, string caption, Dump dump)
        {
            AddButton(group, buttonText, Click, 600);

            void Click()
            {
                var result = dump(out string path);

                if (result)
                {
                    var messageBox = MessageBoxBase.ShowModal<TwoButtonMessageBox>();
                    messageBox.CaptionText = caption;
                    messageBox.MessageText = Localize.Settings_DumpMessageSuccess;
                    messageBox.Button1Text = Localize.Settings_CopyPathToClipboard;
                    messageBox.Button2Text = NodeMarkupMessageBox.Ok;
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
                    var messageBox = MessageBoxBase.ShowModal<OkMessageBox>();
                    messageBox.CaptionText = caption;
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
                messageBox.CaptionText = caption;
                messageBox.MessageText = message;

            }
        }


        #endregion

        #region SUPPORT

        private static void AddSupport(UIHelperBase helper)
        {
            UIHelper group = helper.AddGroup() as UIHelper;
            AddWiki(group);
            AddTroubleshooting(group);
            AddDiscord(group);
            AddChangeLog(group);
        }
        private static void AddWiki(UIHelper helper) => AddButton(helper, "Wiki", () => Utilities.Utilities.OpenUrl(Mod.WikiUrl));
        private static void AddDiscord(UIHelper helper) => AddButton(helper, "Discord", () => Utilities.Utilities.OpenUrl(Mod.DiscordURL));
        private static void AddTroubleshooting(UIHelper helper) => AddButton(helper, Localize.Settings_Troubleshooting, () => Utilities.Utilities.OpenUrl(Mod.TroubleshootingUrl));
        private static void AddChangeLog(UIHelper helper) => AddButton(helper, Localize.Settings_ChangeLog, ShowChangeLog);

        private static void ShowChangeLog()
        {
            var messages = SingletonMod<Mod>.Instance.GetWhatsNewMessages(new Version(1, 0));
            var messageBox = MessageBoxBase.ShowModal<WhatsNewMessageBox>();
            messageBox.CaptionText = Localize.Settings_ChangeLog;
            messageBox.OkText = NodeMarkupMessageBox.Ok;
            messageBox.Init(messages, SingletonMod<Mod>.Instance.GetVersionString, false);
        }

        #endregion

        #region DEBUG

#if DEBUG
        private static void AddDebug(UIHelperBase helper)
        {
            UIHelper group = helper.AddGroup("Debug") as UIHelper;

            AddCheckBox(group, "Alpha blend overlay", AlphaBlendOverlay);
            AddCheckBox(group, "Render overlay center", RenderOverlayCentre);
            AddCheckBox(group, "Render overlay borders", RenderOverlayBorders);
            AddFloatField(group, "Overlay width", BorderOverlayWidth, 3f, 1f);
        }
#endif

        #endregion

        #region UTILITY

        private static void AddFloatField(UIHelper group, string label, SavedFloat saved, float? defaultValue, float? min = null, float? max = null, Action onSubmit = null)
        {
            UITextField field = null;
            field = group.AddTextfield(label, saved.ToString(), OnChanged, OnSubmitted) as UITextField;

            static void OnChanged(string distance) { }
            void OnSubmitted(string text)
            {
                if (float.TryParse(text, out float value))
                {
                    if ((min.HasValue && value < min.Value) || (max.HasValue && value > max.Value))
                        value = defaultValue ?? 0;

                    saved.value = value;
                    field.text = value.ToString();
                }
                else
                    field.text = saved.ToString();

                onSubmit?.Invoke();
            }
        }

        private static void AddCheckBox(UIHelper group, string label, SavedBool saved, Action onChanged = null)
        {
            group.AddCheckbox(label, saved, OnValueChanged);

            void OnValueChanged(bool value)
            {
                saved.value = value;
                onChanged?.Invoke();
            }
        }

        private static void AddCheckboxPanel(UIHelper group, string mainLabel, SavedBool mainSaved, SavedInt optionsSaved, string[] labels, Action onChanged = null)
        {
            var inProcess = false;
            var checkBoxes = new UICheckBox[labels.Length];
            var optionsPanel = default(CustomUIPanel);

            var mainCheckBox = group.AddCheckbox(mainLabel, mainSaved, OnMainChanged) as UICheckBox;

            optionsPanel = (group.self as UIComponent).AddUIComponent<CustomUIPanel>();
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
                onChanged?.Invoke();
                SetVisible();
            }
            void SetVisible() => optionsPanel.isVisible = mainSaved;
            void Set(int index, bool value)
            {
                if (!inProcess)
                {
                    inProcess = true;
                    optionsSaved.value = index;
                    onChanged?.Invoke();
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
        public static void AddLabel(this UIHelper helper, string text, float size = 1.125f, Color? color = null, int padding = 0)
        {
            var component = helper.self as UIComponent;

            var label = component.AddUIComponent<CustomUILabel>();
            label.text = text;
            label.textScale = size;
            label.textColor = color ?? Color.white;
            label.padding = new RectOffset(padding, 0, 0, 0);
        }

        private class LanguageDropDown : UIDropDown<string>
        {
            public LanguageDropDown()
            {
                SetSettingsStyle(new Vector2(300, 31));
            }
        }

        #endregion
    }
}
