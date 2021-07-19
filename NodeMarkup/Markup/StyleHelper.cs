using ModsCommon.Utilities;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;

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
        public static List<ITrajectory> CalculateSolid(ITrajectory trajectory, float minAngle, float minLength, float maxLength)
        {
            var result = new List<ITrajectory>();
            CalculateSolid(0, trajectory, trajectory.DeltaAngle, minAngle, minLength, maxLength, t => result.Add(t));
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
                foreach (var part in calculateDashes(trajectory, partT.Start, partT.End))
                    yield return part;
            }
        }
        public static List<PartT> CalculateDashesBezierT(BezierTrajectory trajectory, float dashLength, float spaceLength, uint iterations = 3)
        {
            var points = new TrajectoryPoints(trajectory);
            return CalculateDashesBezierT(points, dashLength, spaceLength, iterations);
        }
        public static List<PartT> CalculateDashesBezierT(IEnumerable<ITrajectory> trajectories, float dashLength, float spaceLength, uint iterations = 3)
        {
            var points = new TrajectoryPoints(trajectories.ToArray());
            return CalculateDashesBezierT(points, dashLength, spaceLength, iterations);
        }

        private static List<PartT> CalculateDashesBezierT(TrajectoryPoints points, float dashLength, float spaceLength, uint iterations)
        {
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
                        parts.Add(new PartT() { Start = currentT, End = nextT });

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

                parts.Add(new PartT { Start = tStart, End = tEnd });
            }

            return parts;
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

            public override string ToString() => $"{Start}:{End}";
        }

        public static List<FillerContour.Part> RemoveSelfIntersections(List<FillerContour.Part> parts)
        {
            var result = new List<FillerContour.Part>(parts);

            if (parts.Count > 3)
            {
                for (var i = 0; i < parts.Count; i += 1)
                {
                    for (var j = 2; j < parts.Count - 1; j += 1)
                    {
                        var x = i;
                        var y = (i + j) % parts.Count;
                        var intersect = Intersection.CalculateSingle(parts[x].Trajectory, parts[y].Trajectory);
                        if (intersect.IsIntersect && (intersect.FirstT > 0.5f || intersect.SecondT < 0.5f))
                        {
                            var xPart = parts[x];
                            var yPart = parts[y];
                            xPart.Trajectory = xPart.Trajectory.Cut(0f, intersect.FirstT);
                            yPart.Trajectory = yPart.Trajectory.Cut(intersect.SecondT, 1f);
                            parts[x] = xPart;
                            parts[y] = yPart;

                            if (y > x)
                            {
                                var count = y - (x + 1);
                                parts.RemoveRange(x + 1, count);
                                j -= count;
                            }
                            else
                            {
                                parts.RemoveRange(x + 1, parts.Count - (x + 1));
                                parts.RemoveRange(0, y);
                                i -= y;
                            }
                        }
                    }
                }
            }

            return result;
        }
        public static List<List<FillerContour.Part>> SetOffset(List<FillerContour.Part> originalParts, float offset, float medianOffset)
        {
            var direction = originalParts.Select(i => i.Trajectory).GetDirection();

            var parts = Move(originalParts, direction, offset, medianOffset);
            Connect(parts, originalParts, offset, medianOffset);
            var intersections = GetIntersections(parts);
            var partOfPart = GetParts(parts, intersections);
            var contours = GetContours(partOfPart);

            var result = new List<List<FillerContour.Part>>();
            foreach (var contour in contours)
            {
                if (contour.Direction == direction)
                {
                    var processed = contour.Select(i => new FillerContour.Part(i.Processed)).ToList();
                    result.Add(processed);
                }
            }

            return result;
        }
        private static List<FillerContour.Part> Move(List<FillerContour.Part> originalParts, TrajectoryHelper.Direction direction, float offset, float medianOffset)
        {
            var result = new List<FillerContour.Part>(originalParts.Count);

            foreach (var part in originalParts)
            {
                var move = part.IsEnter ? medianOffset : offset;

                if (move == 0f)
                    result.Add(part);
                else
                {
                    var trajectory = part.Trajectory;
                    var startNormal = trajectory.StartDirection.MakeFlatNormalized().Turn90(direction == TrajectoryHelper.Direction.ClockWise) * move;
                    var endNormal = trajectory.EndDirection.MakeFlatNormalized().Turn90(direction == TrajectoryHelper.Direction.CounterClockWise) * move;

                    var movedTrajectory = part.IsEnter ? (ITrajectory)new StraightTrajectory(trajectory.StartPosition + startNormal, trajectory.EndPosition + endNormal) : (ITrajectory)new BezierTrajectory(trajectory.StartPosition + startNormal, trajectory.StartDirection, trajectory.EndPosition + endNormal, trajectory.EndDirection);
                    var newPart = new FillerContour.Part(movedTrajectory, part.IsEnter);
                    result.Add(newPart);
                }
            }

            return result;
        }
        private static void Connect(List<FillerContour.Part> parts, List<FillerContour.Part> originalParts, float offset, float medianOffset)
        {
            var count = 0;
            for (var i = 0; i < parts.Count; i += 1)
            {
                var j = (i + 1) % parts.Count;
                var first = parts[i];
                var second = parts[j];

                if ((first.IsEnter ? medianOffset : offset) != 0)
                {
                    if ((second.IsEnter ? medianOffset : offset) != 0)
                    {
                        var nextCount = (count + 1) % originalParts.Count;
                        var firstTrajectory = new StraightTrajectory(first.Trajectory.EndPosition, originalParts[count].Trajectory.EndPosition);
                        var secondTrajectory = new StraightTrajectory(originalParts[nextCount].Trajectory.StartPosition, second.Trajectory.StartPosition);

                        AddToList(parts, i + 1, new FillerContour.Part(firstTrajectory));
                        AddToList(parts, i + 2, new FillerContour.Part(secondTrajectory));
                        i += 2;
                    }
                    else if (Intersection.CalculateSingle(first.Trajectory, second.Trajectory, out var firstT, out var secondT))
                    {
                        if (first.Trajectory.Length * firstT < 0.1f || second.Trajectory.Length * secondT < 0.1f)
                        {
                            first = new FillerContour.Part(first.Trajectory.Cut(0f, firstT), first.IsEnter);
                            parts[i] = first;
                            second = new FillerContour.Part(second.Trajectory.Cut(secondT, 1f), second.IsEnter);
                            parts[j] = second;
                        }
                        else
                            Add(parts, ref i, first.Trajectory.EndPosition, second.Trajectory.StartPosition);
                    }
                    else if (Intersection.CalculateSingle(new StraightTrajectory(first.Trajectory.EndPosition, first.Trajectory.EndPosition - first.Trajectory.EndDirection), second.Trajectory, out _, out var t))
                    {
                        second = new FillerContour.Part(second.Trajectory.Cut(t, 1f), second.IsEnter);
                        parts[j] = second;

                        Add(parts, ref i, first.Trajectory.EndPosition, second.Trajectory.StartPosition);
                    }
                    else
                        Add(parts, ref i, first.Trajectory.EndPosition, second.Trajectory.StartPosition);
                }
                else if ((second.IsEnter ? medianOffset : offset) != 0)
                {
                    if (Intersection.CalculateSingle(first.Trajectory, second.Trajectory, out var firstT, out var secondT))
                    {
                        if (first.Trajectory.Length * firstT < 0.1f || second.Trajectory.Length * secondT < 0.1f)
                        {
                            first = new FillerContour.Part(first.Trajectory.Cut(0f, firstT), first.IsEnter);
                            parts[i] = first;
                            second = new FillerContour.Part(second.Trajectory.Cut(secondT, 1f), second.IsEnter);
                            parts[j] = second;
                        }
                        else
                            Add(parts, ref i, first.Trajectory.EndPosition, second.Trajectory.StartPosition);
                    }
                    else if (Intersection.CalculateSingle(first.Trajectory, new StraightTrajectory(second.Trajectory.StartPosition, second.Trajectory.StartPosition - second.Trajectory.StartDirection), out var t, out _))
                    {
                        var newTrajectory = first.Trajectory.Cut(0f, t);
                        first = new FillerContour.Part(newTrajectory, first.IsEnter);
                        parts[i] = first;

                        Add(parts, ref i, first.Trajectory.EndPosition, second.Trajectory.StartPosition);
                    }
                    else
                        Add(parts, ref i, first.Trajectory.EndPosition, second.Trajectory.StartPosition);
                }

                count += 1;
            }

            static void Add(List<FillerContour.Part> parts, ref int i, Vector3 start, Vector3 end)
            {
                var addTrajectory = new StraightTrajectory(start, end);
                if (addTrajectory.Length >= 0.01f)
                {
                    AddToList(parts, i + 1, new FillerContour.Part(addTrajectory));
                    i += 1;
                }
            }
            static void AddToList(List<FillerContour.Part> parts, int i, FillerContour.Part value)
            {
                if (i >= parts.Count)
                    parts.Add(value);
                else
                    parts.Insert(i, value);
            }
        }
        private static List<TrajectoryIntersect>[] GetIntersections(List<FillerContour.Part> parts)
        {
            var partsIntersections = new List<TrajectoryIntersect>[parts.Count];

            for (var i = 0; i < parts.Count; i += 1)
                partsIntersections[i] = new List<TrajectoryIntersect>();

            for (var i = 0; i < parts.Count; i += 1)
            {
                var j = (i + 1) % parts.Count;
                TrajectoryIntersect.Create(i, j, 1f, 0f, out var iIntersect, out var jIntersect);
                partsIntersections[i].Add(iIntersect);
                partsIntersections[j].Add(jIntersect);

                for (j = i + 2; j < (i == 0 ? parts.Count - 1 : parts.Count); j += 1)
                {
                    var intersections = Intersection.Calculate(parts[i].Trajectory, parts[j].Trajectory);
                    foreach (var intersection in intersections)
                    {
                        //if (intersection.FirstT > 1f - (0.01f / parts[i].Trajectory.Length) && intersection.SecondT < (0.01f / parts[j].Trajectory.Length))
                        //    continue;

                        TrajectoryIntersect.Create(i, j, intersection.FirstT, intersection.SecondT, out iIntersect, out jIntersect);
                        partsIntersections[i].Add(iIntersect);
                        partsIntersections[j].Add(jIntersect);
                    }
                }
            }

            for (var i = 0; i < parts.Count; i += 1)
                partsIntersections[i].Sort(TrajectoryIntersect.Comparer);

            return partsIntersections;
        }
        private static List<TrajectoryPart>[] GetParts(List<FillerContour.Part> parts, List<TrajectoryIntersect>[] intersections)
        {
            var partOfParts = new List<TrajectoryPart>[intersections.Length];

            for (var i = 0; i < intersections.Length; i += 1)
            {
                partOfParts[i] = new List<TrajectoryPart>();
                var intersection = intersections[i];
                for (var j = 0; j < intersection.Count - 1; j += 1)
                {
                    var part = new TrajectoryPart(parts[i].Trajectory, intersection[j], intersection[j + 1]);
                    partOfParts[i].Add(part);
                }
            }

            return partOfParts;
        }
        private static List<TrajectoryContour> GetContours(List<TrajectoryPart>[] parts)
        {
            var process = new List<TrajectoryPart>(parts.Length);
            foreach (var part in parts)
                process.AddRange(part);

            var countours = new List<TrajectoryContour>();

            while (process.Count != 0)
            {
                var contour = new TrajectoryContour();
                var part = process[0];
                process.RemoveAt(0);
                contour.Add(part);

                while (process.Count != 0)
                {
                    var index = process.FindIndex(p => p.Start == part.End.Other);
                    if (index < 0)
                        break;

                    part = process[index];
                    process.RemoveAt(index);
                    contour.Add(part);

                    if (part.End.Other == contour.First().Start)
                    {
                        countours.Add(contour);
                        break;
                    }
                }
            }

            return countours;
        }
        private class TrajectoryIntersect
        {
            public static IntersectComparer Comparer { get; } = new IntersectComparer();
            public int Index { get; private set; }
            public float T { get; private set; }
            public TrajectoryIntersect Other { get; private set; }

            public static void Create(int i, int j, float iT, float jT, out TrajectoryIntersect first, out TrajectoryIntersect second)
            {
                first = new TrajectoryIntersect()
                {
                    Index = i,
                    T = iT,
                };
                second = new TrajectoryIntersect()
                {
                    Index = j,
                    T = jT,
                };

                first.Other = second;
                second.Other = first;
            }
            public override string ToString() => Other != null ? $"{Index}:{T} × {Other.Index}:{Other.T}" : $"{Index}:{T} × null";

            public class IntersectComparer : IComparer<TrajectoryIntersect>
            {
                public int Compare(TrajectoryIntersect x, TrajectoryIntersect y) => x.T.CompareTo(y.T);
            }
        }
        private class TrajectoryPart
        {
            public ITrajectory Trajectory { get; }
            public TrajectoryIntersect Start { get; }
            public TrajectoryIntersect End { get; }
            public ITrajectory Processed => Trajectory.Cut(Start.T, End.T);
            public TrajectoryPart(ITrajectory trajectory, TrajectoryIntersect start, TrajectoryIntersect end)
            {
                Trajectory = trajectory;
                Start = start;
                End = end;
            }

            public override string ToString() => $"{Start} — {End}";
        }
        private class TrajectoryContour : List<TrajectoryPart>
        {
            public TrajectoryHelper.Direction Direction
            {
                get
                {
                    var processed = this.Select(i => i.Processed).ToArray();
                    return processed.GetDirection();
                }
            }
        }
    }
}
