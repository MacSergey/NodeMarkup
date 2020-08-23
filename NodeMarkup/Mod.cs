using ICities;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NodeMarkup.UI;
using System.Globalization;
using ColossalFramework.Globalization;
using ColossalFramework;
using NodeMarkup.Utils;

namespace NodeMarkup
{
    public class Mod : LoadingExtensionBase, IUserMod
    {
        public static string StableURL { get; } = "https://steamcommunity.com/sharedfiles/filedetails/?id=2140418403";
        public static string BetaURL { get; } = "https://steamcommunity.com/sharedfiles/filedetails/?id=2159934925";
        public static string DiscordURL { get; } = "https://discord.gg/QRYq8m2";
        public static string ReportBugUrl { get; } = "https://github.com/MacSergey/NodeMarkup/issues/new?assignees=&labels=NEW+ISSUE&template=bug_report.md";
        public static string WikiUrl { get; } = "https://github.com/MacSergey/NodeMarkup/wiki";
        public static string TroubleshootingUrl { get; } = "https://github.com/MacSergey/NodeMarkup/wiki/Troubleshooting";

        public static string StaticName { get; } = "Intersection Marking Tool";

        public static Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public static Version VersionBuild => Version.Build();

        public static List<Version> Versions { get; } = new List<Version>
        {
            new Version("1.3"),
            new Version("1.2.1"),
            new Version("1.2"),
            new Version("1.1"),
            new Version("1.0")
        };

#if DEBUG
        public static bool IsBeta => true;
        public string Name { get; } = $"{StaticName} {Version.GetString()} [BETA]";
        public string Description => Localize.Mod_DescriptionBeta;
#else
        public static bool IsBeta => false;
        public string Name { get; } = $"{StaticName} {Version.GetString()}";
        public string Description => Localize.Mod_Description;
#endif

        static CultureInfo Culture
        {
            get
            {
                var locale = string.IsNullOrEmpty(UI.Settings.Locale.value) ? SingletonLite<LocaleManager>.instance.language : UI.Settings.Locale.value;
                if (locale == "zh")
                    locale = "zh-cn";

                return new CultureInfo(locale);
            }
        }

        public void OnEnabled()
        {
            Logger.LogDebug($"{nameof(Mod)}.{nameof(OnEnabled)}");
            Patcher.Patch();
        }
        public void OnDisabled()
        {
            Logger.LogDebug($"{nameof(Mod)}.{nameof(OnDisabled)}");
            Patcher.Unpatch();
            NodeMarkupTool.Remove();

            LocaleManager.eventLocaleChanged -= LocaleChanged;
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            Logger.LogDebug($"{nameof(Mod)}.{nameof(OnLevelLoaded)}");
            if (mode == LoadMode.LoadGame || mode == LoadMode.NewGame || mode == LoadMode.NewGameFromScenario || mode == LoadMode.NewAsset)
            {
                NodeMarkupTool.Create();
                MarkupManager.Init();

                ShowWhatsNew();
                ShowBetaWarning();
                ShowLoadError();
            }
        }

        public override void OnLevelUnloading()
        {
            Logger.LogDebug($"{nameof(Mod)}.{nameof(OnLevelUnloading)}");
            NodeMarkupTool.Remove();
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            LocaleManager.eventLocaleChanged -= LocaleChanged;
            LocaleChanged();
            LocaleManager.eventLocaleChanged += LocaleChanged;

            Logger.LogDebug($"{nameof(Mod)}.{nameof(OnSettingsUI)}");
            UI.Settings.OnSettingsUI(helper);
        }

        private void LocaleChanged()
        {
            Logger.LogDebug($"{nameof(Mod)}.{nameof(LocaleChanged)}");
            Localize.Culture = Culture;
            Logger.LogDebug($"current cultute - {Localize.Culture?.Name ?? "null"}");
        }

        public static bool OpenTroubleshooting()
        {
            Utilities.OpenUrl(TroubleshootingUrl);
            return true;
        }
        private void ShowLoadError()
        {
            if (MarkupManager.LoadErrors != 0)
            {
                var messageBox = MessageBoxBase.ShowModal<TwoButtonMessageBox>();
                messageBox.CaprionText = StaticName;
                messageBox.MessageText = string.Format(Localize.Mod_LoadFailed, MarkupManager.LoadErrors);
                messageBox.Button1Text = Localize.MessageBox_OK;
                messageBox.Button2Text = Localize.Mod_Support;
                messageBox.OnButton2Click = OpenTroubleshooting;
            }
        }
        private void ShowBetaWarning()
        {
            if (!IsBeta)
                UI.Settings.BetaWarning.value = true;
            else if (UI.Settings.BetaWarning.value)
            {
                var messageBox = MessageBoxBase.ShowModal<TwoButtonMessageBox>();
                messageBox.CaprionText = Localize.Mod_BetaWarningCaption;
                messageBox.MessageText = string.Format(Localize.Mod_BetaWarningMessage, StaticName);
                messageBox.Button1Text = Localize.Mod_BetaWarningAgree;
                messageBox.Button2Text = Localize.Mod_BetaWarningGetStable;
                messageBox.OnButton1Click = AgreeClick;
                messageBox.OnButton2Click = GetStable;

                bool AgreeClick()
                {
                    UI.Settings.BetaWarning.value = false;
                    return true;
                }
                bool GetStable()
                {
                    Utilities.OpenUrl(StableURL);
                    return true;
                }
            }
        }
        private void ShowWhatsNew()
        {
            var whatNewVersion = new Version(UI.Settings.WhatsNewVersion);

            if (!UI.Settings.ShowWhatsNew || Version <= whatNewVersion)
                return;

            var messages = GetWhatsNewMessages(whatNewVersion);
            if (!messages.Any())
                return;

            var messageBox = MessageBoxBase.ShowModal<WhatsNewMessageBox>();
            messageBox.CaprionText = string.Format(Localize.Mod_WhatsNewCaption, StaticName);
            messageBox.OnButtonClick = Confirm;
            messageBox.Init(messages);

            bool Confirm()
            {
                UI.Settings.WhatsNewVersion.value = Version.ToString();
                return true;
            }
        }
        private Dictionary<Version, string> GetWhatsNewMessages(Version whatNewVersion)
        {
            var messages = new Dictionary<Version, string>(Versions.Count);
#if DEBUG
            messages[Version] = Localize.Mod_WhatsNewMessageBeta;
#endif
            foreach (var version in Versions)
            {
                if (Version < version)
                    continue;

                if (version <= whatNewVersion)
                    break;

                if (UI.Settings.ShowOnlyMajor && !version.IsMinor())
                    continue;

                if (GetWhatsNew(version) is string message && !string.IsNullOrEmpty(message))
                    messages[version] = message;
            }

            return messages;
        }
        private string GetWhatsNew(Version version) => Localize.ResourceManager.GetString($"Mod_WhatsNewMessage{version.ToString().Replace('.', '_')}", Localize.Culture);
    }
}
