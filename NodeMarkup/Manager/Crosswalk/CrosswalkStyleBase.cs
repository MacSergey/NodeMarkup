using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public abstract class CrosswalkStyle : Style
    {
        public static float DefaultCrosswalkWidth { get; } = 2f;
        public static float DefaultCrosswalkDashLength { get; } = 0.4f;
        public static float DefaultCrosswalkSpaceLength { get; } = 0.6f;
        public static float DefaultCrosswalkOffset { get; } = 0.3f;

        static Dictionary<CrosswalkType, CrosswalkStyle> Defaults { get; } = new Dictionary<CrosswalkType, CrosswalkStyle>()
        {
            {CrosswalkType.Existent, new ExistCrosswalkStyle(DefaultCrosswalkWidth) },
            {CrosswalkType.Zebra, new ZebraCrosswalkStyle(DefaultColor, DefaultCrosswalkWidth, DefaultCrosswalkOffset, DefaultCrosswalkOffset, DefaultCrosswalkDashLength, DefaultCrosswalkSpaceLength, true) },
            {CrosswalkType.DoubleZebra, new DoubleZebraCrosswalkStyle(DefaultColor, DefaultCrosswalkWidth, DefaultCrosswalkOffset, DefaultCrosswalkOffset, DefaultCrosswalkDashLength, DefaultCrosswalkSpaceLength, true, DefaultCrosswalkOffset) },
            {CrosswalkType.ParallelLines, new ParallelLinesCrosswalkStyle(DefaultColor, DefaultCrosswalkWidth, DefaultCrosswalkOffset, DefaultCrosswalkOffset, DefaultWidth) },
            //{CrosswalkType.Ladder, new LadderCrosswalkStyle(DefaultColor, DefaultCrosswalkWidth, DefaultCrosswalkOffset, DefaultCrosswalkOffset, DefaultWidth, DefaultCrosswalkDashLength, DefaultCrosswalkSpaceLength) }
        };

        public static Style GetDefault(CrosswalkType type) => Defaults.TryGetValue(type, out CrosswalkStyle style) ? style.CopyCrosswalkStyle() : null;

        public abstract float GetTotalWidth(MarkupCrosswalk crosswalk);

        public CrosswalkStyle(Color32 color, float width) : base(color, width) { }

        public override Style Copy() => CopyCrosswalkStyle();
        public abstract CrosswalkStyle CopyCrosswalkStyle();

        public abstract IEnumerable<MarkupStyleDash> Calculate(MarkupCrosswalk crosswalk);

        public enum CrosswalkType
        {
            [Description(nameof(Localize.CrosswalkStyle_Existent))]
            Existent = StyleType.CrosswalkExistent,

            [Description(nameof(Localize.CrosswalkStyle_Zebra))]
            Zebra = StyleType.CrosswalkZebra,

            [Description(nameof(Localize.CrosswalkStyle_DoubleZebra))]
            DoubleZebra = StyleType.CrosswalkDoubleZebra,

            [Description(nameof(Localize.CrosswalkStyle_ParallelLines))]
            ParallelLines = StyleType.CrosswalkParallelLines,
        }


        protected float GetOffset(MarkupIntersect intersect, float offset)
        {
            var tan = Mathf.Tan(intersect.Angle);
            return tan != 0 ? offset / tan : 1000f;
        }

        protected IEnumerable<MarkupStyleDash> CalculateCroswalkDash(ILineTrajectory trajectory, float startT, float endT, Vector3 direction, ILineTrajectory[] borders, float length, float width)
        {
            var position = trajectory.Position((startT + endT) / 2);
            var dashTrajectory = new StraightTrajectory(position, position + direction, false);
            var intersects = MarkupIntersect.Calculate(dashTrajectory, borders, true);
            intersects = intersects.OrderBy(i => i.FirstT).ToList();

            var halfLength = length / 2;
            var halfWidth = width / 2;
            for (var i = 1; i < intersects.Count; i += 2)
            {
                var startOffset = GetOffset(intersects[i - 1], halfWidth);
                var endOffset = GetOffset(intersects[i], halfWidth);

                var start = Mathf.Clamp(intersects[i - 1].FirstT + startOffset, -halfLength, halfLength);
                var end = Mathf.Clamp(intersects[i].FirstT - endOffset, -halfLength, halfLength);

                if ((end - start) < width)
                    continue;

                var startPosition = position + direction * start;
                var endPosition = position + direction * end;

                yield return new MarkupStyleDash(startPosition, endPosition, direction, width, Color);
            }
        }
    }
}
