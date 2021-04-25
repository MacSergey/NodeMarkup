using ModsCommon;
using ModsCommon.UI;

namespace NodeMarkup.UI
{
    public static class NodeMarkupMessageBox
    {
        public static string CantUndone => Localize.MessageBox_CantUndone;
        public static string ItWillReplace => Localize.MessageBox_ItWillReplace;
    }
    public class OkMessageBox : OneButtonMessageBox
    {
        public OkMessageBox()
        {
            ButtonText = ModLocalize<Mod>.Ok;
        }
    }
    public class YesNoMessageBox : TwoButtonMessageBox
    {
        public YesNoMessageBox()
        {
            Button1Text = ModLocalize<Mod>.Yes;
            Button2Text = ModLocalize<Mod>.No;
        }
    }

    public class ErrorLoadedMessageBox : TwoButtonMessageBox
    {
        public ErrorLoadedMessageBox()
        {
            CaptionText = SingletonMod<Mod>.Instance.Name;
            Button1Text = ModLocalize<Mod>.Ok;
            Button2Text = NodeMarkup.Localize.Mod_Support;
            OnButton2Click = Mod.OpenTroubleshooting;
        }
    }
}
