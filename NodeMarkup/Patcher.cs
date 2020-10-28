using CitiesHarmony.API;
using ColossalFramework;
using ColossalFramework.Packaging;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using HarmonyLib;
using NodeMarkup.Manager;
using NodeMarkup.UI;
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
        public static bool Success { get; private set; }

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

            Success = true;

            var harmony = new Harmony(HarmonyId);
            Success &= PatchNetNodeRenderInstance(harmony);
            Success &= PatchNetManagerReleaseNodeImplementation(harmony);
            Success &= PatchNetManagerUpdateNode(harmony);
            Success &= PatchNetSegmentUpdateLanes(harmony);
            Success &= PatchNetManagerSimulationStepImpl(harmony);
            Success &= PatchBuildingDecorationLoadPaths(harmony);
            Success &= PatchLoadAssetPanelOnLoad(harmony);

            if (Settings.RailUnderMarking)
                Success &= PatchNetInfoNodeInitNodeInfo(harmony);

            Success &= PatchLoadingManagerLoadCustomContent(harmony);
            Success &= PatchLoadingScreenModLoadImpl(harmony);

            //if (!success)
            //{
            //    var messageBox = MessageBoxBase.ShowModal<ErrorLoadedMessageBox>();
            //    messageBox.MessageText = "Mod loaded with errors";
            //}
        }

        private static bool AddPrefix(Harmony harmony, MethodInfo original, MethodInfo prefix)
            => AddPatch(original, () => harmony.Patch(original, prefix: new HarmonyMethod(prefix)));

        private static bool AddPostfix(Harmony harmony, MethodInfo original, MethodInfo postfix)
            => AddPatch(original, () => harmony.Patch(original, postfix: new HarmonyMethod(postfix)));

        private static bool AddTranspiler(Harmony harmony, MethodInfo original, MethodInfo transpiler)
            => AddPatch(original, () => harmony.Patch(original, transpiler: new HarmonyMethod(transpiler)));

        private static bool AddPatch(MethodInfo original, Action patch)
        {
            var methodName = $"{original.DeclaringType.Name}.{original.Name}";

            try
            {
                Logger.LogDebug($"Patch {methodName}");
                patch();
                Logger.LogDebug($"Patched {methodName}");
                return true;
            }
            catch (Exception error)
            {
                Logger.LogError(() => $"Failed Patch {methodName}", error);
                return false;
            }
        }

        private static bool PatchNetNodeRenderInstance(Harmony harmony)
        {
            var original = AccessTools.Method(typeof(NetNode), "RenderInstance", new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(NetInfo), typeof(int), typeof(NetNode.Flags), typeof(uint).MakeByRefType(), typeof(RenderManager.Instance).MakeByRefType() });
            var postfix = AccessTools.Method(typeof(MarkupManager), nameof(MarkupManager.NetNodeRenderInstancePostfix));

            return AddPostfix(harmony, original, postfix);
        }

        private static bool PatchNetManagerUpdateNode(Harmony harmony)
        {
            var original = AccessTools.Method(typeof(NetManager), nameof(NetManager.UpdateNode), new Type[] { typeof(ushort), typeof(ushort), typeof(int) });
            var postfix = AccessTools.Method(typeof(MarkupManager), nameof(MarkupManager.NetManagerUpdateNodePostfix));

            return AddPostfix(harmony, original, postfix);
        }

        private static bool PatchNetManagerReleaseNodeImplementation(Harmony harmony)
        {
            var original = AccessTools.Method(typeof(NetManager), "ReleaseNodeImplementation", new Type[] { typeof(ushort), typeof(NetNode).MakeByRefType() });
            var prefix = AccessTools.Method(typeof(MarkupManager), nameof(MarkupManager.NetManagerReleaseNodeImplementationPrefix));

            return AddPrefix(harmony, original, prefix);
        }

        private static bool PatchNetSegmentUpdateLanes(Harmony harmony)
        {
            var original = AccessTools.Method(typeof(NetSegment), nameof(NetSegment.UpdateLanes));
            var postfix = AccessTools.Method(typeof(MarkupManager), nameof(MarkupManager.NetSegmentUpdateLanesPostfix));

            return AddPostfix(harmony, original, postfix);
        }

        private static bool PatchNetManagerSimulationStepImpl(Harmony harmony)
        {
            var original = AccessTools.Method(typeof(NetManager), "SimulationStepImpl");
            var postfix = AccessTools.Method(typeof(MarkupManager), nameof(MarkupManager.NetManagerSimulationStepImplPostfix));

            return AddPostfix(harmony, original, postfix);
        }
        private static bool PatchBuildingDecorationLoadPaths(Harmony harmony)
        {
            var original = AccessTools.Method(typeof(BuildingDecoration), nameof(BuildingDecoration.LoadPaths));
            var transpiler = AccessTools.Method(typeof(Patcher), nameof(Patcher.BuildingDecorationLoadPathsTranspiler));

            return AddTranspiler(harmony, original, transpiler);
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

                if (!inserted && matchCount == 2)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, segmentBufferField);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, nodeBufferField);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(MarkupManager), nameof(MarkupManager.PlaceIntersection)));
                    inserted = true;
                }

                if (prevInstruction != null)
                    yield return prevInstruction;

                prevInstruction = instruction;
            }

            if (prevInstruction != null)
                yield return prevInstruction;
        }
        private static bool PatchLoadAssetPanelOnLoad(Harmony harmony)
        {
            var original = AccessTools.Method(typeof(LoadAssetPanel), nameof(LoadAssetPanel.OnLoad));
            var postfix = AccessTools.Method(typeof(AssetDataExtension), nameof(AssetDataExtension.LoadAssetPanelOnLoadPostfix));

            return AddPostfix(harmony, original, postfix);
        }
        private static bool PatchNetInfoNodeInitNodeInfo(Harmony harmony)
        {
            var original = AccessTools.Method(typeof(NetInfo), "InitNodeInfo");
            var postfix = AccessTools.Method(typeof(MarkupManager), nameof(MarkupManager.NetInfoNodeInitNodeInfoPostfix));

            return AddPostfix(harmony, original, postfix);
        }
        private static bool PatchLoadingManagerLoadCustomContent(Harmony harmony)
        {
            var type = typeof(LoadingManager).GetNestedTypes(AccessTools.all).FirstOrDefault(t => t.FullName.Contains("LoadCustomContent"));
            var original = AccessTools.Method(type, "MoveNext");
            var transpiler = AccessTools.Method(typeof(Patcher), nameof(Patcher.LoadingManagerLoadCustomContentTranspiler));

            return AddTranspiler(harmony, original, transpiler);
        }

        private static IEnumerable<CodeInstruction> LoadingManagerLoadCustomContentTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions)
        {
            var type = typeof(LoadingManager).GetNestedTypes(AccessTools.all).FirstOrDefault(t => t.FullName.Contains("LoadCustomContent"));
            var field = AccessTools.Field(type, "<metaData>__4");
            var additional = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldloc_S, 19),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, field),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CustomAssetMetaData), nameof(CustomAssetMetaData.assetRef))),
            };

            return LoadingTranspiler(instructions, OpCodes.Ldloc_S, 26, additional);
        }
        private static bool PatchLoadingScreenModLoadImpl(Harmony harmony)
        {
            var original = AccessTools.Method(AccessTools.TypeByName("LoadingScreenMod.AssetLoader"), "LoadImpl");
            var transpiler = AccessTools.Method(typeof(Patcher), nameof(Patcher.LoadingScreenModLoadImplTranspiler));

            return AddTranspiler(harmony, original, transpiler);
        }
        private static IEnumerable<CodeInstruction> LoadingScreenModLoadImplTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions)
        {
            var additional = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Ldarg_1),
            };

            return LoadingTranspiler(instructions, OpCodes.Stloc_S, 12, additional);
        }
        private static IEnumerable<CodeInstruction> LoadingTranspiler(IEnumerable<CodeInstruction> instructions, OpCode startOc, int startOp, CodeInstruction[] additional)
        {
            var enumerator = instructions.GetEnumerator();

            while (enumerator.MoveNext())
            {
                var instruction = enumerator.Current;
                yield return instruction;

                if (instruction.opcode == startOc && instruction.operand is LocalBuilder local && local.LocalIndex == startOp)
                    break;
            }

            var elseLabel = (Label)default;
            while (enumerator.MoveNext())
            {
                var instruction = enumerator.Current;
                yield return instruction;

                if (instruction.opcode == OpCodes.Brfalse || instruction.opcode == OpCodes.Brfalse_S)
                {
                    if (instruction.operand is Label label)
                        elseLabel = label;

                    break;
                }
            }

            if (elseLabel == default)
                throw new Exception("else label not founded");

            while (enumerator.MoveNext())
            {
                var instruction = enumerator.Current;
                yield return instruction;

                if (instruction.labels.Contains(elseLabel))
                {
                    foreach (var additionalInst in additional)
                        yield return additionalInst;

                    yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(TemplateManager), nameof(TemplateManager.LoadAsset)));
                    break;
                }
            }

            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }
    }
}
