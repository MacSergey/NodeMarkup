using ColossalFramework;
using ColossalFramework.Math;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeMarkup.Utilities
{
    public class MarkingPartData
    {
        public Material Material { get; set; }
        public Vector3 Position { get; set; }
        public float Angle { get; set; }
        public float Length { get; set; }
        public float Width { get; set; }
        public Color32 Color { get; set; }

        public MarkingPartData(Vector3 position, float angle, float length, float width, Color32 color, Material material)
        {
            Position = position;
            Angle = angle;
            Length = length;
            Width = width;
            Color = color;
            Material = material;
        }
        public MarkingPartData(Vector3 start, Vector3 end, float angle, float length, float width, Color32 color, Material material)
            : this((start + end) / 2, angle, length, width, color, material) { }

        public MarkingPartData(Vector3 start, Vector3 end, Vector3 dir, float length, float width, Color32 color, Material material)
            : this(start, end, dir.AbsoluteAngle(), length, width, color, material) { }

        public MarkingPartData(Vector3 start, Vector3 end, Vector3 dir, float width, Color32 color, Material material)
            : this(start, end, dir, (end - start).magnitude, width, color, material) { }

        public MarkingPartData(Vector3 start, Vector3 end, float width, Color32 color, Material material)
            : this(start, end, end - start, (end - start).magnitude, width, color, material) { }
    }
    public class MarkingPartGroupData : IStyleData, IEnumerable<MarkingPartData>
    {
        private List<MarkingPartData> Dashes { get; }
        public MarkingLOD LOD { get; }
        public MarkingLODType LODType => MarkingLODType.Dash;

        public MarkingPartGroupData(MarkingLOD lod)
        {
            LOD = lod;
            Dashes = new List<MarkingPartData>();
        }
        public MarkingPartGroupData(MarkingLOD lod, IEnumerable<MarkingPartData> dashes)
        {
            LOD = lod;
            Dashes = dashes.ToList();
        }

        public IEnumerator<MarkingPartData> GetEnumerator() => Dashes.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerable<IDrawData> GetDrawData() => MarkingPartsBatchData.FromDashes(this);
    }

    public class MarkingPartsBatchData : IDrawData
    {
        public static float MeshHeight => 5f;

        public Mesh Mesh { get; }
        public Material Material { get; }
        public int Count { get; }
        public Vector4[] Locations { get; }
        public Vector4[] Indices { get; }
        public Vector4[] Colors { get; }
        public Vector4 Size { get; }

        public MarkingPartsBatchData(MarkingPartData[] dashes, int count, Vector3 size, Material material)
        {
            Material = material;
            Count = count;
            Locations = new Vector4[Count];
            Indices = new Vector4[Count];
            Colors = new Vector4[Count];
            Size = size;

            for (var i = 0; i < Count; i += 1)
            {
                var dash = dashes[i];
                Locations[i] = dash.Position;
                Locations[i].w = dash.Angle;
                Indices[i] = new Vector4(0f, 0f, 0f, 1f);
                Colors[i] = dash.Color.ToX3Vector();
            }

            Mesh = RenderHelper.CreateMesh(Count, size);
        }

        public static IEnumerable<IDrawData> FromDashes(IEnumerable<MarkingPartData> dashes)
        {
            var materialGroups = dashes.GroupBy(d => d.Material);

            foreach (var materialGroup in materialGroups)
            {
                var sizeGroups = materialGroup.Where(d => d.Length >= 0.1f).GroupBy(d => new Vector3(Round(d.Length), MeshHeight, d.Width));

                foreach (var sizeGroup in sizeGroups)
                {
                    var groupEnumerator = sizeGroup.GetEnumerator();

                    var buffer = new MarkingPartData[16];
                    var count = 0;

                    bool isEnd = groupEnumerator.MoveNext();
                    do
                    {
                        buffer[count] = groupEnumerator.Current;
                        count += 1;
                        isEnd = !groupEnumerator.MoveNext();
                        if (isEnd || count == 16)
                        {
                            var batch = new MarkingPartsBatchData(buffer, count, sizeGroup.Key, materialGroup.Key);
                            yield return batch;
                            count = 0;
                        }
                    }
                    while (!isEnd);
                }
            }
        }
        private static int RoundTo => 5;
        public static float Round(float length)
        {
            var cm = (int)(length * 100);
            var mod = cm % RoundTo;
            return (mod == 0 ? cm : cm - mod + RoundTo) / 100f;
        }

        public override string ToString() => $"{Count}: {Size}";

        public void Draw(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data, bool infoView)
        {
            if (infoView)
                return;

            var instance = Singleton<PropManager>.instance;
            var materialBlock = instance.m_materialBlock;
            materialBlock.Clear();

            materialBlock.SetVectorArray(instance.ID_PropLocation, Locations);
            materialBlock.SetVectorArray(instance.ID_PropObjectIndex, Indices);
            materialBlock.SetVectorArray(instance.ID_PropColor, Colors);
            materialBlock.SetVector(RenderHelper.ID_DecalSize, Size);

            Graphics.DrawMesh(Mesh, Matrix4x4.identity, Material, RenderHelper.RoadLayer, null, 0, materialBlock);
        }
    }
}
