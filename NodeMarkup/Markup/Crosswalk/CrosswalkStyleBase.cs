using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utilities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public abstract class CrosswalkStyle : Style<CrosswalkStyle>
    {
        public static float DefaultCrosswalkWidth { get; } = 2f;
        public static float DefaultCrosswalkDashLength { get; } = 0.4f;
        public static float DefaultCrosswalkSpaceLength { get; } = 0.6f;
        public static float DefaultCrosswalkOffset { get; } = 0.3f;

        public static float DefaultCrosswalkSquareSide { get; } = 1f;
        public static int DefaultCrosswalkLineCount { get; } = 2;

        public static int DefaulCrosswalkGapPeriod => 2;

        public static Dictionary<CrosswalkType, CrosswalkStyle> Defaults { get; } = new Dictionary<CrosswalkType, CrosswalkStyle>()
        {
            {CrosswalkType.Existent, new ExistCrosswalkStyle(DefaultCrosswalkWidth) },
            {CrosswalkType.Zebra, new ZebraCrosswalkStyle(DefaultColor, DefaultColor, false, DefaultCrosswalkWidth, DefaultCrosswalkOffset, DefaultCrosswalkOffset, DefaultCrosswalkDashLength, DefaultCrosswalkSpaceLength, false, DefaultCrosswalkSpaceLength, DefaulCrosswalkGapPeriod, true) },
            {CrosswalkType.DoubleZebra, new DoubleZebraCrosswalkStyle(DefaultColor, DefaultColor, false, DefaultCrosswalkWidth, DefaultCrosswalkOffset, DefaultCrosswalkOffset, DefaultCrosswalkDashLength, DefaultCrosswalkSpaceLength, false, DefaultCrosswalkSpaceLength, DefaulCrosswalkGapPeriod,true, DefaultCrosswalkOffset) },
            {CrosswalkType.ParallelSolidLines, new ParallelSolidLinesCrosswalkStyle(DefaultColor, DefaultCrosswalkWidth, DefaultCrosswalkOffset, DefaultCrosswalkOffset, DefaultWidth) },
            {CrosswalkType.ParallelDashedLines, new ParallelDashedLinesCrosswalkStyle(DefaultColor, DefaultCrosswalkWidth, DefaultCrosswalkOffset, DefaultCrosswalkOffset, DefaultWidth, DefaultDashLength, DefaultSpaceLength) },
            {CrosswalkType.Ladder, new LadderCrosswalkStyle(DefaultColor, DefaultCrosswalkWidth, DefaultCrosswalkOffset, DefaultCrosswalkOffset, DefaultCrosswalkDashLength, DefaultCrosswalkSpaceLength, DefaultWidth) },
            {CrosswalkType.Solid, new SolidCrosswalkStyle(DefaultColor, DefaultCrosswalkWidth, DefaultCrosswalkOffset, DefaultCrosswalkOffset) },
            {CrosswalkType.ChessBoard, new ChessBoardCrosswalkStyle(DefaultColor, DefaultCrosswalkOffset, DefaultCrosswalkOffset, DefaultCrosswalkSquareSide, DefaultCrosswalkLineCount, false) },
        };

        protected override float WidthWheelStep => 0.1f;
        protected override float WidthMinValue => 0.1f;

        public abstract float GetTotalWidth(MarkupCrosswalk crosswalk);

        public CrosswalkStyle(Color32 color, float width) : base(color, width) { }

        public sealed override List<EditorItem> GetUIComponents(object editObject, UIComponent parent, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, isTemplate);
            if (editObject is MarkupCrosswalk crosswalk)
                GetUIComponents(crosswalk, components, parent, isTemplate);
            else if (isTemplate)
                GetUIComponents(null, components, parent, isTemplate);
            return components;
        }
        public virtual void GetUIComponents(MarkupCrosswalk crosswalk, List<EditorItem> components, UIComponent parent, bool isTemplate = false) { }

        public abstract IEnumerable<MarkupStylePart> Calculate(MarkupCrosswalk crosswalk, MarkupLOD lod);

        protected FloatPropertyPanel AddDashLengthProperty(IDashedCrosswalk dashedStyle, UIComponent parent)
        {
            var dashLengthProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(dashedStyle.DashLength));
            dashLengthProperty.Text = Localize.StyleOption_DashedLength;
            dashLengthProperty.UseWheel = true;
            dashLengthProperty.WheelStep = 0.1f;
            dashLengthProperty.WheelTip = Settings.ShowToolTip;
            dashLengthProperty.CheckMin = true;
            dashLengthProperty.MinValue = 0.1f;
            dashLengthProperty.Init();
            dashLengthProperty.Value = dashedStyle.DashLength;
            dashLengthProperty.OnValueChanged += (float value) => dashedStyle.DashLength.Value = value;

            return dashLengthProperty;
        }
        protected FloatPropertyPanel AddSpaceLengthProperty(IDashedCrosswalk dashedStyle, UIComponent parent)
        {
            var spaceLengthProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(dashedStyle.SpaceLength));
            spaceLengthProperty.Text = Localize.StyleOption_SpaceLength;
            spaceLengthProperty.UseWheel = true;
            spaceLengthProperty.WheelStep = 0.1f;
            spaceLengthProperty.WheelTip = Settings.ShowToolTip;
            spaceLengthProperty.CheckMin = true;
            spaceLengthProperty.MinValue = 0.1f;
            spaceLengthProperty.Init();
            spaceLengthProperty.Value = dashedStyle.SpaceLength;
            spaceLengthProperty.OnValueChanged += (float value) => dashedStyle.SpaceLength.Value = value;

            return spaceLengthProperty;
        }

        protected IEnumerable<MarkupStylePart> CalculateCroswalkPart(ITrajectory trajectory, float startT, float endT, Vector3 direction, ITrajectory[] borders, float length, float width, Color32 color)
        {
            var position = trajectory.Position((startT + endT) / 2);
            var partTrajectory = new StraightTrajectory(position, position + direction, false);
            var intersects = Intersection.Calculate(partTrajectory, borders, true);
            intersects = intersects.OrderBy(i => i.FirstT).ToList();

            var halfLength = length / 2;
            var halfWidth = width / 2;
            for (var i = 1; i < intersects.Count; i += 2)
            {
                var startOffset = GetOffset(intersects[i - 1], halfWidth);
                var endOffset = GetOffset(intersects[i], halfWidth);

                var start = Mathf.Clamp(intersects[i - 1].FirstT + startOffset, -halfLength, halfLength);
                var end = Mathf.Clamp(intersects[i].FirstT - endOffset, -halfLength, halfLength);

                var delta = end - start;
                if (delta < 0.9 * length && delta < 0.67 * width)
                    continue;

                var startPosition = position + direction * start;
                var endPosition = position + direction * end;

                yield return new MarkupStylePart(startPosition, endPosition, direction, width, color);
            }

            static float GetOffset(Intersection intersect, float offset)
            {
                var firstDir = intersect.First.Tangent(intersect.FirstT);
                var secondDir = intersect.Second.Tangent(intersect.SecondT);
                var angel = Vector3.Angle(firstDir, secondDir) * Mathf.Deg2Rad;
                var tan = Mathf.Tan(angel);
                return tan != 0 ? offset / tan : 1000f;
            }
        }

        public enum CrosswalkType
        {
            [Description(nameof(Localize.CrosswalkStyle_Existent))]
            Existent = StyleType.CrosswalkExistent,

            [Description(nameof(Localize.CrosswalkStyle_Zebra))]
            Zebra = StyleType.CrosswalkZebra,

            [Description(nameof(Localize.CrosswalkStyle_DoubleZebra))]
            DoubleZebra = StyleType.CrosswalkDoubleZebra,

            [Description(nameof(Localize.CrosswalkStyle_ParallelSolidLines))]
            ParallelSolidLines = StyleType.CrosswalkParallelSolidLines,

            [Description(nameof(Localize.CrosswalkStyle_ParallelDashedLines))]
            ParallelDashedLines = StyleType.CrosswalkParallelDashedLines,

            [Description(nameof(Localize.CrosswalkStyle_Ladder))]
            Ladder = StyleType.CrosswalkLadder,

            [Description(nameof(Localize.CrosswalkStyle_Solid))]
            Solid = StyleType.CrosswalkSolid,

            [Description(nameof(Localize.CrosswalkStyle_ChessBoard))]
            ChessBoard = StyleType.CrosswalkChessBoard,

            [Description(nameof(Localize.Style_FromClipboard))]
            [NotVisible]
            Buffer = StyleType.CrosswalkBuffer,
        }
    }
}
