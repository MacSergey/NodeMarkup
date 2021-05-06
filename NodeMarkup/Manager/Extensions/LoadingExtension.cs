using ICities;
using ModsCommon;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Tools;

namespace NodeMarkup
{
    public class LoadingExtension : BaseLoadingExtension<Mod>
    {
        protected override void OnLoad()
        {
            TemplateManager.Reload();
            SingletonMod<Mod>.Instance.ShowLoadError();

            base.OnLoad();
        }
        protected override void OnUnload()
        {
            base.OnUnload();
            MarkupManager.Destroy();
        }
    }
}
