using ICities;
using ModsCommon;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Tools;

namespace NodeMarkup
{
    public class LoadingExtension : LoadingExtensionBase
    {
        public override void OnLevelLoaded(LoadMode mode)
        {
            SingletonMod<Mod>.Instance.Logger.Debug($"On level loaded");
            switch (mode)
            {
                case LoadMode.NewGame:
                case LoadMode.LoadGame:
                case LoadMode.NewGameFromScenario:
                case LoadMode.NewAsset:
                case LoadMode.LoadAsset:
                case LoadMode.NewMap:
                case LoadMode.LoadMap:
                    TemplateManager.Reload();

                    SingletonMod<Mod>.Instance.ShowWhatsNew();
                    SingletonMod<Mod>.Instance.ShowBetaWarning();
                    SingletonMod<Mod>.Instance.ShowLoadError();
                    break;
            }
        }
    }
}
