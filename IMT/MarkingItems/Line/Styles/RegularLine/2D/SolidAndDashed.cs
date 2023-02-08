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
    public class SolidAndDashedLineStyle : RegularLineStyle, IRegularLine, IDoubleLine, IDoubleAlignmentLine, IDashedLine, IAsymLine, IEffectStyle
    {
        public override StyleType Type => StyleType.LineSolidAndDashed;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;

        public PropertyBoolValue TwoColors { get; }
        public PropertyColorValue SecondColor { get; }
        public new PropertyValue<float> Offset { get; }
        public PropertyValue<float> DashLength { get; }
        public PropertyValue<float> SpaceLength { get; }
        public PropertyBoolValue Invert { get; }
        public PropertyBoolValue CenterSolid { get; }
        private FakeAligmentProperty FakeAligment { get; }
        public PropertyEnumValue<Alignment> Alignment => FakeAligment;

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
                yield return nameof(CenterSolid);
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
                yield return new StylePropertyDataProvider<bool>(nameof(CenterSolid), CenterSolid);
                yield return new StylePropertyDataProvider<Alignment>(nameof(Alignment), Alignment);
                yield return new StylePropertyDataProvider<bool>(nameof(Invert), Invert);
            }
        }

        public SolidAndDashedLineStyle(Color32 color, Color32 secondColor, bool useSecondColor, float width, Vector2 cracks, Vector2 voids, float texture, float dashLength, float spaceLength, float offset) : base(color, width, cracks, voids, texture)
        {
            TwoColors = GetTwoColorsProperty(useSecondColor);
            SecondColor = GetSecondColorProperty(TwoColors ? secondColor : color);
            Offset = GetOffsetProperty(offset);
            DashLength = GetDashLengthProperty(dashLength);
            SpaceLength = GetSpaceLengthProperty(spaceLength);
            Invert = GetInvertProperty(false);
            CenterSolid = GetCenterSolidProperty(false);
            FakeAligment = new FakeAligmentProperty(string.Empty, base.StyleChanged, this.GetAlignment, this.SetAlignment, Manager.Alignment.Centre);
        }
        private Alignment GetAlignment() => CenterSolid ? Invert ? Manager.Alignment.Right : Manager.Alignment.Left : Manager.Alignment.Centre;
        private void SetAlignment(Alignment value)
        {
            CenterSolid.Value = value != Manager.Alignment.Centre;
            Invert.Value = value == Manager.Alignment.Right;
        }

        protected override void CalculateImpl(MarkingRegularLine line, ITrajectory trajectory, MarkingLOD lod, Action<IStyleData> addData)
        {
            var solidOffset = CenterSolid ? 0 : Invert ? Offset : -Offset;
            var dashedOffset = (Invert ? -Offset : Offset) * (CenterSolid ? 2 : 1);
            var borders = line.Borders;

            var solidParts = StyleHelper.CalculateSolid(trajectory, lod);
            foreach (var part in solidParts)
            {
                StyleHelper.GetPartParams(trajectory, part, solidOffset, out var startPos, out var endPos, out var dir);
                if (StyleHelper.CheckBorders(borders, ref startPos, ref endPos, dir, Width))
                {
                    var data = new DecalData(this, MaterialType.Dash, lod, startPos, endPos, Width, Color);
                    addData(data);
                }
            }

            if (CheckDashedLod(lod, Width, DashLength))
            {
                var dashedParts = StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength);
                foreach (var part in dashedParts)
                {
                    StyleHelper.GetPartParams(trajectory, part, dashedOffset, out var pos, out var dir);
                    if (StyleHelper.CheckBorders(borders, pos, dir, DashLength, Width))
                    {
                        var data = new DecalData(this, MaterialType.Dash, lod, pos, dir, DashLength, Width, TwoColors ? SecondColor : Color);
                        addData(data);
                    }
                }
            }
        }
        public override RegularLineStyle CopyLineStyle() => new SolidAndDashedLineStyle(Color, SecondColor, TwoColors, Width, Cracks, Voids, Texture, DashLength, SpaceLength, Offset);
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
            if (target is IDoubleAlignmentLine doubleAlignmentTarget)
                doubleAlignmentTarget.Alignment.Value = Alignment;
        }

        protected override void GetUIComponents(MarkingRegularLine line, EditorProvider provider)
        {
            base.GetUIComponents(line, provider);

            provider.AddProperty(new PropertyInfo<BoolListPropertyPanel>(this, nameof(TwoColors), MainCategory, AddUseSecondColorProperty));
            provider.AddProperty(new PropertyInfo<ColorAdvancedPropertyPanel>(this, nameof(SecondColor), MainCategory, AddSecondColorProperty));
            provider.AddProperty(new PropertyInfo<Vector2PropertyPanel>(this, nameof(Length), MainCategory, AddLengthProperty));
            provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Offset), MainCategory, AddOffsetProperty));
            if (!provider.isTemplate)
            {
                provider.AddProperty(new PropertyInfo<BoolListPropertyPanel>(this, nameof(CenterSolid), MainCategory, AddCenterSolidProperty));
                provider.AddProperty(new PropertyInfo<ButtonPanel>(this, nameof(Invert), MainCategory, AddInvertProperty));
            }

            //UseSecondColorChanged(this, parent, TwoColors);
        }

        protected void AddCenterSolidProperty(BoolListPropertyPanel centerSolidProperty, EditorProvider provider)
        {
            centerSolidProperty.Text = Localize.StyleOption_SolidInCenter;
            centerSolidProperty.Init(Localize.StyleOption_No, Localize.StyleOption_Yes);
            centerSolidProperty.SelectedObject = CenterSolid;
            centerSolidProperty.OnSelectObjectChanged += (value) => CenterSolid.Value = value;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            TwoColors.ToXml(config);
            SecondColor.ToXml(config);
            Offset.ToXml(config);
            DashLength.ToXml(config);
            SpaceLength.ToXml(config);
            Invert.ToXml(config);
            CenterSolid.ToXml(config);
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
            CenterSolid.FromXml(config, false);
            Invert.FromXml(config, false);
            Invert.Value ^= map.Invert ^ invert ^ typeChanged;
        }

        private class FakeAligmentProperty : PropertyEnumValue<Alignment>
        {
            private Func<Alignment> OnGet { get; }
            private Action<Alignment> OnSet { get; }

            public override Alignment Value { get => OnGet(); set => OnSet(value); }

            public FakeAligmentProperty(string label, Action onChanged, Func<Alignment> onGet, Action<Alignment> onSet, Alignment value = default) : base(label, onChanged, value)
            {
                OnGet = onGet;
                OnSet = onSet;
                Value = value;
            }
        }
    }
}
