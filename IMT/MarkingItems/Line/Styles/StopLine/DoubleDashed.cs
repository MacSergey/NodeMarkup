using ColossalFramework.UI;
using IMT.API;
using IMT.UI;
using IMT.UI.Editors;
using IMT.Utilities;
using IMT.Utilities.API;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using static IMT.Manager.StyleHelper;

namespace IMT.Manager
{
    public class DoubleDashedStopLineStyle : DashedStopLineStyle, IStopLine, IDoubleLine, IEffectStyle
    {
        public override StyleType Type { get; } = StyleType.StopLineDoubleDashed;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;

        public PropertyBoolValue TwoColors { get; }
        public PropertyColorValue SecondColor { get; }
        public PropertyValue<float> Offset { get; }

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

        public DoubleDashedStopLineStyle(Color32 color, Color32 secondColor, bool useSecondColor, float width, Vector2 cracks, Vector2 voids, float texture, float dashLength, float spaceLength, float offset) : base(color, width, cracks, voids, texture, dashLength, spaceLength)
        {
            TwoColors = GetTwoColorsProperty(useSecondColor);
            SecondColor = GetSecondColorProperty(TwoColors ? secondColor : color);
            Offset = GetOffsetProperty(offset);
        }
        public override StopLineStyle CopyLineStyle() => new DoubleDashedStopLineStyle(Color, SecondColor, TwoColors, Width, Cracks, Voids, Texture, DashLength, SpaceLength, Offset);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is IDoubleLine doubleTarget)
            {
                doubleTarget.Offset.Value = Offset;
                doubleTarget.SecondColor.Value = SecondColor;
                doubleTarget.TwoColors.Value = TwoColors;
            }
        }

        protected override void CalculateImpl(MarkingStopLine stopLine, ITrajectory trajectory, MarkingLOD lod, Action<IStyleData> addData)
        {
            if (CheckDashedLod(lod, Width, DashLength))
            {
                var offsetNormal = ((stopLine.Start.Direction + stopLine.End.Direction) / -2).normalized;
                var offsetLeft = offsetNormal * (Width / 2);
                var offsetRight = offsetNormal * (Width / 2 + 2 * Offset);

                var parts = StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength);
                foreach (var part in parts)
                {
                    StyleHelper.GetPartParams(trajectory, part, offsetLeft, offsetLeft, out var leftPos, out var leftDir);
                    var left = new DecalData(this, MaterialType.Dash, lod, leftPos, leftDir, DashLength, Width, Color);

                    StyleHelper.GetPartParams(trajectory, part, offsetRight, offsetRight, out var rightPos, out var rightDir);
                    var right = new DecalData(this, MaterialType.Dash, lod, rightPos, rightDir, DashLength, Width, Color);

                    addData(left);
                    addData(right);
                }
            }
        }

        protected override void GetUIComponents(MarkingStopLine line, EditorProvider provider)
        {
            base.GetUIComponents(line, provider);

            provider.AddProperty(new PropertyInfo<BoolListPropertyPanel>(this, nameof(TwoColors), MainCategory, AddUseSecondColorProperty));
            provider.AddProperty(new PropertyInfo<ColorAdvancedPropertyPanel>(this, nameof(SecondColor), MainCategory, AddSecondColorProperty));
            provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Offset), MainCategory, AddOffsetProperty));

            //UseSecondColorChanged(this, parent, TwoColors);
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            TwoColors.ToXml(config);
            SecondColor.ToXml(config);
            Offset.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            TwoColors.FromXml(config, false);
            SecondColor.FromXml(config, DefaultColor);
            Offset.FromXml(config, DefaultDoubleOffset);
        }
    }
}
