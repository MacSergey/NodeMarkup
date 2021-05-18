using ModsCommon;
using ModsCommon.UI;

namespace NodeMarkup.UI
{
    public static class NodeMarkupMessageBox
    {
        public static string CantUndone => Localize.MessageBox_CantUndone;
        public static string ItWillReplace => Localize.MessageBox_ItWillReplace;
    }
    public class ErrorLoadedMessageBox : TwoButtonMessageBox
    {
        public ErrorLoadedMessageBox()
        {
            CaptionText = SingletonMod<Mod>.Instance.NameRaw;
            Button1Text = CommonLocalize.MessageBox_OK;
            Button2Text = CommonLocalize.Mod_Support;
            OnButton2Click = Mod.OpenTroubleshooting;
        }
    }
}
