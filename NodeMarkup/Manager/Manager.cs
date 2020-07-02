using ColossalFramework;
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
                materialBlock.SetVector(RenderHelper.ID_DecalSize, batch.Size);

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

    public class RenderBatch
    {
        static int ToBatch => 4;
        public Vector4[] Locations { get; }
        public Vector4[] Indices { get; }
        public Vector4[] Colors { get; }
        public Mesh Mesh { get; }

        public Vector4 Size { get; }

        public RenderBatch(MarkupDash[] dashes, int count, float length)
        {
            Locations = new Vector4[count];
            Indices = new Vector4[count];
            Colors = new Vector4[count];
            Size = new Vector4(length, 3f, 0.15f);

            for (var i = 0; i < count; i += 1)
            {
                var dash = dashes[i];
                Locations[i] = dash.Position;
                Locations[i].w = dash.Angle;
                Indices[i] = new Vector4(0f, 0f, 0f, 1f);
                Colors[i] = dash.Color.ToVector();
            }

            Mesh = RenderHelper.CreateMesh(count, Size);
        }

        public static IEnumerable<RenderBatch> FromDashes(MarkupDash[] dashes)
        {
            var groups = dashes.GroupBy(d => Round(d.Length));

            foreach(var group in groups)
            {
                var length = group.Key;
                var groupEnumerator = group.GetEnumerator();

                var buffer = new MarkupDash[16];
                var count = 0;

                bool isEnd = groupEnumerator.MoveNext();
                do
                {
                    buffer[count] = groupEnumerator.Current;
                    count += 1;
                    isEnd = !groupEnumerator.MoveNext();
                    if (isEnd || count == 16)
                    {
                        var batch = new RenderBatch(buffer, count, length);
                        yield return batch;
                        count = 0;
                    }
                }
                while (!isEnd);
            }

            float Round(float value)
            {
                var temp = (int)(value * 100);
                var mod = temp % 10;
                return (mod == 0 ? temp : temp - mod + 10) / 100f;
            }
        }
    }
}
