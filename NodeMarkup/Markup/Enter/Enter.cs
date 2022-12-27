using ColossalFramework.Math;
using ModsCommon;
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
        public event Action OnPointOrderChanged;

        public static string XmlName { get; } = "E";

        public virtual MarkupPoint.PointType SupportPoints => MarkupPoint.PointType.Enter;
        public Markup Markup { get; private set; }
        public abstract EnterType Type { get; }
        public Markup.SupportType Support => Markup.SupportType.Enters;
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
        public string RoadName => GetSegment().Info.name;

        private static VehicleInfo.VehicleType RoadType { get; } =
            VehicleInfo.VehicleType.Car |
            VehicleInfo.VehicleType.Bicycle |
            VehicleInfo.VehicleType.Tram |
            VehicleInfo.VehicleType.Trolleybus;
        private static bool IsVehicleLane(NetInfo.Lane info) => (info.m_vehicleType & RoadType) != 0;
        private static bool IsTaxiwayLane(NetInfo.Lane info) => (info.m_vehicleType & VehicleInfo.VehicleType.Plane) != 0;
        private static bool IsTrackLane(NetInfo.Lane info) => (info.m_vehicleType & (VehicleInfo.VehicleType.Train | VehicleInfo.VehicleType.Metro | VehicleInfo.VehicleType.Monorail)) != 0;
        private static bool IsPathLane(NetInfo.Lane info) => (info.m_vehicleType & VehicleInfo.VehicleType.Bicycle) != 0 || (info.m_laneType & NetInfo.LaneType.Pedestrian) != 0;

        public IEnumerable<DriveLane> DriveLanes
        {
            get
            {
                var segment = GetSegment();
                var info = segment.Info;
                var lanes = segment.GetLaneIds().ToArray();
                var isRoad = info.m_netAI is RoadBaseAI;
                var isTaxiway = info.m_netAI is TaxiwayAI || info.m_netAI is RunwayAI || info.m_netAI is AirportAreaRunwayAI;
                var isTrack = info.m_netAI is TrainTrackBaseAI || info.m_netAI is MetroTrackBaseAI;
                var isPath = info.m_netAI is PedestrianPathAI || info.m_netAI is PedestrianBridgeAI || info.m_netAI is PedestrianTunnelAI || info.m_netAI is PedestrianWayAI;

                foreach (var index in IsLaneInvert ? info.m_sortedLanes : info.m_sortedLanes.Reverse())
                {
                    var lane = info.m_lanes[index];
                    if (isRoad)
                    {
                        if (IsVehicleLane(lane))
                        {
                            var driveLane = new DriveLane(this, lanes[index], lane, NetworkType.Road);
                            yield return driveLane;
                        }
                    }
                    else if (isTaxiway)
                    {
                        if (IsTaxiwayLane(lane))
                        {
                            var driveLane = new DriveLane(this, lanes[index], lane, NetworkType.Taxiway);
                            yield return driveLane;
                            yield return driveLane;
                        }
                    }
                    else if (isTrack)
                    {
                        if (IsTrackLane(lane))
                        {
                            var driveLane = new DriveLane(this, lanes[index], lane, NetworkType.Track);
                            yield return driveLane;
                        }
                    }
                    else if (isPath)
                    {
                        if (IsPathLane(lane))
                        {
                            var driveLane = new DriveLane(this, lanes[index], lane, NetworkType.Path);
                            yield return driveLane;
                        }
                    }
                }
            }
        }
        protected Dictionary<byte, MarkupEnterPoint> EnterPointsDic { get; private set; } = new Dictionary<byte, MarkupEnterPoint>();
        protected byte[] SortedIndexes { get; private set; }
        protected Dictionary<byte, MarkupLanePoint> LanePointsDic { get; private set; } = new Dictionary<byte, MarkupLanePoint>();

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

        public int LanePointCount => LanePointsDic.Count;
        public IEnumerable<MarkupLanePoint> LanePoints => LanePointsDic.Values;

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

            Update();
            Init();
        }
        protected virtual void Init()
        {
            var segment = GetSegment();
            IsStartSide = GetIsStartSide();
            IsLaneInvert = IsStartSide ^ segment.IsInvert();
            FirstLane = segment.m_lanes;

            var sources = new List<IPointSource>();

            //if (segment.Info is IMarkingNetInfo info)
            //{
            //    foreach (var position in IsLaneInvert ? info.MarkupPoints : info.MarkupPoints.Reverse())
            //        sources.Add(new RoadGeneratorPointSource(this, IsLaneInvert ? position : -position));
            //}
            //else
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

            for (var i = 0; i < sources.Count; i += 1)
            {
                var point = new MarkupEnterPoint((byte)(i + 1), sources[i]);
                EnterPointsDic[point.Index] = point;
            }

            ResetPoints();

            for (int i = 0; i < sources.Count - 1; i += 1)
            {
                //var index = SideSign > 0 ? i : sources.Count - 1 - i;
                var laneSource = new NetLanePointSource(this, (byte)i);
                var lanePoint = new MarkupLanePoint((byte)(i + 1), laneSource);
                LanePointsDic[lanePoint.Index] = lanePoint;
            }
        }

        public abstract ushort GetSegmentId();
        public abstract ref NetSegment GetSegment();
        public abstract bool GetIsStartSide();
        public virtual bool TryGetPoint(byte index, MarkupPoint.PointType type, out MarkupPoint point)
        {
            switch (type)
            {
                case MarkupPoint.PointType.Lane:
                    if (LanePointsDic.TryGetValue(index, out var lanePoint))
                    {
                        point = lanePoint;
                        return true;
                    }
                    break;
                default:
                    if (EnterPointsDic.TryGetValue(index, out var enterPoint))
                    {
                        point = enterPoint;
                        return true;
                    }
                    break;
            }
            point = null;
            return false;
        }
        public virtual bool TryGetSortedPoint(byte index, MarkupPoint.PointType type, out MarkupPoint point)
        {
            if (type == MarkupPoint.PointType.Enter && index < PointCount)
            {
                var sortedIndex = SortedIndexes[index];
                return TryGetPoint(sortedIndex, type, out point);
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

            foreach (var point in LanePointsDic.Values)
                point.Update();
        }

        public void ResetPoints()
        {
            var points = Points.ToArray();
            if (SingletonManager<RoadTemplateManager>.Instance.TryGetOffsets(RoadName, out var offsets))
            {
                for (var i = 0; i < Math.Min(offsets.Length, points.Length); i += 1)
                {
                    if (IsLaneInvert)
                        points[points.Length - 1 - i].Offset.Value = -offsets[i];
                    else
                        points[i].Offset.Value = offsets[i];
                }
            }
            else
            {
                foreach (var point in points)
                    point.Reset();
            }

            SortPoints();
        }
        public void SortPoints()
        {
            var sortedIndexes = Points.OrderBy(p => (IsLaneInvert ? 1 : -1) * p.GetRelativePosition()).Select(p => p.Index).ToArray();

            if (SortedIndexes == null || SortedIndexes.Length != sortedIndexes.Length)
                SortedIndexes = sortedIndexes;
            else
            {
                var changed = false;
                for (var i = 0; i < sortedIndexes.Length; i += 1)
                {
                    if (sortedIndexes[i] != SortedIndexes[i])
                    {
                        changed = true;
                        break;
                    }
                }

                if (changed)
                {
                    SortedIndexes = sortedIndexes;
                    OnPointOrderChanged?.Invoke();
                }
            }
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
        public int PointCount { get; private set; }
        public float NormalAngle { get; private set; }
        public float CornerAngle { get; private set; }

        public string XmlSection => Enter.XmlName;

        private EnterData() { }
        public EnterData(Enter enter)
        {
            Id = enter.Id;
            PointCount = enter.PointCount;
            NormalAngle = enter.NormalAngle;
            CornerAngle = enter.CornerAngle;
        }
        public static EnterData FromXml(XElement config)
        {
            var data = new EnterData
            {
                Id = config.GetAttrValue<ushort>(nameof(Id)),
                PointCount = config.GetAttrValue<int>("P"),
                NormalAngle = config.GetAttrValue<float>("A")
            };
            return data;
        }

        public XElement ToXml()
        {
            var config = new XElement(XmlSection);
            config.AddAttr(nameof(Id), Id);
            config.AddAttr("P", PointCount);
            config.AddAttr("A", NormalAngle);
            return config;
        }
    }
    public enum EnterType
    {
        Node,
        Segment
    }
}
