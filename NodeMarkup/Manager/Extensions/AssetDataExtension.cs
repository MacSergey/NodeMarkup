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
    public class BuildingAssetDataExtension : BaseBuildingAssetDataExtension<Mod, BuildingAssetDataExtension, ObjectsMap>
    {
        protected override string DataId { get; } = $"{Loader.Id}.Data";
        protected override string MapId { get; } = $"{Loader.Id}.Map";

        protected override ObjectsMap CreateMap(bool isSimple) => new ObjectsMap(isSimple: isSimple);
        protected override XElement GetConfig() => MarkupManager.ToXml();

        protected override void PlaceAsset(XElement config, ObjectsMap map) => MarkupManager.FromXml(config, map, true);
    }
    public class NetworkAssetDataExtension : BaseNetworkAssetDataExtension<Mod, NetworkAssetDataExtension, ObjectsMap>
    {
        protected override string DataId { get; } = $"{Loader.Id}.Data";
        protected override string MapId { get; } = $"{Loader.Id}.Map";

        protected override ObjectsMap CreateMap(bool isSimple) => new ObjectsMap(isSimple: isSimple);
        protected override XElement GetConfig(NetInfo prefab, out ushort segmentId, out ushort startNodeId, out ushort endNodeId)
        {
            foreach(var markup in SingletonManager<SegmentMarkupManager>.Instance)
            {
                segmentId = markup.Id;
                ref var segment = ref segmentId.GetSegment();
                if (segment.Info == prefab)
                {
                    startNodeId = segment.m_startNode;
                    endNodeId = segment.m_endNode;
                    var config = new XElement(nameof(NodeMarkup));
                    config.AddAttr("V", SingletonMod<Mod>.Version);
                    config.Add(markup.ToXml());
                    return config;
                }
            }

            segmentId = 0;
            startNodeId = 0;
            endNodeId = 0;
            return null;
        }
        protected override void PlaceAsset(XElement config, ObjectsMap map)
        {
            MarkupManager.FromXml(config, map, true);
        }
    }
}
