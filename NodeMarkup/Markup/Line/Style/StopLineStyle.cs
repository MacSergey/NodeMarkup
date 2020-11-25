using ColossalFramework.Math;
using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public class SolidStopLineStyle : StopLineStyle, IStopLine
    {
        public override StyleType Type => StyleType.StopLineSolid;

        public SolidStopLineStyle(Color32 color, float width) : base(color, width) { }

        protected override IEnumerable<MarkupStyleDash> Calculate(MarkupStopLine stopLine, ILineTrajectory trajectory)
        {
            var offset = ((stopLine.Start.Direction + stopLine.End.Direction) / -2).normalized * (Width / 2);
            return StyleHelper.CalculateSolid(trajectory, CalculateDashes);

            IEnumerable<MarkupStyleDash> CalculateDashes(ILineTrajectory dashTrajectory)
            {
                yield return StyleHelper.CalculateSolidDash(dashTrajectory, offset, offset, Width, Color);
            }
        }

        public override StopLineStyle CopyStopLineStyle() => new SolidStopLineStyle(Color, Width);
    }
    public class DoubleSolidStopLineStyle : SolidStopLineStyle, IStopLine, IDoubleLine
    {
        public override StyleType Type => StyleType.StopLineDoubleSolid;

        public PropertyValue<float> Offset { get; }

        public DoubleSolidStopLineStyle(Color32 color, float width, float offset) : base(color, width)
        {
            Offset = GetOffsetProperty(offset);
        }
        protected override IEnumerable<MarkupStyleDash> Calculate(MarkupStopLine stopLine, ILineTrajectory trajectory)
        {
            var offsetNormal = ((stopLine.Start.Direction + stopLine.End.Direction) / -2).normalized;
            var offsetLeft = offsetNormal * (Width / 2);
            var offsetRight = offsetNormal * (Width / 2 + 2 * Offset);

            return StyleHelper.CalculateSolid(trajectory, CalculateDashes);

            IEnumerable<MarkupStyleDash> CalculateDashes(ILineTrajectory dashTrajectory)
            {
                yield return StyleHelper.CalculateSolidDash(dashTrajectory, offsetLeft, offsetLeft, Width, Color);
                yield return StyleHelper.CalculateSolidDash(dashTrajectory, offsetRight, offsetRight, Width, Color);
            }
        }

        public override StopLineStyle CopyStopLineStyle() => new DoubleSolidStopLineStyle(Color, Width, Offset);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is IDoubleLine doubleTarget)
            {
                doubleTarget.Offset.Value = Offset;
            }
        }

        public override List<EditorItem> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            components.Add(AddOffsetProperty(this, parent, onHover, onLeave));
            return components;
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(Offset.ToXml());
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Offset.FromXml(config, DefaultOffset);
        }
    }
    public class DashedStopLineStyle : StopLineStyle, IStopLine, IDashedLine
    {
        public override StyleType Type { get; } = StyleType.StopLineDashed;

        public PropertyValue<float> DashLength { get; }
        public PropertyValue<float> SpaceLength { get; }

        public DashedStopLineStyle(Color32 color, float width, float dashLength, float spaceLength) : base(color, width)
        {
            DashLength = GetDashLengthProperty(dashLength);
            SpaceLength = GetSpaceLengthProperty(spaceLength);
        }

        protected override IEnumerable<MarkupStyleDash> Calculate(MarkupStopLine stopLine, ILineTrajectory trajectory)
        {
            var offset = ((stopLine.Start.Direction + stopLine.End.Direction) / -2).normalized * (Width / 2);
            return StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength, CalculateDashes);

            IEnumerable<MarkupStyleDash> CalculateDashes(ILineTrajectory dashTrajectory, float startT, float endT)
            {
                yield return StyleHelper.CalculateDashedDash(dashTrajectory, startT, endT, DashLength, offset, offset, Width, Color);
            }
        }

        public override StopLineStyle CopyStopLineStyle() => new DashedStopLineStyle(Color, Width, DashLength, SpaceLength);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is IDashedLine dashedTarget)
            {
                dashedTarget.DashLength.Value = DashLength;
                dashedTarget.SpaceLength.Value = SpaceLength;
            }
        }
        public override List<EditorItem> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            components.Add(AddDashLengthProperty(this, parent, onHover, onLeave));
            components.Add(AddSpaceLengthProperty(this, parent, onHover, onLeave));
            return components;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(DashLength.ToXml());
            config.Add(SpaceLength.ToXml());
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            DashLength.FromXml(config, DefaultDashLength);
            SpaceLength.FromXml(config, DefaultSpaceLength);
        }
    }
    public class DoubleDashedStopLineStyle : DashedStopLineStyle, IStopLine, IDoubleLine
    {
        public override StyleType Type { get; } = StyleType.StopLineDoubleDashed;

        public PropertyValue<float> Offset { get; }
        public DoubleDashedStopLineStyle(Color32 color, float width, float dashLength, float spaceLength, float offset) : base(color, width, dashLength, spaceLength)
        {
            Offset = GetOffsetProperty(offset);
        }
        public override StopLineStyle CopyStopLineStyle() => new DoubleDashedStopLineStyle(Color, Width, DashLength, SpaceLength, Offset);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is IDoubleLine doubleTarget)
                doubleTarget.Offset.Value = Offset;
        }

        protected override IEnumerable<MarkupStyleDash> Calculate(MarkupStopLine stopLine, ILineTrajectory trajectory)
        {
            var offsetNormal = ((stopLine.Start.Direction + stopLine.End.Direction) / -2).normalized;
            var offsetLeft = offsetNormal * (Width / 2);
            var offsetRight = offsetNormal * (Width / 2 + 2 * Offset);

            return StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength, CalculateDashes);

            IEnumerable<MarkupStyleDash> CalculateDashes(ILineTrajectory dashTrajectory, float startT, float endT)
            {
                yield return StyleHelper.CalculateDashedDash(dashTrajectory, startT, endT, DashLength, offsetLeft, offsetLeft, Width, Color);
                yield return StyleHelper.CalculateDashedDash(dashTrajectory, startT, endT, DashLength, offsetRight, offsetRight, Width, Color);
            }
        }

        public override List<EditorItem> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            components.Add(AddOffsetProperty(this, parent, onHover, onLeave));
            return components;
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(Offset.ToXml());
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Offset.FromXml(config, DefaultOffset);
        }
    }
    public class SolidAndDashedStopLineStyle : StopLineStyle, IStopLine, IDoubleLine, IDashedLine
    {
        public override StyleType Type => StyleType.StopLineSolidAndDashed;

        public PropertyValue<float> Offset { get; }
        public PropertyValue<float> DashLength { get; }
        public PropertyValue<float> SpaceLength { get; }

        public SolidAndDashedStopLineStyle(Color32 color, float width, float dashLength, float spaceLength, float offset) : base(color, width)
        {
            Offset = GetOffsetProperty(offset);
            DashLength = GetDashLengthProperty(dashLength);
            SpaceLength = GetSpaceLengthProperty(spaceLength);
        }


        protected override IEnumerable<MarkupStyleDash> Calculate(MarkupStopLine stopLine, ILineTrajectory trajectory)
        {
            var offsetNormal = ((stopLine.Start.Direction + stopLine.End.Direction) / -2).normalized;
            var solidOffset = offsetNormal * (Width / 2);
            var dashedOffset = offsetNormal * (Width / 2 + 2 * Offset);

            foreach (var dash in StyleHelper.CalculateSolid(trajectory, CalculateSolidDash))
                yield return dash;

            foreach (var dash in StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength, CalculateDashedDash))
                yield return dash;

            IEnumerable<MarkupStyleDash> CalculateSolidDash(ILineTrajectory lineTrajectory)
            {
                yield return StyleHelper.CalculateSolidDash(lineTrajectory, solidOffset, solidOffset, Width, Color);
            }

            IEnumerable<MarkupStyleDash> CalculateDashedDash(ILineTrajectory lineTrajectory, float startT, float endT)
            {
                yield return StyleHelper.CalculateDashedDash(lineTrajectory, startT, endT, DashLength, dashedOffset, dashedOffset, Width, Color);
            }
        }

        public override StopLineStyle CopyStopLineStyle() => new SolidAndDashedStopLineStyle(Color, Width, DashLength, SpaceLength, Offset);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is IDashedLine dashedTarget)
            {
                dashedTarget.DashLength.Value = DashLength;
                dashedTarget.SpaceLength.Value = SpaceLength;
            }

            if (target is IDoubleLine doubleTarget)
                doubleTarget.Offset.Value = Offset;
        }
        public override List<EditorItem> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            components.Add(AddDashLengthProperty(this, parent, onHover, onLeave));
            components.Add(AddSpaceLengthProperty(this, parent, onHover, onLeave));
            components.Add(AddOffsetProperty(this, parent, onHover, onLeave));

            return components;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(Offset.ToXml());
            config.Add(DashLength.ToXml());
            config.Add(SpaceLength.ToXml());
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Offset.FromXml(config, DefaultOffset);
            DashLength.FromXml(config, DefaultDashLength);
            SpaceLength.FromXml(config, DefaultSpaceLength);
        }
    }
    public class SharkTeethStopLineStyle : StopLineStyle, IColorStyle, ISharkLine
    {
        public override StyleType Type { get; } = StyleType.StopLineSharkTeeth;

        public PropertyValue<float> Base { get; }
        public PropertyValue<float> Height { get; }
        public PropertyValue<float> Space { get; }
        public SharkTeethStopLineStyle(Color32 color, float baseValue, float height, float space) : base(color, 0)
        {
            Base = GetBaseProperty(baseValue);
            Height = GetHeightProperty(height);
            Space = GetSpaceProperty(space);
        }
        protected override IEnumerable<MarkupStyleDash> Calculate(MarkupStopLine stopLine, ILineTrajectory trajectory)
        {
            foreach (var dash in StyleHelper.CalculateDashed(trajectory, Base, Space, CalculateDashes))
            {
                dash.MaterialType = MaterialType.Triangle;
                yield return dash;
            }
        }
        IEnumerable<MarkupStyleDash> CalculateDashes(ILineTrajectory lineTrajectory, float startT, float endT)
        {
            yield return StyleHelper.CalculateDashedDash(lineTrajectory, startT, endT, Base, Height / -2, Height, Color);
        }

        public override StopLineStyle CopyStopLineStyle() => new SharkTeethStopLineStyle(Color, Base, Height, Space);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is SharkTeethStopLineStyle sharkTeethTarget)
            {
                sharkTeethTarget.Base.Value = Base;
                sharkTeethTarget.Height.Value = Height;
                sharkTeethTarget.Space.Value = Space;
            }
        }
        public override List<EditorItem> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, onHover, onLeave, isTemplate);
            components.Add(AddBaseProperty(this, parent, onHover, onLeave));
            components.Add(AddHeightProperty(this, parent, onHover, onLeave));
            components.Add(AddSpaceProperty(this, parent, onHover, onLeave));

            return components;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(Base.ToXml());
            config.Add(Height.ToXml());
            config.Add(Space.ToXml());
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Base.FromXml(config, DefaultSharkBaseLength);
            Height.FromXml(config, DefaultSharkHeight);
            Space.FromXml(config, DefaultSharkSpaceLength);
        }
    }
}
