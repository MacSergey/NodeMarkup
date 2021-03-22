using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.Manager
{
    public class NodeEnter : Enter<SegmentMarkup>
    {
        public override EnterType Type => EnterType.Node;

        public override int SideSign => IsStartSide ? 1 : -1;
        public override int NormalSign => 1;

        protected override bool IsExist => Id.ExistNode();

        public NodeEnter(SegmentMarkup markup, ushort nodeId) : base(markup, nodeId) { }

        protected override ushort GetSegmentId() => Markup.Id;
        protected override NetSegment GetSegment() => Markup.Id.GetSegment();
        protected override bool GetIsStartSide() => GetSegment().m_startNode == Id;

    }
}
