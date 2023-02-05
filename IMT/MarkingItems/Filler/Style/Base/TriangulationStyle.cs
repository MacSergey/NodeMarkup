using ColossalFramework.UI;
using IMT.API;
using IMT.Utilities;
using IMT.Utilities.API;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public abstract class TriangulationFillerStyle : FillerStyle
    {
        protected abstract MaterialType MaterialType { get; }

        public PropertyValue<float> Elevation { get; }
        public PropertyValue<float> CornerRadius { get; }
        public PropertyValue<float> MedianCornerRadius { get; }

        public TriangulationFillerStyle(Color32 color, float width, float lineOffset, float medianOffset, float elevation, float cornerRadius, float medianCornerRadius) : base(color, width, lineOffset, medianOffset)
        {
            Elevation = GetElevationProperty(elevation);
            CornerRadius = GetCornerRadiusProperty(cornerRadius);
            MedianCornerRadius = GetMedianCornerRadiusProperty(medianCornerRadius);
        }

        public override void CopyTo(FillerStyle target)
        {
            base.CopyTo(target);
            if (target is TriangulationFillerStyle triangulationTarget)
            {
                triangulationTarget.Elevation.Value = Elevation;
                triangulationTarget.CornerRadius.Value = CornerRadius;
                triangulationTarget.MedianCornerRadius.Value = MedianCornerRadius;
            }
        }

        protected override ContourGroup GetContours(MarkingFiller filler)
        {
            var contours = base.GetContours(filler);

            for (int i = 0; i < contours.Count; i += 1)
                contours[i] = contours[i].SetCornerRadius(CornerRadius, MedianCornerRadius);

            return contours;
        }
        protected override void CalculateImpl(MarkingFiller filler, ContourGroup contours, MarkingLOD lod, Action<IStyleData> addData)
        {
            if ((SupportLOD & lod) == 0)
                return;

            foreach (var contour in contours)
            {
                var points = GetContourPoints(contour, lod, out var groups);
                if (Triangulate(points, out var triangles))
                {
                    //SplitTriangles(contour, points, triangles, 2f, out var topPoints, out var topTriangles);

                    var data = new MarkingFillerMeshData(lod, Elevation, MarkingFillerMeshData.RawData.SetSide(groups, points, MaterialType.Pavement), MarkingFillerMeshData.RawData.SetTop(points, triangles, MaterialType));
                    addData(data);
#if DEBUG
                    //if ((Settings.ShowFillerTriangulation & 2) != 0)
                    //    yield return GetTriangulationLines(topPoints, topTriangles, UnityEngine.Color.red, MaterialType.RectangleFillers);
                    if ((Settings.ShowFillerTriangulation & 1) != 0)
                        GetTriangulationLines(points, triangles, UnityEngine.Color.green, MaterialType.Dash, addData);
#endif
                }
            }
        }
        protected Vector3[] GetContourPoints(List<ContourEdge> contour, MarkingLOD lod, out int[] groups)
        {
            var trajectories = contour.Select(i => i.trajectory).ToList();
            if (trajectories.GetDirection() == TrajectoryHelper.Direction.CounterClockWise)
                trajectories = trajectories.Select(t => t.Invert()).Reverse().ToList();

            var parts = GetParts(trajectories, lod);
            var points = parts.SelectMany(p => p).Select(t => t.StartPosition).ToArray();
            groups = parts.Select(g => g.Count).ToArray();

            return points;
        }
        protected bool Triangulate(Vector3[] points, out int[] triangles)
        {
            triangles = Triangulator.Triangulate(points, TrajectoryHelper.Direction.ClockWise);
            return triangles != null;
        }
        private List<List<ITrajectory>> GetParts(List<ITrajectory> trajectories, MarkingLOD lod)
        {
            var allParts = new List<List<ITrajectory>>();

            foreach (var trajectory in trajectories)
            {
                var partsT = StyleHelper.CalculateSolid(trajectory, lod, MinAngle, MinLength, MaxLength);
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
        //private void SplitTriangles(List<FillerContour.Part> contour, Vector3[] points, int[] triangles, float maxLenght, out Vector3[] pointsResult, out int[] trianglesResult)
        //{
        //    var contourCount = points.Length;
        //    var tempPoints = new List<Vector3>(points);
        //    var tempTriang = new List<int>(triangles);
        //    maxLenght = maxLenght * maxLenght;

        //    var pointDic = new Dictionary<Vector3, int>();

        //    var i = 0;
        //    while(i < tempTriang.Count && tempPoints.Count < 1000 && tempTriang.Count < 3000)
        //    {
        //        var index1 = tempTriang[i + 0];
        //        var index2 = tempTriang[i + 1];
        //        var index3 = tempTriang[i + 2];

        //        var point1 = tempPoints[index1];
        //        var point2 = tempPoints[index2];
        //        var point3 = tempPoints[index3];

        //        var dist12 = (point1 - point2).sqrMagnitude;
        //        var dist23 = (point2 - point3).sqrMagnitude;
        //        var dist31 = (point3 - point1).sqrMagnitude;

        //        if (dist12 > maxLenght && dist12 > dist23 && dist12 > dist31)
        //            ProcessSplitTriangle(contour, ref i, index1, index2, index3, tempPoints, tempTriang, pointDic, ref contourCount);
        //        else if (dist23 > maxLenght && dist23 > dist31 && dist23 > dist12)
        //            ProcessSplitTriangle(contour, ref i, index2, index3, index1, tempPoints, tempTriang, pointDic, ref contourCount);
        //        else if (dist31 > maxLenght && dist31 > dist12 && dist31 > dist23)
        //            ProcessSplitTriangle(contour, ref i, index3, index1, index2, tempPoints, tempTriang, pointDic, ref contourCount);
        //        else
        //            i += 3;
        //    }

        //    pointsResult = tempPoints.ToArray();
        //    trianglesResult = tempTriang.ToArray();
        //}
        //private void ProcessSplitTriangle(List<FillerContour.Part> contour, ref int i, int index1, int index2, int index3, List<Vector3> points, List<int> triangles, Dictionary<Vector3, int> pointDic, ref int contourCount)
        //{
        //    var newPoint = (points[index1] + points[index2]) * 0.5f;

        //    if ((newPoint - points[index3]).sqrMagnitude < 0.25f)
        //    {
        //        i += 3;
        //        return;
        //    }

        //    var minPart = -1;
        //    var minDist = float.MaxValue;
        //    var minT = 0f;
        //    for(int j = 0; j < contour.Count; j += 1)
        //    {
        //        var pos = contour[j].Trajectory.GetClosestPosition(newPoint, out var t);
        //        var dist = (pos - newPoint).sqrMagnitude;
        //        if(dist < minDist)
        //        {
        //            minDist = dist;
        //            minPart = j;
        //            minT = t;
        //        }
        //    }

        //    if (minPart >= 0)
        //    {
        //        var plane = new Plane();
        //        if (minT < 0.1f || minT > 0.9f)
        //        {
        //            var pos = contour[minPart].Trajectory.Position(minT);
        //            var dir = contour[minPart].Trajectory.Tangent(minT);
        //            var normal = dir.Turn90(true);
        //            plane.Set3Points(pos + dir, pos, pos + normal);
        //        }
        //        else
        //        {
        //            var posA = contour[minPart].Trajectory.Position(minT - 0.1f);
        //            var pos0 = contour[minPart].Trajectory.Position(minT);
        //            var posB = contour[minPart].Trajectory.Position(minT + 0.1f);

        //            var minDot = Vector3.Dot((posA - pos0).normalized, (posB - pos0).normalized);
        //            if(minDot > -0.995f)
        //                plane.Set3Points(posA, pos0, posB);
        //            else
        //                plane.Set3Points(posA, pos0, pos0 + (posA - pos0).Turn90(true));

        //        }
        //        plane.Raycast(new Ray(newPoint, newPoint + Vector3.up), out var newT);
        //        newPoint += Vector3.up * newT;
        //    }

        //    var dot = Vector3.Dot((points[index1] - newPoint).normalized, (points[index2] - newPoint).normalized);
        //    if(Mathf.Acos(dot) * Mathf.Rad2Deg > 175f)
        //    {
        //        i += 3;
        //        return;
        //    }

        //    if (!pointDic.TryGetValue(newPoint, out var indexNew))
        //    {
        //        //if(Math.Abs(index2 - index1) == 1)
        //        //{
        //        //    contourCount += 1;
        //        //    if (index2 > index1)
        //        //    {
        //        //        points.Insert(index2, newPoint);
        //        //        indexNew = index2;
        //        //        index2 += 1;
        //        //    }
        //        //    else
        //        //    {
        //        //        points.Insert(index1, newPoint);
        //        //        indexNew = index1;
        //        //        index1 += 1;
        //        //    }
        //        //    if (index3 > indexNew)
        //        //        index3 += 1;

        //        //    for(int j = 0; j < triangles.Count; j += 1)
        //        //    {
        //        //        if (triangles[j] >= indexNew)
        //        //            triangles[j] += 1;
        //        //    }
        //        //}
        //        //else
        //        //{
        //        //    indexNew = points.Count;
        //        //    points.Add(newPoint);
        //        //}
        //        indexNew = points.Count;
        //        points.Add(newPoint);
        //        pointDic.Add(newPoint, indexNew);
        //    }

        //    triangles[i + 0] = index1;
        //    triangles[i + 1] = indexNew;
        //    triangles[i + 2] = index3;

        //    triangles.Add(index3);
        //    triangles.Add(indexNew);
        //    triangles.Add(index2);
        //}
#if DEBUG
        private void GetTriangulationLines(Vector3[] points, int[] triangles, Color32 color, MaterialType materialType, Action<IStyleData> addData)
        {
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var point1 = points[triangles[i + 0]];
                var point2 = points[triangles[i + 1]];
                var point3 = points[triangles[i + 2]];

                addData(new DecalData(null, materialType, MarkingLOD.NoLOD, point1, point2, 0.05f, color));
                addData(new DecalData(null, materialType, MarkingLOD.NoLOD, point2, point3, 0.05f, color));
                addData(new DecalData(null, materialType, MarkingLOD.NoLOD, point3, point1, 0.05f, color));
            }
        }
#endif

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

        public override void GetUIComponents(MarkingFiller filler, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(filler, components, parent, isTemplate);
            components.Add(AddElevationProperty(this, parent, false));

            if (!isTemplate)
            {
                if (!filler.IsMedian)
                    components.Add(AddCornerRadiusProperty(this, parent, false));
                else
                    components.Add(AddMedianCornerRadiusProperty(this, parent, false));
            }
#if DEBUG
            //var material = GetVectorProperty(parent, "Material");
            //var textureA = GetVectorProperty(parent, "TextureA");
            //var textureB = GetVectorProperty(parent, "TextureB");

            //material.Value = (RenderHelper.MaterialLib[MaterialType].GetTexture("_APRMap") as Texture2D).GetPixel(0, 0);
            //textureA.Value = RenderHelper.SurfaceALib[MaterialType].GetPixel(0, 0);
            //textureB.Value = RenderHelper.SurfaceBLib[MaterialType].GetPixel(0, 0);

            //material.OnValueChanged += MaterialValueChanged;
            //textureA.OnValueChanged += TextureAValueChanged;
            //textureB.OnValueChanged += TextureBValueChanged;

            //components.Add(material);
            //components.Add(textureA);
            //components.Add(textureB);

            //void MaterialValueChanged(Color32 value)
            //{
            //    RenderHelper.MaterialLib[MaterialType] = RenderHelper.CreateRoadMaterial(TextureHelper.CreateTexture(128, 128, UnityEngine.Color.white), TextureHelper.CreateTexture(128, 128, value));
            //}
            //void TextureAValueChanged(Color32 value)
            //{
            //    RenderHelper.SurfaceALib[MaterialType] = TextureHelper.CreateTexture(512, 512, value);
            //}
            //void TextureBValueChanged(Color32 value)
            //{
            //    RenderHelper.SurfaceBLib[MaterialType] = TextureHelper.CreateTexture(512, 512, value);
            //}

            //static ColorPropertyPanel GetVectorProperty(UIComponent parent, string name)
            //{
            //    var vector = ComponentPool.Get<ColorPropertyPanel>(parent);
            //    vector.Init();
            //    vector.Text = name;
            //    return vector;
            //}
#endif
        }

        private static FloatPropertyPanel AddElevationProperty(TriangulationFillerStyle triangulationStyle, UIComponent parent, bool canCollapse)
        {
            var elevationProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(Elevation));
            elevationProperty.Text = Localize.FillerStyle_Elevation;
            elevationProperty.Format = Localize.NumberFormat_Meter;
            elevationProperty.UseWheel = true;
            elevationProperty.WheelStep = 0.1f;
            elevationProperty.WheelTip = Settings.ShowToolTip;
            elevationProperty.CheckMin = true;
            elevationProperty.MinValue = 0f;
            elevationProperty.CheckMax = true;
            elevationProperty.MaxValue = 10f;
            elevationProperty.CanCollapse = canCollapse;
            elevationProperty.Init();
            elevationProperty.Value = triangulationStyle.Elevation;
            elevationProperty.OnValueChanged += (float value) => triangulationStyle.Elevation.Value = value;

            return elevationProperty;
        }
        private static FloatPropertyPanel AddCornerRadiusProperty(TriangulationFillerStyle triangulationStyle, UIComponent parent, bool canCollapse)
        {
            var cornerRadiusProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(CornerRadius));
            cornerRadiusProperty.Text = Localize.FillerStyle_CornerRadius;
            cornerRadiusProperty.Format = Localize.NumberFormat_Meter;
            cornerRadiusProperty.UseWheel = true;
            cornerRadiusProperty.WheelStep = 0.1f;
            cornerRadiusProperty.WheelTip = Settings.ShowToolTip;
            cornerRadiusProperty.CheckMin = true;
            cornerRadiusProperty.MinValue = 0f;
            cornerRadiusProperty.CheckMax = true;
            cornerRadiusProperty.MaxValue = 10f;
            cornerRadiusProperty.CanCollapse = canCollapse;
            cornerRadiusProperty.Init();
            cornerRadiusProperty.Value = triangulationStyle.CornerRadius;
            cornerRadiusProperty.OnValueChanged += (float value) => triangulationStyle.CornerRadius.Value = value;

            return cornerRadiusProperty;
        }
        private static Vector2PropertyPanel AddMedianCornerRadiusProperty(TriangulationFillerStyle triangulationStyle, UIComponent parent, bool canCollapse)
        {
            var cornerRadiusProperty = ComponentPool.Get<Vector2PropertyPanel>(parent, nameof(CornerRadius));
            cornerRadiusProperty.Text = Localize.FillerStyle_CornerRadius;
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
            cornerRadiusProperty.CanCollapse = canCollapse;
            cornerRadiusProperty.Init(0, 1);
            cornerRadiusProperty.Value = new Vector2(triangulationStyle.CornerRadius, triangulationStyle.MedianCornerRadius);
            cornerRadiusProperty.OnValueChanged += (Vector2 value) =>
            {
                triangulationStyle.CornerRadius.Value = value.x;
                triangulationStyle.MedianCornerRadius.Value = value.y;
            };

            return cornerRadiusProperty;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Elevation.ToXml(config);
            CornerRadius.ToXml(config);
            MedianCornerRadius.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            Elevation.FromXml(config, DefaultElevation);
            CornerRadius.FromXml(config, DefaultCornerRadius);
            MedianCornerRadius.FromXml(config, DefaultCornerRadius);
        }
    }
}
