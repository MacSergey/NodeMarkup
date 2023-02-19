using ColossalFramework;
using System.Collections.Generic;
using UnityEngine;
using IMT.Manager;
using ModsCommon.Utilities;
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
        public static float DefaultHeight => 5f;


        public MarkingLOD LOD { get; }
        public MarkingLODType LODType => MarkingLODType.Dash;

        private readonly Material material;
        private readonly Texture2D mainTexture;
        private readonly Texture2D alphaTexture;
        public readonly Vector3 position;
        public readonly Quaternion rotation;
        public readonly Color color;
        public readonly Vector4 size;
        private readonly Vector4 tiling;
        private readonly Vector4[] points;
        private readonly float cracksDensity;
        private readonly Vector4 cracksTiling;
        private readonly float voidDensity;
        private readonly Vector4 voidTiling;
        private readonly float texture;

        public float Length => size.x;
        public float Width => size.z;
        public float Angle => rotation.eulerAngles.z * Mathf.Deg2Rad;

        private DecalData(MaterialType materialType, MarkingLOD lod, Texture2D mainTexture, Texture2D alphaTexture, Vector3 position, float angle, Color32 color, Vector3 size, Vector2 tiling, float cracksDensity, Vector2 cracksTiling, float voidDensity, Vector2 voidTiling, float texture, params Vector2[] points)
        {
            LOD = lod;
            this.material = RenderHelper.MaterialLib[materialType];
            this.mainTexture = mainTexture;
            this.alphaTexture = alphaTexture;
            this.position = position;
            this.rotation = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.down);
            this.color = color.ToX3Vector();
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

        public DecalData(IEffectStyle effectStyle, MaterialType materialType, MarkingLOD lod, Texture2D mainTexture, Texture2D alphaTexture, Vector3 position, float angle, float length, float width, Color32 color)
        {
            this = new DecalData(materialType, lod, mainTexture, alphaTexture, position, angle, color, new Vector3(length, DefaultHeight, width), Vector3.one, effectStyle.CracksDensity, effectStyle.CracksTiling, effectStyle.VoidDensity, effectStyle.VoidTiling, effectStyle.TextureDensity);
        }
        public DecalData(IEffectStyle effectStyle, MaterialType materialType, MarkingLOD lod, Vector3 pos, Vector3 dir, float length, float width, Color32 color)
        {
            this = new DecalData(materialType, lod, null, null, pos, dir.AbsoluteAngle(), color, new Vector3(length, DefaultHeight, width), Vector3.one, effectStyle.CracksDensity, effectStyle.CracksTiling, effectStyle.VoidDensity, effectStyle.VoidTiling, effectStyle.TextureDensity);
        }
        public DecalData(IEffectStyle effectStyle, MaterialType materialType, MarkingLOD lod, Vector3 startPos, Vector3 endPos, float width, Color32 color)
        {
            var pos = (startPos + endPos) * 0.5f;
            var angle = (endPos - startPos).AbsoluteAngle();
            var length = (endPos - startPos).magnitude;
            this = new DecalData(materialType, lod, null, null, pos, angle, color, new Vector3(length, DefaultHeight, width), Vector3.one, effectStyle.CracksDensity, effectStyle.CracksTiling, effectStyle.VoidDensity, effectStyle.VoidTiling, effectStyle.TextureDensity);
        }

        public DecalData(Marking.Item itemType, MarkingLOD lod, Vector3[] points, Color32 color, Vector2 tiling, float cracksDensity, Vector2 cracksTiling, float voidDensity, Vector2 voidTiling, float texture)
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

            var materialType = itemType switch
            {
                Marking.Item.Filler => GetFillerMaterial(points.Length),
                Marking.Item.Crosswalk => GetCrosswalkMaterial(points.Length),
                _ => MaterialType.Dash,
            };

            this = new DecalData(materialType, lod, null, null, position, 0f, color, size, tiling, cracksDensity, cracksTiling, voidDensity, voidTiling, texture, pointUVs);
        }
        public DecalData(Marking.Item itemType, MarkingLOD lod, Area area, Color32 color, Vector2 tiling, float cracksDensity, Vector2 cracksTiling, float voidDensity, Vector2 voidTiling, float texture)
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

            var materialType = itemType switch
            {
                Marking.Item.Filler => GetFillerMaterial(area.Count),
                Marking.Item.Crosswalk => GetCrosswalkMaterial(area.Count),
                _ => MaterialType.Dash,
            };

            this = new DecalData(materialType, lod, null, null, position, 0f, color, size, tiling, cracksDensity, cracksTiling, voidDensity, voidTiling, texture, pointUVs);
        }

        public void OnDestroy() { }
        public IEnumerable<IDrawData> GetDrawData() { yield return this; }

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

        public static List<DecalData> GetData(IEffectStyle effectStyle, Marking.Item itemType, MarkingLOD lod, ITrajectory[] trajectories, StyleHelper.SplitParams splitParams, Color32 color
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
                        result.Add(new DecalData(effectStyle, MaterialType.Dash, lod, point, Vector3.forward, 0.3f, 0.3f, Color.green));
                }
#endif
                if (points.Length <= 16)
                {
                    if (effectStyle != null)
                        result.Add(new DecalData(itemType, lod, points, color, Vector3.one, effectStyle.CracksDensity, effectStyle.CracksTiling, effectStyle.VoidDensity, effectStyle.VoidTiling, effectStyle.TextureDensity));
                    else
                        result.Add(new DecalData(itemType, lod, points, color, Vector3.one, 0f, Vector3.one, 0f, Vector3.one, 0f));
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
                                result.Add(new DecalData(effectStyle, MaterialType.Dash, lod, side.Start.Position, side.End.Position, 0.05f, Color.magenta));
                            }
                        }
#endif
                        if (effectStyle != null)
                            result.Add(new DecalData(itemType, lod, area, color, Vector3.one, effectStyle.CracksDensity, effectStyle.CracksTiling, effectStyle.VoidDensity, effectStyle.VoidTiling, effectStyle.TextureDensity));
                        else
                            result.Add(new DecalData(itemType, lod, area, color, Vector3.one, 0f, Vector3.one, 0f, Vector3.one, 0f));
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

        public void Draw(RenderManager.CameraInfo cameraInfo, RenderManager.Instance data, bool infoView)
        {
            if (infoView)
                return;

            var instance = Singleton<PropManager>.instance;
            var materialBlock = instance.m_materialBlock;
            materialBlock.Clear();

            if (mainTexture != null)
                materialBlock.SetTexture(mainTexId, mainTexture);

            if (alphaTexture != null)
                materialBlock.SetTexture(alphaTexId, alphaTexture);

            if (points != null && points.Length > 0)
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
