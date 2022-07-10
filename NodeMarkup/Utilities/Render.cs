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
            { MaterialType.Pavement, CreateRoadMaterial(MaterialType.Pavement, TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,Color.black)) },
            { MaterialType.Grass, CreateRoadMaterial(MaterialType.Grass, TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,new Color32(255, 0, 0, 255)))},
            { MaterialType.Gravel, CreateRoadMaterial(MaterialType.Gravel, TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,Color.black))},
            { MaterialType.Asphalt, CreateRoadMaterial(MaterialType.Asphalt, TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,new Color32(0, 255, 255, 255)))},
            { MaterialType.Ruined, CreateRoadMaterial(MaterialType.Ruined, TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,Color.black))},
            { MaterialType.Cliff, CreateRoadMaterial(MaterialType.Cliff, TextureHelper.CreateTexture(128,128,Color.white), TextureHelper.CreateTexture(128,128,Color.black))},
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

        public static Material CreateRoadMaterial(MaterialType type, Texture2D texture, Texture2D apr = null, int renderQueue = 2461)
        {
            var material = new Material(Shader.Find("Custom/Net/Road"))
            {
                name = type.ToString(),
                mainTexture = texture,
                color = new Color(1f, 1f, 1f, 1f),
                renderQueue = renderQueue,
            };

            if (apr != null)
                material.SetTexture("_APRMap", apr);

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

    public abstract class BaseMarkupStyleMesh : IStyleData, IDrawData
    {
        protected Vector4 Scale { get; }

        public BaseMarkupStyleMesh(float meshWidth, float meshLength)
        {
            Scale = new Vector4(1f / meshWidth, 1f / meshLength, 1f, 1f);
        }

        public abstract IEnumerable<IDrawData> GetDrawData();
        public abstract void Draw(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data);

        protected void CalculateMatrix(ITrajectory trajectory, float halfWidth, Vector3 position, out Matrix4x4 left, out Matrix4x4 right)
        {
            var startNormal = trajectory.StartDirection.Turn90(true).MakeFlatNormalized();
            var endNormal = trajectory.EndDirection.Turn90(false).MakeFlatNormalized();

            var bezierL = new Bezier3()
            {
                a = trajectory.StartPosition - startNormal * halfWidth,
                d = trajectory.EndPosition - endNormal * halfWidth,
            };
            var bezierR = new Bezier3()
            {
                a = trajectory.StartPosition + startNormal * halfWidth,
                d = trajectory.EndPosition + endNormal * halfWidth,
            };

            NetSegment.CalculateMiddlePoints(bezierL.a, trajectory.StartDirection, bezierL.d, trajectory.EndDirection, true, true, out bezierL.b, out bezierL.c);
            NetSegment.CalculateMiddlePoints(bezierR.a, trajectory.StartDirection, bezierR.d, trajectory.EndDirection, true, true, out bezierR.b, out bezierR.c);

            left = NetSegment.CalculateControlMatrix(bezierL.a, bezierL.b, bezierL.c, bezierL.d, bezierR.a, bezierR.b, bezierR.c, bezierR.d, position, 0.05f);
            right = NetSegment.CalculateControlMatrix(bezierR.a, bezierR.b, bezierR.c, bezierR.d, bezierL.a, bezierL.b, bezierL.c, bezierL.d, position, 0.05f);
        }
    }

    public abstract class MarkupStyleMesh : BaseMarkupStyleMesh
    {
        protected virtual bool CastShadow => true;
        protected virtual bool ReceiveShadow => true;

        protected Vector3 Position { get; private set; }

        protected Matrix4x4 Left { get; private set; }
        protected Matrix4x4 Right { get; private set; }
        protected Mesh[] Meshes { get; private set; }
        protected MaterialType[] MaterialTypes { get; private set; }

        public MarkupStyleMesh(float meshWidth, float meshLength) : base(meshWidth, meshLength) { }

        protected void Init(Vector3 position, Matrix4x4 left, Matrix4x4 right, params MaterialType[] materialTypes)
        {
            Position = position;
            Left = left;
            Right = right;
            MaterialTypes = materialTypes;
        }

        public override IEnumerable<IDrawData> GetDrawData()
        {
            if (Meshes == null)
                Meshes = GetMeshes().ToArray();
            yield return this;
        }
        protected abstract IEnumerable<Mesh> GetMeshes();

        public override void Draw(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data)
        {
            var instance = Singleton<NetManager>.instance;

            for (var i = 0; i < Meshes.Length && i < MaterialTypes.Length; i += 1)
            {
                var materialType = MaterialTypes[i];
                instance.m_materialBlock.Clear();

                instance.m_materialBlock.SetMatrix(instance.ID_LeftMatrix, Left);
                instance.m_materialBlock.SetMatrix(instance.ID_RightMatrix, Right);
                instance.m_materialBlock.SetVector(instance.ID_MeshScale, Scale);

                instance.m_materialBlock.SetTexture(instance.ID_SurfaceTexA, RenderHelper.SurfaceALib[materialType]);
                instance.m_materialBlock.SetTexture(instance.ID_SurfaceTexB, RenderHelper.SurfaceBLib[materialType]);

                Graphics.DrawMesh(Meshes[i], Position, Quaternion.identity, RenderHelper.MaterialLib[materialType], 0, null, 0, instance.m_materialBlock, CastShadow, ReceiveShadow);
            }
        }

        protected static IEnumerable<Vector3> FixNormals(Vector3[] normals) => FixNormals(normals, 0, normals.Length);
        protected static IEnumerable<Vector3> FixNormals(Vector3[] normals, int from, int count)
        {
            for (var i = 0; i < normals.Length; i += 1)
                yield return (i >= from && i < from + count ? -1 : 1) * normals[i];
        }
    }
    public class MarkupStyleFillerMesh : MarkupStyleMesh
    {
        public enum MeshType
        {
            Side,
            Top,
        }
        public struct RawData
        {
            public MeshType _meshType;
            public MaterialType _materialType;
            public int[] _groups;
            public Vector3[] _points;
            public int[] _polygons;

            public static RawData SetSide(int[] groups, Vector3[] points, MaterialType materialType)
            {
                return new RawData()
                {
                    _meshType = MeshType.Side,
                    _groups = groups.ToArray(),
                    _points = points.ToArray(),
                    _materialType = materialType,
                };
            }
            public static RawData SetTop(Vector3[] points, int[] polygons, MaterialType materialType)
            {
                return new RawData()
                {
                    _meshType = MeshType.Top,
                    _points = points.ToArray(),
                    _polygons = polygons.ToArray(),
                    _materialType = materialType,
                };
            }
        }
        public struct RenderData
        {
            public MeshType _meshType;
            public Vector3[] _vertixes;
            public int[] _triangles;
        }

        protected override bool ReceiveShadow => false;
        private static float MeshHalfWidth => 20f;
        private static float MeshHalfLength => 20f;
        private RenderData[] Data {get;set; }

        public MarkupStyleFillerMesh(float elevation, params RawData[] datas) : base(MeshHalfWidth * 2f, MeshHalfLength * 2f) 
        {
            Data = new RenderData[datas.Length];

            for (var i = 0; i < datas.Length; i += 1)
            {
                var data = datas[i];
                Data[i]._meshType = data._meshType;

                if (data._meshType == MeshType.Side)
                {
                    var vertices = new List<Vector3>();
                    var triangles = new List<int>();

                    var index = 0;
                    for (var j = 0; j < data._groups.Length; j += 1)
                    {
                        for (var k = 0; k <= data._groups[j]; k += 1)
                        {
                            var point = data._points[index % data._points.Length];

                            vertices.Add(point);
                            vertices.Add(point - new Vector3(0f, elevation + 0.5f, 0f));

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


                    Data[i]._vertixes = vertices.ToArray();
                    Data[i]._triangles = triangles.ToArray();
                }
                else
                {
                    Data[i]._vertixes = data._points;
                    Data[i]._triangles = data._polygons;
                }
            }

            var vertixes = Data.SelectMany(i => i._vertixes).ToArray();
            var minMax = GetMinMax(vertixes);

            var xRatio = MeshHalfWidth / minMax.size.x;
            var yRatio = MeshHalfLength / minMax.size.z;

            for(var i = 0; i < Data.Length; i += 1)
            {
                for (var j = 0; j < Data[i]._vertixes.Length; j += 1)
                {
                    var vertex = Data[i]._vertixes[j];
                    Data[i]._vertixes[j] = new Vector3((vertex.x - minMax.center.x) * xRatio, vertex.y - minMax.min.y, (vertex.z - minMax.center.z) * yRatio);
                }
            }

            CalculateMatrix(minMax.size.x, minMax.size.z, out Matrix4x4 left, out Matrix4x4 right);
            var position = new Vector3(minMax.center.x, minMax.min.y + elevation, minMax.center.z);

            Init(position, left, right, datas.Select(i => i._materialType).ToArray());
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
        protected Bounds GetMinMax(Vector3[] vertixes)
        {
            var minMax = new Bounds();
            minMax.SetMinMax(new Vector3(vertixes.Min(p => p.x), vertixes.Min(p => p.y), vertixes.Min(p => p.z)), new Vector3(vertixes.Max(p => p.x), vertixes.Max(p => p.y), vertixes.Max(p => p.z)));
            return minMax;
        }
        protected override IEnumerable<Mesh> GetMeshes()
        {
            foreach(var data in Data)
            {
                var mesh = new Mesh
                {
                    name = "MarkupStyleFillerMesh",
                    vertices = data._vertixes,
                    triangles = data._triangles,
                    bounds = new Bounds(new Vector3(0f, 0f, 0f), new Vector3(128, 57, 128)),
                };
                mesh.RecalculateNormals();
                if(data._meshType == MeshType.Top)
                    mesh.normals = mesh.normals.Select(n => -n).ToArray();
                mesh.RecalculateTangents();
                mesh.UploadMeshData(false);

                yield return mesh;
            }
        }
    }

    public class MarkupStyleLineMesh : MarkupStyleMesh
    {
        private static int Split => 22;
        private static float HalfWidth => 10f;
        private static float HalfLength => 11f;
        private static float Height => 2f;
        private static Mesh LineMesh { get; set; }

        public MarkupStyleLineMesh(ITrajectory trajectory, float width, float elevation, MaterialType materialType) : base(HalfWidth * 2f, HalfLength * 2f)
        {
            var position = (trajectory.StartPosition + trajectory.EndPosition) / 2;
            CalculateMatrix(trajectory, width, position, out Matrix4x4 left, out Matrix4x4 right);
            position += Vector3.up * (elevation - Height);
            Init(position, left, right, materialType);
        }

        protected override IEnumerable<Mesh> GetMeshes()
        {
            if (LineMesh == null)
            {
                var mesh = new Mesh()
                {
                    name = nameof(MarkupStyleLineMesh),
                    vertices = GetVertices().ToArray(),
                    triangles = GetTriangles().ToArray(),
                    bounds = new Bounds(new Vector3(0f, 0f, 0f), new Vector3(128, 57, 128)),
                };
                mesh.RecalculateNormals();
                var count = (Split + 1) * 2;
                var startIndex = count + 4;
                var endIndex = startIndex + count;
                mesh.normals = mesh.normals.Select((n, i) => i >= startIndex && i < endIndex ? -n : n).ToArray();
                mesh.RecalculateTangents();
                mesh.UploadMeshData(false);

                LineMesh = mesh;
            }

            yield return LineMesh;
        }

        private static IEnumerable<Vector3> GetVertices()
        {
            var maxHeight = Height;
            var minHeight = 0f;

            var maxWidth = HalfWidth / 2f;
            var minWidth = -HalfWidth / 2f;

            var maxLength = HalfLength;
            var minLength = -HalfLength;

            yield return new Vector3(minWidth, minHeight, minLength);
            yield return new Vector3(maxWidth, minHeight, minLength);
            yield return new Vector3(minWidth, maxHeight, minLength);
            yield return new Vector3(maxWidth, maxHeight, minLength);

            for (var i = 0; i <= Split; i += 1)
            {
                var z = GetZ(i);
                yield return new Vector3(minWidth, minHeight, z);
                yield return new Vector3(minWidth, maxHeight, z);
            }
            for (var i = 0; i <= Split; i += 1)
            {
                var z = GetZ(i);
                yield return new Vector3(minWidth, maxHeight, z);
                yield return new Vector3(maxWidth, maxHeight, z);
            }
            for (var i = 0; i <= Split; i += 1)
            {
                var z = GetZ(i);
                yield return new Vector3(maxWidth, maxHeight, z);
                yield return new Vector3(maxWidth, minHeight, z);
            }

            yield return new Vector3(maxWidth, minHeight, maxLength);
            yield return new Vector3(minWidth, minHeight, maxLength);
            yield return new Vector3(maxWidth, maxHeight, maxLength);
            yield return new Vector3(minWidth, maxHeight, maxLength);

            static float GetZ(int i) => (2f / Split * i - 1) * HalfLength;
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
    }

    public struct MarkupStylePropItem
    {
        public Vector3 Position;
        public float Angle;
        public float Tilt;
        public float Slope;
        public float Scale;
        public Color32 Color;
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
                RenderInstance(cameraInfo, Info, instance, item.Position, item.Scale, item.Angle, item.Tilt, item.Slope, item.Color, new Vector4(), true);

            //foreach (var item in Items)
            //    PropInstance.RenderInstance(cameraInfo, Info, instance, item.Position, item.Scale, item.Angle, item.Color, new Vector4(), true);
        }

        public static void RenderInstance(RenderManager.CameraInfo cameraInfo, PropInfo info, InstanceID id, Vector3 position, float scale, float angle, float tilt, float slope, Color color, Vector4 objectIndex, bool active)
        {
            if (!info.m_prefabInitialized)
                return;

            if (info.m_hasEffects && (active || info.m_alwaysActive))
            {
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(position, Quaternion.AngleAxis(angle * 57.29578f, Vector3.down), new Vector3(scale, scale, scale));
                float simulationTimeDelta = Singleton<SimulationManager>.instance.m_simulationTimeDelta;
                for (int i = 0; i < info.m_effects.Length; i++)
                {
                    Vector3 position2 = matrix.MultiplyPoint(info.m_effects[i].m_position);
                    Vector3 direction = matrix.MultiplyVector(info.m_effects[i].m_direction);
                    EffectInfo.SpawnArea area = new EffectInfo.SpawnArea(position2, direction, 0f);
                    info.m_effects[i].m_effect.RenderEffect(id, area, Vector3.zero, 0f, 1f, -1f, simulationTimeDelta, cameraInfo);
                }
            }
            if (!info.m_hasRenderer || (cameraInfo.m_layerMask & (1 << info.m_prefabDataLayer)) == 0)
                return;

            if (Singleton<InfoManager>.instance.CurrentMode == InfoManager.InfoMode.None)
            {
                float num = 0f;
                if (!active && !info.m_alwaysActive)
                    objectIndex.z = 0f;
                else if (info.m_illuminationOffRange.x < 1000f || info.m_illuminationBlinkType != 0)
                {
                    LightSystem lightSystem = Singleton<RenderManager>.instance.lightSystem;
                    Randomizer randomizer = new Randomizer(id.Index);
                    num = info.m_illuminationOffRange.x + (float)randomizer.Int32(100000u) * 1E-05f * (info.m_illuminationOffRange.y - info.m_illuminationOffRange.x);
                    objectIndex.z = MathUtils.SmoothStep(num + 0.01f, num - 0.01f, lightSystem.DayLightIntensity);
                    if (info.m_illuminationBlinkType != 0)
                    {
                        Vector4 blinkVector = LightEffect.GetBlinkVector(info.m_illuminationBlinkType);
                        float num2 = num * 3.71f + Singleton<SimulationManager>.instance.m_simulationTimer / blinkVector.w;
                        num2 = (num2 - Mathf.Floor(num2)) * blinkVector.w;
                        float num3 = MathUtils.SmoothStep(blinkVector.x, blinkVector.y, num2);
                        float num4 = MathUtils.SmoothStep(blinkVector.w, blinkVector.z, num2);
                        objectIndex.z *= 1f - num3 * num4;
                    }
                }
                else
                    objectIndex.z = 1f;
            }

            if (cameraInfo == null || cameraInfo.CheckRenderDistance(position, info.m_lodRenderDistance))
            {
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(position, Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.down) * Quaternion.AngleAxis(tilt * Mathf.Rad2Deg, Vector3.right) * Quaternion.AngleAxis(slope * Mathf.Rad2Deg, Vector3.forward), new Vector3(scale, scale, scale));
                PropManager instance = Singleton<PropManager>.instance;
                MaterialPropertyBlock materialBlock = instance.m_materialBlock;
                materialBlock.Clear();
                materialBlock.SetColor(instance.ID_Color, color);
                materialBlock.SetVector(instance.ID_ObjectIndex, objectIndex);
                if (info.m_rollLocation != null)
                {
                    info.m_material.SetVectorArray(instance.ID_RollLocation, info.m_rollLocation);
                    info.m_material.SetVectorArray(instance.ID_RollParams, info.m_rollParams);
                }
                instance.m_drawCallData.m_defaultCalls++;
                Graphics.DrawMesh(info.m_mesh, matrix, info.m_material, info.m_prefabDataLayer, null, 0, materialBlock);
            }
            else if (info.m_lodMaterialCombined == null)
            {
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(position, Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.down) * Quaternion.AngleAxis(tilt * Mathf.Rad2Deg, Vector3.right) * Quaternion.AngleAxis(slope * Mathf.Rad2Deg, Vector3.forward), new Vector3(scale, scale, scale));
                PropManager instance2 = Singleton<PropManager>.instance;
                MaterialPropertyBlock materialBlock2 = instance2.m_materialBlock;
                materialBlock2.Clear();
                materialBlock2.SetColor(instance2.ID_Color, color);
                materialBlock2.SetVector(instance2.ID_ObjectIndex, objectIndex);
                if (info.m_rollLocation != null)
                {
                    info.m_material.SetVectorArray(instance2.ID_RollLocation, info.m_rollLocation);
                    info.m_material.SetVectorArray(instance2.ID_RollParams, info.m_rollParams);
                }
                instance2.m_drawCallData.m_defaultCalls++;
                Graphics.DrawMesh(info.m_lodMesh, matrix, info.m_lodMaterial, info.m_prefabDataLayer, null, 0, materialBlock2);
            }
            else
            {
                objectIndex.w = scale;
                ref Vector4 reference = ref info.m_lodLocations[info.m_lodCount];
                reference = new Vector4(position.x, position.y, position.z, angle);
                info.m_lodObjectIndices[info.m_lodCount] = objectIndex;
                ref Vector4 reference2 = ref info.m_lodColors[info.m_lodCount];
                reference2 = color.linear;
                info.m_lodMin = Vector3.Min(info.m_lodMin, position);
                info.m_lodMax = Vector3.Max(info.m_lodMax, position);
                if (++info.m_lodCount == info.m_lodLocations.Length)
                    PropInstance.RenderLod(cameraInfo, info);
            }
        }
    }
    public class MarkupStyleTree : BaseMarkupStyleProp<TreeInfo>
    {
        public MarkupStyleTree(TreeInfo info, MarkupStylePropItem[] items) : base(info, items) { }

        public override void Draw(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data)
        {
            foreach (var item in Items)
                RenderInstance(cameraInfo, Info, item.Position, item.Scale, item.Angle, item.Tilt, item.Slope, 1f, new Vector4());
        }

        public static void RenderInstance(RenderManager.CameraInfo cameraInfo, TreeInfo info, Vector3 position, float scale, float angle, float tilt, float slope, float brightness, Vector4 objectIndex)
        {
            if (!info.m_prefabInitialized)
            {
                return;
            }
            if (cameraInfo == null || info.m_lodMesh1 == null || cameraInfo.CheckRenderDistance(position, info.m_lodRenderDistance))
            {
                TreeManager instance = Singleton<TreeManager>.instance;
                MaterialPropertyBlock materialBlock = instance.m_materialBlock;
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(position, Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.down) * Quaternion.AngleAxis(tilt * Mathf.Rad2Deg, Vector3.right) * Quaternion.AngleAxis(slope * Mathf.Rad2Deg, Vector3.forward), new Vector3(scale, scale, scale));
                Color value = info.m_defaultColor * brightness;
                value.a = Singleton<WeatherManager>.instance.GetWindSpeed(position);
                materialBlock.Clear();
                materialBlock.SetColor(instance.ID_Color, value);
                materialBlock.SetVector(instance.ID_ObjectIndex, objectIndex);
                instance.m_drawCallData.m_defaultCalls++;
                Graphics.DrawMesh(info.m_mesh, matrix, info.m_material, info.m_prefabDataLayer, null, 0, materialBlock);
                return;
            }
            position.y += info.m_generatedInfo.m_center.y * (scale - 1f);
            Color color = info.m_defaultColor * brightness;
            color.a = Singleton<WeatherManager>.instance.GetWindSpeed(position);
            ref Vector4 reference = ref info.m_lodLocations[info.m_lodCount];
            reference = new Vector4(position.x, position.y, position.z, scale);
            ref Vector4 reference2 = ref info.m_lodColors[info.m_lodCount];
            reference2 = color.linear;
            info.m_lodObjectIndices[info.m_lodCount] = objectIndex;
            info.m_lodMin = Vector3.Min(info.m_lodMin, position);
            info.m_lodMax = Vector3.Max(info.m_lodMax, position);
            if (++info.m_lodCount == info.m_lodLocations.Length)
            {
                TreeInstance.RenderLod(cameraInfo, info);
            }
        }
    }

    public class MarkupStyleNetwork : BaseMarkupStyleMesh
    {
        private struct Data
        {
            public NetInfo.Segment segment;
            public Vector3 position;
            public Matrix4x4 left;
            public Matrix4x4 right;
            public Texture heightMap;
            public Vector4 heightMapping;
            public Vector4 surfaceMapping;
        }

        Data[] Datas { get; set; }

        public MarkupStyleNetwork(NetInfo info, ITrajectory[] trajectories, float width, float length, float scale, float elevation) : base(width, length)
        {
            var count = info.m_segments.Count(s => s.CheckFlags(NetSegment.Flags.None, out _));
            Datas = new Data[trajectories.Length * count];

            for (int i = 0; i < trajectories.Length; i += 1)
            {
                var position = (trajectories[i].StartPosition + trajectories[i].EndPosition) / 2;
                CalculateMatrix(trajectories[i], width * 0.5f * scale, position, out var left, out var right);
                position += Vector3.up * elevation;

                int j = 0;
                foreach (var segment in info.m_segments)
                {
                    if (segment.CheckFlags(NetSegment.Flags.None, out _))
                    {
                        int index = i * count + j;

                        Datas[index].segment = segment;
                        Datas[index].position = position;
                        Datas[index].left = left;
                        Datas[index].right = right;

                        if (segment.m_requireHeightMap)
                            Singleton<TerrainManager>.instance.GetHeightMapping(Datas[index].position, out Datas[index].heightMap, out Datas[i].heightMapping, out Datas[index].surfaceMapping);

                        j += 1;
                    }
                }
            }
        }

        public override IEnumerable<IDrawData> GetDrawData()
        {
            yield return this;
        }

        public override void Draw(RenderManager.CameraInfo cameraInfo, RenderManager.Instance renderData)
        {
            var instance = Singleton<NetManager>.instance;

            foreach (var data in Datas)
            {
                if (cameraInfo.CheckRenderDistance(renderData.m_position, data.segment.m_lodRenderDistance))
                {
                    instance.m_materialBlock.Clear();
                    instance.m_materialBlock.SetMatrix(instance.ID_LeftMatrix, data.left);
                    instance.m_materialBlock.SetMatrix(instance.ID_RightMatrix, data.right);
                    instance.m_materialBlock.SetVector(instance.ID_MeshScale, Scale);
                    if (data.segment.m_requireHeightMap)
                    {
                        instance.m_materialBlock.SetTexture(instance.ID_HeightMap, data.heightMap);
                        instance.m_materialBlock.SetVector(instance.ID_HeightMapping, data.heightMapping);
                        instance.m_materialBlock.SetVector(instance.ID_SurfaceMapping, data.surfaceMapping);
                    }

                    instance.m_drawCallData.m_defaultCalls++;
                    Graphics.DrawMesh(data.segment.m_segmentMesh, data.position, Quaternion.identity, data.segment.m_segmentMaterial, 0, null, 0, instance.m_materialBlock);
                }
                else if (data.segment.m_combinedLod is NetInfo.LodValue combinedLod)
                {
                    if (data.segment.m_requireHeightMap && data.heightMap != combinedLod.m_heightMap)
                    {
                        if (combinedLod.m_lodCount != 0)
                            NetSegment.RenderLod(cameraInfo, combinedLod);

                        combinedLod.m_heightMap = data.heightMap;
                        combinedLod.m_heightMapping = data.heightMapping;
                        combinedLod.m_surfaceMapping = data.surfaceMapping;
                    }

                    combinedLod.m_leftMatrices[combinedLod.m_lodCount] = data.left;
                    combinedLod.m_rightMatrices[combinedLod.m_lodCount] = data.right;
                    combinedLod.m_meshScales[combinedLod.m_lodCount] = Scale;
                    combinedLod.m_meshLocations[combinedLod.m_lodCount] = data.position;
                    combinedLod.m_lodMin = Vector3.Min(combinedLod.m_lodMin, data.position);
                    combinedLod.m_lodMax = Vector3.Max(combinedLod.m_lodMax, data.position);

                    if (++combinedLod.m_lodCount == combinedLod.m_leftMatrices.Length)
                        NetSegment.RenderLod(cameraInfo, combinedLod);
                }
            }
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
