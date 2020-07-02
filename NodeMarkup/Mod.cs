using ICities;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NodeMarkup
{
    public class Mod : LoadingExtensionBase, IUserMod
    {
        public static string Version => Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true).OfType<AssemblyFileVersionAttribute>().FirstOrDefault() is AssemblyFileVersionAttribute versionAttribute ? versionAttribute.Version : string.Empty;
        public string Name { get; } = $"{nameof(NodeMarkup)} {Version} [ALPHA]";
        public string Description => "Marking on nodes";

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
                Manager.MarkupManager.Init();
            }
        }

        public override void OnLevelUnloading()
        {
            Logger.LogDebug($"{nameof(Mod)}.{nameof(OnLevelUnloading)}");
            NodeMarkupTool.Remove();
        }

        static bool CheckGameMode(AppMode mode)
        {
            try
            {
                return CurrentMode == mode;
            }
            catch
            {
                return false;
            }

        }
    }
}
