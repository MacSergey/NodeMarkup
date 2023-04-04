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
        private static int MaxDepth => 5;

        public struct SplitParams
        {
            public float minAngle;
            public float minLength;
            public float maxLength;
            public float maxHeight;

            public static SplitParams Default => new SplitParams()
            {
                minAngle = 5f,
                minLength = 1f,
                maxLength = 10f,
                maxHeight = 3f,
            };
        }

        #region SOLID

        public static List<PartT> CalculateSolid(ITrajectory trajectory, MarkingLOD lod, SplitParams splitParams)
        {
            var lodScale = lod switch
            {
                MarkingLOD.LOD0 or MarkingLOD.NoLOD => 1f,
                MarkingLOD.LOD1 => 4f,
            };

            splitParams.minLength *= lodScale;
            splitParams.maxLength *= lodScale;
            splitParams.minAngle *= lodScale;

            var parts = new List<PartT>();

            CalculateSolid(parts, trajectory, splitParams, 0, 0, 1, trajectory.DeltaAngle);

            return parts;
        }
        public static List<PartT> CalculateSolid(ITrajectory trajectory, SplitParams splitParams)
        {
            var parts = new List<PartT>();
            CalculateSolid(parts, trajectory, splitParams, 0, 0, 1, trajectory.DeltaAngle);
            return parts;
        }
        private static void CalculateSolid(List<PartT> parts, ITrajectory trajectory, SplitParams splitParams, int depth, int index, int total, float deltaAngle)
        {
            var startT = 1f / total * index;
            var endT = 1f / total * (index + 1);
            var startPos = trajectory.Position(startT);
            var endPos = trajectory.Position(endT);
            var length = (endPos - startPos).magnitude;
            var height = Mathf.Abs(endPos.y - startPos.y);

            var needDivide = (deltaAngle > splitParams.minAngle && length >= splitParams.minLength) || length > splitParams.maxLength || height > splitParams.maxHeight;
            if (depth < MaxDepth && (needDivide || depth == 0))
            {
                var middleT = (startT + endT) * 0.5f;

                var startDir = trajectory.Tangent(startT);
                var middleDir = trajectory.Tangent(middleT);
                var endDir = trajectory.Tangent(endT);

                var firstDeltaAngle = 180 - Vector3.Angle(startDir, -middleDir);
                var secondDeltaAngle = 180 - Vector3.Angle(middleDir, -endDir);

                if (needDivide || deltaAngle > splitParams.minAngle || (firstDeltaAngle + secondDeltaAngle) > splitParams.minAngle)
                {
                    CalculateSolid(parts, trajectory, splitParams, depth + 1, index * 2, total * 2, firstDeltaAngle);
                    CalculateSolid(parts, trajectory, splitParams, depth + 1, index * 2 + 1, total * 2, secondDeltaAngle);

                    return;
                }
            }

            parts.Add(new PartT(startT, endT));
        }

        #endregion

        #region DASHED

        public static List<PartT> CalculateDashed(ITrajectory trajectory, float dashLength, float spaceLength)
        {
            switch (trajectory)
            {
                case BezierTrajectory bezierTrajectory:
                    return CalculateDashesBezierT(bezierTrajectory, dashLength, spaceLength);
                case StraightTrajectory straightTrajectory:
                    return CalculateDashesStraightT(straightTrajectory, dashLength, spaceLength);
                default:
                    return new List<PartT>();
            }
        }

        public static List<PartT> CalculateDashesBezierT(ITrajectory trajectory, float dashLength, float spaceLength, uint iterations = 3)
        {
            var points = new TrajectoryPoints(trajectory);
            var startSpace = spaceLength * 0.5f;

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

                startSpace = (startSpace + endSpace) * 0.5f;
            }
        }
        public static List<PartT> CalculateDashesStraightT(StraightTrajectory straightTrajectory, float dashLength, float spaceLength)
        {
            var length = straightTrajectory.Length;
            var partCount = (int)(length / (dashLength + spaceLength));
            var startSpace = (length + spaceLength - (dashLength + spaceLength) * partCount) * 0.5f;

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

        #endregion

        public static void GetPartParams(ITrajectory trajectory, PartT partT, float offset, out Vector3 position, out Vector3 direction)
        {
            if (offset == 0)
                GetPartParams(trajectory, partT, Vector3.zero, Vector3.zero, out position, out direction);
            else
            {
                var startOffset = trajectory.Tangent(partT.start).Turn90(true).normalized * offset;
                var endOffset = trajectory.Tangent(partT.end).Turn90(true).normalized * offset;
                GetPartParams(trajectory, partT, startOffset, endOffset, out position, out direction);
            }
        }
        public static void GetPartParams(ITrajectory trajectory, PartT partT, float offset, out Vector3 startPosition, out Vector3 endPosition, out Vector3 direction)
        {
            if (offset == 0)
                GetPartParams(trajectory, partT, Vector3.zero, Vector3.zero, out startPosition, out endPosition, out direction);
            else
            {
                var startOffset = trajectory.Tangent(partT.start).Turn90(true).normalized * offset;
                var endOffset = trajectory.Tangent(partT.end).Turn90(true).normalized * offset;
                GetPartParams(trajectory, partT, startOffset, endOffset, out startPosition, out endPosition, out direction);
            }
        }
        public static void GetPartParams(ITrajectory trajectory, PartT partT, Vector3 startOffset, Vector3 endOffset, out Vector3 position, out Vector3 direction)
        {
            var startPosition = trajectory.Position(partT.start) + startOffset;
            var endPosition = trajectory.Position(partT.end) + endOffset;
            position = (startPosition + endPosition) * 0.5f;
            direction = (endPosition - startPosition).normalized;
        }
        public static void GetPartParams(ITrajectory trajectory, PartT partT, Vector3 startOffset, Vector3 endOffset, out Vector3 startPosition, out Vector3 endPosition, out Vector3 direction)
        {
            startPosition = trajectory.Position(partT.start) + startOffset;
            endPosition = trajectory.Position(partT.end) + endOffset;
            direction = (endPosition - startPosition).normalized;
        }

        public static bool CheckBorders(LineBorders borders, Vector3 pos, Vector3 dir, float length, float width)
        {
            if (borders.IsEmpty)
                return true;

            var vertex = borders.GetVertex(pos, dir, length, width);
            return !borders.Any(c => vertex.Any(v => Intersection.CalculateSingle(c, v).isIntersect));
        }
        public static bool CheckBorders(LineBorders borders, ref Vector3 startPos, ref Vector3 endPos, Vector3 dir, float width)
        {
            if (borders.IsEmpty)
                return true;

            var pos = (startPos + endPos) * 0.5f;
            var length = (endPos - startPos).magnitude;
            var vertex = borders.GetVertex(pos, dir, length, width);

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
                var line = new StraightTrajectory(startPos, endPos).Cut(from, to);
                startPos = line.StartPosition;
                endPos = line.EndPosition;
            }
            return true;
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
        private static bool SetRadius(int i, Contour contour, float lineRadius, float medianRadius)
        {
            var j = (i + 1) % contour.Count;
            var radius = (contour[i].isEnter || contour[j].isEnter) ? medianRadius : lineRadius;
            if (radius <= 0f)
                return false;

            var iParts = CalculateSolid(contour[i].trajectory, new SplitParams() { minAngle = 3f, minLength = 0.3f, maxLength = 40f, maxHeight = 10f });
            var jParts = CalculateSolid(contour[j].trajectory, new SplitParams() { minAngle = 3f, minLength = 0.3f, maxLength = 40f, maxHeight = 10f });

            var width = Math.Max(iParts.Count, jParts.Count);
            width = (width % 2 == 0 ? width : width + 1) / 2;
            var sum = iParts.Count + jParts.Count - 1;

            for (var k = 0; k < width; k += 1)
            {
                for (var l = k * 2; l < sum; l += 1)
                {
                    var first = (l / 2) + k + (l % 2);
                    var second = (l / 2) - k;

                    if (first < iParts.Count && second < jParts.Count)
                    {
                        if (CheckRadius(i, j, contour, iParts[iParts.Count - 1 - first], jParts[second], radius))
                            return true;
                    }

                    if (first != second && first < jParts.Count && second < iParts.Count)
                    {
                        if (CheckRadius(i, j, contour, iParts[iParts.Count - 1 - second], jParts[first], radius))
                            return true;
                    }
                }
            }

            return false;
        }
        private static bool CheckRadius(int i, int j, Contour parts, PartT firstPart, PartT secondPart, float radius)
        {
            var firstStartPos = parts[i].trajectory.Position(firstPart.start).MakeFlat();
            var firstEndPos = parts[i].trajectory.Position(firstPart.end).MakeFlat();

            var secondStartPos = parts[j].trajectory.Position(secondPart.start).MakeFlat();
            var secondEndPos = parts[j].trajectory.Position(secondPart.end).MakeFlat();

            var firstDir = (firstEndPos - firstStartPos).normalized;
            var secondDir = (secondEndPos - secondStartPos).normalized;

            var first = new StraightTrajectory(firstStartPos, firstStartPos + firstDir, false);
            var second = new StraightTrajectory(secondStartPos, secondStartPos + secondDir, false);

            if (!Intersection.CalculateSingle(first, second, out var firstInterT, out var secondInterT))
                return false;

            var angleA = Vector3.Angle(firstDir, -secondDir);
            var tan = Mathf.Tan(angleA * 0.5f * Mathf.Deg2Rad);
            var distance = radius / tan;
            var delta = 0.2f / tan;

            var firstLen = (firstEndPos - firstStartPos).magnitude;
            var secondLen = (secondEndPos - secondStartPos).magnitude;

            var firstStartDist = firstInterT;
            var firstEndDist = firstStartDist - firstLen;

            var secondStartDist = -secondInterT;
            var secondEndDist = secondStartDist + secondLen;

            var firstStartDelta = (firstStartDist + delta) - distance;
            var firstEndDelta = distance - (firstEndDist - delta);
            var secondStartDelta = distance - (secondStartDist - delta);
            var secondEndDelta = (secondEndDist + delta) - distance;

            if (firstStartDelta < 0 || firstEndDelta < 0 || secondStartDelta < 0 || secondEndDelta < 0)
                return false;

            var firstT = Mathf.Lerp(firstPart.start, firstPart.end, (firstStartDist - distance) / firstLen);
            var secondT = Mathf.Lerp(secondPart.start, secondPart.end, (distance - secondStartDist) / secondLen);

            var startPos = parts[i].trajectory.Position(firstT);
            var startDir = parts[i].trajectory.Tangent(firstT);
            var endPos = parts[j].trajectory.Position(secondT);
            var endDir = -parts[j].trajectory.Tangent(secondT);

            parts[i] = new ContourEdge(parts[i].trajectory.Cut(0f, firstT), parts[i].isEnter);
            parts[j] = new ContourEdge(parts[j].trajectory.Cut(secondT, 1f), parts[j].isEnter);

            parts.Insert(j, new ContourEdge(new BezierTrajectory(startPos, startDir, endPos, endDir, BezierTrajectory.Data.Default)));

            return true;
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

            public PartT Invert => new PartT(end, start);

            public override string ToString() => $"{start}:{end}";
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
