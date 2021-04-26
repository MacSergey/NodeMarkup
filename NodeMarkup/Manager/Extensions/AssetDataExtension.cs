using ColossalFramework;
using ColossalFramework.UI;
using HarmonyLib;
using ICities;
using ModsCommon;
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
    public class AssetDataExtension : BaseAssetDataExtension<AssetDataExtension, AssetMarking>
    {
        private static string DataId { get; } = $"{Loader.Id}.Data";
        private static string MapId { get; } = $"{Loader.Id}.Map";

        public override bool Load(BuildingInfo prefab, Dictionary<string, byte[]> userData, out AssetMarking markingData)
        {
            if (userData.TryGetValue(DataId, out byte[] data) && userData.TryGetValue(MapId, out byte[] map))
            {
                SingletonMod<Mod>.Logger.Debug($"Start load prefab data \"{prefab.name}\"");
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

                    markingData = new AssetMarking(config, segments, nodes);
                    SingletonMod<Mod>.Logger.Debug($"Prefab data was loaded; Size = {data.Length} bytes");
                    return true;
                }
                catch (Exception error)
                {
                    SingletonMod<Mod>.Logger.Error("Could not load prefab data", error);
                }
            }

            markingData = default;
            return false;
        }
        public override void Save(BuildingInfo prefab, Dictionary<string, byte[]> userData)
        {
            SingletonMod<Mod>.Logger.Debug($"Start save prefab data \"{prefab.name}\"");
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

                SingletonMod<Mod>.Logger.Debug($"Prefab data was saved; Size = {data.Length} bytes");
            }
            catch (Exception error)
            {
                SingletonMod<Mod>.Logger.Error("Could not save prefab data", error);
            }
        }
        protected override void PlaceAsset(AssetMarking data, FastList<ushort> segments, FastList<ushort> nodes)
        {
            var map = data.GetMap(segments.m_buffer, nodes.m_buffer);
            MarkupManager.FromXml(data.Config, map, false);
        }

        private void GetBytes(ushort n, out byte b1, out byte b2)
        {
            b1 = (byte)(n >> 8);
            b2 = (byte)n;
        }
        private ushort GetUShort(byte b1, byte b2) => (ushort)((b1 << 8) + b2);
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
