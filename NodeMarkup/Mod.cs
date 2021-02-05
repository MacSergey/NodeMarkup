using ICities;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NodeMarkup.UI;
using System.Globalization;
using ColossalFramework.Globalization;
using ColossalFramework;
using ColossalFramework.UI;
using ColossalFramework.PlatformServices;
using NodeMarkup.Utils;
using NodeMarkup.Tools;
using UnityEngine.SceneManagement;

namespace NodeMarkup
{
    public class Mod : IUserMod
    {
        public static string StableURL { get; } = "https://steamcommunity.com/sharedfiles/filedetails/?id=2140418403";
        public static string BetaURL { get; } = "https://steamcommunity.com/sharedfiles/filedetails/?id=2159934925";
        public static string DiscordURL { get; } = "https://discord.gg/QRYq8m2";
        public static string ReportBugUrl { get; } = "https://github.com/MacSergey/NodeMarkup/issues/new?assignees=&labels=NEW+ISSUE&template=bug_report.md";
        public static string WikiUrl { get; } = "https://github.com/MacSergey/NodeMarkup/wiki";
        public static string TroubleshootingUrl { get; } = "https://github.com/MacSergey/NodeMarkup/wiki/Troubleshooting";

        public static bool LoadedSuccess { get; set; } = true;

        public static string StaticName { get; } = "Intersection Marking Tool";
#if DEBUG
        public static string StaticFullName => $"{StaticName} {Version.GetString()} [BETA]";
#else
        public static string StaticFullName => $"{StaticName} {Version.GetString()}";
#endif

        public static Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public static Version VersionBuild => Version.Build();

        public static List<Version> Versions { get; } = new List<Version>
        {
            new Version("1.5.1"),
            new Version("1.5"),
            new Version("1.4.1"),
            new Version("1.4"),
            new Version("1.3"),
            new Version("1.2.1"),
            new Version("1.2"),
            new Version("1.1"),
            new Version("1.0")
        };

        public string Name => StaticFullName;
#if DEBUG
        public static bool IsBeta => true;
        public string Description => Localize.Mod_DescriptionBeta;
#else
        public static bool IsBeta => false;
        public string Description => Localize.Mod_Description;
#endif

        public static bool InGame => SceneManager.GetActiveScene().name is string scene && scene != "MainMenu" && scene != "IntroScreen";
        static CultureInfo Culture
        {
            get
            {
                var locale = string.IsNullOrEmpty(Settings.Locale.value) ? SingletonLite<LocaleManager>.instance.language : Settings.Locale.value;
                if (locale == "zh")
                    locale = "zh-cn";

                return new CultureInfo(locale);
            }
        }
        public void OnEnabled()
        {
            LoadedSuccess = true;

            LoadingManager.instance.m_introLoaded += CheckLoadError;
            Logger.LogDebug($"Version {Version}");
            Logger.LogDebug("Enabled");

            try
            {
                Patcher.Patch();
            }
            catch (Exception error)
            {
                Logger.LogError("Patch failed", error);
                LoadedSuccess = false;
            }

            CheckLoadError();
        }
        public void OnDisabled()
        {
            LoadingManager.instance.m_introLoaded -= CheckLoadError;
            Logger.LogDebug("Disabled");

            try
            {
                Patcher.Unpatch();
            }
            catch (Exception error)
            {
                Logger.LogError("Unpatch failed", error);
            }

            NodeMarkupTool.Remove();

            LocaleManager.eventLocaleChanged -= LocaleChanged;

            LoadedSuccess = true;
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            LocaleManager.eventLocaleChanged -= LocaleChanged;
            LocaleChanged();
            LocaleManager.eventLocaleChanged += LocaleChanged;

            Logger.LogDebug("Load SettingsUI");
            Settings.OnSettingsUI(helper);
        }

        public static void LocaleChanged()
        {
            Localize.Culture = Culture;
            Logger.LogDebug($"Current cultute - {Localize.Culture?.Name ?? "null"}");
        }

        public static bool OpenTroubleshooting()
        {
            Utilities.OpenUrl(TroubleshootingUrl);
            return true;
        }
        public static bool GetStable()
        {
            Utilities.OpenUrl(StableURL);
            return true;
        }

        public static void CheckLoadError()
        {
            if (!InGame && !LoadedSuccess)
            {
                var messageBox = MessageBoxBase.ShowModal<ErrorLoadedMessageBox>();
                messageBox.MessageText = Localize.Mod_LoaledWithErrors;
            }
        }
    }
}
