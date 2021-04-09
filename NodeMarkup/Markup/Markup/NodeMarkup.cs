using ColossalFramework.Math;
using ModsCommon.Utilities;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public class NodeMarkup : Markup<SegmentEnter>, ISupportCrosswalks
    {
        public static string XmlName { get; } = "M";

        public override MarkupType Type => MarkupType.Node;
        protected override bool IsExist => Id.ExistNode();
        public override string XmlSection => XmlName;
        public override string PanelCaption => string.Format(Localize.Panel_NodeCaption, Id);
        public override MarkupLine.LineType SupportLines => MarkupLine.LineType.All;
        protected EnterDic<ITrajectory> BetweenEnters { get; } = new EnterDic<ITrajectory>();
        public IEnumerable<ITrajectory> Contour
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
        protected override IEnumerable<ushort> GetEnters() => Id.GetNode().SegmentIds();
        protected override Enter NewEnter(ushort id) => new SegmentEnter(this, id);

        protected override void UpdateEntersProcess() => UpdateСontour();
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

                BetweenEnters[i, j] = new BezierTrajectory(betweenBezier);
            }
        }
        public bool GetBordersLine(Enter first, Enter second, out ITrajectory line)
        {
            var i = EntersList.IndexOf(first);
            var j = EntersList.IndexOf(second);
            return BetweenEnters.TryGetValue(i, j, out line);
        }
        public override void FromXml(Version version, XElement config, ObjectsMap map)
        {
            if (version < new Version("1.2"))
                map = VersionMigration.Befor1_2(this, map);

            base.FromXml(version, config, map);
        }

        protected class EnterDic<T> : Dictionary<int, T>
        {
            public T this[int i, int j]
            {
                get => this[GetId(i, j)];
                set => this[GetId(i, j)] = value;
            }
            private int GetId(int i, int j) => (i + 1) * 10 + j + 1;

            public bool TryGetValue(int i, int j, out T value) => TryGetValue(GetId(i, j), out value);
        }
    }
}
