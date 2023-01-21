using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
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

        protected static string Gap => string.Empty;

        private static Dictionary<CrosswalkType, CrosswalkStyle> Defaults { get; } = new Dictionary<CrosswalkType, CrosswalkStyle>()
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
        public static CrosswalkStyle GetDefault(CrosswalkType type)
        {
            return Defaults.TryGetValue(type, out var style) ? style.CopyStyle() : null;
        }

        protected override float WidthWheelStep => 0.1f;
        protected override float WidthMinValue => 0.1f;

        public abstract float GetTotalWidth(MarkingCrosswalk crosswalk);

        public CrosswalkStyle(Color32 color, float width) : base(color, width) { }

        public sealed override List<EditorItem> GetUIComponents(object editObject, UIComponent parent, bool isTemplate = false)
        {
            var components = base.GetUIComponents(editObject, parent, isTemplate);
            if (editObject is MarkingCrosswalk crosswalk)
                GetUIComponents(crosswalk, components, parent, isTemplate);
            else if (isTemplate)
                GetUIComponents(null, components, parent, isTemplate);
            return components;
        }
        public virtual void GetUIComponents(MarkingCrosswalk crosswalk, List<EditorItem> components, UIComponent parent, bool isTemplate = false) { }

        public IStyleData Calculate(MarkingCrosswalk crosswalk, MarkingLOD lod)
        {
            if ((SupportLOD & lod) != 0)
                return new MarkingPartGroupData(lod, CalculateImpl(crosswalk, lod));
            else
                return new MarkingPartGroupData(lod);
        }
        protected abstract IEnumerable<MarkingPartData> CalculateImpl(MarkingCrosswalk crosswalk, MarkingLOD lod);

        protected Vector2PropertyPanel AddLengthProperty(IDashedCrosswalk dashedStyle, UIComponent parent, bool canCollapse)
        {
            var lengthProperty = ComponentPool.Get<Vector2PropertyPanel>(parent, nameof(Length));
            lengthProperty.Text = Localize.StyleOption_Length;
            lengthProperty.FieldsWidth = 50f;
            lengthProperty.SetLabels(Localize.StyleOption_Dash, Localize.StyleOption_Space);
            lengthProperty.Format = Localize.NumberFormat_Meter;
            lengthProperty.UseWheel = true;
            lengthProperty.WheelStep = new Vector2(0.1f, 0.1f);
            lengthProperty.WheelTip = Settings.ShowToolTip;
            lengthProperty.CheckMin = true;
            lengthProperty.MinValue = new Vector2(0.1f, 0.1f);
            lengthProperty.CanCollapse = canCollapse;
            lengthProperty.Init(0, 1);
            lengthProperty.Value = new Vector2(dashedStyle.DashLength, dashedStyle.SpaceLength);
            lengthProperty.OnValueChanged += (Vector2 value) =>
                {
                    dashedStyle.DashLength.Value = value.x;
                    dashedStyle.SpaceLength.Value = value.y;
                };

            return lengthProperty;
        }

        protected IEnumerable<MarkingPartData> CalculateCroswalkPart(ITrajectory trajectory, float startT, float endT, Vector3 direction, ITrajectory[] borders, float length, float width, Color32 color)
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

                yield return new MarkingPartData(startPosition, endPosition, direction, width, color, RenderHelper.MaterialLib[MaterialType.RectangleLines]);
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
