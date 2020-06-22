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
        Dictionary<ushort, SegmentEnter> EntersDic { get; } = new Dictionary<ushort, SegmentEnter>();
        Dictionary<MarkupPointPair, MarkupLine> LinesDic { get; } = new Dictionary<MarkupPointPair, MarkupLine>(new MarkupPointPairComparer());

        public IEnumerable<MarkupLine> Lines
        {
            get
            {
                foreach (var line in LinesDic.Values)
                    yield return line;
            }
        }
        public IEnumerable<SegmentEnter> Enters
        {
            get
            {
                foreach (var enter in EntersDic.Values)
                    yield return enter;
            }
        }

        public Markup(ushort nodeId)
        {
            NodeId = nodeId;

            var node = Utilities.GetNode(NodeId);
            foreach (var segmentId in node.SegmentsId())
            {
                var enter = new SegmentEnter(NodeId, segmentId);
                EntersDic[segmentId] = enter;
            }
        }

        public void AddConnect(MarkupPointPair pointPair)
        {
            var line = new MarkupLine(pointPair);
            LinesDic[pointPair] = line;
        }
        public bool ExistConnection(MarkupPointPair pointPair) => LinesDic.ContainsKey(pointPair);
        public void RemoveConnect(MarkupPointPair pointPair)
        {
            LinesDic.Remove(pointPair);
        }
        public void ToggleConnection(MarkupPointPair pointPair)
        {
            if (!ExistConnection(pointPair))
                AddConnect(pointPair);
            else
                RemoveConnect(pointPair);
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
    }
    public class MarkupPointPairComparer : IEqualityComparer<MarkupPointPair>
    {
        public bool Equals(MarkupPointPair x, MarkupPointPair y) => (x.First == y.First && x.Second == y.Second) || (x.First == y.Second && x.Second == y.First);

        public int GetHashCode(MarkupPointPair pair) => pair.GetHashCode();
    }

    public class SegmentEnter : IEnumerable<MarkupPoint>
    {
        public ushort SegmentId { get; }
        public NetSegment Segment { get; }
        public bool IsStartSide { get; }
        public bool IsLaneInvert => IsStartSide ^ Segment.IsInvert();
        List<MarkupPoint> Points { get; } = new List<MarkupPoint>();
        public Vector3 CornerDir { get; private set; }

        public int PointCount => Points.Count;
        public MarkupPoint this[int index] => Points[index];


        public SegmentEnter(ushort nodeId, ushort segmentId)
        {
            SegmentId = segmentId;
            Segment = Utilities.GetSegment(SegmentId);
            IsStartSide = Segment.m_startNode == nodeId;

            Update();

            CreatePoints();
        }
        private void CreatePoints()
        {
            var info = Segment.Info;
            var lanes = Segment.GetLanesId().ToArray();
            var driveLanesIdxs = info.m_sortedLanes.Where(s => Utilities.IsDriveLane(info.m_lanes[s]));
            if (!IsLaneInvert)
                driveLanesIdxs = driveLanesIdxs.Reverse();

            var driveLanes = driveLanesIdxs.Select(d => new SegmentLane(lanes[d], info.m_lanes[d])).ToArray();

            var markupLines = new SegmentMarkupLine[driveLanes.Length + 1];

            for (int i = 0; i < markupLines.Length; i += 1)
            {
                var left = i - 1 >= 0 ? driveLanes[i - 1] : null;
                var right = i < driveLanes.Length ? driveLanes[i] : null;
                var markupLine = new SegmentMarkupLine(this, left, right);
                markupLines[i] = markupLine;
            }

            foreach (var markupLine in markupLines)
            {
                var points = markupLine.GetMarkupPoints();
                Points.AddRange(points);
            }
        }

        public void Update()
        {
            var cornerAngle = IsStartSide ? Segment.m_cornerAngleStart : Segment.m_cornerAngleEnd;
            CornerDir = Vector3.right.TurnDeg(cornerAngle / 255f * 360f, false).normalized * (IsLaneInvert ? 1 : -1);
        }

        public IEnumerator<MarkupPoint> GetEnumerator() => Points.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public class SegmentLane
    {
        public uint LaneId { get; }
        public NetInfo.Lane Info { get; }
        public NetLane NetLane => Utilities.GetLane(LaneId);
        public float Position => Info.m_position;
        public float HalfWidth => Info.m_width / 2;
        public float LeftSidePos => Position - HalfWidth;
        public float RightSidePos => Position + HalfWidth;

        public SegmentLane(uint laneId, NetInfo.Lane info)
        {
            LaneId = laneId;
            Info = info;
        }
    }
    public class SegmentMarkupLine
    {
        public SegmentEnter SegmentEnter { get; }

        SegmentLane LeftLane { get; }
        SegmentLane RightLane { get; }
        float Point => SegmentEnter.IsStartSide ? 0f : 1f;

        public bool IsRightEdge => LeftLane == null;
        public bool IsLeftEdge => RightLane == null;
        public bool IsEdge => IsRightEdge ^ IsLeftEdge;
        public bool NeedSplit => !IsEdge && SideDelta >= (RightLane.HalfWidth + LeftLane.HalfWidth) / 2;

        public float CenterDelte => IsEdge ? 0f : RightLane.Position - LeftLane.Position;
        public float SideDelta => IsEdge ? 0f : RightLane.LeftSidePos - LeftLane.RightSidePos;
        public float HalfSideDelta => SideDelta / 2;

        public SegmentMarkupLine(SegmentEnter segmentEnter, SegmentLane leftLane, SegmentLane rightLane)
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
                return new MarkupPoint[] { pointLeft, pointRight };
            }
            else
            {
                var point = new MarkupPoint(this, MarkupPoint.Type.Between);
                return new MarkupPoint[] { point };
            }
        }

        public void GetPositionAndDirection(MarkupPoint.Type pointType, out Vector3 position, out Vector3 direction)
        {
            if ((pointType & MarkupPoint.Type.Between) != MarkupPoint.Type.None)
                GetMiddlePosition(out position, out direction);

            else if ((pointType & MarkupPoint.Type.Edge) != MarkupPoint.Type.None)
                GetEdgePosition(pointType, out position, out direction);

            else
                throw new Exception();
        }
        void GetMiddlePosition(out Vector3 position, out Vector3 direction)
        {
            RightLane.NetLane.CalculatePositionAndDirection(Point, out Vector3 rightPos, out Vector3 rightDir);
            LeftLane.NetLane.CalculatePositionAndDirection(Point, out Vector3 leftPos, out Vector3 leftDir);

            var part = (RightLane.HalfWidth + HalfSideDelta) / CenterDelte;
            position = Vector3.Lerp(rightPos, leftPos, part);
            direction = (rightDir + leftDir) / (SegmentEnter.IsStartSide ? -2 : 2);
            direction.Normalize();
        }
        void GetEdgePosition(MarkupPoint.Type pointType, out Vector3 position, out Vector3 direction)
        {
            float lineShift;
            switch (pointType)
            {
                case MarkupPoint.Type.LeftEdge:
                    LeftLane.NetLane.CalculatePositionAndDirection(Point, out position, out direction);
                    lineShift = -LeftLane.HalfWidth;
                    break;
                case MarkupPoint.Type.RightEdge:
                    RightLane.NetLane.CalculatePositionAndDirection(Point, out position, out direction);
                    lineShift = RightLane.HalfWidth;
                    break;
                default:
                    throw new Exception();
            }
            direction = SegmentEnter.IsStartSide ? -direction : direction;

            var angle = Vector3.Angle(direction, SegmentEnter.CornerDir);
            angle = (angle > 90 ? 180 - angle : angle);
            lineShift /= Mathf.Sin(angle * Mathf.Deg2Rad);

            direction.Normalize();
            position += SegmentEnter.CornerDir * lineShift;
        }
    }
    public class MarkupPoint
    {
        public static Vector3 MarkerSize { get; } = Vector3.one * 1f;
        public Vector3 Position { get; private set; }
        public Vector3 Direction { get; private set; }
        public Type PointType { get; private set; }
        public Bounds Bounds { get; private set; }

        SegmentMarkupLine MarkupLine { get; }
        public SegmentEnter Enter => MarkupLine.SegmentEnter;

        public MarkupPoint(SegmentMarkupLine markupLine, Type pointType)
        {
            MarkupLine = markupLine;
            PointType = pointType;

            Update();
        }

        public void Update()
        {
            MarkupLine.GetPositionAndDirection(PointType, out Vector3 position, out Vector3 direction);
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
    }
    public class MarkupLine
    {
        float _startOffset = 0;
        float _endOffset = 0;

        public MarkupPointPair PointPair { get; }

        public Bezier3 Trajectory { get; private set; }
        public float StartOffset
        {
            get => _startOffset;
            set
            {
                _startOffset = value;
                Update();
            }
        }
        public float EndOffset
        {
            get => _endOffset;
            set
            {
                _endOffset = value;
                Update();
            }
        }

        public MarkupLine(MarkupPointPair pointPair)
        {
            PointPair = pointPair;

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
        }

        public enum Type
        {
            Solid,
            Dash,
            DoubleSolid,
            DoubleDash
        }
    }
}
