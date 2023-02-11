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
    public abstract class RegularLine3DStyle : RegularLineStyle, IWidthStyle, I3DLine
    {
        protected abstract MaterialType MaterialType { get; }
        public PropertyValue<float> Elevation { get; }

        public override bool CanOverlap => true;

        public RegularLine3DStyle(float width, float elevation) : base(default, width)
        {
            Elevation = GetElevationProperty(elevation);
        }
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is I3DLine line3DTarget)
                line3DTarget.Elevation.Value = Elevation;
        }

        protected override void CalculateImpl(MarkingRegularLine line, ITrajectory trajectory, MarkingLOD lod, Action<IStyleData> addData)
        {
            if (trajectory is CombinedTrajectory combined)
            {
                var data = new IStyleData[combined.Count];
                for (var i = 0; i < data.Length; i += 1)
                    data[i] = new MarkingLineMeshData(lod, combined[i], Width, Elevation, MaterialType.Pavement);

                addData(new RenderGroupData(lod, MarkingLODType.Mesh, data));
            }
            else
                addData(new MarkingLineMeshData(lod, trajectory, Width, Elevation, MaterialType.Pavement));
        }

        protected override void GetUIComponents(MarkingRegularLine line, EditorProvider provider)
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

    public class PavementLineStyle : RegularLine3DStyle
    {
        public override StyleType Type { get; } = StyleType.LinePavement;
        public override MarkingLOD SupportLOD => MarkingLOD.NoLOD;
        protected override MaterialType MaterialType => MaterialType.Pavement;

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Width);
                yield return nameof(Elevation);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<float>(nameof(Width), Width);
                yield return new StylePropertyDataProvider<float>(nameof(Elevation), Elevation);
            }
        }

        public PavementLineStyle(float width, float elevation) : base(width, elevation) { }

        public override RegularLineStyle CopyLineStyle() => new PavementLineStyle(Width, Elevation);
    }
}
