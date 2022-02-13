using ModsCommon.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace NodeMarkup.Manager
{
    public class SegmentEnter : Enter<NodeMarkup>
    {
        public override EnterType Type => EnterType.Segment;
        public override MarkupPoint.PointType SupportPoints => MarkupPoint.PointType.All;

        private Dictionary<byte, MarkupCrosswalkPoint> CrosswalkPointsDic { get; set; } = new Dictionary<byte, MarkupCrosswalkPoint>();
        private Dictionary<byte, MarkupNormalPoint> NormalPointsDic { get; set; } = new Dictionary<byte, MarkupNormalPoint>();

        public int CrosswalkCount => CrosswalkPointsDic.Count;
        public int NormalCount => NormalPointsDic.Count;

        public IEnumerable<MarkupCrosswalkPoint> Crosswalks => CrosswalkPointsDic.Values;
        public IEnumerable<MarkupNormalPoint> Normals => NormalPointsDic.Values;

        public override int SideSign => IsStartSide ? -1 : 1;
        public override int NormalSign => -1;

        protected override bool IsExist => Id.ExistSegment();

        public SegmentEnter(NodeMarkup markup, ushort segmentId) : base(markup, segmentId) { }
        protected override void Init()
        {
            base.Init();

            CrosswalkPointsDic = EnterPointsDic.Values.ToDictionary(p => p.Num, p => new MarkupCrosswalkPoint(p));
            NormalPointsDic = EnterPointsDic.Values.ToDictionary(p => p.Num, p => new MarkupNormalPoint(p));
        }
        public override void UpdatePoints()
        {
            base.UpdatePoints();

            foreach (var point in CrosswalkPointsDic.Values)
                point.Update();
            foreach (var point in NormalPointsDic.Values)
                point.Update();
        }

        public override bool TryGetPoint(byte pointNum, MarkupPoint.PointType type, out MarkupPoint point)
        {
            switch (type)
            {
                case MarkupPoint.PointType.Enter:
                    return base.TryGetPoint(pointNum, type, out point);
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
        public bool GetBorder(MarkupEnterPoint point, out ITrajectory line)
        {
            if (point.IsFirst && Markup.GetBordersLine(Prev, this, out line))
                return true;
            else if (point.IsLast && Markup.GetBordersLine(this, Next, out line))
                return true;
            else
            {
                line = null;
                return false;
            }
        }

        public override ushort GetSegmentId() => Id;
        public override ref NetSegment GetSegment() => ref Id.GetSegment();
        public override bool GetIsStartSide() => GetSegment().m_startNode == Markup.Id;
    }
}
