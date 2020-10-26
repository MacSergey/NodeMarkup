using ICities;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using NodeMarkup.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup
{
    public class LoadingExtension : LoadingExtensionBase
    {
        public override void OnLevelLoaded(LoadMode mode)
        {
            Logger.LogDebug($"{nameof(Mod)}.{nameof(OnLevelLoaded)}");
            switch (mode)
            {
                case LoadMode.NewGame:
                case LoadMode.LoadGame:
                case LoadMode.NewGameFromScenario:
                case LoadMode.NewAsset:
                case LoadMode.LoadAsset:
                case LoadMode.NewMap:
                case LoadMode.LoadMap:
                    NodeMarkupTool.Create();
                    //RenderHelper.Init();

                    //EarlyAccess.CheckAccess();
                    ShowWhatsNew();
                    ShowBetaWarning();
                    ShowLoadError();
                    break;
            }
        }

        public override void OnLevelUnloading()
        {
            Logger.LogDebug($"{nameof(Mod)}.{nameof(OnLevelUnloading)}");
            NodeMarkupTool.Remove();
        }

        private void ShowLoadError()
        {
            if (MarkupManager.LoadErrors != 0)
            {
                var messageBox = MessageBoxBase.ShowModal<TwoButtonMessageBox>();
                messageBox.CaprionText = Mod.StaticName;
                messageBox.MessageText = string.Format(Localize.Mod_LoadFailed, MarkupManager.LoadErrors);
                messageBox.Button1Text = Localize.MessageBox_OK;
                messageBox.Button2Text = Localize.Mod_Support;
                messageBox.OnButton2Click = Mod.OpenTroubleshooting;
            }
        }
        private void ShowBetaWarning()
        {
            if (!Mod.IsBeta)
                Settings.BetaWarning.value = true;
            else if (Settings.BetaWarning.value)
            {
                var messageBox = MessageBoxBase.ShowModal<TwoButtonMessageBox>();
                messageBox.CaprionText = Localize.Mod_BetaWarningCaption;
                messageBox.MessageText = string.Format(Localize.Mod_BetaWarningMessage, Mod.StaticName);
                messageBox.Button1Text = Localize.Mod_BetaWarningAgree;
                messageBox.Button2Text = Localize.Mod_BetaWarningGetStable;
                messageBox.OnButton1Click = AgreeClick;
                messageBox.OnButton2Click = Mod.GetStable;

                static bool AgreeClick()
                {
                    Settings.BetaWarning.value = false;
                    return true;
                }
            }
        }
        private void ShowWhatsNew()
        {
            var whatNewVersion = new Version(Settings.WhatsNewVersion);

            if (!Settings.ShowWhatsNew || Mod.Version <= whatNewVersion)
                return;

            var messages = GetWhatsNewMessages(whatNewVersion);
            if (!messages.Any())
                return;

            var messageBox = !Mod.IsBeta ? MessageBoxBase.ShowModal<WhatsNewMessageBox>() : MessageBoxBase.ShowModal<BetaWhatsNewMessageBox>();
            messageBox.CaprionText = string.Format(Localize.Mod_WhatsNewCaption, Mod.StaticName);
            messageBox.OnButtonClick = Confirm;
            messageBox.Init(messages);

            static bool Confirm()
            {
                Settings.WhatsNewVersion.value = Mod.Version.ToString();
                return true;
            }
        }
        private Dictionary<Version, string> GetWhatsNewMessages(Version whatNewVersion)
        {
            var messages = new Dictionary<Version, string>(Mod.Versions.Count);
#if DEBUG
            messages[Mod.Version] = Localize.Mod_WhatsNewMessageBeta;
#endif
            foreach (var version in Mod.Versions)
            {
                if (Mod.Version < version)
                    continue;

                if (version <= whatNewVersion)
                    break;

                if (Settings.ShowOnlyMajor && !version.IsMinor())
                    continue;

                if (GetWhatsNew(version) is string message && !string.IsNullOrEmpty(message))
                    messages[version] = message;
            }

            return messages;
        }
        private string GetWhatsNew(Version version) => Localize.ResourceManager.GetString($"Mod_WhatsNewMessage{version.ToString().Replace('.', '_')}", Localize.Culture);
    }
}
