using ICities;
using NodeMarkup.Manager;
using NodeMarkup.Tools;

namespace NodeMarkup
{
    public class LoadingExtension : LoadingExtensionBase
    {
        public override void OnLevelLoaded(LoadMode mode)
        {
            Mod.Logger.Debug($"On level loaded");
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

                    Mod.ShowWhatsNew();
                    Mod.ShowBetaWarning();
                    Mod.ShowLoadError();
                    break;
            }
        }
    }
}
