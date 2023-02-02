using ColossalFramework;
using System.Collections.Generic;
using UnityEngine;
using IMT.Manager;
using ModsCommon.Utilities;
using System;
using System.Linq;

namespace IMT.Utilities
{
    public struct DecalData : IStyleData, IDrawData
    {
        public MarkingLOD LOD { get; }
        public MarkingLODType LODType => MarkingLODType.Dash;

        private Vector3 Position { get; }
        private Color Color { get; }
        private Vector4 Size { get; }
        private Vector4 Tiling { get; }
        private Vector4[] Points { get; }
        private float ScratchDensity { get; }
        private Vector4 ScratchTiling { get; }
        private float VoidDensity { get; }
        private Vector4 VoidTiling { get; }

        public DecalData(MarkingLOD lod, Vector3 position, Color32 color, Vector3 size, Vector2 tiling, float scratch, Vector2 scratchTiling, float voidDensity, Vector2 voidTiling, params Vector2[] points)
        {
            LOD = lod;
            Position = position;
            Color = color.ToX3Vector();
            size.y = 3f;
            Size = size;
            Tiling = new Vector4(tiling.x, 0, tiling.y, 0);
            ScratchDensity = scratch;
            ScratchTiling = new Vector4(scratchTiling.x, 0, scratchTiling.y, 0);
            VoidDensity = voidDensity;
            VoidTiling = new Vector4(voidTiling.x, 0, voidTiling.y, 0);

            var count = (points.Length + 3) / 4;
            Points = new Vector4[count * 2];
            for (var i = 0; i < count * 4; i += 1)
            {
                var point = i < points.Length ? points[i] : points[points.Length - 1];
                if (i % 2 == 0)
                    Points[i / 2] += new Vector4(point.x, point.y, 0f, 0f);
                else
                    Points[i / 2] += new Vector4(0f, 0f, point.x, point.y);
            }
        }
        public DecalData(MarkingLOD lod, Vector3[] points, Color32 color, Vector2 tiling, float scratch, Vector2 scratchTiling, float voidDensity, Vector2 voidTiling)
        {
            var min = points[0];
            var max = points[0];

            for (var i = 1; i < points.Length; i += 1)
            {
                min = Vector3.Min(min, points[i]);
                max = Vector3.Max(max, points[i]);
            }

            var position = (min + max) * 0.5f;
            var size = (max - min);

            var pointUVs = new Vector2[points.Length];
            for (var i = 0; i < pointUVs.Length; i += 1)
            {
                var pos = points[i];
                var x = (pos.x - min.x) / size.x;
                var y = (pos.z - min.z) / size.z;
                pointUVs[i] = new Vector2(x, y);
            }

            this = new DecalData(lod, position, color, size, tiling, scratch, scratchTiling, voidDensity, voidTiling, pointUVs);
        }
        public DecalData(MarkingLOD lod, Area area, Color32 color, Vector2 tiling, float scratch, Vector2 scratchTiling, float voidDensity, Vector2 voidTiling)
        {
            var min = area.Min;
            var max = area.Max;
            var position = (min + max) * 0.5f;
            var size = (max - min);

            var pointUVs = new Vector2[area.Count];
            for (var i = 0; i < pointUVs.Length; i += 1)
            {
                var pos = area.Sides[i].Start.Position;
                var x = (pos.x - min.x) / size.x;
                var y = (pos.z - min.z) / size.z;
                pointUVs[i] = new Vector2(x, y);
            }

            this = new DecalData(lod, position, color, size, tiling, scratch, scratchTiling, voidDensity, voidTiling, pointUVs);
        }
        public IEnumerable<IDrawData> GetDrawData() { yield return this; }

        public static IEnumerable<DecalData> GetData(MarkingLOD lod, ITrajectory[] trajectories, float minAngle, float minLength, float maxLength, Color32 color, Vector2 tiling, float scratch, Vector2 scratchTiling, float voidDensity, Vector2 voidTiling)
        {
            var points = trajectories.SelectMany(c => GetPoints(c, lod, minAngle, minLength, maxLength)).ToArray();
            var triangles = Triangulator.TriangulateSimple(points, trajectories.GetDirection());
            if (triangles == null)
                yield break;

            if (points.Length <= 16)
            {
                yield return new DecalData(lod, points, color, tiling, scratch, scratchTiling, voidDensity, voidTiling);
            }
            else
            {
                var polygon = new Polygon(points, triangles);
                polygon.Arange(8, 3f);

                foreach (var area in polygon)
                {
                    yield return new DecalData(lod, area, color, tiling, scratch, scratchTiling, voidDensity, voidTiling);
                }
            }
        }
        private static IEnumerable<Vector3> GetPoints(ITrajectory trajectory, MarkingLOD lod, float minAngle, float minLength, float maxLength)
        {
            if (trajectory is StraightTrajectory straight)
                return new Vector3[] { trajectory.StartPosition };
            else
                return StyleHelper.CalculateSolid(trajectory, lod, (tr) => tr.StartPosition, minAngle, minLength, maxLength);
        }

        public void Draw(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data, bool infoView)
        {
            if (infoView)
                return;

            var instance = Singleton<PropManager>.instance;
            var materialBlock = instance.m_materialBlock;
            materialBlock.Clear();
            var material = RenderHelper.GetMaterial(Points.Length * 2);

            materialBlock.SetVector("_Color", Color);
            materialBlock.SetVector("_Size", Size);
            materialBlock.SetVector("_Tiling", Tiling);
            materialBlock.SetVectorArray("_Points", Points);
            materialBlock.SetFloat("_ScratchDensity", ScratchDensity);
            materialBlock.SetVector("_ScratchTiling", ScratchTiling);
            materialBlock.SetFloat("_VoidDensity", VoidDensity);
            materialBlock.SetVector("_VoidTiling", VoidTiling);

            Graphics.DrawMesh(RenderHelper.DecalMesh, Position, Quaternion.identity, material, RenderHelper.RoadLayer, null, 0, materialBlock);
        }
    }
}
