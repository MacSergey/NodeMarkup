using ColossalFramework.Math;
using ModsBridge;
using ModsCommon.Utilities;
using NodeMarkup.Tools;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{

    public abstract class Enter : IOverlay, IDeletable, ISupport, IComparable<Enter>
    {
        private byte _pointNum;
        public static string XmlName { get; } = "E";

        public virtual MarkupPoint.PointType SupportPoints => MarkupPoint.PointType.Enter;
        public Markup Markup { get; private set; }
        public abstract EnterType Type { get; }
        public ushort Id { get; }
        protected abstract bool IsExist { get; }

        private uint FirstLane { get; set; }
        public bool IsStartSide { get; private set; }
        public abstract int SideSign { get; }
        public abstract int NormalSign { get; }
        public bool IsLaneInvert { get; private set; }
        public float RoadHalfWidth { get; private set; }
        public Vector3 Position { get; private set; }
        public Vector3 FirstPointSide { get; private set; }
        public Vector3 LastPointSide { get; private set; }
        public StraightTrajectory Line { get; private set; }
        public bool LanesChanged => GetSegment().m_lanes != FirstLane;

        public IEnumerable<DriveLane> DriveLanes
        {
            get
            {
                var segment = GetSegment();
                var info = segment.Info;
                var lanes = segment.GetLanesId().ToArray();

                foreach (var index in (IsLaneInvert ? info.m_sortedLanes : info.m_sortedLanes.Reverse()).Where(s => info.m_lanes[s].IsDriveLane()))
                    yield return new DriveLane(this, lanes[index], info.m_lanes[index]);
            }
        }
        protected Dictionary<byte, MarkupEnterPoint> EnterPointsDic { get; private set; } = new Dictionary<byte, MarkupEnterPoint>();

        public byte PointNum => ++_pointNum;

        public Vector3 CornerDir { get; private set; }
        public Vector3 NormalDir { get; private set; }

        public float CornerAngle { get; private set; }
        public float NormalAngle { get; private set; }
        public float CornerAndNormalAngle { get; private set; }
        public float TranformCoef { get; private set; }

        public Enter Next => Markup.GetNextEnter(this);
        public Enter Prev => Markup.GetPrevEnter(this);
        public MarkupPoint FirstPoint => EnterPointsDic[1];
        public MarkupPoint LastPoint => EnterPointsDic[(byte)PointCount];

        public int PointCount => EnterPointsDic.Count;
        public IEnumerable<MarkupEnterPoint> Points => EnterPointsDic.Values;

        public float T => IsStartSide ? 0f : 1f;
        public string XmlSection => XmlName;

        public EnterData Data => new EnterData(this);

        public string DeleteCaptionDescription => throw new NotImplementedException();
        public string DeleteMessageDescription => throw new NotImplementedException();

        public Enter(Markup markup, ushort id)
        {
            Markup = markup;
            Id = id;

            if (!IsExist)
                throw new NotExistEnterException(Type, Id);

            Init();
            Update();
        }
        protected virtual void Init()
        {
            _pointNum = 0;

            var segment = GetSegment();
            IsStartSide = GetIsStartSide();
            IsLaneInvert = IsStartSide ^ segment.IsInvert();
            FirstLane = segment.m_lanes;

            var sources = new List<IPointSource>();

            if (segment.Info is IMarkingNetInfo info)
            {
                foreach (var position in IsLaneInvert ? info.MarkupPoints : info.MarkupPoints.Reverse())
                    sources.Add(new RoadGeneratorPointSource(this, IsLaneInvert ? position : -position));
            }
            else
            {
                var driveLanes = DriveLanes.ToArray();
                if (driveLanes.Any())
                {
                    for (var i = 0; i <= driveLanes.Length; i += 1)
                    {
                        var left = i - 1 >= 0 ? driveLanes[i - 1] : null;
                        var right = i < driveLanes.Length ? driveLanes[i] : null;
                        foreach (var source in NetInfoPointSource.GetSource(this, left, right))
                            sources.Add(source);
                    }
                }
            }

            var points = sources.Select(s => new MarkupEnterPoint(this, s)).ToArray();
            EnterPointsDic = points.ToDictionary(p => p.Num, p => p);
        }

        protected abstract ushort GetSegmentId();
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
            var segmentId = GetSegmentId();

            Vector3 leftPos;
            Vector3 rightPos;
            Vector3 leftDir;
            Vector3 rightDir;

            if (IsStartSide)
            {
                segment.CalculateCorner(segmentId, true, true, true, out leftPos, out leftDir, out _);
                segment.CalculateCorner(segmentId, true, true, false, out rightPos, out rightDir, out _);
            }
            else
            {
                segment.CalculateCorner(segmentId, true, false, true, out leftPos, out leftDir, out _);
                segment.CalculateCorner(segmentId, true, false, false, out rightPos, out rightDir, out _);
            }

            CornerDir = (rightPos - leftPos).normalized;
            CornerAngle = CornerDir.AbsoluteAngle();

            NormalDir = NormalSign * (leftDir + rightDir).normalized;
            NormalAngle = NormalDir.AbsoluteAngle();

            var angle = Vector3.Angle(NormalDir, CornerDir);
            CornerAndNormalAngle = (angle > 90 ? 180 - angle : angle) * Mathf.Deg2Rad;
            TranformCoef = Mathf.Sin(CornerAndNormalAngle);

            Position = (leftPos + rightPos) / 2f;

            RoadHalfWidth = segment.Info.m_halfWidth - segment.Info.m_pavementWidth;
            FirstPointSide = GetPosition(-RoadHalfWidth);
            LastPointSide = GetPosition(RoadHalfWidth);
            Line = new StraightTrajectory(FirstPointSide, LastPointSide);
        }
        public virtual void UpdatePoints()
        {
            foreach (var point in EnterPointsDic.Values)
                point.Update();
        }

        public void ResetPoints()
        {
            foreach (var point in Points)
                point.Reset();
        }
        public Vector3 GetPosition(float offset) => Position + offset / TranformCoef * CornerDir;
        public void Render(OverlayData data)
        {
            if (Position == null)
                return;

            var bezier = new Line3(GetPosition(-RoadHalfWidth), GetPosition(RoadHalfWidth)).GetBezier();
            bezier.RenderBezier(data);
#if DEBUG_ENTER
            var normalBezier = new Line3(Position, Position + NormalDir * 10f).GetBezier();
            NodeMarkupTool.RenderBezier(cameraInfo, normalBezier, Colors.Purple);

            var cornerBezier = new Line3(Position, Position + CornerDir * 10f).GetBezier();
            NodeMarkupTool.RenderBezier(cameraInfo, cornerBezier, Colors.Orange);
#endif
        }
        public int CompareTo(Enter other) => other.NormalAngle.CompareTo(NormalAngle);
        public override string ToString() => Id.ToString();

        public Dependences GetDependences() => throw new NotImplementedException();
    }
    public abstract class Enter<MarkupType> : Enter
        where MarkupType : Markup
    {
        public new MarkupType Markup => (MarkupType)base.Markup;

        public Enter(MarkupType markup, ushort id) : base(markup, id) { }
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
    public enum EnterType
    {
        Node,
        Segment
    }
}
