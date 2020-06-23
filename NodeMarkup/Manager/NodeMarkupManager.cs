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

        public static bool HasMarkup(ushort nodeId) => NodesMarkup.ContainsKey(nodeId);

        static NodeMarkupManager()
        {
            Material = CreateMaterial();
            Mesh = CreateMesh();
        }
        private static Texture2D CreateTexture()
        {
            var height = 256;
            var width = 256;
            var texture = new Texture2D(height, width)
            {
                name = "Markup",
            };
            for(var i = 0; i < height * width; i += 1)
            {
                var row = i / height;
                var column = i % width;

                var colorRow = row / (height / 4);
                var colorColumn = column / (width / 4);
                var color = (colorColumn + colorRow) % 2 == 0 ? 0f : 1f;
                texture.SetPixel(row, column, new Color(color, color, color, 1));
            }

            //texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return texture;
        }
        private static Material CreateMaterial()
        {
            var texture = CreateTexture();

            var material = new Material(Shader.Find("Custom/Props/Decal/Blend"))
            {
                mainTexture = texture,
                //mainTextureScale = new Vector2(1f, 1f),
                name = "NodeMarkup",
                color = new Color(1f, 1f, 1f, 0.5f),
                doubleSidedGI = false,
                enableInstancing = false,
                globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack,
                shaderKeywords = new string[] { "MULTI_INSTANCE" }
            };

            var size = new Vector2(8.0f, 8.0f);
            var tile = new Vector3(1.0f, 1.0f);
            var slopeTolerance = Mathf.Clamp((size.x + size.y) / 4f, 2f, 32f);

            var scale = new Vector4(size.x, slopeTolerance, size.y, 0);
            var tiling = new Vector4(tile.x, 0, tile.y, 0);

            material.SetVector("_DecalSize", scale);
            material.SetVector("_DecalTiling", tiling);

            return material;
        }
        private static Mesh CreateMesh()
        {
            float length = 8f;
            float width = 8f;
            float height = 8f;

            Vector3[] c = new Vector3[8];

            c[0] = new Vector3(-length * .5f, -width * .5f, height * .5f); //0 --+
            c[1] = new Vector3(length * .5f, -width * .5f, height * .5f);  //1 +-+
            c[2] = new Vector3(length * .5f, -width * .5f, -height * .5f); //2 +--
            c[3] = new Vector3(-length * .5f, -width * .5f, -height * .5f);//3 ---

            c[4] = new Vector3(-length * .5f, width * .5f, height * .5f);  //4 -++
            c[5] = new Vector3(length * .5f, width * .5f, height * .5f);   //5 +++
            c[6] = new Vector3(length * .5f, width * .5f, -height * .5f);  //6 ++-
            c[7] = new Vector3(-length * .5f, width * .5f, -height * .5f); //7 -+-

            Vector3[] vertices = new Vector3[]
            {
            c[1], c[3], c[2], c[0], // Bottom
            c[5], c[7], c[4], c[6], // Top
            c[1], c[4], c[0], c[5], // Front
            c[0], c[7], c[3], c[4], // Left
            c[3], c[6], c[2], c[7], // Back
            c[2], c[5], c[1], c[6], // Right
            };

            int[] triangles = new int[]
            {
                0,1,2,      1,0,3,      // Bottom
                4,5,6,      5,4,7,      // Top
                8,9,10,     9,8,11,     // Front
                12,13,14,   13,12,15,   // Left 
                16,17,18,   17,16,19,   // Back
                20,21,22,   21,20,23    // Right
            };

            var colors = Enumerable.Range(0, vertices.Length).Select(i => new Color(1f, 1f, 1f, 0f)).ToArray();

            var mesh = new Mesh()
            {
                vertices = vertices,
                triangles = triangles,
                colors = colors,
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

            foreach (var line in markup.Lines)
            {
                var propinfo = PrefabCollection<PropInfo>.FindLoaded("Road Arrow F");

                var materialBlock = instance.m_materialBlock;
                materialBlock.Clear();

                var pos = line.Trajectory.Position(0.5f);

                //var loc = new Vector4[] { pos };
                //materialBlock.SetVectorArray(instance.ID_PropLocation, loc);
                var index = new Vector4[] { new Vector4(0f, 0f, 0f, 1f) };
                materialBlock.SetVectorArray(instance.ID_PropObjectIndex, index);
                var color = new Vector4[] { new Vector4(1f, 1f, 1f, 1f) };
                materialBlock.SetVectorArray(instance.ID_PropColor, color);

                var mesh = Mesh;
                Bounds bounds = default;
                bounds.SetMinMax(pos - new Vector3(100f, 100f, 100f), pos + new Vector3(100f, 100f, 100f));
                mesh.bounds = bounds;

                //var material = propinfo.m_lodMaterialCombined;
                var material = Material;

                //var trs = new Matrix4x4();
                //trs.SetTRS();

                Graphics.DrawMesh(mesh, Matrix4x4.identity, material, 10, null, 0, materialBlock);
            }
        }
    }
}
