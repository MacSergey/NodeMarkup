using ColossalFramework.Math;
using ModsCommon.Utilities;
using NodeMarkup.Tools;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{

    public abstract class Enter : IRender, IComparable<Enter>
    {
        byte _pointNum;
        public static string XmlName { get; } = "E";

        public virtual MarkupPoint.PointType SupportPoints => MarkupPoint.PointType.Enter;
        public Markup Markup { get; private set; }
        public ushort Id { get; }
        public bool IsStartSide { get; private set; }
        public abstract int SideSign { get; }
        public bool IsLaneInvert { get; private set; }
        public float RoadHalfWidth { get; private set; }
        public float RoadHalfWidthTransform { get; private set; }
        public Vector3? Position { get; private set; } = null;
        public Vector3 FirstPointSide { get; private set; }
        public Vector3 LastPointSide { get; private set; }
        public StraightTrajectory Line { get; private set; }
        public bool LanesChanged
        {
            get
            {
                var segment = GetSegment();
                var info = segment.Info;
                for (var i = 0; i < info.m_sortedLanes.Length; i += 1)
                {
                    var index = info.m_sortedLanes[i];
                    if (info.m_lanes[index].IsDriveLane())
                    {
                        var laneId = segment.GetLanesId().Skip(index).FirstOrDefault();
                        return laneId != DriveLanes[IsLaneInvert ? 0 : DriveLanes.Length - 1].LaneId;
                    }
                }

                return DriveLanes.Any();
            }
        }

        DriveLane[] DriveLanes { get; set; } = new DriveLane[0];
        SegmentMarkupLine[] Lines { get; set; } = new SegmentMarkupLine[0];
        protected Dictionary<byte, MarkupEnterPoint> EnterPointsDic { get; private set; } = new Dictionary<byte, MarkupEnterPoint>();

        public byte PointNum => ++_pointNum;

        public Vector3 CornerDir { get; private set; }
        public Vector3 NormalDir { get; private set; }

        public float CornerAngle { get; private set; }
        public float NormalAngle { get; private set; }
        public float CornerAndNormalAngle { get; private set; }

        public Enter Next => Markup.GetNextEnter(this);
        public Enter Prev => Markup.GetPrevEnter(this);
        public MarkupPoint FirstPoint => EnterPointsDic[1];
        public MarkupPoint LastPoint => EnterPointsDic[(byte)PointCount];

        public int PointCount => EnterPointsDic.Count;
        public IEnumerable<MarkupEnterPoint> Points => EnterPointsDic.Values;

        public float T => IsStartSide ? 0f : 1f;
        public string XmlSection => XmlName;

        public EnterData Data => new EnterData(this);


        public Enter(Markup markup, ushort segmentId)
        {
            Markup = markup;
            Id = segmentId;

            Init();
            Update();
        }
        protected virtual void Init()
        {
            _pointNum = 0;

            var segment = GetSegment();
            IsStartSide = GetIsStartSide();
            IsLaneInvert = IsStartSide ^ segment.IsInvert();

            var info = segment.Info;
            var lanes = segment.GetLanesId().ToArray();
            var driveLanesIdxs = info.m_sortedLanes.Where(s => info.m_lanes[s].IsDriveLane());
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

            var points = Lines.SelectMany(l => l.GetMarkupPoints()).ToArray();
            EnterPointsDic = points.ToDictionary(p => p.Num, p => p);
        }
        protected abstract NetSegment GetSegment();
        protected abstract bool GetIsStartSide();
        public virtual bool TryGetPoint(byte pointNum, MarkupPoint.PointType type, out MarkupPoint point)
        {
            if (type == MarkupPoint.PointType.Enter && EnterPointsDic.TryGetValue(pointNum, out MarkupEnterPoint enterPoint))
            {
                point = enterPoint;
                return true;
            }
            else
            {
                point = null;
                return false;
            }
        }

        public void Update()
        {
            var segment = GetSegment();

            CalculateCorner(segment);
            CalculatePosition(segment);
        }
        public virtual void UpdatePoints()
        {
            foreach (var point in EnterPointsDic.Values)
                point.Update();
        }
        private void CalculateCorner(NetSegment segment)
        {
            var cornerAngle = (IsStartSide ? segment.m_cornerAngleStart : segment.m_cornerAngleEnd) / 255f * 360f;
            if (IsLaneInvert)
                cornerAngle = cornerAngle >= 180 ? cornerAngle - 180 : cornerAngle + 180;
            CornerAngle = cornerAngle * Mathf.Deg2Rad;
            CornerDir = DriveLanes.Length <= 1 ? CornerAngle.Direction() : (DriveLanes.Last().NetLane.CalculatePosition(T) - DriveLanes.First().NetLane.CalculatePosition(T)).normalized;
            NormalDir = (DriveLanes.Any() ? DriveLanes.Aggregate(Vector3.zero, (v, l) => v + l.NetLane.CalculateDirection(T)).normalized : Vector3.zero) * SideSign;
            NormalAngle = NormalDir.AbsoluteAngle();

            var angle = Vector3.Angle(NormalDir, CornerDir);
            CornerAndNormalAngle = (angle > 90 ? 180 - angle : angle) * Mathf.Deg2Rad;
        }
        private void CalculatePosition(NetSegment segment)
        {
            if (DriveLanes.FirstOrDefault() is DriveLane driveLane)
            {
                var position = driveLane.NetLane.CalculatePosition(T);
                var coef = Mathf.Sin(CornerAndNormalAngle);

                RoadHalfWidth = segment.Info.m_halfWidth - segment.Info.m_pavementWidth;
                RoadHalfWidthTransform = RoadHalfWidth / coef;

                Position = position + (IsLaneInvert ? -CornerDir : CornerDir) * driveLane.Position / coef;
                FirstPointSide = Position.Value - RoadHalfWidthTransform * CornerDir;
                LastPointSide = Position.Value + RoadHalfWidthTransform * CornerDir;
                Line = new StraightTrajectory(FirstPointSide, LastPointSide);
            }
            else
                Position = null;
        }

        public void ResetOffsets()
        {
            foreach (var point in Points)
                point.Offset = 0;
        }
        public void Render(RenderManager.CameraInfo cameraInfo, Color? color = null, float? width = null, bool? alphaBlend = null, bool? cut = null)
        {
            if (Position == null)
                return;

            var bezier = new Line3(Position.Value - CornerDir * RoadHalfWidthTransform, Position.Value + CornerDir * RoadHalfWidthTransform).GetBezier();
            NodeMarkupTool.RenderBezier(cameraInfo, bezier, color, width, alphaBlend, cut);
        }
        public int CompareTo(Enter other) => other.NormalAngle.CompareTo(NormalAngle);
        public override string ToString() => Id.ToString();
    }
    public abstract class Enter<MarkupType> : Enter
        where MarkupType : Markup
    {
        public new MarkupType Markup => (MarkupType)base.Markup;

        public Enter(MarkupType markup, ushort segmentId) : base(markup, segmentId) { }
    }
    public class DriveLane
    {
        private Enter Enter { get; }

        public uint LaneId { get; }
        public NetInfo.Lane Info { get; }
        public NetLane NetLane => LaneId.GetLane();
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

        public override string ToString() => LaneId.ToString();
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
                GetMiddlePositionAndDirection(offset, out position, out direction);

            else if ((location & MarkupPoint.LocationType.Edge) != MarkupPoint.LocationType.None)
                GetEdgePositionAndDirection(location, offset, out position, out direction);

            else
                throw new Exception();
        }
        void GetMiddlePositionAndDirection(float offset, out Vector3 position, out Vector3 direction)
        {
            RightLane.NetLane.CalculatePositionAndDirection(Enter.T, out Vector3 rightPos, out Vector3 rightDir);
            LeftLane.NetLane.CalculatePositionAndDirection(Enter.T, out Vector3 leftPos, out Vector3 leftDir);

            direction = ((rightDir + leftDir) / (Enter.SideSign * 2)).normalized;

            var part = (RightLane.HalfWidth + HalfSideDelta) / CenterDelte;
            position = Vector3.Lerp(rightPos, leftPos, part) + Enter.CornerDir * (offset / Mathf.Sin(Enter.CornerAndNormalAngle));
        }
        void GetEdgePositionAndDirection(MarkupPoint.LocationType location, float offset, out Vector3 position, out Vector3 direction)
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
            direction = (direction * Enter.SideSign).normalized;

            var shift = (lineShift + offset) / Mathf.Sin(Enter.CornerAndNormalAngle);

            position += Enter.CornerDir * shift;
        }
    }
    public class EnterData : IToXml
    {
        public ushort Id { get; private set; }
        public int Points { get; private set; }
        public float NormalAngle { get; private set; }
        public float CornerAngle { get; private set; }

        public string XmlSection => Enter.XmlName;

        private EnterData() { }
        public EnterData(Enter enter)
        {
            Id = enter.Id;
            Points = enter.PointCount;
            NormalAngle = enter.NormalAngle;
            CornerAngle = enter.CornerAngle;
        }
        public static EnterData FromXml(XElement config)
        {
            var data = new EnterData
            {
                Id = config.GetAttrValue<ushort>(nameof(Id)),
                Points = config.GetAttrValue<int>("P"),
                NormalAngle = config.GetAttrValue<float>("A")
            };
            return data;
        }

        public XElement ToXml()
        {
            var config = new XElement(XmlSection);
            config.Add(new XAttribute(nameof(Id), Id));
            config.Add(new XAttribute("P", Points));
            config.Add(new XAttribute("A", NormalAngle));
            return config;
        }
    }
}
