using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions.Must;

namespace NodeMarkup.Utils
{
    public static class RenderHelper
    {
        public static Dictionary<MaterialType, Material> MaterialLib { get; private set; } = Init();
        private static Dictionary<MaterialType, Material> Init()
        {
            var assembly = Assembly.GetExecutingAssembly();
            return new Dictionary<MaterialType, Material>()
            {
                { MaterialType.RectangleLines, CreateDecalMaterial(TextureHelper.CreateTexture(1,1,Color.white))},
                { MaterialType.RectangleFillers, CreateDecalMaterial(TextureHelper.CreateTexture(1,1,Color.white), renderQueue: 2459)},
                { MaterialType.Triangle, CreateDecalMaterial(TextureHelper.CreateTexture(64,64,Color.white), assembly.LoadTextureFromAssembly("SharkTooth"))},
                { MaterialType.Pavement, CreateRoadMaterial(TextureHelper.CreateTexture(512,512,Color.white), CreateTextTexture(512,512)) },
            };
        }

        public static int ID_DecalSize { get; } = Shader.PropertyToID("_DecalSize");
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
        static int VCount { get; } = 24;
        static int TCount { get; } = 36;

        public static Mesh CreateMesh(int count, Vector3 size)
        {
            var vertices = new Vector3[VCount * count];
            var triangles = new int[TCount * count];
            var colors32 = new Color32[VCount * count];
            var uv = new Vector2[VCount * count];

            CreateTemp(size.x, size.y, size.z, out Vector3[] tempV, out int[] tempT);

            for (var i = 0; i < count; i += 1)
            {
                var tempColor = VerticesColor(i);

                for (var j = 0; j < VCount; j += 1)
                {
                    vertices[i * VCount + j] = tempV[j];
                    colors32[i * VCount + j] = tempColor;
                    uv[i * VCount + j] = Vector2.zero;
                }
                for (var j = 0; j < TCount; j += 1)
                {
                    triangles[i * TCount + j] = tempT[j] + (VCount * i);
                }
            }

            Bounds bounds = default;
            bounds.SetMinMax(new Vector3(-100000f, -100000f, -100000f), new Vector3(100000f, 100000f, 100000f));

            var mesh = new Mesh();
            mesh.Clear();

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.colors32 = colors32;
            mesh.bounds = bounds;
            mesh.uv = uv;

            return mesh;
        }
        private static void CreateTemp(float length, float height, float width, out Vector3[] vertices, out int[] triangles)
        {
            Vector3[] c = new Vector3[8];

            c[0] = new Vector3(-length * .5f, -height * .5f, width * .5f); //0 --+
            c[1] = new Vector3(length * .5f, -height * .5f, width * .5f);  //1 +-+
            c[2] = new Vector3(length * .5f, -height * .5f, -width * .5f); //2 +--
            c[3] = new Vector3(-length * .5f, -height * .5f, -width * .5f);//3 ---

            c[4] = new Vector3(-length * .5f, height * .5f, width * .5f);  //4 -++
            c[5] = new Vector3(length * .5f, height * .5f, width * .5f);   //5 +++
            c[6] = new Vector3(length * .5f, height * .5f, -width * .5f);  //6 ++-
            c[7] = new Vector3(-length * .5f, height * .5f, -width * .5f); //7 -+-

            vertices = new Vector3[24];
            triangles = new int[36];

            for (var j = 0; j < 24; j += 1)
            {
                vertices[j] = c[VerticesIdxs[j]];
            }
            for (var j = 0; j < 36; j += 1)
            {
                triangles[j] = TrianglesIdxs[j];
            }
        }
        public static Color32 VerticesColor(int i) => new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)(16 * i));

        public static Material CreateDecalMaterial(Texture2D texture, Texture2D aci = null, int renderQueue = 2460)
        {
            var material = new Material(Shader.Find("Custom/Props/Decal/Blend"))
            {
                mainTexture = texture,
                name = "NodeMarkupDecal",
                color = new Color(1f, 1f, 1f, 1f),
                doubleSidedGI = false,
                enableInstancing = false,
                globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack,
                renderQueue = renderQueue,
            };
            if (aci != null)
                material.SetTexture("_ACIMap", aci);

            material.EnableKeyword("MULTI_INSTANCE");

            var tiling = new Vector4(1f, 0f, 1f, 0f);
            material.SetVector("_DecalTiling", tiling);

            return material;
        }
        public static Material CreateRoadMaterial(Texture2D texture, Texture2D apr = null, int renderQueue = 2461)
        {
            var material = new Material(Shader.Find("Custom/Net/Road"))
            {
                mainTexture = texture,
                name = "NodeMarkupRoad",
                color = new Color(0.5f, 0.5f, 0.5f, 0f),
                globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack & MaterialGlobalIlluminationFlags.RealtimeEmissive,
                renderQueue = renderQueue,
            };
            if (apr != null)
                material.SetTexture("_APRMap", apr);

            material.EnableKeyword("NET_SEGMENT");

            return material;
        }
        public static Texture2D CreateTextTexture(int height, int width)
        {
            var texture = new Texture2D(height, width) { name = "Markup" };
            //var count = 8f;
            for (var i = 0; i < width; i += 1)
            {
                Color color;

                if (i < width / 2)
                    color = Color.black;
                else
                    color = new Color(1f, 1f, 0f, 1f);

                //if(i<width / count * 1)
                //    color = new Color32(255, 0, 0, 255);
                //else if (i < width / count * 2)
                //    color = new Color32(0, 255, 0, 255);
                //else if (i < width / count * 3)
                //    color = new Color32(0, 0, 255, 255);
                //else if (i < width / count * 4)
                //    color = new Color32(255, 255, 0, 255);
                //else if (i < width / count * 5)
                //    color = new Color32(255, 0, 255, 255);
                //else if (i < width / count * 6)
                //    color = new Color32(0, 255, 255, 255);
                //else if (i < width / count * 7)
                //    color = new Color32(255, 255, 255, 255);
                //else
                //    color = new Color32(0, 0, 0, 255);

                for (var j = 0; j < height; j += 1)
                    texture.SetPixel(i, j, color);
            }
            texture.Apply();
            return texture;
        }

        public static Texture2D CreateChessBoardTexture()
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
    }

    public interface IDrawData
    {
        public void Draw();
    }

    public class MarkupStyleDash
    {
        public MaterialType MaterialType { get; set; }
        public Vector3 Position { get; set; }
        public float Angle { get; set; }
        public float Length { get; set; }
        public float Width { get; set; }
        public Color32 Color { get; set; }

        public MarkupStyleDash(Vector3 position, float angle, float length, float width, Color32 color, MaterialType materialType = MaterialType.RectangleLines)
        {
            Position = position;
            Angle = angle;
            Length = length;
            Width = width;
            Color = color;
            MaterialType = materialType;
        }
        public MarkupStyleDash(Vector3 start, Vector3 end, float angle, float length, float width, Color32 color, MaterialType materialType = MaterialType.RectangleLines)
            : this((start + end) / 2, angle, length, width, color, materialType) { }

        public MarkupStyleDash(Vector3 start, Vector3 end, Vector3 dir, float length, float width, Color32 color, MaterialType materialType = MaterialType.RectangleLines)
            : this(start, end, dir.AbsoluteAngle(), length, width, color, materialType) { }

        public MarkupStyleDash(Vector3 start, Vector3 end, Vector3 dir, float width, Color32 color, MaterialType materialType = MaterialType.RectangleLines)
            : this(start, end, dir, (end - start).magnitude, width, color, materialType) { }

        public MarkupStyleDash(Vector3 start, Vector3 end, float width, Color32 color, MaterialType materialType = MaterialType.RectangleLines)
            : this(start, end, end - start, (end - start).magnitude, width, color, materialType) { }
    }
    public class MarkupStyleDashes : IStyleData, IEnumerable<MarkupStyleDash>
    {
        private List<MarkupStyleDash> Dashes { get; }

        public MarkupStyleDashes() => Dashes = new List<MarkupStyleDash>();
        public MarkupStyleDashes(IEnumerable<MarkupStyleDash> dashes) => Dashes = dashes.ToList();

        public IEnumerator<MarkupStyleDash> GetEnumerator() => Dashes.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerable<IDrawData> GetDrawData() => RenderBatch.FromDashes(this);
    }
    public class MarkupStyleMesh : IStyleData, IDrawData
    {
        private static float HalfWidth => 10f;
        private static float HalfLength => 10f;
        private static Vector4 Scale { get; } = new Vector4(0.5f / HalfWidth, 0.5f / HalfLength, 1f, 1f);

        private Vector3 Position;
        private Vector3[] Vertices { get; set; }
        private int[] Triangles { get; set; }
        private Vector2[] UV { get; set; }
        public MaterialType MaterialType { get; }
        public Matrix4x4 Left { get; private set; }
        public Matrix4x4 Right { get; private set; }
        private Texture SurfaceTexA { get; }
        private Texture SurfaceTexB { get; }
        private Vector4 SurfaceMapping { get; }

        private Mesh Mesh { get; set; }

        public MarkupStyleMesh(float height, Vector3[] vertices, int[] triangles, MaterialType materialType)
        {
            var minMax = Rect.MinMaxRect(vertices.Min(p => p.x), vertices.Min(p => p.z), vertices.Max(p => p.x), vertices.Max(p => p.z));

            Position = new Vector3(minMax.center.x, height + 0.3f, minMax.center.y);
            MaterialType = materialType;

            ItemsExtension.TerrainManager.GetSurfaceMapping(Position, out var surfaceTexA, out var surfaceTexB, out var surfaceMapping);
            SurfaceTexA = surfaceTexA;
            SurfaceTexB = surfaceTexB;
            SurfaceMapping = surfaceMapping;

            CalculateVertices(vertices, minMax);
            CalculateTriangles(triangles);
            CalculateMatrix(minMax);
        }
        private void CalculateVertices(Vector3[] vertices, Rect minMax)
        {
            var xRatio = (2 * HalfWidth) / minMax.width;
            var yRatio = (2 * HalfLength) / minMax.height;
            Vertices = vertices.Select(v => new Vector3((v.x - minMax.center.x) * xRatio, v.y - Position.y + 0.3f, (v.z - minMax.center.y) * yRatio)).ToArray();
            UV = Vertices.Select(v => new Vector2((1f - (v.x / HalfWidth)) / 2f, (1f - (v.z / HalfLength)) / 2f)).ToArray();

        }
        private void CalculateTriangles(int[] triangles)
        {
            Triangles = new int[triangles.Length];
            for (var i = 0; i < triangles.Length; i += 3)
            {
                Triangles[i] = triangles[i + 2];
                Triangles[i + 1] = triangles[i + 1];
                Triangles[i + 2] = triangles[i];
            }
        }
        private void CalculateMatrix(Rect minMax)
        {
            var left = new Bezier3()
            {
                a = new Vector3(-minMax.width / 2, 0f, -minMax.height / 2),
                d = new Vector3(-minMax.width / 2, 0f, minMax.height / 2)
            };
            var right = new Bezier3()
            {
                a = new Vector3(minMax.width / 2, 0f, -minMax.height / 2),
                d = new Vector3(minMax.width / 2, 0f, minMax.height / 2)
            };

            var leftDir = (left.d - left.a).normalized;
            var rightDir = (right.d - right.a).normalized;

            NetSegment.CalculateMiddlePoints(left.a, leftDir, left.d, -leftDir, true, true, out left.b, out left.c);
            NetSegment.CalculateMiddlePoints(right.a, rightDir, right.d, -rightDir, true, true, out right.b, out right.c);

            Left = NetSegment.CalculateControlMatrix(left.a, left.b, left.c, left.d, right.a, right.b, right.c, right.d, Vector3.zero, 0.05f);
            Right = NetSegment.CalculateControlMatrix(right.a, right.b, right.c, right.d, left.a, left.b, left.c, left.d, Vector3.zero, 0.05f);
        }

        public IEnumerable<IDrawData> GetDrawData()
        {
            if (Mesh == null)
            {
                Mesh = new Mesh
                {
                    vertices = Vertices,
                    triangles = Triangles,
                    bounds = new Bounds(new Vector3(0f, 0f, 0f), new Vector3(128, 57, 128)),
                    uv = UV,
                };

                Mesh.RecalculateNormals();
                Mesh.RecalculateTangents();
            }
            yield return this;
        }
        public void Draw()
        {
            var instance = ItemsExtension.NetManager;
            var materialBlock = instance.m_materialBlock;

            materialBlock.Clear();
            materialBlock.SetMatrix(instance.ID_LeftMatrix, Left);
            materialBlock.SetMatrix(instance.ID_RightMatrix, Right);
            materialBlock.SetVector(instance.ID_MeshScale, Scale);
            //materialBlock.SetVector(instance.ID_Color, Color);

            materialBlock.SetTexture(instance.ID_SurfaceTexA, SurfaceTexA);
            materialBlock.SetTexture(instance.ID_SurfaceTexB, SurfaceTexB);
            materialBlock.SetVector(instance.ID_SurfaceMapping, SurfaceMapping);

            Graphics.DrawMesh(Mesh, Position, Quaternion.identity, RenderHelper.MaterialLib[MaterialType], 9, null, 0, materialBlock);
        }
    }

    public class RenderBatch : IDrawData
    {
        public static float MeshHeight => 3f;
        public MaterialType MaterialType { get; }
        public int Count { get; }
        public Vector4[] Locations { get; }
        public Vector4[] Indices { get; }
        public Vector4[] Colors { get; }
        public Mesh Mesh { get; }

        public Vector4 Size { get; }

        public RenderBatch(MarkupStyleDash[] dashes, int count, Vector3 size, MaterialType materialType)
        {
            MaterialType = materialType;
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

        public static IEnumerable<IDrawData> FromDashes(IEnumerable<MarkupStyleDash> dashes)
        {
            var materialGroups = dashes.GroupBy(d => d.MaterialType);

            foreach (var materialGroup in materialGroups)
            {
                var sizeGroups = materialGroup.Where(d => d.Length >= 0.1f).GroupBy(d => new Vector3(Round(d.Length), MeshHeight, d.Width));

                foreach (var sizeGroup in sizeGroups)
                {
                    var groupEnumerator = sizeGroup.GetEnumerator();

                    var buffer = new MarkupStyleDash[16];
                    var count = 0;

                    bool isEnd = groupEnumerator.MoveNext();
                    do
                    {
                        buffer[count] = groupEnumerator.Current;
                        count += 1;
                        isEnd = !groupEnumerator.MoveNext();
                        if (isEnd || count == 16)
                        {
                            var batch = new RenderBatch(buffer, count, sizeGroup.Key, materialGroup.Key);
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

        public void Draw()
        {
            var instance = ItemsExtension.PropManager;
            var materialBlock = instance.m_materialBlock;
            materialBlock.Clear();

            materialBlock.SetVectorArray(instance.ID_PropLocation, Locations);
            materialBlock.SetVectorArray(instance.ID_PropObjectIndex, Indices);
            materialBlock.SetVectorArray(instance.ID_PropColor, Colors);
            materialBlock.SetVector(RenderHelper.ID_DecalSize, Size);

            var mesh = Mesh;
            var material = RenderHelper.MaterialLib[MaterialType];

            Graphics.DrawMesh(mesh, Matrix4x4.identity, material, 9, null, 0, materialBlock);
        }
    }
}
