using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;
using ColossalFramework.Math;

namespace NodeMarkup.Manager
{
    public abstract class Filler3DStyle : FillerStyle
    {
        public Filler3DStyle(Color32 color, float width, float lineOffset, float medianOffset) : base(color, width, lineOffset, medianOffset) { }
    }
    public abstract class TriangulationFillerStyle : Filler3DStyle
    {
        protected abstract MaterialType MaterialType { get; }

        public float MinAngle => 5f;
        public float MinLength => 0.3f;
        public float MaxLength => 10f;
        public PropertyValue<float> Elevation { get; }
        public PropertyValue<float> CornerRadius { get; }
        public PropertyValue<float> MedianCornerRadius { get; }

        protected abstract int ElevationIndex { get; }
        protected abstract int CornerRadiusIndex { get; }

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

        protected override List<List<FillerContour.Part>> GetContours(MarkupFiller filler)
        {
            var contours = base.GetContours(filler);

            for (int i = 0; i < contours.Count; i += 1)
                contours[i] = StyleHelper.SetCornerRadius(contours[i], CornerRadius, MedianCornerRadius);

            return contours;
        }
        protected override IEnumerable<IStyleData> CalculateImpl(MarkupFiller filler, List<List<FillerContour.Part>> contours, MarkupLOD lod)
        {
            if ((SupportLOD & lod) == 0)
                yield break;

            foreach (var contour in contours)
            {
                var points = GetContourPoints(contour, lod, out var groups);
                if (Triangulate(points, out var triangles))
                {
                    //SplitTriangles(contour, points, triangles, 2f, out var topPoints, out var topTriangles);

                    yield return new MarkupFillerMeshData(lod, Elevation, MarkupFillerMeshData.RawData.SetSide(groups, points, MaterialType.Pavement), MarkupFillerMeshData.RawData.SetTop(points, triangles, MaterialType));
#if DEBUG
                    //if ((Settings.ShowFillerTriangulation & 2) != 0)
                    //    yield return GetTriangulationLines(topPoints, topTriangles, UnityEngine.Color.red, MaterialType.RectangleFillers);
                    if ((Settings.ShowFillerTriangulation & 1) != 0)
                        yield return GetTriangulationLines(points, triangles, UnityEngine.Color.green, MaterialType.RectangleLines);
#endif
                }
            }
        }
        protected Vector3[] GetContourPoints(List<FillerContour.Part> contour, MarkupLOD lod, out int[] groups)
        {
            var trajectories = contour.Select(i => i.Trajectory).ToList();
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
        private List<List<ITrajectory>> GetParts(List<ITrajectory> trajectories, MarkupLOD lod)
        {
            var parts = trajectories.Select(t => StyleHelper.CalculateSolid(t, lod, (tr) => tr, MinAngle, MinLength, MaxLength)).ToList();
            for (var i = 0; i < parts.Count; i += 1)
            {
                var xm = (i - 1 + parts.Count) % parts.Count;
                var x = i;
                var y = (i + 1) % parts.Count;
                var yp = (i + 2) % parts.Count;

                if (FindIntersects(parts[x], parts[y], true, 1))
                    continue;
                if (parts.Count > 3 && parts[y].Count == 1 && FindIntersects(parts[x], parts[yp], true, 0))
                {
                    parts.RemoveAt(y);
                    continue;
                }
                if (FindIntersects(parts[y], parts[x], false, 1))
                    continue;
                if (parts.Count > 3 && parts[x].Count == 1 && FindIntersects(parts[y], parts[xm], false, 0))
                {
                    parts.RemoveAt(x);
                    i -= 1;
                    continue;
                }
            }

            return parts;
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
        private IStyleData GetTriangulationLines(Vector3[] points, int[] triangles, Color32 color, MaterialType materialType)
        {
            var dashes = new List<MarkupPartData>();

            for (int i = 0; i < triangles.Length; i += 3)
            {
                var point1 = points[triangles[i + 0]];
                var point2 = points[triangles[i + 1]];
                var point3 = points[triangles[i + 2]];

                dashes.Add(new MarkupPartData(point1, point2, 0.05f, color, materialType));
                dashes.Add(new MarkupPartData(point2, point3, 0.05f, color, materialType));
                dashes.Add(new MarkupPartData(point3, point1, 0.05f, color, materialType));
            }

            return new MarkupPartGroupData(MarkupLOD.NoLOD, dashes);
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
                    if (intersect.IsIntersect)
                    {
                        if (isClockWise)
                        {
                            A[x] = xPart.Cut(0f, intersect.FirstT);
                            B[y] = yPart.Cut(intersect.SecondT, 1f);
                            B.RemoveRange(0, y);
                        }
                        else
                        {
                            A[x] = xPart.Cut(intersect.FirstT, 1f);
                            B[y] = yPart.Cut(0f, intersect.SecondT);
                            B.RemoveRange(y + 1, B.Count - (y + 1));
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        public override void GetUIComponents(MarkupFiller filler, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
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
        public override int GetUIComponentSortIndex(EditorItem item)
        {
            if (item.name == nameof(Elevation))
                return ElevationIndex;
            else if (item.name == nameof(CornerRadius))
                return CornerRadiusIndex;
            else
                return base.GetUIComponentSortIndex(item);
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
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Elevation.FromXml(config, DefaultElevation);
            CornerRadius.FromXml(config, DefaultCornerRadius);
            MedianCornerRadius.FromXml(config, DefaultCornerRadius);
        }
    }
    public abstract class CurbTriangulationFillerStyle : TriangulationFillerStyle
    {
        public struct CounterData
        {
            public List<FillerContour.Part> _side;
            public List<FillerContour.Part> _hole;
        }

        public PropertyValue<float> CurbSize { get; }
        public PropertyValue<float> MedianCurbSize { get; }

        protected abstract int CurbSizeIndex { get; }

        public CurbTriangulationFillerStyle(Color32 color, float width, float lineOffset, float medianOffset, float elevation, float cornerRadius, float medianCornerRadius, float curbSize, float medianCurbSize) : base(color, width, lineOffset, medianOffset, elevation, cornerRadius, medianCornerRadius)
        {
            CurbSize = GetCurbSizeProperty(curbSize);
            MedianCurbSize = GetMedianCurbSizeProperty(medianCurbSize);
        }

        public override void CopyTo(FillerStyle target)
        {
            base.CopyTo(target);
            if (target is CurbTriangulationFillerStyle curbTarget)
            {
                curbTarget.CurbSize.Value = CurbSize;
                curbTarget.MedianCurbSize.Value = MedianCurbSize;
            }
        }
        public override IEnumerable<IStyleData> Calculate(MarkupFiller filler)
        {
            if (CurbSize == 0f && MedianCurbSize == 0f)
            {
                foreach (var data in base.Calculate(filler))
                    yield return data;
            }
            else
            {
                var originalContour = filler.Contour.Parts.ToList();

                var contourDatas = StyleHelper.SetOffset(originalContour, LineOffset, MedianOffset).Select(i => new CounterData() { _side = i }).ToArray();

                for (int i = 0; i < contourDatas.Length; i += 1)
                {
                    contourDatas[i]._side = StyleHelper.SetCornerRadius(contourDatas[i]._side, CornerRadius, MedianCornerRadius);
                    if (CurbSize > 0 || MedianCurbSize > 0)
                        contourDatas[i]._hole = StyleHelper.SetOffset(contourDatas[i]._side, CurbSize, MedianCurbSize).FirstOrDefault();
                }

                foreach (var lod in EnumExtension.GetEnumValues<MarkupLOD>())
                {
                    foreach (var data in Calculate(filler, contourDatas, lod))
                        yield return data;
                }
            }
        }
        private IEnumerable<IStyleData> Calculate(MarkupFiller filler, CounterData[] contours, MarkupLOD lod)
        {
            if ((SupportLOD & lod) == 0)
                yield break;

            if (lod == MarkupLOD.LOD1)
            {
                foreach (var data in base.CalculateImpl(filler, contours.Select(c => c._side).ToList(), lod))
                    yield return data;
            }
            else
            {
                foreach (var contour in contours)
                {
                    var meshParts = new List<MarkupFillerMeshData.RawData>();

                    var sidePoints = GetContourPoints(contour._side, lod, out var sideGroups);
                    if (Triangulate(sidePoints, out var triangles))
                    {
                        meshParts.Add(MarkupFillerMeshData.RawData.SetSide(sideGroups, sidePoints, MaterialType.Pavement));

                        if (contour._hole != null)
                        {
                            var holePoints = GetContourPoints(contour._hole, lod, out var holeGroups);
                            if (Triangulate(holePoints, out var holeTriangles))
                            {
                                holePoints = holePoints.Select(p => p += new Vector3(0f, 0.03f, 0f)).ToArray();
                                meshParts.Add(MarkupFillerMeshData.RawData.SetTop(holePoints, holeTriangles, MaterialType));
                                //var sideStartI = 0;
                                //var sideHalfI = contour._side.Count / 2;

                                //var holeStartI = 0;
                                //var holeHalfI = 0;
                                //float startMinDist = float.MaxValue;
                                //float halfMinDist = float.MaxValue;

                                //for (int i = 0; i < contour._hole.Count; i += 1)
                                //{
                                //    var dist = (contour._side[sideStartI].Trajectory.StartPosition - contour._hole[i].Trajectory.StartPosition).sqrMagnitude;
                                //    if (dist < startMinDist)
                                //    {
                                //        holeStartI = i;
                                //        startMinDist = dist;
                                //    }
                                //    dist = (contour._side[sideHalfI].Trajectory.StartPosition - contour._hole[i].Trajectory.StartPosition).sqrMagnitude;
                                //    if (dist < halfMinDist)
                                //    {
                                //        holeHalfI = i;
                                //        halfMinDist = dist;
                                //    }
                                //}

                                //if (sideStartI != sideHalfI && holeStartI != holeHalfI)
                                //{
                                //    var firstHalf = new List<FillerContour.Part>();
                                //    firstHalf.AddRange(contour._side.Take(sideHalfI - sideStartI));
                                //    firstHalf.Add(new FillerContour.Part(new StraightTrajectory(contour._side[sideHalfI].Trajectory.StartPosition, contour._hole[holeHalfI].Trajectory.StartPosition)));
                                //    firstHalf.AddRange(contour._hole.Take(holeHalfI - holeStartI).Select(i => new FillerContour.Part(i.Trajectory.Invert())).Reverse());
                                //    firstHalf.Add(new FillerContour.Part(new StraightTrajectory(contour._hole[holeStartI].Trajectory.StartPosition, contour._side[sideStartI].Trajectory.StartPosition)));

                                //    var secondHalf = new List<FillerContour.Part>();
                                //    secondHalf.AddRange(contour._side.Skip(sideHalfI));
                                //    secondHalf.Add(new FillerContour.Part(new StraightTrajectory(contour._side[sideStartI].Trajectory.StartPosition, contour._hole[holeStartI].Trajectory.StartPosition)));
                                //    secondHalf.AddRange(contour._hole.Skip(holeHalfI).Select(i => new FillerContour.Part(i.Trajectory.Invert())).Reverse());
                                //    secondHalf.Add(new FillerContour.Part(new StraightTrajectory(contour._hole[holeHalfI].Trajectory.StartPosition, contour._side[sideHalfI].Trajectory.StartPosition)));

                                //    var firstPoints = GetContourPoints(firstHalf, lod, out _);
                                //    var secondPoints = GetContourPoints(secondHalf, lod, out _);
                                //    if (Triangulate(firstPoints, out var firstTriangles) && Triangulate(secondPoints, out var secondTriangles))
                                //    {
                                //        sidePoints = new Vector3[firstPoints.Length + secondPoints.Length];
                                //        Array.Copy(firstPoints, sidePoints, firstPoints.Length);
                                //        Array.Copy(secondPoints, 0, sidePoints, firstPoints.Length, secondPoints.Length);

                                //        triangles = new int[firstTriangles.Length + secondTriangles.Length];
                                //        Array.Copy(firstTriangles, triangles, firstTriangles.Length);
                                //        Array.Copy(secondTriangles, 0, triangles, firstTriangles.Length, secondTriangles.Length);

                                //        for (int i = firstTriangles.Length; i < triangles.Length; i += 1)
                                //            triangles[i] += firstPoints.Length;

                                //        meshParts.Add(MarkupStyleFillerMesh.RawData.SetTop(holePoints, holeTriangles, MaterialType));
                                //    }
                                //}
                            }
                        }

                        meshParts.Add(MarkupFillerMeshData.RawData.SetTop(sidePoints, triangles, MaterialType.Pavement));
                    }

                    yield return new MarkupFillerMeshData(lod, Elevation, meshParts.ToArray());
                }
            }
        }

        public override void GetUIComponents(MarkupFiller filler, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(filler, components, parent, isTemplate);

            if (!isTemplate)
            {
                if (!filler.IsMedian)
                    components.Add(AddCurbSizeProperty(this, parent, false));
                else
                    components.Add(AddMedianCurbSizeProperty(this, parent, true));
            }
        }
        public override int GetUIComponentSortIndex(EditorItem item)
        {
            if (item.name == nameof(CurbSize))
                return CurbSizeIndex;
            else
                return base.GetUIComponentSortIndex(item);
        }

        private static FloatPropertyPanel AddCurbSizeProperty(CurbTriangulationFillerStyle curbStyle, UIComponent parent, bool canCollapse)
        {
            var curbSizeProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(CurbSize));
            curbSizeProperty.Text = Localize.FillerStyle_CurbSize;
            curbSizeProperty.Format = Localize.NumberFormat_Meter;
            curbSizeProperty.UseWheel = true;
            curbSizeProperty.WheelStep = 0.1f;
            curbSizeProperty.WheelTip = Settings.ShowToolTip;
            curbSizeProperty.CheckMin = true;
            curbSizeProperty.MinValue = 0f;
            curbSizeProperty.CheckMax = true;
            curbSizeProperty.MaxValue = 10f;
            curbSizeProperty.CanCollapse = canCollapse;
            curbSizeProperty.Init();
            curbSizeProperty.Value = curbStyle.CurbSize;
            curbSizeProperty.OnValueChanged += (float value) => curbStyle.CurbSize.Value = value;

            return curbSizeProperty;
        }
        private static Vector2PropertyPanel AddMedianCurbSizeProperty(CurbTriangulationFillerStyle curbStyle, UIComponent parent, bool canCollapse)
        {
            var curbSizeProperty = ComponentPool.Get<Vector2PropertyPanel>(parent, nameof(CurbSize));
            curbSizeProperty.Text = Localize.FillerStyle_CurbSize;
            curbSizeProperty.FieldsWidth = 50f;
            curbSizeProperty.SetLabels(Localize.FillerStyle_CurbSizeAbrv, Localize.FillerStyle_CurbSizeMedianAbrv);
            curbSizeProperty.Format = Localize.NumberFormat_Meter;
            curbSizeProperty.UseWheel = true;
            curbSizeProperty.WheelStep = new Vector2(0.1f, 0.1f);
            curbSizeProperty.WheelTip = Settings.ShowToolTip;
            curbSizeProperty.CheckMin = true;
            curbSizeProperty.MinValue = new Vector2(0f, 0f);
            curbSizeProperty.CheckMax = true;
            curbSizeProperty.MaxValue = new Vector2(10f, 10f);
            curbSizeProperty.CanCollapse = canCollapse;
            curbSizeProperty.Init(0, 1);
            curbSizeProperty.Value = new Vector2(curbStyle.CurbSize, curbStyle.MedianCurbSize);
            curbSizeProperty.OnValueChanged += (Vector2 value) =>
            {
                curbStyle.CurbSize.Value = value.x;
                curbStyle.MedianCurbSize.Value = value.y;
            };

            return curbSizeProperty;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            CurbSize.ToXml(config);
            MedianCurbSize.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            CurbSize.FromXml(config, DefaultCurbSize);
            MedianCurbSize.FromXml(config, DefaultCurbSize);
        }
    }
    public class PavementFillerStyle : TriangulationFillerStyle
    {
        public override StyleType Type => StyleType.FillerPavement;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;
        protected override MaterialType MaterialType => MaterialType.Pavement;

        protected override int ColorIndex => 0;
        protected override int WidthIndex => 1;
        protected override int ElevationIndex => 2;
        protected override int CornerRadiusIndex => 3;
        protected override int OffsetIndex => 4;

        public PavementFillerStyle(Color32 color, float width, float lineOffset, float medianOffset, float elevation, float cornerRadius, float medianCornerRadius) : base(color, width, lineOffset, medianOffset, elevation, cornerRadius, medianCornerRadius) { }

        public override FillerStyle CopyStyle() => new PavementFillerStyle(Color, Width, LineOffset, DefaultOffset, Elevation, CornerRadius, DefaultCornerRadius);
    }
    public class GrassFillerStyle : CurbTriangulationFillerStyle
    {
        public override StyleType Type => StyleType.FillerGrass;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;
        protected override MaterialType MaterialType => MaterialType.Grass;

        protected override int ColorIndex => 0;
        protected override int WidthIndex => 1;
        protected override int ElevationIndex => 2;
        protected override int CornerRadiusIndex => 3;
        protected override int CurbSizeIndex => 4;
        protected override int OffsetIndex => 5;

        public GrassFillerStyle(Color32 color, float width, float lineOffset, float medianOffset, float elevation, float cornerRadius, float medianCornerRadius, float curbSize, float medianCurbSize) : base(color, width, lineOffset, medianOffset, elevation, cornerRadius, medianCornerRadius, curbSize, medianCurbSize) { }

        public override FillerStyle CopyStyle() => new GrassFillerStyle(Color, Width, LineOffset, DefaultOffset, Elevation, CornerRadius, DefaultCornerRadius, CurbSize, DefaultCurbSize);
    }
    public class GravelFillerStyle : CurbTriangulationFillerStyle
    {
        public override StyleType Type => StyleType.FillerGravel;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;
        protected override MaterialType MaterialType => MaterialType.Gravel;

        protected override int ColorIndex => 0;
        protected override int WidthIndex => 1;
        protected override int ElevationIndex => 2;
        protected override int CornerRadiusIndex => 3;
        protected override int CurbSizeIndex => 4;
        protected override int OffsetIndex => 5;

        public GravelFillerStyle(Color32 color, float width, float lineOffset, float medianOffset, float elevation, float cornerRadius, float medianCornerRadius, float curbSize, float medianCurbSize) : base(color, width, lineOffset, medianOffset, elevation, cornerRadius, medianCornerRadius, curbSize, medianCurbSize) { }

        public override FillerStyle CopyStyle() => new GravelFillerStyle(Color, Width, LineOffset, DefaultOffset, Elevation, CornerRadius, DefaultCornerRadius, CurbSize, DefaultCurbSize);
    }
    public class RuinedFillerStyle : CurbTriangulationFillerStyle
    {
        public override StyleType Type => StyleType.FillerRuined;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;
        protected override MaterialType MaterialType => MaterialType.Ruined;

        protected override int ColorIndex => 0;
        protected override int WidthIndex => 1;
        protected override int ElevationIndex => 2;
        protected override int CornerRadiusIndex => 3;
        protected override int CurbSizeIndex => 4;
        protected override int OffsetIndex => 5;

        public RuinedFillerStyle(Color32 color, float width, float lineOffset, float medianOffset, float elevation, float cornerRadius, float medianCornerRadius, float curbSize, float medianCurbSize) : base(color, width, lineOffset, medianOffset, elevation, cornerRadius, medianCornerRadius, curbSize, medianCurbSize) { }

        public override FillerStyle CopyStyle() => new RuinedFillerStyle(Color, Width, LineOffset, DefaultOffset, Elevation, CornerRadius, DefaultCornerRadius, CurbSize, DefaultCurbSize);
    }
    public class CliffFillerStyle : CurbTriangulationFillerStyle
    {
        public override StyleType Type => StyleType.FillerCliff;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;
        protected override MaterialType MaterialType => MaterialType.Cliff;

        protected override int ColorIndex => 0;
        protected override int WidthIndex => 1;
        protected override int ElevationIndex => 2;
        protected override int CornerRadiusIndex => 3;
        protected override int CurbSizeIndex => 4;
        protected override int OffsetIndex => 5;

        public CliffFillerStyle(Color32 color, float width, float lineOffset, float medianOffset, float elevation, float cornerRadius, float medianCornerRadius, float curbSize, float medianCurbSize) : base(color, width, lineOffset, medianOffset, elevation, cornerRadius, medianCornerRadius, curbSize, medianCurbSize) { }

        public override FillerStyle CopyStyle() => new CliffFillerStyle(Color, Width, LineOffset, DefaultOffset, Elevation, CornerRadius, DefaultCornerRadius, CurbSize, DefaultCurbSize);
    }
}
