using ICities;
using IMT.Manager;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;

namespace IMT
{
    public class LoadingExtension : BaseLoadingExtension<Mod>
    {
        protected override void OnLoad()
        {
            MarkingManager.UpdateAll();
            DataManager.Reload();

            if (MarkingManager.HasErrors)
            {
                var messageBox = MessageBox.Show<ErrorSupportMessageBox>();
                messageBox.Init<Mod>();
                messageBox.MessageText = MarkingManager.Errors > 0 ? string.Format(Localize.Mod_LoadFailed, MarkingManager.Errors) : Localize.Mod_LoadFailedAll;
            }

            base.OnLoad();
        }
        protected override void OnUnload()
        {
            base.OnUnload();
            MarkingManager.Destroy();
        }
        protected override void OnPreLoaded()
        {
            RenderHelper.LoadBundle();
            ThemeHelper.LoadThemes();
        }
        protected override void OnPreUnloaded()
        {
            RenderHelper.UnloadBundle();
            ThemeHelper.UnloadThemes();
        }
    }
}
