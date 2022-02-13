using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utilities;
using System.Collections.Generic;
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
        public override CrosswalkStyle CopyStyle() => new ExistCrosswalkStyle(Width);

        public override XElement ToXml()
        {
            var config = BaseToXml();
            Width.ToXml(config);
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
        public override void CopyTo(CrosswalkStyle target)
        {
            base.CopyTo(target);

            if (target is CustomCrosswalkStyle customTarget)
            {
                customTarget.OffsetBefore.Value = OffsetBefore;
                customTarget.OffsetAfter.Value = OffsetAfter;
            }
        }
        public override void GetUIComponents(MarkupCrosswalk crosswalk, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(crosswalk, components, parent, isTemplate);
            components.Add(AddOffsetBeforeProperty(this, parent));
            components.Add(AddOffsetAfterProperty(this, parent));
        }
        protected BoolListPropertyPanel AddParallelProperty(IParallel parallelStyle, UIComponent parent)
        {
            var parallelProperty = ComponentPool.Get<BoolListPropertyPanel>(parent, nameof(parallelStyle.Parallel));
            parallelProperty.Text = Localize.StyleOption_ParallelToLanes;
            parallelProperty.Init(Localize.StyleOption_No, Localize.StyleOption_Yes);
            parallelProperty.SelectedObject = parallelStyle.Parallel;
            parallelProperty.OnSelectObjectChanged += (value) => parallelStyle.Parallel.Value = value;

            return parallelProperty;
        }

        protected FloatPropertyPanel AddOffsetBeforeProperty(CustomCrosswalkStyle customStyle, UIComponent parent)
        {
            var offsetBeforeProperty = AddOffsetProperty(parent, nameof(customStyle.OffsetBefore));
            offsetBeforeProperty.Text = Localize.StyleOption_OffsetBefore;
            offsetBeforeProperty.Value = customStyle.OffsetBefore;
            offsetBeforeProperty.OnValueChanged += (float value) => customStyle.OffsetBefore.Value = value;

            return offsetBeforeProperty;
        }
        protected FloatPropertyPanel AddOffsetAfterProperty(CustomCrosswalkStyle customStyle, UIComponent parent)
        {
            var offsetAfterProperty = AddOffsetProperty(parent, nameof(customStyle.OffsetAfter));
            offsetAfterProperty.Text = Localize.StyleOption_OffsetAfter;
            offsetAfterProperty.Value = customStyle.OffsetAfter;
            offsetAfterProperty.OnValueChanged += (float value) => customStyle.OffsetAfter.Value = value;

            return offsetAfterProperty;
        }
        protected FloatPropertyPanel AddOffsetBetweenProperty(IDoubleCrosswalk doubleStyle, UIComponent parent)
        {
            var offsetAfterProperty = AddOffsetProperty(parent, nameof(doubleStyle.Offset), 0.1f);
            offsetAfterProperty.Text = Localize.StyleOption_OffsetBetween;
            offsetAfterProperty.Value = doubleStyle.Offset;
            offsetAfterProperty.OnValueChanged += (float value) => doubleStyle.Offset.Value = value;

            return offsetAfterProperty;
        }
        protected FloatPropertyPanel AddOffsetProperty(UIComponent parent, string name, float minValue = 0f)
        {
            var offsetProperty = ComponentPool.Get<FloatPropertyPanel>(parent, name);
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = 0.1f;
            offsetProperty.CheckMin = true;
            offsetProperty.MinValue = minValue;
            offsetProperty.WheelTip = Settings.ShowToolTip;
            offsetProperty.Init();

            return offsetProperty;
        }
        protected FloatPropertyPanel AddLineWidthProperty(ILinedCrosswalk linedStyle, UIComponent parent)
        {
            var widthProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(linedStyle.LineWidth));
            widthProperty.Text = Localize.StyleOption_LineWidth;
            widthProperty.UseWheel = true;
            widthProperty.WheelStep = 0.1f;
            widthProperty.WheelTip = Settings.ShowToolTip;
            widthProperty.CheckMin = true;
            widthProperty.MinValue = 0.05f;
            widthProperty.Init();
            widthProperty.Value = linedStyle.LineWidth;
            widthProperty.OnValueChanged += (float value) => linedStyle.LineWidth.Value = value;

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
            OffsetBefore.ToXml(config);
            OffsetAfter.ToXml(config);
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
        public override void CopyTo(CrosswalkStyle target)
        {
            base.CopyTo(target);
            if (target is ILinedCrosswalk linedTarget)
                linedTarget.LineWidth.Value = LineWidth;
        }
        public override void GetUIComponents(MarkupCrosswalk crosswalk, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(crosswalk, components, parent, isTemplate);
            components.Add(AddLineWidthProperty(this, parent));
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            LineWidth.ToXml(config);
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
        public PropertyBoolValue UseSecondColor { get; }
        public PropertyColorValue SecondColor { get; }

        public PropertyValue<bool> UseGap { get; }
        public PropertyValue<float> GapLength { get; }
        public PropertyValue<int> GapPeriod { get; }


        protected override float GetVisibleWidth(MarkupCrosswalk crosswalk) => GetLengthCoef(Width, crosswalk);
        protected float GetLengthCoef(float length, MarkupCrosswalk crosswalk) => length / (Parallel ? 1 : Mathf.Sin(crosswalk.CornerAndNormalAngle));

        public ZebraCrosswalkStyle(Color32 color, Color32 secondColor, bool useSecondColor, float width, float offsetBefore, float offsetAfter, float dashLength, float spaceLength, bool useGap, float gapLength, int gapPeriod, bool parallel) : base(color, width, offsetBefore, offsetAfter)
        {
            DashLength = GetDashLengthProperty(dashLength);
            SpaceLength = GetSpaceLengthProperty(spaceLength);
            Parallel = GetParallelProperty(parallel);

            UseSecondColor = GetUseSecondColorProperty(useSecondColor);
            SecondColor = GetSecondColorProperty(UseSecondColor ? secondColor : color);

            UseGap = GetUseGapProperty(useGap);
            GapLength = GetGapLengthProperty(gapLength);
            GapPeriod = GetGapPeriodProperty(gapPeriod);
        }
        public override CrosswalkStyle CopyStyle() => new ZebraCrosswalkStyle(Color, SecondColor, UseSecondColor, Width, OffsetBefore, OffsetAfter, DashLength, SpaceLength, UseGap, GapLength, GapPeriod, Parallel);
        public override void CopyTo(CrosswalkStyle target)
        {
            base.CopyTo(target);

            if (target is ZebraCrosswalkStyle zebraTarget)
            {
                zebraTarget.UseSecondColor.Value = UseSecondColor;
                zebraTarget.SecondColor.Value = SecondColor;

                zebraTarget.UseGap.Value = UseGap;
                zebraTarget.GapLength.Value = GapLength;
                zebraTarget.GapPeriod.Value = GapPeriod;
            }

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
            var borders = crosswalk.BorderTrajectories;
            var index = 0;
            var direction = Parallel ? crosswalk.NormalDir : crosswalk.CornerDir.Turn90(true);
            var trajectory = crosswalk.GetFullTrajectory(offset, direction);

            if (!UseGap)
            {
                var dashLength = Parallel ? DashLength / coef : DashLength;
                var spaceLength = Parallel ? SpaceLength / coef : SpaceLength;

                return StyleHelper.CalculateDashed(trajectory, dashLength, spaceLength, CalculateDashes);

                IEnumerable<MarkupStylePart> CalculateDashes(ITrajectory crosswalkTrajectory, float startT, float endT)
                {
                    index += 1;
                    foreach (var part in CalculateCroswalkPart(crosswalkTrajectory, startT, endT, direction, borders, Width, DashLength, GetColor(index)))
                        yield return part;
                }
            }
            else
            {
                var groupLength = (DashLength * GapPeriod + SpaceLength * (GapPeriod - 1));
                var dashT = DashLength / groupLength;
                var spaceT = SpaceLength / groupLength;

                groupLength /= (Parallel ? coef : 1f);
                var gapLength = GapLength / (Parallel ? coef : 1f);

                return StyleHelper.CalculateDashed(trajectory, groupLength, gapLength, CalculateDashes);

                IEnumerable<MarkupStylePart> CalculateDashes(ITrajectory crosswalkTrajectory, float startT, float endT)
                {
                    index += 1;
                    for (var i = 0; i < GapPeriod; i += 1)
                    {
                        var partStartT = startT + (endT - startT) * (dashT + spaceT) * i;
                        var partEndT = partStartT + (endT - startT) * dashT;
                        foreach (var part in CalculateCroswalkPart(crosswalkTrajectory, partStartT, partEndT, direction, borders, Width, DashLength, GetColor(index)))
                            yield return part;
                    }
                }
            }
        }
        protected Color32 GetColor(int index) => UseSecondColor && index % 2 != 0 ? SecondColor : Color;

        public override void GetUIComponents(MarkupCrosswalk crosswalk, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(crosswalk, components, parent, isTemplate);

            var useSecondColor = AddUseSecondColorProperty(parent);
            var secondColor = AddSecondColorProperty(parent);
            components.Add(useSecondColor);
            components.Add(secondColor);
            useSecondColor.OnSelectObjectChanged += ChangeSecondColorVisible;
            ChangeSecondColorVisible(useSecondColor.SelectedObject);

            components.Add(AddDashLengthProperty(this, parent));
            components.Add(AddSpaceLengthProperty(this, parent));

            var useGap = AddUseGapProperty(parent);
            var gapLength = AddGapLengthProperty(parent);
            var gapPeriod = AddGapPeriodProperty(parent);
            components.Add(useGap);
            components.Add(gapLength);
            components.Add(gapPeriod);
            useGap.OnSelectObjectChanged += ChangeGapVisible;
            ChangeGapVisible(useGap.SelectedObject);

            components.Add(AddParallelProperty(this, parent));

            void ChangeSecondColorVisible(bool useSecondColor) => secondColor.isVisible = useSecondColor;
            void ChangeGapVisible(bool useGap)
            {
                gapLength.isVisible = useGap;
                gapPeriod.isVisible = useGap;
            }
        }

        protected BoolListPropertyPanel AddUseSecondColorProperty(UIComponent parent)
        {
            var useSecondColorProperty = ComponentPool.GetBefore<BoolListPropertyPanel>(parent, nameof(Color), nameof(UseSecondColor));
            useSecondColorProperty.Text = Localize.StyleOption_ColorCount;
            useSecondColorProperty.Init(Localize.StyleOption_ColorCountOne, Localize.StyleOption_ColorCountTwo, false);
            useSecondColorProperty.SelectedObject = UseSecondColor;
            useSecondColorProperty.OnSelectObjectChanged += (value) => UseSecondColor.Value = value;

            return useSecondColorProperty;
        }
        protected ColorAdvancedPropertyPanel AddSecondColorProperty(UIComponent parent)
        {
            var colorProperty = ComponentPool.GetAfter<ColorAdvancedPropertyPanel>(parent, nameof(Color), nameof(SecondColor));
            colorProperty.Text = Localize.StyleOption_Color;
            colorProperty.WheelTip = Settings.ShowToolTip;
            colorProperty.Init();
            colorProperty.Value = SecondColor;
            colorProperty.OnValueChanged += (Color32 color) => SecondColor.Value = color;

            return colorProperty;
        }

        protected BoolListPropertyPanel AddUseGapProperty(UIComponent parent)
        {
            var useGapProperty = ComponentPool.Get<BoolListPropertyPanel>(parent, nameof(UseGap));
            useGapProperty.Text = Localize.StyleOption_UseGap;
            useGapProperty.Init();
            useGapProperty.SelectedObject = UseGap;
            useGapProperty.OnSelectObjectChanged += (value) => UseGap.Value = value;

            return useGapProperty;
        }
        protected FloatPropertyPanel AddGapLengthProperty(UIComponent parent)
        {
            var gapLengthProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(GapLength));
            gapLengthProperty.Text = Localize.StyleOption_GapLength;
            gapLengthProperty.UseWheel = true;
            gapLengthProperty.WheelStep = 0.1f;
            gapLengthProperty.WheelTip = Settings.ShowToolTip;
            gapLengthProperty.CheckMin = true;
            gapLengthProperty.MinValue = 0.1f;
            gapLengthProperty.Init();
            gapLengthProperty.Value = GapLength;
            gapLengthProperty.OnValueChanged += (float value) => GapLength.Value = value;

            return gapLengthProperty;
        }
        protected IntPropertyPanel AddGapPeriodProperty(UIComponent parent)
        {
            var gapPeriodProperty = ComponentPool.Get<IntPropertyPanel>(parent, nameof(GapPeriod));
            gapPeriodProperty.Text = Localize.StyleOption_GapPeriod;
            gapPeriodProperty.UseWheel = true;
            gapPeriodProperty.WheelStep = 1;
            gapPeriodProperty.WheelTip = Settings.ShowToolTip;
            gapPeriodProperty.CheckMin = true;
            gapPeriodProperty.MinValue = DefaultCrosswalkLineCount;
            gapPeriodProperty.Init();
            gapPeriodProperty.Value = GapPeriod;
            gapPeriodProperty.OnValueChanged += (int value) => GapPeriod.Value = value;

            return gapPeriodProperty;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            DashLength.ToXml(config);
            SpaceLength.ToXml(config);
            Parallel.ToXml(config);
            UseSecondColor.ToXml(config);
            SecondColor.ToXml(config);
            UseGap.ToXml(config);
            GapLength.ToXml(config);
            GapPeriod.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            base.FromXml(config, map, invert);
            DashLength.FromXml(config, DefaultDashLength);
            SpaceLength.FromXml(config, DefaultSpaceLength);
            Parallel.FromXml(config, true);
            UseSecondColor.FromXml(config, false);
            SecondColor.FromXml(config, DefaultColor);
            UseGap.FromXml(config, false);
            GapLength.FromXml(config, DefaultSpaceLength);
            GapPeriod.FromXml(config, DefaulCrosswalkGapPeriod);
        }
    }
    public class DoubleZebraCrosswalkStyle : ZebraCrosswalkStyle, ICrosswalkStyle, IDoubleCrosswalk
    {
        public override StyleType Type => StyleType.CrosswalkDoubleZebra;

        public PropertyValue<float> Offset { get; }

        public DoubleZebraCrosswalkStyle(Color32 color, Color32 secondColor, bool useSecondColor, float width, float offsetBefore, float offsetAfter, float dashLength, float spaceLength, bool useGap, float gapLength, int gapPeriod, bool parallel, float offset) :
            base(color, secondColor, useSecondColor, width, offsetBefore, offsetAfter, dashLength, spaceLength, useGap, gapLength, gapPeriod, parallel)
        {
            Offset = GetOffsetProperty(offset);
        }
        protected override float GetVisibleWidth(MarkupCrosswalk crosswalk) => GetLengthCoef(Width * 2 + Offset, crosswalk);
        public override CrosswalkStyle CopyStyle() => new DoubleZebraCrosswalkStyle(Color, SecondColor, UseSecondColor, Width, OffsetBefore, OffsetAfter, DashLength, SpaceLength, UseGap, GapLength, GapPeriod, Parallel, Offset);
        public override void CopyTo(CrosswalkStyle target)
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
            var direction = Parallel ? crosswalk.NormalDir : crosswalk.CornerDir.Turn90(true);
            var borders = crosswalk.BorderTrajectories;
            var index = 0;

            var trajectoryFirst = crosswalk.GetFullTrajectory(middleOffset - deltaOffset, direction);
            var trajectorySecond = crosswalk.GetFullTrajectory(middleOffset + deltaOffset, direction);

            if (!UseGap)
            {
                var dashLength = Parallel ? DashLength / coef : DashLength;
                var spaceLength = Parallel ? SpaceLength / coef : SpaceLength;

                foreach (var dash in StyleHelper.CalculateDashed(trajectoryFirst, dashLength, spaceLength, CalculateDashes))
                    yield return dash;

                index = 0;

                foreach (var dash in StyleHelper.CalculateDashed(trajectorySecond, dashLength, spaceLength, CalculateDashes))
                    yield return dash;

                IEnumerable<MarkupStylePart> CalculateDashes(ITrajectory crosswalkTrajectory, float startT, float endT)
                {
                    index += 1;
                    foreach (var part in CalculateCroswalkPart(crosswalkTrajectory, startT, endT, direction, borders, Width, DashLength, GetColor(index)))
                        yield return part;
                }
            }
            else
            {
                var groupLength = (DashLength * GapPeriod + SpaceLength * (GapPeriod - 1));
                var dashT = DashLength / groupLength;
                var spaceT = SpaceLength / groupLength;

                groupLength /= (Parallel ? coef : 1f);
                var gapLength = GapLength / (Parallel ? coef : 1f);

                foreach (var dash in StyleHelper.CalculateDashed(trajectoryFirst, groupLength, gapLength, CalculateDashes))
                    yield return dash;

                index = 0;

                foreach (var dash in StyleHelper.CalculateDashed(trajectorySecond, groupLength, gapLength, CalculateDashes))
                    yield return dash;

                IEnumerable<MarkupStylePart> CalculateDashes(ITrajectory crosswalkTrajectory, float startT, float endT)
                {
                    index += 1;
                    for (var i = 0; i < GapPeriod; i += 1)
                    {
                        var partStartT = startT + (endT - startT) * (dashT + spaceT) * i;
                        var partEndT = partStartT + (endT - startT) * dashT;
                        foreach (var part in CalculateCroswalkPart(crosswalkTrajectory, partStartT, partEndT, direction, borders, Width, DashLength, GetColor(index)))
                            yield return part;
                    }
                }
            }
        }

        public override void GetUIComponents(MarkupCrosswalk crosswalk, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(crosswalk, components, parent, isTemplate);
            var offsetBetween = AddOffsetBetweenProperty(this, parent);
            if (parent.Find(nameof(OffsetBefore)) is UIComponent offsetBefore)
                offsetBetween.zOrder = offsetBefore.zOrder + 1;
            components.Add(offsetBetween);
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
            Offset.FromXml(config, DefaultCrosswalkOffset);
        }
    }
    public class ParallelSolidLinesCrosswalkStyle : LinedCrosswalkStyle, ICrosswalkStyle
    {
        public override StyleType Type => StyleType.CrosswalkParallelSolidLines;

        public ParallelSolidLinesCrosswalkStyle(Color32 color, float width, float offsetBefore, float offsetAfter, float lineWidth) :
            base(color, width, offsetBefore, offsetAfter, lineWidth)
        { }

        public override CrosswalkStyle CopyStyle() => new ParallelSolidLinesCrosswalkStyle(Color, Width, OffsetBefore, OffsetAfter, LineWidth);

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

            MarkupStylePart CalculateDashes(ITrajectory dashTrajectory) => StyleHelper.CalculateSolidPart(dashTrajectory, 0, LineWidth, Color);
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

        public override CrosswalkStyle CopyStyle() => new ParallelDashedLinesCrosswalkStyle(Color, Width, OffsetBefore, OffsetAfter, LineWidth, DashLength, SpaceLength);

        public override void CopyTo(CrosswalkStyle target)
        {
            base.CopyTo(target);
            if (target is IDashedLine dashedTarget)
            {
                dashedTarget.DashLength.Value = DashLength;
                dashedTarget.SpaceLength.Value = SpaceLength;
            }
        }
        public override void GetUIComponents(MarkupCrosswalk crosswalk, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(crosswalk, components, parent, isTemplate);
            components.Add(AddDashLengthProperty(this, parent));
            components.Add(AddSpaceLengthProperty(this, parent));
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

        public override CrosswalkStyle CopyStyle() => new LadderCrosswalkStyle(Color, Width, OffsetBefore, OffsetAfter, DashLength, SpaceLength, LineWidth);
        public override void CopyTo(CrosswalkStyle target)
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
                => CalculateCroswalkPart(crosswalkTrajectory, startT, endT, direction, borders, Width, DashLength, Color);
        }

        public override void GetUIComponents(MarkupCrosswalk crosswalk, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(crosswalk, components, parent, isTemplate);
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
    public class SolidCrosswalkStyle : CustomCrosswalkStyle, ICrosswalkStyle
    {
        public override StyleType Type => StyleType.CrosswalkSolid;

        public SolidCrosswalkStyle(Color32 color, float width, float offsetBefore, float offsetAfter) : base(color, width, offsetBefore, offsetAfter) { }

        public override CrosswalkStyle CopyStyle() => new SolidCrosswalkStyle(Color, Width, OffsetBefore, OffsetAfter);
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
                    foreach (var dash in CalculateCroswalkPart(trajectory, startT + squareT * (j - 1), startT + squareT * j, direction, borders, SquareSide, SquareSide, Color))
                        yield return dash;
                }
            }
        }

        public override CrosswalkStyle CopyStyle() => new ChessBoardCrosswalkStyle(Color, OffsetBefore, OffsetAfter, SquareSide, LineCount, Invert);
        public override void CopyTo(CrosswalkStyle target)
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

        public override void GetUIComponents(MarkupCrosswalk crosswalk, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(crosswalk, components, parent, isTemplate);
            components.Add(AddSquareSideProperty(parent));
            components.Add(AddLineCountProperty(parent));
            if (!isTemplate)
                components.Add(AddInvertProperty(this, parent));
        }
        protected FloatPropertyPanel AddSquareSideProperty(UIComponent parent)
        {
            var squareSideProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(SquareSide));
            squareSideProperty.Text = Localize.StyleOption_SquareSide;
            squareSideProperty.UseWheel = true;
            squareSideProperty.WheelStep = 0.1f;
            squareSideProperty.WheelTip = Settings.ShowToolTip;
            squareSideProperty.CheckMin = true;
            squareSideProperty.MinValue = 0.1f;
            squareSideProperty.Init();
            squareSideProperty.Value = SquareSide;
            squareSideProperty.OnValueChanged += (float value) => SquareSide.Value = value;

            return squareSideProperty;
        }
        protected IntPropertyPanel AddLineCountProperty(UIComponent parent)
        {
            var lineCountProperty = ComponentPool.Get<IntPropertyPanel>(parent, nameof(LineCount));
            lineCountProperty.Text = Localize.StyleOption_LineCount;
            lineCountProperty.UseWheel = true;
            lineCountProperty.WheelStep = 1;
            lineCountProperty.WheelTip = Settings.ShowToolTip;
            lineCountProperty.CheckMin = true;
            lineCountProperty.MinValue = DefaultCrosswalkLineCount;
            lineCountProperty.Init();
            lineCountProperty.Value = LineCount;
            lineCountProperty.OnValueChanged += (int value) => LineCount.Value = value;

            return lineCountProperty;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            SquareSide.ToXml(config);
            LineCount.ToXml(config);
            Invert.ToXml(config);
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
