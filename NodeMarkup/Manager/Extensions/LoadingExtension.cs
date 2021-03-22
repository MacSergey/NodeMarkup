using ICities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using NodeMarkup.UI;
using NodeMarkup.Utilities;
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
                    NodeMarkupTool.Create();
                    TemplateManager.Reload();

                    ShowWhatsNew();
                    ShowBetaWarning();
                    ShowLoadError();
                    break;
            }
        }

        public override void OnLevelUnloading()
        {
            Mod.Logger.Debug($"On level unloading");
            NodeMarkupTool.Remove();
        }

        private void ShowLoadError()
        {
            if (MarkupManager.HasErrors)
            {
                var messageBox = MessageBoxBase.ShowModal<ErrorLoadedMessageBox>();
                messageBox.MessageText = MarkupManager.Errors > 0 ? string.Format(Localize.Mod_LoadFailed, MarkupManager.Errors) : Localize.Mod_LoadFailedAll;
            }
        }
        private void ShowBetaWarning()
        {
            if (!Mod.IsBeta)
                Settings.BetaWarning.value = true;
            else if (Settings.BetaWarning.value)
            {
                var messageBox = MessageBoxBase.ShowModal<TwoButtonMessageBox>();
                messageBox.CaptionText = Localize.Mod_BetaWarningCaption;
                messageBox.MessageText = string.Format(Localize.Mod_BetaWarningMessage, Mod.ShortName);
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

            if(!Mod.IsBeta)
            {
                var messageBox = MessageBoxBase.ShowModal<WhatsNewMessageBox>();
                messageBox.CaptionText = string.Format(Localize.Mod_WhatsNewCaption, Mod.ShortName);
                messageBox.OnButtonClick = Confirm;
                messageBox.OkText = NodeMarkupMessageBox.Ok;
                messageBox.Init(messages, GetVersionString);
            }
            else
            {
                var messageBox = MessageBoxBase.ShowModal<BetaWhatsNewMessageBox>();
                messageBox.CaptionText = string.Format(Localize.Mod_WhatsNewCaption, Mod.ShortName);
                messageBox.OnButtonClick = Confirm;
                messageBox.OnGetStableClick = GetStable;
                messageBox.OkText = NodeMarkupMessageBox.Ok;
                messageBox.GetStableText = Localize.Mod_BetaWarningGetStable;
                messageBox.Init(messages, string.Format(Localize.Mod_BetaWarningMessage, Mod.ShortName), GetVersionString);
            }

            static bool Confirm()
            {
                Settings.WhatsNewVersion.value = Mod.Version.ToString();
                return true;
            }
            static bool GetStable()
            {
                Mod.GetStable();
                return true;
            }
        }
        private Dictionary<Version, string> GetWhatsNewMessages(Version whatNewVersion)
        {
            var messages = new Dictionary<Version, string>(Mod.Versions.Count);
#if BETA
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
        private string GetVersionString(Version version) => string.Format(Localize.Mod_WhatsNewVersion, version == Mod.Version ? Mod.VersionString : version.ToString());
        private string GetWhatsNew(Version version) => Localize.ResourceManager.GetString($"Mod_WhatsNewMessage{version.ToString().Replace('.', '_')}", Localize.Culture);
    }
}
