using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.Manager
{
    public class SegmentEnter : Enter<SegmentMarkup>
    {
        public override int SideSign => IsStartSide ? 1 : -1;
        public SegmentEnter(SegmentMarkup markup, ushort nodeId) : base(markup, nodeId) { }
        protected override NetSegment GetSegment() => Markup.Id.GetSegment();
        protected override bool GetIsStartSide() => GetSegment().m_startNode == Id;

    }
}
