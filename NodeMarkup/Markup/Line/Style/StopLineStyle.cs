using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Utilities;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public class SolidStopLineStyle : StopLineStyle, IStopLine
    {
        public override StyleType Type => StyleType.StopLineSolid;

        public SolidStopLineStyle(Color32 color, float width) : base(color, width) { }

        protected override IStyleData Calculate(MarkupStopLine stopLine, ITrajectory trajectory, MarkupLOD lod)
        {
            var offset = ((stopLine.Start.Direction + stopLine.End.Direction) / -2).normalized * (Width / 2);
            return new MarkupStyleParts(StyleHelper.CalculateSolid(trajectory, lod, CalculateDashes));

            MarkupStylePart CalculateDashes(ITrajectory dashTrajectory) => StyleHelper.CalculateSolidPart(dashTrajectory, offset, offset, Width, Color);
        }

        public override StopLineStyle CopyLineStyle() => new SolidStopLineStyle(Color, Width);
    }
    public class DoubleSolidStopLineStyle : SolidStopLineStyle, IStopLine, IDoubleLine
    {
        public override StyleType Type => StyleType.StopLineDoubleSolid;

        public PropertyValue<float> Offset { get; }

        public DoubleSolidStopLineStyle(Color32 color, float width, float offset) : base(color, width)
        {
            Offset = GetOffsetProperty(offset);
        }
        protected override IStyleData Calculate(MarkupStopLine stopLine, ITrajectory trajectory, MarkupLOD lod)
        {
            var offsetNormal = ((stopLine.Start.Direction + stopLine.End.Direction) / -2).normalized;
            var offsetLeft = offsetNormal * (Width / 2);
            var offsetRight = offsetNormal * (Width / 2 + 2 * Offset);

            return new MarkupStyleParts(StyleHelper.CalculateSolid(trajectory, lod, CalculateDashes));

            IEnumerable<MarkupStylePart> CalculateDashes(ITrajectory dashTrajectory)
            {
                yield return StyleHelper.CalculateSolidPart(dashTrajectory, offsetLeft, offsetLeft, Width, Color);
                yield return StyleHelper.CalculateSolidPart(dashTrajectory, offsetRight, offsetRight, Width, Color);
            }
        }

        public override StopLineStyle CopyLineStyle() => new DoubleSolidStopLineStyle(Color, Width, Offset);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is IDoubleLine doubleTarget)
            {
                doubleTarget.Offset.Value = Offset;
            }
        }

        public override void GetUIComponents(MarkupStopLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);
            components.Add(AddOffsetProperty(this, parent));
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            Offset.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Offset.FromXml(config, DefaultDoubleOffset);
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

        protected override IStyleData Calculate(MarkupStopLine stopLine, ITrajectory trajectory, MarkupLOD lod)
        {
            if (!CheckDashedLod(lod, Width, DashLength))
                return new MarkupStyleParts();

            var offset = ((stopLine.Start.Direction + stopLine.End.Direction) / -2).normalized * (Width / 2);
            return new MarkupStyleParts(StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength, CalculateDashes));

            IEnumerable<MarkupStylePart> CalculateDashes(ITrajectory dashTrajectory, float startT, float endT)
            {
                yield return StyleHelper.CalculateDashedPart(dashTrajectory, startT, endT, DashLength, offset, offset, Width, Color);
            }
        }

        public override StopLineStyle CopyLineStyle() => new DashedStopLineStyle(Color, Width, DashLength, SpaceLength);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is IDashedLine dashedTarget)
            {
                dashedTarget.DashLength.Value = DashLength;
                dashedTarget.SpaceLength.Value = SpaceLength;
            }
        }
        public override void GetUIComponents(MarkupStopLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
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
    public class DoubleDashedStopLineStyle : DashedStopLineStyle, IStopLine, IDoubleLine
    {
        public override StyleType Type { get; } = StyleType.StopLineDoubleDashed;

        public PropertyValue<float> Offset { get; }
        public DoubleDashedStopLineStyle(Color32 color, float width, float dashLength, float spaceLength, float offset) : base(color, width, dashLength, spaceLength)
        {
            Offset = GetOffsetProperty(offset);
        }
        public override StopLineStyle CopyLineStyle() => new DoubleDashedStopLineStyle(Color, Width, DashLength, SpaceLength, Offset);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is IDoubleLine doubleTarget)
                doubleTarget.Offset.Value = Offset;
        }

        protected override IStyleData Calculate(MarkupStopLine stopLine, ITrajectory trajectory, MarkupLOD lod)
        {
            if (!CheckDashedLod(lod, Width, DashLength))
                return new MarkupStyleParts();

            var offsetNormal = ((stopLine.Start.Direction + stopLine.End.Direction) / -2).normalized;
            var offsetLeft = offsetNormal * (Width / 2);
            var offsetRight = offsetNormal * (Width / 2 + 2 * Offset);

            return new MarkupStyleParts(StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength, CalculateDashes));

            IEnumerable<MarkupStylePart> CalculateDashes(ITrajectory dashTrajectory, float startT, float endT)
            {
                yield return StyleHelper.CalculateDashedPart(dashTrajectory, startT, endT, DashLength, offsetLeft, offsetLeft, Width, Color);
                yield return StyleHelper.CalculateDashedPart(dashTrajectory, startT, endT, DashLength, offsetRight, offsetRight, Width, Color);
            }
        }

        public override void GetUIComponents(MarkupStopLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);
            components.Add(AddOffsetProperty(this, parent));
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            Offset.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Offset.FromXml(config, DefaultDoubleOffset);
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


        protected override IStyleData Calculate(MarkupStopLine stopLine, ITrajectory trajectory, MarkupLOD lod)
        {
            var offsetNormal = ((stopLine.Start.Direction + stopLine.End.Direction) / -2).normalized;
            var solidOffset = offsetNormal * (Width / 2);
            var dashedOffset = offsetNormal * (Width / 2 + 2 * Offset);

            var dashes = new List<MarkupStylePart>();
            dashes.AddRange(StyleHelper.CalculateSolid(trajectory, lod, CalculateSolidDash));
            if (CheckDashedLod(lod, Width, DashLength))
                dashes.AddRange(StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength, CalculateDashedDash));

            return new MarkupStyleParts(dashes);

            MarkupStylePart CalculateSolidDash(ITrajectory lineTrajectory) => StyleHelper.CalculateSolidPart(lineTrajectory, solidOffset, solidOffset, Width, Color);

            IEnumerable<MarkupStylePart> CalculateDashedDash(ITrajectory lineTrajectory, float startT, float endT)
            {
                yield return StyleHelper.CalculateDashedPart(lineTrajectory, startT, endT, DashLength, dashedOffset, dashedOffset, Width, Color);
            }
        }

        public override StopLineStyle CopyLineStyle() => new SolidAndDashedStopLineStyle(Color, Width, DashLength, SpaceLength, Offset);
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
        }
        public override void GetUIComponents(MarkupStopLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);
            components.Add(AddDashLengthProperty(this, parent));
            components.Add(AddSpaceLengthProperty(this, parent));
            components.Add(AddOffsetProperty(this, parent));
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Offset.ToXml(config);
            DashLength.ToXml(config);
            SpaceLength.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            Offset.FromXml(config, DefaultDoubleOffset);
            DashLength.FromXml(config, DefaultDashLength);
            SpaceLength.FromXml(config, DefaultSpaceLength);
        }
    }
    public class SharkTeethStopLineStyle : StopLineStyle, IColorStyle, ISharkLine
    {
        public override StyleType Type { get; } = StyleType.StopLineSharkTeeth;
        protected override float LodWidth => 0.5f;

        public PropertyValue<float> Base { get; }
        public PropertyValue<float> Height { get; }
        public PropertyValue<float> Space { get; }
        public SharkTeethStopLineStyle(Color32 color, float baseValue, float height, float space) : base(color, 0)
        {
            Base = GetBaseProperty(baseValue);
            Height = GetHeightProperty(height);
            Space = GetSpaceProperty(space);
        }
        protected override IStyleData Calculate(MarkupStopLine stopLine, ITrajectory trajectory, MarkupLOD lod)
        {
            if (!CheckDashedLod(lod, Base, Height))
                return new MarkupStyleParts();

            var styleData = new MarkupStyleParts(StyleHelper.CalculateDashed(trajectory, Base, Space, CalculateDashes));
            foreach (var dash in styleData)
                dash.MaterialType = MaterialType.Triangle;

            return styleData;
        }

        private IEnumerable<MarkupStylePart> CalculateDashes(ITrajectory lineTrajectory, float startT, float endT)
        {
            yield return StyleHelper.CalculateDashedPart(lineTrajectory, startT, endT, Base, Height / -2, Height, Color);
        }

        public override StopLineStyle CopyLineStyle() => new SharkTeethStopLineStyle(Color, Base, Height, Space);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is SharkTeethStopLineStyle sharkTeethTarget)
            {
                sharkTeethTarget.Base.Value = Base;
                sharkTeethTarget.Height.Value = Height;
                sharkTeethTarget.Space.Value = Space;
            }
        }
        public override void GetUIComponents(MarkupStopLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);
            components.Add(AddBaseProperty(this, parent));
            components.Add(AddHeightProperty(this, parent));
            components.Add(AddSpaceProperty(this, parent));
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            Base.ToXml(config);
            Height.ToXml(config);
            Space.ToXml(config);
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
