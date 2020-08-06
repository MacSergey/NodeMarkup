using ColossalFramework;
using ColossalFramework.PlatformServices;
using ColossalFramework.Threading;
using ColossalFramework.UI;
using NodeMarkup.UI;
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
        private static string URL { get; } = "https://discord.gg/SHwDZY";
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
                    SaveAccess(refreshToken);
                    Status = true;
                }
            }
            Logger.LogDebug($"Early access {(Status ? "allowed" : "forbidden")}");
            return Status;
        }

        public static bool CheckFunctionAccess(string function, bool alert = true)
        {
            if (Status)
                return true;
            else if (alert)
                ShowNoEarlyAccess("This function is unavailable", $"Function «{function}» is currently only available to those who have registered for early access. It will be available to everyone later.");
            return false;
        }
        public static void ShowNoEarlyAccess(string caption, string message)
        {
            var messageBox = MessageBoxBase.ShowModal<TwoButtonMessageBox>();
            messageBox.CaprionText = caption;
            messageBox.MessageText = message;
            messageBox.Button1Text = "OK";
            messageBox.OnButton1Click = () => true;
            messageBox.Button2Text = "Get early access";
            messageBox.OnButton2Click = GetAccess;
        }
        public static bool GetAccess()
        {
            PlatformService.ActivateGameOverlayToWebPage(URL);
            return true;
        }
        public static void SaveAccess(string token = null)
        {
            EarlyAccessVersion.value = Version;
            PatreonToken.value = token ?? string.Empty;
            CheckAccess();
        }
    }

    public class EarlyAccessPanel : UIPanel
    {
        private UILabel Status { get; set; }
        private UIButton PatreonButton { get; set; }
        private UIButton ActivateButton { get; set; }

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
            AddLinkPatreon(helper);
            AddActivateKey(helper);

            Refresh();
        }
        private void AddStatus(UIHelper helper)
        {
            Status = AddUIComponent<UILabel>();
            Status.textScale = 1.1f;
        }
        private void AddLinkPatreon(UIHelper helper)
        {
            var PatreonButton = helper.AddButton("Link Patreon account", Click) as UIButton;
            PatreonButton.autoSize = false;
            PatreonButton.textHorizontalAlignment = UIHorizontalAlignment.Center;
            PatreonButton.width = 300;
            PatreonButton.hoveredTextColor = PatreonButton.pressedTextColor = Color.red;
            //button.eventLostFocus += TestLostFocus;

            void Click()
            {
                State = Guid.NewGuid().ToString();
                Listen();
                Process.Start(Patreon.GetOAuthURLWithState(State));
            }
        }
        private void AddActivateKey(UIHelper helper)
        {
            var ActivateButton = helper.AddButton("Activate key", Click) as UIButton;
            ActivateButton.autoSize = false;
            ActivateButton.textHorizontalAlignment = UIHorizontalAlignment.Center;
            ActivateButton.width = 300;

            void Click()
            {
                EarlyAccess.CheckAccess();
            }
        }

        private void Refresh()
        {
            Status.text = $"Early access status: {(EarlyAccess.Status ? "Activated" : "Not activated")}";
        }

        private void ProcessData(string code, string state)
        {
            if (string.IsNullOrEmpty(code))
            {
                Logger.LogDebug("Patreon access denied");
                MainThreadAction = () =>
                {
                    var messageBox = MessageBoxBase.ShowModal<OkMessageBox>();
                    messageBox.CaprionText = "Link Patreon account";
                    messageBox.MessageText = "The Patreon account can't be linked because you denied access request";
                };
            }
            else if (!Patreon.GetToken(code, out string accessToken, out string refreshToken))
            {
                Logger.LogDebug("Patreon get token error");
                MainThreadAction = () =>
                {
                    var messageBox = MessageBoxBase.ShowModal<OkMessageBox>();
                    messageBox.CaprionText = "Link Patreon account";
                    messageBox.MessageText = "An error occurred when linking the account, try again";
                };
                return;
            }
            else if (!Patreon.IsMember(accessToken, out string id))
            {
                Logger.LogDebug("Patreon no early access");
                MainThreadAction = () =>
                {
                    EarlyAccess.ShowNoEarlyAccess("Link Patreon account", "Your account is linked, but you don't have early access");
                };
            }
            else
            {
                Logger.LogDebug("Patreon access success");
                EarlyAccess.SaveAccess(refreshToken);
                MainThreadAction = () =>
                {
                    var messageBox = MessageBoxBase.ShowModal<OkMessageBox>();
                    messageBox.CaprionText = "Link Patreon account";
                    messageBox.MessageText = "Thank you for your support, now you can enjoy all the features";
                };
            }
        }

        public void Listen()
        {
            if (Listener != null)
                Abort();

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
                var responseString = "<HTML><BODY>You can close this page and return to the game.</BODY></HTML>";
                var buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                var output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();

                if (string.IsNullOrEmpty(state) || state == State)
                {
                    Listener.Close();
                    Listener = null;
                    ProcessData(code, state);
                }
            }
            catch (Exception error)
            {
                Logger.LogError(() => nameof(ListenResult), error);
            }
        }
        public void Abort()
        {
            Logger.LogDebug(nameof(Abort));
            if (Listener != null)
            {
                try { Listener.Abort(); }
                catch (Exception error)
                {
                    Logger.LogError(() => nameof(Abort), error);
                }
                Listener = null;
            }
        }

        private void TestLostFocus(UIComponent component, UIFocusEventParameter eventParam)
        {
            Abort();
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
