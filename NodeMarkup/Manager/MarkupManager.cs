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
        private static ushort[] NeedUpdateNodeIds { get; set; }
        private static ushort[] NeedUpdateSegmentIds { get; set; }

        public static void Clear()
        {
            SingletonManager<NodeMarkupManager>.Instance.Clear();
            SingletonManager<SegmentMarkupManager>.Instance.Clear();
        }
        public static void Destroy()
        {
            SingletonManager<NodeMarkupManager>.Destroy();
            SingletonManager<SegmentMarkupManager>.Destroy();
        }

        public static void NetNodeRenderInstancePostfix(RenderManager.CameraInfo cameraInfo, ushort nodeID, ref RenderManager.Instance data) => SingletonManager<NodeMarkupManager>.Instance.Render(cameraInfo, nodeID, ref data);

        public static void NetSegmentRenderInstancePostfix(RenderManager.CameraInfo cameraInfo, ushort segmentID, ref RenderManager.Instance data) => SingletonManager<SegmentMarkupManager>.Instance.Render(cameraInfo, segmentID, ref data);

        public static void NetManagerReleaseNodeImplementationPrefix(ushort node) => SingletonManager<NodeMarkupManager>.Instance.Remove(node);
        public static void NetManagerReleaseSegmentImplementationPrefix(ushort segment) => SingletonManager<SegmentMarkupManager>.Instance.Remove(segment);

        public static void GetToUpdate()
        {
            NeedUpdateNodeIds = NetManager.instance.GetUpdateNodes().ToArray();
            NeedUpdateSegmentIds = NetManager.instance.GetUpdateSegments().ToArray();
        }
        public static void Update()
        {
            SingletonManager<NodeMarkupManager>.Instance.Update(NeedUpdateNodeIds);
            SingletonManager<SegmentMarkupManager>.Instance.Update(NeedUpdateSegmentIds);
        }
        public static void UpdateNode(ushort nodeId) => SingletonManager<NodeMarkupManager>.Instance.Update(nodeId);
        public static void UpdateSegment(ushort segmentId) => SingletonManager<SegmentMarkupManager>.Instance.Update(segmentId);
        public static void UpdateAll()
        {
            SingletonManager<NodeMarkupManager>.Instance.UpdateAll();
            SingletonManager<SegmentMarkupManager>.Instance.UpdateAll();
        }

        public static void NetInfoInitNodeInfoPostfix_Rail(Node info)
        {
            if (info.m_nodeMaterial.shader.name == "Custom/Net/TrainBridge")
                info.m_nodeMaterial.renderQueue = 2470;
        }
        public static void NetInfoInitNodeInfoPostfix_LevelCrossing(Node info)
        {
            if (info.m_flagsRequired.IsFlagSet(NetNode.Flags.LevelCrossing))
                info.m_nodeMaterial.renderQueue = 2470;
        }
        public static void NetInfoInitSegmentInfoPostfix(Segment info)
        {
            if (info.m_segmentMaterial.shader.name == "Custom/Net/TrainBridge")
                info.m_segmentMaterial.renderQueue = 2470;
        }

        public static void Import(XElement config)
        {
            Clear();
            FromXml(config, new ObjectsMap(), true);
        }
        public static XElement ToXml()
        {
            var config = new XElement(nameof(NodeMarkup));
            config.AddAttr("V", SingletonMod<Mod>.Version);

            Errors = 0;

            SingletonManager<NodeMarkupManager>.Instance.ToXml(config);
            SingletonManager<SegmentMarkupManager>.Instance.ToXml(config);

            return config;
        }
        public static void FromXml(XElement config, ObjectsMap map, bool needUpdate)
        {
            Errors = 0;

            var version = GetVersion(config);

            SingletonManager<NodeMarkupManager>.Instance.FromXml(config, map, version, needUpdate);
            SingletonManager<SegmentMarkupManager>.Instance.FromXml(config, map, version, needUpdate);
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

        protected abstract void AddToUpdate(ushort id);
        public void Update(params ushort[] ids)
        {
            foreach (var id in ids)
            {
                if (Markups.TryGetValue(id, out TypeMarkup markup))
                    markup.Update();
            }
        }
        public void UpdateAll()
        {
            SingletonMod<Mod>.Logger.Debug($"Update all {Type} markings");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var toUpdate = Markups.Keys.ToArray();
            Update(toUpdate);
            sw.Stop();

            SingletonMod<Mod>.Logger.Debug($"{toUpdate.Length} {Type} markings updated in {sw.ElapsedMilliseconds}ms");
        }

        public void Render(RenderManager.CameraInfo cameraInfo, ushort id, ref RenderManager.Instance data)
        {
            if (data.m_nextInstance != ushort.MaxValue)
                return;

            if ((cameraInfo.m_layerMask & (3 << 24)) == 0)
                return;

            if (!TryGetMarkup(id, out TypeMarkup markup))
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

        public void Remove(ushort id) => Markups.Remove(id);
        public void Clear()
        {
            SingletonMod<Mod>.Logger.Debug($"{typeof(TypeMarkup).Name} {nameof(Clear)}");
            Markups.Clear();
        }
        public void ToXml(XElement config)
        {
            foreach (var markup in Markups.Values.OrderBy(m => m.Id))
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
        public void FromXml(XElement config, ObjectsMap map, Version version, bool needUpdate)
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
                    {
                        id = targetId;
                        if (map.IsSimple)
                            break;
                    }

                    var markup = this[id];

                    markup.FromXml(version, markupConfig, map, needUpdate);
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
        public NodeMarkupManager()
        {
            SingletonMod<Mod>.Logger.Debug("Create node markup manager");
        }

        protected override NodeMarkup NewMarkup(ushort id) => new NodeMarkup(id);
        protected override MarkupType Type => MarkupType.Node;
        protected override string XmlName => NodeMarkup.XmlName;
        protected override ObjectsMap.TryGetDelegate<ushort> MapTryGet(ObjectsMap map) => map.TryGetNode;
        protected override void AddToUpdate(ushort id) => NetManager.instance.UpdateNode(id);
    }
    public class SegmentMarkupManager : MarkupManager<SegmentMarkup>
    {
        public SegmentMarkupManager()
        {
            SingletonMod<Mod>.Logger.Debug("Create segment markup manager");
        }

        protected override SegmentMarkup NewMarkup(ushort id) => new SegmentMarkup(id);
        protected override MarkupType Type => MarkupType.Segment;
        protected override string XmlName => SegmentMarkup.XmlName;
        protected override ObjectsMap.TryGetDelegate<ushort> MapTryGet(ObjectsMap map) => map.TryGetSegment;
        protected override void AddToUpdate(ushort id) => NetManager.instance.UpdateSegment(id);
    }
}
