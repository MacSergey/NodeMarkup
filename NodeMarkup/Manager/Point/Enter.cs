using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public class Enter
    {
        byte _pointNum;
        public static string XmlName { get; } = "E";

        public Markup Markup { get; private set; }
        public ushort Id { get; }
        public bool IsStartSide { get; private set; }
        public bool IsLaneInvert { get; private set; }
        public float RoadHalfWidth { get; private set; }
        public Vector3? Position { get; private set; } = null;
        public Vector3 LeftSide { get; set; }
        public Vector3 RightSide { get; set; }

        DriveLane[] DriveLanes { get; set; } = new DriveLane[0];
        SegmentMarkupLine[] Lines { get; set; } = new SegmentMarkupLine[0];
        Dictionary<byte, MarkupEnterPoint> EnterPointsDic { get; } = new Dictionary<byte, MarkupEnterPoint>();
        Dictionary<byte, MarkupCrosswalkPoint> CrosswalkPointsDic { get; } = new Dictionary<byte, MarkupCrosswalkPoint>();
        Dictionary<byte, MarkupNormalPoint> NormalPointsDic { get; } = new Dictionary<byte, MarkupNormalPoint>();

        public byte PointNum => ++_pointNum;

        public float AbsoluteAngle { get; private set; }
        public float CornerAndNormalAngle { get; private set; }
        public Vector3 CornerDir { get; private set; }
        public Vector3 NormalDir { get; private set; }

        public Enter Next => Markup.GetNextEnter(this);
        public Enter Prev => Markup.GetPrevEnter(this);
        public MarkupPoint FirstPoint => EnterPointsDic[1];
        public MarkupPoint LastPoint => EnterPointsDic[(byte)PointCount];

        public int PointCount => EnterPointsDic.Count;
        public int CrosswalkCount => CrosswalkPointsDic.Count;
        public int NormalCount => NormalPointsDic.Count;

        public IEnumerable<MarkupEnterPoint> Points => EnterPointsDic.Values;
        public IEnumerable<MarkupCrosswalkPoint> Crosswalks => CrosswalkPointsDic.Values;
        public IEnumerable<MarkupNormalPoint> Normals => NormalPointsDic.Values;

        public float T => IsStartSide ? 0f : 1f;
        public string XmlSection => XmlName;


        public Enter(Markup markup, ushort segmentId)
        {
            Markup = markup;
            Id = segmentId;

            Init();
            Update();
            UpdatePoints();

            var points = Lines.SelectMany(l => l.GetMarkupPoints()).ToArray();
            EnterPointsDic = points.ToDictionary(p => p.Num, p => p);
            CrosswalkPointsDic = points.ToDictionary(p => p.Num, p => new MarkupCrosswalkPoint(p));
            NormalPointsDic = points.ToDictionary(p => p.Num, p => new MarkupNormalPoint(p));
        }
        private void Init()
        {
            var segment = Utilities.GetSegment(Id);
            IsStartSide = segment.m_startNode == Markup.Id;
            IsLaneInvert = IsStartSide ^ segment.IsInvert();

            var info = segment.Info;
            var lanes = segment.GetLanesId().ToArray();
            var driveLanesIdxs = info.m_sortedLanes.Where(s => Utilities.IsDriveLane(info.m_lanes[s]));
            if (!IsLaneInvert)
                driveLanesIdxs = driveLanesIdxs.Reverse();

            DriveLanes = driveLanesIdxs.Select(d => new DriveLane(this, lanes[d], info.m_lanes[d])).ToArray();
            if (!DriveLanes.Any())
                return;

            Lines = new SegmentMarkupLine[DriveLanes.Length + 1];

            for (int i = 0; i < Lines.Length; i += 1)
            {
                var left = i - 1 >= 0 ? DriveLanes[i - 1] : null;
                var right = i < DriveLanes.Length ? DriveLanes[i] : null;
                var markupLine = new SegmentMarkupLine(this, left, right);
                Lines[i] = markupLine;
            }
        }
        public bool TryGetPoint(byte pointNum, MarkupPoint.PointType type, out MarkupPoint point)
        {
            switch (type)
            {
                case MarkupPoint.PointType.Enter:
                    if (EnterPointsDic.TryGetValue(pointNum, out MarkupEnterPoint enterPoint))
                    {
                        point = enterPoint;
                        return true;
                    }
                    break;
                case MarkupPoint.PointType.Crosswalk:
                    if (CrosswalkPointsDic.TryGetValue(pointNum, out MarkupCrosswalkPoint crosswalkPoint))
                    {
                        point = crosswalkPoint;
                        return true;
                    }
                    break;
                case MarkupPoint.PointType.Normal:
                    if (NormalPointsDic.TryGetValue(pointNum, out MarkupNormalPoint normalPoint))
                    {
                        point = normalPoint;
                        return true;
                    }
                    break;
            }
            point = null;
            return false;
        }

        public void Update()
        {
            var segment = Utilities.GetSegment(Id);

            CalculateCorner(segment);
            CalculatePosition(segment);
        }
        public void UpdatePoints()
        {
            foreach (var point in EnterPointsDic.Values)
                point.Update();
            foreach (var point in CrosswalkPointsDic.Values)
                point.Update();
            foreach (var point in NormalPointsDic.Values)
                point.Update();
        }
        private void CalculateCorner(NetSegment segment)
        {
            var cornerAngle = (IsStartSide ? segment.m_cornerAngleStart : segment.m_cornerAngleEnd) / 255f * 360f;
            if (IsLaneInvert)
                cornerAngle = cornerAngle >= 180 ? cornerAngle - 180 : cornerAngle + 180;
            AbsoluteAngle = cornerAngle * Mathf.Deg2Rad;

            CornerDir = DriveLanes.Length <= 1 ?
                Vector3.right.TurnRad(AbsoluteAngle, false).normalized :
                (DriveLanes.Last().NetLane.CalculatePosition(T) - DriveLanes.First().NetLane.CalculatePosition(T)).normalized;
            NormalDir = DriveLanes.Any() ? DriveLanes.Aggregate(Vector3.zero, (v, l) => v + l.NetLane.CalculateDirection(T)).normalized : Vector3.zero;
            NormalDir = IsStartSide ? -NormalDir : NormalDir;
            var angle = Vector3.Angle(NormalDir, CornerDir);
            CornerAndNormalAngle = (angle > 90 ? 180 - angle : angle) * Mathf.Deg2Rad;
        }
        private void CalculatePosition(NetSegment segment)
        {
            if (DriveLanes.FirstOrDefault() is DriveLane driveLane)
            {
                var position = driveLane.NetLane.CalculatePosition(T);
                var coef = Mathf.Sin(CornerAndNormalAngle);

                RoadHalfWidth = (segment.Info.m_halfWidth - segment.Info.m_pavementWidth) / coef;

                Position = position + (IsLaneInvert ? -CornerDir : CornerDir) * driveLane.Position / coef;
                RightSide = Position.Value - RoadHalfWidth * CornerDir;
                LeftSide = Position.Value + RoadHalfWidth * CornerDir;
            }
            else
                Position = null;
        }

        public override string ToString() => Id.ToString();
    }
    public class DriveLane
    {
        private Enter Enter { get; }

        public uint LaneId { get; }
        public NetInfo.Lane Info { get; }
        public NetLane NetLane => Utilities.GetLane(LaneId);
        public float Position => Info.m_position;
        public float HalfWidth => Mathf.Abs(Info.m_width) / 2;
        public float LeftSidePos => Position + (Enter.IsLaneInvert ? -HalfWidth : HalfWidth);
        public float RightSidePos => Position + (Enter.IsLaneInvert ? HalfWidth : -HalfWidth);

        public DriveLane(Enter enter, uint laneId, NetInfo.Lane info)
        {
            Enter = enter;
            LaneId = laneId;
            Info = info;
        }
    }
    public class SegmentMarkupLine
    {
        public Enter Enter { get; }

        DriveLane LeftLane { get; }
        DriveLane RightLane { get; }

        public bool IsRightEdge => RightLane == null;
        public bool IsLeftEdge => LeftLane == null;
        public bool IsEdge => IsRightEdge ^ IsLeftEdge;
        public bool NeedSplit => !IsEdge && SideDelta >= (RightLane.HalfWidth + LeftLane.HalfWidth) / 2;

        public float CenterDelte => IsEdge ? 0f : Mathf.Abs(RightLane.Position - LeftLane.Position);
        public float SideDelta => IsEdge ? 0f : Mathf.Abs(RightLane.LeftSidePos - LeftLane.RightSidePos);
        public float HalfSideDelta => SideDelta / 2;

        public SegmentMarkupLine(Enter enter, DriveLane leftLane, DriveLane rightLane)
        {
            Enter = enter;
            LeftLane = leftLane;
            RightLane = rightLane;
        }

        public IEnumerable<MarkupEnterPoint> GetMarkupPoints()
        {
            if (IsEdge)
            {
                yield return new MarkupEnterPoint(this, IsRightEdge ? MarkupPoint.LocationType.RightEdge : MarkupPoint.LocationType.LeftEdge);
            }
            else if (NeedSplit)
            {
                yield return new MarkupEnterPoint(this, MarkupPoint.LocationType.RightEdge);
                yield return new MarkupEnterPoint(this, MarkupPoint.LocationType.LeftEdge);
            }
            else
            {
                yield return new MarkupEnterPoint(this, MarkupPoint.LocationType.Between);
            }
        }

        public void GetPositionAndDirection(MarkupPoint.LocationType location, float offset, out Vector3 position, out Vector3 direction)
        {
            if ((location & MarkupPoint.LocationType.Between) != MarkupPoint.LocationType.None)
                GetMiddlePosition(offset, out position, out direction);

            else if ((location & MarkupPoint.LocationType.Edge) != MarkupPoint.LocationType.None)
                GetEdgePosition(location, offset, out position, out direction);

            else
                throw new Exception();
        }
        void GetMiddlePosition(float offset, out Vector3 position, out Vector3 direction)
        {
            RightLane.NetLane.CalculatePositionAndDirection(Enter.T, out Vector3 rightPos, out Vector3 rightDir);
            LeftLane.NetLane.CalculatePositionAndDirection(Enter.T, out Vector3 leftPos, out Vector3 leftDir);

            direction = ((rightDir + leftDir) / (Enter.IsStartSide ? -2 : 2)).normalized;

            var part = (RightLane.HalfWidth + HalfSideDelta) / CenterDelte;
            position = Vector3.Lerp(rightPos, leftPos, part) + Enter.CornerDir * (offset / Mathf.Sin(Enter.CornerAndNormalAngle));
        }
        void GetEdgePosition(MarkupPoint.LocationType location, float offset, out Vector3 position, out Vector3 direction)
        {
            float lineShift;
            switch (location)
            {
                case MarkupPoint.LocationType.LeftEdge:
                    RightLane.NetLane.CalculatePositionAndDirection(Enter.T, out position, out direction);
                    lineShift = -RightLane.HalfWidth;
                    break;
                case MarkupPoint.LocationType.RightEdge:
                    LeftLane.NetLane.CalculatePositionAndDirection(Enter.T, out position, out direction);
                    lineShift = LeftLane.HalfWidth;
                    break;
                default:
                    throw new Exception();
            }
            direction = (Enter.IsStartSide ? -direction : direction).normalized;

            var shift = (lineShift + offset) / Mathf.Sin(Enter.CornerAndNormalAngle);

            position += Enter.CornerDir * shift;
        }
    }
}
