using ColossalFramework.Math;
using ModsCommon.Utilities;
using IMT.Tools;
using IMT.UI;
using IMT.UI.Editors;
using IMT.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public class NodeMarkup : Markup<NodeEnter>, ISupportFillers, ISupportCrosswalks
    {
        public static string XmlName { get; } = "M";

        public override string XmlSection => XmlName;

        public NodeMarkup(ushort nodeId) : base(nodeId) { }

        protected override Vector3 GetPosition() => Id.GetNode().m_position;
        protected override IEnumerable<ushort> GetEnters() => Id.GetNode().SegmentsId();
        protected override Enter NewEnter(ushort id) => new NodeEnter(this, id);
        

        public static bool FromXml(Version version, XElement config, ObjectsMap map, out NodeMarkup markup)
        {
            var nodeId = config.GetAttrValue<ushort>(nameof(Id));
            while (map.TryGetValue(new ObjectId() { Node = nodeId }, out ObjectId targetNode))
                nodeId = targetNode.Node;

            try
            {
                markup = MarkupManager.NodeManager.Get(nodeId);
                markup.FromXml(version, config, map);
                return true;
            }
            catch (Exception error)
            {
                Mod.Logger.Error($"Could not load node #{nodeId} markup", error);
                markup = null;
                MarkupManager.LoadErrors += 1;
                return false;
            }
        }
        public override void FromXml(Version version, XElement config, ObjectsMap map)
        {
            if (version < new Version("1.2"))
                map = VersionMigration.Befor1_2(this, map);

            base.FromXml(version, config, map);
        }
    }
}
