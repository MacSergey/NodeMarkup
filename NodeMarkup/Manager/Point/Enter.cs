using ColossalFramework.Math;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
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

        DriveLane[] DriveLanes { get; set; } = new DriveLane[0];
        SegmentMarkupLine[] Lines { get; set; } = new SegmentMarkupLine[0];
        List<MarkupPoint> PointsList { get; set; } = new List<MarkupPoint>();

        public byte PointNum => ++_pointNum;

        public float CornerAngle { get; private set; }
        public float CornerDeltaAngle { get; private set; }
        public Vector3 CornerDir { get; private set; }

        public Enter Next => Markup.GetNextEnter(this);
        public Enter Prev => Markup.GetPrevEnter(this);
        public MarkupPoint FirstPoint => PointsList.FirstOrDefault();
        public MarkupPoint LastPoint => PointsList.LastOrDefault();

        public int PointCount => PointsList.Count;
        public IEnumerable<MarkupPoint> Points => PointsList;
        public float T => IsStartSide ? 0f : 1f;
        public bool TryGetPoint(byte pointNum, out MarkupPoint point)
        {
            if (1 <= pointNum && pointNum <= PointCount)
            {
                point = PointsList[pointNum - 1];
                return true;
            }
            else
            {
                point = null;
                return false;
            }
        }


        public string XmlSection => XmlName;


        public Enter(Markup markup, ushort segmentId)
        {
            Markup = markup;
            Id = segmentId;

            Init();
            Update();

            foreach (var markupLine in Lines)
                PointsList.AddRange(markupLine.GetMarkupPoints());
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

        public void Update()
        {
            var segment = Utilities.GetSegment(Id);

            CalculateCorner(segment);
            CalculatePosition(segment);

            foreach (var point in PointsList)
            {
                point.Update();
            }
        }
        private void CalculateCorner(NetSegment segment)
        {
            var cornerAngle = (IsStartSide ? segment.m_cornerAngleStart : segment.m_cornerAngleEnd) / 255f * 360f;
            if (IsLaneInvert)
                cornerAngle = cornerAngle >= 180 ? cornerAngle - 180 : cornerAngle + 180;
            CornerAngle = cornerAngle * Mathf.Deg2Rad;
            CornerDir = Vector3.right.TurnRad(CornerAngle, false).normalized;
            CornerDeltaAngle = DriveLanes.Average(d => Vector3.Angle(d.NetLane.CalculateDirection(T), CornerDir) * Mathf.Deg2Rad);
        }
        private void CalculatePosition(NetSegment segment)
        {
            var lane = DriveLanes.Aggregate((i, j) => Mathf.Abs(i.Position) <= Mathf.Abs(j.Position) ? i : j);

            if (DriveLanes.FirstOrDefault() is DriveLane driveLane)
            {
                var position = driveLane.NetLane.CalculatePosition(T);
                var coef = Mathf.Sin(CornerDeltaAngle);

                Position = position + (IsLaneInvert ? -CornerDir : CornerDir) * driveLane.Position / coef;
                RoadHalfWidth = segment.Info.m_halfWidth / coef;
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

        public IEnumerable<MarkupPoint> GetMarkupPoints()
        {
            if (IsEdge)
            {
                yield return new MarkupPoint(this, IsRightEdge ? MarkupPoint.Type.RightEdge : MarkupPoint.Type.LeftEdge);
            }
            else if (NeedSplit)
            {
                yield return new MarkupPoint(this, MarkupPoint.Type.RightEdge);
                yield return new MarkupPoint(this, MarkupPoint.Type.LeftEdge);
            }
            else
            {
                yield return new MarkupPoint(this, MarkupPoint.Type.Between);
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
            RightLane.NetLane.CalculatePositionAndDirection(Enter.T, out Vector3 rightPos, out Vector3 rightDir);
            LeftLane.NetLane.CalculatePositionAndDirection(Enter.T, out Vector3 leftPos, out Vector3 leftDir);

            direction = ((rightDir + leftDir) / (Enter.IsStartSide ? -2 : 2)).normalized;

            var part = (RightLane.HalfWidth + HalfSideDelta) / CenterDelte;
            position = Vector3.Lerp(rightPos, leftPos, part) + Enter.CornerDir * (offset / Mathf.Sin(Enter.CornerDeltaAngle));
        }
        void GetEdgePosition(MarkupPoint.Type pointType, float offset, out Vector3 position, out Vector3 direction)
        {
            float lineShift;
            switch (pointType)
            {
                case MarkupPoint.Type.LeftEdge:
                    RightLane.NetLane.CalculatePositionAndDirection(Enter.T, out position, out direction);
                    lineShift = -RightLane.HalfWidth;
                    break;
                case MarkupPoint.Type.RightEdge:
                    LeftLane.NetLane.CalculatePositionAndDirection(Enter.T, out position, out direction);
                    lineShift = LeftLane.HalfWidth;
                    break;
                default:
                    throw new Exception();
            }
            direction = (Enter.IsStartSide ? -direction : direction).normalized;

            var shift = (lineShift + offset) / Mathf.Sin(Enter.CornerDeltaAngle);

            position += Enter.CornerDir * shift;
        }
    }
}
