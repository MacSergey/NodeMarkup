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
    public class SolidCrosswalkStyle : CustomCrosswalkStyle, ICrosswalkStyle, IEffectStyle
    {
        public override StyleType Type => StyleType.CrosswalkSolid;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Width);
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
                yield return new StylePropertyDataProvider<float>(nameof(OffsetBefore), OffsetBefore);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetAfter), OffsetAfter);
                yield return new StylePropertyDataProvider<float>(nameof(Texture), Texture);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Cracks), Cracks);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Voids), Voids);
            }
        }

        public SolidCrosswalkStyle(Color32 color, float width, Vector2 cracks, Vector2 voids, float texture, float offsetBefore, float offsetAfter) : base(color, width, cracks, voids, texture, offsetBefore, offsetAfter) { }

        public override CrosswalkStyle CopyStyle() => new SolidCrosswalkStyle(Color, Width, Cracks, Voids, Texture, OffsetBefore, OffsetAfter);
        protected override float GetVisibleWidth(MarkingCrosswalk crosswalk) => Width / Mathf.Sin(crosswalk.CornerAndNormalAngle);

        protected override void CalculateImpl(MarkingCrosswalk crosswalk, MarkingLOD lod, Action<IStyleData> addData)
        {
            var width = GetAbsoluteWidth(Width, crosswalk);
            var offset = width * 0.5f + OffsetBefore;

            if (GetContour(crosswalk, offset, width, out var contour))
            {
                var trajectories = contour.Select(c => c.trajectory).ToArray();
                foreach (var data in DecalData.GetData(this, lod, trajectories, StyleHelper.SplitParams.Default, Color))
                {
                    addData(data);
                }
            }
        }
    }
}
