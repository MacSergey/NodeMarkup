using ColossalFramework.UI;
using IMT.API;
using IMT.UI;
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
    public class ParallelSolidLinesCrosswalkStyle : LinedCrosswalkStyle, ICrosswalkStyle, IEffectStyle
    {
        public override StyleType Type => StyleType.CrosswalkParallelSolidLines;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Width);
                yield return nameof(LineWidth);
                yield return nameof(Offset);
                yield return nameof(Texture);
                yield return nameof(Cracks);
                yield return nameof(Voids);
#if DEBUG
                yield return nameof(RenderOnly);
                yield return nameof(Start);
                yield return nameof(End);
                yield return nameof(StartBorder);
                yield return nameof(EndBorder);
#endif
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<float>(nameof(Width), Width);
                yield return new StylePropertyDataProvider<float>(nameof(LineWidth), LineWidth);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetBefore), OffsetBefore);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetAfter), OffsetAfter);
            }
        }

        public ParallelSolidLinesCrosswalkStyle(Color32 color, float width, Vector2 cracks, Vector2 voids, float texture, float offsetBefore, float offsetAfter, float lineWidth) : base(color, width, cracks, voids, texture, offsetBefore, offsetAfter, lineWidth)
        { }

        public override CrosswalkStyle CopyStyle() => new ParallelSolidLinesCrosswalkStyle(Color, Width, Cracks, Voids, Texture, OffsetBefore, OffsetAfter, LineWidth);

        protected override void CalculateImpl(MarkingCrosswalk crosswalk, MarkingLOD lod, Action<IStyleData> addData)
        {
            var middleOffset = GetVisibleWidth(crosswalk) * 0.5f + OffsetBefore;
            var deltaOffset = (Width - LineWidth) * 0.5f / Mathf.Sin(crosswalk.CornerAndNormalAngle);
            var firstTrajectory = crosswalk.GetTrajectory(middleOffset - deltaOffset);
            var secondTrajectory = crosswalk.GetTrajectory(middleOffset + deltaOffset);


            var firstParts = StyleHelper.CalculateSolid(firstTrajectory, lod);
            foreach (var part in firstParts)
            {
                StyleHelper.GetPartParams(firstTrajectory, part, Vector3.zero, Vector3.zero, out var startPos, out var endPos, out var dir);
                var data = new DecalData(this, MaterialType.Dash, lod, startPos, endPos, LineWidth, Color);
                addData(data);
            }

            var secondParts = StyleHelper.CalculateSolid(secondTrajectory, lod);
            foreach (var part in secondParts)
            {
                StyleHelper.GetPartParams(secondTrajectory, part, Vector3.zero, Vector3.zero, out var startPos, out var endPos, out var dir);
                var data = new DecalData(this, MaterialType.Dash, lod, startPos, endPos, LineWidth, Color);
                addData(data);
            }
        }
    }
}
