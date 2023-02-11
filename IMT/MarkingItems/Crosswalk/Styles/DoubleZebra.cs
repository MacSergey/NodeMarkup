using ColossalFramework.UI;
using IMT.API;
using IMT.UI;
using IMT.UI.Editors;
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
    public class DoubleZebraCrosswalkStyle : ZebraCrosswalkStyle, ICrosswalkStyle, IDoubleCrosswalk, IEffectStyle
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
                yield return nameof(StraightEnds);
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
                yield return new StylePropertyDataProvider<float>(nameof(OffsetBetween), OffsetBetween);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetAfter), OffsetAfter);
                yield return new StylePropertyDataProvider<bool>(nameof(Parallel), Parallel);
                yield return new StylePropertyDataProvider<bool>(nameof(UseGap), UseGap);
                yield return new StylePropertyDataProvider<float>(nameof(GapLength), GapLength);
                yield return new StylePropertyDataProvider<int>(nameof(GapPeriod), GapPeriod);
                yield return new StylePropertyDataProvider<float>(nameof(Texture), Texture);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Cracks), Cracks);
                yield return new StylePropertyDataProvider<Vector2>(nameof(Voids), Voids);
            }
        }

        public DoubleZebraCrosswalkStyle(Color32 color, Color32 secondColor, bool useSecondColor, float width, Vector2 cracks, Vector2 voids, float texture, float offsetBefore, float offsetAfter, float dashLength, float spaceLength, bool useGap, float gapLength, int gapPeriod, bool parallel, bool straightEnds, float offset) : base(color, secondColor, useSecondColor, width, cracks, voids, texture, offsetBefore, offsetAfter, dashLength, spaceLength, useGap, gapLength, gapPeriod, parallel,  straightEnds)
        {
            OffsetBetween = GetOffsetProperty(offset);
        }
        protected override float GetVisibleWidth(MarkingCrosswalk crosswalk) => GetAbsoluteWidth(Width * 2f + OffsetBetween, crosswalk);
        public override CrosswalkStyle CopyStyle() => new DoubleZebraCrosswalkStyle(Color, SecondColor, TwoColors, Width, Cracks, Voids, Texture, OffsetBefore, OffsetAfter, DashLength, SpaceLength, UseGap, GapLength, GapPeriod, Parallel, StraightEnds, OffsetBetween);
        public override void CopyTo(CrosswalkStyle target)
        {
            base.CopyTo(target);
            if (target is IDoubleCrosswalk doubleTarget)
                doubleTarget.OffsetBetween.Value = OffsetBetween;
        }

        protected override void CalculateImpl(MarkingCrosswalk crosswalk, MarkingLOD lod, Action<IStyleData> addData)
        {
            var middleOffset = GetVisibleWidth(crosswalk) * 0.5f + OffsetBefore;
            var deltaOffset = GetAbsoluteWidth((Width + OffsetBetween) * 0.5f, crosswalk);

            var width = GetAbsoluteWidth(Width, crosswalk);
            var direction = Parallel ? crosswalk.NormalDir : crosswalk.CornerDir.Turn90(true);

            var trajectoryFirst = crosswalk.GetFullTrajectory(middleOffset - deltaOffset, direction);
            var trajectorySecond = crosswalk.GetFullTrajectory(middleOffset + deltaOffset, direction);

            if (Parallel && StraightEnds)
            {
                var firstDashes = GetDashes(crosswalk, trajectoryFirst);
                for (int i = 0; i < firstDashes.Count; i += 1)
                {
#if DEBUG
                    bool renderOnly = RenderOnly != -1 && i != RenderOnly;
                    if (renderOnly)
                        continue;
#endif
                    var dashContour = GetDashContour(trajectoryFirst, direction, firstDashes[i]);
                    var color = GetColor(UseGap ? i / GapPeriod : i);
                    CalculateCrosswalkPart(dashContour, crosswalk.RightBorderTrajectory, crosswalk.LeftBorderTrajectory, color, lod, addData);
                }

                var secondDashes = GetDashes(crosswalk, trajectorySecond);
                for (int i = 0; i < secondDashes.Count; i += 1)
                {
#if DEBUG
                    bool renderOnly = RenderOnly != -1 && i != RenderOnly;
                    if (renderOnly)
                        continue;
#endif
                    var dashContour = GetDashContour(trajectorySecond, direction, secondDashes[i]);
                    var color = GetColor(UseGap ? i / GapPeriod : i);
                    CalculateCrosswalkPart(dashContour, crosswalk.RightBorderTrajectory, crosswalk.LeftBorderTrajectory, color, lod, addData);
                }
            }
            else
            {
                if (GetContour(crosswalk, middleOffset - deltaOffset, width, out var firstContour))
                {
                    var firstDashes = GetDashes(crosswalk, trajectoryFirst);

                    for (int i = 0; i < firstDashes.Count; i += 1)
                    {
#if DEBUG
                        bool renderOnly = RenderOnly != -1 && i != RenderOnly;
                        if (renderOnly)
                            continue;
#endif
                        var color = GetColor(UseGap ? i / GapPeriod : i);
                        CalculateCrosswalkPart(trajectoryFirst, firstDashes[i], direction, firstContour, color, lod, addData);
                    }
                }

                if (GetContour(crosswalk, middleOffset + deltaOffset, width, out var secondContour))
                {
                    var secondDashes = GetDashes(crosswalk, trajectorySecond);
                    for (int i = 0; i < secondDashes.Count; i += 1)
                    {
#if DEBUG
                        bool renderOnly = RenderOnly != -1 && i != RenderOnly;
                        if (renderOnly)
                            continue;
#endif
                        var color = GetColor(UseGap ? i / GapPeriod : i);
                        CalculateCrosswalkPart(trajectorySecond, secondDashes[i], direction, secondContour, color, lod, addData);
                    }
                }
            }
        }

        protected override void GetUIComponents(MarkingCrosswalk crosswalk, EditorProvider provider)
        {
            base.GetUIComponents(crosswalk, provider);
            provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(OffsetBetween), MainCategory, AddOffsetBetweenProperty));
        }

        protected void AddOffsetBetweenProperty(FloatPropertyPanel offsetBetweenProperty, EditorProvider provider)
        {
            offsetBetweenProperty.Text = Localize.StyleOption_OffsetBetween;
            offsetBetweenProperty.Format = Localize.NumberFormat_Meter;
            offsetBetweenProperty.UseWheel = true;
            offsetBetweenProperty.WheelStep = 0.1f;
            offsetBetweenProperty.CheckMin = true;
            offsetBetweenProperty.MinValue = 0.1f;
            offsetBetweenProperty.WheelTip = Settings.ShowToolTip;
            offsetBetweenProperty.Init();
            offsetBetweenProperty.Value = OffsetBetween;
            offsetBetweenProperty.OnValueChanged += (float value) => OffsetBetween.Value = value;
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
