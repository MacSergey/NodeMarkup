using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.PlatformServices;
using ModsCommon.Utilities;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;
using static NetInfo;

namespace NodeMarkup.Manager
{
    public static class MarkupManager
    {
        public static NodeMarkupManager NodeManager { get; }
        public static SegmentMarkupManager SegmentManager { get; }
        public static int Errors { get; set; } = 0;
        public static bool HasErrors => Errors != 0;

        static MarkupManager()
        {
            NodeManager = new NodeMarkupManager();
            SegmentManager = new SegmentMarkupManager();
        }

        public static void Clear()
        {
            NodeManager.Clear();
            SegmentManager.Clear();
        }

        public static void NetNodeRenderInstancePostfix(RenderManager.CameraInfo cameraInfo, ushort nodeID, ref RenderManager.Instance data) => NodeManager.Render(cameraInfo, nodeID, ref data);

        public static void NetSegmentRenderInstancePostfix(RenderManager.CameraInfo cameraInfo, ushort segmentID, ref RenderManager.Instance data) => SegmentManager.Render(cameraInfo, segmentID, ref data);

        public static void NetManagerReleaseNodeImplementationPrefix(ushort node) => NodeManager.Remove(node);
        public static void NetManagerReleaseSegmentImplementationPrefix(ushort segment) => SegmentManager.Remove(segment);
        public static void NetManagerUpdateNodePostfix(ushort node) => NodeManager.AddToUpdate(node);
        public static void NetManagerUpdateSegmentPostfix(ushort segment) => SegmentManager.AddToUpdate(segment);
        public static void NetSegmentUpdateLanesPostfix(ushort segmentID)
        {
            SegmentManager.AddToUpdate(segmentID);
            var segment = segmentID.GetSegment();
            NodeManager.AddToUpdate(segment.m_startNode);
            NodeManager.AddToUpdate(segment.m_endNode);
        }
        public static void NetManagerSimulationStepImplPostfix()
        {
            NodeManager.Update();
            SegmentManager.Update();
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
            var confix = new XElement(nameof(NodeMarkup), new XAttribute("V", Mod.Version));

            Errors = 0;

            NodeManager.ToXml(confix);
            SegmentManager.ToXml(confix);

            return confix;
        }
        public static void FromXml(XElement config, ObjectsMap map, bool clear = true)
        {
            if (clear)
                Clear();

            Errors = 0;

            var version = GetVersion(config);

            NodeManager.FromXml(config, map, version);
            SegmentManager.FromXml(config, map, version);
        }
        private static Version GetVersion(XElement config)
        {
            try { return new Version(config.Attribute("V").Value); }
            catch { return Mod.Version; }
        }
        public static void SetFiled()
        {
            Clear();
            Errors = -1;
        }
    }
    public abstract class MarkupManager<MarkupType>
        where MarkupType : Markup
    {
        protected Dictionary<ushort, MarkupType> Markups { get; } = new Dictionary<ushort, MarkupType>();
        protected HashSet<ushort> NeedUpdate { get; } = new HashSet<ushort>();
        protected abstract string ItemName { get; }
        protected abstract string XmlName { get; }
        protected abstract ObjectsMap.TryGetDelegate<ushort> MapTryGet(ObjectsMap map);

        static PropManager PropManager => Singleton<PropManager>.instance;

        public bool TryGetMarkup(ushort id, out MarkupType markup) => Markups.TryGetValue(id, out markup);
        public MarkupType Get(ushort id)
        {
            if (!Markups.TryGetValue(id, out MarkupType markup))
            {
                markup = NewMarkup(id);
                Markups[id] = markup;
            }

            return markup;
        }
        protected abstract MarkupType NewMarkup(ushort id);

        public void Update()
        {
            var needUpdate = NeedUpdate.ToArray();
            NeedUpdate.Clear();
            foreach (var nodeId in needUpdate)
            {
                if (Markups.TryGetValue(nodeId, out MarkupType markup))
                    markup.Update();
            }
        }
        public void Render(RenderManager.CameraInfo cameraInfo, ushort id, ref RenderManager.Instance data)
        {
            if (data.m_nextInstance != ushort.MaxValue)
                return;

            if (!TryGetMarkup(id, out MarkupType markup))
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
        private void Render(MarkupType markup, RenderManager.Instance data, MarkupLOD lod)
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
            Mod.Logger.Debug($"{typeof(MarkupType).Name} {nameof(Clear)}");
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
                    Mod.Logger.Error($"Could not save {ItemName} #{markup.Id} markup", error);
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

                while (tryGet(id, out var targetId))
                    id = targetId;

                var markup = Get(id);

                try
                {
                    markup.FromXml(version, markupConfig, map);
                    NeedUpdate.Add(markup.Id);
                    Mod.Logger.Debug($"{ItemName} #{markup.Id} markup loaded");
                }
                catch (Exception error)
                {
                    Mod.Logger.Error($"Could not load {ItemName} #{markup.Id} markup", error);
                    MarkupManager.Errors += 1;
                }
            }
        }
    }
    public class NodeMarkupManager : MarkupManager<NodeMarkup>
    {
        protected override NodeMarkup NewMarkup(ushort id) => new NodeMarkup(id);
        protected override string ItemName => "Node";
        protected override string XmlName => NodeMarkup.XmlName;
        protected override ObjectsMap.TryGetDelegate<ushort> MapTryGet(ObjectsMap map) => map.TryGetNode;
    }
    public class SegmentMarkupManager : MarkupManager<SegmentMarkup>
    {
        protected override SegmentMarkup NewMarkup(ushort id) => new SegmentMarkup(id);
        protected override string ItemName => "Segment";
        protected override string XmlName => SegmentMarkup.XmlName;
        protected override ObjectsMap.TryGetDelegate<ushort> MapTryGet(ObjectsMap map) => map.TryGetSegment;
    }

    public enum MaterialType
    {
        RectangleLines,
        RectangleFillers,
        Triangle,
        Pavement,
        Grass,
    }
}
