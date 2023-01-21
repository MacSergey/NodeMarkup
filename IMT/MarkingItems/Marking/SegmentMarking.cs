using ModsCommon.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public class SegmentMarking : Marking<NodeEntrance>
    {
        public static string XmlName { get; } = "S";

        public override MarkingType Type => MarkingType.Segment;
        public override SupportType Support { get; } = SupportType.Enters | SupportType.Points | SupportType.Lines | SupportType.Fillers | SupportType.StyleTemplates | SupportType.IntersectionTemplates;

        public BezierTrajectory Trajectory { get; private set; }

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
        public SegmentMarking(ushort segmentId) : base(segmentId) { }

        protected override Vector3 GetPosition() => Id.GetSegment().m_middlePosition;
        protected override IEnumerable<ushort> GetEnters() => Id.GetSegment().NodeIds();
        protected override Entrance NewEnter(ushort id) => new NodeEntrance(this, id);

        protected override void UpdateEnters()
        {
            base.UpdateEnters();

            var firstEnter = EntersList[0];
            var lastEnter = EntersList[1];

            Trajectory = new BezierTrajectory(firstEnter.Position, firstEnter.NormalDir, lastEnter.Position, lastEnter.NormalDir, false, firstEnter.IsSmooth, lastEnter.IsSmooth);
        }

        public override string ToString() => $"S:{base.ToString()}";
    }
}
