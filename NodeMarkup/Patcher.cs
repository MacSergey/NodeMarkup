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
            return AddPrefix(typeof(NodeMarkupTool), nameof(NodeMarkupTool.Create), typeof(ToolController), "Awake");
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
            var parameters = new Type[] { typeof(ushort), typeof(NetNode).MakeByRefType() };
            return AddPrefix(typeof(MarkupManager), nameof(MarkupManager.NetManagerReleaseNodeImplementationPrefix), typeof(NetManager), "ReleaseNodeImplementation", parameters);
        }
        private bool Patch_NetManagerReleas_SegmentImplementation()
        {
            var parameters = new Type[] { typeof(ushort), typeof(NetSegment).MakeByRefType(), typeof(bool) };
            return AddPrefix(typeof(MarkupManager), nameof(MarkupManager.NetManagerReleaseSegmentImplementationPrefix), typeof(NetManager), "ReleaseSegmentImplementation", parameters);
        }
        private bool Patch_NetManager_UpdateNode()
        {
            var parameters = new Type[] { typeof(ushort), typeof(ushort), typeof(int) };
            return AddPostfix(typeof(MarkupManager), nameof(MarkupManager.NetManagerUpdateNodePostfix), typeof(NetManager), nameof(NetManager.UpdateNode), parameters);
        }
        private bool Patch_NetManager_UpdateSegment()
        {
            var parameters = new Type[] { typeof(ushort), typeof(ushort), typeof(int) };
            return AddPostfix(typeof(MarkupManager), nameof(MarkupManager.NetManagerUpdateSegmentPostfix), typeof(NetManager), nameof(NetManager.UpdateSegment), parameters);
        }
        private bool Patch_NetManager_SimulationStepImpl()
        {
            return AddPostfix(typeof(MarkupManager), nameof(MarkupManager.NetManagerSimulationStepImplPostfix), typeof(NetManager), "SimulationStepImpl");
        }

        #endregion

        #region NETNODE

        private void PatchNetNode(ref bool success)
        {
            success &= Patch_NetNode_RenderInstance();
        }
        private bool Patch_NetNode_RenderInstance()
        {
            var parameters = new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(NetInfo), typeof(int), typeof(NetNode.Flags), typeof(uint).MakeByRefType(), typeof(RenderManager.Instance).MakeByRefType() };
            return AddPostfix(typeof(MarkupManager), nameof(MarkupManager.NetNodeRenderInstancePostfix), typeof(NetNode), nameof(NetNode.RenderInstance), parameters);
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
            var parameters = new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(int), typeof(NetInfo), typeof(RenderManager.Instance).MakeByRefType() };
            return AddPostfix(typeof(MarkupManager), nameof(MarkupManager.NetSegmentRenderInstancePostfix), typeof(NetSegment), nameof(NetSegment.RenderInstance), parameters);
        }
        private bool Patch_NetSegment_UpdateLanes()
        {
            return AddPostfix(typeof(MarkupManager), nameof(MarkupManager.NetSegmentUpdateLanesPostfix), typeof(NetSegment), nameof(NetSegment.UpdateLanes));
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
            return AddPostfix(typeof(MarkupManager), nameof(MarkupManager.NetInfoInitNodeInfoPostfix), typeof(NetInfo), "InitNodeInfo");
        }
        private bool Patch_NetInfo_InitSegmentInfo()
        {
            return AddPostfix(typeof(MarkupManager), nameof(MarkupManager.NetInfoInitSegmentInfoPostfix), typeof(NetInfo), "InitSegmentInfo");
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
            return AddTranspiler(typeof(Patcher), nameof(Patcher.LoadingManagerLoadCustomContentTranspiler), nestedType, "MoveNext");
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
            if ((AccessTools.TypeByName("LoadingScreenMod.AssetLoader") ?? AccessTools.TypeByName("LoadingScreenModTest.AssetLoader")) is not Type type)
            {
                Mod.Logger.Warning($"LSM not founded, patch skip");
                return true;
            }
            else
                return AddTranspiler(typeof(Patcher), nameof(Patcher.LoadingScreenModLoadImplTranspiler), type, "LoadImpl");
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
            return AddTranspiler(typeof(Patcher), nameof(Patcher.BuildingDecorationLoadPathsTranspiler), typeof(BuildingDecoration), nameof(BuildingDecoration.LoadPaths));
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
            return AddPostfix(typeof(AssetDataExtension), nameof(AssetDataExtension.LoadAssetPanelOnLoadPostfix), typeof(LoadAssetPanel), nameof(LoadAssetPanel.OnLoad));
        }
        private bool PatchGeneratedScrollPanelCreateOptionPanel()
        {
            return AddPostfix(typeof(NodeMarkupButton), nameof(NodeMarkupButton.GeneratedScrollPanelCreateOptionPanelPostfix), typeof(GeneratedScrollPanel), "CreateOptionPanel");
        }
        private bool PatchGameKeyShortcutsEscape()
        {
            return AddTranspiler(typeof(Patcher), nameof(Patcher.GameKeyShortcutsEscapeTranspiler), typeof(GameKeyShortcuts), "Escape");
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
