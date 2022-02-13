using ColossalFramework.UI;
using HarmonyLib;
using ICities;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using NodeMarkup.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Resources;
using UnityEngine.SceneManagement;

namespace NodeMarkup
{
    public class Mod : BasePatcherMod<Mod>
    {
        #region PROPERTIES

        public static string ReportBugUrl { get; } = "https://github.com/MacSergey/NodeMarkup/issues/new?assignees=&labels=NEW+ISSUE&template=bug_report.md";
        public static string WikiUrl { get; } = "https://github.com/MacSergey/NodeMarkup/wiki";
        public static string TroubleshootingUrl { get; } = "https://github.com/MacSergey/NodeMarkup/wiki/Troubleshooting";

        protected override string IdRaw => nameof(NodeMarkup);
        public override List<Version> Versions { get; } = new List<Version>
        {
            new Version("1.8.2"),
            new Version("1.8.1"),
            new Version("1.8"),
            new Version("1.7.4"),
            new Version("1.7.3"),
            new Version("1.7.2"),
            new Version("1.7.1"),
            new Version("1.7"),
            new Version("1.6"),
            new Version("1.5.3"),
            new Version("1.5.2"),
            new Version("1.5.1"),
            new Version("1.5"),
            new Version("1.4.1"),
            new Version("1.4"),
            new Version("1.3"),
            new Version("1.2.1"),
            new Version("1.2"),
            new Version("1.1"),
            new Version("1.0")
        };

        public override string NameRaw => "Intersection Marking Tool";
        public override string Description => !IsBeta ? Localize.Mod_Description : CommonLocalize.Mod_DescriptionBeta;
        protected override ulong StableWorkshopId => 2140418403ul;
        protected override ulong BetaWorkshopId => 2159934925ul;
        protected override string ModSupportUrl => TroubleshootingUrl;

#if BETA
        public override bool IsBeta => true;
#else
        public override bool IsBeta => false;
#endif
        #endregion

        protected override ResourceManager LocalizeManager => Localize.ResourceManager;
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

            success &= AddTool();
            success &= AddNetToolButton();
            success &= ToolOnEscape();
            success &= AssetDataExtensionFix();
            success &= AssetDataLoad();

            PatchNetManager(ref success);
            PatchNetNode(ref success);
            PatchNetSegment(ref success);
            PatchNetInfo(ref success);
            PatchLoading(ref success);

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
        private bool AssetDataExtensionFix()
        {
            return AddPostfix(typeof(Patcher), nameof(Patcher.LoadAssetPanelOnLoadPostfix), typeof(LoadAssetPanel), nameof(LoadAssetPanel.OnLoad));
        }
        private bool AssetDataLoad()
        {
            return AddTranspiler(typeof(Patcher), nameof(Patcher.BuildingDecorationLoadPathsTranspiler), typeof(BuildingDecoration), nameof(BuildingDecoration.LoadPaths));
        }

        #endregion

        #region NETMANAGER

        private void PatchNetManager(ref bool success)
        {
            success &= Patch_NetManagerRelease_NodeImplementation();
            success &= Patch_NetManagerReleas_SegmentImplementation();
            success &= Patch_NetManager_SimulationStepImpl_Prefix();
            success &= Patch_NetManager_SimulationStepImpl_Postfix();
            success &= Patch_NetManager_EndOverlay_Prefix();
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
        private bool Patch_NetManager_SimulationStepImpl_Prefix()
        {
            return AddPrefix(typeof(MarkupManager), nameof(MarkupManager.GetToUpdate), typeof(NetManager), "SimulationStepImpl");
        }
        private bool Patch_NetManager_SimulationStepImpl_Postfix()
        {
            return AddPostfix(typeof(MarkupManager), nameof(MarkupManager.Update), typeof(NetManager), "SimulationStepImpl");
        }
        private bool Patch_NetManager_EndOverlay_Prefix()
        {
            return AddPrefix(typeof(Mod), nameof(Mod.NetManagerEndOverlay), typeof(NetManager), "EndOverlayImpl");
        }
        private static bool NetManagerEndOverlay()
        {
            return !SingletonTool<NodeMarkupTool>.Instance.enabled || !Settings.HideStreetName;
        }

        #endregion

        #region NETNODE

        private void PatchNetNode(ref bool success)
        {
            success &= Patch_NetNode_RenderInstance();
            success &= Patch_NetNode_CheckHeightOffset();
        }
        private bool Patch_NetNode_RenderInstance()
        {
            var parameters = new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(NetInfo), typeof(int), typeof(NetNode.Flags), typeof(uint).MakeByRefType(), typeof(RenderManager.Instance).MakeByRefType() };
            return AddPostfix(typeof(MarkupManager), nameof(MarkupManager.NetNodeRenderInstancePostfix), typeof(NetNode), nameof(NetNode.RenderInstance), parameters);
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
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MarkupManager), nameof(MarkupManager.UpdateNode)));
                    yield return instruction;
                }
                else if(instruction.opcode == OpCodes.Call && instruction.operand == updateLanes)
                {
                    yield return instruction;
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 13);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MarkupManager), nameof(MarkupManager.UpdateSegment)));
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
        }
        private bool Patch_NetSegment_RenderInstance()
        {
            var parameters = new Type[] { typeof(RenderManager.CameraInfo), typeof(ushort), typeof(int), typeof(NetInfo), typeof(RenderManager.Instance).MakeByRefType() };
            return AddPostfix(typeof(MarkupManager), nameof(MarkupManager.NetSegmentRenderInstancePostfix), typeof(NetSegment), nameof(NetSegment.RenderInstance), parameters);
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
            if(Settings.LevelCrossingUnderMarking)
                success &= Patch_NetInfo_NodeInitNodeInfo_LevelCrossing();

        }
        private bool Patch_NetInfo_NodeInitNodeInfo_Rail()
        {
            return AddPostfix(typeof(MarkupManager), nameof(MarkupManager.NetInfoInitNodeInfoPostfix_Rail), typeof(NetInfo), "InitNodeInfo");
        }
        private bool Patch_NetInfo_NodeInitNodeInfo_LevelCrossing()
        {
            return AddPostfix(typeof(MarkupManager), nameof(MarkupManager.NetInfoInitNodeInfoPostfix_LevelCrossing), typeof(NetInfo), "InitNodeInfo");
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
            return AddTranspiler(typeof(Mod), nameof(Mod.LoadingManagerLoadCustomContentTranspiler), nestedType, "MoveNext");
        }

        private static IEnumerable<CodeInstruction> LoadingManagerLoadCustomContentTranspiler(IEnumerable<CodeInstruction> instructions)
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
                Logger.Error($"LSM not founded, patch skip");
                return true;
            }
            else
                return AddTranspiler(typeof(Mod), nameof(Mod.LoadingScreenModLoadImplTranspiler), type, "LoadImpl");
        }
        private static IEnumerable<CodeInstruction> LoadingScreenModLoadImplTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var additional = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Ldarg_1),
            };

            return LoadingTranspiler(instructions, OpCodes.Stloc_S, 12, additional);
        }
        private static IEnumerable<CodeInstruction> LoadingTranspiler(IEnumerable<CodeInstruction> instructions, OpCode startOc, int startVarIndex, CodeInstruction[] additional)
        {
            var enumerator = instructions.GetEnumerator();

            while (enumerator.MoveNext())
            {
                var instruction = enumerator.Current;
                yield return instruction;

                if (instruction.opcode == startOc && instruction.operand is LocalBuilder local && local.LocalIndex == startVarIndex)
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

        #endregion
    }

    public static class Patcher
    {
        public static IEnumerable<CodeInstruction> ToolControllerAwakeTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions) => ModsCommon.Patcher.ToolControllerAwakeTranspiler<Mod, NodeMarkupTool>(generator, instructions);

        public static void GeneratedScrollPanelCreateOptionPanelPostfix(string templateName, ref OptionPanelBase __result) => ModsCommon.Patcher.GeneratedScrollPanelCreateOptionPanelPostfix<Mod, NodeMarkupButton>(templateName, ref __result, ModsCommon.Patcher.RoadsOptionPanel);

        public static IEnumerable<CodeInstruction> GameKeyShortcutsEscapeTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions) => ModsCommon.Patcher.GameKeyShortcutsEscapeTranspiler<Mod, NodeMarkupTool>(generator, instructions);

        public static void LoadAssetPanelOnLoadPostfix(LoadAssetPanel __instance, UIListBox ___m_SaveList) => ModsCommon.Patcher.LoadAssetPanelOnLoadPostfix<AssetDataExtension>(__instance, ___m_SaveList);

        public static IEnumerable<CodeInstruction> BuildingDecorationLoadPathsTranspiler(IEnumerable<CodeInstruction> instructions) => ModsCommon.Patcher.BuildingDecorationLoadPathsTranspiler<AssetDataExtension>(instructions);
    }
}
