using ColossalFramework.UI;
using IMT.API;
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
    public class SolidLineStyle : RegularLineStyle, IRegularLine, IEffectStyle
    {
        public override StyleType Type => StyleType.LineSolid;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Width);
                yield return nameof(Texture);
                yield return nameof(Cracks);
                yield return nameof(Voids);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<float>(nameof(Width), Width);
            }
        }

        public SolidLineStyle(Color32 color, float width, Vector2 cracks, Vector2 voids, float texture) : base(color, width, cracks, voids, texture) { }

        public override RegularLineStyle CopyLineStyle() => new SolidLineStyle(Color, Width, Cracks, Voids, Texture);

        protected override void CalculateImpl(MarkingRegularLine line, ITrajectory trajectory, MarkingLOD lod, Action<IStyleData> addData)
        {
            var borders = line.Borders;
            var parts = StyleHelper.CalculateSolid(trajectory, lod);
            foreach (var part in parts)
            {
                StyleHelper.GetPartParams(trajectory, part, 0f, out var startPos, out var endPos, out var dir);
                if(StyleHelper.CheckBorders(borders, ref startPos, ref endPos, dir, Width))
                {
                    var data = new DecalData(this, MaterialType.RectangleLines, lod, startPos, endPos, Width, Color);
                    addData(data);
                }
            }
        }
    }
}
