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
    public class AssetDataExtension : BaseIntersectionAssetDataExtension<Mod, AssetDataExtension>
    {
        protected override string DataId { get; } = $"{Loader.Id}.Data";
        protected override string MapId { get; } = $"{Loader.Id}.Map";

        protected override XElement GetConfig() => MarkupManager.ToXml();
        protected override void PlaceAsset(AssetData data, FastList<ushort> segments, FastList<ushort> nodes)
        {
            var map = new ObjectsMap(isSimple: true);

            var segmentsCount = Math.Min(data.Segments.Length, segments.m_buffer.Length);
            for (var i = 0; i < segmentsCount; i += 1)
                map.AddSegment(data.Segments[i], segments[i]);

            var nodesCount = Math.Min(data.Nodes.Length, nodes.m_buffer.Length);
            for (var i = 0; i < nodesCount; i += 1)
                map.AddNode(data.Nodes[i], nodes[i]);

            MarkupManager.FromXml(data.Config, map, false);
        }
    }
}
