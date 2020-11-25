using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IMT.Manager
{
    public class SegmentEnter : Enter<SegmentMarkup>
    {
        public SegmentEnter(SegmentMarkup markup, ushort nodeId) : base(markup, nodeId) { }
        protected override NetSegment GetSegment() => Markup.Id.GetSegment();
        protected override bool GetIsStartSide() => GetSegment().m_startNode == Id;

    }
}
