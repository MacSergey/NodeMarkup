﻿using ColossalFramework.Math;
using ColossalFramework.UI;
using HarmonyLib;
using ICities;
using IMT.Manager;
using IMT.Tools;
using IMT.UI;
using IMT.Utilities;
using ModsCommon;
using ModsCommon.Utilities;
using ModsCommon.Settings;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace IMT
{
    [SettingFile("NodeMarkup")]
    public class Mod : BasePatcherMod<Mod>
    {
        #region PROPERTIES

        public static string ReportBugUrl { get; } = "https://github.com/MacSergey/NodeMarkup/issues/new?assignees=&labels=NEW+ISSUE&template=bug_report.md";
        public static string WikiUrl { get; } = "https://github.com/MacSergey/NodeMarkup/wiki";
        public static string TroubleshootingUrl { get; } = "https://github.com/MacSergey/NodeMarkup/wiki/Troubleshooting";

        protected override string IdRaw => "IntersectionMarkingTool";
        public override List<ModVersion> Versions { get; } = new List<ModVersion>
        {
            new ModVersion(new Version(1,14,6), new DateTime(2024, 10, 26)),
            new ModVersion(new Version(1,14,5), new DateTime(2024, 9, 27)),
            new ModVersion(new Version(1,14,4), new DateTime(2023, 10, 14)),
            new ModVersion(new Version(1,14,3), new DateTime(2023, 6, 13)),
            new ModVersion(new Version(1,14,2), new DateTime(2023, 5, 27)),
            new ModVersion(new Version(1,14,1), new DateTime(2023, 4, 15)),
            new ModVersion(new Version(1,14), new DateTime(2023, 4, 1)),
            new ModVersion(new Version(1,13,1), new DateTime(2023, 2, 19)),
            new ModVersion(new Version(1,13), new DateTime(2023, 2, 12)),
            new ModVersion(new Version(1,12), new DateTime(2023, 1, 7)),
            new ModVersion(new Version(1,11), new DateTime(2022, 12, 23)),
            new ModVersion(new Version(1,10,2), new DateTime(2022, 9, 14)),
            new ModVersion(new Version(1,10,1), new DateTime(2022, 7, 9)),
            new ModVersion(new Version(1,10), new DateTime(2022, 7, 2)),
            new ModVersion(new Version(1,9), new DateTime(2022, 6, 2)),
            new ModVersion(new Version(1,8,2), new DateTime(2021, 8, 25)),
            new ModVersion(new Version(1,8,1), new DateTime(2021, 8, 8)),
            new ModVersion(new Version(1,8), new DateTime(2021, 7, 21)),
            new ModVersion(new Version(1,7,4), new DateTime(2021, 7, 6)),
            new ModVersion(new Version(1,7,3), new DateTime(2021, 5, 29)),
            new ModVersion(new Version(1,7,2), new DateTime(2021, 5, 19)),
            new ModVersion(new Version(1,7,1), new DateTime(2021, 5, 6)),
            new ModVersion(new Version(1,7), new DateTime(2021, 4, 3)),
            new ModVersion(new Version(1,6), new DateTime(2021, 3, 12)),
            new ModVersion(new Version(1,5,3), new DateTime(2021, 3, 8)),
            new ModVersion(new Version(1,5,2), new DateTime(2021, 2, 15)),
            new ModVersion(new Version(1,5,1), new DateTime(2021, 2, 5)),
            new ModVersion(new Version(1,5), new DateTime(2020, 11, 16)),
            new ModVersion(new Version(1,4,1), new DateTime(2020, 10, 25)),
            new ModVersion(new Version(1,4), new DateTime(2020, 10, 18)),
            new ModVersion(new Version(1,3), new DateTime(2020, 8, 22)),
            new ModVersion(new Version(1,2,1), new DateTime(2020, 7, 30)),
            new ModVersion(new Version(1,2), new DateTime(2020, 7, 27)),
            new ModVersion(new Version(1,1), new DateTime(2020, 7, 14)),
            new ModVersion(new Version(1,0), new DateTime(2020, 7, 7)),
        };
        protected override Version RequiredGameVersion => new Version(1, 18, 1, 3);

        public override string NameRaw => "Intersection Marking Tool";
        public override string Description => !IsBeta ? Localize.Mod_Description : CommonLocalize.Mod_DescriptionBeta;
        protected override ulong StableWorkshopId => 2140418403ul;
        protected override ulong BetaWorkshopId => 2159934925ul;
        protected override string ModSupportUrl => TroubleshootingUrl;
        public override string CrowdinUrl => "https://crowdin.com/translate/intersection-marking-tool/147";

#if BETA
        public override bool IsBeta => true;
#else
        public override bool IsBeta => false;
#endif
        #endregion

        protected override LocalizeManager LocalizeManager => Localize.LocaleManager;
        protected override bool NeedMonoDevelopImpl => true;

        #region BASIC

        protected override void GetSettings(UIHelperBase helper)
        {
            var settings = new Settings();
            settings.OnSettingsUI(helper);
        }

        protected override void SetCulture(CultureInfo culture) => Localize.Culture = culture;

        #endregion

        #region PATCHES

        protected override bool PatchProcess()
        {
            var success = true;

            success &= FixHasInputFocus();
            success &= AddTool();
            success &= AddNetToolButton();
            success &= ToolOnEscape();
            success &= AssetDataExtensionFix();
            success &= BuildingAssetDataLoad();
            success &= NetworkAssetDataLoad();

            PatchNetManager(ref success);
            PatchNetNode(ref success);
            PatchNetSegment(ref success);
            PatchNetInfo(ref success);
            PatchLoading(ref success);

            success &= Patch_ThemeMixer_TerrainTexture();

            return success;
        }

        #region TEMP

        private bool AddTool()
        {
            return AddTranspiler(typeof(Patcher), nameof(Patcher.ToolControllerAwakeTranspiler), typeof(ToolController), "Awake");
        }
        private bool AddNetToolButton()
        {
            return AddPostfix(typeof(Patcher), nameof(Patcher.GeneratedScrollPanelCreateOptionPanelPostfix), typeof(GeneratedScrollPanel), "CreateOptionPanel");
        }
        private bool ToolOnEscape()
        {
            return AddTranspiler(typeof(Patcher), nameof(Patcher.GameKeyShortcutsEscapeTranspiler), typeof(GameKeyShortcuts), "Escape");
        }
        private bool FixHasInputFocus()
        {
            return AddPrefix(typeof(ModsCommon.Patcher), nameof(ModsCommon.Patcher.UIViewHasInputFocusPrefix), typeof(UIView), nameof(UIView.HasInputFocus));
        }
        private bool AssetDataExtensionFix()
        {
            return AddPostfix(typeof(Patcher), nameof(Patcher.LoadAssetPanelOnLoadPostfix), typeof(LoadAssetPanel), nameof(LoadAssetPanel.OnLoad));
        }
        private bool BuildingAssetDataLoad()
        {
            return AddTranspiler(typeof(Patcher), nameof(Patcher.BuildingDecorationLoadPathsTranspiler), typeof(BuildingDecoration), nameof(BuildingDecoration.LoadPaths));
        }
        private bool NetworkAssetDataLoad()
        {
            return AddPostfix(typeof(Patcher), nameof(Patcher.NetManagerCreateSegmentPostfix), typeof(NetManager), nameof(NetManager.CreateSegment), new Type[] { typeof(ushort).MakeByRefType(), typeof(Randomizer).MakeByRefType(), typeof(NetInfo), typeof(TreeInfo), typeof(ushort), typeof(ushort), typeof(Vector3), typeof(Vector3), typeof(uint), typeof(uint), typeof(bool) });
        }

        #endregion

        #region NETMANAGER

        private void PatchNetManager(ref bool success)
        {
            success &= Patch_NetManager_ReleaseNodeImplementation();
            success &= Patch_NetManager_ReleaseSegmentImplementation();
            success &= Patch_NetManager_SimulationStepImpl_Prefix();
            success &= Patch_NetManager_SimulationStepImpl_Postfix();
            success &= Patch_NetManager_EndOverlay_Prefix();
            success &= Patch_NetNode_UpdateNodeRenderer();
            success &= Patch_NetSegment_UpdateSegmentRenderer();
        }

        private bool Patch_NetManager_ReleaseNodeImplementation()
        {
            var parameters = new Type[] { typeof(ushort), typeof(NetNode).MakeByRefType() };
            return AddPrefix(typeof(MarkingManager), nameof(MarkingManager.NetManagerReleaseNodeImplementationPrefix), typeof(NetManager), "ReleaseNodeImplementation", parameters);
        }
        private bool Patch_NetManager_ReleaseSegmentImplementation()
        {
            var parameters = new Type[] { typeof(ushort), typeof(NetSegment).MakeByRefType(), typeof(bool) };
            return AddPrefix(typeof(MarkingManager), nameof(MarkingManager.NetManagerReleaseSegmentImplementationPrefix), typeof(NetManager), "ReleaseSegmentImplementation", parameters);
        }
        private bool Patch_NetManager_SimulationStepImpl_Prefix()
        {
            return AddPrefix(typeof(MarkingManager), nameof(MarkingManager.GetToUpdate), typeof(NetManager), "SimulationStepImpl");
        }
        private bool Patch_NetManager_SimulationStepImpl_Postfix()
        {
            return AddPostfix(typeof(MarkingManager), nameof(MarkingManager.Update), typeof(NetManager), "SimulationStepImpl");
        }
        private bool Patch_NetManager_EndOverlay_Prefix()
        {
            return AddPrefix(typeof(Mod), nameof(Mod.NetManagerEndOverlay), typeof(NetManager), "EndOverlayImpl");
        }
        private static bool NetManagerEndOverlay()
        {
            return !SingletonTool<IntersectionMarkingTool>.Instance.enabled || !Settings.HideStreetName;
        }

        private bool Patch_NetNode_UpdateNodeRenderer()
        {
            return AddTranspiler(typeof(Mod), nameof(Mod.NetNode_UpdateNodeRenderer_Transpiler), typeof(NetManager), nameof(NetManager.UpdateNodeRenderer));
        }
        private static IEnumerable<CodeInstruction> NetNode_UpdateNodeRenderer_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var patched = false;

            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (!patched && instruction.opcode == OpCodes.Stloc_S && instruction.operand is LocalBuilder local && local.LocalIndex == 6)
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_S, local);
                    yield return new CodeInstruction(TranspilerUtilities.GetLDArg(original, "node"));
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MarkingManager), nameof(MarkingManager.GetNodeRenderLayer)));
                    yield return new CodeInstruction(OpCodes.Or);
                    yield return instruction;
                    patched = true;
                }
            }
        }

        private bool Patch_NetSegment_UpdateSegmentRenderer()
        {
            return AddTranspiler(typeof(Mod), nameof(Mod.NetSegment_UpdateSegmentRenderer_Transpiler), typeof(NetManager), nameof(NetManager.UpdateSegmentRenderer));
        }
        private static IEnumerable<CodeInstruction> NetSegment_UpdateSegmentRenderer_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var patched = false;

            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (!patched && instruction.opcode == OpCodes.Stloc_S && instruction.operand is LocalBuilder local && local.LocalIndex == 10)
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_S, local);
                    yield return new CodeInstruction(TranspilerUtilities.GetLDArg(original, "segment"));
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MarkingManager), nameof(MarkingManager.GetSegmentRenderLayer)));
                    yield return new CodeInstruction(OpCodes.Or);
                    yield return instruction;
                    patched = true;
                }
            }
        }

        #endregion

        #region NETNODE

        private void PatchNetNode(ref bool success)
        {
            success &= Patch_NetNode_RenderInstance();
            success &= Patch_NetNode_CalculateGroupData();
            success &= Patch_NetNode_PopulateGroupData();
            success &= Patch_NetNode_CheckHeightOffset();
        }
        private bool Patch_NetNode_RenderInstance()
        {
            var parameters = new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(NetInfo), typeof(int), typeof(NetNode.Flags), typeof(uint).MakeByRefType(), typeof(RenderManager.Instance).MakeByRefType() };
            return AddPostfix(typeof(MarkingManager), nameof(MarkingManager.NetNodeRenderInstancePostfix), typeof(NetNode), nameof(NetNode.RenderInstance), parameters);
        }
        private bool Patch_NetNode_CalculateGroupData()
        {
            var parameters = new Type[] { typeof(ushort), typeof(int), typeof(int).MakeByRefType(), typeof(int).MakeByRefType(), typeof(int).MakeByRefType(), typeof(RenderGroup.VertexArrays).MakeByRefType() };
            return AddPostfix(typeof(MarkingManager), nameof(MarkingManager.NetNodeCalculateGroupDataPostfix), typeof(NetNode), nameof(NetNode.CalculateGroupData), parameters);
        }
        private bool Patch_NetNode_PopulateGroupData()
        {
            var parameters = new Type[] { typeof(ushort), typeof(int), typeof(int), typeof(int), typeof(int).MakeByRefType(), typeof(int).MakeByRefType(), typeof(Vector3), typeof(RenderGroup.MeshData), typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(float).MakeByRefType(), typeof(float).MakeByRefType(), typeof(bool).MakeByRefType() };
            return AddPrefix(typeof(MarkingManager), nameof(MarkingManager.NetNodePopulateGroupDataPrefix), typeof(NetNode), nameof(NetNode.PopulateGroupData), parameters);
        }

        private bool Patch_NetNode_CheckHeightOffset()
        {
            return AddTranspiler(typeof(Mod), nameof(Mod.NetNode_CheckHeightOffset_Transpiler), typeof(NetNode), "CheckHeightOffset");
        }
        private static IEnumerable<CodeInstruction> NetNode_CheckHeightOffset_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var updateLanes = AccessTools.Method(typeof(NetSegment), nameof(NetSegment.UpdateLanes));

            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ret)
                {
                    yield return TranspilerUtilities.GetLDArg(original, "nodeID");
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MarkingManager), nameof(MarkingManager.UpdateNode)));
                    yield return instruction;
                }
                else if (instruction.opcode == OpCodes.Call && instruction.operand == updateLanes)
                {
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 13);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MarkingManager), nameof(MarkingManager.UpdateSegment)));
                }
                else
                    yield return instruction;
            }
        }

        #endregion

        #region NETSEGMENT

        private void PatchNetSegment(ref bool success)
        {
            success &= Patch_NetSegment_RenderInstance();
            success &= Patch_NetSegment_CalculateGroupData();
            success &= Patch_NetSegment_PopulateGroupData();
        }
        private bool Patch_NetSegment_RenderInstance()
        {
            var parameters = new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(int), typeof(NetInfo), typeof(RenderManager.Instance).MakeByRefType() };
            return AddPostfix(typeof(MarkingManager), nameof(MarkingManager.NetSegmentRenderInstancePostfix), typeof(NetSegment), nameof(NetSegment.RenderInstance), parameters);
        }
        private bool Patch_NetSegment_CalculateGroupData()
        {
            var parameters = new Type[] { typeof(ushort), typeof(int), typeof(int).MakeByRefType(), typeof(int).MakeByRefType(), typeof(int).MakeByRefType(), typeof(RenderGroup.VertexArrays).MakeByRefType() };
            return AddPostfix(typeof(MarkingManager), nameof(MarkingManager.NetSegmentCalculateGroupDataPostfix), typeof(NetSegment), nameof(NetSegment.CalculateGroupData), parameters);
        }
        private bool Patch_NetSegment_PopulateGroupData()
        {
            var parameters = new Type[] { typeof(ushort), typeof(int), typeof(int), typeof(int), typeof(int).MakeByRefType(), typeof(int).MakeByRefType(), typeof(Vector3), typeof(RenderGroup.MeshData), typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(float).MakeByRefType(), typeof(float).MakeByRefType(), typeof(bool).MakeByRefType() };
            return AddPrefix(typeof(MarkingManager), nameof(MarkingManager.NetSegmentPopulateGroupDataPrefix), typeof(NetSegment), nameof(NetSegment.PopulateGroupData), parameters);
        }

        #endregion

        #region NETINFO

        private void PatchNetInfo(ref bool success)
        {
            if (Settings.RailUnderMarking)
            {
                success &= Patch_NetInfo_NodeInitNodeInfo_Rail();
                success &= Patch_NetInfo_InitSegmentInfo();
            }
            if (Settings.LevelCrossingUnderMarking)
                success &= Patch_NetInfo_NodeInitNodeInfo_LevelCrossing();

        }
        private bool Patch_NetInfo_NodeInitNodeInfo_Rail()
        {
            return AddPostfix(typeof(MarkingManager), nameof(MarkingManager.NetInfoInitNodeInfoPostfix_Rail), typeof(NetInfo), "InitNodeInfo");
        }
        private bool Patch_NetInfo_NodeInitNodeInfo_LevelCrossing()
        {
            return AddPostfix(typeof(MarkingManager), nameof(MarkingManager.NetInfoInitNodeInfoPostfix_LevelCrossing), typeof(NetInfo), "InitNodeInfo");
        }
        private bool Patch_NetInfo_InitSegmentInfo()
        {
            return AddPostfix(typeof(MarkingManager), nameof(MarkingManager.NetInfoInitSegmentInfoPostfix), typeof(NetInfo), "InitSegmentInfo");
        }

        #endregion

        #region LOADING

        private void PatchLoading(ref bool success)
        {
            if (Settings.LoadMarkingAssets)
            {
                success &= Patch_LoadingManager_LoadCustomContent();
                success &= Patch_LoadingScreenMod_LoadImpl();
                success &= Patch_LoadingScreenMod_UsedAssets();
                success &= Patch_PackageHelper_ResolveLegacyTypeHandler();
            }
        }
        private bool Patch_LoadingManager_LoadCustomContent()
        {
            var nestedType = typeof(LoadingManager).GetNestedTypes(AccessTools.all).FirstOrDefault(t => t.FullName.Contains("LoadCustomContent"));
            return AddTranspiler(typeof(Mod), nameof(Mod.LoadingManagerLoadCustomContentTranspiler), nestedType, "MoveNext");
        }

        private const int gameObjectVarIndex = 38;
        private const int component7VarIndex = 45;
        private const int flagVarIndex = 46;
        private static IEnumerable<CodeInstruction> LoadingManagerLoadCustomContentTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions)
        {
            var newInstructions = new List<CodeInstruction>(instructions);

            var index = 0;
            LocalBuilder markingLocal = null;
            for (; index < newInstructions.Count; index += 1)
            {
                var instruction = newInstructions[index];

                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand is LocalBuilder local && local.LocalIndex == component7VarIndex)
                {
                    markingLocal = generator.DeclareLocal(typeof(MarkingInfo));
                    newInstructions.Insert(++index, new CodeInstruction(OpCodes.Ldloc_S, gameObjectVarIndex));
                    newInstructions.Insert(++index, new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(GameObject), nameof(GameObject.GetComponent), new Type[0], new Type[] { typeof(MarkingInfo) })));
                    newInstructions.Insert(++index, new CodeInstruction(OpCodes.Stloc_S, markingLocal));
                    break;
                }
            }

            if (markingLocal == null)
                return newInstructions;

            bool lastIfFound = false;
            for (index += 1; index < newInstructions.Count; index += 1)
            {
                var instruction = newInstructions[index];
                if (instruction.opcode == OpCodes.Ldloc_S && instruction.operand is LocalBuilder local && local.LocalIndex == component7VarIndex)
                {
                    lastIfFound = true;
                    break;
                }
            }

            if (!lastIfFound)
                return newInstructions;

            bool elseJumpFound = false;
            Label elseLabel = default;
            Label newElseLabel = default;
            for (index += 1; index < newInstructions.Count; index += 1)
            {
                var instruction = newInstructions[index];

                if (instruction.opcode == OpCodes.Brfalse)
                {
                    elseLabel = (Label)instruction.operand;
                    elseJumpFound = true;
                    newElseLabel = generator.DefineLabel();
                    instruction.operand = newElseLabel;
                    break;
                }
            }

            if (!elseJumpFound)
                return newInstructions;

            bool added = false;
            Label endLabel = default;
            for (index += 1; index < newInstructions.Count; index += 1)
            {
                var instruction = newInstructions[index];

                if (instruction.opcode == OpCodes.Br)
                {
                    var newInstruction = new CodeInstruction(OpCodes.Ldloc_S, markingLocal);
                    newInstruction.labels.Add(newElseLabel);
                    newInstructions.Insert(++index, newInstruction);
                    newInstructions.Insert(++index, new CodeInstruction(OpCodes.Ldnull));
                    newInstructions.Insert(++index, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Object), "op_Inequality")));
                    newInstructions.Insert(++index, new CodeInstruction(OpCodes.Brfalse, elseLabel));

                    newInstructions.Insert(++index, new CodeInstruction(OpCodes.Ldloc_S, markingLocal));
                    newInstructions.Insert(++index, new CodeInstruction(OpCodes.Ldarg_0));
                    var type = typeof(LoadingManager).GetNestedTypes(AccessTools.all).FirstOrDefault(t => t.FullName.Contains("LoadCustomContent"));
                    var field = AccessTools.Field(type, "<metaData>__4");
                    newInstructions.Insert(++index, new CodeInstruction(OpCodes.Ldfld, field));
                    newInstructions.Insert(++index, new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CustomAssetMetaData), nameof(CustomAssetMetaData.assetRef))));
                    newInstructions.Insert(++index, new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(Loader), nameof(Loader.LoadTemplateAsset))));

                    endLabel = generator.DefineLabel();
                    newInstructions.Insert(++index, new CodeInstruction(OpCodes.Br, endLabel));
                    added = true;
                    break;
                }
            }

            if (!added)
                return newInstructions;

            for (index += 1; index < newInstructions.Count; index += 1)
            {
                var instruction = newInstructions[index];

                if (instruction.opcode == OpCodes.Ldloc_S && instruction.operand is LocalBuilder local && local.LocalIndex == flagVarIndex)
                {
                    instruction.labels.Add(endLabel);
                    break;
                }
            }

            return newInstructions;
        }

        private bool Patch_LoadingScreenMod_LoadImpl()
        {
            var lsmFound = false;
            var success = true;
            {
                if (AccessTools.TypeByName("LoadingScreenMod.AssetLoader") is Type type)
                {
                    lsmFound = true;
                    success &= AddTranspiler(typeof(Mod), nameof(Mod.LoadingScreenModLoadImplTranspiler), type, "LoadImpl");
                }
            }
            {
                if (AccessTools.TypeByName("LoadingScreenModTest.AssetLoader") is Type type)
                {
                    lsmFound = true;
                    success &= AddTranspiler(typeof(Mod), nameof(Mod.LoadingScreenModLoadImplTranspiler), type, "LoadImpl");
                }
            }
            {
                if (AccessTools.TypeByName("LoadingScreenModRevisited.AssetLoader") is Type type)
                {
                    lsmFound = true;
                    success &= AddTranspiler(typeof(Mod), nameof(Mod.LoadingScreenModLoadImplTranspiler), type, "LoadImpl");
                }
            }

            if (lsmFound)
                return success;
            else
            {
                Logger.Error($"LSM is not found, patch skiped");
                return true;
            }
        }
        private const int component6VarIndex = 13;
        private static IEnumerable<CodeInstruction> LoadingScreenModLoadImplTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions)
        {
            var newInstructions = new List<CodeInstruction>(instructions);

            var index = 0;
            bool lastIfFound = false;
            for (; index < newInstructions.Count; index += 1)
            {
                var instruction = newInstructions[index];
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand is LocalBuilder local && local.LocalIndex == component6VarIndex)
                {
                    lastIfFound = true;
                    break;
                }
            }

            if (!lastIfFound)
                return newInstructions;

            bool elseJumpFound = false;
            Label elseLabel = default;
            Label newElseLabel = default;
            for (index += 1; index < newInstructions.Count; index += 1)
            {
                var instruction = newInstructions[index];

                if (instruction.opcode == OpCodes.Brfalse_S)
                {
                    elseLabel = (Label)instruction.operand;
                    elseJumpFound = true;
                    newElseLabel = generator.DefineLabel();
                    instruction.operand = newElseLabel;
                    break;
                }
            }

            if (!elseJumpFound)
                return newInstructions;

            bool added = false;
            Label endLabel = default;
            for (index += 1; index < newInstructions.Count; index += 1)
            {
                var instruction = newInstructions[index];

                if (instruction.opcode == OpCodes.Br_S)
                {
                    var newInstruction = new CodeInstruction(OpCodes.Ldloc_1);
                    newInstruction.labels.Add(newElseLabel);
                    newInstructions.Insert(++index, newInstruction);
                    newInstructions.Insert(++index, new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(UnityEngine.GameObject), nameof(UnityEngine.GameObject.GetComponent), new Type[0], new Type[] { typeof(IMT.Manager.MarkingInfo) })));
                    newInstructions.Insert(++index, new CodeInstruction(OpCodes.Dup));

                    var markingLocal = generator.DeclareLocal(typeof(IMT.Manager.MarkingInfo));
                    newInstructions.Insert(++index, new CodeInstruction(OpCodes.Stloc_S, markingLocal));
                    newInstructions.Insert(++index, new CodeInstruction(OpCodes.Ldnull));
                    newInstructions.Insert(++index, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UnityEngine.Object), "op_Inequality")));
                    newInstructions.Insert(++index, new CodeInstruction(OpCodes.Brfalse_S, elseLabel));

                    newInstructions.Insert(++index, new CodeInstruction(OpCodes.Ldloc_S, markingLocal));
                    newInstructions.Insert(++index, new CodeInstruction(OpCodes.Ldarg_1));
                    newInstructions.Insert(++index, new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(Loader), nameof(Loader.LoadTemplateAsset))));

                    endLabel = generator.DefineLabel();
                    newInstructions.Insert(++index, new CodeInstruction(OpCodes.Br_S, endLabel));
                    added = true;
                    break;
                }
            }

            if (!added)
                return newInstructions;

            for (index += 1; index < newInstructions.Count; index += 1)
            {
                var instruction = newInstructions[index];

                if (instruction.opcode == OpCodes.Ldarg_0)
                {
                    instruction.labels.Add(endLabel);
                    break;
                }
            }

            return newInstructions;
        }

        private bool Patch_LoadingScreenMod_UsedAssets()
        {
            if (AccessTools.TypeByName("LoadingScreenModRevisited.UsedAssets") is Type type)
                return AddPostfix(typeof(Mod), nameof(Mod.LoadingScreenMod_UsedAssets_Postfix), type);
            else
            {
                Logger.Error($"LSM is not found, patch skiped");
                return true;
            }
        }
        private static void LoadingScreenMod_UsedAssets_Postfix(HashSet<string> ____netAssets, HashSet<string> ____propAssets, HashSet<string> ____treeAssets)
        {
            if (SingletonItem<SerializableDataExtension>.Exist)
                SingletonItem<SerializableDataExtension>.Instance.SetUsedPrefabs(____netAssets, ____propAssets, ____treeAssets);
        }

        private bool Patch_PackageHelper_ResolveLegacyTypeHandler()
        {
            return AddPrefix(typeof(Mod), nameof(Mod.PackageHelperResolveLegacyTypeHandlerPrefix), typeof(PackageHelper), nameof(PackageHelper.ResolveLegacyTypeHandler));
        }
        private static bool PackageHelperResolveLegacyTypeHandlerPrefix(string type, ref string __result)
        {
            if (type.StartsWith("NodeMarkup.Manager.MarkingInfo"))
            {
                __result = typeof(MarkingInfo).AssemblyQualifiedName;
                return false;
            }
            else
                return true;
        }

        #endregion

        #region OTHERS

        private bool Patch_ThemeMixer_TerrainTexture()
        {
            if (AccessTools.TypeByName("ThemeMixer.Themes.Terrain.TerrainTexture") is Type type)
                return AddPostfix(typeof(Mod), nameof(Mod.ThemeMixer_TerrainTexture_Postfix), type, "LoadValue");
            else
            {
                Logger.Error($"Theme mixer is not found, patch skiped");
                return true;
            }
        }
        private static void ThemeMixer_TerrainTexture_Postfix()
        {
            if (SingletonManager<NodeMarkingManager>.Exist)
            {
                for (int i = 0; i < NetManager.MAX_NODE_COUNT; i += 1)
                {
                    if (SingletonManager<NodeMarkingManager>.Instance.TryGetMarking((ushort)i, out var marking))
                    {
                        foreach (var filler in marking.Fillers)
                        {
                            if (filler.Style.Value is IThemeFiller)
                                marking.Update(filler, true, false);
                        }
                    }
                }
            }

            if (SingletonManager<SegmentMarkingManager>.Exist)
            {
                for (int i = 0; i < NetManager.MAX_SEGMENT_COUNT; i += 1)
                {
                    if (SingletonManager<SegmentMarkingManager>.Instance.TryGetMarking((ushort)i, out var marking))
                    {
                        foreach (var filler in marking.Fillers)
                        {
                            if (filler.Style.Value is IThemeFiller)
                                marking.Update(filler, true, false);
                        }
                    }
                }
            }
        }

        #endregion

        #endregion
    }

    public static class Patcher
    {
        public static IEnumerable<CodeInstruction> ToolControllerAwakeTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions) => ModsCommon.Patcher.ToolControllerAwakeTranspiler<Mod, IntersectionMarkingTool>(generator, instructions);

        public static void GeneratedScrollPanelCreateOptionPanelPostfix(string templateName, ref OptionPanelBase __result) => SingletonTool<IntersectionMarkingTool>.Instance.CreateButton<NodeMarkingButton>(templateName, ref __result, ModsCommon.Patcher.RoadsOptionPanel);

        public static IEnumerable<CodeInstruction> GameKeyShortcutsEscapeTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions) => ModsCommon.Patcher.GameKeyShortcutsEscapeTranspiler<Mod, IntersectionMarkingTool>(generator, instructions);

        public static void LoadAssetPanelOnLoadPostfix(LoadAssetPanel __instance, UIListBox ___m_SaveList) => ModsCommon.Patcher.LoadAssetPanelOnLoadPostfix<BuildingAssetDataExtension>(__instance, ___m_SaveList);

        public static IEnumerable<CodeInstruction> BuildingDecorationLoadPathsTranspiler(IEnumerable<CodeInstruction> instructions) => ModsCommon.Patcher.BuildingDecorationLoadPathsTranspiler<BuildingAssetDataExtension>(instructions);

        public static void NetManagerCreateSegmentPostfix(NetInfo info, ushort segment, ushort startNode, ushort endNode) => IntersectionMarkingTool.ApplyDefaultMarking(info, segment, startNode, endNode);
    }
}
