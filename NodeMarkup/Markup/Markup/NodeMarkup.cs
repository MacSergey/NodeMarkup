using ColossalFramework.Math;
using ModsCommon.Utilities;
using NodeMarkup.Tools;
using NodeMarkup.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public class NodeMarkup : Markup<NodeEnter>, ISupportCrosswalks, ISupportIntersectionTemplate
    {
        public static string XmlName { get; } = "M";

        public override string XmlSection => XmlName;
        public override string PanelCaption => string.Format(Localize.Panel_NodeCaption, Id);
        public override MarkupLine.LineType SupportLines => MarkupLine.LineType.All;
        protected Dictionary<int, ILineTrajectory> BetweenEnters { get; } = new Dictionary<int, ILineTrajectory>();
        public IEnumerable<ILineTrajectory> Contour
        {
            get
            {
                foreach (var enter in Enters)
                    yield return enter.Line;
                foreach (var line in BetweenEnters.Values)
                    yield return line;
            }
        }

        public NodeMarkup(ushort nodeId) : base(nodeId) { }

        protected override Vector3 GetPosition() => Id.GetNode().m_position;
        protected override IEnumerable<ushort> GetEnters() => Id.GetNode().SegmentsId();
        protected override Enter NewEnter(ushort id) => new NodeEnter(this, id);

        protected override void UpdateBackup(ushort[] delete, ushort[] add, List<Enter> oldEnters, List<Enter> newEnters)
        {
            if (delete.Length != 1 || add.Length != 1)
                return;

            var oldEnter = oldEnters.Find(e => e.Id == delete[0]);
            var newEnter = newEnters.Find(e => e.Id == add[0]);

            var before = oldEnter.PointCount;
            var after = newEnter.PointCount;

            if (before != after && !NeedSetOrder && !IsEmpty && HaveLines(oldEnter))
                NeedSetOrder = true;

            if (NeedSetOrder)
            {
                if (Backup.Map.FirstOrDefault(p => p.Value.Type == ObjectType.Segment && p.Value.Segment == delete[0]) is KeyValuePair<ObjectId, ObjectId> pair)
                {
                    Backup.Map.Remove(pair.Key);
                    Backup.Map.AddSegment(pair.Key.Segment, add[0]);
                }
                else
                    Backup.Map.AddSegment(delete[0], add[0]);
            }

            if (before != after)
                return;

            var map = new ObjectsMap();
            map.AddSegment(delete[0], add[0]);
            var currentData = ToXml();
            RowEntersList = newEnters;
            Clear();
            FromXml(Mod.Version, currentData, map);
        }

        protected override void ProcessUpdate() => UpdateСontour();
        private void UpdateСontour()
        {
            BetweenEnters.Clear();

            for (var i = 0; i < EntersList.Count; i += 1)
            {
                var j = i.NextIndex(EntersList.Count);
                var prev = EntersList[i];
                var next = EntersList[j];

                var betweenBezier = new Bezier3()
                {
                    a = prev.LastPointSide,
                    d = next.FirstPointSide
                };
                NetSegment.CalculateMiddlePoints(betweenBezier.a, prev.NormalDir, betweenBezier.d, next.NormalDir, true, true, out betweenBezier.b, out betweenBezier.c);

                BetweenEnters[Math.Max(i, j) * 10 + Math.Min(i, j)] = new BezierTrajectory(betweenBezier);
            }
        }
        public bool GetBordersLine(Enter first, Enter second, out ILineTrajectory line)
        {
            var i = EntersList.IndexOf(first);
            var j = EntersList.IndexOf(second);
            return BetweenEnters.TryGetValue(Math.Max(i, j) * 10 + Math.Min(i, j), out line);
        }

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
