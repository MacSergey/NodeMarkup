using ColossalFramework;
using ColossalFramework.PlatformServices;
using ColossalFramework.Threading;
using ColossalFramework.UI;
using NodeMarkup.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace NodeMarkup
{
    public static class EarlyAccess
    {
        public static SavedString EarlyAccessVersion { get; } = new SavedString(nameof(EarlyAccessVersion), UI.Settings.SettingsFile, string.Empty, true);
        public static SavedString PatreonToken { get; } = new SavedString(nameof(PatreonToken), UI.Settings.SettingsFile, string.Empty, true);
#if DEBUG
        public static string Version => $"{Mod.VersionMajor} [BETA]";
#else
        private static string Version => Mod.VersionMajor;
#endif
        private static string GetEarlyAccessURL { get; } = "https://discord.gg/QRYq8m2";
        public static string Id { get; } = Crypt.GetHash($"{PlatformService.userID}{Version}");

        public static bool Status { get; private set; }

        public static bool CheckAccess()
        {
            if (EarlyAccessVersion.value == Version)
                Status = true;
            else if (!string.IsNullOrEmpty(PatreonToken.value))
            {
                if (Patreon.RefreshToken(PatreonToken.value, out string accessToken, out string refreshToken) && Patreon.IsMember(accessToken, out _))
                {
                    SaveAccess(refreshToken, false);
                    Status = true;
                }
            }
            Logger.LogDebug($"Early access {(Status ? "allowed" : "forbidden")}");
            return Status;
        }
        public static void Reset() => Status = false;
        public static bool CheckSign(string sign) => Crypt.Verify(Id, sign);

        public static bool CheckFunctionAccess(string function, bool alert = true)
        {

            if (Status)
                return true;
            else if (alert)
                ShowNoEarlyAccess(Localize.EarlyAccess_FunctionUnavailableCaption, string.Format(Localize.EarlyAccess_FunctionUnavailableMessage, function));
#if !DEBUG
            return false;
#else
            return true;
#endif
        }
        public static void ShowNoEarlyAccess(string caption, string message)
        {
            var messageBox = MessageBoxBase.ShowModal<TwoButtonMessageBox>();
            messageBox.CaprionText = caption;
            messageBox.MessageText = message;
            messageBox.Button1Text = Localize.MessageBox_OK;
            messageBox.OnButton1Click = () => true;
            messageBox.Button2Text = Localize.EarlyAccess_GetButton;
            messageBox.OnButton2Click = GetAccess;
        }
        public static void ShowEarlyAccess(string caption)
        {
            var messageBox = MessageBoxBase.ShowModal<OkMessageBox>();
            messageBox.CaprionText = caption;
            messageBox.MessageText = Localize.EarlyAccess_ThanksMessage;
        }

        public static bool GetAccess()
        {
            Utilities.OpenUrl(GetEarlyAccessURL);
            return true;
        }
        public static void SaveAccess(string token = null, bool check = true)
        {
            EarlyAccessVersion.value = Version;
            PatreonToken.value = token ?? string.Empty;
            if (check)
                CheckAccess();
        }
    }

    public class EarlyAccessPanel : UIPanel
    {
        private bool _linkInProcess;

        private UILabel Status { get; set; }
        private UIButton GetAccessButton { get; set; }
        private UIButton PatreonButton { get; set; }
        private UIButton ActivateButton { get; set; }
        private OneButtonMessageBox PatreonProcess { get; set; }
        private bool LinkInProcess
        {
            get => _linkInProcess;
            set
            {
                if (value == _linkInProcess)
                    return;

                _linkInProcess = value;

                if (_linkInProcess)
                    ShowProcess();
                else
                    HideProcess();
            }
        }

        private Action MainThreadAction { get; set; }
        private string State { get; set; }

        private static HttpListener Listener { get; set; }

        public EarlyAccessPanel()
        {
            autoLayout = true;
            autoLayoutDirection = LayoutDirection.Vertical;
            autoFitChildrenHorizontally = true;
            autoFitChildrenVertically = true;
            autoLayoutPadding = new RectOffset(0, 0, 5, 5);

            var helper = new UIHelper(this);
            AddStatus(helper);
            AddGetAccess(helper);
            AddLinkPatreon(helper);
            AddActivateKey(helper);
            //AddCheckAccessKey(helper);
            //AddResetAccessKey(helper);
            //AddRefreshKey(helper);

            Refresh();
        }
        private void AddStatus(UIHelper helper)
        {
            Status = AddUIComponent<UILabel>();
            Status.textScale = 1.1f;
        }
        private void AddGetAccess(UIHelper helper)
        {
            GetAccessButton = helper.AddButton(NodeMarkup.Localize.EarlyAccess_GetButton, Click) as UIButton;
            GetAccessButton.autoSize = false;
            GetAccessButton.textHorizontalAlignment = UIHorizontalAlignment.Center;
            GetAccessButton.width = 400;

            void Click() => EarlyAccess.GetAccess();
        }
        private void AddLinkPatreon(UIHelper helper)
        {
            PatreonButton = helper.AddButton(NodeMarkup.Localize.EarlyAccess_LinkPatreonButton, Click) as UIButton;
            PatreonButton.autoSize = false;
            PatreonButton.textHorizontalAlignment = UIHorizontalAlignment.Center;
            PatreonButton.width = 400;
            PatreonButton.hoveredTextColor = PatreonButton.pressedTextColor = Color.red;

            void Click()
            {
                LinkInProcess = true;
                State = Guid.NewGuid().ToString();
                Listen();

                var url = Patreon.GetOAuthURLWithState(State);
                Utilities.OpenUrl(url);
            }
        }
        private void AddActivateKey(UIHelper helper)
        {
            ActivateButton = helper.AddButton(NodeMarkup.Localize.EarlyAccess_ActivateButton, Click) as UIButton;
            ActivateButton.autoSize = false;
            ActivateButton.textHorizontalAlignment = UIHorizontalAlignment.Center;
            ActivateButton.width = 400;

            void Click()
            {
                var messageBox = MessageBoxBase.ShowModal<EarlyAccessMessageBox>();
                messageBox.OnButton1Click = Activate;

                bool Activate()
                {
                    if (EarlyAccess.CheckSign(messageBox.Key))
                    {
                        EarlyAccess.SaveAccess();
                        EarlyAccess.ShowEarlyAccess(NodeMarkup.Localize.EarlyAccess_ActivationSuccess);
                    }
                    else
                        EarlyAccess.ShowNoEarlyAccess(NodeMarkup.Localize.EarlyAccess_ActivationFailed, NodeMarkup.Localize.EarlyAccess_KeyNotValid);

                    Refresh();
                    return true;
                }
            }
        }
        private void AddCheckAccessKey(UIHelper helper)
        {
            var button = helper.AddButton("Check Access", Click) as UIButton;
            button.autoSize = false;
            button.textHorizontalAlignment = UIHorizontalAlignment.Center;
            button.width = 300;
            void Click() => EarlyAccess.CheckAccess();
        }
        private void AddResetAccessKey(UIHelper helper)
        {
            var button = helper.AddButton("Reset Access", EarlyAccess.Reset) as UIButton;
            button.autoSize = false;
            button.textHorizontalAlignment = UIHorizontalAlignment.Center;
            button.width = 300;
        }
        private void AddRefreshKey(UIHelper helper)
        {
            var button = helper.AddButton("Refresh", Refresh) as UIButton;
            button.autoSize = false;
            button.textHorizontalAlignment = UIHorizontalAlignment.Center;
            button.width = 300;
        }

        private void ShowProcess()
        {
            PatreonProcess = MessageBoxBase.ShowModal<OneButtonMessageBox>();
            PatreonProcess.ButtonText = NodeMarkup.Localize.EarlyAccess_ProcessCancel;
            PatreonProcess.CaprionText = NodeMarkup.Localize.EarlyAccess_LinkPatreonCaption;
            PatreonProcess.MessageText = NodeMarkup.Localize.EarlyAccess_LinkInProcess;
            PatreonProcess.OnButtonClick = Cancel;

            bool Cancel()
            {
                LinkInProcess = false;
                AbortListen();
                return false;
            }
        }
        private void HideProcess()
        {
            if (PatreonProcess != null)
                MessageBoxBase.HideModal(PatreonProcess);
        }

        private void Refresh()
        {
            Status.text = string.Format(NodeMarkup.Localize.EarlyAccess_Status, EarlyAccess.Status ? NodeMarkup.Localize.EarlyAccess_StatusActivated : NodeMarkup.Localize.EarlyAccess_StatusNotActivated);
            GetAccessButton.isVisible = PatreonButton.isVisible = ActivateButton.isVisible = !EarlyAccess.Status;
        }

        private void ProcessData(string code)
        {
            if (!LinkInProcess)
                return;
            else if (string.IsNullOrEmpty(code))
            {
                if (LinkInProcess)
                    CodeIsEmpty();
            }
            else if (!LinkInProcess)
                return;
            else if (!Patreon.GetToken(code, out string accessToken, out string refreshToken))
            {
                if (LinkInProcess)
                    NotGetToken();
            }
            else if (!LinkInProcess)
                return;
            else if (!Patreon.IsMember(accessToken, out _))
            {
                if (LinkInProcess)
                    NotIsMember();
            }
            else
            {
                if (LinkInProcess)
                    IsMember(refreshToken);
            }
        }
        private void CodeIsEmpty()
        {
            LinkInProcess = false;
            Logger.LogDebug("Patreon access denied");
            MainThreadAction = () =>
            {
                var messageBox = MessageBoxBase.ShowModal<OkMessageBox>();
                messageBox.CaprionText = NodeMarkup.Localize.EarlyAccess_LinkPatreonCaption;
                messageBox.MessageText = NodeMarkup.Localize.EarlyAccess_PatreonCantLinked;
            };
        }
        private void NotGetToken()
        {
            LinkInProcess = false;
            Logger.LogDebug("Patreon get token error");
            MainThreadAction = () =>
            {
                var messageBox = MessageBoxBase.ShowModal<OkMessageBox>();
                messageBox.CaprionText = NodeMarkup.Localize.EarlyAccess_LinkPatreonCaption;
                messageBox.MessageText = NodeMarkup.Localize.EarlyAccess_PatreonLinkedError;
            };
        }
        private void NotIsMember()
        {
            LinkInProcess = false;
            Logger.LogDebug("Patreon no early access");
            MainThreadAction = () => EarlyAccess.ShowNoEarlyAccess(NodeMarkup.Localize.EarlyAccess_LinkPatreonCaption, NodeMarkup.Localize.EarlyAccess_PatreonLinkedNotMember);
        }
        private void IsMember(string refreshToken)
        {
            LinkInProcess = false;
            Logger.LogDebug("Patreon access success");
            EarlyAccess.SaveAccess(refreshToken);
            Refresh();
            MainThreadAction = () => EarlyAccess.ShowEarlyAccess(NodeMarkup.Localize.EarlyAccess_LinkPatreonCaption);
        }


        public void Listen()
        {
            if (Listener != null)
                AbortListen();

            Logger.LogDebug("Start Listener");
            Listener = new HttpListener();
            Listener.Prefixes.Add(Patreon.RedirectURL);
            Listener.Start();
            Listener.BeginGetContext(ListenResult, null);
        }
        private void ListenResult(IAsyncResult result)
        {
            try
            {
                Logger.LogDebug(nameof(ListenResult));
                if (Listener == null)
                    return;

                var context = Listener.EndGetContext(result);
                Listener.BeginGetContext(ListenResult, null);

                var request = context.Request;
                var code = request.QueryString.GetValues("code")?.FirstOrDefault();
                var state = request.QueryString.GetValues("state")?.FirstOrDefault();

                var response = context.Response;
                var responseString = $"<HTML><BODY>{NodeMarkup.Localize.EarlyAccess_PatreonRedirect}</BODY></HTML>";
                var buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                var output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();

                if (string.IsNullOrEmpty(state) || state == State)
                {
                    Listener.Close();
                    Listener = null;
                    ProcessData(code);
                }
            }
            catch (Exception error)
            {
                Logger.LogError(() => nameof(ListenResult), error);
            }
        }
        public void AbortListen()
        {
            Logger.LogDebug(nameof(AbortListen));
            if (Listener != null)
            {
                try { Listener.Abort(); }
                catch (Exception error)
                {
                    Logger.LogError(() => nameof(AbortListen), error);
                }
                Listener = null;
            }
        }

        public override void LateUpdate()
        {
            base.LateUpdate();
            if (MainThreadAction != null)
            {
                MainThreadAction.Invoke();
                MainThreadAction = null;
            }
        }
    }
}
