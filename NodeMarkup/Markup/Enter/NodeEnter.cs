using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IMT.Manager
{
    public class NodeEnter : Enter<NodeMarkup>
    {
        public NodeEnter(NodeMarkup markup, ushort segmentId) : base(markup, segmentId) { }
        protected override NetSegment GetSegment() => Id.GetSegment();
        protected override bool GetIsStartSide() => GetSegment().m_startNode == Markup.Id;
    }
}
