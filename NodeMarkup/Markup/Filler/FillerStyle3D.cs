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

        //public PropertyValue<float> MinAngle { get; }
        //public PropertyValue<float> MinLength { get; }
        //public PropertyValue<float> MaxLength { get; }
        public float MinAngle => 10f;
        public float MinLength => 2f;
        public float MaxLength => 10f;
        public PropertyValue<float> Elevation { get; }

        public TriangulationFillerStyle(Color32 color, float width, float medianOffset, float height) : base(color, width, medianOffset)
        {
            //MinAngle = new PropertyValue<float>(StyleChanged, minAngle);
            //MinLength = new PropertyValue<float>(StyleChanged, minLength);
            //MaxLength = new PropertyValue<float>(StyleChanged, maxLength);
            Elevation = GetElevationProperty(height);
        }

        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is TriangulationFillerStyle triangulationTarget)
                triangulationTarget.Elevation.Value = Elevation;
        }

        protected override IStyleData GetStyleData(ILineTrajectory[] trajectories, Rect _, float height)
        {
            var pointsGroups = trajectories.Select(t => StyleHelper.CalculateSolid(t, MinAngle, MinLength, MaxLength, (tr) => GetPoint(tr)).ToArray()).ToArray();

            var points = pointsGroups.SelectMany(g => g).ToArray();
            var polygon = new Polygon(points.Select(p => new PolygonPoint(p.x, p.z)));
            P2T.Triangulate(polygon);
            var triangles = polygon.Triangles.SelectMany(t => t.Points.Select(p => polygon.IndexOf(p))).ToList();

            var isClockWise = Vector3.Cross(trajectories[0].EndDirection, trajectories[1].StartDirection).y < 0;
            return new MarkupStylePolygonMesh(height, Elevation, isClockWise, pointsGroups.Select(g => g.Length).ToArray(), points.ToArray(), triangles.ToArray(), MaterialType);
        }

        static IEnumerable<Vector3> GetPoint(ILineTrajectory trajectory)
        {
            yield return trajectory.StartPosition;
        }

        public override List<EditorItem> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            components.Add(AddHeightProperty(this, parent, onHover, onLeave));
            //components.Add(AddMinAngleProperty(this, parent, onHover, onLeave));
            //components.Add(AddMinLengthProperty(this, parent, onHover, onLeave));
            //components.Add(AddMaxLengthProperty(this, parent, onHover, onLeave));
            return components;
        }
        private static FloatPropertyPanel AddHeightProperty(TriangulationFillerStyle triangulationStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var heightProperty = parent.AddUIComponent<FloatPropertyPanel>();
            heightProperty.Text = Localize.FillerStyle_Elevation;
            heightProperty.UseWheel = true;
            heightProperty.WheelStep = 0.1f;
            heightProperty.CheckMin = true;
            heightProperty.MinValue = 0f;
            heightProperty.CheckMax = true;
            heightProperty.MaxValue = 1f;
            heightProperty.Init();
            heightProperty.Value = triangulationStyle.Elevation;
            heightProperty.OnValueChanged += (float value) => triangulationStyle.Elevation.Value = value;
            AddOnHoverLeave(heightProperty, onHover, onLeave);
            return heightProperty;
        }
        //private static FloatPropertyPanel AddMinAngleProperty(TriangulationFillerStyle triangulationStyle, UIComponent parent, Action onHover, Action onLeave)
        //{
        //    var minAngleProperty = parent.AddUIComponent<FloatPropertyPanel>();
        //    minAngleProperty.Text = "Min angle";
        //    minAngleProperty.UseWheel = true;
        //    minAngleProperty.WheelStep = 1f;
        //    minAngleProperty.CheckMin = true;
        //    minAngleProperty.MinValue = 5f;
        //    minAngleProperty.CheckMax = true;
        //    minAngleProperty.MaxValue = 90f;
        //    minAngleProperty.Init();
        //    minAngleProperty.Value = triangulationStyle.MinAngle;
        //    minAngleProperty.OnValueChanged += (float value) => triangulationStyle.MinAngle.Value = value;
        //    AddOnHoverLeave(minAngleProperty, onHover, onLeave);
        //    return minAngleProperty;
        //}
        //private static FloatPropertyPanel AddMinLengthProperty(TriangulationFillerStyle triangulationStyle, UIComponent parent, Action onHover, Action onLeave)
        //{
        //    var minAngleProperty = parent.AddUIComponent<FloatPropertyPanel>();
        //    minAngleProperty.Text = "Min length";
        //    minAngleProperty.UseWheel = true;
        //    minAngleProperty.WheelStep = 0.1f;
        //    minAngleProperty.CheckMin = true;
        //    minAngleProperty.MinValue = 1f;
        //    minAngleProperty.Init();
        //    minAngleProperty.Value = triangulationStyle.MinLength;
        //    minAngleProperty.OnValueChanged += (float value) => triangulationStyle.MinLength.Value = value;
        //    AddOnHoverLeave(minAngleProperty, onHover, onLeave);
        //    return minAngleProperty;
        //}
        //private static FloatPropertyPanel AddMaxLengthProperty(TriangulationFillerStyle triangulationStyle, UIComponent parent, Action onHover, Action onLeave)
        //{
        //    var minAngleProperty = parent.AddUIComponent<FloatPropertyPanel>();
        //    minAngleProperty.Text = "Max length";
        //    minAngleProperty.UseWheel = true;
        //    minAngleProperty.WheelStep = 0.1f;
        //    minAngleProperty.CheckMin = true;
        //    minAngleProperty.MinValue = 1f;
        //    minAngleProperty.Init();
        //    minAngleProperty.Value = triangulationStyle.MaxLength;
        //    minAngleProperty.OnValueChanged += (float value) => triangulationStyle.MaxLength.Value = value;
        //    AddOnHoverLeave(minAngleProperty, onHover, onLeave);
        //    return minAngleProperty;
        //}
        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(Elevation.ToXml());
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Elevation.FromXml(config, DefaultHeight);
        }
    }
    public class PavementFillerStyle : TriangulationFillerStyle
    {
        public override StyleType Type => StyleType.FillerPavement;
        protected override MaterialType MaterialType => MaterialType.Pavement;

        public PavementFillerStyle(Color32 color, float width, float medianOffset, float height) : base(color, width, medianOffset, height) { }

        public override FillerStyle CopyFillerStyle() => new PavementFillerStyle(Color, Width, MedianOffset, Elevation);
    }
    public class GrassFillerStyle : TriangulationFillerStyle
    {
        public override StyleType Type => StyleType.FillerGrass;
        protected override MaterialType MaterialType => MaterialType.Grass;

        public GrassFillerStyle(Color32 color, float width, float medianOffset, float height) : base(color, width, medianOffset, height) { }

        public override FillerStyle CopyFillerStyle() => new GrassFillerStyle(Color, Width, MedianOffset, Elevation);
    }
}
