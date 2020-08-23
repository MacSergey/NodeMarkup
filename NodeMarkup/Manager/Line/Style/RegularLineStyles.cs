using ColossalFramework.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
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
    public class DoubleSolidLineStyle : SolidLineStyle, IRegularLine, IDoubleLine
    {
        public override StyleType Type { get; } = StyleType.LineDoubleSolid;

        float _offset;
        public float Offset
        {
            get => _offset;
            set
            {
                _offset = value;
                StyleChanged();
            }
        }

        public DoubleSolidLineStyle(Color color, float width, float offset) : base(color, width)
        {
            Offset = offset;
        }

        public override RegularLineStyle CopyRegularLineStyle() => new DoubleSolidLineStyle(Color, Width, Offset);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is IDoubleLine doubleTarget)
            {
                doubleTarget.Offset = Offset;
            }
        }

        protected override IEnumerable<MarkupStyleDash> CalculateDashes(ILineTrajectory trajectory)
        {
            yield return StyleHelper.CalculateSolidDash(trajectory, Offset, Width, Color);
            yield return StyleHelper.CalculateSolidDash(trajectory, -Offset, Width, Color);
        }
        public override List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            components.Add(AddOffsetProperty(this, parent, onHover, onLeave));
            return components;
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute("O", Offset));
            return config;
        }
        public override void FromXml(XElement config)
        {
            base.FromXml(config);
            Offset = config.GetAttrValue("O", DefaultOffset);
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
        public override void FromXml(XElement config)
        {
            base.FromXml(config);
            DashLength = config.GetAttrValue("DL", DefaultDashLength);
            SpaceLength = config.GetAttrValue("SL", DefaultSpaceLength);
        }
    }
    public class DoubleDashedLineStyle : DashedLineStyle, IRegularLine, IDoubleLine
    {
        public override StyleType Type { get; } = StyleType.LineDoubleDashed;

        float _offset;
        public float Offset
        {
            get => _offset;
            set
            {
                _offset = value;
                StyleChanged();
            }
        }

        public DoubleDashedLineStyle(Color color, float width, float dashLength, float spaceLength, float offset) : base(color, width, dashLength, spaceLength)
        {
            Offset = offset;
        }

        public override RegularLineStyle CopyRegularLineStyle() => new DoubleDashedLineStyle(Color, Width, DashLength, SpaceLength, Offset);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is IDoubleLine doubleTarget)
            {
                doubleTarget.Offset = Offset;
            }
        }

        protected override IEnumerable<MarkupStyleDash> CalculateDashes(ILineTrajectory trajectory, float startT, float endT)
        {
            yield return StyleHelper.CalculateDashedDash(trajectory, startT, endT, DashLength, Offset, Width, Color);
            yield return StyleHelper.CalculateDashedDash(trajectory, startT, endT, DashLength, -Offset, Width, Color);
        }
        public override List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            components.Add(AddOffsetProperty(this, parent, onHover, onLeave));
            return components;
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute("O", Offset));
            return config;
        }
        public override void FromXml(XElement config)
        {
            base.FromXml(config);
            Offset = config.GetAttrValue("O", DefaultOffset);
        }
    }
    public class SolidAndDashedLineStyle : RegularLineStyle, IRegularLine, IDoubleLine, IDashedLine, IAsymLine
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

        public SolidAndDashedLineStyle(Color color, float width, float dashLength, float spaceLength, float offset, bool invert, bool centerSolid) : base(color, width)
        {
            Offset = offset;
            DashLength = dashLength;
            SpaceLength = spaceLength;
            Invert = invert;
            CenterSolid = centerSolid;
        }


        public override IEnumerable<MarkupStyleDash> Calculate(MarkupLine line, ILineTrajectory trajectory)
        {
            foreach (var dash in StyleHelper.CalculateSolid(trajectory, CalculateSolidDash))
            {
                yield return dash;
            }
            foreach (var dash in StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength, CalculateDashedDash))
            {
                yield return dash;
            }
        }

        protected IEnumerable<MarkupStyleDash> CalculateSolidDash(ILineTrajectory trajectory)
        {
            var offset = CenterSolid ? 0 : Invert ? Offset : -Offset;

            yield return StyleHelper.CalculateSolidDash(trajectory, offset, Width, Color);
        }
        protected IEnumerable<MarkupStyleDash> CalculateDashedDash(ILineTrajectory trajectory, float startT, float endT)
        {
            var offset = (Invert ? -Offset : Offset) * (CenterSolid ? 2 : 1);
            yield return StyleHelper.CalculateDashedDash(trajectory, startT, endT, DashLength, offset, Width, Color);
        }
        public override RegularLineStyle CopyRegularLineStyle() => new SolidAndDashedLineStyle(Color, Width, DashLength, SpaceLength, Offset, Invert, CenterSolid);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is IDashedLine dashedTarget)
            {
                dashedTarget.DashLength = DashLength;
                dashedTarget.SpaceLength = SpaceLength;
            }
            if (target is IDoubleLine doubleTarget)
            {
                doubleTarget.Offset = Offset;
            }
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
        protected static BoolPropertyPanel AddCenterSolidProperty(SolidAndDashedLineStyle solidAndDashedStyle, UIComponent parent)
        {
            var centerSolidProperty = parent.AddUIComponent<BoolPropertyPanel>();
            centerSolidProperty.Text = Localize.LineEditor_CenterSolid;
            centerSolidProperty.Init();
            centerSolidProperty.Value = solidAndDashedStyle.CenterSolid;
            centerSolidProperty.OnValueChanged += (bool value) => solidAndDashedStyle.CenterSolid = value;
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
        public override void FromXml(XElement config)
        {
            base.FromXml(config);
            Offset = config.GetAttrValue("O", DefaultOffset);
            DashLength = config.GetAttrValue("DL", DefaultDashLength);
            SpaceLength = config.GetAttrValue("SL", DefaultSpaceLength);
            Invert = config.GetAttrValue("I", 0) == 1;
            CenterSolid = config.GetAttrValue("CS", 0) == 1;
        }
    }
}
