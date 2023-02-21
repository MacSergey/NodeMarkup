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
    public abstract class CurbFillerStyle : MeshFillerStyle
    {
        public PropertyVector2Value CurbSize { get; }
        public float LineCurbSize => CurbSize.Value.x;
        public float MedianCurbSize => CurbSize.Value.y;

        public CurbFillerStyle(Vector2 offset, float elevation, Vector2 cornerRadius, Vector2 curbSize) : base(offset, elevation, cornerRadius)
        {
            CurbSize = new PropertyVector2Value(StyleChanged, curbSize, "CS", "MCS");
        }

        public override void CopyTo(BaseFillerStyle target)
        {
            base.CopyTo(target);
            if (target is CurbFillerStyle curbTarget)
            {
                curbTarget.CurbSize.Value = CurbSize;
            }
        }

        protected override void CalculateImpl(MarkingFiller filler, ContourGroup contours, MarkingLOD lod, Action<IStyleData> addData)
        {
            if ((SupportLOD & lod) == 0)
                return;

            if (LineCurbSize == 0f && MedianCurbSize == 0f)
                base.CalculateImpl(filler, contours, lod, addData);
            else
            {
                foreach (var contour in contours)
                {
                    var roundedContour = contour.SetCornerRadius(LineCornerRadius, MedianCornerRadius);
                    if (Triangulate(roundedContour, lod, out var points, out var groups, out var triangles))
                    {
                        var curbTexture = GetCurbTexture();

                        var datas = new List<FillerMeshData.RawData>()
                        {
                            FillerMeshData.RawData.GetTop(points, triangles, curbTexture),
                            FillerMeshData.RawData.GetSide(groups, points, curbTexture),
                        };

                        var topContours = contour.SetOffset(LineCurbSize, MedianCurbSize);
                        if (topContours.Count > 0)
                        {
                            var lineCornerRadius = Mathf.Max(0f, LineCornerRadius - LineCurbSize);
                            var medianCornerRadius = Mathf.Max(0f, MedianCornerRadius - MedianCurbSize);
                            var topTexture = GetTopTexture();

                            foreach (var topContour in topContours)
                            {
                                var roundedTop = topContour.SetCornerRadius(lineCornerRadius, medianCornerRadius);
                                if (Triangulate(roundedTop, lod, out var topPoints, out _, out var topTriangles))
                                {
                                    topPoints = topPoints.Select(p => p += new Vector3(0f, 0.03f, 0f)).ToArray();
                                    datas.Add(FillerMeshData.RawData.GetTop(topPoints, topTriangles, topTexture));
                                }
                            }
                        }

                        var renderData = new FillerMeshData(lod, Elevation, datas.ToArray());
                        addData(renderData);
                    }
                }
            }
        }

        protected virtual FillerMeshData.TextureData GetCurbTexture() => GetSideTexture();

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
            curbSizeProperty.Value = LineCurbSize;
            curbSizeProperty.OnValueChanged += (float value) => CurbSize.Value = new Vector2(value, value);
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
            curbSizeProperty.Value = CurbSize;
            curbSizeProperty.OnValueChanged += (Vector2 value) => CurbSize.Value = value;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            CurbSize.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            CurbSize.FromXml(config, DefaultCurbSize);
        }
    }
}
