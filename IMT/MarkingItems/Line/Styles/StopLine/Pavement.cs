using ColossalFramework.UI;
using IMT.API;
using IMT.UI.Editors;
using IMT.Utilities;
using IMT.Utilities.API;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public abstract class StopLine3DStyle : StopLineStyle, IWidthStyle, I3DLine
    {
        protected abstract MaterialType MaterialType { get; }
        public PropertyValue<float> Elevation { get; }

        public StopLine3DStyle(float width, float elevation) : base(default, width)
        {
            Elevation = GetElevationProperty(elevation);
        }
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is I3DLine line3DTarget)
                line3DTarget.Elevation.Value = Elevation;
        }

        protected override void CalculateImpl(MarkingStopLine line, ITrajectory trajectory, MarkingLOD lod, Action<IStyleData> addData)
        {
            addData(new MarkingLineMeshData(lod, trajectory, Width, Elevation, MaterialType.Pavement));
        }

        protected override void GetUIComponents(MarkingStopLine line, EditorProvider provider)
        {
            base.GetUIComponents(line, provider);
            provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Elevation), MainCategory, AddElevationProperty));
        }

        public override XElement ToXml()
        {
            var config = BaseToXml();
            Width.ToXml(config);
            Elevation.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            Width.FromXml(config, Default3DWidth);
            Elevation.FromXml(config, Default3DHeigth);
        }
    }
    public class PavementStopLineStyle : StopLine3DStyle
    {
        public override StyleType Type { get; } = StyleType.StopLinePavement;
        public override MarkingLOD SupportLOD => MarkingLOD.NoLOD;
        protected override MaterialType MaterialType => MaterialType.Pavement;

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Width);
                yield return nameof(Elevation);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<float>(nameof(Width), Width);
                yield return new StylePropertyDataProvider<float>(nameof(Elevation), Elevation);
            }
        }

        public PavementStopLineStyle(float width, float elevation) : base(width, elevation) { }

        public override StopLineStyle CopyLineStyle() => new PavementStopLineStyle(Width, Elevation);
    }
}
