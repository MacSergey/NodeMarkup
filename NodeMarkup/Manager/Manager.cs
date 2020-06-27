using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.PlatformServices;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public static class Manager
    {
        static Dictionary<ushort, Markup> NodesMarkup { get; } = new Dictionary<ushort, Markup>();
        static HashSet<ushort> NeedUpdate { get; } = new HashSet<ushort>();
        static float RenderDistance { get; } = 300f;
        static PropManager PropManager => Singleton<PropManager>.instance;
        static Material Material { get; set; }

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
            if (!TryGetMarkup(nodeID, out Markup markup))
                return;

            if (!cameraInfo.CheckRenderDistance(data.m_position, RenderDistance))
                return;

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

                var mesh = batch.Mesh;
                var material = Material;

                Graphics.DrawMesh(mesh, Matrix4x4.identity, material, 10, null, 0, materialBlock);
            }
            //Logger.LogDebug($"End render node {nodeID} markup success");
        }

        public static void NetManagerReleaseNodeImplementationPrefix(ushort node)
        {
            NodesMarkup.Remove(node);
        }
        public static void NetManagerUpdateNodePostfix(ushort node, ushort fromSegment, int level)
        {
            AddToUpdate(node);
        }
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
    }

    public class RenderBatch
    {
        public Vector4[] Locations { get; }
        public Vector4[] Indices { get; }
        public Vector4[] Colors { get; }
        public Mesh Mesh { get; }

        public RenderBatch(MarkupDash[] dashes, int from = 0)
        {
            var count = Math.Min(16, dashes.Length - from);
            Locations = new Vector4[count];
            Indices = new Vector4[count];
            Colors = new Vector4[count];

            var lengths = new float[count];
            var widths = new float[count];
            var heights = new float[count];

            for (var i = 0; i < count; i += 1)
            {
                var dash = dashes[i + from];
                Locations[i] = dash.Position;
                Locations[i].w = dash.Angle;
                Indices[i] = new Vector4(0f, 0f, 0f, 1f);
                Colors[i] = dash.Color.ToVector();

                lengths[i] = dash.Length;
                widths[i] = 0.15f;
                heights[i] = 0.1f;
            }

            Mesh = RenderHelper.CreateMesh(count, lengths, widths, heights);
        }

        public static RenderBatch[] FromDashes(MarkupDash[] dashes)
        {
            var batches = new List<RenderBatch>();
            for (var i = 0; i < dashes.Length; i += 16)
            {
                batches.Add(new RenderBatch(dashes, i));
            }
            return batches.ToArray();
        }
    }
}
