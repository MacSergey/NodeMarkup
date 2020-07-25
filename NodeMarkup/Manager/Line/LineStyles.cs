using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
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
    public interface ISimpleLine { }
    public interface IStopLine { }
    public interface IDashedLine
    {
        float DashLength { get; set; }
        float SpaceLength { get; set; }
    }
    public interface IDoubleLine
    {
        float Offset { get; set; }
    }
    public interface IAsymLine
    {
        bool Invert { get; set; }
    }

    public class SolidLineStyle : LineStyle, ISimpleLine
    {
        public override StyleType Type { get; } = StyleType.LineSolid;

        public SolidLineStyle(Color color, float width) : base(color, width) { }

        public override LineStyle CopyLineStyle() => new SolidLineStyle(Color, Width);

        public override IEnumerable<MarkupStyleDash> Calculate(Bezier3 trajectory) => CalculateSolid(trajectory, 0, CalculateDashes);
        protected virtual IEnumerable<MarkupStyleDash> CalculateDashes(Bezier3 trajectory)
        {
            yield return CalculateSolidDash(trajectory, 0f);
        }
    }
    public class DoubleSolidLineStyle : SolidLineStyle, ISimpleLine, IDoubleLine
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

        public override LineStyle CopyLineStyle() => new DoubleSolidLineStyle(Color, Width, Offset);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is IDoubleLine doubleTarget)
            {
                doubleTarget.Offset = Offset;
            }
        }

        protected override IEnumerable<MarkupStyleDash> CalculateDashes(Bezier3 trajectory)
        {
            yield return CalculateSolidDash(trajectory, Offset);
            yield return CalculateSolidDash(trajectory, -Offset);
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
            Offset = config.GetAttrValue("O", DefaultOffser);
        }
    }
    public class DashedLineStyle : LineStyle, ISimpleLine, IDashedLine
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

        public override LineStyle CopyLineStyle() => new DashedLineStyle(Color, Width, DashLength, SpaceLength);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is IDashedLine dashedTarget)
            {
                dashedTarget.DashLength = DashLength;
                dashedTarget.SpaceLength = SpaceLength;
            }
        }

        public override IEnumerable<MarkupStyleDash> Calculate(Bezier3 trajectory) => CalculateDashed(trajectory, DashLength, SpaceLength, CalculateDashes);

        protected virtual IEnumerable<MarkupStyleDash> CalculateDashes(Bezier3 trajectory, float startT, float endT)
        {
            yield return CalculateDashedDash(trajectory, startT, endT, DashLength, 0);
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
    public class DoubleDashedLineStyle : DashedLineStyle, ISimpleLine, IDoubleLine
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

        public override LineStyle CopyLineStyle() => new DoubleDashedLineStyle(Color, Width, DashLength, SpaceLength, Offset);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is IDoubleLine doubleTarget)
            {
                doubleTarget.Offset = Offset;
            }
        }

        protected override IEnumerable<MarkupStyleDash> CalculateDashes(Bezier3 trajectory, float startT, float endT)
        {
            yield return CalculateDashedDash(trajectory, startT, endT, DashLength, Offset);
            yield return CalculateDashedDash(trajectory, startT, endT, DashLength, -Offset);
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
            Offset = config.GetAttrValue("O", DefaultOffser);
        }
    }
    public class SolidAndDashedLineStyle : LineStyle, ISimpleLine, IDoubleLine, IDashedLine, IAsymLine
    {
        public override StyleType Type => StyleType.LineSolidAndDashed;

        float _offset;
        float _dashLength;
        float _spaceLength;
        bool _invert;
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

        public SolidAndDashedLineStyle(Color color, float width, float dashLength, float spaceLength, float offset, bool invert) : base(color, width)
        {
            Offset = offset;
            DashLength = dashLength;
            SpaceLength = spaceLength;
            Invert = invert;
        }


        public override IEnumerable<MarkupStyleDash> Calculate(Bezier3 trajectory)
        {
            foreach (var dash in CalculateSolid(trajectory, 0, CalculateSolidDash))
            {
                yield return dash;
            }
            foreach (var dash in CalculateDashed(trajectory, DashLength, SpaceLength, CalculateDashedDash))
            {
                yield return dash;
            }
        }

        protected IEnumerable<MarkupStyleDash> CalculateSolidDash(Bezier3 trajectory)
        {
            yield return CalculateSolidDash(trajectory, Invert ? Offset : -Offset);
        }
        protected IEnumerable<MarkupStyleDash> CalculateDashedDash(Bezier3 trajectory, float startT, float endT)
        {
            yield return CalculateDashedDash(trajectory, startT, endT, DashLength, Invert ? -Offset : Offset);
        }
        public override LineStyle CopyLineStyle() => new SolidAndDashedLineStyle(Color, Width, DashLength, SpaceLength, Offset, Invert);
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
            if(target is IAsymLine asymTarget)
            {
                asymTarget.Invert = Invert;
            }
        }
        public override List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            components.Add(AddDashLengthProperty(this, parent, onHover, onLeave));
            components.Add(AddSpaceLengthProperty(this, parent, onHover, onLeave));
            components.Add(AddOffsetProperty(this, parent, onHover, onLeave));
            components.Add(AddInvertProperty(this, parent));
            return components;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute("O", Offset));
            config.Add(new XAttribute("DL", DashLength));
            config.Add(new XAttribute("SL", SpaceLength));
            config.Add(new XAttribute("I", Invert ? 1 : 0));
            return config;
        }
        public override void FromXml(XElement config)
        {
            base.FromXml(config);
            Offset = config.GetAttrValue("O", DefaultOffser);
            DashLength = config.GetAttrValue("DL", DefaultDashLength);
            SpaceLength = config.GetAttrValue("SL", DefaultSpaceLength);
            Invert = config.GetAttrValue("I", 0) == 1;
        }
    }
    public class SolidStopLineStyle : LineStyle, IStopLine
    {
        public override StyleType Type => StyleType.StopLineSolid;

        public SolidStopLineStyle(Color32 color, float width) : base(color, width) { }

        public override IEnumerable<MarkupStyleDash> Calculate(Bezier3 trajectory)
        {
            var offset = (trajectory.a - trajectory.b).normalized * (Width / 2);

            trajectory.b = trajectory.d;
            trajectory.c = trajectory.a;

            foreach (var dash in CalculateSolid(trajectory, 0, CalculateDashes))
            {
                dash.Position += offset;
                yield return dash;
            }
        }
        protected virtual IEnumerable<MarkupStyleDash> CalculateDashes(Bezier3 trajectory)
        {
            yield return CalculateSolidDash(trajectory, 0f);
        }

        public override LineStyle CopyLineStyle() => new SolidStopLineStyle(Color, Width);
    }
    public class DashedStopLineStyle : LineStyle, IStopLine, IDashedLine
    {
        public override StyleType Type { get; } = StyleType.StopLineDashed;

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

        public DashedStopLineStyle(Color color, float width, float dashLength, float spaceLength) : base(color, width)
        {
            DashLength = dashLength;
            SpaceLength = spaceLength;
        }

        public override IEnumerable<MarkupStyleDash> Calculate(Bezier3 trajectory)
        {
            var offset = (trajectory.a - trajectory.b).normalized * (Width / 2);

            trajectory.b = trajectory.d;
            trajectory.c = trajectory.a;

            foreach (var dash in CalculateDashed(trajectory, DashLength, SpaceLength, CalculateDashes))
            {
                dash.Position += offset;
                yield return dash;
            }
        }
        protected virtual IEnumerable<MarkupStyleDash> CalculateDashes(Bezier3 trajectory, float startT, float endT)
        {
            yield return CalculateDashedDash(trajectory, startT, endT, DashLength, 0);
        }

        public override LineStyle CopyLineStyle() => new DashedStopLineStyle(Color, Width, DashLength, SpaceLength);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is IDashedLine dashedTarget)
            {
                dashedTarget.DashLength = DashLength;
                dashedTarget.SpaceLength = SpaceLength;
            }
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
}
