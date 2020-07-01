using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public class Enter : IToXml, IFromXml
    {
        byte _pointNum;
        public static string XmlName { get; } = "E";

        public Markup Markup { get; private set; }
        public ushort Id { get; }
        public bool IsStartSide { get; }
        public bool IsLaneInvert { get; }

        DriveLane[] DriveLanes { get; set; } = new DriveLane[0];
        SegmentMarkupLine[] Lines { get; set; } = new SegmentMarkupLine[0];
        Dictionary<byte, MarkupPoint> PointsDictionary { get; set; } = new Dictionary<byte, MarkupPoint>();

        public byte PointNum => ++_pointNum;

        public Vector3 cornerDir;

        public int PointCount => PointsDictionary.Count;
        public IEnumerable<MarkupPoint> Points
        {
            get
            {
                foreach (var line in PointsDictionary.Values)
                    yield return line;
            }
        }
        public bool TryGetPoint(byte pointNum, out MarkupPoint point) => PointsDictionary.TryGetValue(pointNum, out point);


        public string XmlSection => XmlName;


        public Enter(Markup markup, ushort segmentId)
        {
            Markup = markup;
            Id = segmentId;
            var segment = Utilities.GetSegment(Id);
            IsStartSide = segment.m_startNode == markup.Id;
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

            DriveLanes = driveLanesIdxs.Select(d => new DriveLane(this, lanes[d], info.m_lanes[d])).ToArray();

            Lines = new SegmentMarkupLine[DriveLanes.Length + 1];

            for (int i = 0; i < Lines.Length; i += 1)
            {
                var left = i - 1 >= 0 ? DriveLanes[i - 1] : null;
                var right = i < DriveLanes.Length ? DriveLanes[i] : null;
                var markupLine = new SegmentMarkupLine(this, left, right);
                Lines[i] = markupLine;
            }

            foreach (var markupLine in Lines)
            {
                foreach(var point in markupLine.GetMarkupPoints())
                {
                    PointsDictionary.Add(point.Num, point);
                }
            }
        }

        public void Update()
        {
            var segment = Utilities.GetSegment(Id);
            var cornerAngle = IsStartSide ? segment.m_cornerAngleStart : segment.m_cornerAngleEnd;
            cornerDir = Vector3.right.TurnDeg(cornerAngle / 255f * 360f, false).normalized * (IsLaneInvert ? -1 : 1);

            foreach (var point in PointsDictionary.Values)
            {
                point.Update();
            }
        }

        public override string ToString() => Id.ToString();

        public XElement ToXml()
        {
            var confix = new XElement(XmlSection,
                new XAttribute(nameof(Id), Id)
            );
            foreach(var point in PointsDictionary.Values)
            {
                var pointConfig = point.ToXml();
                confix.Add(pointConfig);
            }

            return confix;
        }
        public void FromXml(XElement config)
        {
            foreach (var pointConfig in config.Elements(MarkupPoint.XmlName))
            {
                MarkupPoint.FromXml(pointConfig, this);
            }
        }
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
        public Enter SegmentEnter { get; }

        DriveLane LeftLane { get; }
        DriveLane RightLane { get; }
        float Point => SegmentEnter.IsStartSide ? 0f : 1f;

        public bool IsRightEdge => RightLane == null;
        public bool IsLeftEdge => LeftLane == null;
        public bool IsEdge => IsRightEdge ^ IsLeftEdge;
        public bool NeedSplit => !IsEdge && SideDelta >= (RightLane.HalfWidth + LeftLane.HalfWidth) / 2;

        public float CenterDelte => IsEdge ? 0f : Mathf.Abs(RightLane.Position - LeftLane.Position);
        public float SideDelta => IsEdge ? 0f : Mathf.Abs(RightLane.LeftSidePos - LeftLane.RightSidePos);
        public float HalfSideDelta => SideDelta / 2;

        public SegmentMarkupLine(Enter segmentEnter, DriveLane leftLane, DriveLane rightLane)
        {
            SegmentEnter = segmentEnter;
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
                yield return new MarkupPoint(this, MarkupPoint.Type.LeftEdge);
                yield return new MarkupPoint(this, MarkupPoint.Type.RightEdge);
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
}
