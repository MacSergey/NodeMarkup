using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public class SolidLineStyle : RegularLineStyle, IRegularLine
    {
        public override StyleType Type => StyleType.LineSolid;

        public SolidLineStyle(Color32 color, float width) : base(color, width) { }

        public override RegularLineStyle CopyLineStyle() => new SolidLineStyle(Color, Width);

        public override IStyleData Calculate(MarkupLine line, ITrajectory trajectory, MarkupLOD lod)
        {
            var borders = line.Borders;
            return new MarkupStyleParts(StyleHelper.CalculateSolid(trajectory, lod, GetDashes));

            IEnumerable<MarkupStylePart> GetDashes(ITrajectory trajectory) => CalculateDashes(trajectory, borders);
        }
        protected virtual IEnumerable<MarkupStylePart> CalculateDashes(ITrajectory trajectory, LineBorders borders)
        {
            if (StyleHelper.CalculateSolidPart(borders, trajectory, 0f, Width, Color, out MarkupStylePart dash))
                yield return dash;
        }
    }
    public class DoubleSolidLineStyle : SolidLineStyle, IRegularLine, IDoubleLine, IDoubleAlignmentLine
    {
        public override StyleType Type => StyleType.LineDoubleSolid;

        public PropertyValue<float> Offset { get; }
        public PropertyEnumValue<Alignment> Alignment { get; }

        public DoubleSolidLineStyle(Color32 color, float width, float offset) : base(color, width)
        {
            Offset = GetOffsetProperty(offset);
            Alignment = GetAlignmentProperty(Manager.Alignment.Centre);
        }

        public override RegularLineStyle CopyLineStyle() => new DoubleSolidLineStyle(Color, Width, Offset);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is IDoubleLine doubleTarget)
                doubleTarget.Offset.Value = Offset;
            if (target is IDoubleAlignmentLine doubleAlignmentTarget)
                doubleAlignmentTarget.Alignment.Value = Alignment;
        }

        protected override IEnumerable<MarkupStylePart> CalculateDashes(ITrajectory trajectory, LineBorders borders)
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

            if (StyleHelper.CalculateSolidPart(borders, trajectory, firstOffset, Width, Color, out MarkupStylePart firstDash))
                yield return firstDash;

            if (StyleHelper.CalculateSolidPart(borders, trajectory, secondOffset, Width, Color, out MarkupStylePart secondDash))
                yield return secondDash;
        }
        public override void GetUIComponents(MarkupRegularLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);
            components.Add(AddOffsetProperty(this, parent));
            if (!isTemplate)
                components.Add(AddAlignmentProperty(this, parent));
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            Offset.ToXml(config);
            Alignment.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, Utilities.ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Offset.FromXml(config, DefaultDoubleOffset);
            Alignment.FromXml(config, Manager.Alignment.Centre);
            if (invert)
                Alignment.Value = Alignment.Value.Invert();
        }
    }
    public class DashedLineStyle : RegularLineStyle, IRegularLine, IDashedLine
    {
        public override StyleType Type => StyleType.LineDashed;

        public PropertyValue<float> DashLength { get; }
        public PropertyValue<float> SpaceLength { get; }

        public DashedLineStyle(Color32 color, float width, float dashLength, float spaceLength) : base(color, width)
        {
            DashLength = GetDashLengthProperty(dashLength);
            SpaceLength = GetSpaceLengthProperty(spaceLength);
        }

        public override RegularLineStyle CopyLineStyle() => new DashedLineStyle(Color, Width, DashLength, SpaceLength);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is IDashedLine dashedTarget)
            {
                dashedTarget.DashLength.Value = DashLength;
                dashedTarget.SpaceLength.Value = SpaceLength;
            }
        }

        public override IStyleData Calculate(MarkupLine line, ITrajectory trajectory, MarkupLOD lod)
        {
            if (!CheckDashedLod(lod, Width, DashLength))
                return new MarkupStyleParts();

            var borders = line.Borders;
            return new MarkupStyleParts(StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength, GetDashes));

            IEnumerable<MarkupStylePart> GetDashes(ITrajectory trajectory, float startT, float endT)
                => CalculateDashes(trajectory, startT, endT, borders);
        }

        protected virtual IEnumerable<MarkupStylePart> CalculateDashes(ITrajectory trajectory, float startT, float endT, LineBorders borders)
        {
            if (StyleHelper.CalculateDashedParts(borders, trajectory, startT, endT, DashLength, 0, Width, Color, out MarkupStylePart dash))
                yield return dash;
        }

        public override void GetUIComponents(MarkupRegularLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);
            components.Add(AddDashLengthProperty(this, parent));
            components.Add(AddSpaceLengthProperty(this, parent));
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            DashLength.ToXml(config);
            SpaceLength.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, Utilities.ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            DashLength.FromXml(config, DefaultDashLength);
            SpaceLength.FromXml(config, DefaultSpaceLength);
        }
    }
    public class DoubleDashedLineStyle : DashedLineStyle, IRegularLine, IDoubleLine, IDoubleAlignmentLine
    {
        public override StyleType Type => StyleType.LineDoubleDashed;

        public PropertyValue<float> Offset { get; }
        public PropertyEnumValue<Alignment> Alignment { get; }

        public DoubleDashedLineStyle(Color32 color, float width, float dashLength, float spaceLength, float offset) : base(color, width, dashLength, spaceLength)
        {
            Offset = GetOffsetProperty(offset);
            Alignment = GetAlignmentProperty(Manager.Alignment.Centre);
        }

        public override RegularLineStyle CopyLineStyle() => new DoubleDashedLineStyle(Color, Width, DashLength, SpaceLength, Offset);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is IDoubleLine doubleTarget)
                doubleTarget.Offset.Value = Offset;
            if (target is IDoubleAlignmentLine doubleAlignmentTarget)
                doubleAlignmentTarget.Alignment.Value = Alignment;
        }

        protected override IEnumerable<MarkupStylePart> CalculateDashes(ITrajectory trajectory, float startT, float endT, LineBorders borders)
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

            if (StyleHelper.CalculateDashedParts(borders, trajectory, startT, endT, DashLength, firstOffset, Width, Color, out MarkupStylePart firstDash))
                yield return firstDash;

            if (StyleHelper.CalculateDashedParts(borders, trajectory, startT, endT, DashLength, secondOffset, Width, Color, out MarkupStylePart secondDash))
                yield return secondDash;
        }
        public override void GetUIComponents(MarkupRegularLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);
            components.Add(AddOffsetProperty(this, parent));
            if (!isTemplate)
                components.Add(AddAlignmentProperty(this, parent));
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            Offset.ToXml(config);
            Alignment.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, Utilities.ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Offset.FromXml(config, DefaultDoubleOffset);
            Alignment.FromXml(config, Manager.Alignment.Centre);
            if (invert)
                Alignment.Value = Alignment.Value.Invert();
        }
    }
    public class SolidAndDashedLineStyle : RegularLineStyle, IRegularLine, IDoubleLine, IDoubleAlignmentLine, IDashedLine, IAsymLine
    {
        public override StyleType Type => StyleType.LineSolidAndDashed;

        public PropertyValue<float> Offset { get; }
        public PropertyValue<float> DashLength { get; }
        public PropertyValue<float> SpaceLength { get; }
        public PropertyBoolValue Invert { get; }
        public PropertyBoolValue CenterSolid { get; }
        private FakeAligmentProperty FakeAligment { get; }
        public PropertyEnumValue<Alignment> Alignment => FakeAligment;

        public SolidAndDashedLineStyle(Color32 color, float width, float dashLength, float spaceLength, float offset) : base(color, width)
        {
            Offset = GetOffsetProperty(offset);
            DashLength = GetDashLengthProperty(dashLength);
            SpaceLength = GetSpaceLengthProperty(spaceLength);
            Invert = GetInvertProperty(false);
            CenterSolid = GetCenterSolidProperty(false);
            FakeAligment = new FakeAligmentProperty(string.Empty, base.StyleChanged, this.GetAlignment, this.SetAlignment, Manager.Alignment.Centre);
        }
        private Alignment GetAlignment() => CenterSolid ? (Invert ? Manager.Alignment.Right : Manager.Alignment.Left) : Manager.Alignment.Centre;
        private void SetAlignment(Alignment value)
        {
            CenterSolid.Value = value != Manager.Alignment.Centre;
            Invert.Value = value == Manager.Alignment.Right;
        }

        public override IStyleData Calculate(MarkupLine line, ITrajectory trajectory, MarkupLOD lod)
        {
            var solidOffset = CenterSolid ? 0 : Invert ? Offset : -Offset;
            var dashedOffset = (Invert ? -Offset : Offset) * (CenterSolid ? 2 : 1);
            var borders = line.Borders;

            var dashes = new List<MarkupStylePart>();
            dashes.AddRange(StyleHelper.CalculateSolid(trajectory, lod, CalculateSolidDash));
            if (CheckDashedLod(lod, Width, DashLength))
                dashes.AddRange(StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength, CalculateDashedDash));

            return new MarkupStyleParts(dashes);

            IEnumerable<MarkupStylePart> CalculateSolidDash(ITrajectory lineTrajectory)
            {
                if (StyleHelper.CalculateSolidPart(borders, lineTrajectory, solidOffset, Width, Color, out MarkupStylePart dash))
                    yield return dash;
            }

            IEnumerable<MarkupStylePart> CalculateDashedDash(ITrajectory lineTrajectory, float startT, float endT)
            {
                if (StyleHelper.CalculateDashedParts(borders, lineTrajectory, startT, endT, DashLength, dashedOffset, Width, Color, out MarkupStylePart dash))
                    yield return dash;
            }
        }
        public override RegularLineStyle CopyLineStyle() => new SolidAndDashedLineStyle(Color, Width, DashLength, SpaceLength, Offset);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is IDashedLine dashedTarget)
            {
                dashedTarget.DashLength.Value = DashLength;
                dashedTarget.SpaceLength.Value = SpaceLength;
            }
            if (target is IDoubleLine doubleTarget)
                doubleTarget.Offset.Value = Offset;
            if (target is IDoubleAlignmentLine doubleAlignmentTarget)
                doubleAlignmentTarget.Alignment.Value = Alignment;
        }
        public override void GetUIComponents(MarkupRegularLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);
            components.Add(AddDashLengthProperty(this, parent));
            components.Add(AddSpaceLengthProperty(this, parent));
            components.Add(AddOffsetProperty(this, parent));
            if (!isTemplate)
            {
                components.Add(AddCenterSolidProperty(parent));
                components.Add(AddInvertProperty(this, parent));
            }
        }
        protected BoolListPropertyPanel AddCenterSolidProperty(UIComponent parent)
        {
            var centerSolidProperty = ComponentPool.Get<BoolListPropertyPanel>(parent, nameof(CenterSolid));
            centerSolidProperty.Text = Localize.StyleOption_SolidInCenter;
            centerSolidProperty.Init(Localize.StyleOption_No, Localize.StyleOption_Yes);
            centerSolidProperty.SelectedObject = CenterSolid;
            centerSolidProperty.OnSelectObjectChanged += (value) => CenterSolid.Value = value;
            return centerSolidProperty;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Offset.ToXml(config);
            DashLength.ToXml(config);
            SpaceLength.ToXml(config);
            Invert.ToXml(config);
            CenterSolid.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, Utilities.ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Offset.FromXml(config, DefaultDoubleOffset);
            DashLength.FromXml(config, DefaultDashLength);
            SpaceLength.FromXml(config, DefaultSpaceLength);
            Invert.FromXml(config, false);
            Invert.Value ^= map.IsMirror ^ invert;
            CenterSolid.FromXml(config, false);
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
    public class SharkTeethLineStyle : RegularLineStyle, IColorStyle, IAsymLine, ISharkLine
    {
        public override StyleType Type => StyleType.LineSharkTeeth;
        protected override float LodWidth => 0.5f;

        public PropertyValue<float> Base { get; }
        public PropertyValue<float> Height { get; }
        public PropertyValue<float> Space { get; }
        public PropertyBoolValue Invert { get; }
        public SharkTeethLineStyle(Color32 color, float baseValue, float height, float space) : base(color, 0)
        {
            Base = GetBaseProperty(baseValue);
            Height = GetHeightProperty(height);
            Space = GetSpaceProperty(space);
            Invert = GetInvertProperty(true);
        }
        public override IStyleData Calculate(MarkupLine line, ITrajectory trajectory, MarkupLOD lod)
        {
            if (!CheckDashedLod(lod, Height, Base))
                return new MarkupStyleParts();

            var borders = line.Borders;
            return new MarkupStyleParts(StyleHelper.CalculateDashed(trajectory, Base, Space, CalculateDashes));

            IEnumerable<MarkupStylePart> CalculateDashes(ITrajectory trajectory, float startT, float endT)
            {
                if (StyleHelper.CalculateDashedParts(borders, trajectory, Invert ? endT : startT, Invert ? startT : endT, Base, Height / (Invert ? 2 : -2), Height, Color, out MarkupStylePart dash))
                {
                    dash.MaterialType = MaterialType.Triangle;
                    yield return dash;
                }
            }
        }

        public override RegularLineStyle CopyLineStyle() => new SharkTeethLineStyle(Color, Base, Height, Space);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is SharkTeethLineStyle sharkTeethTarget)
            {
                sharkTeethTarget.Base.Value = Base;
                sharkTeethTarget.Height.Value = Height;
                sharkTeethTarget.Space.Value = Space;
            }
        }
        public override void GetUIComponents(MarkupRegularLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);
            components.Add(AddBaseProperty(this, parent));
            components.Add(AddHeightProperty(this, parent));
            components.Add(AddSpaceProperty(this, parent));
            if (!isTemplate)
                components.Add(AddInvertProperty(this, parent));
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Base.ToXml(config);
            Height.ToXml(config);
            Space.ToXml(config);
            Invert.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, Utilities.ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Base.FromXml(config, DefaultSharkBaseLength);
            Height.FromXml(config, DefaultSharkHeight);
            Space.FromXml(config, DefaultSharkSpaceLength);
            Invert.FromXml(config, false);
            Invert.Value ^= map.IsMirror ^ invert;
        }
    }
}
