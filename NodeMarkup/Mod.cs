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

namespace NodeMarkup
{
    public class Mod : LoadingExtensionBase, IUserMod
    {
        public static string StaticName { get; } = "Intersection Marking Tool";

        public static string Version => Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true).OfType<AssemblyFileVersionAttribute>().FirstOrDefault() is AssemblyFileVersionAttribute versionAttribute ? versionAttribute.Version : string.Empty;

        public static List<string> Versions { get; } = new List<string>
        {
            "1.2.1",
            "1.2",
            "1.1",
            "1.0"
        };

#if DEBUG
        public string Name { get; } = $"{StaticName} {Version} [BETA]";
        public string Description => Localize.Mod_DescriptionBeta;
#else
        public string Name { get; } = $"{StaticName} {Version}";
        public string Description => Localize.Mod_Description;
#endif

        static CultureInfo Culture => new CultureInfo(SingletonLite<LocaleManager>.instance.language == "zh" ? "zh-cn" : SingletonLite<LocaleManager>.instance.language);


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

        private void ShowWhatsNew()
        {
            if (!UI.Settings.ShowWhatsNew || VersionComparer.Instance.Compare(Version, UI.Settings.WhatsNewVersion) <= 0)
                return;

            var messages = GetWhatsNewMessages();
            if (!messages.Any())
                return;

            var messageBox = MessageBoxBase.ShowModal<WhatsNewMessageBox>();
            messageBox.CaprionText = string.Format(Localize.Mod_WhatsNewCaption, Name);
            messageBox.OnButtonClick = Confirm;
            messageBox.Init(messages);

            bool Confirm()
            {
                //UI.Settings.WhatsNewVersion.value = Version;
                return true;
            }
        }
        private Dictionary<string, string> GetWhatsNewMessages()
        {
            var messages = new Dictionary<string, string>(Versions.Count);

            foreach (var version in Versions)
            {
                if (VersionComparer.Instance.Compare(version, UI.Settings.WhatsNewVersion) <= 0)
                    break;

                if (UI.Settings.ShowOnlyImportantWhatsNew && !IsImportantVersion(version))
                    continue;

                if (GetWhatsNew(version) is string message && !string.IsNullOrEmpty(message))
                    messages[version] = message;
            }

            return messages;
        }
        private string GetWhatsNew(string version) => Localize.ResourceManager.GetString($"Mod_WhatsNewMessage{version.Replace('.', '_')}", Localize.Culture);
        private bool IsImportantVersion(string version) => version.Split('.').Length <= 2;
    }
    public class VersionComparer : Comparer<string>
    {
        public static VersionComparer Instance { get; } = new VersionComparer();

        public override int Compare(string x, string y)
        {
            var xVer = Parse(x);
            var yVer = Parse(y);

            for (var i = 0; i < Math.Max(xVer.Length, yVer.Length); i += 1)
            {
                var xVerPart = i < xVer.Length ? xVer[i] : 0;
                var yVerPart = i < yVer.Length ? yVer[i] : 0;
                if (xVerPart != yVerPart)
                    return xVerPart - yVerPart;
            }

            return 0;
        }

        private int[] Parse(string versionString)
        {
            var parts = new List<int>();
            foreach (var versionPart in versionString.Split('.'))
            {
                if (!int.TryParse(versionPart, out int part))
                    return new int[0];
                else
                    parts.Add(part);
            }
            return parts.ToArray();
        }
    }
}
