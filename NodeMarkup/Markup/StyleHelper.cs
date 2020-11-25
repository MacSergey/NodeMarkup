using ModsCommon.Utilities;
using IMT.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace IMT.Manager
{
    public static class StyleHelper
    {
        public delegate IEnumerable<MarkupStyleDash> SolidGetter(ILineTrajectory trajectory);
        public delegate IEnumerable<MarkupStyleDash> DashedGetter(ILineTrajectory trajectory, float startT, float endT);
        public static float MinAngleDelta { get; } = 5f;
        public static float MaxLength { get; } = 10f;
        public static float MinLength { get; } = 1f;
        private static int MaxDepth => 5;

        public static IEnumerable<Result> CalculateSolid<Result>(ILineTrajectory trajectory, Func<ILineTrajectory, IEnumerable<Result>> calculateDashes)
            => CalculateSolid(trajectory, MinAngleDelta, MinLength, MaxLength, calculateDashes);
        public static IEnumerable<Result> CalculateSolid<Result>(ILineTrajectory trajectory, float minAngle, float minLength, float maxLength, Func<ILineTrajectory, IEnumerable<Result>> calculateDashes)
            => CalculateSolid(0, trajectory, trajectory.DeltaAngle, minAngle, minLength, maxLength, calculateDashes);

        public static IEnumerable<Result> CalculateSolid<Result>(int depth, ILineTrajectory trajectory, float deltaAngle, float minAngle, float minLength, float maxLength, Func<ILineTrajectory, IEnumerable<Result>> calculateDashes)
        {
            var length = trajectory.Magnitude;

            var needDivide = (minAngle < deltaAngle && minLength <= length) || maxLength < length;
            if (depth < MaxDepth && (needDivide || depth == 0))
            {
                trajectory.Divide(out ILineTrajectory first, out ILineTrajectory second);
                var firstDeltaAngle = first.DeltaAngle;
                var secondDeltaAngle = second.DeltaAngle;

                if (needDivide || minAngle < deltaAngle || minAngle < firstDeltaAngle + secondDeltaAngle)
                {
                    foreach (var dash in CalculateSolid(depth + 1, first, firstDeltaAngle, minAngle, minLength, maxLength, calculateDashes))
                        yield return dash;

                    foreach (var dash in CalculateSolid(depth + 1, second, secondDeltaAngle, minAngle, minLength, maxLength, calculateDashes))
                        yield return dash;

                    yield break;
                }
            }

            foreach (var dash in calculateDashes(trajectory))
                yield return dash;
        }

        public static IEnumerable<MarkupStyleDash> CalculateDashed(ILineTrajectory trajectory, float dashLength, float spaceLength, DashedGetter calculateDashes)
        {
            List<DashT> dashesT;
            switch (trajectory)
            {
                case BezierTrajectory bezierTrajectory:
                    dashesT = CalculateDashesBezierT(bezierTrajectory, dashLength, spaceLength);
                    break;
                case StraightTrajectory straightTrajectory:
                    dashesT = CalculateDashesStraightT(straightTrajectory, dashLength, spaceLength);
                    break;
                default:
                    yield break;
            }

            foreach (var dashT in dashesT)
            {
                foreach (var dash in calculateDashes(trajectory, dashT.Start, dashT.End))
                    yield return dash;
            }
        }
        private static List<DashT> CalculateDashesBezierT(BezierTrajectory bezierTrajectory, float dashLength, float spaceLength)
        {
            var dashesT = new List<DashT>();
            var trajectory = bezierTrajectory.Trajectory;
            var startSpace = spaceLength / 2;
            for (var i = 0; i < 3; i += 1)
            {
                dashesT.Clear();
                var isDash = false;

                var prevT = 0f;
                var currentT = 0f;
                var nextT = trajectory.Travel(currentT, startSpace);

                while (nextT < 1)
                {
                    if (isDash)
                        dashesT.Add(new DashT { Start = currentT, End = nextT });

                    isDash = !isDash;

                    prevT = currentT;
                    currentT = nextT;
                    nextT = trajectory.Travel(currentT, isDash ? dashLength : spaceLength);
                }

                float endSpace;
                if (isDash || ((trajectory.Position(1) - trajectory.Position(currentT)).magnitude is float tempLength && tempLength < spaceLength / 2))
                    endSpace = (trajectory.Position(1) - trajectory.Position(prevT)).magnitude;
                else
                    endSpace = tempLength;

                startSpace = (startSpace + endSpace) / 2;

                if (Mathf.Abs(startSpace - endSpace) / (startSpace + endSpace) < 0.05)
                    break;
            }

            return dashesT;
        }
        private static List<DashT> CalculateDashesStraightT(StraightTrajectory straightTrajectory, float dashLength, float spaceLength)
        {
            var length = straightTrajectory.Length;
            var dashCount = (int)(length / (dashLength + spaceLength));
            var startSpace = (length + spaceLength - (dashLength + spaceLength) * dashCount) / 2;

            var dashesT = new List<DashT>(dashCount);

            var startT = startSpace / length;
            var dashT = dashLength / length;
            var spaceT = spaceLength / length;

            for (var i = 0; i < dashCount; i += 1)
            {
                var tStart = startT + (dashT + spaceT) * i;
                var tEnd = tStart + spaceT;

                dashesT.Add(new DashT { Start = tStart, End = tEnd });
            }

            return dashesT;
        }
        public static bool CalculateDashedDash(LineBorders borders, ILineTrajectory trajectory, float startT, float endT, float dashLength, float offset, float width, Color32 color, out MarkupStyleDash dash)
        {
            dash = CalculateDashedDash(trajectory, startT, endT, dashLength, offset, width, color);

            if (borders.IsEmpty)
                return true;

            var vertex = borders.GetVertex(dash);
            return !borders.Any(c => vertex.Any(v => MarkupIntersect.CalculateSingle(c, v).IsIntersect));

        }
        public static MarkupStyleDash CalculateDashedDash(ILineTrajectory trajectory, float startT, float endT, float dashLength, float offset, float width, Color32 color)
        {
            if (offset == 0)
                return CalculateDashedDash(trajectory, startT, endT, dashLength, Vector3.zero, Vector3.zero, width, color);
            else
            {
                var startOffset = trajectory.Tangent(startT).Turn90(true).normalized * offset;
                var endOffset = trajectory.Tangent(endT).Turn90(true).normalized * offset;
                return CalculateDashedDash(trajectory, startT, endT, dashLength, startOffset, endOffset, width, color);
            }
        }
        public static MarkupStyleDash CalculateDashedDash(ILineTrajectory trajectory, float startT, float endT, float dashLength, Vector3 startOffset, Vector3 endOffset, float width, Color32 color, float? angle = null)
        {
            var startPosition = trajectory.Position(startT);
            var endPosition = trajectory.Position(endT);

            startPosition += startOffset;
            endPosition += endOffset;

            var dir = angle?.Direction() ?? (endPosition - startPosition);

            return new MarkupStyleDash(startPosition, endPosition, dir, dashLength, width, color);
        }

        public static bool CalculateSolidDash(LineBorders borders, ILineTrajectory trajectory, float offset, float width, Color32 color, out MarkupStyleDash dash)
        {
            dash = CalculateSolidDash(trajectory, offset, width, color);

            if (borders.IsEmpty)
                return true;

            var vertex = borders.GetVertex(dash);

            var from = 0f;
            var to = 1f;

            foreach (var border in borders)
            {
                for (var i = 0; i < vertex.Length; i += 2)
                {
                    var start = MarkupIntersect.CalculateSingle(border, vertex[i]);
                    var end = MarkupIntersect.CalculateSingle(border, vertex[i + 1]);

                    if (start.IsIntersect && end.IsIntersect)
                        return false;

                    if (!start.IsIntersect && !end.IsIntersect)
                        continue;

                    var intersect = MarkupIntersect.CalculateSingle(border, new StraightTrajectory(vertex[i].EndPosition, vertex[i + 1].EndPosition));
                    if (intersect.IsIntersect)
                    {
                        if (start.IsIntersect)
                            from = Mathf.Max(from, intersect.SecondT);
                        else if (end.IsIntersect)
                            to = Mathf.Min(to, intersect.SecondT);
                    }
                }
            }

            if (from != 0f || to != 1f)
            {
                var dir = dash.Angle.Direction();
                var line = new StraightTrajectory(dash.Position + dir * (dash.Length / 2), dash.Position - dir * (dash.Length / 2)).Cut(from, to);
                dash = new MarkupStyleDash(line.StartPosition, line.EndPosition, line.Direction, dash.Width, dash.Color);
            }
            return true;
        }
        public static MarkupStyleDash CalculateSolidDash(ILineTrajectory trajectory, float offset, float width, Color32 color)
        {
            if (offset == 0)
                return CalculateSolidDash(trajectory, Vector3.zero, Vector3.zero, width, color);
            else
            {
                var startOffset = trajectory.StartDirection.Turn90(true).normalized * offset;
                var endOffset = trajectory.EndDirection.Turn90(false).normalized * offset;
                return CalculateSolidDash(trajectory, startOffset, endOffset, width, color);
            }
        }
        public static MarkupStyleDash CalculateSolidDash(ILineTrajectory trajectory, Vector3 startOffset, Vector3 endOffset, float width, Color32 color)
        {
            var startPosition = trajectory.StartPosition + startOffset;
            var endPosition = trajectory.EndPosition + endOffset;
            return new MarkupStyleDash(startPosition, endPosition, endPosition - startPosition, width, color);
        }
        public static void GetParts(float width, float offset, out int count, out float partWidth)
        {
            if (width < 0.2f || offset != 0f)
            {
                count = 1;
                partWidth = width;
            }
            else
            {
                var intWidth = (int)(width * 100);
                var delta = 20;
                var num = 0;
                for (var i = 10; i < 20; i += 1)
                {
                    var iDelta = intWidth - (intWidth / i) * i;
                    if (iDelta < delta)
                    {
                        delta = iDelta;
                        num = i;
                    }
                }
                count = intWidth / num;
                partWidth = num / 100f;
            }
        }

        struct DashT
        {
            public float Start;
            public float End;
        }
    }
}
