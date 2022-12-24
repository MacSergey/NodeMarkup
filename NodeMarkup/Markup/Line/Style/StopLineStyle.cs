using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Utilities;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using static ColossalFramework.IO.EncodedArray;

namespace NodeMarkup.Manager
{
    public class SolidStopLineStyle : StopLineStyle, IStopLine
    {
        public override StyleType Type => StyleType.StopLineSolid;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Width);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;

        public SolidStopLineStyle(Color32 color, float width) : base(color, width) { }

        protected override IStyleData CalculateImpl(MarkupStopLine stopLine, ITrajectory trajectory, MarkupLOD lod)
        {
            var offset = ((stopLine.Start.Direction + stopLine.End.Direction) / -2).normalized * (Width / 2);
            return new MarkupPartGroupData(lod, StyleHelper.CalculateSolid(trajectory, lod, CalculateDashes));

            MarkupPartData CalculateDashes(ITrajectory dashTrajectory) => StyleHelper.CalculateSolidPart(dashTrajectory, offset, offset, Width, Color);
        }

        public override StopLineStyle CopyLineStyle() => new SolidStopLineStyle(Color, Width);
    }
    public class DoubleSolidStopLineStyle : SolidStopLineStyle, IStopLine, IDoubleLine
    {
        public override StyleType Type => StyleType.StopLineDoubleSolid;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;

        public PropertyBoolValue ColorCount { get; }
        public PropertyColorValue SecondColor { get; }
        public PropertyValue<float> Offset { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(ColorCount);
                yield return nameof(Color);
                yield return nameof(SecondColor);
                yield return nameof(Width);
                yield return nameof(Offset);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;

        public DoubleSolidStopLineStyle(Color32 color, Color32 secondColor, bool useSecondColor, float width, float offset) : base(color, width)
        {
            ColorCount = GetUseSecondColorProperty(useSecondColor);
            SecondColor = GetSecondColorProperty(ColorCount ? secondColor : color);
            Offset = GetOffsetProperty(offset);
        }
        protected override IStyleData CalculateImpl(MarkupStopLine stopLine, ITrajectory trajectory, MarkupLOD lod)
        {
            var offsetNormal = ((stopLine.Start.Direction + stopLine.End.Direction) / -2).normalized;
            var offsetLeft = offsetNormal * (Width / 2);
            var offsetRight = offsetNormal * (Width / 2 + 2 * Offset);

            return new MarkupPartGroupData(lod, StyleHelper.CalculateSolid(trajectory, lod, CalculateDashes));

            IEnumerable<MarkupPartData> CalculateDashes(ITrajectory dashTrajectory)
            {
                yield return StyleHelper.CalculateSolidPart(dashTrajectory, offsetLeft, offsetLeft, Width, Color);
                yield return StyleHelper.CalculateSolidPart(dashTrajectory, offsetRight, offsetRight, Width, ColorCount ? SecondColor : Color);
            }
        }

        public override StopLineStyle CopyLineStyle() => new DoubleSolidStopLineStyle(Color, SecondColor, ColorCount, Width, Offset);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is IDoubleLine doubleTarget)
            {
                doubleTarget.Offset.Value = Offset;
                doubleTarget.SecondColor.Value = SecondColor;
                doubleTarget.ColorCount.Value = ColorCount;
            }
        }

        public override void GetUIComponents(MarkupStopLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);

            components.Add(AddUseSecondColorProperty(this, parent, true));
            components.Add(AddSecondColorProperty(this, parent, true));
            UseSecondColorChanged(this, parent, ColorCount);

            components.Add(AddOffsetProperty(this, parent, false));
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            ColorCount.ToXml(config);
            SecondColor.ToXml(config);
            Offset.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            ColorCount.FromXml(config, false);
            SecondColor.FromXml(config, DefaultColor);
            Offset.FromXml(config, DefaultDoubleOffset);
        }
    }
    public class DashedStopLineStyle : StopLineStyle, IStopLine, IDashedLine
    {
        public override StyleType Type { get; } = StyleType.StopLineDashed;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;

        public PropertyValue<float> DashLength { get; }
        public PropertyValue<float> SpaceLength { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Width);
                yield return nameof(Length);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;

        public DashedStopLineStyle(Color32 color, float width, float dashLength, float spaceLength) : base(color, width)
        {
            DashLength = GetDashLengthProperty(dashLength);
            SpaceLength = GetSpaceLengthProperty(spaceLength);
        }

        protected override IStyleData CalculateImpl(MarkupStopLine stopLine, ITrajectory trajectory, MarkupLOD lod)
        {
            if (!CheckDashedLod(lod, Width, DashLength))
                return new MarkupPartGroupData(lod);

            var offset = ((stopLine.Start.Direction + stopLine.End.Direction) / -2).normalized * (Width / 2);
            return new MarkupPartGroupData(lod, StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength, CalculateDashes));

            IEnumerable<MarkupPartData> CalculateDashes(ITrajectory dashTrajectory, float startT, float endT)
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
            components.Add(AddLengthProperty(this, parent, false));
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
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;

        public PropertyBoolValue ColorCount { get; }
        public PropertyColorValue SecondColor { get; }
        public PropertyValue<float> Offset { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(ColorCount);
                yield return nameof(Color);
                yield return nameof(SecondColor);
                yield return nameof(Width);
                yield return nameof(Offset);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;

        public DoubleDashedStopLineStyle(Color32 color, Color32 secondColor, bool useSecondColor, float width, float dashLength, float spaceLength, float offset) : base(color, width, dashLength, spaceLength)
        {
            ColorCount = GetUseSecondColorProperty(useSecondColor);
            SecondColor = GetSecondColorProperty(ColorCount ? secondColor : color);
            Offset = GetOffsetProperty(offset);
        }
        public override StopLineStyle CopyLineStyle() => new DoubleDashedStopLineStyle(Color, SecondColor, ColorCount, Width, DashLength, SpaceLength, Offset);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is IDoubleLine doubleTarget)
            {
                doubleTarget.Offset.Value = Offset;
                doubleTarget.SecondColor.Value = SecondColor;
                doubleTarget.ColorCount.Value = ColorCount;
            }
        }

        protected override IStyleData CalculateImpl(MarkupStopLine stopLine, ITrajectory trajectory, MarkupLOD lod)
        {
            if (!CheckDashedLod(lod, Width, DashLength))
                return new MarkupPartGroupData(lod);

            var offsetNormal = ((stopLine.Start.Direction + stopLine.End.Direction) / -2).normalized;
            var offsetLeft = offsetNormal * (Width / 2);
            var offsetRight = offsetNormal * (Width / 2 + 2 * Offset);

            return new MarkupPartGroupData(lod, StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength, CalculateDashes));

            IEnumerable<MarkupPartData> CalculateDashes(ITrajectory dashTrajectory, float startT, float endT)
            {
                yield return StyleHelper.CalculateDashedPart(dashTrajectory, startT, endT, DashLength, offsetLeft, offsetLeft, Width, Color);
                yield return StyleHelper.CalculateDashedPart(dashTrajectory, startT, endT, DashLength, offsetRight, offsetRight, Width, ColorCount ? SecondColor : Color);
            }
        }

        public override void GetUIComponents(MarkupStopLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);

            components.Add(AddUseSecondColorProperty(this, parent, true));
            components.Add(AddSecondColorProperty(this, parent, true));
            UseSecondColorChanged(this, parent, ColorCount);

            components.Add(AddOffsetProperty(this, parent, false));
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            ColorCount.ToXml(config);
            SecondColor.ToXml(config);
            Offset.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            ColorCount.FromXml(config, false);
            SecondColor.FromXml(config, DefaultColor);
            Offset.FromXml(config, DefaultDoubleOffset);
        }
    }
    public class SolidAndDashedStopLineStyle : StopLineStyle, IStopLine, IDoubleLine, IDashedLine
    {
        public override StyleType Type => StyleType.StopLineSolidAndDashed;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;

        public PropertyBoolValue ColorCount { get; }
        public PropertyColorValue SecondColor { get; }
        public PropertyValue<float> Offset { get; }
        public PropertyValue<float> DashLength { get; }
        public PropertyValue<float> SpaceLength { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(ColorCount);
                yield return nameof(Color);
                yield return nameof(SecondColor);
                yield return nameof(Width);
                yield return nameof(Offset);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;

        public SolidAndDashedStopLineStyle(Color32 color, Color32 secondColor, bool useSecondColor, float width, float dashLength, float spaceLength, float offset) : base(color, width)
        {
            ColorCount = GetUseSecondColorProperty(useSecondColor);
            SecondColor = GetSecondColorProperty(ColorCount ? secondColor : color);
            Offset = GetOffsetProperty(offset);
            DashLength = GetDashLengthProperty(dashLength);
            SpaceLength = GetSpaceLengthProperty(spaceLength);
        }


        protected override IStyleData CalculateImpl(MarkupStopLine stopLine, ITrajectory trajectory, MarkupLOD lod)
        {
            var offsetNormal = ((stopLine.Start.Direction + stopLine.End.Direction) / -2).normalized;
            var solidOffset = offsetNormal * (Width / 2);
            var dashedOffset = offsetNormal * (Width / 2 + 2 * Offset);

            var dashes = new List<MarkupPartData>();
            dashes.AddRange(StyleHelper.CalculateSolid(trajectory, lod, CalculateSolidDash));
            if (CheckDashedLod(lod, Width, DashLength))
                dashes.AddRange(StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength, CalculateDashedDash));

            return new MarkupPartGroupData(lod, dashes);

            MarkupPartData CalculateSolidDash(ITrajectory lineTrajectory) => StyleHelper.CalculateSolidPart(lineTrajectory, solidOffset, solidOffset, Width, Color);

            IEnumerable<MarkupPartData> CalculateDashedDash(ITrajectory lineTrajectory, float startT, float endT)
            {
                yield return StyleHelper.CalculateDashedPart(lineTrajectory, startT, endT, DashLength, dashedOffset, dashedOffset, Width, ColorCount ? SecondColor : Color);
            }
        }

        public override StopLineStyle CopyLineStyle() => new SolidAndDashedStopLineStyle(Color, SecondColor, ColorCount, Width, DashLength, SpaceLength, Offset);
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
                doubleTarget.ColorCount.Value = ColorCount;
            }
        }
        public override void GetUIComponents(MarkupStopLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);

            components.Add(AddUseSecondColorProperty(this, parent, true));
            components.Add(AddSecondColorProperty(this, parent, true));
            UseSecondColorChanged(this, parent, ColorCount);

            components.Add(AddLengthProperty(this, parent, false));
            components.Add(AddOffsetProperty(this, parent, false));
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            ColorCount.ToXml(config);
            SecondColor.ToXml(config);
            Offset.ToXml(config);
            DashLength.ToXml(config);
            SpaceLength.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            ColorCount.FromXml(config, false);
            SecondColor.FromXml(config, DefaultColor);
            Offset.FromXml(config, DefaultDoubleOffset);
            DashLength.FromXml(config, DefaultDashLength);
            SpaceLength.FromXml(config, DefaultSpaceLength);
        }
    }
    public class SharkTeethStopLineStyle : StopLineStyle, IColorStyle, ISharkLine
    {
        public override StyleType Type { get; } = StyleType.StopLineSharkTeeth;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;
        protected override float LodWidth => 0.5f;

        public PropertyValue<float> Base { get; }
        public PropertyValue<float> Height { get; }
        public PropertyValue<float> Space { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Triangle);
                yield return nameof(Space);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;

        public SharkTeethStopLineStyle(Color32 color, float baseValue, float height, float space) : base(color, 0)
        {
            Base = GetBaseProperty(baseValue);
            Height = GetHeightProperty(height);
            Space = GetSpaceProperty(space);
        }
        protected override IStyleData CalculateImpl(MarkupStopLine stopLine, ITrajectory trajectory, MarkupLOD lod)
        {
            if (!CheckDashedLod(lod, Base, Height))
                return new MarkupPartGroupData(lod);

            var styleData = new MarkupPartGroupData(lod, StyleHelper.CalculateDashed(trajectory, Base, Space, CalculateDashes));
            foreach (var dash in styleData)
                dash.Material = RenderHelper.MaterialLib[MaterialType.Triangle];

            return styleData;
        }

        private IEnumerable<MarkupPartData> CalculateDashes(ITrajectory lineTrajectory, float startT, float endT)
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
            components.Add(AddTriangleProperty(this, parent, false));
            components.Add(AddSpaceProperty(this, parent, false));
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

    public abstract class StopLine3DStyle : StopLineStyle, IWidthStyle, I3DLine
    {
        protected abstract MaterialType MaterialType { get; }
        public PropertyValue<float> Elevation { get; }

        public StopLine3DStyle(float width, float elevation) : base(default, width)
        {
            Elevation = GetElevationProperty(elevation);
        }
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is I3DLine line3DTarget)
                line3DTarget.Elevation.Value = Elevation;
        }

        protected override IStyleData CalculateImpl(MarkupStopLine line, ITrajectory trajectory, MarkupLOD lod) => new MarkupLineMeshData(lod, trajectory, Width, Elevation, MaterialType.Pavement);

        public override void GetUIComponents(MarkupStopLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);
            components.Add(AddElevationProperty(this, parent, false));
        }

        public override XElement ToXml()
        {
            var config = BaseToXml();
            Width.ToXml(config);
            Elevation.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            Width.FromXml(config, Default3DWidth);
            Elevation.FromXml(config, Default3DHeigth);
        }
    }
    public class PavementStopLineStyle : StopLine3DStyle
    {
        public override StyleType Type { get; } = StyleType.StopLinePavement;
        public override MarkupLOD SupportLOD => MarkupLOD.NoLOD;
        protected override MaterialType MaterialType => MaterialType.Pavement;

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Width);
                yield return nameof(Elevation);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;

        public PavementStopLineStyle(float width, float elevation) : base(width, elevation) { }

        public override StopLineStyle CopyLineStyle() => new PavementStopLineStyle(Width, Elevation);
    }
}
