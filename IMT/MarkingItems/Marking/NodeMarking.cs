using ModsCommon.Utilities;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public class NodeMarking : Marking<SegmentEntrance>
    {
        public static string XmlName { get; } = "M";

        public override MarkingType Type => MarkingType.Node;
        public override SupportType Support { get; } = SupportType.Enters | SupportType.Points | SupportType.Lines | SupportType.Fillers | SupportType.Croswalks | SupportType.StyleTemplates | SupportType.IntersectionTemplates;

        protected override bool IsExist => Id.ExistNode();
        public override string XmlSection => XmlName;
        public override string PanelCaption => string.Format(Localize.Panel_NodeCaption, Id);
        public override LineType SupportLines => LineType.All;
        public override bool IsUnderground => Id.GetNode().m_flags.IsSet(NetNode.Flags.Underground);

        public NodeMarking(ushort nodeId) : base(nodeId) { }

        protected override Vector3 GetPosition()
        {
            var node = Id.GetNode();
            return node.m_position + Vector3.up * (node.m_heightOffset / 64f);
        }
        protected override IEnumerable<ushort> GetEnters() => Id.GetNode().SegmentIds();
        protected override Entrance NewEnter(ushort id) => new SegmentEntrance(this, id);
        public override void FromXml(Version version, XElement config, ObjectsMap map, bool needUpdate = true)
        {
            if (version < new Version("1.2"))
                map = VersionMigration.Befor1_2(this, map);

            base.FromXml(version, config, map, needUpdate);
        }

        public override string ToString() => $"N:{base.ToString()}";
    }
}
