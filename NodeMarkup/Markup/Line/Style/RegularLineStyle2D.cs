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
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;

        public SolidLineStyle(Color32 color, float width) : base(color, width) { }

        public override RegularLineStyle CopyLineStyle() => new SolidLineStyle(Color, Width);

        protected override IStyleData CalculateImpl(MarkupRegularLine line, ITrajectory trajectory, MarkupLOD lod)
        {
            var borders = line.Borders;
            return new MarkupPartGroupData(lod, StyleHelper.CalculateSolid(trajectory, lod, GetDashes));

            IEnumerable<MarkupPartData> GetDashes(ITrajectory trajectory) => CalculateDashes(trajectory, borders);
        }
        protected virtual IEnumerable<MarkupPartData> CalculateDashes(ITrajectory trajectory, LineBorders borders)
        {
            if (StyleHelper.CalculateSolidPart(borders, trajectory, 0f, Width, Color, out MarkupPartData dash))
                yield return dash;
        }
    }
    public class DoubleSolidLineStyle : SolidLineStyle, IRegularLine, IDoubleLine, IDoubleAlignmentLine
    {
        public override StyleType Type => StyleType.LineDoubleSolid;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;

        public PropertyBoolValue UseSecondColor { get; }
        public PropertyColorValue SecondColor { get; }
        public PropertyValue<float> Offset { get; }
        public PropertyEnumValue<Alignment> Alignment { get; }

        public DoubleSolidLineStyle(Color32 color, Color32 secondColor, bool useSecondColor, float width, float offset) : base(color, width)
        {
            UseSecondColor = GetUseSecondColorProperty(useSecondColor);
            SecondColor = GetSecondColorProperty(UseSecondColor ? secondColor : color);
            Offset = GetOffsetProperty(offset);
            Alignment = GetAlignmentProperty(Manager.Alignment.Centre);
        }

        public override RegularLineStyle CopyLineStyle() => new DoubleSolidLineStyle(Color, SecondColor, UseSecondColor, Width, Offset);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is IDoubleLine doubleTarget)
            {
                doubleTarget.Offset.Value = Offset;
                doubleTarget.SecondColor.Value = SecondColor;
                doubleTarget.UseSecondColor.Value = UseSecondColor;
            }
            if (target is IDoubleAlignmentLine doubleAlignmentTarget)
                doubleAlignmentTarget.Alignment.Value = Alignment;
        }

        protected override IEnumerable<MarkupPartData> CalculateDashes(ITrajectory trajectory, LineBorders borders)
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

            if (StyleHelper.CalculateSolidPart(borders, trajectory, firstOffset, Width, Color, out MarkupPartData firstDash))
                yield return firstDash;

            if (StyleHelper.CalculateSolidPart(borders, trajectory, secondOffset, Width, UseSecondColor ? SecondColor : Color, out MarkupPartData secondDash))
                yield return secondDash;
        }
        public override void GetUIComponents(MarkupRegularLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);

            components.Add(AddUseSecondColorProperty(this, parent));
            components.Add(AddSecondColorProperty(this, parent));
            UseSecondColorChanged(this, parent, UseSecondColor);

            components.Add(AddOffsetProperty(this, parent));
            if (!isTemplate)
                components.Add(AddAlignmentProperty(this, parent));
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            UseSecondColor.ToXml(config);
            SecondColor.ToXml(config);
            Offset.ToXml(config);
            Alignment.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            UseSecondColor.FromXml(config, false);
            SecondColor.FromXml(config, DefaultColor);
            Offset.FromXml(config, DefaultDoubleOffset);
            Alignment.FromXml(config, Manager.Alignment.Centre);
            if (invert)
                Alignment.Value = Alignment.Value.Invert();
        }
    }
    public class DashedLineStyle : RegularLineStyle, IRegularLine, IDashedLine
    {
        public override StyleType Type => StyleType.LineDashed;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;

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

        protected override IStyleData CalculateImpl(MarkupRegularLine line, ITrajectory trajectory, MarkupLOD lod)
        {
            if (!CheckDashedLod(lod, Width, DashLength))
                return new MarkupPartGroupData(lod);

            var borders = line.Borders;
            return new MarkupPartGroupData(lod, StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength, GetDashes));

            IEnumerable<MarkupPartData> GetDashes(ITrajectory trajectory, float startT, float endT)
                => CalculateDashes(trajectory, startT, endT, borders);
        }

        protected virtual IEnumerable<MarkupPartData> CalculateDashes(ITrajectory trajectory, float startT, float endT, LineBorders borders)
        {
            if (StyleHelper.CalculateDashedParts(borders, trajectory, startT, endT, DashLength, 0, Width, Color, out MarkupPartData dash))
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
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            DashLength.FromXml(config, DefaultDashLength);
            SpaceLength.FromXml(config, DefaultSpaceLength);
        }
    }
    public class DoubleDashedLineStyle : DashedLineStyle, IRegularLine, IDoubleLine, IDoubleAlignmentLine
    {
        public override StyleType Type => StyleType.LineDoubleDashed;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;

        public PropertyValue<float> Offset { get; }
        public PropertyEnumValue<Alignment> Alignment { get; }
        public PropertyBoolValue UseSecondColor { get; }
        public PropertyColorValue SecondColor { get; }

        public DoubleDashedLineStyle(Color32 color, Color32 secondColor, bool useSecondColor, float width, float dashLength, float spaceLength, float offset) : base(color, width, dashLength, spaceLength)
        {
            Offset = GetOffsetProperty(offset);
            Alignment = GetAlignmentProperty(Manager.Alignment.Centre);
            UseSecondColor = GetUseSecondColorProperty(useSecondColor);
            SecondColor = GetSecondColorProperty(UseSecondColor ? secondColor : color);
        }

        public override RegularLineStyle CopyLineStyle() => new DoubleDashedLineStyle(Color, SecondColor, UseSecondColor, Width, DashLength, SpaceLength, Offset);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is IDoubleLine doubleTarget)
            {
                doubleTarget.Offset.Value = Offset;
                doubleTarget.SecondColor.Value = SecondColor;
                doubleTarget.UseSecondColor.Value = UseSecondColor;
            }
            if (target is IDoubleAlignmentLine doubleAlignmentTarget)
                doubleAlignmentTarget.Alignment.Value = Alignment;
        }

        protected override IEnumerable<MarkupPartData> CalculateDashes(ITrajectory trajectory, float startT, float endT, LineBorders borders)
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

            if (StyleHelper.CalculateDashedParts(borders, trajectory, startT, endT, DashLength, firstOffset, Width, Color, out MarkupPartData firstDash))
                yield return firstDash;

            if (StyleHelper.CalculateDashedParts(borders, trajectory, startT, endT, DashLength, secondOffset, Width, UseSecondColor ? SecondColor : Color, out MarkupPartData secondDash))
                yield return secondDash;
        }
        public override void GetUIComponents(MarkupRegularLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);
            components.Add(AddUseSecondColorProperty(this, parent));
            components.Add(AddSecondColorProperty(this, parent));
            components.Add(AddOffsetProperty(this, parent));
            if (!isTemplate)
                components.Add(AddAlignmentProperty(this, parent));

            UseSecondColorChanged(this, parent, UseSecondColor);
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            UseSecondColor.ToXml(config);
            SecondColor.ToXml(config);
            Offset.ToXml(config);
            Alignment.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            UseSecondColor.FromXml(config, false);
            SecondColor.FromXml(config, DefaultColor);
            Offset.FromXml(config, DefaultDoubleOffset);
            Alignment.FromXml(config, Manager.Alignment.Centre);
            if (invert)
                Alignment.Value = Alignment.Value.Invert();
        }
    }
    public class SolidAndDashedLineStyle : RegularLineStyle, IRegularLine, IDoubleLine, IDoubleAlignmentLine, IDashedLine, IAsymLine
    {
        public override StyleType Type => StyleType.LineSolidAndDashed;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;

        public PropertyBoolValue UseSecondColor { get; }
        public PropertyColorValue SecondColor { get; }
        public PropertyValue<float> Offset { get; }
        public PropertyValue<float> DashLength { get; }
        public PropertyValue<float> SpaceLength { get; }
        public PropertyBoolValue Invert { get; }
        public PropertyBoolValue CenterSolid { get; }
        private FakeAligmentProperty FakeAligment { get; }
        public PropertyEnumValue<Alignment> Alignment => FakeAligment;

        public SolidAndDashedLineStyle(Color32 color, Color32 secondColor, bool useSecondColor, float width, float dashLength, float spaceLength, float offset) : base(color, width)
        {
            UseSecondColor = GetUseSecondColorProperty(useSecondColor);
            SecondColor = GetSecondColorProperty(UseSecondColor ? secondColor : color);
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

        protected override IStyleData CalculateImpl(MarkupRegularLine line, ITrajectory trajectory, MarkupLOD lod)
        {
            var solidOffset = CenterSolid ? 0 : Invert ? Offset : -Offset;
            var dashedOffset = (Invert ? -Offset : Offset) * (CenterSolid ? 2 : 1);
            var borders = line.Borders;

            var dashes = new List<MarkupPartData>();

            dashes.AddRange(StyleHelper.CalculateSolid(trajectory, lod, CalculateSolidDash));
            if (CheckDashedLod(lod, Width, DashLength))
                dashes.AddRange(StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength, CalculateDashedDash));

            return new MarkupPartGroupData(lod, dashes);

            IEnumerable<MarkupPartData> CalculateSolidDash(ITrajectory lineTrajectory)
            {
                if (StyleHelper.CalculateSolidPart(borders, lineTrajectory, solidOffset, Width, Color, out MarkupPartData dash))
                    yield return dash;
            }

            IEnumerable<MarkupPartData> CalculateDashedDash(ITrajectory lineTrajectory, float startT, float endT)
            {
                if (StyleHelper.CalculateDashedParts(borders, lineTrajectory, startT, endT, DashLength, dashedOffset, Width, UseSecondColor ? SecondColor : Color, out MarkupPartData dash))
                    yield return dash;
            }
        }
        public override RegularLineStyle CopyLineStyle() => new SolidAndDashedLineStyle(Color, SecondColor, UseSecondColor, Width, DashLength, SpaceLength, Offset);
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
                doubleTarget.UseSecondColor.Value = UseSecondColor;
            }
            if (target is IDoubleAlignmentLine doubleAlignmentTarget)
                doubleAlignmentTarget.Alignment.Value = Alignment;
        }
        public override void GetUIComponents(MarkupRegularLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);
            components.Add(AddUseSecondColorProperty(this, parent));
            components.Add(AddSecondColorProperty(this, parent));
            components.Add(AddDashLengthProperty(this, parent));
            components.Add(AddSpaceLengthProperty(this, parent));
            components.Add(AddOffsetProperty(this, parent));
            if (!isTemplate)
            {
                components.Add(AddCenterSolidProperty(parent));
                components.Add(AddInvertProperty(this, parent));
            }

            UseSecondColorChanged(this, parent, UseSecondColor);
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
            UseSecondColor.ToXml(config);
            SecondColor.ToXml(config);
            Offset.ToXml(config);
            DashLength.ToXml(config);
            SpaceLength.ToXml(config);
            Invert.ToXml(config);
            CenterSolid.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            UseSecondColor.FromXml(config, false);
            SecondColor.FromXml(config, DefaultColor);
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
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;
        protected override float LodWidth => 0.5f;

        public PropertyValue<float> Base { get; }
        public PropertyValue<float> Height { get; }
        public PropertyValue<float> Space { get; }
        public PropertyBoolValue Invert { get; }
        public PropertyValue<float> Angle { get; }

        public SharkTeethLineStyle(Color32 color, float baseValue, float height, float space, float angle) : base(color, 0)
        {
            Base = GetBaseProperty(baseValue);
            Height = GetHeightProperty(height);
            Space = GetSpaceProperty(space);
            Invert = GetInvertProperty(true);
            Angle = GetAngleProperty(angle);
        }
        protected override IStyleData CalculateImpl(MarkupRegularLine line, ITrajectory trajectory, MarkupLOD lod)
        {
            if (!CheckDashedLod(lod, Height, Base))
                return new MarkupPartGroupData(lod);

            var borders = line.Borders;
            var coef = Mathf.Cos(Angle * Mathf.Deg2Rad);
            return new MarkupPartGroupData(lod, StyleHelper.CalculateDashed(trajectory, Base / coef, Space / coef, CalculateDashes));

            IEnumerable<MarkupPartData> CalculateDashes(ITrajectory trajectory, float startT, float endT)
            {
                if (StyleHelper.CalculateDashedParts(borders, trajectory, Invert ? endT : startT, Invert ? startT : endT, Base, Height / (Invert ? 2 : -2), Height, Color, out MarkupPartData dash))
                {
                    dash.MaterialType = MaterialType.Triangle;
                    dash.Angle -= Angle * Mathf.Deg2Rad;
                    yield return dash;
                }
            }
        }

        public override RegularLineStyle CopyLineStyle() => new SharkTeethLineStyle(Color, Base, Height, Space, Angle);
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
            var angle = AddAngleProperty(parent);
            components.Add(angle);
            if (!isTemplate)
            {
                var invertButton = AddInvertProperty(parent);
                components.Add(invertButton);

                invertButton.OnButtonClick += OnButtonClick;

                void OnButtonClick()
                {
                    Invert.Value = !Invert;
                    Angle.Value = -Angle;
                    angle.Value = Angle;
                }
            }
        }
        protected FloatPropertyPanel AddAngleProperty(UIComponent parent)
        {
            var angleProperty = parent.AddUIComponent<FloatPropertyPanel>();
            angleProperty.Text = Localize.StyleOption_SharkToothAngle;
            angleProperty.Format = Localize.NumberFormat_Degree;
            angleProperty.UseWheel = true;
            angleProperty.WheelStep = 1f;
            angleProperty.CheckMin = true;
            angleProperty.MinValue = -60f;
            angleProperty.CheckMax = true;
            angleProperty.MaxValue = 60f;
            angleProperty.Init();
            angleProperty.Value = Angle;
            angleProperty.OnValueChanged += (float value) => Angle.Value = value;

            return angleProperty;
        }
        protected ButtonPanel AddInvertProperty(UIComponent parent)
        {
            var buttonsPanel = ComponentPool.Get<ButtonPanel>(parent, nameof(Invert));
            buttonsPanel.Text = Localize.StyleOption_Invert;
            buttonsPanel.Init();

            return buttonsPanel;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Base.ToXml(config);
            Height.ToXml(config);
            Space.ToXml(config);
            Invert.ToXml(config);
            Angle.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Base.FromXml(config, DefaultSharkBaseLength);
            Height.FromXml(config, DefaultSharkHeight);
            Space.FromXml(config, DefaultSharkSpaceLength);
            Invert.FromXml(config, false);
            Invert.Value ^= map.IsMirror ^ invert;
            Angle.FromXml(config, DefaultSharkAngle);
        }
    }
}
