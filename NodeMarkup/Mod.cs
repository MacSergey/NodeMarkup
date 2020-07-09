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

namespace NodeMarkup
{
    public class Mod : LoadingExtensionBase, IUserMod
    {
        public static string StaticName { get; } = "Intersection Marking Tool";

        public static string Version => Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true).OfType<AssemblyFileVersionAttribute>().FirstOrDefault() is AssemblyFileVersionAttribute versionAttribute ? versionAttribute.Version : string.Empty;

#if DEBUG
        public string Name { get; } = $"{StaticName} {Version} [BETA]";
        public string Description => Localize.Mod_DescriptionBeta;
#else
        public string Name { get; } = $"{StaticName} {Version}";
        public string Description => Localize.Mod_Description;
#endif

        static AppMode CurrentMode => SimulationManager.instance.m_ManagersWrapper.loading.currentMode;
        static CultureInfo Culture => new CultureInfo(SingletonLite<LocaleManager>.instance.language);

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
                NodeMarkupTool.Instance?.DisableTool();
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
            LocaleManager.eventLocaleChanged += LocaleChanged;
            LocaleChanged();

            Logger.LogDebug($"{nameof(Mod)}.{nameof(OnSettingsUI)}");
            UI.Settings.OnSettingsUI(helper);
        }

        private void LocaleChanged()
        {
            Logger.LogDebug($"{nameof(Mod)}.{nameof(LocaleChanged)}");
            Localize.Culture = Culture;
            Logger.LogDebug($"current cultute - {Localize.Culture?.Name ?? "null"}");
        }
    }
}
