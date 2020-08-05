using ColossalFramework.PlatformServices;
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
        public static string Id { get; } = Crypt.GetHash($"{PlatformService.userID}{Mod.Versions.First()}");
        public static bool Allowed { get; private set; }

        public static bool CheckAccess(string sign)
        {
            Allowed = Crypt.Verify(Id, sign);
            return Allowed;
        }
    }
}
