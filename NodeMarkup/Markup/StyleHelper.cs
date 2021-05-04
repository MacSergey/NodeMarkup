using ModsCommon.Utilities;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public static class StyleHelper
    {
        public delegate IEnumerable<MarkupStylePart> SolidGetter(ITrajectory trajectory);
        public delegate IEnumerable<MarkupStylePart> DashedGetter(ITrajectory trajectory, float startT, float endT);
        public static float MinAngleDelta { get; } = 5f;
        public static float MinLength { get; } = 1f;
        public static float MaxLength { get; } = 10f;
        private static Dictionary<MarkupLOD, float> LodScale { get; } = new Dictionary<MarkupLOD, float>()
        {
            { MarkupLOD.LOD0, 1f },
            { MarkupLOD.LOD1, 4f }
        };
        private static int MaxDepth => 5;

        public static List<Result> CalculateSolid<Result>(ITrajectory trajectory, MarkupLOD lod, Func<ITrajectory, Result> calculateParts, float? minAngle = null, float? minLength = null, float? maxLength = null)
        {
            return CalculateSolid<Result>(trajectory, lod, minAngle, minLength, maxLength, AddToResult);
            void AddToResult(List<Result> result, ITrajectory trajectory) => result.Add(calculateParts(trajectory));
        }
        public static List<Result> CalculateSolid<Result>(ITrajectory trajectory, MarkupLOD lod, Func<ITrajectory, IEnumerable<Result>> calculateParts, float? minAngle = null, float? minLength = null, float? maxLength = null)
        {
            return CalculateSolid<Result>(trajectory, lod, minAngle, minLength, maxLength, AddToResult);
            void AddToResult(List<Result> result, ITrajectory trajectory) => result.AddRange(calculateParts(trajectory));
        }

        private static List<Result> CalculateSolid<Result>(ITrajectory trajectory, MarkupLOD lod, float? minAngle, float? minLength, float? maxLength, Action<List<Result>, ITrajectory> addToResult)
        {
            var lodScale = LodScale[lod];
            var result = new List<Result>();

            CalculateSolid(0, trajectory, trajectory.DeltaAngle, (minAngle ?? MinAngleDelta) * lodScale, (minLength ?? MinLength) * lodScale, (maxLength ?? MaxLength) * lodScale, t => addToResult(result, t));

            return result;
        }

        private static void CalculateSolid(int depth, ITrajectory trajectory, float deltaAngle, float minAngle, float minLength, float maxLength, Action<ITrajectory> addToResult)
        {
            var length = trajectory.Magnitude;

            var needDivide = (minAngle < deltaAngle && minLength <= length) || maxLength < length;
            if (depth < MaxDepth && (needDivide || depth == 0))
            {
                trajectory.Divide(out ITrajectory first, out ITrajectory second);
                var firstDeltaAngle = first.DeltaAngle;
                var secondDeltaAngle = second.DeltaAngle;

                if (needDivide || minAngle < deltaAngle || minAngle < firstDeltaAngle + secondDeltaAngle)
                {
                    CalculateSolid(depth + 1, first, firstDeltaAngle, minAngle, minLength, maxLength, addToResult);
                    CalculateSolid(depth + 1, second, secondDeltaAngle, minAngle, minLength, maxLength, addToResult);

                    return;
                }
            }

            addToResult(trajectory);
        }

        public static IEnumerable<MarkupStylePart> CalculateDashed(ITrajectory trajectory, float dashLength, float spaceLength, DashedGetter calculateDashes)
        {
            PartT[] partsT;
            switch (trajectory)
            {
                case BezierTrajectory bezierTrajectory:
                    partsT = CalculateDashesBezierT(bezierTrajectory, dashLength, spaceLength).ToArray();
                    break;
                case StraightTrajectory straightTrajectory:
                    partsT = CalculateDashesStraightT(straightTrajectory, dashLength, spaceLength).ToArray();
                    break;
                default:
                    yield break;
            }

            foreach (var partT in partsT)
            {
                foreach (var part in calculateDashes(trajectory, partT.Start, partT.End))
                    yield return part;
            }
        }
        public static IEnumerable<PartT> CalculateDashesBezierT(BezierTrajectory bezierTrajectory, float dashLength, float spaceLength, uint iterations = 3)
        {
            var points = GetDashesBezierPoints(bezierTrajectory);
            var indices = CalculateDashesBezierT(points, dashLength, spaceLength, iterations);
            var count = points.Length - 1;
            for (var j = 1; j < indices.Count; j += 2)
            {
                var part = new PartT { Start = 1f / count * indices[j - 1], End = 1f / count * indices[j] };
                yield return part;
            }
        }
        public static IEnumerable<PartT> CalculateDashesBezierT(IEnumerable<ITrajectory> trajectories, float dashLength, float spaceLength, uint iterations = 3)
        {
            var pointsList = new List<Vector2[]>();
            foreach (var trajectory in trajectories)
                pointsList.Add(GetDashesBezierPoints(trajectory));

            var indices = CalculateDashesBezierT(pointsList.SelectMany(i => i).ToArray(), dashLength, spaceLength, iterations);
            var counts = pointsList.Select(l => l.Length - 1).ToArray();
            var sum = 0;
            var endIndices = pointsList.Select(x => (sum += x.Length) - x.Length).ToArray();

            for (var j = 1; j < indices.Count; j += 2)
                yield return new PartT { Start = GetT(indices[j - 1]), End = GetT(indices[j]) };

            float GetT(int index)
            {
                var i = endIndices.Length - 1;
                while (index < endIndices[i])
                    i -= 1;

                return 1f / counts[i] * (index - endIndices[i]) + i;
            }
        }
        private static Vector2[] GetDashesBezierPoints(ITrajectory trajectory)
        {
            var length = trajectory.Length;
            if (length > 200f)
                return new Vector2[0];

            var count = (int)(length * 20);
            var points = new Vector2[count + 1];
            for (var i = 0; i <= count; i += 1)
                points[i] = trajectory.Position(1f / count * i).XZ();

            return points;
        }
        private static List<int> CalculateDashesBezierT(Vector2[] points, float dashLength, float spaceLength, uint iterations)
        {
            var startSpace = spaceLength / 2;
            var comparer = new PartsComparer();

            for (var i = 0; ;)
            {
                var partsI = new List<int>();
                var isPart = false;

                var prevI = 0;
                var currentI = 0;
                var nextI = GetI(currentI, startSpace);

                while (nextI < points.Length)
                {
                    //var l = (points[nextI] - points[currentI]).magnitude;
                    if (isPart)
                    {
                        partsI.Add(currentI);
                        partsI.Add(nextI);
                    }

                    isPart = !isPart;

                    prevI = currentI;
                    currentI = nextI;
                    nextI = GetI(currentI, isPart ? dashLength : spaceLength);
                }

                var endSpace = (points.Last() - points[isPart ? prevI : currentI]).magnitude;
                i += 1;
                if (i >= iterations || Mathf.Abs(startSpace - endSpace) / (startSpace + endSpace) < 0.05)
                    return partsI;

                startSpace = (startSpace + endSpace) / 2;
            }

            int GetI(int startI, float distance)
            {
                comparer.Distance = distance;
                var i = Array.BinarySearch(points, startI + 1, points.Length - (startI + 1), points[startI], comparer);
                return i < 0 ? ~i : i;
            }
        }
        private static IEnumerable<PartT> CalculateDashesStraightT(StraightTrajectory straightTrajectory, float dashLength, float spaceLength)
        {
            var length = straightTrajectory.Length;
            var partCount = (int)(length / (dashLength + spaceLength));
            var startSpace = (length + spaceLength - (dashLength + spaceLength) * partCount) / 2;

            var startT = startSpace / length;
            var partT = dashLength / length;
            var spaceT = spaceLength / length;

            for (var i = 0; i < partCount; i += 1)
            {
                var tStart = startT + (partT + spaceT) * i;
                var tEnd = tStart + partT;

                yield return new PartT { Start = tStart, End = tEnd };
            }
        }
        public static bool CalculateDashedParts(LineBorders borders, ITrajectory trajectory, float startT, float endT, float dashLength, float offset, float width, Color32 color, out MarkupStylePart part)
        {
            part = CalculateDashedPart(trajectory, startT, endT, dashLength, offset, width, color);

            if (borders.IsEmpty)
                return true;

            var vertex = borders.GetVertex(part);
            return !borders.Any(c => vertex.Any(v => Intersection.CalculateSingle(c, v).IsIntersect));

        }
        public static MarkupStylePart CalculateDashedPart(ITrajectory trajectory, float startT, float endT, float dashLength, float offset, float width, Color32 color)
        {
            if (offset == 0)
                return CalculateDashedPart(trajectory, startT, endT, dashLength, Vector3.zero, Vector3.zero, width, color);
            else
            {
                var startOffset = trajectory.Tangent(startT).Turn90(true).normalized * offset;
                var endOffset = trajectory.Tangent(endT).Turn90(true).normalized * offset;
                return CalculateDashedPart(trajectory, startT, endT, dashLength, startOffset, endOffset, width, color);
            }
        }
        public static MarkupStylePart CalculateDashedPart(ITrajectory trajectory, float startT, float endT, float dashLength, Vector3 startOffset, Vector3 endOffset, float width, Color32 color, float? angle = null)
        {
            var startPosition = trajectory.Position(startT);
            var endPosition = trajectory.Position(endT);

            startPosition += startOffset;
            endPosition += endOffset;

            var dir = angle?.Direction() ?? (endPosition - startPosition);

            return new MarkupStylePart(startPosition, endPosition, dir, dashLength, width, color);
        }

        public static bool CalculateSolidPart(LineBorders borders, ITrajectory trajectory, float offset, float width, Color32 color, out MarkupStylePart part)
        {
            part = CalculateSolidPart(trajectory, offset, width, color);

            if (borders.IsEmpty)
                return true;

            var vertex = borders.GetVertex(part);

            var from = 0f;
            var to = 1f;

            foreach (var border in borders)
            {
                for (var i = 0; i < vertex.Length; i += 2)
                {
                    var start = Intersection.CalculateSingle(border, vertex[i]);
                    var end = Intersection.CalculateSingle(border, vertex[i + 1]);

                    if (start.IsIntersect && end.IsIntersect)
                        return false;

                    if (!start.IsIntersect && !end.IsIntersect)
                        continue;

                    var intersect = Intersection.CalculateSingle(border, new StraightTrajectory(vertex[i].EndPosition, vertex[i + 1].EndPosition));
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
                var dir = part.Angle.Direction();
                var line = new StraightTrajectory(part.Position + dir * (part.Length / 2), part.Position - dir * (part.Length / 2)).Cut(from, to);
                part = new MarkupStylePart(line.StartPosition, line.EndPosition, line.Direction, part.Width, part.Color);
            }
            return true;
        }
        public static MarkupStylePart CalculateSolidPart(ITrajectory trajectory, float offset, float width, Color32 color)
        {
            if (offset == 0)
                return CalculateSolidPart(trajectory, Vector3.zero, Vector3.zero, width, color);
            else
            {
                var startOffset = trajectory.StartDirection.Turn90(true) * offset;
                var endOffset = trajectory.EndDirection.Turn90(false) * offset;
                return CalculateSolidPart(trajectory, startOffset, endOffset, width, color);
            }
        }
        public static MarkupStylePart CalculateSolidPart(ITrajectory trajectory, Vector3 startOffset, Vector3 endOffset, float width, Color32 color)
        {
            var startPosition = trajectory.StartPosition + startOffset;
            var endPosition = trajectory.EndPosition + endOffset;
            return new MarkupStylePart(startPosition, endPosition, endPosition - startPosition, width, color);
        }
        private static Dictionary<MarkupLOD, float> LodMax { get; } = new Dictionary<MarkupLOD, float>
        {
            {MarkupLOD.LOD0, 0.2f},
            {MarkupLOD.LOD1, 1f}
        };
        public static void GetParts(float width, float offset, MarkupLOD lod, out int count, out float partWidth)
        {
            var max = LodMax[lod];

            if (width < max || offset != 0f)
            {
                count = 1;
                partWidth = width;
            }
            else
            {
                var intWidth = (int)(width * 100);
                var delta = (int)(max * 100);
                var num = 0;
                for (var i = (int)(max * 50); i < (int)(max * 100); i += 1)
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

        public struct PartT
        {
            public float Start;
            public float End;
        }
        private class PartsComparer : IComparer<Vector2>
        {
            public float Distance { get; set; }
            public int Compare(Vector2 x, Vector2 y) => (int)Mathf.Sign(Distance - (y - x).magnitude);
        }
    }
}
