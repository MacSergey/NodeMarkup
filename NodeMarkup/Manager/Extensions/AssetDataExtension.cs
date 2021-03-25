using ColossalFramework;
using ColossalFramework.UI;
using HarmonyLib;
using ICities;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace NodeMarkup
{
    public class AssetDataExtension : AssetDataExtensionBase
    {
        private static string DataId => $"{Loader.Id}.Data";
        private static string MapId => $"{Loader.Id}.Map";

        public static AssetDataExtension Instance { get; private set; }

        private static Dictionary<BuildingInfo, AssetMarking> AssetMarkings { get; } = new Dictionary<BuildingInfo, AssetMarking>();
        public override void OnCreated(IAssetData assetData)
        {
            base.OnCreated(assetData);
            Instance = this;
        }

        public override void OnReleased() => Instance = null;
        public override void OnAssetLoaded(string name, object asset, Dictionary<string, byte[]> userData)
        {
            if (asset is not BuildingInfo prefab || userData == null || !userData.TryGetValue(DataId, out byte[] data) || !userData.TryGetValue(MapId, out byte[] map))
                return;

            Mod.Logger.Debug($"Start load prefab data \"{prefab.name}\"");
            try
            {
                var decompress = Loader.Decompress(data);
                var config = XmlExtension.Parse(decompress);

                var count = map.Length / 6;
                var segments = new ushort[count];
                var nodes = new ushort[count * 2];

                for (var i = 0; i < count; i += 1)
                {
                    segments[i] = GetUShort(map[i * 6], map[i * 6 + 1]);
                    nodes[i * 2] = GetUShort(map[i * 6 + 2], map[i * 6 + 3]);
                    nodes[i * 2 + 1] = GetUShort(map[i * 6 + 4], map[i * 6 + 5]);
                }

                AssetMarkings[prefab] = new AssetMarking(config, segments, nodes);

                Mod.Logger.Debug($"Prefab data was loaded; Size = {data.Length} bytes");
            }
            catch (Exception error)
            {
                Mod.Logger.Error("Could not load prefab data", error);
            }
        }
        public override void OnAssetSaved(string name, object asset, out Dictionary<string, byte[]> userData)
        {
            userData = new Dictionary<string, byte[]>();
            if (asset is not BuildingInfo prefab || !prefab.m_paths.Any())
                return;

            Mod.Logger.Debug($"Start save prefab data \"{prefab.name}\"");
            try
            {
                var config = Loader.GetString(MarkupManager.ToXml());
                var data = Loader.Compress(config);

                userData[DataId] = data;

                var instance = Singleton<NetManager>.instance;

                var segmentsId = new List<ushort>();
                for (ushort i = 0; i < NetManager.MAX_SEGMENT_COUNT; i += 1)
                {
                    if ((instance.m_segments.m_buffer[i].m_flags & NetSegment.Flags.Created) == NetSegment.Flags.Created)
                        segmentsId.Add(i);
                }

                var map = new byte[sizeof(ushort) * 3 * segmentsId.Count];

                for (var i = 0; i < segmentsId.Count; i += 1)
                {
                    var segmentId = segmentsId[i];
                    var segment = instance.m_segments.m_buffer[segmentId];
                    GetBytes(segmentId, out map[i * 6], out map[i * 6 + 1]);
                    GetBytes(segment.m_startNode, out map[i * 6 + 2], out map[i * 6 + 3]);
                    GetBytes(segment.m_endNode, out map[i * 6 + 4], out map[i * 6 + 5]);
                }

                userData[MapId] = map;

                Mod.Logger.Debug($"Prefab data was saved; Size = {data.Length} bytes");
            }
            catch (Exception error)
            {
                Mod.Logger.Error("Could not save prefab data", error);
            }
        }
        private void GetBytes(ushort n, out byte b1, out byte b2)
        {
            b1 = (byte)(n >> 8);
            b2 = (byte)n;
        }
        private ushort GetUShort(byte b1, byte b2) => (ushort)((b1 << 8) + b2);

        public static bool TryGetValue(BuildingInfo buildingInfo, out AssetMarking assetMarking) => AssetMarkings.TryGetValue(buildingInfo, out assetMarking);

        public static void LoadAssetPanelOnLoadPostfix(LoadAssetPanel __instance, UIListBox ___m_SaveList)
        {
            if (AccessTools.Method(typeof(LoadSavePanelBase<CustomAssetMetaData>), "GetListingMetaData") is not MethodInfo method)
                return;

            var listingMetaData = (CustomAssetMetaData)method.Invoke(__instance, new object[] { ___m_SaveList.selectedIndex });
            if (listingMetaData.userDataRef != null)
            {
                var userAssetData = (listingMetaData.userDataRef.Instantiate() as AssetDataWrapper.UserAssetData) ?? new AssetDataWrapper.UserAssetData();
                Instance.OnAssetLoaded(listingMetaData.name, ToolsModifierControl.toolController.m_editPrefabInfo, userAssetData.Data);
            }
        }
    }
    public struct AssetMarking
    {
        public XElement Config { get; }
        private ushort[] Segments { get; }
        private ushort[] Nodes { get; }

        public AssetMarking(XElement config, ushort[] segments, ushort[] nodes)
        {
            Config = config;
            Segments = segments;
            Nodes = nodes;
        }

        public ObjectsMap GetMap(ushort[] targetSegments, ushort[] tagretNodes)
        {
            var map = new ObjectsMap();

            var segmentsCount = Math.Min(Segments.Length, targetSegments.Length);
            for (var i = 0; i < segmentsCount; i += 1)
                map.AddSegment(Segments[i], targetSegments[i]);

            var nodesCount = Math.Min(Nodes.Length, tagretNodes.Length);
            for (var i = 0; i < nodesCount; i += 1)
                map.AddNode(Nodes[i], tagretNodes[i]);

            return map;
        }
    }
}
