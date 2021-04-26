using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Utilities;
using System.Collections.Generic;
using System.Xml.Linq;

namespace NodeMarkup.Manager
{
    public abstract class Line3DStyle : RegularLineStyle, IWidthStyle
    {
        protected abstract MaterialType MaterialType { get; }
        public PropertyValue<float> Elevation { get; }

        public Line3DStyle(float width, float elevation) : base(default, width)
        {
            Elevation = GetElevationProperty(elevation);
        }
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is Line3DStyle pavementTarget)
                pavementTarget.Elevation.Value = Elevation;
        }

        public override IStyleData Calculate(MarkupLine line, ITrajectory trajectory, MarkupLOD lod) => new MarkupStyleLineMesh(trajectory, Width, Elevation, MaterialType.Pavement);

        public override void GetUIComponents(MarkupRegularLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);
            components.Add(AddElevationProperty(this, parent));
        }
        private static FloatPropertyPanel AddElevationProperty(Line3DStyle triangulationStyle, UIComponent parent)
        {
            var elevationProperty = parent.AddUIComponent<FloatPropertyPanel>();
            elevationProperty.Text = Localize.LineStyle_Elevation;
            elevationProperty.UseWheel = true;
            elevationProperty.WheelStep = 0.1f;
            elevationProperty.CheckMin = true;
            elevationProperty.MinValue = 0f;
            elevationProperty.CheckMax = true;
            elevationProperty.MaxValue = 1f;
            elevationProperty.Init();
            elevationProperty.Value = triangulationStyle.Elevation;
            elevationProperty.OnValueChanged += (float value) => triangulationStyle.Elevation.Value = value;

            return elevationProperty;
        }

        public override XElement ToXml()
        {
            var config = BaseToXml();
            Width.ToXml(config);
            Elevation.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, Utilities.ObjectsMap map, bool invert)
        {
            Width.FromXml(config, Default3DWidth);
            Elevation.FromXml(config, Default3DHeigth);
        }
    }

    public class PavementLineStyle : Line3DStyle
    {
        public override StyleType Type { get; } = StyleType.LinePavement;
        protected override MaterialType MaterialType => MaterialType.Pavement;

        public PavementLineStyle(float width, float elevation) : base(width, elevation) { }

        public override RegularLineStyle CopyLineStyle() => new PavementLineStyle(Width, Elevation);
    }
}
