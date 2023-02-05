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
    public class MarkingStopLine : MarkingLine
    {
        public override LineType Type => LineType.Stop;

        public MarkingLineRawRule<StopLineStyle> Rule { get; set; }
        public override IEnumerable<MarkingLineRawRule> Rules { get { yield return Rule; } }
        public override IEnumerable<ILinePartEdge> RulesEdges => RulesEnterPointEdge;

        public PropertyEnumValue<Alignment> RawStartAlignment { get; private set; }
        public PropertyEnumValue<Alignment> RawEndAlignment { get; private set; }

        public MarkingStopLine(Marking marking, MarkingPointPair pointPair, StopLineStyle style = null, Alignment firstAlignment = Alignment.Centre, Alignment secondAlignment = Alignment.Centre) : base(marking, pointPair, false)
        {
            RawStartAlignment = new PropertyEnumValue<Alignment>("AL", AlignmentChanged, firstAlignment);
            RawEndAlignment = new PropertyEnumValue<Alignment>("AR", AlignmentChanged, secondAlignment);

            style ??= SingletonManager<StyleTemplateManager>.Instance.GetDefault<StopLineStyle>(Style.StyleType.StopLineSolid);
            var rule = new MarkingLineRawRule<StopLineStyle>(this, style, new EnterPointEdge(Start), new EnterPointEdge(End));
            SetRule(rule);

            Update(true);
            Marking.RecalculateStyleData(this);
        }

        private void AlignmentChanged() => Marking.Update(this, true, true);
        protected override ITrajectory CalculateTrajectory() => new StraightTrajectory(PointPair.First.GetAbsolutePosition(RawStartAlignment), PointPair.Second.GetAbsolutePosition(RawEndAlignment));
        protected void SetRule(MarkingLineRawRule<StopLineStyle> rule)
        {
            rule.OnRuleChanged = RuleChanged;
            Rule = rule;
        }
        public override bool ContainsRule(MarkingLineRawRule rule) => rule != null && rule == Rule;
        protected override void GetStyleData(Action<IStyleData> addData)
        {
            Rule.Style.Calculate(this, Trajectory, addData);
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();

            config.Add(Rule.ToXml());
            RawStartAlignment.ToXml(config);
            RawEndAlignment.ToXml(config);

            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            if (config.Element(MarkingLineRawRule<StopLineStyle>.XmlName) is XElement ruleConfig && MarkingLineRawRule<StopLineStyle>.FromXml(ruleConfig, this, map, invert, typeChanged, out MarkingLineRawRule<StopLineStyle> rule))
                SetRule(rule);

            RawStartAlignment.FromXml(config);
            RawEndAlignment.FromXml(config);
        }
    }
}
