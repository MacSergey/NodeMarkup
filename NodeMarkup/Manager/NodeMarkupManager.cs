using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.PlatformServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public static class NodeMarkupManager
    {
        static Dictionary<ushort, Markup> NodesMarkup { get; } = new Dictionary<ushort, Markup>();
        static float RenderDistance => 500f;
        static PropManager PropManager => Singleton<PropManager>.instance;
        static Mesh Mesh { get; set; }
        static Material Material { get; set; }
        static int ID_DecalSize { get; } = Shader.PropertyToID("_DecalSize");
        static int ID_DecalTiling { get; } = Shader.PropertyToID("_DecalTiling");

        static int[] VerticesIdxs { get; } = new int[]
        {
            1,3,2,0,// Bottom
            5,7,4,6,// Top
            1,4,0,5,// Front
            0,7,3,4,// Left
            3,6,2,7,// Back
            2,5,1,6, // Right
        };
        static int[] TrianglesIdxs { get; } = new int[]
        {
                0,1,2,      1,0,3,      // Bottom
                4,5,6,      5,4,7,      // Top
                8,9,10,     9,8,11,     // Front
                12,13,14,   13,12,15,   // Left 
                16,17,18,   17,16,19,   // Back
                20,21,22,   21,20,23,    // Right
        };

        public static bool HasMarkup(ushort nodeId) => NodesMarkup.ContainsKey(nodeId);

        static NodeMarkupManager()
        {
            Material = CreateMaterial();
            Mesh = CreateMesh(16);
        }
        private static Texture2D CreateTexture()
        {
            var height = 256;
            var width = 256;
            var texture = new Texture2D(height, width)
            {
                name = "Markup",
            };
            for (var i = 0; i < height * width; i += 1)
            {
                var row = i / height;
                var column = i % width;

                var colorRow = row / (height / 4);
                var colorColumn = column / (width / 4);
                var color = (colorColumn + colorRow) % 2 == 0 ? 0f : 1f;
                texture.SetPixel(row, column, new Color(color, color, color, 1));
            }

            texture.Apply();
            return texture;
        }
        private static Material CreateMaterial()
        {
            var texture = CreateTexture();

            var material = new Material(Shader.Find("Custom/Props/Decal/Blend"))
            {
                mainTexture = texture,
                name = "NodeMarkup",
                color = new Color(1f, 1f, 1f, 0.1960784f),
                doubleSidedGI = false,
                enableInstancing = false,
                globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack,
            };
            material.EnableKeyword("MULTI_INSTANCE");

            var size = new Vector2(MarkupLine.Dash, 0.15f);
            var tile = new Vector3(1.0f, 1.0f);
            var slopeTolerance = Mathf.Clamp((size.x + size.y) / 4f, 2f, 32f);

            var scale = new Vector4(size.x, slopeTolerance, size.y, 0);
            var tiling = new Vector4(tile.x, 0, tile.y, 0);

            material.SetVector("_DecalSize", scale);
            material.SetVector("_DecalTiling", tiling);

            return material;
        }
        private static Mesh CreateMesh(int count = 1)
        {
            float length = 10f;
            float width = 10f;
            float height = 10f;

            Vector3[] c = new Vector3[8];

            c[0] = new Vector3(-length * .5f, -width * .5f, height * .5f); //0 --+
            c[1] = new Vector3(length * .5f, -width * .5f, height * .5f);  //1 +-+
            c[2] = new Vector3(length * .5f, -width * .5f, -height * .5f); //2 +--
            c[3] = new Vector3(-length * .5f, -width * .5f, -height * .5f);//3 ---

            c[4] = new Vector3(-length * .5f, width * .5f, height * .5f);  //4 -++
            c[5] = new Vector3(length * .5f, width * .5f, height * .5f);   //5 +++
            c[6] = new Vector3(length * .5f, width * .5f, -height * .5f);  //6 ++-
            c[7] = new Vector3(-length * .5f, width * .5f, -height * .5f); //7 -+-


            Vector3[] vertices = new Vector3[count * 24];
            int[] triangles = new int[count * 36];
            var colors32 = new Color32[count * 24];

            for (var i = 0; i < count; i += 1)
            {
                for (var j = 0; j < 24; j += 1)
                {
                    vertices[i * 24 + j] = c[VerticesIdxs[j]];
                    colors32[i * 24 + j] = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)(16 * i));
                }
                for (var j = 0; j < 36; j += 1)
                {
                    triangles[i * 36 + j] = TrianglesIdxs[j] + (24 * i);
                }
            }

            var uv = Enumerable.Range(0, vertices.Length).Select(i => new Vector2()).ToArray();

            Bounds bounds = default;
            bounds.SetMinMax(new Vector3(-1000f, -1000f, -1000f), new Vector3(1000f, 1000f, 1000f));

            var mesh = new Mesh()
            {
                vertices = vertices,
                triangles = triangles,
                colors32 = colors32,
                bounds = bounds,
            };

            return mesh;
        }

        public static Markup Get(ushort nodeId)
        {
            if (!NodesMarkup.TryGetValue(nodeId, out Markup markup))
            {
                markup = new Markup(nodeId);
                NodesMarkup[nodeId] = markup;
            }

            return markup;
        }

        public static void NetNodeRenderPostfix(RenderManager.CameraInfo cameraInfo, ushort nodeID, ref RenderManager.Instance data)
        {
            if (!HasMarkup(nodeID))
                return;

            if (!cameraInfo.CheckRenderDistance(data.m_position, RenderDistance))
                return;

            var markup = NodesMarkup[nodeID];
            var instance = PropManager;
            var materialBlock = instance.m_materialBlock;

            foreach(var batch in markup.RenderBatchs)
            {
                materialBlock.Clear();
                materialBlock.SetVectorArray(instance.ID_PropLocation, batch.Locations);
                materialBlock.SetVectorArray(instance.ID_PropObjectIndex, batch.Indices);
                materialBlock.SetVectorArray(instance.ID_PropColor, batch.Colors);

                var mesh = Mesh;
                var material = Material;

                Graphics.DrawMesh(mesh, Matrix4x4.identity, material, 10, null, 0, materialBlock);
            }    


            //foreach (var line in markup.Lines)
            //{
            //    var propinfo = PrefabCollection<PropInfo>.FindLoaded("Road Arrow F");

            //    var materialBlock = instance.m_materialBlock;
            //    materialBlock.Clear();

            //    var pos1 = line.Trajectory.Position(0.25f);
            //    var pos2 = line.Trajectory.Position(0.75f);

            //    var loc = new Vector4[16];
            //    loc[0] = pos1;
            //    loc[1] = pos2;
            //    materialBlock.SetVectorArray(instance.ID_PropLocation, loc);

            //    var index = new Vector4[16];
            //    index[0] = new Vector4(0f, 0f, 0f, 1f);
            //    index[1] = new Vector4(0f, 0f, 0f, 1f);
            //    materialBlock.SetVectorArray(instance.ID_PropObjectIndex, index);

            //    var color = new Vector4[16];
            //    color[0] = new Vector4(1f, 1f, 1f, 0.5f);
            //    color[1] = new Vector4(1f, 1f, 1f, 0.5f);
            //    materialBlock.SetVectorArray(instance.ID_PropColor, color);

            //    var mesh = CreateMesh(2);
            //    var material = Material;

            //    Graphics.DrawMesh(mesh, Matrix4x4.identity, material, 10, null, 0, materialBlock);
            //}
        }
    }
}
