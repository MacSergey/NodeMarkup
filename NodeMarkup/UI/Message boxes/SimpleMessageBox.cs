using ModsCommon.UI;

namespace NodeMarkup.UI
{
    public static class NodeMarkupMessageBox
    {
        public static string Yes => Localize.MessageBox_Yes;
        public static string No => Localize.MessageBox_No;
        public static string Ok => Localize.MessageBox_OK;
        public static string Cancel => Localize.MessageBox_Cancel;
        public static string CantUndone => Localize.MessageBox_CantUndone;
    }
    public class OkMessageBox : OneButtonMessageBox
    {
        public OkMessageBox()
        {
            ButtonText = NodeMarkupMessageBox.Ok;
        }
    }
    public class YesNoMessageBox : TwoButtonMessageBox
    {
        public YesNoMessageBox()
        {
            Button1Text = NodeMarkupMessageBox.Yes;
            Button2Text = NodeMarkupMessageBox.No;
        }
    }

    public class ErrorLoadedMessageBox : TwoButtonMessageBox
    {
        public ErrorLoadedMessageBox()
        {
            CaptionText = Mod.ShortName;
            Button1Text = NodeMarkupMessageBox.Ok;
            Button2Text = NodeMarkup.Localize.Mod_Support;
            OnButton2Click = Mod.OpenTroubleshooting;
        }
    }
}
