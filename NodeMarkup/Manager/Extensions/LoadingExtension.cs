using ICities;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.UI;

namespace NodeMarkup
{
    public class LoadingExtension : BaseLoadingExtension<Mod>
    {
        protected override void OnLoad()
        {
            TemplateManager.Reload();

            if (MarkupManager.HasErrors)
            {
                var messageBox = MessageBox.Show<ErrorSupportMessageBox>();
                messageBox.Init<Mod>();
                messageBox.MessageText = MarkupManager.Errors > 0 ? string.Format(Localize.Mod_LoadFailed, MarkupManager.Errors) : Localize.Mod_LoadFailedAll;
            }

            base.OnLoad();
        }
        protected override void OnUnload()
        {
            base.OnUnload();
            MarkupManager.Destroy();
        }
    }
}
