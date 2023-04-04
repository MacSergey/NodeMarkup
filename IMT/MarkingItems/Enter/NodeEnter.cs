using ModsCommon.Utilities;
using UnityEngine;

namespace IMT.Manager
{
    public class NodeEntrance : Entrance<SegmentMarking>
    {
        public override EntranceType Type => EntranceType.Node;

        public override int SideSign => IsStartSide ? 1 : -1;
        public override int NormalSign => 1;
        protected override bool CanBeSmooth => (Id.GetNode().flags & NetNode.FlagsLong.Middle) != 0;

        protected override bool IsExist => Id.ExistNode();

        public NodeEntrance(SegmentMarking marking, ushort nodeId) : base(marking, nodeId) { }

        public override ushort GetSegmentId() => Marking.Id;
        public override ref NetSegment GetSegment() => ref Marking.Id.GetSegment();
        protected override bool GetIsStartSide() => GetSegment().m_startNode == Id;

    }
}
