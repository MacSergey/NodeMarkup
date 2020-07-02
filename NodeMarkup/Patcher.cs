using CitiesHarmony.API;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using HarmonyLib;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

            var harmony = new Harmony(HarmonyId);
            PatchNetNodeRenderInstance(harmony);
            PatchNetManagerReleaseNodeImplementation(harmony);
            PatchNetManagerUpdateNode(harmony);
            PatchNetSegmentUpdateLanes(harmony);
            PatchNetManagerSimulationStepImpl(harmony);
            //PatchUIAnchorLayoutResetLayoutAbsolute(harmony);
        }

        private static void AddPrefix(Harmony harmony, MethodInfo original, MethodInfo prefix)
        {
            var methodName = $"{original.DeclaringType.Name}.{original.Name}";

            Logger.LogDebug($"Patch {methodName}");
            harmony.Patch(original, prefix: new HarmonyMethod(prefix));
            Logger.LogDebug($"Patched {methodName}");
        }
        private static void AddPostfix(Harmony harmony, MethodInfo original, MethodInfo postfix)
        {
            var methodName = $"{original.DeclaringType.Name}.{original.Name}";

            Logger.LogDebug($"Patch {methodName}");
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            Logger.LogDebug($"Patched {methodName}");
        }

        private static void PatchNetNodeRenderInstance(Harmony harmony)
        {
            var original = AccessTools.Method(typeof(NetNode), "RenderInstance", new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(NetInfo), typeof(int), typeof(NetNode.Flags), typeof(uint).MakeByRefType(), typeof(RenderManager.Instance).MakeByRefType() });
            var postfix = AccessTools.Method(typeof(Manager.MarkupManager), nameof(Manager.MarkupManager.NetNodeRenderInstancePostfix));

            AddPostfix(harmony, original, postfix);
        }

        private static void PatchNetManagerUpdateNode(Harmony harmony)
        {
            var original = AccessTools.Method(typeof(NetManager), nameof(NetManager.UpdateNode), new Type[] { typeof(ushort), typeof(ushort), typeof(int) });
            var postfix = AccessTools.Method(typeof(Manager.MarkupManager), nameof(Manager.MarkupManager.NetManagerUpdateNodePostfix));

            AddPostfix(harmony, original, postfix);
        }

        private static void PatchNetManagerReleaseNodeImplementation(Harmony harmony)
        {
            var original = AccessTools.Method(typeof(NetManager), "ReleaseNodeImplementation", new Type[] { typeof(ushort), typeof(NetNode).MakeByRefType() });
            var prefix = AccessTools.Method(typeof(Manager.MarkupManager), nameof(Manager.MarkupManager.NetManagerReleaseNodeImplementationPrefix));

            AddPrefix(harmony, original, prefix);
        }

        private static void PatchNetSegmentUpdateLanes(Harmony harmony)
        {
            var original = AccessTools.Method(typeof(NetSegment), nameof(NetSegment.UpdateLanes));
            var postfix = AccessTools.Method(typeof(Manager.MarkupManager), nameof(Manager.MarkupManager.NetSegmentUpdateLanesPostfix));

            AddPostfix(harmony, original, postfix);
        }

        private static void PatchNetManagerSimulationStepImpl(Harmony harmony)
        {
            var original = AccessTools.Method(typeof(NetManager), "SimulationStepImpl");
            var postfix = AccessTools.Method(typeof(Manager.MarkupManager), nameof(Manager.MarkupManager.NetManagerSimulationStepImplPostfix));

            AddPostfix(harmony, original, postfix);
        }

        //private static void PatchUIAnchorLayoutResetLayoutAbsolute(Harmony harmony)
        //{
        //    var original = AccessTools.Method(typeof(UIAnchorLayout), "ResetLayoutAbsolute");
        //    var postfix = AccessTools.Method(typeof(Patcher), nameof(ResetLayoutAbsolute));

        //    AddPostfix(harmony, original, postfix);
        //}

        //private static void ResetLayoutAbsolute(ref UIAnchorMargins ___m_Margins)
        //{
        //    ___m_Margins.left = 0;
        //    ___m_Margins.right = 0;
        //    ___m_Margins.top = 0;
        //    ___m_Margins.bottom = 0;
        //}
    }
}
