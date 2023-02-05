using ColossalFramework.UI;
using IMT.API;
using IMT.UI;
using IMT.Utilities;
using IMT.Utilities.API;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public class ZebraCrosswalkStyle : CustomCrosswalkStyle, ICrosswalkStyle, IDashedCrosswalk, IParallel, IEffectStyle
    {
        public override StyleType Type => StyleType.CrosswalkZebra;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;

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
                yield return nameof(Texture);
                yield return nameof(Cracks);
                yield return nameof(Voids);
#if DEBUG
                yield return nameof(RenderOnly);
                yield return nameof(Start);
                yield return nameof(End);
                yield return nameof(StartBorder);
                yield return nameof(EndBorder);
#endif
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

        protected override float GetVisibleWidth(MarkingCrosswalk crosswalk) => GetAbsoluteWidth(Width, crosswalk);
        protected override float GetAbsoluteWidth(float length, MarkingCrosswalk crosswalk) =>  Parallel ? length : length / Mathf.Sin(crosswalk.CornerAndNormalAngle);
        protected float GetRelativeWidth(float length, MarkingCrosswalk crosswalk) => Parallel ? length / Mathf.Sin(crosswalk.CornerAndNormalAngle) : length;

        public ZebraCrosswalkStyle(Color32 color, Color32 secondColor, bool useSecondColor, float width, Vector2 cracks, Vector2 voids, float texture, float offsetBefore, float offsetAfter, float dashLength, float spaceLength, bool useGap, float gapLength, int gapPeriod, bool parallel) : base(color, width, cracks, voids, texture, offsetBefore, offsetAfter)
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
        public override CrosswalkStyle CopyStyle() => new ZebraCrosswalkStyle(Color, SecondColor, TwoColors, Width, Cracks, Voids, Texture, OffsetBefore, OffsetAfter, DashLength, SpaceLength, UseGap, GapLength, GapPeriod, Parallel);
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

        protected override void CalculateImpl(MarkingCrosswalk crosswalk, MarkingLOD lod, Action<IStyleData> addData)
        {
            var offset = GetVisibleWidth(crosswalk) * 0.5f + OffsetBefore;
            var width = GetAbsoluteWidth(Width, crosswalk);

            var index = 0;
            var direction = Parallel ? crosswalk.NormalDir : crosswalk.CornerDir.Turn90(true);
            var trajectory = crosswalk.GetFullTrajectory(offset, direction);

            if (!UseGap)
            {
                if (GetContour(crosswalk, offset, width, out var contour))
                {
                    var dashLength = GetRelativeWidth(DashLength, crosswalk);
                    var spaceLength = GetRelativeWidth(SpaceLength, crosswalk);

                    var dashes = StyleHelper.CalculateDashesStraightT(trajectory, dashLength, spaceLength);
                    for (int i = 0; i < dashes.Count; i += 1)
                    {
#if DEBUG
                        bool renderOnly = RenderOnly != -1 && i != RenderOnly;
                        if (renderOnly)
                            continue;
#endif
                        CalculateCrosswalkPart(trajectory, dashes[i], direction, contour, GetColor(index++), lod, addData);
                    }
                }
            }
            else
            {
                if (GetContour(crosswalk, offset , width, out var contour))
                {
                    var groupLength = DashLength * GapPeriod + SpaceLength * (GapPeriod - 1);
                    var dashT = DashLength / groupLength;
                    var spaceT = SpaceLength / groupLength;

                    groupLength = GetRelativeWidth(groupLength, crosswalk);
                    var gapLength = GetRelativeWidth(GapLength, crosswalk);

                    foreach (var part in StyleHelper.CalculateDashesStraightT(trajectory, groupLength, gapLength))
                    {
                        for (var i = 0; i < GapPeriod; i += 1)
                        {
                            var startT = part.start + (part.end - part.start) * (dashT + spaceT) * i;
                            var endT = startT + (part.end - part.start) * dashT;
                            CalculateCrosswalkPart(trajectory, new StyleHelper.PartT(startT, endT), direction, contour, GetColor(index++), lod, addData);
                        }
                    }
                }
            }
        }
        protected Color32 GetColor(int index) => TwoColors && index % 2 != 0 ? SecondColor : Color;

        public override void GetUIComponents(MarkingCrosswalk crosswalk, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
        {
            base.GetUIComponents(crosswalk, components, parent, isTemplate);

            components.Add(AddUseSecondColorProperty(parent, true));
            components.Add(AddSecondColorProperty(parent, true));
            TwoColorsChanged(parent, TwoColors);

            components.Add(AddLengthProperty(this, parent, false));
            components.Add(AddParallelProperty(parent, true));

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
        protected BoolListPropertyPanel AddParallelProperty(UIComponent parent, bool canCollapse)
        {
            var parallelProperty = ComponentPool.Get<BoolListPropertyPanel>(parent, nameof(Parallel));
            parallelProperty.Text = Localize.StyleOption_ParallelToLanes;
            parallelProperty.CanCollapse = canCollapse;
            parallelProperty.Init(Localize.StyleOption_No, Localize.StyleOption_Yes);
            parallelProperty.SelectedObject = Parallel;
            parallelProperty.OnSelectObjectChanged += (value) => Parallel.Value = value;

            return parallelProperty;
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
}
