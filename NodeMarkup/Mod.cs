using ICities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup
{
    public class Mod : LoadingExtensionBase, IUserMod
    {
        public string Name { get; } = nameof(NodeMarkup);
        public string Description => Name;

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
                NodeMarkupTool.Create();
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
