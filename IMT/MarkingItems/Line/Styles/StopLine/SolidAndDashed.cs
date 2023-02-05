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
    public class SolidAndDashedStopLineStyle : StopLineStyle, IStopLine, IDoubleLine, IDashedLine, IEffectStyle
    {
        public override StyleType Type => StyleType.StopLineSolidAndDashed;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;

        public PropertyBoolValue TwoColors { get; }
        public PropertyColorValue SecondColor { get; }
        public PropertyValue<float> Offset { get; }
        public PropertyValue<float> DashLength { get; }
        public PropertyValue<float> SpaceLength { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(TwoColors);
                yield return nameof(Color);
                yield return nameof(SecondColor);
                yield return nameof(Width);
                yield return nameof(Offset);
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
                yield return new StylePropertyDataProvider<bool>(nameof(TwoColors), TwoColors);
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<Color32>(nameof(SecondColor), SecondColor);
                yield return new StylePropertyDataProvider<float>(nameof(Width), Width);
                yield return new StylePropertyDataProvider<float>(nameof(DashLength), DashLength);
                yield return new StylePropertyDataProvider<float>(nameof(SpaceLength), SpaceLength);
                yield return new StylePropertyDataProvider<float>(nameof(Offset), Offset);
            }
        }

        public SolidAndDashedStopLineStyle(Color32 color, Color32 secondColor, bool useSecondColor, float width, Vector2 cracks, Vector2 voids, float texture, float dashLength, float spaceLength, float offset) : base(color, width, cracks, voids, texture)
        {
            TwoColors = GetTwoColorsProperty(useSecondColor);
            SecondColor = GetSecondColorProperty(TwoColors ? secondColor : color);
            Offset = GetOffsetProperty(offset);
            DashLength = GetDashLengthProperty(dashLength);
            SpaceLength = GetSpaceLengthProperty(spaceLength);
        }

        protected override void CalculateImpl(MarkingStopLine stopLine, ITrajectory trajectory, MarkingLOD lod, Action<IStyleData> addData)
        {
            var offsetNormal = ((stopLine.Start.Direction + stopLine.End.Direction) / -2).normalized;
            var solidOffset = offsetNormal * (Width / 2);
            var dashedOffset = offsetNormal * (Width / 2 + 2 * Offset);

            var solidParts = StyleHelper.CalculateSolid(trajectory, lod);
            foreach (var part in solidParts)
            {
                StyleHelper.GetPartParams(trajectory, part, solidOffset, solidOffset, out var startPos, out var endPos, out var dir);
                var data = new DecalData(this, MaterialType.RectangleLines, lod, startPos, endPos, Width, Color);
                addData(data);
            }

            if (CheckDashedLod(lod, Width, DashLength))
            {
                var dashedParts = StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength);
                foreach (var part in dashedParts)
                {
                    StyleHelper.GetPartParams(trajectory, part, dashedOffset, dashedOffset, out var pos, out var dir);
                    var data = new DecalData(this, MaterialType.RectangleLines, lod, pos, dir, DashLength, Width, TwoColors ? SecondColor : Color);
                    addData(data);
                }
            }
        }

        public override StopLineStyle CopyLineStyle() => new SolidAndDashedStopLineStyle(Color, SecondColor, TwoColors, Width, Cracks, Voids, Texture, DashLength, SpaceLength, Offset);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is IDashedLine dashedTarget)
            {
                dashedTarget.DashLength.Value = DashLength;
                dashedTarget.SpaceLength.Value = SpaceLength;
            }

            if (target is IDoubleLine doubleTarget)
            {
                doubleTarget.Offset.Value = Offset;
                doubleTarget.SecondColor.Value = SecondColor;
                doubleTarget.TwoColors.Value = TwoColors;
            }
        }
        public override void GetUIComponents(MarkingStopLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);

            components.Add(AddUseSecondColorProperty(this, parent, true));
            components.Add(AddSecondColorProperty(this, parent, true));
            UseSecondColorChanged(this, parent, TwoColors);

            components.Add(AddLengthProperty(this, parent, false));
            components.Add(AddOffsetProperty(this, parent, false));
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            TwoColors.ToXml(config);
            SecondColor.ToXml(config);
            Offset.ToXml(config);
            DashLength.ToXml(config);
            SpaceLength.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            TwoColors.FromXml(config, false);
            SecondColor.FromXml(config, DefaultColor);
            Offset.FromXml(config, DefaultDoubleOffset);
            DashLength.FromXml(config, DefaultDashLength);
            SpaceLength.FromXml(config, DefaultSpaceLength);
        }
    }
}
