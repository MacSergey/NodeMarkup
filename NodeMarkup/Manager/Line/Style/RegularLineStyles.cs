using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public class SolidLineStyle : RegularLineStyle, IRegularLine
    {
        public override StyleType Type { get; } = StyleType.LineSolid;

        public SolidLineStyle(Color color, float width) : base(color, width) { }

        public override RegularLineStyle CopyRegularLineStyle() => new SolidLineStyle(Color, Width);

        public override IEnumerable<MarkupStyleDash> Calculate(MarkupLine line, ILineTrajectory trajectory) => StyleHelper.CalculateSolid(trajectory, CalculateDashes);
        protected virtual IEnumerable<MarkupStyleDash> CalculateDashes(ILineTrajectory trajectory)
        {
            yield return StyleHelper.CalculateSolidDash(trajectory, 0f, Width, Color);
        }
    }
    public class DoubleSolidLineStyle : SolidLineStyle, IRegularLine, IDoubleLine, IDoubleAlignmentLine
    {
        public override StyleType Type { get; } = StyleType.LineDoubleSolid;

        float _offset;
        StyleAlignment _alignment;
        public float Offset
        {
            get => _offset;
            set
            {
                _offset = value;
                StyleChanged();
            }
        }
        public StyleAlignment Alignment
        {
            get => _alignment;
            set
            {
                _alignment = value;
                StyleChanged();
            }
        }

        public DoubleSolidLineStyle(Color color, float width, float offset) : base(color, width)
        {
            Offset = offset;
            Alignment = StyleAlignment.Centre;
        }

        public override RegularLineStyle CopyRegularLineStyle() => new DoubleSolidLineStyle(Color, Width, Offset);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is IDoubleLine doubleTarget)
                doubleTarget.Offset = Offset;
            if (target is IDoubleAlignmentLine doubleAlignmentTarget)
                doubleAlignmentTarget.Alignment = Alignment;
        }

        protected override IEnumerable<MarkupStyleDash> CalculateDashes(ILineTrajectory trajectory)
        {
            var firstOffset = Alignment switch
            {
                StyleAlignment.Left => 2 * Offset,
                StyleAlignment.Centre => Offset,
                StyleAlignment.Right => 0,
            };
            var secondOffset = Alignment switch
            {
                StyleAlignment.Left => 0,
                StyleAlignment.Centre => -Offset,
                StyleAlignment.Right => -2 * Offset,
            };

            yield return StyleHelper.CalculateSolidDash(trajectory, firstOffset, Width, Color);
            yield return StyleHelper.CalculateSolidDash(trajectory, secondOffset, Width, Color);
        }
        public override List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            components.Add(AddOffsetProperty(this, parent, onHover, onLeave));
            if (!isTemplate)
                components.Add(AddAlignmentProperty(this, parent, onHover, onLeave));
            return components;
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute("O", Offset));
            config.Add(new XAttribute("A", (int)Alignment));
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Offset = config.GetAttrValue("O", DefaultOffset);
            Alignment = (StyleAlignment)config.GetAttrValue("A", (int)StyleAlignment.Centre);
            if (invert)
                Alignment = Alignment.Invert();
        }
    }
    public class DashedLineStyle : RegularLineStyle, IRegularLine, IDashedLine
    {
        public override StyleType Type { get; } = StyleType.LineDashed;

        float _dashLength;
        float _spaceLength;
        public float DashLength
        {
            get => _dashLength;
            set
            {
                _dashLength = value;
                StyleChanged();
            }
        }
        public float SpaceLength
        {
            get => _spaceLength;
            set
            {
                _spaceLength = value;
                StyleChanged();
            }
        }

        public DashedLineStyle(Color color, float width, float dashLength, float spaceLength) : base(color, width)
        {
            DashLength = dashLength;
            SpaceLength = spaceLength;
        }

        public override RegularLineStyle CopyRegularLineStyle() => new DashedLineStyle(Color, Width, DashLength, SpaceLength);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is IDashedLine dashedTarget)
            {
                dashedTarget.DashLength = DashLength;
                dashedTarget.SpaceLength = SpaceLength;
            }
        }

        public override IEnumerable<MarkupStyleDash> Calculate(MarkupLine line, ILineTrajectory trajectory) => StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength, CalculateDashes);

        protected virtual IEnumerable<MarkupStyleDash> CalculateDashes(ILineTrajectory trajectory, float startT, float endT)
        {
            yield return StyleHelper.CalculateDashedDash(trajectory, startT, endT, DashLength, 0, Width, Color);
        }
        public override List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            components.Add(AddDashLengthProperty(this, parent, onHover, onLeave));
            components.Add(AddSpaceLengthProperty(this, parent, onHover, onLeave));
            return components;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute("DL", DashLength));
            config.Add(new XAttribute("SL", SpaceLength));
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            DashLength = config.GetAttrValue("DL", DefaultDashLength);
            SpaceLength = config.GetAttrValue("SL", DefaultSpaceLength);
        }
    }
    public class DoubleDashedLineStyle : DashedLineStyle, IRegularLine, IDoubleLine, IDoubleAlignmentLine
    {
        public override StyleType Type { get; } = StyleType.LineDoubleDashed;

        float _offset;
        StyleAlignment _alignment;
        public float Offset
        {
            get => _offset;
            set
            {
                _offset = value;
                StyleChanged();
            }
        }
        public StyleAlignment Alignment
        {
            get => _alignment;
            set
            {
                _alignment = value;
                StyleChanged();
            }
        }

        public DoubleDashedLineStyle(Color color, float width, float dashLength, float spaceLength, float offset) : base(color, width, dashLength, spaceLength)
        {
            Offset = offset;
            Alignment = StyleAlignment.Centre;
        }

        public override RegularLineStyle CopyRegularLineStyle() => new DoubleDashedLineStyle(Color, Width, DashLength, SpaceLength, Offset);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is IDoubleLine doubleTarget)
                doubleTarget.Offset = Offset;
            if (target is IDoubleAlignmentLine doubleAlignmentTarget)
                doubleAlignmentTarget.Alignment = Alignment;
        }

        protected override IEnumerable<MarkupStyleDash> CalculateDashes(ILineTrajectory trajectory, float startT, float endT)
        {
            var firstOffset = Alignment switch
            {
                StyleAlignment.Left => 2 * Offset,
                StyleAlignment.Centre => Offset,
                StyleAlignment.Right => 0,
            };
            var secondOffset = Alignment switch
            {
                StyleAlignment.Left => 0,
                StyleAlignment.Centre => -Offset,
                StyleAlignment.Right => -2 * Offset,
            };

            yield return StyleHelper.CalculateDashedDash(trajectory, startT, endT, DashLength, firstOffset, Width, Color);
            yield return StyleHelper.CalculateDashedDash(trajectory, startT, endT, DashLength, secondOffset, Width, Color);
        }
        public override List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            components.Add(AddOffsetProperty(this, parent, onHover, onLeave));
            if (!isTemplate)
                components.Add(AddAlignmentProperty(this, parent, onHover, onLeave));
            return components;
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute("O", Offset));
            config.Add(new XAttribute("A", (int)Alignment));
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Offset = config.GetAttrValue("O", DefaultOffset);
            Alignment = (StyleAlignment)config.GetAttrValue("A", (int)StyleAlignment.Centre);
            if (invert)
                Alignment = Alignment.Invert();
        }
    }
    public class SolidAndDashedLineStyle : RegularLineStyle, IRegularLine, IDoubleLine, IDoubleAlignmentLine, IDashedLine, IAsymLine
    {
        public override StyleType Type => StyleType.LineSolidAndDashed;

        float _offset;
        float _dashLength;
        float _spaceLength;
        bool _invert;
        bool _centerSolid;
        public float Offset
        {
            get => _offset;
            set
            {
                _offset = value;
                StyleChanged();
            }
        }
        public float DashLength
        {
            get => _dashLength;
            set
            {
                _dashLength = value;
                StyleChanged();
            }
        }
        public float SpaceLength
        {
            get => _spaceLength;
            set
            {
                _spaceLength = value;
                StyleChanged();
            }
        }
        public bool Invert
        {
            get => _invert;
            set
            {
                _invert = value;
                StyleChanged();
            }
        }
        public bool CenterSolid
        {
            get => _centerSolid;
            set
            {
                _centerSolid = value;
                StyleChanged();
            }
        }
        public StyleAlignment Alignment
        {
            get => CenterSolid ? (Invert ? StyleAlignment.Right : StyleAlignment.Left) : StyleAlignment.Centre;
            set
            {
                _centerSolid = value != StyleAlignment.Centre;
                _invert = value == StyleAlignment.Right;
                StyleChanged();
            }
        }

        public SolidAndDashedLineStyle(Color color, float width, float dashLength, float spaceLength, float offset) : base(color, width)
        {
            Offset = offset;
            DashLength = dashLength;
            SpaceLength = spaceLength;
            Alignment = StyleAlignment.Centre;
        }


        public override IEnumerable<MarkupStyleDash> Calculate(MarkupLine line, ILineTrajectory trajectory)
        {
            var solidOffset = CenterSolid ? 0 : Invert ? Offset : -Offset;
            var dashedOffset = (Invert ? -Offset : Offset) * (CenterSolid ? 2 : 1);

            foreach (var dash in StyleHelper.CalculateSolid(trajectory, CalculateSolidDash))
                yield return dash;

            foreach (var dash in StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength, CalculateDashedDash))
                yield return dash;

            IEnumerable<MarkupStyleDash> CalculateSolidDash(ILineTrajectory lineTrajectory)
            {
                yield return StyleHelper.CalculateSolidDash(lineTrajectory, solidOffset, Width, Color);
            }
            IEnumerable<MarkupStyleDash> CalculateDashedDash(ILineTrajectory lineTrajectory, float startT, float endT)
            {
                yield return StyleHelper.CalculateDashedDash(lineTrajectory, startT, endT, DashLength, dashedOffset, Width, Color);
            }
        }
        public override RegularLineStyle CopyRegularLineStyle() => new SolidAndDashedLineStyle(Color, Width, DashLength, SpaceLength, Offset);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is IDashedLine dashedTarget)
            {
                dashedTarget.DashLength = DashLength;
                dashedTarget.SpaceLength = SpaceLength;
            }
            if (target is IDoubleLine doubleTarget)
                doubleTarget.Offset = Offset;
            if (target is IDoubleAlignmentLine doubleAlignmentTarget)
                doubleAlignmentTarget.Alignment = Alignment;
        }
        public override List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            components.Add(AddDashLengthProperty(this, parent, onHover, onLeave));
            components.Add(AddSpaceLengthProperty(this, parent, onHover, onLeave));
            components.Add(AddOffsetProperty(this, parent, onHover, onLeave));
            if (!isTemplate)
            {
                components.Add(AddCenterSolidProperty(this, parent));
                components.Add(AddInvertProperty(this, parent));
            }
            return components;
        }
        protected static BoolListPropertyPanel AddCenterSolidProperty(SolidAndDashedLineStyle solidAndDashedStyle, UIComponent parent)
        {
            var centerSolidProperty = parent.AddUIComponent<BoolListPropertyPanel>();
            centerSolidProperty.Text = Localize.LineEditor_SolidInCenter;
            centerSolidProperty.Init(Localize.LineEditor_SolidInCenterDisable, Localize.LineEditor_SolidInCenterEnable);
            centerSolidProperty.SelectedObject = solidAndDashedStyle.CenterSolid;
            centerSolidProperty.OnSelectObjectChanged += (value) => solidAndDashedStyle.CenterSolid = value;
            return centerSolidProperty;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute("O", Offset));
            config.Add(new XAttribute("DL", DashLength));
            config.Add(new XAttribute("SL", SpaceLength));
            config.Add(new XAttribute("I", Invert ? 1 : 0));
            config.Add(new XAttribute("CS", CenterSolid ? 1 : 0));
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Offset = config.GetAttrValue("O", DefaultOffset);
            DashLength = config.GetAttrValue("DL", DefaultDashLength);
            SpaceLength = config.GetAttrValue("SL", DefaultSpaceLength);
            Invert = config.GetAttrValue("I", 0) == 1 ^ map.IsMirror ^ invert;
            CenterSolid = config.GetAttrValue("CS", 0) == 1;
        }
    }
    public class SharkTeethLineStyle : RegularLineStyle, IColorStyle, IAsymLine, ISharkLIne
    {
        public override StyleType Type { get; } = StyleType.LineSharkTeeth;

        float _base;
        float _height;
        float _space;
        bool _invert;
        public float Base
        {
            get => _base;
            set
            {
                _base = value;
                StyleChanged();
            }
        }
        public float Height
        {
            get => _height;
            set
            {
                _height = value;
                StyleChanged();
            }
        }
        public float Space
        {
            get => _space;
            set
            {
                _space = value;
                StyleChanged();
            }
        }
        public bool Invert
        {
            get => _invert;
            set
            {
                _invert = value;
                StyleChanged();
            }
        }
        public SharkTeethLineStyle(Color color, float baseValue, float height, float space) : base(color, 0)
        {
            Base = baseValue;
            Height = height;
            Space = space;
            Invert = true;
        }
        public override IEnumerable<MarkupStyleDash> Calculate(MarkupLine line, ILineTrajectory trajectory)
        {
            foreach (var dash in StyleHelper.CalculateDashed(trajectory, Base, Space, CalculateDashes))
            {
                var dir = dash.Angle.Direction().Turn90(true);
                var toStart = new StraightTrajectory(line.Markup.Position, dash.Position + dir * (dash.Width / 2));
                var toEnd = new StraightTrajectory(line.Markup.Position, dash.Position - dir * (dash.Width / 2));

                if (!line.Markup.Contour.Any(c => MarkupIntersect.CalculateSingle(c, toStart).IsIntersect || MarkupIntersect.CalculateSingle(c, toEnd).IsIntersect))
                    yield return dash;
            }

            IEnumerable<MarkupStyleDash> CalculateDashes(ILineTrajectory trajectory, float startT, float endT)
            {
                yield return StyleHelper.CalculateDashedDash(trajectory, Invert ? endT : startT, Invert ? startT : endT, Base, Height / (Invert ? -2 : 2), Height, Color, MaterialType.Triangle);
            }
        }

        public override RegularLineStyle CopyRegularLineStyle() => new SharkTeethLineStyle(Color, Base, Height, Space);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is SharkTeethLineStyle sharkTeethTarget)
            {
                sharkTeethTarget.Base = Base;
                sharkTeethTarget.Height = Height;
                sharkTeethTarget.Space = Space;
            }
        }
        public override List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            components.Add(AddBaseProperty(this, parent, onHover, onLeave));
            components.Add(AddHeightProperty(this, parent, onHover, onLeave));
            components.Add(AddSpaceProperty(this, parent, onHover, onLeave));
            if (!isTemplate)
                components.Add(AddInvertProperty(this, parent));

            return components;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute("B", Base));
            config.Add(new XAttribute("H", Height));
            config.Add(new XAttribute("S", Space));
            config.Add(new XAttribute("I", Invert ? 1 : 0));
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Base = config.GetAttrValue("B", DefaultSharkBaseLength);
            Height = config.GetAttrValue("H", DefaultSharkHeight);
            Space = config.GetAttrValue("S", DefaultSharkSpaceLength);
            Invert = config.GetAttrValue("I", 0) == 1 ^ map.IsMirror ^ invert;
        }
    }
}
