using ColossalFramework.UI;
using NodeMarkup.Manager;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            return new Dictionary<MaterialType, Material>()
            {
                { MaterialType.RectangleLines, CreateDecalMaterial(CreateTexture(1,1,Color.white))},
                { MaterialType.RectangleFillers, CreateDecalMaterial(CreateTexture(1,1,Color.white), renderQueue: 2459)},
                { MaterialType.Triangle, CreateDecalMaterial(CreateTexture(64,64,Color.white), TextureUtil.LoadTextureFromAssembly("SharkTooth.png", 64,64))},
                { MaterialType.Pavement, CreateRoadMaterial(CreateTexture(64,64,Color.white), CreateTexture(64,64,new Color32(0,0,0,255))) },
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
            var material = new Material(Shader.Find("Custom/Net/RoadBridge"))
            {
                mainTexture = texture,
                name = "NodeMarkupRoad",
                color = new Color(0.5f, 0.5f, 0.5f, 0f),
                doubleSidedGI = false,
                enableInstancing = false,
                globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack,
                renderQueue = renderQueue,
            };
            if (apr != null)
                material.SetTexture("_APRMap", apr);

            return material;
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
        public static Texture2D CreateTexture(int height, int width, Color color)
        {
            var texture = new Texture2D(height, width) { name = "Markup" };
            for (var i = 0; i < width; i += 1)
            {
                for (var j = 0; j < height; j += 1)
                    texture.SetPixel(i, j, color);
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
        public Color Color { get; set; }

        public MarkupStyleDash(Vector3 position, float angle, float length, float width, Color color, MaterialType materialType = MaterialType.RectangleLines)
        {
            Position = position;
            Angle = angle;
            Length = length;
            Width = width;
            Color = color;
            MaterialType = materialType;
        }
        public MarkupStyleDash(Vector3 start, Vector3 end, float angle, float length, float width, Color color, MaterialType materialType = MaterialType.RectangleLines)
            : this((start + end) / 2, angle, length, width, color, materialType) { }

        public MarkupStyleDash(Vector3 start, Vector3 end, Vector3 dir, float length, float width, Color color, MaterialType materialType = MaterialType.RectangleLines)
            : this(start, end, dir.AbsoluteAngle(), length, width, color, materialType) { }

        public MarkupStyleDash(Vector3 start, Vector3 end, Vector3 dir, float width, Color color, MaterialType materialType = MaterialType.RectangleLines)
            : this(start, end, dir, (end - start).magnitude, width, color, materialType) { }

        public MarkupStyleDash(Vector3 start, Vector3 end, float width, Color color, MaterialType materialType = MaterialType.RectangleLines)
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
        private Vector3 Position;
        private Vector3[] Vertices { get; }
        private int[] Triangles { get; }
        private Vector2[] UV { get; }
        public MaterialType MaterialType { get; }
        public float ScaleX { get; }
        public float ScaleY { get; }
        public Rect Rect { get; }

        private Mesh Mesh { get; set; }

        public MarkupStyleMesh(Rect rect, float height, Vector3[] vertices, int[] triangles, MaterialType materialType, float scaleX, float scaleY)
        {
            Position = new Vector3(rect.center.x, height + 0.3f, rect.center.y);
            MaterialType = materialType;
            Vertices = vertices;
            Triangles = triangles;
            UV = GetUV(rect).ToArray();
            ScaleX = scaleX;
            ScaleY = scaleY;
        }
        private IEnumerable<Vector2> GetUV(Rect rect)
        {
            foreach (var vertex in Vertices)
                yield return new Vector2((vertex.x - rect.x) / rect.width, (vertex.z - rect.y) / rect.height);
        }

        public IEnumerable<IDrawData> GetDrawData()
        {
            if (Mesh == null)
            {
                Mesh = new Mesh();

                Bounds bounds = default;
                bounds.SetMinMax(new Vector3(-100000f, -100000f, -100000f), new Vector3(100000f, 100000f, 100000f));

                Mesh.vertices = Vertices;
                Mesh.triangles = Triangles;
                Mesh.normals = Vertices.Select(v => new Vector3(0f, 1f, 0f)).ToArray();
                Mesh.tangents = Vertices.Select(v => new Vector4(-1f, 0f, 0f, -1f)).ToArray();
                Mesh.bounds = bounds;
                Mesh.uv = UV;
            }
            yield return this;
        }
        public void Draw()
        {
            var instance = Utilities.NetManager;
            var materialBlock = instance.m_materialBlock;
            materialBlock.Clear();

            materialBlock.SetVector(instance.ID_MeshScale, new Vector4(ScaleX, ScaleY, 1f, 0f));

            var mesh = Mesh;
            var material = RenderHelper.MaterialLib[MaterialType];

            Graphics.DrawMesh(mesh, Position, Quaternion.identity, material, 10, null, 0, materialBlock);
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
            var instance = Utilities.PropManager;
            var materialBlock = instance.m_materialBlock;
            materialBlock.Clear();

            materialBlock.SetVectorArray(instance.ID_PropLocation, Locations);
            materialBlock.SetVectorArray(instance.ID_PropObjectIndex, Indices);
            materialBlock.SetVectorArray(instance.ID_PropColor, Colors);
            materialBlock.SetVector(RenderHelper.ID_DecalSize, Size);

            var mesh = Mesh;
            var material = RenderHelper.MaterialLib[MaterialType];

            Graphics.DrawMesh(mesh, Matrix4x4.identity, material, 10, null, 0, materialBlock);
        }
    }
}
