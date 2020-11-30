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
        private uint FirstLane { get; set; }
        public bool IsStartSide { get; private set; }
        public abstract int SideSign { get; }
        public bool IsLaneInvert { get; private set; }
        public float RoadHalfWidth { get; private set; }
        public float RoadHalfWidthTransform { get; private set; }
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
            FirstLane = segment.m_lanes;

            var driveLanes = DriveLanes.ToArray();
            if (!driveLanes.Any())
                return;

            var sources = new List<NetInfoPointSource>();
            for(var i = 0; i <= driveLanes.Length; i += 1)
            {
                var left = i - 1 >= 0 ? driveLanes[i - 1] : null;
                var right = i < driveLanes.Length ? driveLanes[i] : null;
                sources.AddRange(NetInfoPointSource.GetSource(this, left, right));
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

            NormalDir = (leftDir + rightDir).normalized;
            NormalAngle = NormalDir.AbsoluteAngle();

            var angle = Vector3.Angle(NormalDir, CornerDir);
            CornerAndNormalAngle = (angle > 90 ? 180 - angle : angle) * Mathf.Deg2Rad;

            var coef = Mathf.Sin(CornerAndNormalAngle);
            RoadHalfWidth = segment.Info.m_halfWidth - segment.Info.m_pavementWidth;
            RoadHalfWidthTransform = RoadHalfWidth / coef;

            Position = (leftPos + rightPos) / 2f;

            FirstPointSide = Position - RoadHalfWidthTransform * CornerDir;
            LastPointSide = Position + RoadHalfWidthTransform * CornerDir;
            Line = new StraightTrajectory(FirstPointSide, LastPointSide);
        }
        public virtual void UpdatePoints()
        {
            foreach (var point in EnterPointsDic.Values)
                point.Update();
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

            var bezier = new Line3(Position - CornerDir * RoadHalfWidthTransform, Position + CornerDir * RoadHalfWidthTransform).GetBezier();
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
