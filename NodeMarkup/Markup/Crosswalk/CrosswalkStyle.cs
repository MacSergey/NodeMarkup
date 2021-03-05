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
    public class ExistCrosswalkStyle : CrosswalkStyle, IWidthStyle
    {
        public override StyleType Type => StyleType.CrosswalkExistent;
        public override float GetTotalWidth(MarkupCrosswalk crosswalk) => Width;

        public ExistCrosswalkStyle(float width) : base(new Color32(0, 0, 0, 0), width) { }

        public override IEnumerable<MarkupStylePart> Calculate(MarkupCrosswalk crosswalk, MarkupLOD lod) => new MarkupStylePart[0];
        public override CrosswalkStyle CopyCrosswalkStyle() => new ExistCrosswalkStyle(Width);

        public override XElement ToXml()
        {
            var config = BaseToXml();
            config.Add(Width.ToXml());
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            Width.FromXml(config, DefaultCrosswalkWidth);
        }
    }

    public abstract class CustomCrosswalkStyle : CrosswalkStyle
    {
        public PropertyValue<float> OffsetBefore { get; }
        public PropertyValue<float> OffsetAfter { get; }

        public override float GetTotalWidth(MarkupCrosswalk crosswalk) => OffsetBefore + GetVisibleWidth(crosswalk) + OffsetAfter;
        protected abstract float GetVisibleWidth(MarkupCrosswalk crosswalk);

        public CustomCrosswalkStyle(Color32 color, float width, float offsetBefore, float offsetAfter) : base(color, width)
        {
            OffsetBefore = GetOffsetBeforeProperty(offsetBefore);
            OffsetAfter = GetOffsetAfterProperty(offsetAfter);
        }
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);

            if (target is CustomCrosswalkStyle customTarget)
            {
                customTarget.OffsetBefore.Value = OffsetBefore;
                customTarget.OffsetAfter.Value = OffsetAfter;
            }
        }
        public override void GetUIComponents(MarkupCrosswalk crosswalk, List<EditorItem> components, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            base.GetUIComponents(crosswalk, components, parent, onHover, onLeave, isTemplate);
            components.Add(AddOffsetBeforeProperty(this, parent, onHover, onLeave));
            components.Add(AddOffsetAfterProperty(this, parent, onHover, onLeave));
        }
        protected static BoolListPropertyPanel AddParallelProperty(IParallel parallelStyle, UIComponent parent)
        {
            var parallelProperty = ComponentPool.Get<BoolListPropertyPanel>(parent);
            parallelProperty.Text = Localize.StyleOption_ParallelToLanes;
            parallelProperty.Init(Localize.StyleOption_No, Localize.StyleOption_Yes);
            parallelProperty.SelectedObject = parallelStyle.Parallel;
            parallelProperty.OnSelectObjectChanged += (value) => parallelStyle.Parallel.Value = value;
            return parallelProperty;
        }

        protected static FloatPropertyPanel AddOffsetBeforeProperty(CustomCrosswalkStyle customStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var offsetBeforeProperty = AddOffsetProperty(parent, onHover, onLeave);
            offsetBeforeProperty.Text = Localize.StyleOption_OffsetBefore;
            offsetBeforeProperty.Value = customStyle.OffsetBefore;
            offsetBeforeProperty.OnValueChanged += (float value) => customStyle.OffsetBefore.Value = value;
            return offsetBeforeProperty;
        }
        protected static FloatPropertyPanel AddOffsetAfterProperty(CustomCrosswalkStyle customStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var offsetAfterProperty = AddOffsetProperty(parent, onHover, onLeave);
            offsetAfterProperty.Text = Localize.StyleOption_OffsetAfter;
            offsetAfterProperty.Value = customStyle.OffsetAfter;
            offsetAfterProperty.OnValueChanged += (float value) => customStyle.OffsetAfter.Value = value;
            return offsetAfterProperty;
        }
        protected static FloatPropertyPanel AddOffsetBetweenProperty(IDoubleCrosswalk customStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var offsetAfterProperty = AddOffsetProperty(parent, onHover, onLeave, 0.1f);
            offsetAfterProperty.Text = Localize.StyleOption_OffsetBetween;
            offsetAfterProperty.Value = customStyle.Offset;
            offsetAfterProperty.OnValueChanged += (float value) => customStyle.Offset.Value = value;
            return offsetAfterProperty;
        }
        protected static FloatPropertyPanel AddOffsetProperty(UIComponent parent, Action onHover, Action onLeave, float minValue = 0f)
        {
            var offsetProperty = ComponentPool.Get<FloatPropertyPanel>(parent);
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = minValue;
            offsetProperty.WheelTip = Editor.WheelTip;
            offsetProperty.Init();
            AddOnHoverLeave(offsetProperty, onHover, onLeave);
            return offsetProperty;
        }
        protected FloatPropertyPanel AddLineWidthProperty(ILinedCrosswalk linedStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var widthProperty = ComponentPool.Get<FloatPropertyPanel>(parent);
            widthProperty.Text = Localize.StyleOption_LineWidth;
            widthProperty.UseWheel = true;
            widthProperty.WheelStep = 0.1f;
            widthProperty.WheelTip = Editor.WheelTip;
            widthProperty.CheckMin = true;
            widthProperty.MinValue = 0.05f;
            widthProperty.Init();
            widthProperty.Value = linedStyle.LineWidth;
            widthProperty.OnValueChanged += (float value) => linedStyle.LineWidth.Value = value;
            AddOnHoverLeave(widthProperty, onHover, onLeave);

            return widthProperty;
        }
        protected bool Cut(MarkupCrosswalk crosswalk, ITrajectory trajectory, float width, out ITrajectory cutTrajectory)
        {
            var delta = width / Mathf.Tan(crosswalk.CornerAndNormalAngle) / 2;
            if (2 * delta >= trajectory.Magnitude)
            {
                cutTrajectory = default;
                return false;
            }
            else
            {
                var startCut = trajectory.Travel(0, delta);
                var endCut = trajectory.Invert().Travel(0, delta);
                cutTrajectory = trajectory.Cut(startCut, 1 - endCut);
                return true;
            }
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(OffsetBefore.ToXml());
            config.Add(OffsetAfter.ToXml());
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            OffsetBefore.FromXml(config, DefaultCrosswalkOffset);
            OffsetAfter.FromXml(config, DefaultCrosswalkOffset);
        }
    }
    public abstract class LinedCrosswalkStyle : CustomCrosswalkStyle, ICrosswalkStyle, ILinedCrosswalk
    {
        public PropertyValue<float> LineWidth { get; }

        public LinedCrosswalkStyle(Color32 color, float width, float offsetBefore, float offsetAfter, float lineWidth) :
            base(color, width, offsetBefore, offsetAfter)
        {
            LineWidth = GetLineWidthProperty(lineWidth);
        }
        protected override float GetVisibleWidth(MarkupCrosswalk crosswalk) => Width / Mathf.Sin(crosswalk.CornerAndNormalAngle);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is ILinedCrosswalk linedTarget)
                linedTarget.LineWidth.Value = LineWidth;
        }
        public override void GetUIComponents(MarkupCrosswalk crosswalk, List<EditorItem> components, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            base.GetUIComponents(crosswalk, components, parent, onHover, onLeave, isTemplate);
            components.Add(AddLineWidthProperty(this, parent, onHover, onLeave));
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(LineWidth.ToXml());
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            LineWidth.FromXml(config, DefaultCrosswalkOffset);
        }
    }

    public class ZebraCrosswalkStyle : CustomCrosswalkStyle, ICrosswalkStyle, IDashedCrosswalk, IParallel
    {
        public override StyleType Type => StyleType.CrosswalkZebra;

        public PropertyValue<float> DashLength { get; }
        public PropertyValue<float> SpaceLength { get; }
        public PropertyBoolValue Parallel { get; }

        protected override float GetVisibleWidth(MarkupCrosswalk crosswalk) => GetLengthCoef(Width, crosswalk);
        protected float GetLengthCoef(float length, MarkupCrosswalk crosswalk) => length / (Parallel ? 1 : Mathf.Sin(crosswalk.CornerAndNormalAngle));

        public ZebraCrosswalkStyle(Color32 color, float width, float offsetBefore, float offsetAfter, float dashLength, float spaceLength, bool parallel) : base(color, width, offsetBefore, offsetAfter)
        {
            DashLength = GetDashLengthProperty(dashLength);
            SpaceLength = GetSpaceLengthProperty(spaceLength);
            Parallel = GetParallelProperty(parallel);
        }
        public override CrosswalkStyle CopyCrosswalkStyle() => new ZebraCrosswalkStyle(Color, Width, OffsetBefore, OffsetAfter, DashLength, SpaceLength, Parallel);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);

            if (target is IDashedCrosswalk dashedTarget)
            {
                dashedTarget.DashLength.Value = DashLength;
                dashedTarget.SpaceLength.Value = SpaceLength;
            }

            if (target is IParallel parallelTarget)
                parallelTarget.Parallel.Value = Parallel;
        }

        public override IEnumerable<MarkupStylePart> Calculate(MarkupCrosswalk crosswalk, MarkupLOD lod)
        {
            var offset = GetVisibleWidth(crosswalk) / 2 + OffsetBefore;

            var coef = Mathf.Sin(crosswalk.CornerAndNormalAngle);
            var dashLength = Parallel ? DashLength / coef : DashLength;
            var spaceLength = Parallel ? SpaceLength / coef : SpaceLength;
            var direction = Parallel ? crosswalk.NormalDir : crosswalk.CornerDir.Turn90(true);
            var borders = crosswalk.BorderTrajectories;

            var trajectory = crosswalk.GetFullTrajectory(offset, direction);

            return StyleHelper.CalculateDashed(trajectory, dashLength, spaceLength, CalculateDashes);

            IEnumerable<MarkupStylePart> CalculateDashes(ITrajectory crosswalkTrajectory, float startT, float endT)
                => CalculateCroswalkPart(crosswalkTrajectory, startT, endT, direction, borders, Width, DashLength);
        }

        protected void GetBaseUIComponents(MarkupCrosswalk crosswalk, List<EditorItem> components, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
            => base.GetUIComponents(crosswalk, components, parent, onHover, onLeave, isTemplate);

        public override void GetUIComponents(MarkupCrosswalk crosswalk, List<EditorItem> components, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            GetBaseUIComponents(crosswalk, components, parent, onHover, onLeave, isTemplate);
            components.Add(AddDashLengthProperty(this, parent, onHover, onLeave));
            components.Add(AddSpaceLengthProperty(this, parent, onHover, onLeave));
            components.Add(AddParallelProperty(this, parent));
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(DashLength.ToXml());
            config.Add(SpaceLength.ToXml());
            config.Add(Parallel.ToXml());
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            DashLength.FromXml(config, LineStyle.DefaultDashLength);
            SpaceLength.FromXml(config, LineStyle.DefaultSpaceLength);
            Parallel.FromXml(config, true);
        }
    }
    public class DoubleZebraCrosswalkStyle : ZebraCrosswalkStyle, ICrosswalkStyle, IDoubleCrosswalk
    {
        public override StyleType Type => StyleType.CrosswalkDoubleZebra;

        public PropertyValue<float> Offset { get; }

        public DoubleZebraCrosswalkStyle(Color32 color, float width, float offsetBefore, float offsetAfter, float dashLength, float spaceLength, bool parallel, float offset) :
            base(color, width, offsetBefore, offsetAfter, dashLength, spaceLength, parallel)
        {
            Offset = GetOffsetProperty(offset);
        }
        protected override float GetVisibleWidth(MarkupCrosswalk crosswalk) => GetLengthCoef(Width * 2 + Offset, crosswalk);
        public override CrosswalkStyle CopyCrosswalkStyle() => new DoubleZebraCrosswalkStyle(Color, Width, OffsetBefore, OffsetAfter, DashLength, SpaceLength, Parallel, Offset);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is IDoubleCrosswalk doubleTarget)
                doubleTarget.Offset.Value = Offset;
        }

        public override IEnumerable<MarkupStylePart> Calculate(MarkupCrosswalk crosswalk, MarkupLOD lod)
        {
            var middleOffset = GetVisibleWidth(crosswalk) / 2 + OffsetBefore;
            var deltaOffset = GetLengthCoef((Width + Offset) / 2, crosswalk);
            var firstOffset = -crosswalk.NormalDir * (middleOffset - deltaOffset);
            var secondOffset = -crosswalk.NormalDir * (middleOffset + deltaOffset);

            var coef = Mathf.Sin(crosswalk.CornerAndNormalAngle);
            var dashLength = Parallel ? DashLength / coef : DashLength;
            var spaceLength = Parallel ? SpaceLength / coef : SpaceLength;
            var direction = Parallel ? crosswalk.NormalDir : crosswalk.CornerDir.Turn90(true);
            var borders = crosswalk.BorderTrajectories;

            var trajectoryFirst = crosswalk.GetFullTrajectory(middleOffset - deltaOffset, direction);
            var trajectorySecond = crosswalk.GetFullTrajectory(middleOffset + deltaOffset, direction);

            foreach (var dash in StyleHelper.CalculateDashed(trajectoryFirst, dashLength, spaceLength, CalculateDashes))
                yield return dash;

            foreach (var dash in StyleHelper.CalculateDashed(trajectorySecond, dashLength, spaceLength, CalculateDashes))
                yield return dash;

            IEnumerable<MarkupStylePart> CalculateDashes(ITrajectory crosswalkTrajectory, float startT, float endT)
                => CalculateCroswalkPart(crosswalkTrajectory, startT, endT, direction, borders, Width, DashLength);
        }

        public override void GetUIComponents(MarkupCrosswalk crosswalk, List<EditorItem> components, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            GetBaseUIComponents(crosswalk, components, parent, onHover, onLeave, isTemplate);
            components.Add(AddOffsetBetweenProperty(this, parent, onHover, onLeave));
            components.Add(AddDashLengthProperty(this, parent, onHover, onLeave));
            components.Add(AddSpaceLengthProperty(this, parent, onHover, onLeave));
            components.Add(AddParallelProperty(this, parent));
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
            Offset.FromXml(config, DefaultCrosswalkOffset);
        }
    }
    public class ParallelSolidLinesCrosswalkStyle : LinedCrosswalkStyle, ICrosswalkStyle
    {
        public override StyleType Type => StyleType.CrosswalkParallelSolidLines;

        public ParallelSolidLinesCrosswalkStyle(Color32 color, float width, float offsetBefore, float offsetAfter, float lineWidth) :
            base(color, width, offsetBefore, offsetAfter, lineWidth)
        { }

        public override CrosswalkStyle CopyCrosswalkStyle() => new ParallelSolidLinesCrosswalkStyle(Color, Width, OffsetBefore, OffsetAfter, LineWidth);

        public override IEnumerable<MarkupStylePart> Calculate(MarkupCrosswalk crosswalk, MarkupLOD lod)
        {
            var middleOffset = GetVisibleWidth(crosswalk) / 2 + OffsetBefore;
            var deltaOffset = (Width - LineWidth) / 2 / Mathf.Sin(crosswalk.CornerAndNormalAngle);
            var firstTrajectory = crosswalk.GetTrajectory(middleOffset - deltaOffset);
            var secondTrajectory = crosswalk.GetTrajectory(middleOffset + deltaOffset);

            foreach (var dash in StyleHelper.CalculateSolid(firstTrajectory, lod, CalculateDashes))
                yield return dash;

            foreach (var dash in StyleHelper.CalculateSolid(secondTrajectory, lod, CalculateDashes))
                yield return dash;

            IEnumerable<MarkupStylePart> CalculateDashes(ITrajectory dashTrajectory)
            {
                yield return StyleHelper.CalculateSolidPart(dashTrajectory, 0, LineWidth, Color);
            }
        }
    }
    public class ParallelDashedLinesCrosswalkStyle : LinedCrosswalkStyle, ICrosswalkStyle, IDashedLine
    {
        public override StyleType Type => StyleType.CrosswalkParallelDashedLines;

        public PropertyValue<float> DashLength { get; }
        public PropertyValue<float> SpaceLength { get; }

        public ParallelDashedLinesCrosswalkStyle(Color32 color, float width, float offsetBefore, float offsetAfter, float lineWidth, float dashLength, float spaceLength) :
            base(color, width, offsetBefore, offsetAfter, lineWidth)
        {
            DashLength = GetDashLengthProperty(dashLength);
            SpaceLength = GetSpaceLengthProperty(spaceLength);
        }

        public override CrosswalkStyle CopyCrosswalkStyle() => new ParallelDashedLinesCrosswalkStyle(Color, Width, OffsetBefore, OffsetAfter, LineWidth, DashLength, SpaceLength);

        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is IDashedLine dashedTarget)
            {
                dashedTarget.DashLength.Value = DashLength;
                dashedTarget.SpaceLength.Value = SpaceLength;
            }
        }
        public override void GetUIComponents(MarkupCrosswalk crosswalk, List<EditorItem> components, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            base.GetUIComponents(crosswalk, components, parent, onHover, onLeave, isTemplate);
            components.Add(AddDashLengthProperty(this, parent, onHover, onLeave));
            components.Add(AddSpaceLengthProperty(this, parent, onHover, onLeave));
        }

        public override IEnumerable<MarkupStylePart> Calculate(MarkupCrosswalk crosswalk, MarkupLOD lod)
        {
            var middleOffset = GetVisibleWidth(crosswalk) / 2 + OffsetBefore;
            var deltaOffset = (Width - LineWidth) / 2 / Mathf.Sin(crosswalk.CornerAndNormalAngle);
            var firstTrajectory = crosswalk.GetTrajectory(middleOffset - deltaOffset);
            var secondTrajectory = crosswalk.GetTrajectory(middleOffset + deltaOffset);

            foreach (var dash in StyleHelper.CalculateDashed(firstTrajectory, DashLength, SpaceLength, CalculateDashes))
                yield return dash;

            foreach (var dash in StyleHelper.CalculateDashed(secondTrajectory, DashLength, SpaceLength, CalculateDashes))
                yield return dash;

            IEnumerable<MarkupStylePart> CalculateDashes(ITrajectory dashTrajectory, float startT, float endT)
            {
                yield return StyleHelper.CalculateDashedPart(dashTrajectory, startT, endT, DashLength, 0, LineWidth, Color);
            }
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
            DashLength.FromXml(config, LineStyle.DefaultDashLength);
            SpaceLength.FromXml(config, LineStyle.DefaultSpaceLength);
        }
    }
    public class LadderCrosswalkStyle : ParallelSolidLinesCrosswalkStyle, ICrosswalkStyle, IDashedCrosswalk
    {
        public override StyleType Type => StyleType.CrosswalkLadder;

        public PropertyValue<float> DashLength { get; }
        public PropertyValue<float> SpaceLength { get; }

        public LadderCrosswalkStyle(Color32 color, float width, float offsetBefore, float offsetAfter, float dashLength, float spaceLength, float lineWidth) : base(color, width, offsetBefore, offsetAfter, lineWidth)
        {
            DashLength = GetDashLengthProperty(dashLength);
            SpaceLength = GetSpaceLengthProperty(spaceLength);
        }

        public override CrosswalkStyle CopyCrosswalkStyle() => new LadderCrosswalkStyle(Color, Width, OffsetBefore, OffsetAfter, DashLength, SpaceLength, LineWidth);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);

            if (target is IDashedCrosswalk dashedTarget)
            {
                dashedTarget.DashLength.Value = DashLength;
                dashedTarget.SpaceLength.Value = SpaceLength;
            }
        }

        public override IEnumerable<MarkupStylePart> Calculate(MarkupCrosswalk crosswalk, MarkupLOD lod)
        {
            foreach (var dash in base.Calculate(crosswalk, lod))
                yield return dash;

            var offset = GetVisibleWidth(crosswalk) / 2 + OffsetBefore;

            var direction = crosswalk.CornerDir.Turn90(true);
            var borders = crosswalk.BorderTrajectories;
            var width = Width - 2 * LineWidth;

            var trajectory = crosswalk.GetFullTrajectory(offset, direction);

            foreach (var dash in StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength, CalculateDashes))
                yield return dash;

            IEnumerable<MarkupStylePart> CalculateDashes(ITrajectory crosswalkTrajectory, float startT, float endT)
                => CalculateCroswalkPart(crosswalkTrajectory, startT, endT, direction, borders, Width, DashLength);
        }

        public override void GetUIComponents(MarkupCrosswalk crosswalk, List<EditorItem> components, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            base.GetUIComponents(crosswalk, components, parent, onHover, onLeave, isTemplate);
            components.Add(AddDashLengthProperty(this, parent, onHover, onLeave));
            components.Add(AddSpaceLengthProperty(this, parent, onHover, onLeave));
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
            DashLength.FromXml(config, LineStyle.DefaultDashLength);
            SpaceLength.FromXml(config, LineStyle.DefaultSpaceLength);
        }
    }
    public class SolidCrosswalkStyle : CustomCrosswalkStyle, ICrosswalkStyle
    {
        public override StyleType Type => StyleType.CrosswalkSolid;

        public SolidCrosswalkStyle(Color32 color, float width, float offsetBefore, float offsetAfter) : base(color, width, offsetBefore, offsetAfter) { }

        public override CrosswalkStyle CopyCrosswalkStyle() => new SolidCrosswalkStyle(Color, Width, OffsetBefore, OffsetAfter);
        protected override float GetVisibleWidth(MarkupCrosswalk crosswalk) => Width / Mathf.Sin(crosswalk.CornerAndNormalAngle);

        public override IEnumerable<MarkupStylePart> Calculate(MarkupCrosswalk crosswalk, MarkupLOD lod)
        {
            StyleHelper.GetParts(Width, 0, lod, out int count, out float partWidth);
            var partOffset = GetVisibleWidth(crosswalk) / count;
            var startOffset = partOffset / 2;
            for (var i = 0; i < count; i += 1)
            {
                var trajectory = crosswalk.GetTrajectory(startOffset + partOffset * i + OffsetBefore);
                yield return new MarkupStylePart(trajectory.StartPosition, trajectory.EndPosition, trajectory.Direction, partWidth, Color);
            }
        }
    }
    public class ChessBoardCrosswalkStyle : CustomCrosswalkStyle, IColorStyle, IAsymLine
    {
        public override StyleType Type => StyleType.CrosswalkChessBoard;

        public PropertyValue<float> SquareSide { get; }
        public PropertyValue<int> LineCount { get; }
        public PropertyBoolValue Invert { get; }

        public ChessBoardCrosswalkStyle(Color32 color, float offsetBefore, float offsetAfter, float squareSide, int lineCount, bool invert) : base(color, 0, offsetBefore, offsetAfter)
        {
            SquareSide = GetSquareSideProperty(squareSide);
            LineCount = GetLineCountProperty(lineCount);
            Invert = GetInvertProperty(invert);
        }
        public override IEnumerable<MarkupStylePart> Calculate(MarkupCrosswalk crosswalk, MarkupLOD lod)
        {
            var deltaOffset = GetLengthCoef(SquareSide, crosswalk);
            var startOffset = deltaOffset / 2 + OffsetBefore;

            var direction = crosswalk.CornerDir;
            var normalDirection = direction.Turn90(true);
            var borders = crosswalk.BorderTrajectories;

            for (var i = 0; i < LineCount; i += 1)
            {
                var trajectory = crosswalk.GetFullTrajectory(startOffset + deltaOffset * i, normalDirection);
                var trajectoryLength = trajectory.Length;
                var count = (int)(trajectoryLength / SquareSide);
                var squareT = SquareSide / trajectoryLength;
                var startT = (trajectoryLength - SquareSide * count) / trajectoryLength;

                for (var j = (Invert ? i + 1 : i) % 2; j < count; j += 2)
                {
                    foreach (var dash in CalculateCroswalkPart(trajectory, startT + squareT * (j - 1), startT + squareT * j, direction, borders, SquareSide, SquareSide))
                        yield return dash;
                }
            }
        }

        public override CrosswalkStyle CopyCrosswalkStyle() => new ChessBoardCrosswalkStyle(Color, OffsetBefore, OffsetAfter, SquareSide, LineCount, Invert);
        public override void CopyTo(Style target)
        {
            base.CopyTo(target);
            if (target is ChessBoardCrosswalkStyle chessBoardTarget)
            {
                chessBoardTarget.SquareSide.Value = SquareSide;
                chessBoardTarget.LineCount.Value = LineCount;
                chessBoardTarget.Invert.Value = Invert;
            }
        }
        protected override float GetVisibleWidth(MarkupCrosswalk crosswalk) => GetLengthCoef(SquareSide * LineCount, crosswalk);
        protected float GetLengthCoef(float length, MarkupCrosswalk crosswalk) => length / Mathf.Sin(crosswalk.CornerAndNormalAngle);

        public override void GetUIComponents(MarkupCrosswalk crosswalk, List<EditorItem> components, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            base.GetUIComponents(crosswalk, components, parent, onHover, onLeave, isTemplate);
            components.Add(AddSquareSideProperty(this, parent, onHover, onLeave));
            components.Add(AddLineCountProperty(this, parent, onHover, onLeave));
            if (!isTemplate)
                components.Add(AddInvertProperty(this, parent));
        }
        protected static FloatPropertyPanel AddSquareSideProperty(ChessBoardCrosswalkStyle chessBoardStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var squareSideProperty = ComponentPool.Get<FloatPropertyPanel>(parent);
            squareSideProperty.Text = Localize.StyleOption_SquareSide;
            squareSideProperty.UseWheel = true;
            squareSideProperty.WheelStep = 0.1f;
            squareSideProperty.WheelTip = Editor.WheelTip;
            squareSideProperty.CheckMin = true;
            squareSideProperty.MinValue = 0.1f;
            squareSideProperty.Init();
            squareSideProperty.Value = chessBoardStyle.SquareSide;
            squareSideProperty.OnValueChanged += (float value) => chessBoardStyle.SquareSide.Value = value;
            AddOnHoverLeave(squareSideProperty, onHover, onLeave);
            return squareSideProperty;
        }
        protected static IntPropertyPanel AddLineCountProperty(ChessBoardCrosswalkStyle chessBoardStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var lineCountProperty = ComponentPool.Get<IntPropertyPanel>(parent);
            lineCountProperty.Text = Localize.StyleOption_LineCount;
            lineCountProperty.UseWheel = true;
            lineCountProperty.WheelStep = 1;
            lineCountProperty.WheelTip = Editor.WheelTip;
            lineCountProperty.CheckMin = true;
            lineCountProperty.MinValue = DefaultCrosswalkLineCount;
            lineCountProperty.Init();
            lineCountProperty.Value = chessBoardStyle.LineCount;
            lineCountProperty.OnValueChanged += (int value) => chessBoardStyle.LineCount.Value = value;
            AddOnHoverLeave(lineCountProperty, onHover, onLeave);
            return lineCountProperty;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(SquareSide.ToXml());
            config.Add(LineCount.ToXml());
            config.Add(Invert.ToXml());
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            SquareSide.FromXml(config, DefaultCrosswalkSquareSide);
            LineCount.FromXml(config, DefaultCrosswalkLineCount);
            Invert.FromXml(config, false);
            Invert.Value ^= map.IsMirror ^ invert;
        }
    }
}
