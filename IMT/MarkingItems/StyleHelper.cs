using IMT.Utilities;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public static class StyleHelper
    {
        public delegate IEnumerable<MarkingPartData> SolidGetter(ITrajectory trajectory);
        public delegate IEnumerable<MarkingPartData> DashedGetter(ITrajectory trajectory, float startT, float endT);
        public static float MinAngleDelta { get; } = 5f;
        public static float MinLength { get; } = 1f;
        public static float MaxLength { get; } = 10f;
        private static int MaxDepth => 5;

        public static List<Result> CalculateSolid<Result>(ITrajectory trajectory, MarkingLOD lod, Func<ITrajectory, Result> calculateParts, float? minAngle = null, float? minLength = null, float? maxLength = null)
        {
            return CalculateSolid<Result>(trajectory, lod, minAngle, minLength, maxLength, AddToResult);
            void AddToResult(List<Result> result, ITrajectory trajectory) => result.Add(calculateParts(trajectory));
        }
        public static List<Result> CalculateSolid<Result>(ITrajectory trajectory, MarkingLOD lod, Func<ITrajectory, IEnumerable<Result>> calculateParts, float? minAngle = null, float? minLength = null, float? maxLength = null)
        {
            return CalculateSolid<Result>(trajectory, lod, minAngle, minLength, maxLength, AddToResult);
            void AddToResult(List<Result> result, ITrajectory trajectory) => result.AddRange(calculateParts(trajectory));
        }

        private static List<Result> CalculateSolid<Result>(ITrajectory trajectory, MarkingLOD lod, float? minAngle, float? minLength, float? maxLength, Action<List<Result>, ITrajectory> addToResult)
        {
            var lodScale = lod switch
            {
                MarkingLOD.LOD0 or MarkingLOD.NoLOD => 1f,
                MarkingLOD.LOD1 => 4f,
            };
            var result = new List<Result>();

            CalculateSolid(0, trajectory, trajectory.DeltaAngle, (minAngle ?? MinAngleDelta) * lodScale, (minLength ?? MinLength) * lodScale, (maxLength ?? MaxLength) * lodScale, t => addToResult(result, t));

            return result;
        }

        public static List<Result> CalculateSolid<Result>(ITrajectory trajectory, float minAngle, float minLength, float maxLength, Func<ITrajectory, Result> calculateParts)
        {
            var result = new List<Result>();
            CalculateSolid(0, trajectory, trajectory.DeltaAngle, minAngle, minLength, maxLength, t => result.Add(calculateParts(t)));
            return result;
        }

        private static void CalculateSolid(int depth, ITrajectory trajectory, float deltaAngle, float minAngle, float minLength, float maxLength, Action<ITrajectory> addToResult)
        {
            var length = trajectory.Magnitude;

            var needDivide = (deltaAngle > minAngle && length >= minLength) || length > maxLength;
            if (depth < MaxDepth && (needDivide || depth == 0))
            {
                trajectory.Divide(out ITrajectory first, out ITrajectory second);
                var firstDeltaAngle = first.DeltaAngle;
                var secondDeltaAngle = second.DeltaAngle;

                if (needDivide || deltaAngle > minAngle || (firstDeltaAngle + secondDeltaAngle) > minAngle)
                {
                    CalculateSolid(depth + 1, first, firstDeltaAngle, minAngle, minLength, maxLength, addToResult);
                    CalculateSolid(depth + 1, second, secondDeltaAngle, minAngle, minLength, maxLength, addToResult);

                    return;
                }
            }

            addToResult(trajectory);
        }

        public static IEnumerable<MarkingPartData> CalculateDashed(ITrajectory trajectory, float dashLength, float spaceLength, DashedGetter calculateDashes)
        {
            List<PartT> partsT;
            switch (trajectory)
            {
                case BezierTrajectory bezierTrajectory:
                    partsT = CalculateDashesBezierT(bezierTrajectory, dashLength, spaceLength);
                    break;
                case StraightTrajectory straightTrajectory:
                    partsT = CalculateDashesStraightT(straightTrajectory, dashLength, spaceLength);
                    break;
                default:
                    yield break;
            }

            foreach (var partT in partsT)
            {
                foreach (var part in calculateDashes(trajectory, partT.start, partT.end))
                    yield return part;
            }
        }

        public static List<PartT> CalculateDashesBezierT(ITrajectory trajectory, float dashLength, float spaceLength, uint iterations = 3)
        {
            var points = new TrajectoryPoints(trajectory);
            var startSpace = spaceLength / 2;

            for (var i = 0; ;)
            {
                var parts = new List<PartT>();
                var isPart = false;

                var prevI = 0;
                var currentI = 0;
                var currentT = 0f;
                var nextT = points.Find(currentI, startSpace, out var nextI);

                while (nextI < points.Length)
                {
                    if (isPart)
                        parts.Add(new PartT(currentT, nextT));

                    isPart = !isPart;

                    prevI = currentI;
                    currentI = nextI;
                    currentT = nextT;
                    nextT = points.Find(currentI, isPart ? dashLength : spaceLength, out nextI);
                }

                var endSpace = (points[points.Length - 1] - points[isPart ? prevI : currentI]).magnitude;
                i += 1;
                if (i >= iterations || Mathf.Abs(startSpace - endSpace) / (startSpace + endSpace) < 0.05)
                    return parts;

                startSpace = (startSpace + endSpace) / 2;
            }
        }
        private static List<PartT> CalculateDashesStraightT(StraightTrajectory straightTrajectory, float dashLength, float spaceLength)
        {
            var length = straightTrajectory.Length;
            var partCount = (int)(length / (dashLength + spaceLength));
            var startSpace = (length + spaceLength - (dashLength + spaceLength) * partCount) / 2;

            var startT = startSpace / length;
            var partT = dashLength / length;
            var spaceT = spaceLength / length;

            var parts = new List<PartT>(partCount);

            for (var i = 0; i < partCount; i += 1)
            {
                var tStart = startT + (partT + spaceT) * i;
                var tEnd = tStart + partT;

                parts.Add(new PartT(tStart, tEnd));
            }

            return parts;
        }
        public static bool CalculateDashedParts(LineBorders borders, ITrajectory trajectory, float startT, float endT, float dashLength, float offset, float width, Color32 color, out MarkingPartData part)
        {
            part = CalculateDashedPart(trajectory, startT, endT, dashLength, offset, width, color);

            if (borders.IsEmpty)
                return true;

            var vertex = borders.GetVertex(part);
            return !borders.Any(c => vertex.Any(v => Intersection.CalculateSingle(c, v).isIntersect));

        }
        public static MarkingPartData CalculateDashedPart(ITrajectory trajectory, float startT, float endT, float dashLength, float offset, float width, Color32 color)
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
        public static MarkingPartData CalculateDashedPart(ITrajectory trajectory, float startT, float endT, float dashLength, Vector3 startOffset, Vector3 endOffset, float width, Color32 color, float? angle = null)
        {
            var startPosition = trajectory.Position(startT);
            var endPosition = trajectory.Position(endT);

            startPosition += startOffset;
            endPosition += endOffset;

            var dir = angle?.Direction() ?? (endPosition - startPosition);

            return new MarkingPartData(startPosition, endPosition, dir, dashLength, width, color, RenderHelper.MaterialLib[MaterialType.RectangleLines]);
        }

        public static bool CalculateSolidPart(LineBorders borders, ITrajectory trajectory, float offset, float width, Color32 color, out MarkingPartData part)
        {
            part = CalculateSolidPart(trajectory, offset, width, color);

            if (borders.IsEmpty)
                return true;

            var vertex = borders.GetVertex(part);

            var from = 0f;
            var to = 1f;

            foreach (var border in borders)
            {
                for (var i = 1; i < vertex.Length; i += 2)
                {
                    var start = Intersection.CalculateSingle(border, vertex[i - 1]);
                    var end = Intersection.CalculateSingle(border, vertex[i]);

                    if (start.isIntersect && end.isIntersect)
                        return false;

                    if (!start.isIntersect && !end.isIntersect)
                        continue;

                    if (Intersection.CalculateSingle(border, new StraightTrajectory(vertex[i - 1].EndPosition, vertex[i].EndPosition), out _, out var t))
                    {
                        if (start.isIntersect)
                            from = Mathf.Max(from, t);
                        else if (end.isIntersect)
                            to = Mathf.Min(to, t);
                    }
                }
            }

            if (from != 0f || to != 1f)
            {
                var dir = part.Angle.Direction() * (part.Length / 2);
                var line = new StraightTrajectory(part.Position + dir, part.Position - dir).Cut(from, to);
                part = new MarkingPartData(line.StartPosition, line.EndPosition, line.Direction, part.Width, part.Color, RenderHelper.MaterialLib[MaterialType.RectangleLines]);
            }
            return true;
        }
        public static MarkingPartData CalculateSolidPart(ITrajectory trajectory, float offset, float width, Color32 color)
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
        public static MarkingPartData CalculateSolidPart(ITrajectory trajectory, Vector3 startOffset, Vector3 endOffset, float width, Color32 color)
        {
            var startPosition = trajectory.StartPosition + startOffset;
            var endPosition = trajectory.EndPosition + endOffset;
            return new MarkingPartData(startPosition, endPosition, endPosition - startPosition, width, color, RenderHelper.MaterialLib[MaterialType.RectangleLines]);
        }
        private static Dictionary<MarkingLOD, float> LodMax { get; } = new Dictionary<MarkingLOD, float>
        {
            {MarkingLOD.LOD0, 0.2f},
            {MarkingLOD.LOD1, 1f}
        };
        public static void GetParts(float width, float offset, MarkingLOD lod, out int count, out float partWidth)
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

        public readonly struct PartT
        {
            public readonly float start;
            public readonly float end;

            public PartT(float start, float end)
            {
                this.start = start;
                this.end = end;
            }
            public override string ToString() => $"{start}:{end}";
        }
        public static Contour SetCornerRadius(this Contour originalEdges, float lineRadius, float medianRadius)
        {
            var edges = new Contour(originalEdges);

            if (lineRadius > 0f || medianRadius > 0f)
            {
                for (var i = 0; i < edges.Count; i += 1)
                {
                    if (SetRadius(i, edges, lineRadius, medianRadius))
                        i += 1;
                }
            }

            return edges;
        }
        private static bool SetRadius(int i, Contour parts, float lineRadius, float medianRadius)
        {
            var j = (i + 1) % parts.Count;
            var radius = (parts[i].isEnter || parts[j].isEnter) ? medianRadius : lineRadius;
            if (radius <= 0f)
                return false;

            var iParts = CalculateSolid(parts[i].trajectory, 5, 1f, 40f, Calculate);
            var jParts = CalculateSolid(parts[j].trajectory, 5, 1f, 40f, Calculate);

            var width = Math.Max(iParts.Count, jParts.Count);
            width = (width % 2 == 0 ? width : width + 1) / 2;
            var sum = iParts.Count + jParts.Count - 1;
            var center = Vector3.zero;
            var firstDir = Vector3.zero;
            var secondDir = Vector3.zero;

            for (var k = 0; k < width; k += 1)
            {
                for (var l = k * 2; l < sum; l += 1)
                {
                    var first = (l / 2) + k + (l % 2);
                    var second = (l / 2) - k;

                    if (first < iParts.Count && second < jParts.Count)
                    {
                        if (CheckRadius(iParts[iParts.Count - 1 - first], jParts[second], radius, ref center, ref firstDir, ref secondDir))
                        {
                            AddRadius(i, j, parts, center, firstDir, secondDir);
                            return true;
                        }
                    }
                    if (first != second && first < jParts.Count && second < iParts.Count)
                    {
                        if (CheckRadius(iParts[iParts.Count - 1 - second], jParts[first], radius, ref center, ref firstDir, ref secondDir))
                        {
                            AddRadius(i, j, parts, center, firstDir, secondDir);
                            return true;
                        }
                    }
                }
            }

            return false;

            static StraightTrajectory Calculate(ITrajectory trajectory)
            {
                if (trajectory is StraightTrajectory straight)
                    return straight;
                else
                    return new StraightTrajectory(trajectory.StartPosition, trajectory.EndPosition);
            }
        }
        private static bool CheckRadius(StraightTrajectory first, StraightTrajectory second, float radius, ref Vector3 center, ref Vector3 firstDir, ref Vector3 secondDir)
        {
            var angleA = Vector3.Angle(first.Direction.MakeFlat(), second.Direction.MakeFlat());
            first = new StraightTrajectory(first.StartPosition.MakeFlat(), first.EndPosition.MakeFlat(), false);
            second = new StraightTrajectory(second.StartPosition.MakeFlat(), second.EndPosition.MakeFlat(), false);

            if (!Intersection.CalculateSingle(first, second, out var firstT, out var secondT))
                return false;

            var position = (first.Position(firstT) + second.Position(secondT)) / 2f;
            var direction = (first.Direction - second.Direction).normalized * (firstT < 0 ? 1f : -1f);
            var distance = radius / Mathf.Cos(angleA / 2f * Mathf.Deg2Rad);

            center = position + direction * distance;
            firstDir = first.Direction.Turn90(true);
            secondDir = second.Direction.Turn90(true);

            var firstInter = Intersection.CalculateSingle(first, new StraightTrajectory(center, center + firstDir, false));
            var secondInter = Intersection.CalculateSingle(second, new StraightTrajectory(center, center + secondDir, false));
            return firstInter.isIntersect && CorrectT(firstInter.firstT, first.Length) && secondInter.isIntersect && CorrectT(secondInter.firstT, second.Length);

            static bool CorrectT(float t, float length) => -0.05f / length < t && t < 1f + 0.05f / length;
        }
        private static void AddRadius(int i, int j, Contour parts, Vector3 center, Vector3 firstDir, Vector3 secondDir)
        {
            var firstInter = Intersection.CalculateSingle(parts[i].trajectory, new StraightTrajectory(center, center + firstDir, false), out var firstT, out _);
            var secondInter = Intersection.CalculateSingle(new StraightTrajectory(center, center + secondDir, false), parts[j].trajectory, out _, out var secondT);
            if (firstInter && secondInter)
            {
                parts[i] = new ContourEdge(parts[i].trajectory.Cut(0f, firstT), parts[i].isEnter);
                parts[j] = new ContourEdge(parts[j].trajectory.Cut(secondT, 1f), parts[j].isEnter);

                var corner = new BezierTrajectory(parts[i].trajectory.EndPosition, -parts[i].trajectory.EndDirection, parts[j].trajectory.StartPosition, -parts[j].trajectory.StartDirection);

                parts.Insert(j, new ContourEdge(corner));
            }
        }
    }

    public class TrajectoryIntersect
    {
        public static IntersectComparer Comparer { get; } = new IntersectComparer();
        public readonly int index;
        public readonly float t;
        public TrajectoryIntersect Other { get; private set; }

        public TrajectoryIntersect(int index, float t)
        {
            this.index = index;
            this.t = t;
        }

        public static void Create(int i, int j, float iT, float jT, out TrajectoryIntersect first, out TrajectoryIntersect second)
        {
            first = new TrajectoryIntersect(i, iT);
            second = new TrajectoryIntersect(j, jT);

            first.Other = second;
            second.Other = first;
        }
        public override string ToString() => Other != null ? $"{index}:{t} × {Other.index}:{Other.t}" : $"{index}:{t} × null";

        public class IntersectComparer : IComparer<TrajectoryIntersect>
        {
            public int Compare(TrajectoryIntersect x, TrajectoryIntersect y) => x.t.CompareTo(y.t);
        }
    }
}
