using ColossalFramework.Math;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace NodeMarkup.Manager
{
    public class MarkupLine : IToXml
    {
        public static string XmlName { get; } = "L";

        public Markup Markup { get; private set; }
        public ulong Id => PointPair.Hash;

        public MarkupPointPair PointPair { get; }
        public MarkupPoint Start => PointPair.First;
        public MarkupPoint End => PointPair.Second;

        public List<MarkupLineRawRule> RawRules { get; } = new List<MarkupLineRawRule>();

        public Bezier3 Trajectory { get; private set; }
        public MarkupDash[] Dashes { get; private set; }

        public string XmlSection => XmlName;

        public MarkupLine(Markup markup, MarkupPointPair pointPair)
        {
            Markup = markup;
            PointPair = pointPair;
        }
        public MarkupLine(Markup markup, MarkupPointPair pointPair, LineStyle lineStyle) : this(markup, pointPair)
        {
            AddRule(lineStyle, false, false);

            Update();
            RecalculateDashes();
        }
        public MarkupLine(Markup markup, MarkupPointPair pointPair, LineStyle.LineType lineType) : this(markup, pointPair, LineStyle.GetDefault(lineType)) { }
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
        private void AddRule(MarkupLineRawRule rule, bool empty = true, bool update = true)
        {
            rule.OnRuleChanged = RuleChanged;
            RawRules.Add(rule);

            if (update)
                RuleChanged();
        }
        public MarkupLineRawRule AddRule(LineStyle lineStyle, bool empty = true, bool update = true)
        {
            var newRule = new MarkupLineRawRule(lineStyle, empty ? null : new SelfPointRawRuleEdge(Start), empty ? null : new SelfPointRawRuleEdge(End));
            AddRule(newRule, empty, update);
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
            bool Match(LineRawRuleEdgeBase ruleEdge) => ruleEdge is LineRawRuleEdge lineRuleEdge && lineRuleEdge.Line == intersectLine;

            if (!RawRules.Any())
                AddRule();
        }

        public XElement ToXml()
        {
            var config = new XElement(XmlSection,
                new XAttribute(nameof(Id), Id)
            );

            foreach (var rule in RawRules)
            {
                var ruleConfig = rule.ToXml();
                config.Add(ruleConfig);
            }

            return config;
        }
        public static bool FromXml(XElement config, Markup makrup, out MarkupLine line)
        {
            var lineId = config.GetAttrValue<ulong>(nameof(Id));
            if (!makrup.TryGetLine(lineId, out line) && MarkupPointPair.FromHash(lineId, makrup, out MarkupPointPair pointPair))
            {
                line = new MarkupLine(makrup, pointPair);
                return true;
            }
            else
                return false;
        }
        public void FromXml(XElement config)
        {
            foreach (var ruleConfig in config.Elements(MarkupLineRawRule.XmlName))
            {
                if(MarkupLineRawRule.FromXml(ruleConfig, Markup, out MarkupLineRawRule rule))
                    AddRule(rule, true, false);
            }
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
