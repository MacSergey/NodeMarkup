using IMT.UI;
using IMT.UI.Editors;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public abstract class MeshFillerStyle : BaseFillerStyle, IThemeFiller
    {
        public PropertyThemeValue PavementTheme { get; }
        public PropertyValue<float> Elevation { get; }

        public PropertyVector2Value CornerRadius { get; }
        public float LineCornerRadius => CornerRadius.Value.x;
        public float MedianCornerRadius => CornerRadius.Value.y;

        public MeshFillerStyle(ThemeHelper.IThemeData pavementTheme, Vector2 offset, float elevation, Vector2 cornerRadius) : base(default, default, offset)
        {
            PavementTheme = new PropertyThemeValue("PTHM", StyleChanged, pavementTheme);
            Elevation = GetElevationProperty(elevation);
            CornerRadius = new PropertyVector2Value(StyleChanged, cornerRadius, "CR", "MCR");
        }

        public override void CopyTo(BaseFillerStyle target)
        {
            base.CopyTo(target);
            if (target is MeshFillerStyle meshTarget)
            {
                meshTarget.PavementTheme.Value = PavementTheme.Value;
                meshTarget.Elevation.Value = Elevation;
                meshTarget.CornerRadius.Value = CornerRadius;
            }
        }

        protected override void CalculateImpl(MarkingFiller filler, ContourGroup contours, MarkingLOD lod, Action<IStyleData> addData)
        {
            if ((SupportLOD & lod) == 0)
                return;

            foreach (var contour in contours)
            {
                var roundedContour = contour.SetCornerRadius(LineCornerRadius, MedianCornerRadius);
                if (Triangulate(roundedContour, lod, out var points, out var groups, out var triangles))
                {
                    if (!GetTopTexture(out var topTexture, out var topColor))
                        return;
                    if (!GetSideTexture(out var sideTexture, out var sideColor))
                        return;

                    var top = FillerMeshData.RawData.GetTop(points, triangles, topColor, topTexture);
                    var side = FillerMeshData.RawData.GetSide(groups, points, sideColor, sideTexture);

                    var data = new FillerMeshData(lod, Elevation, top, side);
                    addData(data);
#if DEBUG
                    if ((Settings.ShowFillerTriangulation & 1) != 0)
                        GetTriangulationLines(points, triangles, UnityEngine.Color.green, MaterialType.Dash, addData);
#endif
                }
            }
        }

        protected virtual bool GetTopTexture(out FillerMeshData.TextureData textureData, out Color color) => GetTexture(out textureData, out color);
        protected virtual bool GetSideTexture(out FillerMeshData.TextureData textureData, out Color color) => GetTexture(out textureData, out color);
        private bool GetTexture(out FillerMeshData.TextureData textureData, out Color color)
        {
            var theme = (PavementTheme.Value is ThemeHelper.IThemeData themeData ? themeData : ThemeHelper.DefaultTheme).GetTexture(ThemeHelper.TextureType.Pavement);

            if (theme.texture != null)
            {
                textureData = new FillerMeshData.TextureData(theme.texture, theme.tiling, 0f);
                color = UnityEngine.Color.white;
                return true;
            }
            else
            {
                textureData = default;
                color = default;
                return false;
            }
        }

        protected bool Triangulate(Contour contour, MarkingLOD lod, out Vector3[] points, out int[] groups, out int[] triangles)
        {
            var trajectories = contour.Select(i => i.trajectory).ToList();
            if (trajectories.GetDirection() == TrajectoryHelper.Direction.CounterClockWise)
                trajectories = trajectories.Select(t => t.Invert()).Reverse().ToList();

            var parts = GetParts(trajectories, lod);
            points = parts.SelectMany(p => p).Select(t => t.StartPosition).ToArray();
            groups = parts.Select(g => g.Count).ToArray();
            triangles = Triangulator.TriangulateSimple(points, TrajectoryHelper.Direction.ClockWise);
            return triangles != null;
        }

        private List<List<ITrajectory>> GetParts(List<ITrajectory> trajectories, MarkingLOD lod)
        {
            var allParts = new List<List<ITrajectory>>();

            foreach (var trajectory in trajectories)
            {
                var partsT = StyleHelper.CalculateSolid(trajectory, lod, SplitParams);
                var parts = new List<ITrajectory>();
                foreach (var partT in partsT)
                    parts.Add(trajectory.Cut(partT.start, partT.end));
                allParts.Add(parts);
            }

            for (var i = 0; i < allParts.Count; i += 1)
            {
                var xm = (i - 1 + allParts.Count) % allParts.Count;
                var x = i;
                var y = (i + 1) % allParts.Count;
                var yp = (i + 2) % allParts.Count;

                if (FindIntersects(allParts[x], allParts[y], true, 1))
                    continue;
                if (allParts.Count > 3 && allParts[y].Count == 1 && FindIntersects(allParts[x], allParts[yp], true, 0))
                {
                    allParts.RemoveAt(y);
                    continue;
                }
                if (FindIntersects(allParts[y], allParts[x], false, 1))
                    continue;
                if (allParts.Count > 3 && allParts[x].Count == 1 && FindIntersects(allParts[y], allParts[xm], false, 0))
                {
                    allParts.RemoveAt(x);
                    i -= 1;
                    continue;
                }
            }

            return allParts;
        }
        private bool FindIntersects(List<ITrajectory> A, List<ITrajectory> B, bool isClockWise, int skip)
        {
            for (var x = isClockWise ? A.Count - 1 : 0; isClockWise ? x >= 0 : x < A.Count; x += isClockWise ? -1 : 1)
            {
                var xPart = new StraightTrajectory(A[x]);
                for (var y = isClockWise ? skip : B.Count - 1 - skip; isClockWise ? y < B.Count : y >= 0; y += isClockWise ? 1 : -1)
                {
                    var yPart = new StraightTrajectory(B[y]);
                    var intersect = Intersection.CalculateSingle(xPart, yPart);
                    if (intersect.isIntersect)
                    {
                        if (isClockWise)
                        {
                            A[x] = xPart.Cut(0f, intersect.firstT);
                            B[y] = yPart.Cut(intersect.secondT, 1f);
                            B.RemoveRange(0, y);
                        }
                        else
                        {
                            A[x] = xPart.Cut(intersect.firstT, 1f);
                            B[y] = yPart.Cut(0f, intersect.secondT);
                            B.RemoveRange(y + 1, B.Count - (y + 1));
                        }

                        return true;
                    }
                }
            }

            return false;
        }
#if DEBUG
        protected void GetTriangulationLines(Vector3[] points, int[] triangles, Color32 color, MaterialType materialType, Action<IStyleData> addData)
        {
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var point1 = points[triangles[i + 0]];
                var point2 = points[triangles[i + 1]];
                var point3 = points[triangles[i + 2]];

                addData(new DecalData(materialType, MarkingLOD.NoLOD, point1, point2, 0.05f, color, DecalData.TextureData.Default, DecalData.EffectData.Default));
                addData(new DecalData(materialType, MarkingLOD.NoLOD, point2, point3, 0.05f, color, DecalData.TextureData.Default, DecalData.EffectData.Default));
                addData(new DecalData(materialType, MarkingLOD.NoLOD, point3, point1, 0.05f, color, DecalData.TextureData.Default, DecalData.EffectData.Default));
            }
        }
#endif

        protected override void GetUIComponents(MarkingFiller filler, EditorProvider provider)
        {
            base.GetUIComponents(filler, provider);

            provider.AddProperty(new PropertyInfo<SelectThemeProperty>(this, nameof(PavementTheme), MainCategory, AddPavementThemeProperty, RefreshPavementThemeProperty));
            provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Elevation), MainCategory, AddElevationProperty));
            if (!provider.isTemplate)
            {
                if (!filler.IsMedian)
                    provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(LineCornerRadius), MainCategory, AddCornerRadiusProperty));
                else
                    provider.AddProperty(new PropertyInfo<Vector2PropertyPanel>(this, nameof(LineCornerRadius), MainCategory, AddMedianCornerRadiusProperty));

            }
        }
        private void AddPavementThemeProperty(SelectThemeProperty themeProperty, EditorProvider provider)
        {
            themeProperty.Label = Localize.StyleOption_PavementTheme;
            themeProperty.Init();
            themeProperty.RawName = PavementTheme.RawName;
            themeProperty.TextureType = ThemeHelper.TextureType.Pavement;
            themeProperty.Theme = PavementTheme.Value;
            themeProperty.OnValueChanged += (value) => PavementTheme.Value = value;
        }
        private void RefreshPavementThemeProperty(SelectThemeProperty themeProperty, EditorProvider provider)
        {
            themeProperty.IsHidden = ThemeHelper.ThemeCount == 0 && string.IsNullOrEmpty(PavementTheme.RawName);
        }

        private void AddElevationProperty(FloatPropertyPanel elevationProperty, EditorProvider provider)
        {
            elevationProperty.Label = Localize.FillerStyle_Elevation;
            elevationProperty.Format = Localize.NumberFormat_Meter;
            elevationProperty.UseWheel = true;
            elevationProperty.WheelStep = 0.1f;
            elevationProperty.WheelTip = Settings.ShowToolTip;
            elevationProperty.CheckMin = true;
            elevationProperty.MinValue = -100f;
            elevationProperty.CheckMax = true;
            elevationProperty.MaxValue = 100f;
            elevationProperty.Init();
            elevationProperty.Value = Elevation;
            elevationProperty.OnValueChanged += (float value) => Elevation.Value = value;
        }
        private void AddCornerRadiusProperty(FloatPropertyPanel cornerRadiusProperty, EditorProvider provider)
        {
            cornerRadiusProperty.Label = Localize.FillerStyle_CornerRadius;
            cornerRadiusProperty.Format = Localize.NumberFormat_Meter;
            cornerRadiusProperty.UseWheel = true;
            cornerRadiusProperty.WheelStep = 0.1f;
            cornerRadiusProperty.WheelTip = Settings.ShowToolTip;
            cornerRadiusProperty.CheckMin = true;
            cornerRadiusProperty.MinValue = 0f;
            cornerRadiusProperty.CheckMax = true;
            cornerRadiusProperty.MaxValue = 10f;
            cornerRadiusProperty.Init();
            cornerRadiusProperty.Value = LineCornerRadius;
            cornerRadiusProperty.OnValueChanged += (float value) => CornerRadius.Value = new Vector2(value, value);
        }
        private void AddMedianCornerRadiusProperty(Vector2PropertyPanel cornerRadiusProperty, EditorProvider provider)
        {
            cornerRadiusProperty.Label = Localize.FillerStyle_CornerRadius;
            cornerRadiusProperty.FieldsWidth = 50f;
            cornerRadiusProperty.SetLabels(Localize.FillerStyle_CornerRadiusAbrv, Localize.FillerStyle_CornerRadiusMedianAbrv);
            cornerRadiusProperty.Format = Localize.NumberFormat_Meter;
            cornerRadiusProperty.UseWheel = true;
            cornerRadiusProperty.WheelStep = new Vector2(0.1f, 0.1f);
            cornerRadiusProperty.WheelTip = Settings.ShowToolTip;
            cornerRadiusProperty.CheckMin = true;
            cornerRadiusProperty.MinValue = new Vector2(0f, 0f);
            cornerRadiusProperty.CheckMax = true;
            cornerRadiusProperty.MaxValue = new Vector2(10f, 10f);
            cornerRadiusProperty.Init(0, 1);
            cornerRadiusProperty.Value = CornerRadius;
            cornerRadiusProperty.OnValueChanged += (Vector2 value) => CornerRadius.Value = value;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            PavementTheme.ToXml(config);
            Elevation.ToXml(config);
            CornerRadius.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            PavementTheme.FromXml(config, ThemeHelper.DefaultTheme);
            Elevation.FromXml(config, DefaultElevation);
            CornerRadius.FromXml(config, DefaultCornerRadius);
        }
    }
}
