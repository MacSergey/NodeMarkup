using ColossalFramework;
using System.Collections.Generic;
using UnityEngine;
using IMT.Manager;
using ModsCommon.Utilities;
using System.Linq;
using System;

namespace IMT.Utilities
{
    public readonly struct DecalData : IStyleData
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
        public static float DefaultHeight => 5f;


        public MarkingLOD LOD { get; }
        public MarkingLODType LODType => MarkingLODType.Dash;
        public int RenderLayer => RenderHelper.RoadLayer;

        private readonly Material material;
        private readonly TextureData textureData;
        public readonly Vector3 position;
        public readonly Quaternion rotation;
        public readonly Color color;
        public readonly Vector4 size;
        private readonly Vector4[] points;
        private readonly EffectData effectData;

        public float Length => size.x;
        public float Width => size.z;
        public float Angle => rotation.eulerAngles.z * Mathf.Deg2Rad;

        private DecalData(MaterialType materialType, MarkingLOD lod, Vector3 position, float angle, Color32 color, Vector3 size, TextureData textureData, EffectData effectData, params Vector2[] points)
        {
            LOD = lod;
            this.material = RenderHelper.MaterialLib[materialType];
            this.textureData = textureData;
            this.position = position;
            this.rotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.down);
            this.color = textureData.mainTexture != null ? color : color.ToX3Vector();
            this.size = size;
            this.effectData = effectData;

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

        public DecalData(MaterialType materialType, MarkingLOD lod, Vector3 position, float angle, float length, float width, Color32 color, TextureData textureData, EffectData effectData)
        {
            this = new DecalData(materialType, lod, position, angle, color, new Vector3(length, DefaultHeight, width), textureData, effectData);
        }
        public DecalData(MaterialType materialType, MarkingLOD lod, Vector3 pos, Vector3 dir, float length, float width, Color32 color, TextureData textureData, EffectData effectData)
        {
            this = new DecalData(materialType, lod, pos, dir.AbsoluteAngle(), color, new Vector3(length, DefaultHeight, width), textureData, effectData);
        }
        public DecalData(MaterialType materialType, MarkingLOD lod, Vector3 startPos, Vector3 endPos, float width, Color32 color, TextureData textureData, EffectData effectData)
        {
            var pos = (startPos + endPos) * 0.5f;
            var angle = (endPos - startPos).AbsoluteAngle();
            var length = (endPos - startPos).magnitude;
            this = new DecalData(materialType, lod, pos, angle, color, new Vector3(length, DefaultHeight, width), textureData, effectData);
        }

        public DecalData(DecalType type, MarkingLOD lod, Vector3[] points, Color32 color, TextureData textureData, EffectData effectData)
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
            size.y = Mathf.Max(size.y * 2f, 1f);

            var pointUVs = new Vector2[points.Length];
            for (var i = 0; i < pointUVs.Length; i += 1)
            {
                var pos = points[i];
                var x = (pos.x - min.x) / size.x;
                var y = (pos.z - min.z) / size.z;
                pointUVs[i] = new Vector2(x, y);
            }

            var materialType = type switch
            {
                DecalType.Filler => GetFillerMaterial(points.Length),
                DecalType.FillerIsland => GetFillerIslandMaterial(points.Length),
                DecalType.Crosswalk => GetCrosswalkMaterial(points.Length),
                _ => MaterialType.Dash,
            };

            this = new DecalData(materialType, lod, position, 0f, color, size, textureData, effectData, pointUVs);
        }
        public DecalData(DecalType type, MarkingLOD lod, Area area, Color32 color, TextureData textureData, EffectData effectData)
        {
            var min = area.Min;
            var max = area.Max;
            var position = (min + max) * 0.5f;
            var size = (max - min);
            size.y = Mathf.Max(size.y * 2f, 1f);

            var pointUVs = new Vector2[area.Count];
            for (var i = 0; i < pointUVs.Length; i += 1)
            {
                var pos = area.Sides[i].Start.Position;
                var x = (pos.x - min.x) / size.x;
                var y = (pos.z - min.z) / size.z;
                pointUVs[i] = new Vector2(x, y);
            }

            var materialType = type switch
            {
                DecalType.Filler => GetFillerMaterial(area.Count),
                DecalType.FillerIsland => GetFillerIslandMaterial(area.Count),
                DecalType.Crosswalk => GetCrosswalkMaterial(area.Count),
                _ => MaterialType.Dash,
            };

            this = new DecalData(materialType, lod, position, 0f, color, size, textureData, effectData, pointUVs);
        }

        public static MaterialType GetFillerMaterial(int points)
        {
            if (points == 0)
                return MaterialType.FillerZero;
            else if (points <= 4)
                return MaterialType.FillerUpTo4;
            else if (points <= 8)
                return MaterialType.FillerUpTo8;
            else if (points <= 12)
                return MaterialType.FillerUpTo12;
            else
                return MaterialType.FillerUpTo16;
        }
        public static MaterialType GetFillerIslandMaterial(int points)
        {
            if (points == 0)
                return MaterialType.FillerIslandZero;
            else if (points <= 4)
                return MaterialType.FillerIslandUpTo4;
            else if (points <= 8)
                return MaterialType.FillerIslandUpTo8;
            else if (points <= 12)
                return MaterialType.FillerIslandUpTo12;
            else
                return MaterialType.FillerIslandUpTo16;
        }
        public static MaterialType GetCrosswalkMaterial(int points)
        {
            if (points == 0)
                return MaterialType.CrosswalkZero;
            else if (points <= 4)
                return MaterialType.CrosswalkUpTo4;
            else if (points <= 8)
                return MaterialType.CrosswalkUpTo8;
            else if (points <= 12)
                return MaterialType.CrosswalkUpTo12;
            else
                return MaterialType.CrosswalkUpTo16;
        }

        public static List<DecalData> GetData(DecalType type, MarkingLOD lod, ITrajectory[] trajectories, StyleHelper.SplitParams splitParams, Color32 color, TextureData textureData, EffectData effectData
#if DEBUG
            , bool debug = false
#endif
            )
        {
            var result = new List<DecalData>();

            var points = trajectories.SelectMany(c => GetPoints(c, lod, splitParams)).ToArray();
            var triangles = Triangulator.TriangulateSimple(points, trajectories.GetDirection());

            if (triangles != null)
            {
#if DEBUG
                if (debug)
                {
                    foreach (var point in points)
                        result.Add(new DecalData(MaterialType.Dash, lod, point, Vector3.forward, 0.3f, 0.3f, Color.green, textureData, effectData));
                }
#endif
                if (points.Length <= 16)
                {
                    result.Add(new DecalData(type, lod, points, color, textureData, effectData));
                }
                else
                {
                    var polygon = new Polygon(points, triangles);
                    polygon.Arrange(8, splitParams.maxHeight);

                    foreach (var area in polygon)
                    {
#if DEBUG
                        if (debug)
                        {
                            foreach (var side in area.Sides)
                            {
                                result.Add(new DecalData(MaterialType.Dash, lod, side.Start.Position, side.End.Position, 0.05f, Color.magenta, TextureData.Default, EffectData.Default));
                            }
                        }
#endif
                        result.Add(new DecalData(type, lod, area, color, textureData, effectData));
                    }
                }
            }

            return result;
        }

        private static IEnumerable<Vector3> GetPoints(ITrajectory trajectory, MarkingLOD lod, StyleHelper.SplitParams splitParams)
        {
            if (trajectory is StraightTrajectory straight)
                return new Vector3[] { trajectory.StartPosition };
            else
            {
                var parts = StyleHelper.CalculateSolid(trajectory, lod, splitParams);
                return parts.Select(p => trajectory.Position(p.start));
            }
        }

        public void Render(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data, bool infoView)
        {
            if (infoView)
                return;

            var instance = Singleton<PropManager>.instance;
            var materialBlock = instance.m_materialBlock;
            materialBlock.Clear();

            if (textureData.mainTexture != null)
                materialBlock.SetTexture(mainTexId, textureData.mainTexture);

            if (textureData.alphaTexture != null)
                materialBlock.SetTexture(alphaTexId, textureData.alphaTexture);

            if (points != null && points.Length > 0)
                materialBlock.SetVectorArray(pointsId, points);

            materialBlock.SetVector(colorId, color);
            materialBlock.SetVector(sizeId, size);
            materialBlock.SetVector(tilingId, textureData.tiling);
            materialBlock.SetFloat(cracksDensityId, effectData.cracksDensity);
            materialBlock.SetVector(cracksTilingId, effectData.cracksTiling);
            materialBlock.SetFloat(voidDensityId, effectData.voidDensity);
            materialBlock.SetVector(voidTilingId, effectData.voidTiling);
            materialBlock.SetFloat(textureDensityId, effectData.texture);

            Graphics.DrawMesh(RenderHelper.DecalMesh, position, rotation, material, RenderHelper.RoadLayer, null, 0, materialBlock);
        }

        public bool CalculateGroupData(int layer, ref int vertexCount, ref int triangleCount, ref int objectCount, ref RenderGroup.VertexArrays vertexArrays)
        {
            return false;
        }

        public void PopulateGroupData(int layer, ref int vertexIndex, ref int triangleIndex, Vector3 groupPosition, RenderGroup.MeshData data, ref Vector3 min, ref Vector3 max, ref float maxRenderDistance, ref float maxInstanceDistance, ref bool requireSurfaceMaps)
        {

        }

        public readonly struct TextureData
        {
            public static TextureData Default => new TextureData(null, null);

            public readonly Texture2D mainTexture;
            public readonly Texture2D alphaTexture;
            public readonly Vector4 tiling;

            public TextureData(Texture2D mainTexture, Texture2D alphaTexture)
            {
                this.mainTexture = mainTexture;
                this.alphaTexture = alphaTexture;
                this.tiling = new Vector4(1f, 0f, 1f, 1f);
            }
            public TextureData(Texture2D mainTexture, Texture2D alphaTexture, Vector2 tiling, float angle)
            {
                this.mainTexture = mainTexture;
                this.alphaTexture = alphaTexture;
                this.tiling = new Vector4(tiling.x, Mathf.Sin(angle), tiling.y, Mathf.Cos(angle));
            }
        }
        public readonly struct EffectData
        {
            public static EffectData Default => new EffectData(null);

            public readonly float cracksDensity;
            public readonly Vector4 cracksTiling;
            public readonly float voidDensity;
            public readonly Vector4 voidTiling;
            public readonly float texture;

            public EffectData(IEffectStyle effectStyle)
            {
                if (effectStyle != null)
                {
                    this.cracksDensity = effectStyle.CracksDensity;
                    this.cracksTiling = new Vector4(effectStyle.CracksTiling.x, 0f, effectStyle.CracksTiling.y, 0f);
                    this.voidDensity = effectStyle.VoidDensity;
                    this.voidTiling = new Vector4(effectStyle.VoidTiling.x, 0f, effectStyle.VoidTiling.y, 0f);
                    this.texture = effectStyle.Texture;
                }
                else
                {
                    this.cracksDensity = 0f;
                    this.cracksTiling = new Vector4(1f, 0f, 1f, 0f);
                    this.voidDensity = 0f;
                    this.voidTiling = new Vector4(1f, 0f, 1f, 0f);
                    this.texture = 0f;
                }
            }
            public EffectData(float cracksDensity, Vector2 cracksTiling, float voidDensity, Vector2 voidTiling, float texture)
            {
                this.cracksDensity = cracksDensity;
                this.cracksTiling = new Vector4(cracksTiling.x, 0f, cracksTiling.y, 0f);
                this.voidDensity = voidDensity;
                this.voidTiling = new Vector4(voidTiling.x, 0f, voidTiling.y, 0f);
                this.texture = texture;
            }
        }

        public enum DecalType
        {
            Dash,
            Filler,
            FillerIsland,
            Crosswalk,
        }
    }
}
