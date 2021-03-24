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
using NodeMarkup.Utilities;
using NodeMarkup.Tools;
using UnityEngine.SceneManagement;
using ModsCommon;
using ModsCommon.Utilities;
using ModsCommon.UI;

namespace NodeMarkup
{
    public class Mod : BasePatcherMod
    {
        public static string StableURL { get; } = "https://steamcommunity.com/sharedfiles/filedetails/?id=2140418403";
        public static string BetaURL { get; } = "https://steamcommunity.com/sharedfiles/filedetails/?id=2159934925";
        public static string DiscordURL { get; } = "https://discord.gg/NnwhuBKMqj";
        public static string ReportBugUrl { get; } = "https://github.com/MacSergey/NodeMarkup/issues/new?assignees=&labels=NEW+ISSUE&template=bug_report.md";
        public static string WikiUrl { get; } = "https://github.com/MacSergey/NodeMarkup/wiki";
        public static string TroubleshootingUrl { get; } = "https://github.com/MacSergey/NodeMarkup/wiki/Troubleshooting";

        protected override Version ModVersion => Assembly.GetExecutingAssembly().GetName().Version;
        protected override string ModId => nameof(NodeMarkup);
        protected override List<Version> ModVersions { get; } = new List<Version>
        {
            new Version("1.6"),
            new Version("1.5.3"),
            new Version("1.5.2"),
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

        protected override string ModName => "Intersection Marking Tool";
        protected override string ModDescription => !ModIsBeta ? Localize.Mod_Description : Localize.Mod_DescriptionBeta;
        public override string WorkshopUrl => StableURL;
        protected override string ModLocale => Settings.Locale.value;

#if BETA
        protected override bool ModIsBeta => true;
#else
        protected override bool ModIsBeta => false;
#endif
        public override void OnDisabled()
        {
            base.OnDisabled();
            NodeMarkupTool.Remove();
        }
        protected override BasePatcher CreatePatcher() => new Patcher(this);

        protected override void GetSettings(UIHelperBase helper) => Settings.OnSettingsUI(helper);

        public override void LocaleChanged()
        {
            Localize.Culture = Culture;
            Logger.Debug($"Current cultute - {Localize.Culture?.Name ?? "null"}");
        }

        public static bool OpenTroubleshooting()
        {
            Utilities.Utilities.OpenUrl(TroubleshootingUrl);
            return true;
        }
        public static bool GetStable()
        {
            Utilities.Utilities.OpenUrl(StableURL);
            return true;
        }

        public override void OnLoadedError()
        {
            var messageBox = MessageBoxBase.ShowModal<ErrorLoadedMessageBox>();
            messageBox.MessageText = Localize.Mod_LoaledWithErrors;
        }

        public static void ShowWhatsNew(Version fromVersion = null, bool forceShow = false)
        {
            fromVersion ??= new Version(Settings.WhatsNewVersion);

            if ((!Settings.ShowWhatsNew || Version <= fromVersion) && !forceShow)
                return;

            var messages = GetWhatsNewMessages(fromVersion);
            if (!messages.Any())
                return;

            if (!IsBeta)
            {
                var messageBox = MessageBoxBase.ShowModal<WhatsNewMessageBox>();
                messageBox.CaptionText = string.Format(Localize.Mod_WhatsNewCaption, ShortName);
                messageBox.OnButtonClick = Confirm;
                messageBox.OkText = NodeMarkupMessageBox.Ok;
                messageBox.Init(messages, GetVersionString);
            }
            else
            {
                var messageBox = MessageBoxBase.ShowModal<BetaWhatsNewMessageBox>();
                messageBox.CaptionText = string.Format(Localize.Mod_WhatsNewCaption, ShortName);
                if (!forceShow)
                    messageBox.OnButtonClick = Confirm;
                messageBox.OnGetStableClick = GetStable;
                messageBox.OkText = NodeMarkupMessageBox.Ok;
                messageBox.GetStableText = Localize.Mod_BetaWarningGetStable;
                messageBox.Init(messages, string.Format(Localize.Mod_BetaWarningMessage, ShortName), GetVersionString);
            }

            static bool Confirm()
            {
                Settings.WhatsNewVersion.value = Version.ToString();
                return true;
            }
            static bool GetStable()
            {
                Mod.GetStable();
                return true;
            }
        }
        private static Dictionary<Version, string> GetWhatsNewMessages(Version whatNewVersion)
        {
            var messages = new Dictionary<Version, string>(Versions.Count);
#if BETA
            messages[Version] = Localize.Mod_WhatsNewMessageBeta;
#endif
            foreach (var version in Versions)
            {
                if (Version < version)
                    continue;

                if (version <= whatNewVersion)
                    break;

                if (Settings.ShowOnlyMajor && !version.IsMinor())
                    continue;

                if (GetWhatsNew(version) is string message && !string.IsNullOrEmpty(message))
                    messages[version] = message;
            }

            return messages;
        }
        private static string GetVersionString(Version version) => string.Format(Localize.Mod_WhatsNewVersion, version == Version ? VersionString : version.ToString());
        private static string GetWhatsNew(Version version) => Localize.ResourceManager.GetString($"Mod_WhatsNewMessage{version.ToString().Replace('.', '_')}", Localize.Culture);

        public static void ShowBetaWarning()
        {
            if (!IsBeta)
                Settings.BetaWarning.value = true;
            else if (Settings.BetaWarning.value)
            {
                var messageBox = MessageBoxBase.ShowModal<TwoButtonMessageBox>();
                messageBox.CaptionText = Localize.Mod_BetaWarningCaption;
                messageBox.MessageText = string.Format(Localize.Mod_BetaWarningMessage, ShortName);
                messageBox.Button1Text = Localize.Mod_BetaWarningAgree;
                messageBox.Button2Text = Localize.Mod_BetaWarningGetStable;
                messageBox.OnButton1Click = AgreeClick;
                messageBox.OnButton2Click = GetStable;

                static bool AgreeClick()
                {
                    Settings.BetaWarning.value = false;
                    return true;
                }
            }
        }
        public static void ShowLoadError()
        {
            if (MarkupManager.HasErrors)
            {
                var messageBox = MessageBoxBase.ShowModal<ErrorLoadedMessageBox>();
                messageBox.MessageText = MarkupManager.Errors > 0 ? string.Format(Localize.Mod_LoadFailed, MarkupManager.Errors) : Localize.Mod_LoadFailedAll;
            }
        }
    }
}
