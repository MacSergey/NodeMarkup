using ColossalFramework;
using NodeMarkup.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public static class MarkupManager
    {
        static Dictionary<ushort, Markup> NodesMarkup { get; } = new Dictionary<ushort, Markup>();
        static HashSet<ushort> NeedUpdate { get; } = new HashSet<ushort>();

        static PropManager PropManager => Singleton<PropManager>.instance;
        static Material Material { get; set; }
        public static ushort LoadErrors { get; set; } = 0;

        public static void Init()
        {
            Material = RenderHelper.CreateMaterial();
        }

        public static bool TryGetMarkup(ushort nodeId, out Markup markup) => NodesMarkup.TryGetValue(nodeId, out markup);

        public static Markup Get(ushort nodeId)
        {
            if (!NodesMarkup.TryGetValue(nodeId, out Markup markup))
            {
                markup = new Markup(nodeId);
                NodesMarkup[nodeId] = markup;
            }

            return markup;
        }

        public static void NetNodeRenderInstancePostfix(RenderManager.CameraInfo cameraInfo, ushort nodeID, ref RenderManager.Instance data)
        {
            if (data.m_nextInstance != ushort.MaxValue)
                return;

            if (!TryGetMarkup(nodeID, out Markup markup))
                return;

            if ((cameraInfo.m_layerMask & (3 << 24)) == 0)
                return;

            if (!cameraInfo.CheckRenderDistance(data.m_position, UI.Settings.RenderDistance))
                return;

            if (markup.NeedRecalculateBatches)
            {
                markup.NeedRecalculateBatches = false;
                markup.RecalculateBatches();
            }

            var instance = PropManager;
            var materialBlock = instance.m_materialBlock;

            var renderBatches = markup.RenderBatches;

            foreach (var batch in renderBatches)
            {
                materialBlock.Clear();
                materialBlock.SetVectorArray(instance.ID_PropLocation, batch.Locations);
                materialBlock.SetVectorArray(instance.ID_PropObjectIndex, batch.Indices);
                materialBlock.SetVectorArray(instance.ID_PropColor, batch.Colors);
                materialBlock.SetVector(RenderHelper.ID_DecalSize, batch.Size);

                var mesh = batch.Mesh;
                var material = Material;

                Graphics.DrawMesh(mesh, Matrix4x4.identity, material, 10, null, 0, materialBlock);
            }
        }


        public static void NetManagerReleaseNodeImplementationPrefix(ushort node) => NodesMarkup.Remove(node);
        public static void NetManagerUpdateNodePostfix(ushort node, ushort fromSegment, int level) => AddToUpdate(node);
        public static void NetSegmentUpdateLanesPostfix(ushort segmentID)
        {
            var segment = Utilities.GetSegment(segmentID);
            AddToUpdate(segment.m_startNode);
            AddToUpdate(segment.m_endNode);
        }

        static void AddToUpdate(ushort nodeId)
        {
            if (NodesMarkup.ContainsKey(nodeId))
            {
                NeedUpdate.Add(nodeId);
            }
        }
        public static void NetManagerSimulationStepImplPostfix()
        {
            var needUpdate = NeedUpdate.ToArray();
            NeedUpdate.Clear();
            foreach (var nodeId in needUpdate)
            {
                if (NodesMarkup.TryGetValue(nodeId, out Markup markup))
                    markup.Update();
            }
        }
        public static void DeleteAll()
        {
            Logger.LogDebug($"{nameof(MarkupManager)}.{nameof(DeleteAll)}");
            NodesMarkup.Clear();
        }

        public static XElement ToXml()
        {
            var confix = new XElement(nameof(NodeMarkup), new XAttribute("V", Mod.Version));
            foreach (var markup in NodesMarkup.Values)
            {
                var markupConfig = markup.ToXml();
                confix.Add(markupConfig);
            }
            return confix;
        }
        public static void FromXml(XElement config)
        {
            NodesMarkup.Clear();
            LoadErrors = 0;

            var version = config.GetAttrValue("V", Mod.Version);
            foreach (var markupConfig in config.Elements(Markup.XmlName))
            {
                if (Markup.FromXml(version, markupConfig, out Markup markup))
                    NeedUpdate.Add(markup.Id);
            }
        }
    }
}
