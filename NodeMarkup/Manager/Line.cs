using ColossalFramework.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.Manager
{
    public class MarkupLine
    {
        public Markup Markup { get; private set; }

        public MarkupPointPair PointPair { get; }
        public MarkupPoint Start => PointPair.First;
        public MarkupPoint End => PointPair.Second;

        public List<MarkupLineRawRule> RawRules { get; } = new List<MarkupLineRawRule>();

        public Bezier3 Trajectory { get; private set; }
        public MarkupDash[] Dashes { get; private set; }

        public MarkupLine(Markup markup, MarkupPointPair pointPair, LineStyle.Type lineType) : this(markup, pointPair, LineStyle.GetDefault(lineType)) { }
        public MarkupLine(Markup markup, MarkupPointPair pointPair, LineStyle lineStyle)
        {
            Markup = markup;
            PointPair = pointPair;

            AddRule(lineStyle, false, false);

            Update();
            RecalculateDashes();
        }
        private void RuleChanged() => Markup.Update(this);
        public void Update()
        {
            var trajectory = new Bezier3
            {
                a = PointPair.First.Position,
                d = PointPair.Second.Position,
            };
            NetSegment.CalculateMiddlePoints(trajectory.a, PointPair.First.Direction, trajectory.d, PointPair.Second.Direction, true, true, out trajectory.b, out trajectory.c);

            Trajectory = trajectory;
        }
        public void RecalculateDashes()
        {
            var rules = MarkupLineRawRule.GetRules(this, RawRules);

            var dashes = new List<MarkupDash>();
            foreach (var rule in rules)
            {
                var trajectoryPart = Trajectory.Cut(rule.Start, rule.End);
                var ruleDashes = rule.LineStyle.Calculate(trajectoryPart).ToArray();

                dashes.AddRange(ruleDashes);
            }

            Dashes = dashes.ToArray();
        }
        public override string ToString() => PointPair.ToString();

        public bool ContainPoint(MarkupPoint point) => PointPair.ContainPoint(point);

        public MarkupLine[] IntersectWith()
        {
            var intersectWith = Markup.GetIntersects(this).Where(i => i.IsIntersect).Select(i => i.Pair.GetOther(this)).ToArray();
            return intersectWith;
        }
        public MarkupLineRawRule AddRule(LineStyle lineStyle, bool empty = true, bool update = true)
        {
            var newRule = new MarkupLineRawRule(lineStyle, empty ? null : new SelfPointRawRuleEdge(Start), empty ? null : new SelfPointRawRuleEdge(End));
            newRule.OnRuleChanged = RuleChanged;
            RawRules.Add(newRule);

            if(update)
                RuleChanged();

            return newRule;
        }
        public MarkupLineRawRule AddRule() => AddRule(LineStyle.DefaultDashed);
        public void RemoveRule(MarkupLineRawRule rule)
        {
            RawRules.Remove(rule);
            RuleChanged();
        }

        public void RemoveRules(MarkupLine intersectLine)
        {
            RawRules.RemoveAll(r => Match(r.From) || Match(r.To));
            bool Match(IMarkupLineRawRuleEdge ruleEdge) => ruleEdge is LineRawRuleEdge lineRuleEdge && lineRuleEdge.Line == intersectLine;

            if (!RawRules.Any())
                AddRule();
        }
    }
    public struct MarkupLinePair
    {
        public MarkupLine First;
        public MarkupLine Second;

        public bool IsSelf => First == Second;

        public MarkupLinePair(MarkupLine first, MarkupLine second)
        {
            First = first;
            Second = second;
        }
        public bool ContainLine(MarkupLine line) => First == line || Second == line;

        public MarkupLine GetOther(MarkupLine line)
        {
            if (!ContainLine(line))
                return null;
            else
                return line == First ? Second : First;
        }

        public override string ToString() => $"{First}—{Second}";
    }
    public class MarkupLinePairComparer : IEqualityComparer<MarkupLinePair>
    {
        public bool Equals(MarkupLinePair x, MarkupLinePair y) => (x.First == y.First && x.Second == y.Second) || (x.First == y.Second && x.Second == y.First);

        public int GetHashCode(MarkupLinePair pair) => pair.GetHashCode();
    }
}
