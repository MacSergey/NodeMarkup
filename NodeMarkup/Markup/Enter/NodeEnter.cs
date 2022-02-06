using ModsCommon.Utilities;

namespace NodeMarkup.Manager
{
    public class NodeEnter : Enter<SegmentMarkup>
    {
        public override EnterType Type => EnterType.Node;

        public override int SideSign => IsStartSide ? 1 : -1;
        public override int NormalSign => 1;

        protected override bool IsExist => Id.ExistNode();

        public NodeEnter(SegmentMarkup markup, ushort nodeId) : base(markup, nodeId) { }

        public override ushort GetSegmentId() => Markup.Id;
        public override ref NetSegment GetSegment() => ref Markup.Id.GetSegment();
        public override bool GetIsStartSide() => GetSegment().m_startNode == Id;

    }
}
