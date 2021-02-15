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
using ModsCommon;
using ModsCommon.Utilities;
using ModsCommon.UI;

namespace NodeMarkup
{
    public class Mod : BasePatcherMod<Mod, Patcher>
    {
        public static string StableURL { get; } = "https://steamcommunity.com/sharedfiles/filedetails/?id=2140418403";
        public static string BetaURL { get; } = "https://steamcommunity.com/sharedfiles/filedetails/?id=2159934925";
        public static string DiscordURL { get; } = "https://discord.gg/QRYq8m2";
        public static string ReportBugUrl { get; } = "https://github.com/MacSergey/NodeMarkup/issues/new?assignees=&labels=NEW+ISSUE&template=bug_report.md";
        public static string WikiUrl { get; } = "https://github.com/MacSergey/NodeMarkup/wiki";
        public static string TroubleshootingUrl { get; } = "https://github.com/MacSergey/NodeMarkup/wiki/Troubleshooting";

        protected override Version ModVersion => Assembly.GetExecutingAssembly().GetName().Version;
        public override string Id => nameof(NodeMarkup);
        protected override List<Version> ModVersions { get; } = new List<Version>
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

        protected override string ModName => "Intersection Marking Tool";
        protected override string ModDescription => !ModIsBeta ? Localize.Mod_Description : Localize.Mod_DescriptionBeta;
        public override string WorkshopUrl => StableURL;
        protected override string ModLocale => Settings.Locale.value;

#if DEBUG
        protected override bool ModIsBeta => true;
#else
        protected override bool ModIsBeta => false;
#endif
        public override void OnDisabled()
        {
            base.OnDisabled();
            NodeMarkupTool.Remove();
        }
        protected override Patcher CreatePatcher() => new Patcher(this);

        protected override void GetSettings(UIHelperBase helper) => Settings.OnSettingsUI(helper);

        public override void LocaleChanged()
        {
            Localize.Culture = Culture;
            Logger.Debug($"Current cultute - {Localize.Culture?.Name ?? "null"}");
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

        public override void OnLoadedError()
        {
            var messageBox = MessageBoxBase.ShowModal<ErrorLoadedMessageBox>();
            messageBox.MessageText = Localize.Mod_LoaledWithErrors;
        }
    }
}
