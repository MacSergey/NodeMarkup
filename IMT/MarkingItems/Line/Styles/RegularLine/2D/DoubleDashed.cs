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

namespace IMT.Manager
{
    public class DoubleDashedLineStyle : DashedLineStyle, IRegularLine, IDoubleLine, IDoubleAlignmentLine
    {
        public override StyleType Type => StyleType.LineDoubleDashed;
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
                yield return nameof(Length);
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
                yield return new StylePropertyDataProvider<float>(nameof(DashLength), DashLength);
                yield return new StylePropertyDataProvider<float>(nameof(SpaceLength), SpaceLength);
                yield return new StylePropertyDataProvider<float>(nameof(Offset), Offset);
                yield return new StylePropertyDataProvider<Alignment>(nameof(Alignment), Alignment);
                yield return new StylePropertyDataProvider<float>(nameof(Texture), Texture);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Cracks), Cracks);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Voids), Voids);
            }
        }

        public DoubleDashedLineStyle(Color32 color, Color32 secondColor, bool useSecondColor, float width, Vector2 cracks, Vector2 voids, float texture, float dashLength, float spaceLength, float offset) : base(color, width, cracks, voids, texture, dashLength, spaceLength)
        {
            TwoColors = GetTwoColorsProperty(useSecondColor);
            SecondColor = GetSecondColorProperty(TwoColors ? secondColor : color);
            Offset = GetOffsetProperty(offset);
            Alignment = GetAlignmentProperty(Manager.Alignment.Centre);
        }

        public override RegularLineStyle CopyLineStyle() => new DoubleDashedLineStyle(Color, SecondColor, TwoColors, Width, Cracks, Voids, Texture, DashLength, SpaceLength, Offset);
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

        protected override void CalculateDashes(ITrajectory trajectory, StyleHelper.PartT partT, LineBorders borders, MarkingLOD lod, Action<IStyleData> addData)
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

            StyleHelper.GetPartParams(trajectory, partT, firstOffset, out var firstPos, out var firstDir);
            if (StyleHelper.CheckBorders(borders, firstPos, firstDir, DashLength, Width))
            {
                var data = new DecalData(MaterialType.Dash, lod, firstPos, firstDir, DashLength, Width, Color, DecalData.TextureData.Default, new DecalData.EffectData(this));
                addData(data);
            }

            StyleHelper.GetPartParams(trajectory, partT, secondOffset, out var secondPos, out var secondDir);
            if (StyleHelper.CheckBorders(borders, secondPos, secondDir, DashLength, Width))
            {
                var data = new DecalData(MaterialType.Dash, lod, secondPos, secondDir, DashLength, Width, TwoColors ? SecondColor : Color, DecalData.TextureData.Default, new DecalData.EffectData(this));
                addData(data);
            }
        }

        protected override void GetUIComponents(MarkingRegularLine line, EditorProvider provider)
        {
            base.GetUIComponents(line, provider);

            provider.AddProperty(new PropertyInfo<BoolPropertyPanel>(this, nameof(TwoColors), AdditionalCategory, AddUseSecondColorProperty));
            provider.AddProperty(new PropertyInfo<ColorAdvancedPropertyPanel>(this, nameof(SecondColor), AdditionalCategory, AddSecondColorProperty, RefreshSecondColorProperty));
            provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Offset), MainCategory, AddOffsetProperty));
            if(!provider.isTemplate)
            {
                provider.AddProperty(new PropertyInfo<LineAlignmentPropertyPanel>(this, nameof(Alignment), MainCategory, AddAlignmentProperty));
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
            SecondColor.FromXml(config, DefaultMarkingColor);
            Offset.FromXml(config, DefaultDoubleOffset);
            Alignment.FromXml(config, Manager.Alignment.Centre);
            if (invert)
                Alignment.Value = Alignment.Value.Invert();
        }
    }
}
