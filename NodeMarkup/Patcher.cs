using CitiesHarmony.API;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using HarmonyLib;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
            PatchBuildingDecorationLoadPaths(harmony);
            PatchLoadAssetPanelOnLoad(harmony);
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
        private static void AddTranspiler(Harmony harmony, MethodInfo original, MethodInfo transpiler)
        {
            var methodName = $"{original.DeclaringType.Name}.{original.Name}";

            Logger.LogDebug($"Patch {methodName}");
            harmony.Patch(original, transpiler: new HarmonyMethod(transpiler));
            Logger.LogDebug($"Patched {methodName}");
        }

        private static void PatchNetNodeRenderInstance(Harmony harmony)
        {
            var original = AccessTools.Method(typeof(NetNode), "RenderInstance", new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(NetInfo), typeof(int), typeof(NetNode.Flags), typeof(uint).MakeByRefType(), typeof(RenderManager.Instance).MakeByRefType() });
            var postfix = AccessTools.Method(typeof(MarkupManager), nameof(MarkupManager.NetNodeRenderInstancePostfix));

            AddPostfix(harmony, original, postfix);
        }

        private static void PatchNetManagerUpdateNode(Harmony harmony)
        {
            var original = AccessTools.Method(typeof(NetManager), nameof(NetManager.UpdateNode), new Type[] { typeof(ushort), typeof(ushort), typeof(int) });
            var postfix = AccessTools.Method(typeof(MarkupManager), nameof(MarkupManager.NetManagerUpdateNodePostfix));

            AddPostfix(harmony, original, postfix);
        }

        private static void PatchNetManagerReleaseNodeImplementation(Harmony harmony)
        {
            var original = AccessTools.Method(typeof(NetManager), "ReleaseNodeImplementation", new Type[] { typeof(ushort), typeof(NetNode).MakeByRefType() });
            var prefix = AccessTools.Method(typeof(MarkupManager), nameof(MarkupManager.NetManagerReleaseNodeImplementationPrefix));

            AddPrefix(harmony, original, prefix);
        }

        private static void PatchNetSegmentUpdateLanes(Harmony harmony)
        {
            var original = AccessTools.Method(typeof(NetSegment), nameof(NetSegment.UpdateLanes));
            var postfix = AccessTools.Method(typeof(MarkupManager), nameof(MarkupManager.NetSegmentUpdateLanesPostfix));

            AddPostfix(harmony, original, postfix);
        }

        private static void PatchNetManagerSimulationStepImpl(Harmony harmony)
        {
            var original = AccessTools.Method(typeof(NetManager), "SimulationStepImpl");
            var postfix = AccessTools.Method(typeof(MarkupManager), nameof(MarkupManager.NetManagerSimulationStepImplPostfix));

            AddPostfix(harmony, original, postfix);
        }
        private static void PatchBuildingDecorationLoadPaths(Harmony harmony)
        {
            var original = AccessTools.Method(typeof(BuildingDecoration), nameof(BuildingDecoration.LoadPaths));
            var transpiler = AccessTools.Method(typeof(Patcher), nameof(Patcher.BuildingDecorationLoadPathsTranspiler));

            AddTranspiler(harmony, original, transpiler);
        }
        private static IEnumerable<CodeInstruction> BuildingDecorationLoadPathsTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions)
        {
            var segmentBufferField = AccessTools.DeclaredField(typeof(NetManager), nameof(NetManager.m_tempSegmentBuffer));
            var nodeBufferField = AccessTools.DeclaredField(typeof(NetManager), nameof(NetManager.m_tempNodeBuffer));
            var clearMethod = AccessTools.DeclaredMethod(nodeBufferField.FieldType, nameof(FastList<ushort>.Clear));

            var matchCount = 0;
            var inserted = false;
            var enumerator = instructions.GetEnumerator();
            var prevInstruction = (CodeInstruction)null;
            while (enumerator.MoveNext())
            {
                var instruction = enumerator.Current;

                if (prevInstruction != null && prevInstruction.opcode == OpCodes.Ldfld && prevInstruction.operand == nodeBufferField && instruction.opcode == OpCodes.Callvirt && instruction.operand == clearMethod)
                    matchCount += 1;

                if(!inserted && matchCount == 2)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, segmentBufferField);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, nodeBufferField);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(MarkupManager), nameof(MarkupManager.PlaceIntersection)));
                    inserted = true;
                }

                if(prevInstruction != null)
                    yield return prevInstruction;

                prevInstruction = instruction;
            }

            if (prevInstruction != null)
                yield return prevInstruction;
        }
        private static void PatchLoadAssetPanelOnLoad(Harmony harmony)
        {
            var original = AccessTools.Method(typeof(LoadAssetPanel), nameof(LoadAssetPanel.OnLoad));
            var postfix = AccessTools.Method(typeof(AssetDataExtension), nameof(AssetDataExtension.LoadAssetPanelOnLoadPostfix));

            AddPostfix(harmony, original, postfix);
        }
    }
}
