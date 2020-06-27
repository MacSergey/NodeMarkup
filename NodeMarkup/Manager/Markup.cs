using ColossalFramework.Math;
using NodeMarkup.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public class Markup
    {
        public ushort NodeId { get; }
        Dictionary<ushort, SegmentEnter> EntersDictionary { get; set; } = new Dictionary<ushort, SegmentEnter>();
        Dictionary<MarkupPointPair, MarkupLine> LinesDictionary { get; } = new Dictionary<MarkupPointPair, MarkupLine>(new MarkupPointPairComparer());
        public RenderBatch[] RenderBatches { get; private set; }


        public IEnumerable<MarkupLine> Lines
        {
            get
            {
                foreach (var line in LinesDictionary.Values)
                    yield return line;
            }
        }
        public IEnumerable<SegmentEnter> Enters
        {
            get
            {
                foreach (var enter in EntersDictionary.Values)
                    yield return enter;
            }
        }

        public Markup(ushort nodeId)
        {
            NodeId = nodeId;

            Update();
        }

        public void Update()
        {
            //Logger.LogDebug($"End update node #{NodeId}");

            var node = Utilities.GetNode(NodeId);

            var enters = new Dictionary<ushort, SegmentEnter>();

            foreach (var segmentId in node.SegmentsId())
            {
                if (!EntersDictionary.TryGetValue(segmentId, out SegmentEnter enter))
                    enter = new SegmentEnter(this, segmentId);

                enter.Update();

                enters.Add(segmentId, enter);
            }

            EntersDictionary = enters;

            var pointPairs = LinesDictionary.Keys.ToArray();
            foreach (var pointPair in pointPairs)
            {
                if (EntersDictionary.ContainsKey(pointPair.First.Enter.SegmentId) && EntersDictionary.ContainsKey(pointPair.Second.Enter.SegmentId))
                    LinesDictionary[pointPair].Update();
                else
                    LinesDictionary.Remove(pointPair);
            }

            Recalculate();

                //Logger.LogDebug($"End update node #{NodeId}");
        }
        public void Update(MarkupPoint point)
        {
            point.Update();
            foreach(var line in Lines.Where(l => l.ContainPoint(point)))
            {
                line.Update();
            }
            Recalculate();
        }
        public void Update(MarkupLine line)
        {
            line.Update();
            Recalculate();
        }
        public void Recalculate()
        {
            var dashes = LinesDictionary.Values.SelectMany(l => l.Dashes).ToArray();
            RenderBatches = RenderBatch.FromDashes(dashes);
        }

        public void AddConnect(MarkupPointPair pointPair, MarkupLine.Type lineType)
        {
            var line = new MarkupLine(this, pointPair, lineType);
            LinesDictionary[pointPair] = line;
        }
        public bool ExistConnection(MarkupPointPair pointPair) => LinesDictionary.ContainsKey(pointPair);
        public void RemoveConnect(MarkupPointPair pointPair)
        {
            LinesDictionary.Remove(pointPair);
        }
        public void ToggleConnection(MarkupPointPair pointPair, MarkupLine.Type lineType)
        {
            if (!ExistConnection(pointPair))
                AddConnect(pointPair, lineType);
            else
                RemoveConnect(pointPair);

            Update();
        }
    }
    public struct MarkupPointPair
    {
        public MarkupPoint First;
        public MarkupPoint Second;

        public MarkupPointPair(MarkupPoint first, MarkupPoint second)
        {
            First = first;
            Second = second;
        }
        public bool ContainPoint(MarkupPoint point) => First == point || Second == point;

        public override string ToString() => $"{First}—{Second}";
    }
    public class MarkupPointPairComparer : IEqualityComparer<MarkupPointPair>
    {
        public bool Equals(MarkupPointPair x, MarkupPointPair y) => (x.First == y.First && x.Second == y.Second) || (x.First == y.Second && x.Second == y.First);

        public int GetHashCode(MarkupPointPair pair) => pair.GetHashCode();
    }

    public class SegmentEnter
    {
        public Markup Markup { get; private set; }
        public ushort SegmentId { get; }
        public bool IsStartSide { get; }
        public bool IsLaneInvert { get; }

        SegmentDriveLane[] DriveLanes { get; set; } = new SegmentDriveLane[0];
        SegmentMarkupLine[] Lines { get; set; } = new SegmentMarkupLine[0];
        public MarkupPoint[] Points { get; set; } = new MarkupPoint[0];

        public Vector3 cornerDir;

        public int PointCount => Points.Length;
        public MarkupPoint this[int index] => Points[index];


        public SegmentEnter(Markup markup, ushort segmentId)
        {
            Markup = markup;
            SegmentId = segmentId;
            var segment = Utilities.GetSegment(SegmentId);
            IsStartSide = segment.m_startNode == markup.NodeId;
            IsLaneInvert = IsStartSide ^ segment.IsInvert();

            Update();

            CreatePoints(segment);
        }
        private void CreatePoints(NetSegment segment)
        {
            var info = segment.Info;
            var lanes = segment.GetLanesId().ToArray();
            var driveLanesIdxs = info.m_sortedLanes.Where(s => Utilities.IsDriveLane(info.m_lanes[s]));
            if (!IsLaneInvert)
                driveLanesIdxs = driveLanesIdxs.Reverse();

            DriveLanes = driveLanesIdxs.Select(d => new SegmentDriveLane(this, lanes[d], info.m_lanes[d])).ToArray();

            Lines = new SegmentMarkupLine[DriveLanes.Length + 1];

            for (int i = 0; i < Lines.Length; i += 1)
            {
                var left = i - 1 >= 0 ? DriveLanes[i - 1] : null;
                var right = i < DriveLanes.Length ? DriveLanes[i] : null;
                var markupLine = new SegmentMarkupLine(this, left, right);
                Lines[i] = markupLine;
            }

            var points = new List<MarkupPoint>();
            foreach (var markupLine in Lines)
            {
                var linePoints = markupLine.GetMarkupPoints();
                foreach (var point in linePoints)
                {
                    point.Id = (ushort)(points.Count + 1);
                    points.Add(point);
                }
            }
            Points = points.ToArray();
        }

        public void Update()
        {
            var segment = Utilities.GetSegment(SegmentId);
            var cornerAngle = IsStartSide ? segment.m_cornerAngleStart : segment.m_cornerAngleEnd;
            cornerDir = Vector3.right.TurnDeg(cornerAngle / 255f * 360f, false).normalized * (IsLaneInvert ? -1 : 1);

            foreach (var point in Points)
            {
                point.Update();
            }
        }
        public void Recalculate() => Markup.Recalculate();

        public override string ToString() => SegmentId.ToString();
    }
    public class SegmentDriveLane
    {
        private SegmentEnter Enter { get; }

        public uint LaneId { get; }
        public NetInfo.Lane Info { get; }
        public NetLane NetLane => Utilities.GetLane(LaneId);
        public float Position => Info.m_position;
        public float HalfWidth => Info.m_width / 2;
        public float LeftSidePos => Position + (Enter.IsLaneInvert ? -HalfWidth : HalfWidth);
        public float RightSidePos => Position + (Enter.IsLaneInvert ? HalfWidth : -HalfWidth);

        public SegmentDriveLane(SegmentEnter enter, uint laneId, NetInfo.Lane info)
        {
            Enter = enter;
            LaneId = laneId;
            Info = info;
        }
    }
    public class SegmentMarkupLine
    {
        public SegmentEnter SegmentEnter { get; }

        SegmentDriveLane LeftLane { get; }
        SegmentDriveLane RightLane { get; }
        float Point => SegmentEnter.IsStartSide ? 0f : 1f;

        public bool IsRightEdge => RightLane == null;
        public bool IsLeftEdge => LeftLane == null;
        public bool IsEdge => IsRightEdge ^ IsLeftEdge;
        public bool NeedSplit => !IsEdge && SideDelta >= (RightLane.HalfWidth + LeftLane.HalfWidth) / 2;

        public float CenterDelte => IsEdge ? 0f : Mathf.Abs(RightLane.Position - LeftLane.Position);
        public float SideDelta => IsEdge ? 0f : Mathf.Abs(RightLane.LeftSidePos - LeftLane.RightSidePos);
        public float HalfSideDelta => SideDelta / 2;

        public SegmentMarkupLine(SegmentEnter segmentEnter, SegmentDriveLane leftLane, SegmentDriveLane rightLane)
        {
            SegmentEnter = segmentEnter;
            LeftLane = leftLane;
            RightLane = rightLane;
        }

        public MarkupPoint[] GetMarkupPoints()
        {
            if (IsEdge)
            {
                var point = new MarkupPoint(this, IsRightEdge ? MarkupPoint.Type.RightEdge : MarkupPoint.Type.LeftEdge);
                return new MarkupPoint[] { point };
            }
            else if (NeedSplit)
            {
                var pointLeft = new MarkupPoint(this, MarkupPoint.Type.LeftEdge);
                var pointRight = new MarkupPoint(this, MarkupPoint.Type.RightEdge);
                return new MarkupPoint[] { pointRight, pointLeft };
            }
            else
            {
                var point = new MarkupPoint(this, MarkupPoint.Type.Between);
                return new MarkupPoint[] { point };
            }
        }

        public void GetPositionAndDirection(MarkupPoint.Type pointType, float offset, out Vector3 position, out Vector3 direction)
        {
            if ((pointType & MarkupPoint.Type.Between) != MarkupPoint.Type.None)
                GetMiddlePosition(offset, out position, out direction);

            else if ((pointType & MarkupPoint.Type.Edge) != MarkupPoint.Type.None)
                GetEdgePosition(pointType, offset, out position, out direction);

            else
                throw new Exception();
        }
        void GetMiddlePosition(float offset, out Vector3 position, out Vector3 direction)
        {
            RightLane.NetLane.CalculatePositionAndDirection(Point, out Vector3 rightPos, out Vector3 rightDir);
            LeftLane.NetLane.CalculatePositionAndDirection(Point, out Vector3 leftPos, out Vector3 leftDir);

            var part = (RightLane.HalfWidth + HalfSideDelta) / CenterDelte;
            position = Vector3.Lerp(rightPos, leftPos, part) + SegmentEnter.cornerDir * offset;
            direction = (rightDir + leftDir) / (SegmentEnter.IsStartSide ? -2 : 2);
            direction.Normalize();
        }
        void GetEdgePosition(MarkupPoint.Type pointType, float offset, out Vector3 position, out Vector3 direction)
        {
            float lineShift;
            switch (pointType)
            {
                case MarkupPoint.Type.LeftEdge:
                    RightLane.NetLane.CalculatePositionAndDirection(Point, out position, out direction);
                    lineShift = -RightLane.HalfWidth;
                    break;
                case MarkupPoint.Type.RightEdge:
                    LeftLane.NetLane.CalculatePositionAndDirection(Point, out position, out direction);
                    lineShift = LeftLane.HalfWidth;
                    break;
                default:
                    throw new Exception();
            }
            direction = SegmentEnter.IsStartSide ? -direction : direction;

            var angle = Vector3.Angle(direction, SegmentEnter.cornerDir);
            angle = (angle > 90 ? 180 - angle : angle);
            lineShift /= Mathf.Sin(angle * Mathf.Deg2Rad);

            direction.Normalize();
            position += SegmentEnter.cornerDir * (lineShift + offset);
        }
    }
    public class MarkupPoint
    {
        static Color32[] LinePointColors { get; } = new Color32[]
        {
            new Color32(204, 0, 0, 224),
            new Color32(0, 204, 0, 224),
            new Color32(0, 0, 204, 224),
            new Color32(204, 0, 255, 224),
            new Color32(255, 204, 0, 224),
            new Color32(0, 255, 204, 224),
            new Color32(204, 255, 0, 224),
            new Color32(0, 204, 255, 224),
            new Color32(255, 0, 204, 224),
        };

        float _offset = 0;

        public ushort Id { get; set; }
        public Color32 Color => LinePointColors[(Id - 1) % LinePointColors.Length];

        public static Vector3 MarkerSize { get; } = Vector3.one * 1f;
        public Vector3 Position { get; private set; }
        public Vector3 Direction { get; private set; }
        public Type PointType { get; private set; }
        public Bounds Bounds { get; private set; }

        SegmentMarkupLine MarkupLine { get; }
        public SegmentEnter Enter => MarkupLine.SegmentEnter;
        public Markup Markup => Enter.Markup;

        public float Offset
        {
            get => _offset;
            set
            {
                _offset = value;
                Markup.Update(this);
            }
        }

        public MarkupPoint(SegmentMarkupLine markupLine, Type pointType)
        {
            MarkupLine = markupLine;
            PointType = pointType;

            Update();
        }

        public void Update()
        {
            MarkupLine.GetPositionAndDirection(PointType, Offset, out Vector3 position, out Vector3 direction);
            Position = position;
            Direction = direction;
            Bounds = new Bounds(Position, MarkerSize);
        }
        public bool IsIntersect(Ray ray) => Bounds.IntersectRay(ray);

        public enum Type
        {
            None = 0,
            Edge = 1,
            LeftEdge = 2 + Edge,
            RightEdge = 4 + Edge,
            Between = 8,
            BetweenSomeDir = 16 + Between,
            BetweenDiffDir = 32 + Between,
        }

        public override string ToString() => $"{Enter}-{Id}";
    }
    public class MarkupLine
    {
        public static float DashLength { get; } = 1.5f;
        public static float DashSpace { get; } = 1.5f;
        public static float DashWidth { get; } = 0.15f;
        public static float MinAngleDelta { get; } = 5f;
        public static float MaxLength { get; } = 10f;
        public static float MinLength { get; } = 1f;
        public static Color DashColor { get; } = new Color(0.1f, 0.1f, 0.1f, 0.5f);

        public Markup Markup { get; private set; }

        public MarkupPointPair PointPair { get; }
        public MarkupPoint Start => PointPair.First;
        public MarkupPoint End => PointPair.Second;

        public Type LineType { get; }

        public Bezier3 Trajectory { get; private set; }
        public float Length { get; private set; }
        public MarkupDash[] Dashes { get; private set; }


        public MarkupLine(Markup markup, MarkupPointPair pointPair, MarkupLine.Type lineType)
        {
            Markup = markup;
            PointPair = pointPair;
            LineType = lineType;

            Update();
        }

        public void Update()
        {
            var trajectory = new Bezier3
            {
                a = PointPair.First.Position,
                d = PointPair.Second.Position,
            };
            NetSegment.CalculateMiddlePoints(trajectory.a, PointPair.First.Direction, trajectory.d, PointPair.Second.Direction, true, true, out trajectory.b, out trajectory.c);

            Trajectory = trajectory;
            Length = trajectory.Length(MinAngleDelta);

            switch (LineType)
            {
                case Type.Dash:
                    Dashes = CalcucalteDashes(Trajectory);
                    break;
                case Type.Solid:
                    Dashes = CalculateSolid(Trajectory);
                    break;
            }
        }
        private MarkupDash[] CalcucalteDashes(Bezier3 trajectory)
        {
            var dashCount = (int)((Length - DashSpace) / (DashLength + DashSpace));
            var dashes = new MarkupDash[dashCount];

            var startSpace = (1 - ((DashLength + DashSpace) * dashCount - DashSpace) / Length) / 2;
            var dashT = DashLength / Length;
            var spaceT = DashSpace / Length;
            for (var i = 0; i < dashCount; i += 1)
            {
                var startT = startSpace + (dashT + spaceT) * i;
                var endT = startT + dashT;

                var startPos = trajectory.Position(startT);
                var endPos = trajectory.Position(endT);
                var pos = (startPos + endPos) / 2;
                var dir = trajectory.Tangent((startT + endT) / 2);
                var angle = Mathf.Atan2(dir.z, dir.x);

                var dash = new MarkupDash(pos, angle, DashLength, DashWidth, DashColor);
                dashes[i] = dash;
            }

            return dashes;
        }
        private MarkupDash[] CalculateSolid(Bezier3 trajectory)
        {
            var deltaAngle = Vector3.Angle(trajectory.b - trajectory.a, trajectory.c - trajectory.d);
            var dir = trajectory.d - trajectory.a;
            var length = dir.magnitude;

            if ((180 - deltaAngle > MinAngleDelta || length > MaxLength) && length >= MinLength)
            {
                trajectory.Divide(out Bezier3 first, out Bezier3 second);
                var firstPart = CalculateSolid(first);
                var secondPart = CalculateSolid(second);
                var dashes = firstPart.Concat(secondPart).ToArray();
                return dashes;
            }
            else
            {
                var pos = (trajectory.d + trajectory.a) / 2;
                var angle = Mathf.Atan2(dir.z, dir.x);
                var dash = new MarkupDash(pos, angle, length, DashWidth, DashColor);
                return new MarkupDash[] { dash };
            }
        }
        public override string ToString() => PointPair.ToString();

        public bool ContainPoint(MarkupPoint point) => PointPair.ContainPoint(point);

        public enum Type
        {
            Solid,
            Dash,
            DoubleSolid,
            DoubleDash
        }
    }
    public class MarkupDash
    {
        public Vector3 Position { get; }
        public float Angle { get; }
        public float Length { get; }
        public float Width { get; }
        public Color Color { get; }

        public MarkupDash(Vector3 position, float angle, float length, float width, Color color)
        {
            Position = position;
            Angle = angle;
            Length = length;
            Width = width;
            Color = color;
        }
    }
}
