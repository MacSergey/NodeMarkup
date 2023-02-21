using ColossalFramework;
using IMT.Manager;
using ModsCommon.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IMT.Utilities
{
    public abstract class BaseMeshData : IStyleData
    {
        public MarkingLOD LOD { get; }
        public abstract MarkingLODType LODType { get; }
        protected Vector4 Scale { get; }

        public BaseMeshData(MarkingLOD lod, float width, float length)
        {
            LOD = lod;
            Scale = new Vector4(1f / width, 1f / length, 1f, 1f);
        }

        public abstract void Draw(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data, bool infoView);

        protected void CalculateMatrix(ITrajectory trajectory, float halfWidth, Vector3 position, out Matrix4x4 left, out Matrix4x4 right)
        {
            var startNormal = trajectory.StartDirection.Turn90(true).MakeFlatNormalized();
            var endNormal = trajectory.EndDirection.Turn90(false).MakeFlatNormalized();

            float? startT;
            float? endT;
            if (trajectory is BezierTrajectory bezier)
            {
                startT = bezier.StartT;
                endT = bezier.EndT;
            }
            else
            {
                startT = null;
                endT = null;
            }

            var bezierL = new BezierTrajectory(trajectory.StartPosition - startNormal * halfWidth, trajectory.StartDirection, trajectory.EndPosition - endNormal * halfWidth, trajectory.EndDirection, startT, endT).Trajectory;
            var bezierR = new BezierTrajectory(trajectory.StartPosition + startNormal * halfWidth, trajectory.StartDirection, trajectory.EndPosition + endNormal * halfWidth, trajectory.EndDirection, startT, endT).Trajectory;

            left = NetSegment.CalculateControlMatrix(bezierL.a, bezierL.b, bezierL.c, bezierL.d, bezierR.a, bezierR.b, bezierR.c, bezierR.d, position, 0.05f);
            right = NetSegment.CalculateControlMatrix(bezierR.a, bezierR.b, bezierR.c, bezierR.d, bezierL.a, bezierL.b, bezierL.c, bezierL.d, position, 0.05f);
        }
    }
    public class LineMeshData : BaseMeshData
    {
        private static int Split => 22;
        private static float HalfWidth => 10f;
        private static float HalfLength => 11f;
        private static float Height => 2f;

        private static Mesh lineMesh;
        private static Mesh LineMesh
        {
            get
            {
                if (lineMesh == null)
                {
                    var mesh = new Mesh()
                    {
                        name = nameof(LineMeshData),
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

                    lineMesh = mesh;
                }

                return lineMesh;
            }
        }
        private Vector3 Position { get; set; }

        private Matrix4x4 Left { get; set; }
        private Matrix4x4 Right { get; set; }
        private Mesh[] Meshes { get; set; }
        private MaterialType MaterialType { get; set; }

        public override MarkingLODType LODType => MarkingLODType.Mesh;

        public LineMeshData(MarkingLOD lod, ITrajectory trajectory, float width, float elevation, MaterialType materialType) : base(lod, HalfWidth * 2f, HalfLength * 2f)
        {
            var position = (trajectory.StartPosition + trajectory.EndPosition) * 0.5f;
            CalculateMatrix(trajectory, width, position, out Matrix4x4 left, out Matrix4x4 right);
            position += Vector3.up * (elevation - Height);

            Position = position;
            Left = left;
            Right = right;
            MaterialType = materialType;
        }

        private static IEnumerable<Vector3> GetVertices()
        {
            var maxHeight = Height;
            var minHeight = 0f;

            var maxWidth = HalfWidth * 0.5f;
            var minWidth = -HalfWidth * 0.5f;

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

        public override void Draw(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data, bool infoView)
        {
            var instance = Singleton<NetManager>.instance;

            var materialType = MaterialType;
            instance.m_materialBlock.Clear();

            instance.m_materialBlock.SetMatrix(instance.ID_LeftMatrix, Left);
            instance.m_materialBlock.SetMatrix(instance.ID_RightMatrix, Right);
            instance.m_materialBlock.SetVector(instance.ID_MeshScale, Scale);

            instance.m_materialBlock.SetTexture(instance.ID_SurfaceTexA, RenderHelper.SurfaceALib[materialType]);
            instance.m_materialBlock.SetTexture(instance.ID_SurfaceTexB, RenderHelper.SurfaceBLib[materialType]);

            Graphics.DrawMesh(LineMesh, Position, Quaternion.identity, RenderHelper.MaterialLib[materialType], RenderHelper.RoadLayer, null, 0, instance.m_materialBlock);
        }
    }
}
