using ModsCommon.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public class SegmentMarkup : Markup<SegmentEnter>
    {
        public static string XmlName { get; } = "S";

        public override MarkupType Type => MarkupType.Segment;
        public override SupportType Support { get; } = SupportType.Enters | SupportType.Points | SupportType.Lines | SupportType.Fillers | SupportType.StyleTemplates | SupportType.IntersectionTemplates;

        protected override bool IsExist => Id.ExistSegment();
        public override string XmlSection => XmlName;
        public override string PanelCaption => string.Format(Localize.Panel_SegmentCaption, Id);
        public override bool IsUnderground
        {
            get
            {
                ref var segment = ref Id.GetSegment();
                return segment.m_startNode.GetNode().m_flags.IsSet(NetNode.Flags.Underground) && segment.m_endNode.GetNode().m_flags.IsSet(NetNode.Flags.Underground);
            }
        }
        public SegmentMarkup(ushort segmentId) : base(segmentId) { }

        protected override Vector3 GetPosition() => Id.GetSegment().m_middlePosition;
        protected override IEnumerable<ushort> GetEnters() => Id.GetSegment().NodeIds();
        protected override Enter NewEnter(ushort id) => new NodeEnter(this, id);

        public override string ToString() => $"S:{base.ToString()}";
    }
}
