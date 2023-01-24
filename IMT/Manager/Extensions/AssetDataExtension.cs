using IMT.Manager;
using IMT.Utilities;
using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace IMT
{
    public class BuildingAssetDataExtension : BaseBuildingAssetDataExtension<Mod, BuildingAssetDataExtension, ObjectsMap>
    {
        protected override string DataId { get; } = $"{Loader.Id}.Data";
        protected override string MapId { get; } = $"{Loader.Id}.Map";

        protected override ObjectsMap CreateMap(bool isSimple) => new ObjectsMap(isSimple: isSimple);
        protected override XElement GetConfig() => MarkingManager.ToXml();

        protected override void PlaceAsset(XElement config, ObjectsMap map) => MarkingManager.FromXml(config, map, true);
    }
    public class NetworkAssetDataExtension : BaseNetworkAssetDataExtension<Mod, NetworkAssetDataExtension, ObjectsMap>
    {
        protected override string DataId { get; } = $"{Loader.Id}.Data";
        protected string DataElevatedId { get; } = $"{Loader.Id}.Data_Elevated";
        protected string DataBridgeId { get; } = $"{Loader.Id}.Data_Bridge";
        protected string DataSlopeId { get; } = $"{Loader.Id}.Data_Slope";
        protected string DataTunnelId { get; } = $"{Loader.Id}.Data_Tunnel";
        protected override string MapId { get; } = $"{Loader.Id}.Map";
        protected string MapElevatedId { get; } = $"{Loader.Id}.Map_Elevated";
        protected string MapBridgeId { get; } = $"{Loader.Id}.Map_Bridge";
        protected string MapSlopeId { get; } = $"{Loader.Id}.Map_Slope";
        protected string MapTunnelId { get; } = $"{Loader.Id}.Map_Tunnel";

        protected override ObjectsMap CreateMap(bool isSimple) => new ObjectsMap(isSimple: isSimple);
        protected override XElement GetConfig(NetInfo prefab, out ushort segmentId, out ushort startNodeId, out ushort endNodeId)
        {
            foreach (var marking in SingletonManager<SegmentMarkingManager>.Instance)
            {
                segmentId = marking.Id;
                ref var segment = ref segmentId.GetSegment();
                if (segment.Info == prefab)
                {
                    startNodeId = segment.m_startNode;
                    endNodeId = segment.m_endNode;
                    var config = new XElement("NodeMarkup");
                    config.AddAttr("V", SingletonMod<Mod>.Version);
                    config.Add(marking.ToXml());
                    return config;
                }
            }

            segmentId = 0;
            startNodeId = 0;
            endNodeId = 0;
            return null;
        }
        public override void OnAssetLoaded(string name, object asset, Dictionary<string, byte[]> userData)
        {
            if (asset is NetInfo prefab && userData != null && Load(prefab, userData, out var data))
            {
                AssetDatas[prefab] = data;

                if (prefab.m_netAI != null)
                {
                    if (GetNetInfo(prefab.m_netAI, nameof(RoadAI.m_elevatedInfo), out var elevatedInfo))
                    {
                        if (Load(prefab, userData, out var elevatedData, DataElevatedId, MapElevatedId))
                            AssetDatas[elevatedInfo] = elevatedData;
                        else
                            AssetDatas[elevatedInfo] = data;
                    }
                    if (GetNetInfo(prefab.m_netAI, nameof(RoadAI.m_bridgeInfo), out var bridgeInfo))
                    {
                        if (Load(prefab, userData, out var bridgeData, DataBridgeId, MapBridgeId))
                            AssetDatas[bridgeInfo] = bridgeData;
                        else
                            AssetDatas[bridgeInfo] = data;
                    }
                    if (GetNetInfo(prefab.m_netAI, nameof(RoadAI.m_slopeInfo), out var slopeInfo))
                    {
                        if (Load(prefab, userData, out var slopeData, DataSlopeId, MapSlopeId))
                            AssetDatas[slopeInfo] = slopeData;
                        else
                            AssetDatas[slopeInfo] = data;
                    }
                    if (GetNetInfo(prefab.m_netAI, nameof(RoadAI.m_tunnelInfo), out var tunnelInfo))
                    {
                        if (Load(prefab, userData, out var tunnelData, DataTunnelId, MapTunnelId))
                            AssetDatas[tunnelInfo] = tunnelData;
                        else
                            AssetDatas[tunnelInfo] = data;
                    }
                }
            }
        }
        public override void OnAssetSaved(string name, object asset, out Dictionary<string, byte[]> userData)
        {
            userData = new Dictionary<string, byte[]>();

            if (asset is NetInfo prefab)
            {
                Save(prefab, userData);

                if (prefab.m_netAI != null)
                {
                    if (GetNetInfo(prefab.m_netAI, nameof(RoadAI.m_elevatedInfo), out var elevatedInfo))
                        Save(elevatedInfo, userData, DataElevatedId, MapElevatedId);
                    if (GetNetInfo(prefab.m_netAI, nameof(RoadAI.m_bridgeInfo), out var bridgeInfo))
                        Save(bridgeInfo, userData, DataBridgeId, MapBridgeId);
                    if (GetNetInfo(prefab.m_netAI, nameof(RoadAI.m_slopeInfo), out var slopeInfo))
                        Save(slopeInfo, userData, DataSlopeId, MapSlopeId);
                    if (GetNetInfo(prefab.m_netAI, nameof(RoadAI.m_tunnelInfo), out var tunnelInfo))
                        Save(tunnelInfo, userData, DataTunnelId, MapTunnelId);
                }
            }
        }
        private bool GetNetInfo(NetAI ai, string fieldName, out NetInfo info)
        {
            var field = ai.GetType().GetField(fieldName);
            if (field != null)
            {
                info = field.GetValue(ai) as NetInfo;
                return info != null;
            }
            else
            {
                info = null;
                return false;
            }
        }
        public override void OnPlaceAsset(NetInfo networkInfo, ushort segmentId, ushort startNodeId, ushort endNodeId)
        {
            if (AssetDatas.TryGetValue(networkInfo, out var assetData))
            {
                SingletonMod<Mod>.Logger.Debug($"Place asset {networkInfo.name}");

                var version = MarkingManager.GetVersion(assetData.Config);

                if (assetData.Config.Element(SegmentMarking.XmlName) is XElement config)
                {
                    var map = CreateMap(true);
                    map.AddSegment(assetData.SegmentId, segmentId);
                    map.AddNode(assetData.StartNodeId, startNodeId);
                    map.AddNode(assetData.EndNodeId, endNodeId);

                    var marking = SingletonManager<SegmentMarkingManager>.Instance[segmentId];
                    marking.FromXml(version, config, map);
                }
            }
        }
        protected override void PlaceAsset(XElement config, ObjectsMap map)
        {
            throw new NotImplementedException();
        }
    }
}
