using ColossalFramework;
using ColossalFramework.Math;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NodeMarkup.Utilities
{
    public static class RenderHelper
    {
        public static Dictionary<MaterialType, Material> MaterialLib { get; private set; } = new Dictionary<MaterialType, Material>()
        {
            { MaterialType.RectangleLines, CreateDecalMaterial(TextureHelper.CreateTexture(1,1,Color.white))},
            { MaterialType.RectangleFillers, CreateDecalMaterial(TextureHelper.CreateTexture(1,1,Color.white), renderQueue: 2459)},
            { MaterialType.Triangle, CreateDecalMaterial(TextureHelper.CreateTexture(64,64,Color.white), Assembly.GetExecutingAssembly().LoadTextureFromAssembly("SharkTooth"))},
            { MaterialType.Pavement, CreateRoadMaterial(TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,Color.black)) },
            { MaterialType.Grass, CreateRoadMaterial(TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,new Color32(255, 0, 0, 255)))},
            { MaterialType.Gravel, CreateRoadMaterial(TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,Color.black))},
            { MaterialType.Asphalt, CreateRoadMaterial(TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,new Color32(0, 255, 255, 255)))},
            { MaterialType.Ruined, CreateRoadMaterial(TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,Color.black))},
            { MaterialType.Cliff, CreateRoadMaterial(TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,Color.black))},
        };
        public static Dictionary<MaterialType, Texture2D> SurfaceALib { get; } = new Dictionary<MaterialType, Texture2D>()
        {
            { MaterialType.Pavement, TextureHelper.CreateTexture(512, 512, new Color32(255, 255, 127, 127)) },
            { MaterialType.Grass, TextureHelper.CreateTexture(512, 512, new Color32(255, 255, 127, 127)) },
            { MaterialType.Gravel, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 127, 127)) },
            { MaterialType.Asphalt, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 0, 127)) },
            { MaterialType.Ruined, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 191, 127)) },
            { MaterialType.Cliff, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 127, 191)) },
        };
        public static Dictionary<MaterialType, Texture2D> SurfaceBLib { get; } = new Dictionary<MaterialType, Texture2D>()
        {
            { MaterialType.Pavement, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 0, 0)) },
            { MaterialType.Grass, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 0, 0)) },
            { MaterialType.Gravel, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 255, 0)) },
            { MaterialType.Asphalt, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 0, 0)) },
            { MaterialType.Ruined, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 0, 0)) },
            { MaterialType.Cliff, TextureHelper.CreateTexture(512, 512, new Color32(0, 0, 0, 0)) },
        };


        public static int ID_DecalSize { get; } = Shader.PropertyToID("_DecalSize");
        private static int[] VerticesIdxs { get; } = new int[]
{
            1,3,2,0,// Bottom
            5,7,4,6,// Top
            1,4,0,5,// Front
            0,7,3,4,// Left
            3,6,2,7,// Back
            2,5,1,6, // Right
};
        private static int[] TrianglesIdxs { get; } = new int[]
        {
                0,1,2,      1,0,3,      // Bottom
                4,5,6,      5,4,7,      // Top
                8,9,10,     9,8,11,     // Front
                12,13,14,   13,12,15,   // Left 
                16,17,18,   17,16,19,   // Back
                20,21,22,   21,20,23,    // Right
        };
        private static int VCount { get; } = 24;
        private static int TCount { get; } = 36;

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
                color = new Color(0.5f, 0.5f, 0.5f, 0),
                renderQueue = renderQueue,
            };

            if (apr != null)
                material.SetTexture("_APRMap", apr);

            //material.EnableKeyword("TERRAIN_SURFACE_ON");
            material.EnableKeyword("NET_SEGMENT");

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
    }
    public enum MaterialType
    {
        RectangleLines,
        RectangleFillers,
        Triangle,
        Pavement,
        Grass,
        Gravel,
        Asphalt,
        Ruined,
        Cliff,
    }

    public interface IDrawData
    {
        public void Draw(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data);
    }

    public enum MarkupLOD
    {
        LOD0,
        LOD1
    }
    public class LodDictionary<Type> : Dictionary<MarkupLOD, Type>
    {
        protected virtual Type Default => default;
        public LodDictionary()
        {
            Clear();
        }
        public new void Clear()
        {
            foreach (var lod in EnumExtension.GetEnumValues<MarkupLOD>())
                this[lod] = Default;
        }
    }
    public class LodDictionaryArray<Type> : LodDictionary<Type[]>
    {
        protected override Type[] Default => new Type[0];
    }

    public class MarkupStylePart
    {
        public MaterialType MaterialType { get; set; }
        public Vector3 Position { get; set; }
        public float Angle { get; set; }
        public float Length { get; set; }
        public float Width { get; set; }
        public Color32 Color { get; set; }

        public MarkupStylePart(Vector3 position, float angle, float length, float width, Color32 color, MaterialType materialType = MaterialType.RectangleLines)
        {
            Position = position;
            Angle = angle;
            Length = length;
            Width = width;
            Color = color;
            MaterialType = materialType;
        }
        public MarkupStylePart(Vector3 start, Vector3 end, float angle, float length, float width, Color32 color, MaterialType materialType = MaterialType.RectangleLines)
            : this((start + end) / 2, angle, length, width, color, materialType) { }

        public MarkupStylePart(Vector3 start, Vector3 end, Vector3 dir, float length, float width, Color32 color, MaterialType materialType = MaterialType.RectangleLines)
            : this(start, end, dir.AbsoluteAngle(), length, width, color, materialType) { }

        public MarkupStylePart(Vector3 start, Vector3 end, Vector3 dir, float width, Color32 color, MaterialType materialType = MaterialType.RectangleLines)
            : this(start, end, dir, (end - start).magnitude, width, color, materialType) { }

        public MarkupStylePart(Vector3 start, Vector3 end, float width, Color32 color, MaterialType materialType = MaterialType.RectangleLines)
            : this(start, end, end - start, (end - start).magnitude, width, color, materialType) { }
    }
    public class MarkupStyleParts : IStyleData, IEnumerable<MarkupStylePart>
    {
        private List<MarkupStylePart> Dashes { get; }

        public MarkupStyleParts() => Dashes = new List<MarkupStylePart>();
        public MarkupStyleParts(IEnumerable<MarkupStylePart> dashes) => Dashes = dashes.ToList();

        public IEnumerator<MarkupStylePart> GetEnumerator() => Dashes.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerable<IDrawData> GetDrawData() => RenderBatch.FromDashes(this);
    }
    public abstract class MarkupStyleMesh : IStyleData, IDrawData
    {
        protected abstract float MeshHalfWidth { get; }
        protected abstract float MeshHalfLength { get; }

        protected Vector3 Position { get; private set; }

        protected Mesh Mesh { get; private set; }
        protected Matrix4x4 Left { get; private set; }
        protected Matrix4x4 Right { get; private set; }
        protected Vector4 Scale { get; }
        protected MaterialType MaterialType { get; private set; }

        public MarkupStyleMesh()
        {
            Scale = new Vector4(0.5f / MeshHalfWidth, 0.5f / MeshHalfLength, 1f, 1f);
        }
        protected void Init(Vector3 position, Matrix4x4 left, Matrix4x4 right, MaterialType materialType)
        {
            Position = position;
            Left = left;
            Right = right;
            MaterialType = materialType;
        }

        public void Draw(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data)
        {
            var instance = Singleton<NetManager>.instance;

            instance.m_materialBlock.Clear();

            instance.m_materialBlock.SetMatrix(instance.ID_LeftMatrix, Left);
            instance.m_materialBlock.SetMatrix(instance.ID_RightMatrix, Right);
            instance.m_materialBlock.SetVector(instance.ID_MeshScale, Scale);

            instance.m_materialBlock.SetTexture(instance.ID_SurfaceTexA, /*RenderHelper.SurfaceTexture*/ RenderHelper.SurfaceALib[MaterialType]);
            instance.m_materialBlock.SetTexture(instance.ID_SurfaceTexB, /*RenderHelper.SurfaceTexture*/ RenderHelper.SurfaceBLib[MaterialType]);

            Graphics.DrawMesh(Mesh, Position, Quaternion.identity, RenderHelper.MaterialLib[MaterialType], 0, null, 0, instance.m_materialBlock);
        }
        public IEnumerable<IDrawData> GetDrawData()
        {
            if (Mesh == null)
                Mesh = GetMesh();
            yield return this;
        }
        protected abstract Mesh GetMesh();
        protected static IEnumerable<Vector3> FixNormals(Vector3[] normals) => FixNormals(normals, 0, normals.Length);
        protected static IEnumerable<Vector3> FixNormals(Vector3[] normals, int from, int count)
        {
            for (var i = 0; i < normals.Length; i += 1)
                yield return (i >= from && i < from + count ? -1 : 1) * normals[i];
        }
    }
    public abstract class BaseMarkupStylePolygonMesh : MarkupStyleMesh
    {
        protected override float MeshHalfWidth => 20f;
        protected override float MeshHalfLength => 20f;
        private Vector3[] Vertices { get; set; }
        private int[] Triangles { get; set; }
        private Vector2[] UV { get; set; }

        protected virtual bool NeedFixNormals => false;

        public void Init(float height, float elevation, Vector3[] vertices, int[] triangles, MaterialType materialType)
        {
            var minMax = GetMinMax(vertices);
            var position = new Vector3(minMax.center.x, height, minMax.center.y);

            var xRatio = MeshHalfWidth / minMax.width;
            var yRatio = MeshHalfLength / minMax.height;

            Vertices = new Vector3[vertices.Length];
            UV = new Vector2[vertices.Length];
            for (var i = 0; i < vertices.Length; i += 1)
            {
                Vertices[i] = new Vector3((vertices[i].x - minMax.center.x) * xRatio, vertices[i].y - position.y + elevation, (vertices[i].z - minMax.center.y) * yRatio);
                UV[i] = new Vector2((vertices[i].x / MeshHalfWidth + 1f) * 0.5f, (vertices[i].z / MeshHalfLength + 1f) * 0.5f);
            }

            Triangles = new int[triangles.Length];
            triangles.CopyTo(Triangles, 0);

            CalculateMatrix(minMax.width, minMax.height, out Matrix4x4 left, out Matrix4x4 right);
            Init(position, left, right, materialType);
        }

        private void CalculateMatrix(float width, float height, out Matrix4x4 left, out Matrix4x4 right)
        {
            var bezierL = new Bezier3()
            {
                a = new Vector3(-width, 0f, -height),
                b = new Vector3(-width, 0f, -height / 3),
                c = new Vector3(-width, 0f, height / 3),
                d = new Vector3(-width, 0f, height),
            };
            var bezierR = new Bezier3()
            {
                a = new Vector3(width, 0f, -height),
                b = new Vector3(width, 0f, -height / 3),
                c = new Vector3(width, 0f, height / 3),
                d = new Vector3(width, 0f, height),
            };

            left = NetSegment.CalculateControlMatrix(bezierL.a, bezierL.b, bezierL.c, bezierL.d, bezierR.a, bezierR.b, bezierR.c, bezierR.d, Vector3.zero, 0.05f);
            right = NetSegment.CalculateControlMatrix(bezierR.a, bezierR.b, bezierR.c, bezierR.d, bezierL.a, bezierL.b, bezierL.c, bezierL.d, Vector3.zero, 0.05f);
        }
        protected Rect GetMinMax(Vector3[] points) => Rect.MinMaxRect(points.Min(p => p.x), points.Min(p => p.z), points.Max(p => p.x), points.Max(p => p.z));
        protected override Mesh GetMesh()
        {
            var mesh = new Mesh
            {
                vertices = Vertices,
                triangles = Triangles,
                uv = UV,
                bounds = new Bounds(new Vector3(0f, 0f, 0f), new Vector3(128, 57, 128)),
            };
            mesh.RecalculateNormals();
            if (NeedFixNormals)
                mesh.normals = FixNormals(mesh.normals).ToArray();
            mesh.RecalculateTangents();

            return mesh;
        }
    }
    public class MarkupStylePolygonTopMesh : BaseMarkupStylePolygonMesh
    {
        protected override bool NeedFixNormals => true;
        public MarkupStylePolygonTopMesh(float height, float elevation, Vector3[] points, int[] polygons, MaterialType materialType)
        {
            Init(height, elevation, points, polygons, materialType);
        }
    }
    public class MarkupStylePolygonSideMesh : BaseMarkupStylePolygonMesh
    {
        public MarkupStylePolygonSideMesh(float height, float elevation, int[] groups, Vector3[] points, MaterialType materialType)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            var index = 0;
            for (var i = 0; i < groups.Length; i += 1)
            {
                for (var j = 0; j <= groups[i]; j += 1)
                {
                    var point = points[index % points.Length];

                    vertices.Add(point);
                    vertices.Add(point - new Vector3(0f, elevation, 0f));

                    if (j != 0)
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

            Init(height, elevation, vertices.ToArray(), triangles.ToArray(), materialType);
        }
    }
    public class MarkupStyleLineMesh : MarkupStyleMesh
    {
        private static int Split => 22;
        private static float HalfWidth => 1f;
        private static float HalfLength => 32f;
        private static float Height => 2f;
        private static Mesh LineMesh { get; set; }

        private static Mesh CreateMesh()
        {
            var mesh = new Mesh()
            {
                vertices = GetVertices().ToArray(),
                triangles = GetTriangles().ToArray(),
                uv = GetUV().ToArray(),
                bounds = new Bounds(new Vector3(0f, 0f, 0f), new Vector3(128, 57, 128)),
            };
            mesh.RecalculateNormals();
            var lanePoints = (Split + 1) * 2;
            mesh.normals = FixNormals(mesh.normals, 4 + lanePoints, lanePoints).ToArray();
            mesh.RecalculateTangents();

            return mesh;
        }
        private static IEnumerable<Vector3> GetVertices()
        {
            yield return new Vector3(-HalfWidth, 0f, -HalfLength);
            yield return new Vector3(HalfWidth, 0f, -HalfLength);
            yield return new Vector3(-HalfWidth, Height, -HalfLength);
            yield return new Vector3(HalfWidth, Height, -HalfLength);

            for (var i = 0; i <= Split; i += 1)
            {
                var z = GetZ(i);
                yield return new Vector3(-HalfWidth, 0f, z);
                yield return new Vector3(-HalfWidth, Height, z);
            }
            for (var i = 0; i <= Split; i += 1)
            {
                var z = GetZ(i);
                yield return new Vector3(-HalfWidth, Height, z);
                yield return new Vector3(HalfWidth, Height, z);
            }
            for (var i = 0; i <= Split; i += 1)
            {
                var z = GetZ(i);
                yield return new Vector3(HalfWidth, Height, z);
                yield return new Vector3(HalfWidth, 0f, z);
            }

            yield return new Vector3(HalfWidth, 0f, HalfLength);
            yield return new Vector3(-HalfWidth, 0f, HalfLength);
            yield return new Vector3(HalfWidth, Height, HalfLength);
            yield return new Vector3(-HalfWidth, Height, HalfLength);

            static float GetZ(int i) => (2f / Split * i - 1) * HalfLength;
        }
        protected static float PavementA => 0.03f;
        protected static float PavementB => 0.1f;
        private static IEnumerable<Vector2> GetUV()
        {
            yield return new Vector2(PavementA, 0f);
            yield return new Vector2(PavementB, 0f);
            yield return new Vector2(PavementA, 0f);
            yield return new Vector2(PavementB, 0f);

            for (var i = 0; i <= Split; i += 1)
            {
                var ratio = GetRatio(i);
                yield return new Vector2(PavementA, ratio);
                yield return new Vector2(PavementB, ratio);
            }
            for (var i = 0; i <= Split; i += 1)
            {
                var ratio = GetRatio(i);
                yield return new Vector2(PavementA, ratio);
                yield return new Vector2(PavementB, ratio);
            }
            for (var i = 0; i <= Split; i += 1)
            {
                var ratio = GetRatio(i);
                yield return new Vector2(PavementA, ratio);
                yield return new Vector2(PavementB, ratio);
            }

            yield return new Vector2(PavementA, 1f);
            yield return new Vector2(PavementB, 1f);
            yield return new Vector2(PavementA, 1f);
            yield return new Vector2(PavementB, 1f);

            static float GetRatio(int i) => 1f / Split * i;
        }

        private static IEnumerable<int> GetTriangles()
        {
            var triangles = new List<int>();

            var index = 0;
            triangles.AddRange(GetRect());
            index += 4;

            for (var i = 0; i < 3; i += 1)
            {
                for (var j = 0; j < Split; j += 1)
                {
                    triangles.AddRange(GetRect(index));
                    index += 2;
                }
                index += 2;
            }
            triangles.AddRange(GetRect(index));

            return triangles;

            static IEnumerable<int> GetRect(int index = 0)
            {
                yield return index;
                yield return index + 3;
                yield return index + 1;
                yield return index + 3;
                yield return index;
                yield return index + 2;
            }
        }

        protected override float MeshHalfWidth => HalfWidth * 2;
        protected override float MeshHalfLength => HalfLength;

        public MarkupStyleLineMesh(ITrajectory trajectory, float width, float elevation, MaterialType materialType)
        {
            var position = (trajectory.StartPosition + trajectory.EndPosition) / 2;
            CalculateMatrix(trajectory, width, position, out Matrix4x4 left, out Matrix4x4 right);
            position += Vector3.up * (elevation - Height);
            Init(position, left, right, materialType);
        }
        private void CalculateMatrix(ITrajectory trajectory, float width, Vector3 position, out Matrix4x4 left, out Matrix4x4 right)
        {
            var startNormal = trajectory.StartDirection.Turn90(true);
            startNormal.y = 0f;
            var endNormal = trajectory.EndDirection.Turn90(false);
            endNormal.y = 0f;

            var bezierL = new Bezier3()
            {
                a = trajectory.StartPosition - startNormal * width,
                d = trajectory.EndPosition - endNormal * width,
            };
            var bezierR = new Bezier3()
            {
                a = trajectory.StartPosition + startNormal * width,
                d = trajectory.EndPosition + endNormal * width,
            };

            NetSegment.CalculateMiddlePoints(bezierL.a, trajectory.StartDirection, bezierL.d, trajectory.EndDirection, true, true, out bezierL.b, out bezierL.c);
            NetSegment.CalculateMiddlePoints(bezierR.a, trajectory.StartDirection, bezierR.d, trajectory.EndDirection, true, true, out bezierR.b, out bezierR.c);

            left = NetSegment.CalculateControlMatrix(bezierL.a, bezierL.b, bezierL.c, bezierL.d, bezierR.a, bezierR.b, bezierR.c, bezierR.d, position, 0.05f);
            right = NetSegment.CalculateControlMatrix(bezierR.a, bezierR.b, bezierR.c, bezierR.d, bezierL.a, bezierL.b, bezierL.c, bezierL.d, position, 0.05f);
        }
        protected override Mesh GetMesh()
        {
            if (LineMesh == null)
                LineMesh = CreateMesh();

            return LineMesh;
        }
    }

    public struct MarkupStylePropItem
    {
        public Vector3 position;
        public float angle;
        public float scale;
    }
    public abstract class BaseMarkupStyleProp<PrefabType> : IStyleData, IDrawData
        where PrefabType : PrefabInfo
    {
        public PrefabType Info { get; private set; }
        protected MarkupStylePropItem[] Items { get; private set; }

        public BaseMarkupStyleProp(PrefabType info, MarkupStylePropItem[] items)
        {
            Info = info;
            Items = items;
        }

        public IEnumerable<IDrawData> GetDrawData()
        {
            yield return this;
        }

        public abstract void Draw(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data);
    }
    public class MarkupStyleProp : BaseMarkupStyleProp<PropInfo>
    {
        public MarkupStyleProp(PropInfo info, MarkupStylePropItem[] items) : base(info, items) { }

        public override void Draw(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data)
        {
            var instance = new InstanceID() { };
            foreach (var item in Items)
                PropInstance.RenderInstance(cameraInfo, Info, instance, item.position, item.scale, item.angle, Info.GetColor(ref SimulationManager.instance.m_randomizer), new Vector4(), true);
        }
    }
    public class MarkupStyleTree : BaseMarkupStyleProp<TreeInfo>
    {
        public MarkupStyleTree(TreeInfo info, MarkupStylePropItem[] items) : base(info, items) { }

        public override void Draw(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data)
        {
            foreach (var item in Items)
                TreeInstance.RenderInstance(cameraInfo, Info, item.position, item.scale, 1f, new Vector4());
        }
    }

    public class RenderBatch : IDrawData
    {
        public static float MeshHeight => 5f;
        public MaterialType MaterialType { get; }
        public int Count { get; }
        public Vector4[] Locations { get; }
        public Vector4[] Indices { get; }
        public Vector4[] Colors { get; }
        public Mesh Mesh { get; }

        public Vector4 Size { get; }

        public RenderBatch(MarkupStylePart[] dashes, int count, Vector3 size, MaterialType materialType)
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

        public static IEnumerable<IDrawData> FromDashes(IEnumerable<MarkupStylePart> dashes)
        {
            var materialGroups = dashes.GroupBy(d => d.MaterialType);

            foreach (var materialGroup in materialGroups)
            {
                var sizeGroups = materialGroup.Where(d => d.Length >= 0.1f).GroupBy(d => new Vector3(Round(d.Length), MeshHeight, d.Width));

                foreach (var sizeGroup in sizeGroups)
                {
                    var groupEnumerator = sizeGroup.GetEnumerator();

                    var buffer = new MarkupStylePart[16];
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

        public void Draw(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data)
        {
            var instance = Singleton<PropManager>.instance;
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
