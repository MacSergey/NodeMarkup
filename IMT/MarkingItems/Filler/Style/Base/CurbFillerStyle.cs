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

        public CurbFillerStyle(ThemeHelper.IThemeData pavementTheme, Vector2 offset, float elevation, Vector2 cornerRadius, Vector2 curbSize) : base(pavementTheme, offset, elevation, cornerRadius)
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
                if (!GetSideTexture(out var curbTexture, out var curbColor))
                    return;

                foreach (var contour in contours)
                {
                    var roundedContour = contour.SetCornerRadius(LineCornerRadius, MedianCornerRadius);
                    if (!Triangulate(roundedContour, lod, out var points, out var groups, out var triangles))
                        continue;

                    var datas = new List<FillerMeshData.RawData>()
                    {
                        FillerMeshData.RawData.GetTop(points, triangles, curbColor, curbTexture),
                        FillerMeshData.RawData.GetSide(groups, points, curbColor, curbTexture),
                    };
                    addData(new FillerMeshData(lod, Elevation, datas.ToArray()));
                }

                if (!GetCenterTexture(out var centerTexture, out var centerColor))
                    return;

                foreach (var contour in contours)
                {
                    var lineCurb = Mathf.Max(0.01f, LineCurbSize);
                    var medianCurb = MedianOffset > 0 ? Mathf.Max(0.01f, MedianCurbSize) : MedianCurbSize;
                    var topContours = contour.SetOffset(lineCurb, medianCurb);
                    if (topContours.Count == 0)
                        continue;

                    var lineCornerRadius = Mathf.Max(0f, LineCornerRadius - lineCurb);
                    var medianCornerRadius = Mathf.Max(0f, MedianCornerRadius - medianCurb);

                    foreach (var topContour in topContours)
                    {
                        var roundedTop = topContour.SetCornerRadius(lineCornerRadius, medianCornerRadius);
                        var trajectories = roundedTop.Select(c => c.trajectory.Elevate(Elevation)).ToArray();
                        var centerDatas = DecalData.GetData(DecalData.DecalType.FillerIsland, lod, trajectories, SplitParams, centerColor, centerTexture, DecalData.EffectData.Default);
                        foreach (var data in centerDatas)
                        {
                            addData(data);
                        }
                    }
                }
            }
        }

        protected abstract bool GetCenterTexture(out DecalData.TextureData textureData, out Color color);

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
            curbSizeProperty.Label = Localize.FillerStyle_CurbSize;
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
            curbSizeProperty.Label = Localize.FillerStyle_CurbSize;
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
