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
    public class DoubleSolidLineStyle : SolidLineStyle, IRegularLine, IDoubleLine, IDoubleAlignmentLine, IEffectStyle
    {
        public override StyleType Type => StyleType.LineDoubleSolid;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;

        public PropertyBoolValue TwoColors { get; }
        public PropertyColorValue SecondColor { get; }
        public new PropertyValue<float> Offset { get; }
        public PropertyEnumValue<Alignment> Alignment { get; }

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
                yield return nameof(Alignment);
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
                yield return new StylePropertyDataProvider<float>(nameof(Offset), Offset);
                yield return new StylePropertyDataProvider<Alignment>(nameof(Alignment), Alignment);
            }
        }

        public DoubleSolidLineStyle(Color32 color, Color32 secondColor, bool useSecondColor, float width, Vector2 cracks, Vector2 voids, float texture, float offset) : base(color, width, cracks, voids, texture)
        {
            TwoColors = GetTwoColorsProperty(useSecondColor);
            SecondColor = GetSecondColorProperty(TwoColors ? secondColor : color);
            Offset = GetOffsetProperty(offset);
            Alignment = GetAlignmentProperty(Manager.Alignment.Centre);
        }

        public override RegularLineStyle CopyLineStyle() => new DoubleSolidLineStyle(Color, SecondColor, TwoColors, Width, Cracks, Voids, Texture, Offset);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is IDoubleLine doubleTarget)
            {
                doubleTarget.Offset.Value = Offset;
                doubleTarget.SecondColor.Value = SecondColor;
                doubleTarget.TwoColors.Value = TwoColors;
            }
            if (target is IDoubleAlignmentLine doubleAlignmentTarget)
                doubleAlignmentTarget.Alignment.Value = Alignment;
        }

        protected override void CalculateImpl(MarkingRegularLine line, ITrajectory trajectory, MarkingLOD lod, Action<IStyleData> addData)
        {
            var borders = line.Borders;
            var parts = StyleHelper.CalculateSolid(trajectory, lod);
            foreach (var part in parts)
            {
                var firstOffset = Alignment.Value switch
                {
                    Manager.Alignment.Left => 2 * Offset,
                    Manager.Alignment.Centre => Offset,
                    Manager.Alignment.Right => 0,
                    _ => 0,
                };
                var secondOffset = Alignment.Value switch
                {
                    Manager.Alignment.Left => 0,
                    Manager.Alignment.Centre => -Offset,
                    Manager.Alignment.Right => -2 * Offset,
                    _ => 0,
                };

                StyleHelper.GetPartParams(trajectory, part, firstOffset, out var firstStartPos, out var firstEndPos, out var firstDir);
                if (StyleHelper.CheckBorders(borders, ref firstStartPos, ref firstEndPos, firstDir, Width))
                {
                    var data = new DecalData(this, MaterialType.Dash, lod, firstStartPos, firstEndPos, Width, Color);
                    addData(data);
                }

                StyleHelper.GetPartParams(trajectory, part, secondOffset, out var secondStartPos, out var secondEndPos, out var secondDir);
                if (StyleHelper.CheckBorders(borders, ref secondStartPos, ref secondEndPos, secondDir, Width))
                {
                    var data = new DecalData(this, MaterialType.Dash, lod, secondStartPos, secondEndPos, Width, TwoColors ? SecondColor : Color);
                    addData(data);
                }
            }
        }

        protected override void GetUIComponents(MarkingRegularLine line, EditorProvider provider)
        {
            base.GetUIComponents(line, provider);

            provider.AddProperty(new PropertyInfo<BoolListPropertyPanel>(this, nameof(TwoColors), true, AddUseSecondColorProperty));
            provider.AddProperty(new PropertyInfo<ColorAdvancedPropertyPanel>(this, nameof(SecondColor), true, AddSecondColorProperty));
            //UseSecondColorChanged(this, parent, TwoColors);

            provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Offset), false, AddOffsetProperty));
            if (!provider.isTemplate)
            {
                provider.AddProperty(new PropertyInfo<LineAlignmentPropertyPanel>(this, nameof(Alignment), false, AddAlignmentProperty));
            }
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            TwoColors.ToXml(config);
            SecondColor.ToXml(config);
            Offset.ToXml(config);
            Alignment.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            TwoColors.FromXml(config, false);
            SecondColor.FromXml(config, DefaultColor);
            Offset.FromXml(config, DefaultDoubleOffset);
            Alignment.FromXml(config, Manager.Alignment.Centre);

            if (invert)
                Alignment.Value = Alignment.Value.Invert();
        }
    }
}
