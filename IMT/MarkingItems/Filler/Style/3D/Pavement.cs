using ColossalFramework.UI;
using IMT.API;
using IMT.Utilities;
using IMT.Utilities.API;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public class PavementFillerStyle : TriangulationFillerStyle
    {
        public override StyleType Type => StyleType.FillerPavement;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;
        protected override MaterialType MaterialType => MaterialType.Pavement;

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Elevation);
                yield return nameof(CornerRadius);
                yield return nameof(Offset);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<float>(nameof(Elevation), Elevation);
                yield return new StylePropertyDataProvider<float>(nameof(CornerRadius), CornerRadius);
                yield return new StylePropertyDataProvider<float>(nameof(MedianCornerRadius), MedianCornerRadius);
                yield return new StylePropertyDataProvider<float>(nameof(LineOffset), LineOffset);
                yield return new StylePropertyDataProvider<float>(nameof(MedianOffset), MedianOffset);
            }
        }

        public PavementFillerStyle(Color32 color, float width, float lineOffset, float medianOffset, float elevation, float cornerRadius, float medianCornerRadius) : base(color, width, lineOffset, medianOffset, elevation, cornerRadius, medianCornerRadius) { }

        public override FillerStyle CopyStyle() => new PavementFillerStyle(Color, Width, LineOffset, DefaultOffset, Elevation, CornerRadius, DefaultCornerRadius);
    }
}
