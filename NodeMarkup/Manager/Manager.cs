﻿using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.PlatformServices;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public static void Init()
        {
            Material = Render.CreateMaterial();
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
            if (!TryGetMarkup(nodeID, out Markup markup))
                return;

            if (!cameraInfo.CheckRenderDistance(data.m_position, UI.Settings.RenderDistance))
                return;

            if (markup.NeedRecalculate)
            {
                markup.NeedRecalculate = false;
                markup.RecalculateBatches();
            }

            var instance = PropManager;
            var materialBlock = instance.m_materialBlock;

            var renderBatches = markup.RenderBatches;

            //Logger.LogDebug($"Start render node {nodeID} markup: {renderBatches.Length} batches");
            foreach (var batch in renderBatches)
            {
                materialBlock.Clear();
                materialBlock.SetVectorArray(instance.ID_PropLocation, batch.Locations);
                materialBlock.SetVectorArray(instance.ID_PropObjectIndex, batch.Indices);
                materialBlock.SetVectorArray(instance.ID_PropColor, batch.Colors);
                materialBlock.SetVector(Render.ID_DecalSize, batch.Size);

                var mesh = batch.Mesh;
                var material = Material;

                Graphics.DrawMesh(mesh, Matrix4x4.identity, material, 10, null, 0, materialBlock);
            }
            //Logger.LogDebug($"End render node {nodeID} markup success");
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
            foreach (var nodeId in NeedUpdate)
            {
                if (NodesMarkup.TryGetValue(nodeId, out Markup markup))
                    markup.Update();
            }
            NeedUpdate.Clear();
        }

        public static XElement ToXml()
        {
            var confix = new XElement(nameof(NodeMarkup));
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

            foreach (var markupConfig in config.Elements(Markup.XmlName))
            {
                if (Markup.FromXml(markupConfig, out Markup markup))
                    NeedUpdate.Add(markup.Id);
            }
        }
    }

    //public class Settings
    //{
    //    public float RenderDistance { get; } = 300f;
    //    List<LineStyleTemplate> TemplatesList { get; } = new List<LineStyleTemplate>();

    //    public IEnumerable<LineStyleTemplate> Templates => TemplatesList;

    //    public LineStyleTemplate AddTemplate(LineStyle style, string name = null)
    //    {
    //        var template = new LineStyleTemplate(name ?? "New template", style);
    //        TemplatesList.Add(template);
    //        return template;
    //    }
    //}
}