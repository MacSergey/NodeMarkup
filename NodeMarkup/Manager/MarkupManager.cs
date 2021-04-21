using ColossalFramework;
using ModsCommon;
using ModsCommon.Utilities;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using static NetInfo;

namespace NodeMarkup.Manager
{
    public static class MarkupManager
    {
        public static int Errors { get; set; } = 0;
        public static bool HasErrors => Errors != 0;

        public static void Clear()
        {
            SingletonManager<NodeMarkupManager>.Instance.Clear();
            SingletonManager<SegmentMarkupManager>.Instance.Clear();
        }

        public static void NetNodeRenderInstancePostfix(RenderManager.CameraInfo cameraInfo, ushort nodeID, ref RenderManager.Instance data) => SingletonManager<NodeMarkupManager>.Instance.Render(cameraInfo, nodeID, ref data);

        public static void NetSegmentRenderInstancePostfix(RenderManager.CameraInfo cameraInfo, ushort segmentID, ref RenderManager.Instance data) => SingletonManager<SegmentMarkupManager>.Instance.Render(cameraInfo, segmentID, ref data);

        public static void NetManagerReleaseNodeImplementationPrefix(ushort node) => SingletonManager<NodeMarkupManager>.Instance.Remove(node);
        public static void NetManagerReleaseSegmentImplementationPrefix(ushort segment) => SingletonManager<SegmentMarkupManager>.Instance.Remove(segment);
        public static void NetManagerUpdateNodePostfix(ushort node) => SingletonManager<NodeMarkupManager>.Instance.AddToUpdate(node);
        public static void NetManagerUpdateSegmentPostfix(ushort segment) => SingletonManager<SegmentMarkupManager>.Instance.AddToUpdate(segment);
        public static void NetSegmentUpdateLanesPostfix(ushort segmentID)
        {
            SingletonManager<SegmentMarkupManager>.Instance.AddToUpdate(segmentID);
            var segment = segmentID.GetSegment();
            SingletonManager<NodeMarkupManager>.Instance.AddToUpdate(segment.m_startNode);
            SingletonManager<NodeMarkupManager>.Instance.AddToUpdate(segment.m_endNode);
        }
        public static void NetManagerSimulationStepImplPostfix()
        {
            SingletonManager<NodeMarkupManager>.Instance.Update();
            SingletonManager<SegmentMarkupManager>.Instance.Update();
        }
        public static void NetInfoInitNodeInfoPostfix(Node info)
        {
            if (info.m_nodeMaterial.shader.name == "Custom/Net/TrainBridge")
                info.m_nodeMaterial.renderQueue = 2470;
        }
        public static void NetInfoInitSegmentInfoPostfix(Segment info)
        {
            if (info.m_segmentMaterial.shader.name == "Custom/Net/TrainBridge")
                info.m_segmentMaterial.renderQueue = 2470;
        }


        public static void PlaceIntersection(BuildingInfo buildingInfo, FastList<ushort> segments, FastList<ushort> nodes)
        {
            if (!AssetDataExtension.TryGetValue(buildingInfo, out AssetMarking assetMarking))
                return;

            var map = assetMarking.GetMap(segments.m_buffer, nodes.m_buffer);
            FromXml(assetMarking.Config, map, false);
        }

        public static void Import(XElement config) => FromXml(config, new ObjectsMap());
        public static XElement ToXml()
        {
            var config = new XElement(nameof(NodeMarkup));
            config.AddAttr("V", SingletonMod<Mod>.Version);

            Errors = 0;

            SingletonManager<NodeMarkupManager>.Instance.ToXml(config);
            SingletonManager<SegmentMarkupManager>.Instance.ToXml(config);

            return config;
        }
        public static void FromXml(XElement config, ObjectsMap map, bool clear = true)
        {
            if (clear)
                Clear();

            Errors = 0;

            var version = GetVersion(config);

            SingletonManager<NodeMarkupManager>.Instance.FromXml(config, map, version);
            SingletonManager<SegmentMarkupManager>.Instance.FromXml(config, map, version);
        }
        private static Version GetVersion(XElement config)
        {
            try { return new Version(config.Attribute("V").Value); }
            catch { return SingletonMod<Mod>.Version; }
        }
        public static void SetFailed()
        {
            Clear();
            Errors = -1;
        }
    }
    public abstract class MarkupManager<TypeMarkup> : IManager
        where TypeMarkup : Markup
    {
        protected Dictionary<ushort, TypeMarkup> Markups { get; } = new Dictionary<ushort, TypeMarkup>();
        protected HashSet<ushort> NeedUpdate { get; } = new HashSet<ushort>();
        protected abstract MarkupType Type { get; }
        protected abstract string XmlName { get; }
        protected abstract ObjectsMap.TryGetDelegate<ushort> MapTryGet(ObjectsMap map);

        private static PropManager PropManager => Singleton<PropManager>.instance;

        public bool TryGetMarkup(ushort id, out TypeMarkup markup) => Markups.TryGetValue(id, out markup);
        public TypeMarkup this[ushort id]
        {
            get
            {
                if (!Markups.TryGetValue(id, out TypeMarkup markup))
                {
                    markup = NewMarkup(id);
                    Markups[id] = markup;
                }

                return markup;
            }
        }
        protected abstract TypeMarkup NewMarkup(ushort id);

        public void Update()
        {
            var needUpdate = NeedUpdate.ToArray();
            NeedUpdate.Clear();
            foreach (var nodeId in needUpdate)
            {
                if (Markups.TryGetValue(nodeId, out TypeMarkup markup))
                    markup.Update();
            }
        }
        public void Render(RenderManager.CameraInfo cameraInfo, ushort id, ref RenderManager.Instance data)
        {
            if (data.m_nextInstance != ushort.MaxValue)
                return;

            if (!TryGetMarkup(id, out TypeMarkup markup))
                return;

            if ((cameraInfo.m_layerMask & (3 << 24)) == 0)
                return;

            if (markup.NeedRecalculateDrawData)
                markup.RecalculateDrawData();

            if (cameraInfo.CheckRenderDistance(data.m_position, Settings.LODDistance))
                Render(markup, data, MarkupLOD.LOD0);
            else if (cameraInfo.CheckRenderDistance(data.m_position, Settings.RenderDistance))
                Render(markup, data, MarkupLOD.LOD1);
        }
        private void Render(TypeMarkup markup, RenderManager.Instance data, MarkupLOD lod)
        {
            foreach (var item in markup.DrawData[lod])
                item.Draw(data);
        }

        public void AddToUpdate(ushort id)
        {
            if (Markups.ContainsKey(id))
                NeedUpdate.Add(id);
        }
        public void AddAllToUpdate() => NeedUpdate.AddRange(Markups.Keys);
        public void Remove(ushort id) => Markups.Remove(id);
        public void Clear()
        {
            SingletonMod<Mod>.Logger.Debug($"{typeof(TypeMarkup).Name} {nameof(Clear)}");
            NeedUpdate.Clear();
            Markups.Clear();
        }
        public void ToXml(XElement config)
        {
            foreach (var markup in Markups.Values)
            {
                try
                {
                    config.Add(markup.ToXml());
                }
                catch (Exception error)
                {
                    SingletonMod<Mod>.Logger.Error($"Could not save {Type} #{markup.Id} markup", error);
                    MarkupManager.Errors += 1;
                }
            }
        }
        public void FromXml(XElement config, ObjectsMap map, Version version)
        {
            var tryGet = MapTryGet(map);
            foreach (var markupConfig in config.Elements(XmlName))
            {
                var id = markupConfig.GetAttrValue<ushort>(nameof(Markup.Id));
                if (id == 0)
                    continue;
                try
                {
                    while (tryGet(id, out var targetId))
                        id = targetId;

                    var markup = this[id];

                    markup.FromXml(version, markupConfig, map);
                    NeedUpdate.Add(markup.Id);
                }
                catch (NotExistItemException error)
                {
                    SingletonMod<Mod>.Logger.Error($"Could not load {error.Type} #{error.Id} markup: {error.Type} not exist");
                    MarkupManager.Errors += 1;
                }
                catch (NotExistEnterException error)
                {
                    SingletonMod<Mod>.Logger.Error($"Could not load {Type} #{id} markup: {error.Type} enter #{error.Id} not exist");
                    MarkupManager.Errors += 1;
                }
                catch (Exception error)
                {
                    SingletonMod<Mod>.Logger.Error($"Could not load {Type} #{id} markup", error);
                    MarkupManager.Errors += 1;
                }
            }
        }
    }
    public class NodeMarkupManager : MarkupManager<NodeMarkup>
    {
        protected override NodeMarkup NewMarkup(ushort id) => new NodeMarkup(id);
        protected override MarkupType Type => MarkupType.Node;
        protected override string XmlName => NodeMarkup.XmlName;
        protected override ObjectsMap.TryGetDelegate<ushort> MapTryGet(ObjectsMap map) => map.TryGetNode;
    }
    public class SegmentMarkupManager : MarkupManager<SegmentMarkup>
    {
        protected override SegmentMarkup NewMarkup(ushort id) => new SegmentMarkup(id);
        protected override MarkupType Type => MarkupType.Segment;
        protected override string XmlName => SegmentMarkup.XmlName;
        protected override ObjectsMap.TryGetDelegate<ushort> MapTryGet(ObjectsMap map) => map.TryGetSegment;
    }
}
