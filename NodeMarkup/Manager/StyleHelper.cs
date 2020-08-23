using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public static class StyleHelper
    {
        public static float MinAngleDelta { get; } = 5f;
        public static float MaxLength { get; } = 10f;
        public static float MinLength { get; } = 1f;
        private static int MaxDepth => 5;
        public static IEnumerable<MarkupStyleDash> CalculateSolid(ILineTrajectory trajectory, Func<ILineTrajectory, IEnumerable<MarkupStyleDash>> calculateDashes)
            => CalculateSolid(0, trajectory, trajectory.DeltaAngle, calculateDashes);
        private static IEnumerable<MarkupStyleDash> CalculateSolid(int depth, ILineTrajectory trajectory, float deltaAngle, Func<ILineTrajectory, IEnumerable<MarkupStyleDash>> calculateDashes)
        {
            var length = trajectory.Magnitude;

            var needDivide = (MinAngleDelta < deltaAngle && MinLength <= length) || MaxLength < length;
            if (depth < MaxDepth && (needDivide || depth == 0))
            {
                trajectory.Divide(out ILineTrajectory first, out ILineTrajectory second);
                var firstDeltaAngle = first.DeltaAngle;
                var secondDeltaAngle = second.DeltaAngle;

                if (needDivide || MinAngleDelta < deltaAngle || MinAngleDelta < firstDeltaAngle + secondDeltaAngle)
                {
                    foreach (var dash in CalculateSolid(depth + 1, first, firstDeltaAngle, calculateDashes))
                        yield return dash;

                    foreach (var dash in CalculateSolid(depth + 1, second, secondDeltaAngle, calculateDashes))
                        yield return dash;

                    yield break;
                }
            }

            foreach (var dash in calculateDashes(trajectory))
                yield return dash;
        }

        public static IEnumerable<MarkupStyleDash> CalculateDashed(ILineTrajectory trajectory, float dashLength, float spaceLength, Func<ILineTrajectory, float, float, IEnumerable<MarkupStyleDash>> calculateDashes)
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

        public static MarkupStyleDash CalculateDashedDash(ILineTrajectory trajectory, float startT, float endT, float dashLength, float offset, float width, Color color)
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
        public static MarkupStyleDash CalculateDashedDash(ILineTrajectory trajectory, float startT, float endT, float dashLength, Vector3 startOffset, Vector3 endOffset, float width, Color color, float? angle = null)
        {
            var startPosition = trajectory.Position(startT);
            var endPosition = trajectory.Position(endT);

            startPosition += startOffset;
            endPosition += endOffset;

            if (angle == null)
                return new MarkupStyleDash(startPosition, endPosition, endPosition - startPosition, dashLength, width, color);
            else
                return new MarkupStyleDash(startPosition, endPosition, angle.Value, dashLength, width, color);
        }

        public static MarkupStyleDash CalculateSolidDash(ILineTrajectory trajectory, float offset, float width, Color color)
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
        public static MarkupStyleDash CalculateSolidDash(ILineTrajectory trajectory, Vector3 startOffset, Vector3 endOffset, float width, Color color)
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
