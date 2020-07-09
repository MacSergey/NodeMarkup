using ICities;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NodeMarkup.UI;

namespace NodeMarkup
{
    public class Mod : LoadingExtensionBase, IUserMod
    {
#if DEBUG
        public static string StaticName { get; } = "Intersection Marking Tool [BETA]";
#else
        public static string StaticName { get; } = "Intersection Marking Tool";
#endif
        public static string Version => Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true).OfType<AssemblyFileVersionAttribute>().FirstOrDefault() is AssemblyFileVersionAttribute versionAttribute ? versionAttribute.Version : string.Empty;
        public string Name { get; } = $"{StaticName} {Version}";
#if DEBUG
        public string Description => "This is BETA version, stability and absence of errors are not guaranteed";
#else
        public string Description => "Just do make markings at intersections";
#endif

        static AppMode CurrentMode => SimulationManager.instance.m_ManagersWrapper.loading.currentMode;

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

        public void OnSettingsUI(UIHelperBase helper) => UI.Settings.OnSettingsUI(helper);
    }
}
