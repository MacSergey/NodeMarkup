using ModsCommon.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace IMT.Manager
{
    public class SegmentEntrance : Entrance<NodeMarking>
    {
        public override EntranceType Type => EntranceType.Segment;
        public override MarkingPoint.PointType SupportPoints => MarkingPoint.PointType.All;

        private Dictionary<byte, MarkingCrosswalkPoint> CrosswalkPointsDic { get; set; } = new Dictionary<byte, MarkingCrosswalkPoint>();
        private Dictionary<byte, MarkingNormalPoint> NormalPointsDic { get; set; } = new Dictionary<byte, MarkingNormalPoint>();

        public int CrosswalkCount => CrosswalkPointsDic.Count;
        public int NormalCount => NormalPointsDic.Count;

        public IEnumerable<MarkingCrosswalkPoint> CrosswalkPoints => CrosswalkPointsDic.Values;
        public IEnumerable<MarkingNormalPoint> NormalPoints => NormalPointsDic.Values;

        public override int SideSign => IsStartSide ? -1 : 1;
        public override int NormalSign => -1;
        public override bool IsSmooth => true;

        protected override bool IsExist => Id.ExistSegment();

        public SegmentEntrance(NodeMarking marking, ushort segmentId) : base(marking, segmentId) { }
        protected override void Init()
        {
            base.Init();

            CrosswalkPointsDic = EnterPointsDic.Values.ToDictionary(p => p.Index, p => new MarkingCrosswalkPoint(p));
            NormalPointsDic = EnterPointsDic.Values.ToDictionary(p => p.Index, p => new MarkingNormalPoint(p));
        }
        public override void UpdatePoints()
        {
            base.UpdatePoints();

            foreach (var point in CrosswalkPointsDic.Values)
                point.Update();
            foreach (var point in NormalPointsDic.Values)
                point.Update();
        }

        public override bool TryGetPoint(byte pointIndex, MarkingPoint.PointType type, out MarkingPoint point)
        {
            switch (type)
            {
                case MarkingPoint.PointType.Crosswalk:
                    if (CrosswalkPointsDic.TryGetValue(pointIndex, out MarkingCrosswalkPoint crosswalkPoint))
                    {
                        point = crosswalkPoint;
                        return true;
                    }
                    break;
                case MarkingPoint.PointType.Normal:
                    if (NormalPointsDic.TryGetValue(pointIndex, out MarkingNormalPoint normalPoint))
                    {
                        point = normalPoint;
                        return true;
                    }
                    break;
                default:
                    return base.TryGetPoint(pointIndex, type, out point);
            }
            point = null;
            return false;
        }

        public override ushort GetSegmentId() => Id;
        public override ref NetSegment GetSegment() => ref Id.GetSegment();
        public override bool GetIsStartSide() => GetSegment().m_startNode == Marking.Id;
    }
}
