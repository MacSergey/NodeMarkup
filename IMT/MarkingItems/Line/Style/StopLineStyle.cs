using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.API;
using NodeMarkup.Utilities;
using NodeMarkup.Utilities.API;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public class SolidStopLineStyle : StopLineStyle, IStopLine
    {
        public override StyleType Type => StyleType.StopLineSolid;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;

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
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<float>(nameof(Width), Width);
            }
        }

        public SolidStopLineStyle(Color32 color, float width) : base(color, width) { }

        protected override IStyleData CalculateImpl(MarkingStopLine stopLine, ITrajectory trajectory, MarkingLOD lod)
        {
            var offset = ((stopLine.Start.Direction + stopLine.End.Direction) / -2).normalized * (Width / 2);
            return new MarkingPartGroupData(lod, StyleHelper.CalculateSolid(trajectory, lod, CalculateDashes));

            MarkingPartData CalculateDashes(ITrajectory dashTrajectory) => StyleHelper.CalculateSolidPart(dashTrajectory, offset, offset, Width, Color);
        }

        public override StopLineStyle CopyLineStyle() => new SolidStopLineStyle(Color, Width);
    }
    public class DoubleSolidStopLineStyle : SolidStopLineStyle, IStopLine, IDoubleLine
    {
        public override StyleType Type => StyleType.StopLineDoubleSolid;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;

        public PropertyBoolValue TwoColors { get; }
        public PropertyColorValue SecondColor { get; }
        public PropertyValue<float> Offset { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(TwoColors);
                yield return nameof(Color);
                yield return nameof(SecondColor);
                yield return nameof(Width);
                yield return nameof(Offset);
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
                yield return new StylePropertyDataProvider<float>(nameof(Offset), Offset);
            }
        }

        public DoubleSolidStopLineStyle(Color32 color, Color32 secondColor, bool useSecondColor, float width, float offset) : base(color, width)
        {
            TwoColors = GetTwoColorsProperty(useSecondColor);
            SecondColor = GetSecondColorProperty(TwoColors ? secondColor : color);
            Offset = GetOffsetProperty(offset);
        }
        protected override IStyleData CalculateImpl(MarkingStopLine stopLine, ITrajectory trajectory, MarkingLOD lod)
        {
            var offsetNormal = ((stopLine.Start.Direction + stopLine.End.Direction) / -2).normalized;
            var offsetLeft = offsetNormal * (Width / 2);
            var offsetRight = offsetNormal * (Width / 2 + 2 * Offset);

            return new MarkingPartGroupData(lod, StyleHelper.CalculateSolid(trajectory, lod, CalculateDashes));

            IEnumerable<MarkingPartData> CalculateDashes(ITrajectory dashTrajectory)
            {
                yield return StyleHelper.CalculateSolidPart(dashTrajectory, offsetLeft, offsetLeft, Width, Color);
                yield return StyleHelper.CalculateSolidPart(dashTrajectory, offsetRight, offsetRight, Width, TwoColors ? SecondColor : Color);
            }
        }

        public override StopLineStyle CopyLineStyle() => new DoubleSolidStopLineStyle(Color, SecondColor, TwoColors, Width, Offset);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is IDoubleLine doubleTarget)
            {
                doubleTarget.Offset.Value = Offset;
                doubleTarget.SecondColor.Value = SecondColor;
                doubleTarget.TwoColors.Value = TwoColors;
            }
        }

        public override void GetUIComponents(MarkingStopLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);

            components.Add(AddUseSecondColorProperty(this, parent, true));
            components.Add(AddSecondColorProperty(this, parent, true));
            UseSecondColorChanged(this, parent, TwoColors);

            components.Add(AddOffsetProperty(this, parent, false));
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            TwoColors.ToXml(config);
            SecondColor.ToXml(config);
            Offset.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            TwoColors.FromXml(config, false);
            SecondColor.FromXml(config, DefaultColor);
            Offset.FromXml(config, DefaultDoubleOffset);
        }
    }
    public class DashedStopLineStyle : StopLineStyle, IStopLine, IDashedLine
    {
        public override StyleType Type { get; } = StyleType.StopLineDashed;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;

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
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<float>(nameof(Width), Width);
                yield return new StylePropertyDataProvider<float>(nameof(DashLength), DashLength);
                yield return new StylePropertyDataProvider<float>(nameof(SpaceLength), SpaceLength);
            }
        }

        public DashedStopLineStyle(Color32 color, float width, float dashLength, float spaceLength) : base(color, width)
        {
            DashLength = GetDashLengthProperty(dashLength);
            SpaceLength = GetSpaceLengthProperty(spaceLength);
        }

        protected override IStyleData CalculateImpl(MarkingStopLine stopLine, ITrajectory trajectory, MarkingLOD lod)
        {
            if (!CheckDashedLod(lod, Width, DashLength))
                return new MarkingPartGroupData(lod);

            var offset = ((stopLine.Start.Direction + stopLine.End.Direction) / -2).normalized * (Width / 2);
            return new MarkingPartGroupData(lod, StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength, CalculateDashes));

            IEnumerable<MarkingPartData> CalculateDashes(ITrajectory dashTrajectory, float startT, float endT)
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
        public override void GetUIComponents(MarkingStopLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
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
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            DashLength.FromXml(config, DefaultDashLength);
            SpaceLength.FromXml(config, DefaultSpaceLength);
        }
    }
    public class DoubleDashedStopLineStyle : DashedStopLineStyle, IStopLine, IDoubleLine
    {
        public override StyleType Type { get; } = StyleType.StopLineDoubleDashed;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;

        public PropertyBoolValue TwoColors { get; }
        public PropertyColorValue SecondColor { get; }
        public PropertyValue<float> Offset { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(TwoColors);
                yield return nameof(Color);
                yield return nameof(SecondColor);
                yield return nameof(Width);
                yield return nameof(Offset);
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
            }
        }

        public DoubleDashedStopLineStyle(Color32 color, Color32 secondColor, bool useSecondColor, float width, float dashLength, float spaceLength, float offset) : base(color, width, dashLength, spaceLength)
        {
            TwoColors = GetTwoColorsProperty(useSecondColor);
            SecondColor = GetSecondColorProperty(TwoColors ? secondColor : color);
            Offset = GetOffsetProperty(offset);
        }
        public override StopLineStyle CopyLineStyle() => new DoubleDashedStopLineStyle(Color, SecondColor, TwoColors, Width, DashLength, SpaceLength, Offset);
        public override void CopyTo(LineStyle target)
        {
            base.CopyTo(target);
            if (target is IDoubleLine doubleTarget)
            {
                doubleTarget.Offset.Value = Offset;
                doubleTarget.SecondColor.Value = SecondColor;
                doubleTarget.TwoColors.Value = TwoColors;
            }
        }

        protected override IStyleData CalculateImpl(MarkingStopLine stopLine, ITrajectory trajectory, MarkingLOD lod)
        {
            if (!CheckDashedLod(lod, Width, DashLength))
                return new MarkingPartGroupData(lod);

            var offsetNormal = ((stopLine.Start.Direction + stopLine.End.Direction) / -2).normalized;
            var offsetLeft = offsetNormal * (Width / 2);
            var offsetRight = offsetNormal * (Width / 2 + 2 * Offset);

            return new MarkingPartGroupData(lod, StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength, CalculateDashes));

            IEnumerable<MarkingPartData> CalculateDashes(ITrajectory dashTrajectory, float startT, float endT)
            {
                yield return StyleHelper.CalculateDashedPart(dashTrajectory, startT, endT, DashLength, offsetLeft, offsetLeft, Width, Color);
                yield return StyleHelper.CalculateDashedPart(dashTrajectory, startT, endT, DashLength, offsetRight, offsetRight, Width, TwoColors ? SecondColor : Color);
            }
        }

        public override void GetUIComponents(MarkingStopLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);

            components.Add(AddUseSecondColorProperty(this, parent, true));
            components.Add(AddSecondColorProperty(this, parent, true));
            UseSecondColorChanged(this, parent, TwoColors);

            components.Add(AddOffsetProperty(this, parent, false));
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            TwoColors.ToXml(config);
            SecondColor.ToXml(config);
            Offset.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            TwoColors.FromXml(config, false);
            SecondColor.FromXml(config, DefaultColor);
            Offset.FromXml(config, DefaultDoubleOffset);
        }
    }
    public class SolidAndDashedStopLineStyle : StopLineStyle, IStopLine, IDoubleLine, IDashedLine
    {
        public override StyleType Type => StyleType.StopLineSolidAndDashed;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;

        public PropertyBoolValue TwoColors { get; }
        public PropertyColorValue SecondColor { get; }
        public PropertyValue<float> Offset { get; }
        public PropertyValue<float> DashLength { get; }
        public PropertyValue<float> SpaceLength { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(TwoColors);
                yield return nameof(Color);
                yield return nameof(SecondColor);
                yield return nameof(Width);
                yield return nameof(Offset);
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
            }
        }

        public SolidAndDashedStopLineStyle(Color32 color, Color32 secondColor, bool useSecondColor, float width, float dashLength, float spaceLength, float offset) : base(color, width)
        {
            TwoColors = GetTwoColorsProperty(useSecondColor);
            SecondColor = GetSecondColorProperty(TwoColors ? secondColor : color);
            Offset = GetOffsetProperty(offset);
            DashLength = GetDashLengthProperty(dashLength);
            SpaceLength = GetSpaceLengthProperty(spaceLength);
        }


        protected override IStyleData CalculateImpl(MarkingStopLine stopLine, ITrajectory trajectory, MarkingLOD lod)
        {
            var offsetNormal = ((stopLine.Start.Direction + stopLine.End.Direction) / -2).normalized;
            var solidOffset = offsetNormal * (Width / 2);
            var dashedOffset = offsetNormal * (Width / 2 + 2 * Offset);

            var dashes = new List<MarkingPartData>();
            dashes.AddRange(StyleHelper.CalculateSolid(trajectory, lod, CalculateSolidDash));
            if (CheckDashedLod(lod, Width, DashLength))
                dashes.AddRange(StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength, CalculateDashedDash));

            return new MarkingPartGroupData(lod, dashes);

            MarkingPartData CalculateSolidDash(ITrajectory lineTrajectory) => StyleHelper.CalculateSolidPart(lineTrajectory, solidOffset, solidOffset, Width, Color);

            IEnumerable<MarkingPartData> CalculateDashedDash(ITrajectory lineTrajectory, float startT, float endT)
            {
                yield return StyleHelper.CalculateDashedPart(lineTrajectory, startT, endT, DashLength, dashedOffset, dashedOffset, Width, TwoColors ? SecondColor : Color);
            }
        }

        public override StopLineStyle CopyLineStyle() => new SolidAndDashedStopLineStyle(Color, SecondColor, TwoColors, Width, DashLength, SpaceLength, Offset);
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
        }
        public override void GetUIComponents(MarkingStopLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(line, components, parent, isTemplate);

            components.Add(AddUseSecondColorProperty(this, parent, true));
            components.Add(AddSecondColorProperty(this, parent, true));
            UseSecondColorChanged(this, parent, TwoColors);

            components.Add(AddLengthProperty(this, parent, false));
            components.Add(AddOffsetProperty(this, parent, false));
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            TwoColors.ToXml(config);
            SecondColor.ToXml(config);
            Offset.ToXml(config);
            DashLength.ToXml(config);
            SpaceLength.ToXml(config);
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
        }
    }
    public class SharkTeethStopLineStyle : StopLineStyle, IColorStyle, ISharkLine
    {
        public override StyleType Type { get; } = StyleType.StopLineSharkTeeth;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;
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
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<float>(nameof(Width), Width);
                yield return new StylePropertyDataProvider<float>(nameof(Base), Base);
                yield return new StylePropertyDataProvider<float>(nameof(Height), Height);
                yield return new StylePropertyDataProvider<float>(nameof(Space), Space);
            }
        }

        public SharkTeethStopLineStyle(Color32 color, float baseValue, float height, float space) : base(color, 0)
        {
            Base = GetBaseProperty(baseValue);
            Height = GetHeightProperty(height);
            Space = GetSpaceProperty(space);
        }
        protected override IStyleData CalculateImpl(MarkingStopLine stopLine, ITrajectory trajectory, MarkingLOD lod)
        {
            if (!CheckDashedLod(lod, Base, Height))
                return new MarkingPartGroupData(lod);

            var styleData = new MarkingPartGroupData(lod, StyleHelper.CalculateDashed(trajectory, Base, Space, CalculateDashes));
            foreach (var dash in styleData)
                dash.Material = RenderHelper.MaterialLib[MaterialType.Triangle];

            return styleData;
        }

        private IEnumerable<MarkingPartData> CalculateDashes(ITrajectory lineTrajectory, float startT, float endT)
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
        public override void GetUIComponents(MarkingStopLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
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
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
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

        protected override IStyleData CalculateImpl(MarkingStopLine line, ITrajectory trajectory, MarkingLOD lod) => new MarkingLineMeshData(lod, trajectory, Width, Elevation, MaterialType.Pavement);

        public override void GetUIComponents(MarkingStopLine line, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
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
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            Width.FromXml(config, Default3DWidth);
            Elevation.FromXml(config, Default3DHeigth);
        }
    }
    public class PavementStopLineStyle : StopLine3DStyle
    {
        public override StyleType Type { get; } = StyleType.StopLinePavement;
        public override MarkingLOD SupportLOD => MarkingLOD.NoLOD;
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
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<float>(nameof(Width), Width);
                yield return new StylePropertyDataProvider<float>(nameof(Elevation), Elevation);
            }
        }

        public PavementStopLineStyle(float width, float elevation) : base(width, elevation) { }

        public override StopLineStyle CopyLineStyle() => new PavementStopLineStyle(Width, Elevation);
    }
}
