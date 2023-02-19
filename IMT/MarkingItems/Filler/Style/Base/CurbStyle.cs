using ColossalFramework.UI;
using IMT.API;
using IMT.UI.Editors;
using IMT.Utilities;
using IMT.Utilities.API;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using UnityEngine;
using static IMT.Manager.StyleHelper;

namespace IMT.Manager
{
    public abstract class CurbTriangulationFillerStyle : TriangulationFillerStyle
    {
        public struct CounterData
        {
            public Contour side;
            public Contour center;
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
        public override void Calculate(MarkingFiller filler, Action<IStyleData> addData)
        {
            if (CurbSize == 0f && MedianCurbSize == 0f)
            {
                base.Calculate(filler, addData);
            }
            else
            {
                var originalContour = filler.Contour.Edges;

                var contourDatas = originalContour.SetOffset(LineOffset, MedianOffset).Select(i => new CounterData() { side = i }).ToArray();

                for (int i = 0; i < contourDatas.Length; i += 1)
                {
                    if (CurbSize > 0 || MedianCurbSize > 0)
                    {
                        var centerGroup = contourDatas[i].side.SetOffset(CurbSize, MedianCurbSize);
                        if (centerGroup.Count > 0) {
                            contourDatas[i].center = centerGroup[0];
                            var lineCornerRadius = Mathf.Max(0f, CornerRadius - CurbSize);
                            var medianCornerRadius = Mathf.Max(0f, MedianCornerRadius - MedianCurbSize);
                            contourDatas[i].center = contourDatas[i].center.SetCornerRadius(lineCornerRadius, medianCornerRadius);
                        }
                    }
                    contourDatas[i].side = contourDatas[i].side.SetCornerRadius(CornerRadius, MedianCornerRadius);
                }

                foreach (var lod in EnumExtension.GetEnumValues<MarkingLOD>())
                {
                    Calculate(filler, contourDatas, lod, addData);
                }
            }
        }
        private void Calculate(MarkingFiller filler, CounterData[] contours, MarkingLOD lod, Action<IStyleData> addData)
        {
            if ((SupportLOD & lod) == 0)
                return;

            if (lod == MarkingLOD.LOD1)
            {
                var sideContours = new ContourGroup(contours.Select(c => c.side));
                base.CalculateImpl(filler, sideContours, lod, addData);
            }
            else
            {
                foreach (var contour in contours)
                {
                    var meshParts = new List<MarkingFillerMeshData.RawData>();

                    var sidePoints = GetContourPoints(contour.side, lod, out var sideGroups);
                    if (Triangulate(sidePoints, out var triangles))
                    {
                        meshParts.Add(MarkingFillerMeshData.RawData.GetSide(sideGroups, sidePoints, MaterialType.Pavement));

                        if (contour.center != null)
                        {
                            var holePoints = GetContourPoints(contour.center, lod, out var holeGroups);
                            if (Triangulate(holePoints, out var holeTriangles))
                            {
                                holePoints = holePoints.Select(p => p += new Vector3(0f, 0.03f, 0f)).ToArray();
                                meshParts.Add(MarkingFillerMeshData.RawData.GetTop(holePoints, holeTriangles, MaterialType));
                                //var sideStartI = 0;
                                //var sideHalfI = contour._side.Count * 0.5f;

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

                                //        meshParts.Add(MarkingStyleFillerMesh.RawData.SetTop(holePoints, holeTriangles, MaterialType));
                                //    }
                                //}
                            }
                        }

                        meshParts.Add(MarkingFillerMeshData.RawData.GetTop(sidePoints, triangles, MaterialType.Pavement));
                    }

                    addData(new MarkingFillerMeshData(lod, Elevation, meshParts.ToArray()));
                }
            }
        }

        protected override void GetUIComponents(MarkingFiller filler, EditorProvider provider)
        {
            base.GetUIComponents(filler, provider);

            if (!provider.isTemplate)
            {
                if (!filler.IsMedian)
                    provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(CurbSize), MainCategory, AddCurbSizeProperty));
                else
                    provider.AddProperty(new PropertyInfo<Vector2PropertyPanel>(this, nameof(CurbSize), MainCategory, AddMedianCurbSizeProperty));
            }
        }

        private void AddCurbSizeProperty(FloatPropertyPanel curbSizeProperty, EditorProvider provider)
        {
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
            curbSizeProperty.Value = CurbSize.Value;
            curbSizeProperty.OnValueChanged += (float value) => CurbSize.Value = value;
        }
        private void AddMedianCurbSizeProperty(Vector2PropertyPanel curbSizeProperty, EditorProvider provider)
        {
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
            curbSizeProperty.Init(0, 1);
            curbSizeProperty.Value = new Vector2(CurbSize, MedianCurbSize);
            curbSizeProperty.OnValueChanged += (Vector2 value) =>
            {
                CurbSize.Value = value.x;
                MedianCurbSize.Value = value.y;
            };
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            CurbSize.ToXml(config);
            MedianCurbSize.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            CurbSize.FromXml(config, DefaultCurbSize);
            MedianCurbSize.FromXml(config, DefaultCurbSize);
        }
    }
}
