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
    public class ParallelDashedLinesCrosswalkStyle : LinedCrosswalkStyle, ICrosswalkStyle, IDashedLine, IEffectStyle
    {
        public override StyleType Type => StyleType.CrosswalkParallelDashedLines;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;

        public PropertyValue<float> DashLength { get; }
        public PropertyValue<float> SpaceLength { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Width);
                yield return nameof(LineWidth);
                yield return nameof(Length);
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
                yield return new StylePropertyDataProvider<float>(nameof(DashLength), DashLength);
                yield return new StylePropertyDataProvider<float>(nameof(SpaceLength), SpaceLength);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetBefore), OffsetBefore);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetAfter), OffsetAfter);
            }
        }

        public ParallelDashedLinesCrosswalkStyle(Color32 color, float width, Vector2 cracks, Vector2 voids, float texture, float offsetBefore, float offsetAfter, float lineWidth, float dashLength, float spaceLength) : base(color, width, cracks, voids, texture, offsetBefore, offsetAfter, lineWidth)
        {
            DashLength = GetDashLengthProperty(dashLength);
            SpaceLength = GetSpaceLengthProperty(spaceLength);
        }

        public override CrosswalkStyle CopyStyle() => new ParallelDashedLinesCrosswalkStyle(Color, Width, Cracks, Voids, Texture, OffsetBefore, OffsetAfter, LineWidth, DashLength, SpaceLength);

        public override void CopyTo(CrosswalkStyle target)
        {
            base.CopyTo(target);
            if (target is IDashedLine dashedTarget)
            {
                dashedTarget.DashLength.Value = DashLength;
                dashedTarget.SpaceLength.Value = SpaceLength;
            }
        }
        public override void GetUIComponents(MarkingCrosswalk crosswalk, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(crosswalk, components, parent, isTemplate);
            components.Add(AddLengthProperty(this, parent, false));
        }

        protected override void CalculateImpl(MarkingCrosswalk crosswalk, MarkingLOD lod, Action<IStyleData> addData)
        {
            var middleOffset = GetVisibleWidth(crosswalk) / 2 + OffsetBefore;
            var deltaOffset = (Width - LineWidth) / 2 / Mathf.Sin(crosswalk.CornerAndNormalAngle);
            var firstTrajectory = crosswalk.GetTrajectory(middleOffset - deltaOffset);
            var secondTrajectory = crosswalk.GetTrajectory(middleOffset + deltaOffset);

            var firstParts = StyleHelper.CalculateDashed(firstTrajectory, DashLength, SpaceLength);
            foreach (var part in firstParts)
            {
                StyleHelper.GetPartParams(firstTrajectory, part, 0f, out var pos, out var dir);
                var data = new DecalData(this, MaterialType.Dash, lod, pos, dir, DashLength, LineWidth, Color);
                addData(data);
            }

            var secondParts = StyleHelper.CalculateDashed(secondTrajectory, DashLength, SpaceLength);
            foreach (var part in secondParts)
            {
                StyleHelper.GetPartParams(secondTrajectory, part, 0f, out var pos, out var dir);
                var data = new DecalData(this, MaterialType.Dash, lod, pos, dir, DashLength, LineWidth, Color);
                addData(data);
            }
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            DashLength.ToXml(config);
            SpaceLength.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            DashLength.FromXml(config, DefaultDashLength);
            SpaceLength.FromXml(config, DefaultSpaceLength);
        }
    }
}
