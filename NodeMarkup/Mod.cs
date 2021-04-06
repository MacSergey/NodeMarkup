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
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine.SceneManagement;

namespace NodeMarkup
{
    public class Mod : BasePatcherMod<Mod>
    {
        #region PROPERTIES

        public static string StableURL { get; } = "https://steamcommunity.com/sharedfiles/filedetails/?id=2140418403";
        public static string BetaURL { get; } = "https://steamcommunity.com/sharedfiles/filedetails/?id=2159934925";
        public static string DiscordURL { get; } = "https://discord.gg/NnwhuBKMqj";
        public static string ReportBugUrl { get; } = "https://github.com/MacSergey/NodeMarkup/issues/new?assignees=&labels=NEW+ISSUE&template=bug_report.md";
        public static string WikiUrl { get; } = "https://github.com/MacSergey/NodeMarkup/wiki";
        public static string TroubleshootingUrl { get; } = "https://github.com/MacSergey/NodeMarkup/wiki/Troubleshooting";

        protected override string IdRaw => nameof(NodeMarkup);
        public override List<Version> Versions { get; } = new List<Version>
        {
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
        public override string Description => !IsBeta ? Localize.Mod_Description : Localize.Mod_DescriptionBeta;
        public override string WorkshopUrl => StableURL;
        protected override string Locale => Settings.Locale.value;

#if BETA
        public override bool IsBeta => true;
#else
        protected override bool ModIsBeta => false;
#endif
        #endregion

        #region BASIC

        protected override void GetSettings(UIHelperBase helper) => Settings.OnSettingsUI(helper);

        public override void LocaleChanged()
        {
            Localize.Culture = Culture;
            Logger.Debug($"Current cultute - {Localize.Culture?.Name ?? "null"}");
        }

        public static bool OpenTroubleshooting()
        {
            Utilities.Utilities.OpenUrl(TroubleshootingUrl);
            return true;
        }
        public static bool GetStable()
        {
            Utilities.Utilities.OpenUrl(StableURL);
            return true;
        }

        public override void OnLoadedError()
        {
            var messageBox = MessageBoxBase.ShowModal<ErrorLoadedMessageBox>();
            messageBox.MessageText = Localize.Mod_LoaledWithErrors;
        }

        #endregion

        #region ADDITIONAL

        public void ShowWhatsNew()
        {
            var fromVersion = new Version(Settings.WhatsNewVersion);

            if (!Settings.ShowWhatsNew || Version <= fromVersion)
                return;

            var messages = GetWhatsNewMessages(fromVersion);
            if (!messages.Any())
                return;

            if (!IsBeta)
            {
                var messageBox = MessageBoxBase.ShowModal<WhatsNewMessageBox>();
                messageBox.CaptionText = string.Format(Localize.Mod_WhatsNewCaption, NameRaw);
                messageBox.OnButtonClick = Confirm;
                messageBox.OkText = NodeMarkupMessageBox.Ok;
                messageBox.Init(messages, GetVersionString);
            }
            else
            {
                var messageBox = MessageBoxBase.ShowModal<BetaWhatsNewMessageBox>();
                messageBox.CaptionText = string.Format(Localize.Mod_WhatsNewCaption, NameRaw);
                messageBox.OnButtonClick = Confirm;
                messageBox.OnGetStableClick = GetStable;
                messageBox.OkText = NodeMarkupMessageBox.Ok;
                messageBox.GetStableText = Localize.Mod_BetaWarningGetStable;
                messageBox.Init(messages, string.Format(Localize.Mod_BetaWarningMessage, NameRaw), GetVersionString);
            }

            static bool Confirm()
            {
                Settings.WhatsNewVersion.value = SingletonMod<Mod>.Version.ToString();
                return true;
            }
            static bool GetStable()
            {
                Mod.GetStable();
                return true;
            }
        }
        public Dictionary<Version, string> GetWhatsNewMessages(Version whatNewVersion)
        {
            var messages = new Dictionary<Version, string>(Versions.Count);
#if BETA
            messages[Version] = Localize.Mod_WhatsNewMessageBeta;
#endif
            foreach (var version in Versions)
            {
                if (Version < version)
                    continue;

                if (version <= whatNewVersion)
                    break;

                if (Settings.ShowOnlyMajor && !version.IsMinor())
                    continue;

                if (GetWhatsNew(version) is string message && !string.IsNullOrEmpty(message))
                    messages[version] = message;
            }

            return messages;
        }
        public string GetVersionString(Version version) => string.Format(Localize.Mod_WhatsNewVersion, version == Version ? VersionString : version.ToString());
        private string GetWhatsNew(Version version) => Localize.ResourceManager.GetString($"Mod_WhatsNewMessage{version.ToString().Replace('.', '_')}", Localize.Culture);

        public void ShowBetaWarning()
        {
            if (!IsBeta)
                Settings.BetaWarning.value = true;
            else if (Settings.BetaWarning.value)
            {
                var messageBox = MessageBoxBase.ShowModal<TwoButtonMessageBox>();
                messageBox.CaptionText = Localize.Mod_BetaWarningCaption;
                messageBox.MessageText = string.Format(Localize.Mod_BetaWarningMessage, NameRaw);
                messageBox.Button1Text = Localize.Mod_BetaWarningAgree;
                messageBox.Button2Text = Localize.Mod_BetaWarningGetStable;
                messageBox.OnButton1Click = AgreeClick;
                messageBox.OnButton2Click = GetStable;

                static bool AgreeClick()
                {
                    Settings.BetaWarning.value = false;
                    return true;
                }
            }
        }
        public void ShowLoadError()
        {
            if (MarkupManager.HasErrors)
            {
                var messageBox = MessageBoxBase.ShowModal<ErrorLoadedMessageBox>();
                messageBox.MessageText = MarkupManager.Errors > 0 ? string.Format(Localize.Mod_LoadFailed, MarkupManager.Errors) : Localize.Mod_LoadFailedAll;
            }
        }

        #endregion

        #region PATCHES

        protected override bool PatchProcess()
        {
            var success = true;

            success &= AddTool<NodeMarkupTool>();
            success &= AddNetToolButton<NodeMarkupButton>();
            success &= ToolOnEscape<NodeMarkupTool>();
            success &= AssetDataExtensionFix<AssetDataExtension>();

            PatchNetManager(ref success);
            PatchNetNode(ref success);
            PatchNetSegment(ref success);
            PatchNetInfo(ref success);
            PatchLoading(ref success);

            success &= Patch_BuildingDecoration_LoadPaths();

            return success;
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
                Logger.Warning($"LSM not founded, patch skip");
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

        #region OTHERS

        private bool Patch_BuildingDecoration_LoadPaths()
        {
            return AddTranspiler(typeof(Mod), nameof(Mod.BuildingDecorationLoadPathsTranspiler), typeof(BuildingDecoration), nameof(BuildingDecoration.LoadPaths));
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


        #endregion

        #endregion
    }
}
