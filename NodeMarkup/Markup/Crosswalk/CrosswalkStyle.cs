using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.API;
using NodeMarkup.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utilities;
using NodeMarkup.Utilities.API;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public class ExistCrosswalkStyle : CrosswalkStyle, IWidthStyle
    {
        public override StyleType Type => StyleType.CrosswalkExistent;
        public override MarkupLOD SupportLOD => 0;

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Width);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<float>(nameof(Width), Width);
            }
        }

        public override float GetTotalWidth(MarkupCrosswalk crosswalk) => Width;

        public ExistCrosswalkStyle(float width) : base(new Color32(0, 0, 0, 0), width) { }

        protected override IEnumerable<MarkupPartData> CalculateImpl(MarkupCrosswalk crosswalk, MarkupLOD lod)
        {
            yield break;
        }
        public override CrosswalkStyle CopyStyle() => new ExistCrosswalkStyle(Width);

        public override XElement ToXml()
        {
            var config = BaseToXml();
            Width.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            Width.FromXml(config, DefaultCrosswalkWidth);
        }
    }

    public abstract class CustomCrosswalkStyle : CrosswalkStyle
    {
        public PropertyValue<float> OffsetBefore { get; }
        public PropertyValue<float> OffsetAfter { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Width);
                yield return nameof(Offset);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<float>(nameof(Width), Width);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetBefore), OffsetBefore);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetAfter), OffsetAfter);
            }
        }

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
            components.Add(AddOffsetProperty(parent, false));
        }

        protected BoolListPropertyPanel AddParallelProperty(IParallel parallelStyle, UIComponent parent, bool canCollapse)
        {
            var parallelProperty = ComponentPool.Get<BoolListPropertyPanel>(parent, nameof(parallelStyle.Parallel));
            parallelProperty.Text = Localize.StyleOption_ParallelToLanes;
            parallelProperty.CanCollapse = canCollapse;
            parallelProperty.Init(Localize.StyleOption_No, Localize.StyleOption_Yes);
            parallelProperty.SelectedObject = parallelStyle.Parallel;
            parallelProperty.OnSelectObjectChanged += (value) => parallelStyle.Parallel.Value = value;

            return parallelProperty;
        }

        protected Vector2PropertyPanel AddOffsetProperty(UIComponent parent, bool canCollapse)
        {
            var offsetProperty = ComponentPool.Get<Vector2PropertyPanel>(parent, nameof(Offset));
            offsetProperty.Text = Localize.StyleOption_Offset;
            offsetProperty.FieldsWidth = 50f;
            offsetProperty.SetLabels(Localize.StyleOption_OffsetBeforeAbrv, Localize.StyleOption_OffsetAfterAbrv);
            offsetProperty.Format = Localize.NumberFormat_Meter;
            offsetProperty.UseWheel = true;
            offsetProperty.WheelStep = new Vector2(0.1f, 0.1f);
            offsetProperty.CheckMin = true;
            offsetProperty.WheelTip = Settings.ShowToolTip;
            offsetProperty.CanCollapse = canCollapse;
            offsetProperty.Init(0, 1);
            offsetProperty.Value = new Vector2(OffsetBefore, OffsetAfter);
            offsetProperty.OnValueChanged += (Vector2 value) =>
            {
                OffsetBefore.Value = value.x;
                OffsetAfter.Value = value.y;
            };

            return offsetProperty;
        }

        protected FloatPropertyPanel AddLineWidthProperty(ILinedCrosswalk linedStyle, UIComponent parent, bool canCollapse)
        {
            var widthProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(linedStyle.LineWidth));
            widthProperty.Text = Localize.StyleOption_LineWidth;
            widthProperty.Format = Localize.NumberFormat_Meter;
            widthProperty.UseWheel = true;
            widthProperty.WheelStep = 0.1f;
            widthProperty.WheelTip = Settings.ShowToolTip;
            widthProperty.CheckMin = true;
            widthProperty.MinValue = 0.05f;
            widthProperty.CanCollapse = canCollapse;
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
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            OffsetBefore.FromXml(config, DefaultCrosswalkOffset);
            OffsetAfter.FromXml(config, DefaultCrosswalkOffset);
        }
    }
    public abstract class LinedCrosswalkStyle : CustomCrosswalkStyle, ICrosswalkStyle, ILinedCrosswalk
    {
        public PropertyValue<float> LineWidth { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Width);
                yield return nameof(LineWidth);
                yield return nameof(Offset);
            }
        }
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<float>(nameof(Width), Width);
                yield return new StylePropertyDataProvider<float>(nameof(LineWidth), LineWidth);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetBefore), OffsetBefore);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetAfter), OffsetAfter);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;

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
            components.Add(AddLineWidthProperty(this, parent, false));
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            LineWidth.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            LineWidth.FromXml(config, DefaultCrosswalkOffset);
        }
    }

    public class ZebraCrosswalkStyle : CustomCrosswalkStyle, ICrosswalkStyle, IDashedCrosswalk, IParallel
    {
        public override StyleType Type => StyleType.CrosswalkZebra;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;

        public PropertyValue<float> DashLength { get; }
        public PropertyValue<float> SpaceLength { get; }
        public PropertyBoolValue Parallel { get; }
        public PropertyBoolValue TwoColors { get; }
        public PropertyColorValue SecondColor { get; }

        public PropertyValue<bool> UseGap { get; }
        public PropertyValue<float> GapLength { get; }
        public PropertyValue<int> GapPeriod { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(TwoColors);
                yield return nameof(Color);
                yield return nameof(SecondColor);
                yield return nameof(Width);
                yield return nameof(Length);
                yield return nameof(Offset);
                yield return nameof(Parallel);
                yield return nameof(Gap);
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
                yield return new StylePropertyDataProvider<float>(nameof(OffsetBefore), OffsetBefore);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetAfter), OffsetAfter);
                yield return new StylePropertyDataProvider<bool>(nameof(Parallel), Parallel);
                yield return new StylePropertyDataProvider<bool>(nameof(UseGap), UseGap);
                yield return new StylePropertyDataProvider<float>(nameof(GapLength), GapLength);
                yield return new StylePropertyDataProvider<int>(nameof(GapPeriod), GapPeriod);
            }
        }

        protected override float GetVisibleWidth(MarkupCrosswalk crosswalk) => GetLengthCoef(Width, crosswalk);
        protected float GetLengthCoef(float length, MarkupCrosswalk crosswalk) => length / (Parallel ? 1 : Mathf.Sin(crosswalk.CornerAndNormalAngle));

        public ZebraCrosswalkStyle(Color32 color, Color32 secondColor, bool useSecondColor, float width, float offsetBefore, float offsetAfter, float dashLength, float spaceLength, bool useGap, float gapLength, int gapPeriod, bool parallel) : base(color, width, offsetBefore, offsetAfter)
        {
            DashLength = GetDashLengthProperty(dashLength);
            SpaceLength = GetSpaceLengthProperty(spaceLength);
            Parallel = GetParallelProperty(parallel);

            TwoColors = GetTwoColorsProperty(useSecondColor);
            SecondColor = GetSecondColorProperty(TwoColors ? secondColor : color);

            UseGap = GetUseGapProperty(useGap);
            GapLength = GetGapLengthProperty(gapLength);
            GapPeriod = GetGapPeriodProperty(gapPeriod);
        }
        public override CrosswalkStyle CopyStyle() => new ZebraCrosswalkStyle(Color, SecondColor, TwoColors, Width, OffsetBefore, OffsetAfter, DashLength, SpaceLength, UseGap, GapLength, GapPeriod, Parallel);
        public override void CopyTo(CrosswalkStyle target)
        {
            base.CopyTo(target);

            if (target is ZebraCrosswalkStyle zebraTarget)
            {
                zebraTarget.TwoColors.Value = TwoColors;
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

        protected override IEnumerable<MarkupPartData> CalculateImpl(MarkupCrosswalk crosswalk, MarkupLOD lod)
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

                IEnumerable<MarkupPartData> CalculateDashes(ITrajectory crosswalkTrajectory, float startT, float endT)
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

                IEnumerable<MarkupPartData> CalculateDashes(ITrajectory crosswalkTrajectory, float startT, float endT)
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
        protected Color32 GetColor(int index) => TwoColors && index % 2 != 0 ? SecondColor : Color;

        public override void GetUIComponents(MarkupCrosswalk crosswalk, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(crosswalk, components, parent, isTemplate);

            components.Add(AddUseSecondColorProperty(parent, true));
            components.Add(AddSecondColorProperty(parent, true));
            TwoColorsChanged(parent, TwoColors);

            components.Add(AddLengthProperty(this, parent, false));
            components.Add(AddParallelProperty(this, parent, true));

            components.Add(AddGapProperty(parent, true));
        }

        protected BoolListPropertyPanel AddUseSecondColorProperty(UIComponent parent, bool canCollapse)
        {
            var useSecondColorProperty = ComponentPool.Get<BoolListPropertyPanel>(parent, nameof(TwoColors));
            useSecondColorProperty.Text = Localize.StyleOption_ColorCount;
            useSecondColorProperty.CanCollapse = canCollapse;
            useSecondColorProperty.Init(Localize.StyleOption_ColorCountOne, Localize.StyleOption_ColorCountTwo, false);
            useSecondColorProperty.SelectedObject = TwoColors;
            useSecondColorProperty.OnSelectObjectChanged += (value) =>
                {
                    TwoColors.Value = value;
                    TwoColorsChanged(parent, value);
                };


            return useSecondColorProperty;
        }
        protected void TwoColorsChanged(UIComponent parent, bool value)
        {
            if (parent.Find<ColorAdvancedPropertyPanel>(nameof(Color)) is ColorAdvancedPropertyPanel mainColorProperty)
            {
                mainColorProperty.Text = value ? Localize.StyleOption_MainColor : Localize.StyleOption_Color;
            }

            if (parent.Find<ColorAdvancedPropertyPanel>(nameof(SecondColor)) is ColorAdvancedPropertyPanel secondColorProperty)
            {
                secondColorProperty.IsHidden = !value;
                secondColorProperty.Text = value ? Localize.StyleOption_SecondColor : Localize.StyleOption_Color;
            }
        }


        protected ColorAdvancedPropertyPanel AddSecondColorProperty(UIComponent parent, bool canCollapse)
        {
            var colorProperty = ComponentPool.Get<ColorAdvancedPropertyPanel>(parent, nameof(SecondColor));
            colorProperty.Text = Localize.StyleOption_Color;
            colorProperty.WheelTip = Settings.ShowToolTip;
            colorProperty.CanCollapse = canCollapse;
            colorProperty.Init((GetDefault() as ZebraCrosswalkStyle)?.SecondColor);
            colorProperty.Value = SecondColor;
            colorProperty.OnValueChanged += (Color32 color) => SecondColor.Value = color;

            return colorProperty;
        }

        protected GapProperty AddGapProperty(UIComponent parent, bool canCollapse)
        {
            var gapProperty = ComponentPool.Get<GapProperty>(parent, nameof(Gap));
            gapProperty.Text = Localize.StyleOption_CrosswalkGap;
            gapProperty.CanCollapse = canCollapse;
            gapProperty.Init();
            gapProperty.UseWheel = true;
            gapProperty.WheelTip = Settings.ShowToolTip;
            gapProperty.WheelStepLength = 0.1f;
            gapProperty.CheckMinLength = true;
            gapProperty.MinLength = 0.1f;
            gapProperty.WheelStepPeriod = 1;
            gapProperty.CheckMinPeriod = true;
            gapProperty.MinPeriod = DefaultCrosswalkLineCount;
            gapProperty.Use = UseGap;
            gapProperty.Length = GapLength;
            gapProperty.Period = GapPeriod;
            gapProperty.OnValueChanged += (use, length, period) =>
            {
                UseGap.Value = use;
                GapLength.Value = length;
                GapPeriod.Value = period;
            };

            return gapProperty;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            DashLength.ToXml(config);
            SpaceLength.ToXml(config);
            Parallel.ToXml(config);
            TwoColors.ToXml(config);
            SecondColor.ToXml(config);
            UseGap.ToXml(config);
            GapLength.ToXml(config);
            GapPeriod.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            DashLength.FromXml(config, DefaultDashLength);
            SpaceLength.FromXml(config, DefaultSpaceLength);
            Parallel.FromXml(config, true);
            TwoColors.FromXml(config, false);
            SecondColor.FromXml(config, DefaultColor);
            UseGap.FromXml(config, false);
            GapLength.FromXml(config, DefaultSpaceLength);
            GapPeriod.FromXml(config, DefaulCrosswalkGapPeriod);
        }
    }
    public class DoubleZebraCrosswalkStyle : ZebraCrosswalkStyle, ICrosswalkStyle, IDoubleCrosswalk
    {
        public override StyleType Type => StyleType.CrosswalkDoubleZebra;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;

        public PropertyValue<float> OffsetBetween { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(TwoColors);
                yield return nameof(Color);
                yield return nameof(SecondColor);
                yield return nameof(Width);
                yield return nameof(Length);
                yield return nameof(OffsetBetween);
                yield return nameof(Offset);
                yield return nameof(Parallel);
                yield return nameof(Gap);
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
                yield return new StylePropertyDataProvider<float>(nameof(OffsetBefore), OffsetBefore);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetBetween), OffsetBetween);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetAfter), OffsetAfter);
                yield return new StylePropertyDataProvider<bool>(nameof(Parallel), Parallel);
                yield return new StylePropertyDataProvider<bool>(nameof(UseGap), UseGap);
                yield return new StylePropertyDataProvider<float>(nameof(GapLength), GapLength);
                yield return new StylePropertyDataProvider<int>(nameof(GapPeriod), GapPeriod);
            }
        }

        public DoubleZebraCrosswalkStyle(Color32 color, Color32 secondColor, bool useSecondColor, float width, float offsetBefore, float offsetAfter, float dashLength, float spaceLength, bool useGap, float gapLength, int gapPeriod, bool parallel, float offset) :
            base(color, secondColor, useSecondColor, width, offsetBefore, offsetAfter, dashLength, spaceLength, useGap, gapLength, gapPeriod, parallel)
        {
            OffsetBetween = GetOffsetProperty(offset);
        }
        protected override float GetVisibleWidth(MarkupCrosswalk crosswalk) => GetLengthCoef(Width * 2 + OffsetBetween, crosswalk);
        public override CrosswalkStyle CopyStyle() => new DoubleZebraCrosswalkStyle(Color, SecondColor, TwoColors, Width, OffsetBefore, OffsetAfter, DashLength, SpaceLength, UseGap, GapLength, GapPeriod, Parallel, OffsetBetween);
        public override void CopyTo(CrosswalkStyle target)
        {
            base.CopyTo(target);
            if (target is IDoubleCrosswalk doubleTarget)
                doubleTarget.OffsetBetween.Value = OffsetBetween;
        }

        protected override IEnumerable<MarkupPartData> CalculateImpl(MarkupCrosswalk crosswalk, MarkupLOD lod)
        {
            var middleOffset = GetVisibleWidth(crosswalk) / 2 + OffsetBefore;
            var deltaOffset = GetLengthCoef((Width + OffsetBetween) / 2, crosswalk);
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

                IEnumerable<MarkupPartData> CalculateDashes(ITrajectory crosswalkTrajectory, float startT, float endT)
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

                IEnumerable<MarkupPartData> CalculateDashes(ITrajectory crosswalkTrajectory, float startT, float endT)
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
            components.Add(AddOffsetBetweenProperty(parent, false));
        }

        protected FloatPropertyPanel AddOffsetBetweenProperty(UIComponent parent, bool canCollapse)
        {
            var offsetBetweenProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(OffsetBetween));
            offsetBetweenProperty.Text = Localize.StyleOption_OffsetBetween;
            offsetBetweenProperty.Format = Localize.NumberFormat_Meter;
            offsetBetweenProperty.UseWheel = true;
            offsetBetweenProperty.WheelStep = 0.1f;
            offsetBetweenProperty.CheckMin = true;
            offsetBetweenProperty.MinValue = 0.1f;
            offsetBetweenProperty.WheelTip = Settings.ShowToolTip;
            offsetBetweenProperty.CanCollapse = canCollapse;
            offsetBetweenProperty.Init();
            offsetBetweenProperty.Value = OffsetBetween;
            offsetBetweenProperty.OnValueChanged += (float value) => OffsetBetween.Value = value;

            return offsetBetweenProperty;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();
            OffsetBetween.ToXml(config);
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            OffsetBetween.FromXml(config, DefaultCrosswalkOffset);
        }
    }
    public class ParallelSolidLinesCrosswalkStyle : LinedCrosswalkStyle, ICrosswalkStyle
    {
        public override StyleType Type => StyleType.CrosswalkParallelSolidLines;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Width);
                yield return nameof(LineWidth);
                yield return nameof(Offset);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<float>(nameof(Width), Width);
                yield return new StylePropertyDataProvider<float>(nameof(LineWidth), LineWidth);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetBefore), OffsetBefore);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetAfter), OffsetAfter);
            }
        }

        public ParallelSolidLinesCrosswalkStyle(Color32 color, float width, float offsetBefore, float offsetAfter, float lineWidth) :
            base(color, width, offsetBefore, offsetAfter, lineWidth)
        { }

        public override CrosswalkStyle CopyStyle() => new ParallelSolidLinesCrosswalkStyle(Color, Width, OffsetBefore, OffsetAfter, LineWidth);

        protected override IEnumerable<MarkupPartData> CalculateImpl(MarkupCrosswalk crosswalk, MarkupLOD lod)
        {
            var middleOffset = GetVisibleWidth(crosswalk) / 2 + OffsetBefore;
            var deltaOffset = (Width - LineWidth) / 2 / Mathf.Sin(crosswalk.CornerAndNormalAngle);
            var firstTrajectory = crosswalk.GetTrajectory(middleOffset - deltaOffset);
            var secondTrajectory = crosswalk.GetTrajectory(middleOffset + deltaOffset);

            foreach (var dash in StyleHelper.CalculateSolid(firstTrajectory, lod, CalculateDashes))
                yield return dash;

            foreach (var dash in StyleHelper.CalculateSolid(secondTrajectory, lod, CalculateDashes))
                yield return dash;

            MarkupPartData CalculateDashes(ITrajectory dashTrajectory) => StyleHelper.CalculateSolidPart(dashTrajectory, 0, LineWidth, Color);
        }
    }
    public class ParallelDashedLinesCrosswalkStyle : LinedCrosswalkStyle, ICrosswalkStyle, IDashedLine
    {
        public override StyleType Type => StyleType.CrosswalkParallelDashedLines;
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
                yield return nameof(LineWidth);
                yield return nameof(Length);
                yield return nameof(Offset);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<float>(nameof(Width), Width);
                yield return new StylePropertyDataProvider<float>(nameof(LineWidth), LineWidth);
                yield return new StylePropertyDataProvider<float>(nameof(DashLength), DashLength);
                yield return new StylePropertyDataProvider<float>(nameof(SpaceLength), SpaceLength);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetBefore), OffsetBefore);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetAfter), OffsetAfter);
            }
        }

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
            components.Add(AddLengthProperty(this, parent, false));
        }

        protected override IEnumerable<MarkupPartData> CalculateImpl(MarkupCrosswalk crosswalk, MarkupLOD lod)
        {
            var middleOffset = GetVisibleWidth(crosswalk) / 2 + OffsetBefore;
            var deltaOffset = (Width - LineWidth) / 2 / Mathf.Sin(crosswalk.CornerAndNormalAngle);
            var firstTrajectory = crosswalk.GetTrajectory(middleOffset - deltaOffset);
            var secondTrajectory = crosswalk.GetTrajectory(middleOffset + deltaOffset);

            foreach (var dash in StyleHelper.CalculateDashed(firstTrajectory, DashLength, SpaceLength, CalculateDashes))
                yield return dash;

            foreach (var dash in StyleHelper.CalculateDashed(secondTrajectory, DashLength, SpaceLength, CalculateDashes))
                yield return dash;

            IEnumerable<MarkupPartData> CalculateDashes(ITrajectory dashTrajectory, float startT, float endT)
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
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            DashLength.FromXml(config, DefaultDashLength);
            SpaceLength.FromXml(config, DefaultSpaceLength);
        }
    }
    public class LadderCrosswalkStyle : ParallelSolidLinesCrosswalkStyle, ICrosswalkStyle, IDashedCrosswalk
    {
        public override StyleType Type => StyleType.CrosswalkLadder;
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
                yield return nameof(LineWidth);
                yield return nameof(Length);
                yield return nameof(Offset);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<float>(nameof(Width), Width);
                yield return new StylePropertyDataProvider<float>(nameof(LineWidth), LineWidth);
                yield return new StylePropertyDataProvider<float>(nameof(DashLength), DashLength);
                yield return new StylePropertyDataProvider<float>(nameof(SpaceLength), SpaceLength);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetBefore), OffsetBefore);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetAfter), OffsetAfter);
            }
        }

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

        protected override IEnumerable<MarkupPartData> CalculateImpl(MarkupCrosswalk crosswalk, MarkupLOD lod)
        {
            foreach (var dash in base.CalculateImpl(crosswalk, lod))
                yield return dash;

            var offset = GetVisibleWidth(crosswalk) / 2 + OffsetBefore;

            var direction = crosswalk.CornerDir.Turn90(true);
            var borders = crosswalk.BorderTrajectories;
            var width = Width - 2 * LineWidth;

            var trajectory = crosswalk.GetFullTrajectory(offset, direction);

            foreach (var dash in StyleHelper.CalculateDashed(trajectory, DashLength, SpaceLength, CalculateDashes))
                yield return dash;

            IEnumerable<MarkupPartData> CalculateDashes(ITrajectory crosswalkTrajectory, float startT, float endT)
                => CalculateCroswalkPart(crosswalkTrajectory, startT, endT, direction, borders, Width, DashLength, Color);
        }

        public override void GetUIComponents(MarkupCrosswalk crosswalk, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(crosswalk, components, parent, isTemplate);
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
    public class SolidCrosswalkStyle : CustomCrosswalkStyle, ICrosswalkStyle
    {
        public override StyleType Type => StyleType.CrosswalkSolid;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Width);
                yield return nameof(Offset);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<float>(nameof(Width), Width);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetBefore), OffsetBefore);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetAfter), OffsetAfter);
            }
        }

        public SolidCrosswalkStyle(Color32 color, float width, float offsetBefore, float offsetAfter) : base(color, width, offsetBefore, offsetAfter) { }

        public override CrosswalkStyle CopyStyle() => new SolidCrosswalkStyle(Color, Width, OffsetBefore, OffsetAfter);
        protected override float GetVisibleWidth(MarkupCrosswalk crosswalk) => Width / Mathf.Sin(crosswalk.CornerAndNormalAngle);

        protected override IEnumerable<MarkupPartData> CalculateImpl(MarkupCrosswalk crosswalk, MarkupLOD lod)
        {
            StyleHelper.GetParts(Width, 0, lod, out int count, out float partWidth);
            var partOffset = GetVisibleWidth(crosswalk) / count;
            var startOffset = partOffset / 2;
            for (var i = 0; i < count; i += 1)
            {
                var trajectory = crosswalk.GetTrajectory(startOffset + partOffset * i + OffsetBefore);
                yield return new MarkupPartData(trajectory.StartPosition, trajectory.EndPosition, trajectory.Direction, partWidth, Color, RenderHelper.MaterialLib[MaterialType.RectangleLines]);
            }
        }
    }
    public class ChessBoardCrosswalkStyle : CustomCrosswalkStyle, IColorStyle, IAsymLine
    {
        public override StyleType Type => StyleType.CrosswalkChessBoard;
        public override MarkupLOD SupportLOD => MarkupLOD.LOD0 | MarkupLOD.LOD1;

        public PropertyValue<float> SquareSide { get; }
        public PropertyValue<int> LineCount { get; }
        public PropertyBoolValue Invert { get; }

        private static Dictionary<string, int> PropertyIndicesDic { get; } = CreatePropertyIndices(PropertyIndicesList);
        private static IEnumerable<string> PropertyIndicesList
        {
            get
            {
                yield return nameof(Color);
                yield return nameof(Width);
                yield return nameof(SquareSide);
                yield return nameof(LineCount);
                yield return nameof(Offset);
                yield return nameof(Invert);
            }
        }
        public override Dictionary<string, int> PropertyIndices => PropertyIndicesDic;
        public override IEnumerable<IStylePropertyData> Properties
        {
            get
            {
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<float>(nameof(Width), Width);
                yield return new StylePropertyDataProvider<float>(nameof(SquareSide), SquareSide);
                yield return new StylePropertyDataProvider<int>(nameof(LineCount), LineCount);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetBefore), OffsetBefore);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetAfter), OffsetAfter);
                yield return new StylePropertyDataProvider<bool>(nameof(Invert), Invert);
            }
        }

        public ChessBoardCrosswalkStyle(Color32 color, float offsetBefore, float offsetAfter, float squareSide, int lineCount, bool invert) : base(color, 0, offsetBefore, offsetAfter)
        {
            SquareSide = GetSquareSideProperty(squareSide);
            LineCount = GetLineCountProperty(lineCount);
            Invert = GetInvertProperty(invert);
        }
        protected override IEnumerable<MarkupPartData> CalculateImpl(MarkupCrosswalk crosswalk, MarkupLOD lod)
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
            components.Add(AddSquareSideProperty(parent, false));
            components.Add(AddLineCountProperty(parent, false));
            if (!isTemplate)
                components.Add(AddInvertProperty(this, parent, false));
        }

        protected FloatPropertyPanel AddSquareSideProperty(UIComponent parent, bool canCollapse)
        {
            var squareSideProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(SquareSide));
            squareSideProperty.Text = Localize.StyleOption_SquareSide;
            squareSideProperty.Format = Localize.NumberFormat_Meter;
            squareSideProperty.UseWheel = true;
            squareSideProperty.WheelStep = 0.1f;
            squareSideProperty.WheelTip = Settings.ShowToolTip;
            squareSideProperty.CheckMin = true;
            squareSideProperty.MinValue = 0.1f;
            squareSideProperty.CanCollapse = canCollapse;
            squareSideProperty.Init();
            squareSideProperty.Value = SquareSide;
            squareSideProperty.OnValueChanged += (float value) => SquareSide.Value = value;

            return squareSideProperty;
        }
        protected IntPropertyPanel AddLineCountProperty(UIComponent parent, bool canCollapse)
        {
            var lineCountProperty = ComponentPool.Get<IntPropertyPanel>(parent, nameof(LineCount));
            lineCountProperty.Text = Localize.StyleOption_LineCount;
            lineCountProperty.UseWheel = true;
            lineCountProperty.WheelStep = 1;
            lineCountProperty.WheelTip = Settings.ShowToolTip;
            lineCountProperty.CheckMin = true;
            lineCountProperty.MinValue = DefaultCrosswalkLineCount;
            lineCountProperty.CanCollapse = canCollapse;
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
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            base.FromXml(config, map, invert, typeChanged);
            SquareSide.FromXml(config, DefaultCrosswalkSquareSide);
            LineCount.FromXml(config, DefaultCrosswalkLineCount);
            Invert.FromXml(config, false);
            Invert.Value ^= map.Invert ^ invert;
        }
    }
}
