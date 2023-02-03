using ColossalFramework.UI;
using IMT.API;
using IMT.MarkingItems.Crosswalk.Styles.Base;
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
    public class SolidCrosswalkStyle : CustomCrosswalkStyle, ICrosswalkStyle, ITexture
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
                yield return nameof(Scratches);
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
                yield return new StylePropertyDataProvider<float>(nameof(OffsetBefore), OffsetBefore);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetAfter), OffsetAfter);
            }
        }

        public SolidCrosswalkStyle(Color32 color, float width, Vector2 scratches, Vector2 voids, float offsetBefore, float offsetAfter) : base(color, width, scratches, voids, offsetBefore, offsetAfter) { }

        public override CrosswalkStyle CopyStyle() => new SolidCrosswalkStyle(Color, Width, Scratches, Voids, OffsetBefore, OffsetAfter);
        protected override float GetVisibleWidth(MarkingCrosswalk crosswalk) => Width / Mathf.Sin(crosswalk.CornerAndNormalAngle);

        protected override void CalculateImpl(MarkingCrosswalk crosswalk, MarkingLOD lod, Action<IStyleData> addData)
        {
            var offset = Width * 0.5f + OffsetBefore;

            if (GetContour(crosswalk, offset, Width, out var contour))
            {
                var trajectories = contour.Select(c => c.trajectory).ToArray();
                foreach (var data in DecalData.GetData(lod, trajectories, StyleHelper.MinAngle, StyleHelper.MinLength, StyleHelper.MaxLength, Color, Vector2.one, ScratchDensity, ScratchTiling, VoidDensity, VoidTiling))
                {
                    addData(data);
                }
            }
        }
    }
}
