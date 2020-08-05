using ColossalFramework.PlatformServices;
using NodeMarkup.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace NodeMarkup
{
    public static class EarlyAccess
    {
        private static string URL { get; } = "https://discord.gg/SHwDZY";
        public static string Id { get; } = Crypt.GetHash($"{PlatformService.userID}{Mod.VersionMajor}");

        public static bool Allowed { get; private set; }

        public static bool CheckSign(string sign)
        {
            Allowed = Crypt.Verify(Id, sign);
            return Allowed;
        }

        public static bool CheckAccess(string function)
        {
            if (Allowed)
                return true;
            else
            {
                var messageBox = MessageBoxBase.ShowModal<TwoButtonMessageBox>();
                messageBox.CaprionText = "This function is unavailable";
                messageBox.MessageText = $"Function «{function}» is currently only available to those who have registered for early access.";
                messageBox.Button1Text = "OK";
                messageBox.OnButton1Click = () => true;
                messageBox.Button2Text = "Get early access";
                messageBox.OnButton2Click = GetAccess;

                return false;
            }
        }
        public static bool GetAccess()
        {
            PlatformService.ActivateGameOverlayToWebPage(URL);
            return true;
        }
    }
}
