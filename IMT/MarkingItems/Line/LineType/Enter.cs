using ColossalFramework.Math;
using IMT.Utilities;
using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public class MarkingEnterLine : MarkingLine
    {
        public override LineType Type => throw new NotImplementedException();
        public override IEnumerable<ILinePartEdge> RulesEdges => throw new NotImplementedException();
        public override IEnumerable<MarkingLineRawRule> Rules { get { yield break; } }
        public override int RuleCount => 0;

        public virtual Alignment StartAlignment { get; private set; } = Alignment.Centre;
        public virtual Alignment EndAlignment { get; private set; } = Alignment.Centre;

        public bool IsDot => IsSame && StartAlignment == EndAlignment;


        public MarkingEnterLine(Marking marking, MarkingPoint first, MarkingPoint second, Alignment firstAlignment = Alignment.Centre, Alignment secondAlignment = Alignment.Centre) : base(marking, MarkingPointPair.FromPoints(first, second, out bool invert), false)
        {
            StartAlignment = !invert ? firstAlignment : secondAlignment;
            EndAlignment = !invert ? secondAlignment : firstAlignment;

            Update(true);
        }

        public void Update(Alignment startAlignment, Alignment endAlignment, bool onlySelfUpdate = false)
        {
            StartAlignment = startAlignment;
            EndAlignment = endAlignment;

            Update(onlySelfUpdate);
        }

        protected override ITrajectory CalculateTrajectory() => new StraightTrajectory(Start.GetAbsolutePosition(StartAlignment), End.GetAbsolutePosition(EndAlignment));
        public override bool ContainsRule(MarkingLineRawRule rule) => false;
        protected override void GetStyleData(Action<IStyleData> addData) { }

        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged) { }
        public override void GetUsedAssets(HashSet<string> networks, HashSet<string> props, HashSet<string> trees) { }
    }
}
