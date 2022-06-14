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
        public override IEnumerable<IStyleData> Calculate(MarkupFiller filler, List<List<FillerContour.Part>> contours, MarkupLOD lod)
        {
            foreach (var contour in contours)
            {
                var points = GetContourPoints(contour, lod, out var groups);
                if (Triangulate(points, out var triangles))
                {
                    SplitTriangles(points, triangles, 2f, out var topPoints, out var topTriangles);

                    yield return new MarkupStyleFillerMesh(Elevation, MarkupStyleFillerMesh.RawData.SetSide(groups, points, MaterialType.Pavement), MarkupStyleFillerMesh.RawData.SetTop(topPoints, topTriangles, MaterialType));
#if DEBUG
                    if ((Settings.ShowFillerTriangulation & 2) != 0)
                        yield return GetTriangulationLines(topPoints, topTriangles, UnityEngine.Color.red, MaterialType.RectangleFillers);
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
        private void SplitTriangles(Vector3[] points, int[] triangles, float maxLenght, out Vector3[] pointsResult, out int[] trianglesResult)
        {
            var tempPoints = new List<Vector3>(points);
            var tempTriang = new List<int>(triangles);
            maxLenght = maxLenght * maxLenght;

            var pointDic = new Dictionary<Vector3, int>();

            var i = 0;
            while(i < tempTriang.Count && tempPoints.Count < 1000 && tempTriang.Count < 3000)
            {
                var index1 = tempTriang[i + 0];
                var index2 = tempTriang[i + 1];
                var index3 = tempTriang[i + 2];

                var point1 = tempPoints[index1];
                var point2 = tempPoints[index2];
                var point3 = tempPoints[index3];

                var dist12 = index1 >= points.Length || index2 >= points.Length || (Math.Abs(index2 - index1) != 0 && Math.Abs(index2 - index1) != points.Length - 1) ? (point1 - point2).sqrMagnitude : 0f;
                var dist23 = index2 >= points.Length || index3 >= points.Length || (Math.Abs(index3 - index2) != 0 && Math.Abs(index3 - index2) != points.Length - 1) ? (point2 - point3).sqrMagnitude : 0f;
                var dist31 = index3 >= points.Length || index1 >= points.Length || (Math.Abs(index1 - index3) != 0 && Math.Abs(index1 - index3) != points.Length - 1) ? (point3 - point1).sqrMagnitude : 0f;

                if (dist12 > maxLenght && dist12 > dist23 && dist12 > dist31)
                    ProcessSplitTriangle(ref i, index1, index2, index3, tempPoints, tempTriang, pointDic);
                else if (dist23 > maxLenght && dist23 > dist31 && dist23 > dist12)
                    ProcessSplitTriangle(ref i, index2, index3, index1, tempPoints, tempTriang, pointDic);
                else if (dist31 > maxLenght && dist31 > dist12 && dist31 > dist23)
                    ProcessSplitTriangle(ref i, index3, index1, index2, tempPoints, tempTriang, pointDic);
                else
                    i += 3;
            }

            pointsResult = tempPoints.ToArray();
            trianglesResult = tempTriang.ToArray();
        }
        private void ProcessSplitTriangle(ref int i, int index1, int index2, int index3, List<Vector3> points, List<int> triangles, Dictionary<Vector3, int> pointDic)
        {
            var newPoint = (points[index1] + points[index2]) * 0.5f;

            if((newPoint - points[index3]).sqrMagnitude < 0.25f)
            {
                i += 3;
                return;
            }
            if (!pointDic.TryGetValue(newPoint, out var indexNew))
            {
                indexNew = points.Count;
                points.Add(newPoint);
                pointDic.Add(newPoint, indexNew);
            }

            triangles[i + 0] = index1;
            triangles[i + 1] = indexNew;
            triangles[i + 2] = index3;

            triangles.Add(index3);
            triangles.Add(indexNew);
            triangles.Add(index2);
        }
#if DEBUG
        private IStyleData GetTriangulationLines(Vector3[] points, int[] triangles, Color32 color, MaterialType materialType)
        {
            var dashes = new List<MarkupStylePart>();

            for (int i = 0; i < triangles.Length; i += 3)
            {
                var point1 = points[triangles[i + 0]];
                var point2 = points[triangles[i + 1]];
                var point3 = points[triangles[i + 2]];

                dashes.Add(new MarkupStylePart(point1, point2, 0.05f, color, materialType));
                dashes.Add(new MarkupStylePart(point2, point3, 0.05f, color, materialType));
                dashes.Add(new MarkupStylePart(point3, point1, 0.05f, color, materialType));
            }

            return new MarkupStyleParts(dashes);
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
            components.Add(AddElevationProperty(this, parent));

            if (!isTemplate)
            {
                components.Add(AddCornerRadiusProperty(this, parent));
                if (filler.IsMedian)
                    components.Add(AddMedianCornerRadiusProperty(this, parent));
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

        private static FloatPropertyPanel AddElevationProperty(TriangulationFillerStyle triangulationStyle, UIComponent parent)
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
            elevationProperty.Init();
            elevationProperty.Value = triangulationStyle.Elevation;
            elevationProperty.OnValueChanged += (float value) => triangulationStyle.Elevation.Value = value;

            return elevationProperty;
        }
        private static FloatPropertyPanel AddCornerRadiusProperty(TriangulationFillerStyle triangulationStyle, UIComponent parent)
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
            cornerRadiusProperty.Init();
            cornerRadiusProperty.Value = triangulationStyle.CornerRadius;
            cornerRadiusProperty.OnValueChanged += (float value) => triangulationStyle.CornerRadius.Value = value;

            return cornerRadiusProperty;
        }
        private static FloatPropertyPanel AddMedianCornerRadiusProperty(TriangulationFillerStyle triangulationStyle, UIComponent parent)
        {
            var cornerRadiusProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(MedianCornerRadius));
            cornerRadiusProperty.Text = Localize.FillerStyle_MedianCornerRadius;
            cornerRadiusProperty.Format = Localize.NumberFormat_Meter;
            cornerRadiusProperty.UseWheel = true;
            cornerRadiusProperty.WheelStep = 0.1f;
            cornerRadiusProperty.WheelTip = Settings.ShowToolTip;
            cornerRadiusProperty.CheckMin = true;
            cornerRadiusProperty.MinValue = 0f;
            cornerRadiusProperty.CheckMax = true;
            cornerRadiusProperty.MaxValue = 10f;
            cornerRadiusProperty.Init();
            cornerRadiusProperty.Value = triangulationStyle.MedianCornerRadius;
            cornerRadiusProperty.OnValueChanged += (float value) => triangulationStyle.MedianCornerRadius.Value = value;

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
        public override LodDictionaryArray<IStyleData> Calculate(MarkupFiller filler)
        {
            if (CurbSize == 0f && MedianCurbSize == 0f)
                return base.Calculate(filler);
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

                var data = new LodDictionaryArray<IStyleData>();

                foreach (var lod in EnumExtension.GetEnumValues<MarkupLOD>())
                    data[lod] = Calculate(filler, contourDatas, lod).ToArray();

                return data;
            }
        }
        private IEnumerable<IStyleData> Calculate(MarkupFiller filler, CounterData[] contours, MarkupLOD lod)
        {
            if (lod == MarkupLOD.LOD1)
            {
                foreach (var data in base.Calculate(filler, contours.Select(c => c._side).ToList(), lod))
                    yield return data;
            }
            else
            {
                foreach (var contour in contours)
                {
                    var meshParts = new List<MarkupStyleFillerMesh.RawData>();

                    var sidePoints = GetContourPoints(contour._side, lod, out var sideGroups);
                    if (Triangulate(sidePoints, out var triangles))
                    {
                        meshParts.Add(MarkupStyleFillerMesh.RawData.SetSide(sideGroups, sidePoints, MaterialType.Pavement));
                       
                        if (contour._hole != null)
                        {
                            var holePoints = GetContourPoints(contour._hole, lod, out var holeGroups);
                            if(Triangulate(holePoints, out var holeTriangles))
                            {
                                var sideStartI = 0;
                                var sideHalfI = contour._side.Count / 2;

                                var holeStartI = 0;
                                var holeHalfI = 0;
                                float startMinDist = float.MaxValue;
                                float halfMinDist = float.MaxValue;

                                for (int i = 0; i < contour._hole.Count; i += 1)
                                {
                                    var dist = (contour._side[sideStartI].Trajectory.StartPosition - contour._hole[i].Trajectory.StartPosition).sqrMagnitude;
                                    if (dist < startMinDist)
                                    {
                                        holeStartI = i;
                                        startMinDist = dist;
                                    }
                                    dist = (contour._side[sideHalfI].Trajectory.StartPosition - contour._hole[i].Trajectory.StartPosition).sqrMagnitude;
                                    if (dist < halfMinDist)
                                    {
                                        holeHalfI = i;
                                        halfMinDist = dist;
                                    }
                                }

                                if (sideStartI != sideHalfI && holeStartI != holeHalfI)
                                {
                                    var firstHalf = new List<FillerContour.Part>();
                                    firstHalf.AddRange(contour._side.Take(sideHalfI - sideStartI));
                                    firstHalf.Add(new FillerContour.Part(new StraightTrajectory(contour._side[sideHalfI].Trajectory.StartPosition, contour._hole[holeHalfI].Trajectory.StartPosition)));
                                    firstHalf.AddRange(contour._hole.Take(holeHalfI - holeStartI).Select(i => new FillerContour.Part(i.Trajectory.Invert())).Reverse());
                                    firstHalf.Add(new FillerContour.Part(new StraightTrajectory(contour._hole[holeStartI].Trajectory.StartPosition, contour._side[sideStartI].Trajectory.StartPosition)));

                                    var secondHalf = new List<FillerContour.Part>();
                                    secondHalf.AddRange(contour._side.Skip(sideHalfI));
                                    secondHalf.Add(new FillerContour.Part(new StraightTrajectory(contour._side[sideStartI].Trajectory.StartPosition, contour._hole[holeStartI].Trajectory.StartPosition)));
                                    secondHalf.AddRange(contour._hole.Skip(holeHalfI).Select(i => new FillerContour.Part(i.Trajectory.Invert())).Reverse());
                                    secondHalf.Add(new FillerContour.Part(new StraightTrajectory(contour._hole[holeHalfI].Trajectory.StartPosition, contour._side[sideHalfI].Trajectory.StartPosition)));

                                    var firstPoints = GetContourPoints(firstHalf, lod, out _);
                                    var secondPoints = GetContourPoints(secondHalf, lod, out _);
                                    if (Triangulate(firstPoints, out var firstTriangles) && Triangulate(secondPoints, out var secondTriangles))
                                    {
                                        sidePoints = new Vector3[firstPoints.Length + secondPoints.Length];
                                        Array.Copy(firstPoints, sidePoints, firstPoints.Length);
                                        Array.Copy(secondPoints, 0, sidePoints, firstPoints.Length, secondPoints.Length);

                                        triangles = new int[firstTriangles.Length + secondTriangles.Length];
                                        Array.Copy(firstTriangles, triangles, firstTriangles.Length);
                                        Array.Copy(secondTriangles, 0, triangles, firstTriangles.Length, secondTriangles.Length);

                                        for (int i = firstTriangles.Length; i < triangles.Length; i += 1)
                                            triangles[i] += firstPoints.Length;

                                        meshParts.Add(MarkupStyleFillerMesh.RawData.SetTop(holePoints, holeTriangles, MaterialType));
                                    }
                                }
                            }
                        }

                        meshParts.Add(MarkupStyleFillerMesh.RawData.SetTop(sidePoints, triangles, MaterialType.Pavement));
                    }

                    yield return new MarkupStyleFillerMesh(Elevation, meshParts.ToArray());
                }
            }
        }

        public override void GetUIComponents(MarkupFiller filler, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(filler, components, parent, isTemplate);

            if (!isTemplate)
            {
                components.Add(AddCurbSizeProperty(this, parent));
                if (filler.IsMedian)
                    components.Add(AddMedianCurbSizeProperty(this, parent));
            }
        }
        private static FloatPropertyPanel AddCurbSizeProperty(CurbTriangulationFillerStyle curbStyle, UIComponent parent)
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
            curbSizeProperty.Init();
            curbSizeProperty.Value = curbStyle.CurbSize;
            curbSizeProperty.OnValueChanged += (float value) => curbStyle.CurbSize.Value = value;

            return curbSizeProperty;
        }
        private static FloatPropertyPanel AddMedianCurbSizeProperty(CurbTriangulationFillerStyle curbStyle, UIComponent parent)
        {
            var curbSizeProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(MedianCurbSize));
            curbSizeProperty.Text = Localize.FillerStyle_MedianCurbSize;
            curbSizeProperty.Format = Localize.NumberFormat_Meter;
            curbSizeProperty.UseWheel = true;
            curbSizeProperty.WheelStep = 0.1f;
            curbSizeProperty.WheelTip = Settings.ShowToolTip;
            curbSizeProperty.CheckMin = true;
            curbSizeProperty.MinValue = 0f;
            curbSizeProperty.CheckMax = true;
            curbSizeProperty.MaxValue = 10f;
            curbSizeProperty.Init();
            curbSizeProperty.Value = curbStyle.MedianCurbSize;
            curbSizeProperty.OnValueChanged += (float value) => curbStyle.MedianCurbSize.Value = value;

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
        protected override MaterialType MaterialType => MaterialType.Pavement;

        public PavementFillerStyle(Color32 color, float width, float lineOffset, float medianOffset, float elevation, float cornerRadius, float medianCornerRadius) : base(color, width, lineOffset, medianOffset, elevation, cornerRadius, medianCornerRadius) { }

        public override FillerStyle CopyStyle() => new PavementFillerStyle(Color, Width, LineOffset, DefaultOffset, Elevation, CornerRadius, DefaultCornerRadius);
    }
    public class GrassFillerStyle : CurbTriangulationFillerStyle
    {
        public override StyleType Type => StyleType.FillerGrass;
        protected override MaterialType MaterialType => MaterialType.Grass;

        public GrassFillerStyle(Color32 color, float width, float lineOffset, float medianOffset, float elevation, float cornerRadius, float medianCornerRadius, float curbSize, float medianCurbSize) : base(color, width, lineOffset, medianOffset, elevation, cornerRadius, medianCornerRadius, curbSize, medianCurbSize) { }

        public override FillerStyle CopyStyle() => new GrassFillerStyle(Color, Width, LineOffset, DefaultOffset, Elevation, CornerRadius, DefaultCornerRadius, CurbSize, DefaultCurbSize);
    }
    public class GravelFillerStyle : CurbTriangulationFillerStyle
    {
        public override StyleType Type => StyleType.FillerGravel;
        protected override MaterialType MaterialType => MaterialType.Gravel;

        public GravelFillerStyle(Color32 color, float width, float lineOffset, float medianOffset, float elevation, float cornerRadius, float medianCornerRadius, float curbSize, float medianCurbSize) : base(color, width, lineOffset, medianOffset, elevation, cornerRadius, medianCornerRadius, curbSize, medianCurbSize) { }

        public override FillerStyle CopyStyle() => new GravelFillerStyle(Color, Width, LineOffset, DefaultOffset, Elevation, CornerRadius, DefaultCornerRadius, CurbSize, DefaultCurbSize);
    }
    public class RuinedFillerStyle : CurbTriangulationFillerStyle
    {
        public override StyleType Type => StyleType.FillerRuined;
        protected override MaterialType MaterialType => MaterialType.Ruined;

        public RuinedFillerStyle(Color32 color, float width, float lineOffset, float medianOffset, float elevation, float cornerRadius, float medianCornerRadius, float curbSize, float medianCurbSize) : base(color, width, lineOffset, medianOffset, elevation, cornerRadius, medianCornerRadius, curbSize, medianCurbSize) { }

        public override FillerStyle CopyStyle() => new RuinedFillerStyle(Color, Width, LineOffset, DefaultOffset, Elevation, CornerRadius, DefaultCornerRadius, CurbSize, DefaultCurbSize);
    }
    public class CliffFillerStyle : CurbTriangulationFillerStyle
    {
        public override StyleType Type => StyleType.FillerCliff;
        protected override MaterialType MaterialType => MaterialType.Cliff;

        public CliffFillerStyle(Color32 color, float width, float lineOffset, float medianOffset, float elevation, float cornerRadius, float medianCornerRadius, float curbSize, float medianCurbSize) : base(color, width, lineOffset, medianOffset, elevation, cornerRadius, medianCornerRadius, curbSize, medianCurbSize) { }

        public override FillerStyle CopyStyle() => new CliffFillerStyle(Color, Width, LineOffset, DefaultOffset, Elevation, CornerRadius, DefaultCornerRadius, CurbSize, DefaultCurbSize);
    }
}
