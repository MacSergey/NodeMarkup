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
    public class ChessBoardCrosswalkStyle : CustomCrosswalkStyle, IColorStyle, IAsymLine, IEffectStyle
    {
        public override StyleType Type => StyleType.CrosswalkChessBoard;
        public override MarkingLOD SupportLOD => MarkingLOD.LOD0 | MarkingLOD.LOD1;
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
                yield return new StylePropertyDataProvider<Color32>(nameof(Color), Color);
                yield return new StylePropertyDataProvider<float>(nameof(Width), Width);
                yield return new StylePropertyDataProvider<float>(nameof(SquareSide), SquareSide);
                yield return new StylePropertyDataProvider<int>(nameof(LineCount), LineCount);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetBefore), OffsetBefore);
                yield return new StylePropertyDataProvider<float>(nameof(OffsetAfter), OffsetAfter);
                yield return new StylePropertyDataProvider<bool>(nameof(Invert), Invert);
            }
        }

        public ChessBoardCrosswalkStyle(Color32 color, Vector2 cracks, Vector2 voids, float texture, float offsetBefore, float offsetAfter, float squareSide, int lineCount, bool invert) : base(color, 0, cracks, voids, texture, offsetBefore, offsetAfter)
        {
            SquareSide = GetSquareSideProperty(squareSide);
            LineCount = GetLineCountProperty(lineCount);
            Invert = GetInvertProperty(invert);
        }
        protected override void CalculateImpl(MarkingCrosswalk crosswalk, MarkingLOD lod, Action<IStyleData> addData)
        {
            var width = GetAbsoluteWidth(SquareSide, crosswalk);
            var startOffset = width * 0.5f + OffsetBefore;
            var direction = crosswalk.CornerDir.Turn90(true);

            for (var i = 0; i < LineCount; i += 1)
            {
                var offset = startOffset + width * i;
                if (GetContour(crosswalk, offset, width, out var contour))
                {
                    var trajectory = crosswalk.GetFullTrajectory(offset, direction);
                    var trajectoryLength = trajectory.Length;
                    var count = (int)(trajectoryLength / SquareSide);
                    var squareT = SquareSide / trajectoryLength;
                    var startT = (trajectoryLength - SquareSide * count) * 0.5f / trajectoryLength;

                    for (var j = (Invert ? i + 1 : i) % 2; j < count; j += 2)
                    {
                        var part = new StyleHelper.PartT(startT + squareT * j, startT + squareT * (j + 1));
                        CalculateCrosswalkPart(trajectory, part, direction, contour, Color, lod, addData);
                    }
                }
            }
        }

        public override CrosswalkStyle CopyStyle() => new ChessBoardCrosswalkStyle(Color, Cracks, Voids, Texture, OffsetBefore, OffsetAfter, SquareSide, LineCount, Invert);
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
        protected override float GetVisibleWidth(MarkingCrosswalk crosswalk) => GetAbsoluteWidth(SquareSide * LineCount, crosswalk);

        public override void GetUIComponents(MarkingCrosswalk crosswalk, List<EditorItem> components, UIComponent parent, bool isTemplate = false)
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
