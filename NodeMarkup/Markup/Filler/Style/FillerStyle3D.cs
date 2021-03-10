using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Utils;
using Poly2Tri;
using Poly2Tri.Triangulation.Polygon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public abstract class Filler3DStyle : FillerStyle
    {
        public Filler3DStyle(Color32 color, float width, float medianOffset) : base(color, width, medianOffset) { }
    }
    public abstract class TriangulationFillerStyle : Filler3DStyle
    {
        protected abstract MaterialType MaterialType { get; }

        public float MinAngle => /*Settings.ApproximationMinAngle; */10f;
        public float MinLength => /*Settings.ApproximationMinLength;*/2f;
        public float MaxLength => /*Settings.ApproximationMaxLength;*/10f;
        public PropertyValue<float> Elevation { get; }

        public TriangulationFillerStyle(Color32 color, float width, float medianOffset, float elevation) : base(color, width, medianOffset)
        {
            Elevation = GetElevationProperty(elevation);
        }

        public override void CopyTo(FillerStyle target)
        {
            base.CopyTo(target);
            if (target is TriangulationFillerStyle triangulationTarget)
                triangulationTarget.Elevation.Value = Elevation;
        }

        public override IStyleData Calculate(MarkupFiller filler, MarkupLOD lod)
        {
            var contour = filler.IsMedian ? SetMedianOffset(filler) : filler.Contour.Trajectories.ToArray();

            if (NeedReverse(contour))
                contour = contour.Select(t => t.Invert()).Reverse().ToArray();

            var pointsGroups = contour.Select(t => StyleHelper.CalculateSolid(t, MinAngle, MinLength, MaxLength, lod, (tr) => GetPoint(tr)).ToArray()).ToArray();
            var points = pointsGroups.SelectMany(g => g).ToArray();
            var polygon = new Polygon(points.Select(p => new PolygonPoint(p.x, p.z)));
            P2T.Triangulate(polygon);
            var triangles = polygon.Triangles.SelectMany(t => t.Points.Select(p => polygon.IndexOf(p))).ToArray();

            return new MarkupStylePolygonMesh(filler.Markup.Height, Elevation, pointsGroups.Select(g => g.Length).ToArray(), points, triangles, MaterialType);

            static IEnumerable<Vector3> GetPoint(ITrajectory trajectory)
            {
                yield return trajectory.StartPosition;
            }
        }
        private bool NeedReverse(ITrajectory[] contour)
        {
            var isClockWise = 0;
            for (var i = 0; i < contour.Length; i += 1)
                isClockWise += (Vector3.Cross(-contour[i].Direction, contour[(i + 1) % contour.Length].Direction).y < 0) ? 1 : -1;

            return isClockWise < 0;
        }

        public override void GetUIComponents(MarkupFiller filler, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(filler, components, parent, isTemplate);
            components.Add(AddElevationProperty(this, parent));
        }
        private static FloatPropertyPanel AddElevationProperty(TriangulationFillerStyle triangulationStyle, UIComponent parent)
        {
            var elevationProperty = parent.AddUIComponent<FloatPropertyPanel>();
            elevationProperty.Text = Localize.FillerStyle_Elevation;
            elevationProperty.UseWheel = true;
            elevationProperty.WheelStep = 0.1f;
            elevationProperty.CheckMin = true;
            elevationProperty.MinValue = 0f;
            elevationProperty.CheckMax = true;
            elevationProperty.MaxValue = 10f;
            elevationProperty.Init();
            elevationProperty.Value = triangulationStyle.Elevation;
            elevationProperty.OnValueChanged += (float value) => triangulationStyle.Elevation.Value = value;

            return elevationProperty;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(Elevation.ToXml());
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Elevation.FromXml(config, DefaultElevation);
        }
    }
    public class PavementFillerStyle : TriangulationFillerStyle
    {
        public override StyleType Type => StyleType.FillerPavement;
        protected override MaterialType MaterialType => MaterialType.Pavement;

        public PavementFillerStyle(Color32 color, float width, float medianOffset, float elevation) : base(color, width, medianOffset, elevation) { }

        public override FillerStyle CopyStyle() => new PavementFillerStyle(Color, Width, MedianOffset, Elevation);
    }
    public class GrassFillerStyle : TriangulationFillerStyle
    {
        public override StyleType Type => StyleType.FillerGrass;
        protected override MaterialType MaterialType => MaterialType.Grass;

        public GrassFillerStyle(Color32 color, float width, float medianOffset, float elevation) : base(color, width, medianOffset, elevation) { }

        public override FillerStyle CopyStyle() => new GrassFillerStyle(Color, Width, MedianOffset, Elevation);
    }
}
