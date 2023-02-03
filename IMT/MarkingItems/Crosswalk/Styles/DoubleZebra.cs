using ColossalFramework.UI;
using IMT.API;
using IMT.MarkingItems.Crosswalk.Styles.Base;
using IMT.UI;
using IMT.Utilities;
using IMT.Utilities.API;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public class DoubleZebraCrosswalkStyle : ZebraCrosswalkStyle, ICrosswalkStyle, IDoubleCrosswalk, ITexture
    {
        public override StyleType Type => StyleType.CrosswalkDoubleZebra;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;

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
                yield return nameof(Scratches);
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
                yield return new StylePropertyDataProvider<float>(nameof(OffsetBetween), OffsetBetween);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetAfter), OffsetAfter);
                yield return new StylePropertyDataProvider<bool>(nameof(Parallel), Parallel);
                yield return new StylePropertyDataProvider<bool>(nameof(UseGap), UseGap);
                yield return new StylePropertyDataProvider<float>(nameof(GapLength), GapLength);
                yield return new StylePropertyDataProvider<int>(nameof(GapPeriod), GapPeriod);
            }
        }

        public DoubleZebraCrosswalkStyle(Color32 color, Color32 secondColor, bool useSecondColor, float width, Vector2 scratches, Vector2 voids, float offsetBefore, float offsetAfter, float dashLength, float spaceLength, bool useGap, float gapLength, int gapPeriod, bool parallel, float offset) : base(color, secondColor, useSecondColor, width, scratches, voids, offsetBefore, offsetAfter, dashLength, spaceLength, useGap, gapLength, gapPeriod, parallel)
        {
            OffsetBetween = GetOffsetProperty(offset);
        }
        protected override float GetVisibleWidth(MarkingCrosswalk crosswalk) => GetLengthCoef(Width * 2 + OffsetBetween, crosswalk);
        public override CrosswalkStyle CopyStyle() => new DoubleZebraCrosswalkStyle(Color, SecondColor, TwoColors, Width, Scratches, Voids, OffsetBefore, OffsetAfter, DashLength, SpaceLength, UseGap, GapLength, GapPeriod, Parallel, OffsetBetween);
        public override void CopyTo(CrosswalkStyle target)
        {
            base.CopyTo(target);
            if (target is IDoubleCrosswalk doubleTarget)
                doubleTarget.OffsetBetween.Value = OffsetBetween;
        }

        protected override void CalculateImpl(MarkingCrosswalk crosswalk, MarkingLOD lod, Action<IStyleData> addData)
        {
            var middleOffset = GetVisibleWidth(crosswalk) / 2 + OffsetBefore;
            var deltaOffset = GetLengthCoef((Width + OffsetBetween) / 2, crosswalk);

            var coef = Mathf.Sin(crosswalk.CornerAndNormalAngle);
            var direction = Parallel ? crosswalk.NormalDir : crosswalk.CornerDir.Turn90(true);
            var index = 0;

            var trajectoryFirst = crosswalk.GetFullTrajectory(middleOffset - deltaOffset, direction);
            var trajectorySecond = crosswalk.GetFullTrajectory(middleOffset + deltaOffset, direction);

            if (!UseGap)
            {
                var dashLength = Parallel ? DashLength / coef : DashLength;
                var spaceLength = Parallel ? SpaceLength / coef : SpaceLength;

                if (GetContour(crosswalk, middleOffset - deltaOffset, Width, out var firstContour))
                {
                    foreach (var part in StyleHelper.CalculateDashesStraightT(trajectoryFirst, dashLength, spaceLength))
                    {
                        CalculateCrosswalkPart(trajectoryFirst, part, direction, firstContour, GetColor(index++), lod, addData);
                    }
                }

                index = 0;

                if (GetContour(crosswalk, middleOffset + deltaOffset, Width, out var secondContour))
                {
                    foreach (var part in StyleHelper.CalculateDashesStraightT(trajectorySecond, dashLength, spaceLength))
                    {
                        CalculateCrosswalkPart(trajectorySecond, part, direction, secondContour, GetColor(index++), lod, addData);
                    }
                }
            }
            else
            {
                var groupLength = (DashLength * GapPeriod + SpaceLength * (GapPeriod - 1));
                var dashT = DashLength / groupLength;
                var spaceT = SpaceLength / groupLength;

                groupLength /= (Parallel ? coef : 1f);
                var gapLength = GapLength / (Parallel ? coef : 1f);

                if (GetContour(crosswalk, middleOffset - deltaOffset, Width, out var firstContour))
                {
                    foreach (var part in StyleHelper.CalculateDashesStraightT(trajectoryFirst, groupLength, gapLength))
                    {
                        for (var i = 0; i < GapPeriod; i += 1)
                        {
                            var startT = part.start + (part.end - part.start) * (dashT + spaceT) * i;
                            var endT = startT + (part.end - part.start) * dashT;
                            CalculateCrosswalkPart(trajectoryFirst, new StyleHelper.PartT(startT, endT), direction, firstContour, GetColor(index++), lod, addData);
                        }
                    }
                }

                index = 0;

                if (GetContour(crosswalk, middleOffset + deltaOffset, Width, out var secondContour))
                {
                    foreach (var part in StyleHelper.CalculateDashesStraightT(trajectorySecond, groupLength, gapLength))
                    {
                        for (var i = 0; i < GapPeriod; i += 1)
                        {
                            var startT = part.start + (part.end - part.start) * (dashT + spaceT) * i;
                            var endT = startT + (part.end - part.start) * dashT;
                            CalculateCrosswalkPart(trajectorySecond, new StyleHelper.PartT(startT, endT), direction, secondContour, GetColor(index++), lod, addData);
                        }
                    }
                }
            }
        }

        public override void GetUIComponents(MarkingCrosswalk crosswalk, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
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
}
