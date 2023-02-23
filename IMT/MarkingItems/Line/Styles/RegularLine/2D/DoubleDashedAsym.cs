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
    public class DoubleDashedAsymLineStyle : RegularLineStyle, IRegularLine, IDashedLine, IDoubleLine, IDoubleAlignmentLine, IAsymLine, IEffectStyle
    {
        public override StyleType Type => StyleType.LineDoubleDashedAsym;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;
        public bool KeepColor => true;

        public PropertyValue<float> DashLengthA { get; }
        public PropertyValue<float> DashLengthB { get; }
        public PropertyValue<float> DashLength
        {
            get
            {
                PropertyStructValue<float> dashLength = null;
                dashLength = new PropertyStructValue<float>(() => DashLengthValue = dashLength.Value, DashLengthValue);

                return dashLength;
            }
        }
        public PropertyValue<float> SpaceLength { get; }

        public PropertyBoolValue TwoColors { get; }
        public PropertyColorValue SecondColor { get; }
        public new PropertyValue<float> Offset { get; }
        public PropertyEnumValue<Alignment> Alignment { get; }

        public PropertyBoolValue Invert { get; }

        private float DashLengthValue
        {
            get => Mathf.Max(DashLengthA, DashLengthB);
            set
            {
                DashLengthA.Value = value;
                DashLengthB.Value = value;
            }
        }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(TwoColors);
                yield return nameof(Color);
                yield return nameof(SecondColor);
                yield return nameof(Width);
                yield return nameof(SpaceLength);
                yield return nameof(DashLength);
                yield return nameof(Offset);
                yield return nameof(Alignment);
                yield return nameof(Invert);
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
                yield return new StylePropertyDataProvider<bool>(nameof(Invert), Invert);
                yield return new StylePropertyDataProvider<float>(nameof(Texture), Texture);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Cracks), Cracks);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Voids), Voids);
            }
        }

        public DoubleDashedAsymLineStyle(Color32 color, Color32 secondColor, bool useSecondColor, float width, Vector2 cracks, Vector2 voids, float texture, float dashLengthA, float dashLengthB, float spaceLength, float offset) : base(color, width, cracks, voids, texture)
        {
            DashLengthA = new PropertyStructValue<float>("DLA", StyleChanged, dashLengthA);
            DashLengthB = new PropertyStructValue<float>("DLB", StyleChanged, dashLengthB);
            SpaceLength = GetSpaceLengthProperty(spaceLength);

            Offset = GetOffsetProperty(offset);
            Alignment = GetAlignmentProperty(Manager.Alignment.Centre);
            TwoColors = GetTwoColorsProperty(useSecondColor);
            SecondColor = GetSecondColorProperty(TwoColors ? secondColor : color);

            Invert = GetInvertProperty(false);
        }

        public override RegularLineStyle CopyLineStyle() => new DoubleDashedAsymLineStyle(Color, SecondColor, TwoColors, Width, Cracks, Voids, Texture, DashLengthB, DashLengthA, DashLengthB, Offset);

        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is IDashedLine dashedTarget)
            {
                dashedTarget.DashLength.Value = DashLengthValue;
                dashedTarget.SpaceLength.Value = SpaceLength;
            }
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
            if (CheckDashedLod(lod, Width, DashLengthValue))
            {
                var borders = line.Borders;
                var parts = StyleHelper.CalculateDashed(trajectory, DashLengthValue, SpaceLength);
                foreach (var part in parts)
                {
                    var offsetA = Alignment.Value switch
                    {
                        Manager.Alignment.Left => 2 * Offset,
                        Manager.Alignment.Centre => Offset,
                        Manager.Alignment.Right => 0,
                        _ => 0,
                    };
                    var offsetB = Alignment.Value switch
                    {
                        Manager.Alignment.Left => 0,
                        Manager.Alignment.Centre => -Offset,
                        Manager.Alignment.Right => -2 * Offset,
                        _ => 0,
                    };

                    StyleHelper.GetPartParams(trajectory, part, Invert ? -offsetA : offsetA, out var firstPos, out var firstDir);
                    if (StyleHelper.CheckBorders(borders, firstPos, firstDir, DashLengthA, Width))
                    {
                        var data = new DecalData(MaterialType.Dash, lod, firstPos, firstDir, DashLengthA, Width, Color, DecalData.TextureData.Default, new DecalData.EffectData(this));
                        addData(data);
                    }

                    StyleHelper.GetPartParams(trajectory, part, Invert ? -offsetB : offsetB, out var secondPos, out var secondDir);
                    if (StyleHelper.CheckBorders(borders, secondPos, secondDir, DashLengthB, Width))
                    {
                        var data = new DecalData(MaterialType.Dash, lod, secondPos, secondDir, DashLengthB, Width, TwoColors ? SecondColor : Color, DecalData.TextureData.Default, new DecalData.EffectData(this));
                        addData(data);
                    }
                }
            }
        }

        protected override void GetUIComponents(MarkingRegularLine line, EditorProvider provider)
        {
            base.GetUIComponents(line, provider);

            provider.AddProperty(new PropertyInfo<FloatRangePropertyPanel>(this, nameof(DashLength), MainCategory, AddDashLengthProperty));
            provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(SpaceLength), MainCategory, AddSpaceLengthProperty));
            provider.AddProperty(new PropertyInfo<BoolListPropertyPanel>(this, nameof(TwoColors), AdditionalCategory, AddUseSecondColorProperty));
            provider.AddProperty(new PropertyInfo<ColorAdvancedPropertyPanel>(this, nameof(SecondColor), AdditionalCategory, AddSecondColorProperty, RefreshSecondColorProperty));
            provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Offset), MainCategory, AddOffsetProperty));
            if (!provider.isTemplate)
            {
                provider.AddProperty(new PropertyInfo<LineAlignmentPropertyPanel>(this, nameof(Alignment), MainCategory, AddAlignmentProperty));
                provider.AddProperty(new PropertyInfo<ButtonPanel>(this, nameof(Invert), MainCategory, AddInvertProperty));
            }
        }
        protected void AddDashLengthProperty(FloatRangePropertyPanel dashLengthProperty, EditorProvider provider)
        {
            dashLengthProperty.Text = Localize.StyleOption_DashedLength;
            dashLengthProperty.Format = Localize.NumberFormat_Meter;
            dashLengthProperty.UseWheel = true;
            dashLengthProperty.WheelStep = 0.1f;
            dashLengthProperty.WheelTip = Settings.ShowToolTip;
            dashLengthProperty.CheckMin = true;
            dashLengthProperty.MinValue = 0.1f;
            dashLengthProperty.FieldWidth = 70f;
            dashLengthProperty.Init();
            dashLengthProperty.SetValues(DashLengthA, DashLengthB);
            dashLengthProperty.OnValueChanged += (valueA, valueB) =>
            {
                DashLengthA.Value = valueA;
                DashLengthB.Value = valueB;
            };
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            DashLengthA.ToXml(config);
            DashLengthB.ToXml(config);
            SpaceLength.ToXml(config);
            TwoColors.ToXml(config);
            SecondColor.ToXml(config);
            Offset.ToXml(config);
            Alignment.ToXml(config);
            Invert.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            DashLengthA.FromXml(config, DefaultDashLength);
            DashLengthB.FromXml(config, DefaultDashLength * 2f);
            SpaceLength.FromXml(config, DefaultSpaceLength);
            TwoColors.FromXml(config, false);
            SecondColor.FromXml(config, DefaultMarkingColor);
            Offset.FromXml(config, DefaultDoubleOffset);
            Alignment.FromXml(config, Manager.Alignment.Centre);
            Invert.FromXml(config, false);
            Invert.Value ^= map.Invert ^ invert ^ typeChanged;

            if (invert)
                Alignment.Value = Alignment.Value.Invert();
        }
    }
}
