using ColossalFramework;
using ColossalFramework.Math;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NodeMarkup.Utilities
{
    public abstract class BaseMarkupMeshData : IStyleData, IDrawData
    {
        public MarkupLOD LOD { get; }
        public abstract MarkupLODType LODType { get; }
        protected Vector4 Scale { get; }

        public BaseMarkupMeshData(MarkupLOD lod, float meshWidth, float meshLength)
        {
            Scale = new Vector4(1f / meshWidth, 1f / meshLength, 1f, 1f);
            LOD = lod;
        }

        public abstract IEnumerable<IDrawData> GetDrawData();
        public abstract void Draw(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data, bool infoView);

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
    public abstract class MarkupMeshData : BaseMarkupMeshData
    {
        protected virtual bool CastShadow => true;
        protected virtual bool ReceiveShadow => true;

        protected Vector3 Position { get; private set; }

        protected Matrix4x4 Left { get; private set; }
        protected Matrix4x4 Right { get; private set; }
        protected Mesh[] Meshes { get; private set; }
        protected MaterialType[] MaterialTypes { get; private set; }

        public MarkupMeshData(MarkupLOD lod, float meshWidth, float meshLength) : base(lod, meshWidth, meshLength) { }

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

        public override void Draw(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data, bool infoView)
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
    public class MarkupFillerMeshData : MarkupMeshData
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

        public override MarkupLODType LODType => MarkupLODType.Mesh;
        protected override bool ReceiveShadow => false;
        private static float MeshHalfWidth => 20f;
        private static float MeshHalfLength => 20f;
        private RenderData[] Data { get; set; }

        public MarkupFillerMeshData(MarkupLOD lod, float elevation, params RawData[] datas) : base(lod, MeshHalfWidth * 2f, MeshHalfLength * 2f)
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

            for (var i = 0; i < Data.Length; i += 1)
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
            foreach (var data in Data)
            {
                var mesh = new Mesh
                {
                    name = "MarkupStyleFillerMesh",
                    vertices = data._vertixes,
                    triangles = data._triangles,
                    bounds = new Bounds(new Vector3(0f, 0f, 0f), new Vector3(128, 57, 128)),
                };
                mesh.RecalculateNormals();
                if (data._meshType == MeshType.Top)
                    mesh.normals = mesh.normals.Select(n => -n).ToArray();
                mesh.RecalculateTangents();
                mesh.UploadMeshData(false);

                yield return mesh;
            }
        }
    }
    public class MarkupLineMeshData : MarkupMeshData
    {
        private static int Split => 22;
        private static float HalfWidth => 10f;
        private static float HalfLength => 11f;
        private static float Height => 2f;
        private static Mesh LineMesh { get; set; }

        public override MarkupLODType LODType => MarkupLODType.Mesh;

        public MarkupLineMeshData(MarkupLOD lod, ITrajectory trajectory, float width, float elevation, MaterialType materialType) : base(lod, HalfWidth * 2f, HalfLength * 2f)
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
                    name = nameof(MarkupLineMeshData),
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
}
