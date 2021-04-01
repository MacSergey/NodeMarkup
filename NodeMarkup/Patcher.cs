using HarmonyLib;
using ModsCommon;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using NodeMarkup.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace NodeMarkup
{
    public class Patcher : BasePatcher
    {
        public Patcher(BaseMod mod) : base(mod) { }

        protected override bool PatchProcess()
        {
            var success = true;

            success &= Patch_ToolController_Awake();

            PatchNetManager(ref success);
            PatchNetNode(ref success);
            PatchNetSegment(ref success);
            PatchNetInfo(ref success);
            PatchLoading(ref success);

            success &= Patch_BuildingDecoration_LoadPaths();
            success &= PatchLoadAssetPanelOnLoad();
            success &= PatchGeneratedScrollPanelCreateOptionPanel();
            success &= PatchGameKeyShortcutsEscape();

            return success;
        }

        private bool Patch_ToolController_Awake()
        {
            var prefix = AccessTools.Method(typeof(NodeMarkupTool), nameof(NodeMarkupTool.Create));
            return AddPrefix(prefix, typeof(ToolController), "Awake");
        }

        #region NETMANAGER

        private void PatchNetManager(ref bool success)
        {
            success &= Patch_NetManagerRelease_NodeImplementation();
            success &= Patch_NetManagerReleas_SegmentImplementation();
            success &= Patch_NetManager_UpdateNode();
            success &= Patch_NetManager_UpdateSegment();
            success &= Patch_NetManager_SimulationStepImpl();
        }

        private bool Patch_NetManagerRelease_NodeImplementation()
        {
            var prefix = AccessTools.Method(typeof(MarkupManager), nameof(MarkupManager.NetManagerReleaseNodeImplementationPrefix));
            var parameters = new Type[] { typeof(ushort), typeof(NetNode).MakeByRefType() };
            return AddPrefix(prefix, typeof(NetManager), "ReleaseNodeImplementation", parameters);
        }
        private bool Patch_NetManagerReleas_SegmentImplementation()
        {
            var prefix = AccessTools.Method(typeof(MarkupManager), nameof(MarkupManager.NetManagerReleaseSegmentImplementationPrefix));
            var parameters = new Type[] { typeof(ushort), typeof(NetSegment).MakeByRefType(), typeof(bool) };
            return AddPrefix(prefix, typeof(NetManager), "ReleaseSegmentImplementation", parameters);
        }
        private bool Patch_NetManager_UpdateNode()
        {
            var postfix = AccessTools.Method(typeof(MarkupManager), nameof(MarkupManager.NetManagerUpdateNodePostfix));
            var parameters = new Type[] { typeof(ushort), typeof(ushort), typeof(int) };
            return AddPostfix(postfix, typeof(NetManager), nameof(NetManager.UpdateNode), parameters);
        }
        private bool Patch_NetManager_UpdateSegment()
        {
            var postfix = AccessTools.Method(typeof(MarkupManager), nameof(MarkupManager.NetManagerUpdateSegmentPostfix));
            var parameters = new Type[] { typeof(ushort), typeof(ushort), typeof(int) };
            return AddPostfix(postfix, typeof(NetManager), nameof(NetManager.UpdateSegment), parameters);
        }
        private bool Patch_NetManager_SimulationStepImpl()
        {
            var postfix = AccessTools.Method(typeof(MarkupManager), nameof(MarkupManager.NetManagerSimulationStepImplPostfix));
            return AddPostfix(postfix, typeof(NetManager), "SimulationStepImpl");
        }

        #endregion

        #region NETNODE

        private void PatchNetNode(ref bool success)
        {
            success &= Patch_NetNode_RenderInstance();
        }
        private bool Patch_NetNode_RenderInstance()
        {
            var postfix = AccessTools.Method(typeof(MarkupManager), nameof(MarkupManager.NetNodeRenderInstancePostfix));
            var parameters = new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(NetInfo), typeof(int), typeof(NetNode.Flags), typeof(uint).MakeByRefType(), typeof(RenderManager.Instance).MakeByRefType() };
            return AddPostfix(postfix, typeof(NetNode), nameof(NetNode.RenderInstance), parameters);
        }

        #endregion

        #region NETSEGMENT

        private void PatchNetSegment(ref bool success)
        {
            success &= Patch_NetSegment_RenderInstance();
            success &= Patch_NetSegment_UpdateLanes();
        }
        private bool Patch_NetSegment_RenderInstance()
        {
            var postfix = AccessTools.Method(typeof(MarkupManager), nameof(MarkupManager.NetSegmentRenderInstancePostfix));
            var parameters = new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(int), typeof(NetInfo), typeof(RenderManager.Instance).MakeByRefType() };
            return AddPostfix(postfix, typeof(NetSegment), nameof(NetSegment.RenderInstance), parameters);
        }
        private bool Patch_NetSegment_UpdateLanes()
        {
            var postfix = AccessTools.Method(typeof(MarkupManager), nameof(MarkupManager.NetSegmentUpdateLanesPostfix));
            return AddPostfix(postfix, typeof(NetSegment), nameof(NetSegment.UpdateLanes));
        }

        #endregion

        #region NETINFO

        private void PatchNetInfo(ref bool success)
        {
            if (Settings.RailUnderMarking)
            {
                success &= Patch_NetInfo_NodeInitNodeInfo();
                success &= Patch_NetInfo_InitSegmentInfo();
            }
        }
        private bool Patch_NetInfo_NodeInitNodeInfo()
        {
            var postfix = AccessTools.Method(typeof(MarkupManager), nameof(MarkupManager.NetInfoInitNodeInfoPostfix));
            return AddPostfix(postfix, typeof(NetInfo), "InitNodeInfo");
        }
        private bool Patch_NetInfo_InitSegmentInfo()
        {
            var postfix = AccessTools.Method(typeof(MarkupManager), nameof(MarkupManager.NetInfoInitSegmentInfoPostfix));
            return AddPostfix(postfix, typeof(NetInfo), "InitSegmentInfo");
        }

        #endregion

        #region LOADING

        private void PatchLoading(ref bool success)
        {
            if (Settings.LoadMarkingAssets)
            {
                success &= Patch_LoadingManager_LoadCustomContent();
                success &= Patch_LoadingScreenMod_LoadImpl();
            }
        }
        private bool Patch_LoadingManager_LoadCustomContent()
        {
            var nestedType = typeof(LoadingManager).GetNestedTypes(AccessTools.all).FirstOrDefault(t => t.FullName.Contains("LoadCustomContent"));
            var transpiler = AccessTools.Method(typeof(Patcher), nameof(Patcher.LoadingManagerLoadCustomContentTranspiler));
            return AddTranspiler(transpiler, nestedType, "MoveNext");
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
        private bool Patch_LoadingScreenMod_LoadImpl()
        {
            try
            {
                var type = AccessTools.TypeByName("LoadingScreenMod.AssetLoader") ?? AccessTools.TypeByName("LoadingScreenModTest.AssetLoader");
                var transpiler = AccessTools.Method(typeof(Patcher), nameof(Patcher.LoadingScreenModLoadImplTranspiler));
                return AddTranspiler(transpiler, type, "LoadImpl");
            }
            catch (Exception error)
            {
                Mod.Logger.Warning($"LSM not founded", error);
                return true;
            }
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

                    yield return new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(Loader), nameof(Loader.LoadTemplateAsset)));
                    break;
                }
            }

            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }

        #endregion


        private bool Patch_BuildingDecoration_LoadPaths()
        {
            var transpiler = AccessTools.Method(typeof(Patcher), nameof(Patcher.BuildingDecorationLoadPathsTranspiler));
            return AddTranspiler(transpiler, typeof(BuildingDecoration), nameof(BuildingDecoration.LoadPaths));
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

        private bool PatchLoadAssetPanelOnLoad()
        {
            var postfix = AccessTools.Method(typeof(AssetDataExtension), nameof(AssetDataExtension.LoadAssetPanelOnLoadPostfix));
            return AddPostfix(postfix, typeof(LoadAssetPanel), nameof(LoadAssetPanel.OnLoad));
        }
        private bool PatchGeneratedScrollPanelCreateOptionPanel()
        {
            var postfix = AccessTools.Method(typeof(NodeMarkupButton), nameof(NodeMarkupButton.GeneratedScrollPanelCreateOptionPanelPostfix));
            return AddPostfix(postfix, typeof(GeneratedScrollPanel), "CreateOptionPanel");
        }
        private bool PatchGameKeyShortcutsEscape()
        {
            var transpiler = AccessTools.Method(typeof(Patcher), nameof(Patcher.GameKeyShortcutsEscapeTranspiler));
            return AddTranspiler(transpiler, typeof(GameKeyShortcuts), "Escape");
        }
        private static IEnumerable<CodeInstruction> GameKeyShortcutsEscapeTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions)
        {
            var instructionList = instructions.ToList();

            var elseIndex = instructionList.FindLastIndex(i => i.opcode == OpCodes.Brfalse);
            var elseLabel = (Label)instructionList[elseIndex].operand;

            for (var i = elseIndex + 1; i < instructionList.Count; i += 1)
            {
                if (instructionList[i].labels.Contains(elseLabel))
                {
                    var elseInstruction = instructionList[i];
                    var oldElseLabels = elseInstruction.labels;
                    var newElseLabel = generator.DefineLabel();
                    elseInstruction.labels = new List<Label>() { newElseLabel };
                    var returnLabel = generator.DefineLabel();

                    var newInstructions = new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(NodeMarkupTool), $"get_{nameof(NodeMarkupTool.Instance)}")),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(NodeMarkupTool), $"get_{nameof(NodeMarkupTool.enabled)}")),
                        new CodeInstruction(OpCodes.Brfalse, newElseLabel),

                        new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(NodeMarkupTool), $"get_{nameof(NodeMarkupTool.Instance)}")),
                        new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(NodeMarkupTool), nameof(NodeMarkupTool.Escape))),
                        new CodeInstruction(OpCodes.Br, returnLabel),
                    };

                    newInstructions[0].labels = oldElseLabels;
                    instructionList.InsertRange(i, newInstructions);
                    instructionList.Last().labels.Add(returnLabel);

                    break;
                }
            }

            return instructionList;
        }

        
    }
}
