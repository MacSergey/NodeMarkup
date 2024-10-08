﻿using ColossalFramework;
using IMT.Manager;
using ModsCommon;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IMT.Utilities
{
    public class FillerMeshData : IStyleData
    {
        private static int mainTexId = Shader.PropertyToID("_MainTex");
        private static int colorId = Shader.PropertyToID("_Color");
        private static int tilingId = Shader.PropertyToID("_Tiling");

        public enum MeshType
        {
            Side,
            Top,
        }
        public readonly struct RawData
        {
            public readonly MeshType meshType;
            public readonly int[] groups;
            public readonly Vector3[] points;
            public readonly int[] polygons;
            public readonly Color color;
            public readonly TextureData textureData;

            private RawData(MeshType meshType, int[] groups, Vector3[] points, int[] polygons, Color color, TextureData textureData)
            {
                this.meshType = meshType;
                this.groups = groups;
                this.points = points;
                this.polygons = polygons;
                this.color = color;
                this.textureData = textureData;
            }

            public static RawData GetSide(int[] groups, Vector3[] points, Color color, TextureData textureData)
            {
                return new RawData(MeshType.Side, groups.ToArray(), points.ToArray(), null, color, textureData);
            }
            public static RawData GetTop(Vector3[] points, int[] polygons, Color color, TextureData textureData)
            {
                return new RawData(MeshType.Top, null, points.ToArray(), polygons.ToArray(), color, textureData);
            }
        }
        private readonly struct RenderData

        {
            public readonly Mesh mesh;
            public readonly Color color;
            public readonly TextureData textureData;

            public RenderData(Mesh mesh, Color color, TextureData textureData)
            {
                this.mesh = mesh;
                this.color = color;
                this.textureData = textureData;
            }
        }
        public readonly struct TextureData
        {
            public readonly Texture2D mainTexture;
            public readonly Vector4 tiling;

            public TextureData(Texture2D mainTexture, Vector2 tiling, float angle)
            {
                this.mainTexture = mainTexture;
                this.tiling = new Vector4(tiling.x, Mathf.Sin(angle), tiling.y, Mathf.Cos(angle));
            }
        }

        public MarkingLOD LOD { get; }
        public MarkingLODType LODType => MarkingLODType.Mesh;
        public int RenderLayer => RenderHelper.RoadLayer;
        private RenderData[] Datas { get; }
        private Vector3 Position { get; }

        public FillerMeshData(MarkingLOD lod, float elevation, params RawData[] rawDatas)
        {
            LOD = lod;
            Datas = new RenderData[rawDatas.Length];

            var min = rawDatas[0].points[0];
            var max = rawDatas[0].points[0];

            for (var i = 0; i < rawDatas.Length; i += 1)
            {
                var points = rawDatas[i].points;
                for (var j = 0; j < points.Length; j += 1)
                {
                    var point = points[j];

                    if (rawDatas[i].meshType == MeshType.Side)
                    {
                        max = Vector3.Max(max, point);
                        point.y -= elevation + 0.5f;
                        min = Vector3.Min(min, point);
                    }
                    else
                    {
                        min = Vector3.Min(min, point);
                        max = Vector3.Max(max, point);
                    }
                }
            }
            var minMax = new Bounds((min + max) * 0.5f, max - min);
            var delta = new Vector3(minMax.center.x, minMax.min.y, minMax.center.z);
            Position = new Vector3(minMax.center.x, minMax.min.y + elevation, minMax.center.z);

            for (var i = 0; i < rawDatas.Length; i += 1)
            {
                var rawData = rawDatas[i];

                var mesh = new Mesh
                {
                    name = "MarkingStyleFillerMesh",
                    bounds = new Bounds(new Vector3(0f, 0f, 0f), new Vector3(128, 57, 128)),
                };

                if (rawData.meshType == MeshType.Side)
                {
                    var vertices = new List<Vector3>();
                    var triangles = new List<int>();

                    var index = 0;
                    for (var j = 0; j < rawData.groups.Length; j += 1)
                    {
                        for (var k = 0; k <= rawData.groups[j]; k += 1)
                        {
                            var point = rawData.points[index % rawData.points.Length] - delta;

                            vertices.Add(point);
                            point.y -= elevation + 0.5f;
                            vertices.Add(point);

                            if (k != 0)
                            {
                                triangles.Add(vertices.Count - 4);
                                triangles.Add(vertices.Count - 1);
                                triangles.Add(vertices.Count - 2);

                                triangles.Add(vertices.Count - 1);
                                triangles.Add(vertices.Count - 4);
                                triangles.Add(vertices.Count - 3);
                            }
                            index += 1;
                        }
                        index -= 1;
                    }


                    mesh.vertices = vertices.ToArray();
                    mesh.triangles = triangles.ToArray();
                }
                else
                {
                    mesh.vertices = rawData.points.Select(p => p - delta).ToArray();
                    mesh.triangles = rawData.polygons;
                }

                mesh.RecalculateNormals();
                if (rawData.meshType == MeshType.Top)
                    mesh.normals = mesh.normals.Select(n => -n).ToArray();
                mesh.RecalculateTangents();
                mesh.UploadMeshData(false);

                Datas[i] = new RenderData(mesh, rawData.color, rawData.textureData);
            }
        }
        ~FillerMeshData()
        {
            if (Datas != null)
            {
                for (var i = 0; i < Datas.Length; i += 1)
                {
                    if (Datas[i].mesh != null)
                    {
                        Object.Destroy(Datas[i].mesh);
#if DEBUG
                        SingletonMod<Mod>.Logger.Debug("Destroy mesh");
#endif
                    }
                }
            }
        }

        public void Render(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data, bool infoView)
        {
            var instance = Singleton<PropManager>.instance;
            var materialBlock = instance.m_materialBlock;

            for (var i = 0; i < Datas.Length; i += 1)
            {
                materialBlock.Clear();

                if (!infoView && Datas[i].textureData.mainTexture != null)
                    materialBlock.SetTexture(mainTexId, Datas[i].textureData.mainTexture);

                materialBlock.SetVector(colorId, infoView ? new Color(0.4f, 0.4f, 0.4f, 0f) : Datas[i].color);
                materialBlock.SetVector(tilingId, Datas[i].textureData.tiling);

                var material = RenderHelper.MaterialLib[MaterialType.FillerTexture];
                Graphics.DrawMesh(Datas[i].mesh, Position, Quaternion.identity, material, RenderHelper.RoadLayer, null, 0, materialBlock);
            }
        }

        public bool CalculateGroupData(int layer, ref int vertexCount, ref int triangleCount, ref int objectCount, ref RenderGroup.VertexArrays vertexArrays)
        {
            return false;

            foreach (var data in Datas)
            {
                vertexCount += data.mesh.vertexCount;
                triangleCount += data.mesh.triangles.Length;
                objectCount += 1;
            }

            return true;
        }

        public void PopulateGroupData(int layer, ref int vertexIndex, ref int triangleIndex, Vector3 groupPosition, RenderGroup.MeshData groupData, ref Vector3 min, ref Vector3 max, ref float maxRenderDistance, ref float maxInstanceDistance, ref bool requireSurfaceMaps)
        {
            return;

            foreach (var data in Datas)
            {
                var triangles = data.mesh.triangles;
                for (int i = 0; i < triangles.Length; i += 1)
                {
                    groupData.m_triangles[triangleIndex++] = triangles[i] + vertexIndex;
                }

                var vertices = data.mesh.vertices;
                for (int i = 0; i < vertices.Length; i += 1)
                {
                    groupData.m_vertices[vertexIndex] = Position - groupPosition + vertices[i];
                    vertexIndex += 1;
                }
            }
        }
    }
}
