using ModsCommon.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public class SegmentMarkup : Markup<SegmentEnter>
    {
        public static string XmlName { get; } = "S";

        public override MarkupType Type => MarkupType.Segment;
        protected override bool IsExist => Id.ExistSegment();
        public override string XmlSection => XmlName;
        public override string PanelCaption => string.Format(Localize.Panel_SegmentCaption, Id);

        public SegmentMarkup(ushort segmentId) : base(segmentId) { }

        protected override Vector3 GetPosition() => Id.GetSegment().m_middlePosition;
        protected override IEnumerable<ushort> GetEnters() => Id.GetSegment().NodeIds();
        protected override Enter NewEnter(ushort id) => new NodeEnter(this, id);

        public override string ToString() => $"S:{base.ToString()}";
    }
}
