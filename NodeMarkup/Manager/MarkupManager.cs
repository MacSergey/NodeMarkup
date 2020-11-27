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
        public static ushort LoadErrors { get; set; } = 0;

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
        public static void NetInfoNodeInitNodeInfoPostfix(Node info)
        {
            if (info.m_nodeMaterial.shader.name == "Custom/Net/TrainBridge")
                info.m_nodeMaterial.renderQueue = 2470;
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

            foreach(var markupConfig in NodeManager.ToXml())
                confix.Add(markupConfig);

            foreach (var markupConfig in SegmentManager.ToXml())
                confix.Add(markupConfig);

            return confix;
        }
        public static void FromXml(XElement config, ObjectsMap map, bool clear = true)
        {
            if (clear)
                Clear();
            LoadErrors = 0;

            var version = config.GetAttrValue("V", Mod.Version);

            NodeManager.FromXml(config, map, version);
            SegmentManager.FromXml(config, map, version);
        }
    }
    public abstract class MarkupManager<MarkupType>
        where MarkupType : Markup
    {
        protected Dictionary<ushort, MarkupType> Markups { get; } = new Dictionary<ushort, MarkupType>();
        protected HashSet<ushort> NeedUpdate { get; } = new HashSet<ushort>();

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

            if (!cameraInfo.CheckRenderDistance(data.m_position, Settings.RenderDistance))
                return;

            if (markup.NeedRecalculateDrawData)
            {
                markup.NeedRecalculateDrawData = false;
                markup.RecalculateDrawData();
            }

            foreach (var item in markup.DrawData)
                item.Draw(data.m_dataVector3);
        }

        public void AddToUpdate(ushort id)
        {
            if (Markups.ContainsKey(id))
                NeedUpdate.Add(id);
        }
        public void Remove(ushort id) => Markups.Remove(id);
        public void Clear()
        {
            Mod.Logger.Debug($"{nameof(MarkupManager)}.{nameof(Clear)}");
            NeedUpdate.Clear();
            Markups.Clear();
        }
        public IEnumerable<XElement> ToXml() => Markups.Values.Select(m => m.ToXml());
        public abstract void FromXml(XElement config, ObjectsMap map, Version version);
    }
    public class NodeMarkupManager : MarkupManager<NodeMarkup>
    {
        protected override NodeMarkup NewMarkup(ushort id) => new NodeMarkup(id);

        public override void FromXml(XElement config, ObjectsMap map, Version version)
        {
            foreach (var markupConfig in config.Elements(NodeMarkup.XmlName))
            {
                if (NodeMarkup.FromXml(version, markupConfig, map, out NodeMarkup markup))
                    NeedUpdate.Add(markup.Id);
            }
        }
    }
    public class SegmentMarkupManager : MarkupManager<SegmentMarkup>
    {
        protected override SegmentMarkup NewMarkup(ushort id) => new SegmentMarkup(id);

        public override void FromXml(XElement config, ObjectsMap map, Version version)
        {
            foreach (var markupConfig in config.Elements(SegmentMarkup.XmlName))
            {
                if (SegmentMarkup.FromXml(version, markupConfig, map, out SegmentMarkup markup))
                    NeedUpdate.Add(markup.Id);
            }
        }
    }

    public enum MaterialType
    {
        RectangleLines,
        RectangleFillers,
        Triangle,
        Pavement,
    }
}
