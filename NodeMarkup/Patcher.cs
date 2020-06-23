using CitiesHarmony.API;
using HarmonyLib;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup
{
    public static class Patcher
    {
        private static string HarmonyId { get; } = nameof(NodeMarkup);

        public static void Patch()
        {
            Logger.LogDebug($"{nameof(Patcher)}.{nameof(Patch)}");
            HarmonyHelper.DoOnHarmonyReady(() => Begin());
        }
        public static void Unpatch()
        {
            Logger.LogDebug($"{nameof(Patcher)}.{nameof(Unpatch)}");

            Logger.LogDebug($"Unpatch all");

            var harmony = new Harmony(HarmonyId);
            harmony.UnpatchAll(HarmonyId);

            Logger.LogDebug($"Unpatched");
        }

        private static void Begin()
        {
            Logger.LogDebug($"{nameof(Patcher)}.{nameof(Begin)}");


            Logger.LogDebug($"Patch NetNode.RenderInstance");
            var harmony = new Harmony(HarmonyId);

            var original = AccessTools.Method(typeof(NetNode), "RenderInstance", new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(NetInfo), typeof(int), typeof(NetNode.Flags), typeof(uint).MakeByRefType(), typeof(RenderManager.Instance).MakeByRefType() });
            var postfix = AccessTools.Method(typeof(NodeMarkupManager), nameof(NodeMarkupManager.NetNodeRenderPostfix));

            harmony.Patch(original, postfix: new HarmonyMethod(postfix));

            Logger.LogDebug($"Patched");
        }
    }
}
