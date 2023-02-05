using ColossalFramework;
using System.Collections.Generic;
using UnityEngine;
using IMT.Manager;
using ModsCommon.Utilities;
using System;
using System.Linq;

namespace IMT.Utilities
{
    public readonly struct DecalData : IStyleData, IDrawData
    {
        private static int mainTexId = Shader.PropertyToID("_MainTex");
        private static int alphaTexId = Shader.PropertyToID("_Alpha");
        private static int colorId = Shader.PropertyToID("_Color");
        private static int sizeId = Shader.PropertyToID("_Size");
        private static int tilingId = Shader.PropertyToID("_Tiling");
        private static int pointsId = Shader.PropertyToID("_Points");
        private static int cracksDensityId = Shader.PropertyToID("_CracksDensity");
        private static int cracksTilingId = Shader.PropertyToID("_CracksTiling");
        private static int voidDensityId = Shader.PropertyToID("_VoidDensity");
        private static int voidTilingId = Shader.PropertyToID("_VoidTiling");
        private static int textureDensityId = Shader.PropertyToID("_TextureDensity");


        public MarkingLOD LOD { get; }
        public MarkingLODType LODType => MarkingLODType.Dash;

        private readonly Texture2D mainTexture;
        private readonly Texture2D alphaTexture;
        private readonly Vector3 position;
        private readonly Quaternion rotation;
        private readonly Color color;
        private readonly Vector4 size;
        private readonly Vector4 tiling;
        private readonly Vector4[] points;
        private readonly float cracksDensity;
        private readonly Vector4 cracksTiling;
        private readonly float voidDensity;
        private readonly Vector4 voidTiling;
        private readonly float texture;

        public DecalData(MarkingLOD lod, Texture2D mainTexture, Texture2D alphaTexture, Vector3 position, float angle, Color32 color, Vector3 size, Vector2 tiling, float cracksDensity, Vector2 cracksTiling, float voidDensity, Vector2 voidTiling, float texture, params Vector2[] points)
        {
            LOD = lod;
            this.mainTexture = mainTexture;
            this.alphaTexture = alphaTexture;
            this.position = position;
            this.rotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.down);
            this.color = color.ToX3Vector();
            size.y = 5f;
            this.size = size;
            this.tiling = new Vector4(tiling.x, 0f, tiling.y, 0f);
            this.cracksDensity = cracksDensity;
            this.cracksTiling = new Vector4(cracksTiling.x, 0f, cracksTiling.y, 0f);
            this.voidDensity = voidDensity;
            this.voidTiling = new Vector4(voidTiling.x, 0f, voidTiling.y, 0f);
            this.texture = texture;

            var count = (points.Length + 3) / 4;
            this.points = new Vector4[count * 2];
            for (var i = 0; i < count * 4; i += 1)
            {
                var point = i < points.Length ? points[i] : points[points.Length - 1];
                if (i % 2 == 0)
                    this.points[i / 2] += new Vector4(point.x, point.y, 0f, 0f);
                else
                    this.points[i / 2] += new Vector4(0f, 0f, point.x, point.y);
            }
        }
        public DecalData(MarkingLOD lod, Vector3 position, float angle, Color32 color, Vector3 size, Vector2 tiling, float cracksDensity, Vector2 cracksTiling, float voidDensity, Vector2 voidTiling, float texture, params Vector2[] points)
        {
            this = new DecalData(lod, null, null, position, angle, color, size, tiling, cracksDensity, cracksTiling, voidDensity, voidTiling, texture, points);
        }
        public DecalData(MarkingLOD lod, Vector3[] points, Color32 color, Vector2 tiling, float cracksDensity, Vector2 cracksTiling, float voidDensity, Vector2 voidTiling, float texture)
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

            this = new DecalData(lod, position, 0f, color, size, tiling, cracksDensity, cracksTiling, voidDensity, voidTiling, texture, pointUVs);
        }
        public DecalData(MarkingLOD lod, Area area, Color32 color, Vector2 tiling, float cracksDensity, Vector2 cracksTiling, float voidDensity, Vector2 voidTiling, float texture)
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

            this = new DecalData(lod, position, 0f, color, size, tiling, cracksDensity, cracksTiling, voidDensity, voidTiling, texture, pointUVs);
        }
        public IEnumerable<IDrawData> GetDrawData() { yield return this; }

        public static List<DecalData> GetData(MarkingLOD lod, ITrajectory[] trajectories, float minAngle, float minLength, float maxLength, Color32 color, Vector2 tiling, float cracks, Vector2 cracksTiling, float voidDensity, Vector2 voidTiling, float texture)
        {
            var result = new List<DecalData>();

            var points = trajectories.SelectMany(c => GetPoints(c, lod, minAngle, minLength, maxLength)).ToArray();
            var triangles = Triangulator.TriangulateSimple(points, trajectories.GetDirection());

            if (triangles != null)
            {
                if (points.Length <= 16)
                {
                    result.Add(new DecalData(lod, points, color, tiling, cracks, cracksTiling, voidDensity, voidTiling, texture));
                }
                else
                {
                    var polygon = new Polygon(points, triangles);
                    polygon.Arange(8, 3f);

                    foreach (var area in polygon)
                        result.Add(new DecalData(lod, area, color, tiling, cracks, cracksTiling, voidDensity, voidTiling, texture));
                }
            }

            return result;
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
            var material = RenderHelper.GetMaterial(points.Length * 2);

            if (mainTexture != null)
                materialBlock.SetTexture(mainTexId, mainTexture);

            if (alphaTexture != null)
                materialBlock.SetTexture(alphaTexId, alphaTexture);

            if(points != null && points.Length > 0)
                materialBlock.SetVectorArray(pointsId, points);

            materialBlock.SetVector(colorId, color);
            materialBlock.SetVector(sizeId, size);
            materialBlock.SetVector(tilingId, tiling);
            materialBlock.SetFloat(cracksDensityId, cracksDensity);
            materialBlock.SetVector(cracksTilingId, cracksTiling);
            materialBlock.SetFloat(voidDensityId, voidDensity);
            materialBlock.SetVector(voidTilingId, voidTiling);
            materialBlock.SetFloat(textureDensityId, texture);

            Graphics.DrawMesh(RenderHelper.DecalMesh, position, rotation, material, RenderHelper.RoadLayer, null, 0, materialBlock);
        }
    }
}
