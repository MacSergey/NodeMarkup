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
        public bool IsEnterLine => PointPair.IsSomeEnter;

        public List<MarkupLineRawRule> RawRules { get; } = new List<MarkupLineRawRule>();

        public Bezier3 Trajectory { get; private set; }
        public MarkupDash[] Dashes { get; private set; } = new MarkupDash[0];

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
        public MarkupLine(Markup markup, MarkupPointPair pointPair, LineStyle.LineType lineType) : this(markup, pointPair, TemplateManager.GetDefault(lineType)) { }
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
        private void AddRule(MarkupLineRawRule rule, bool update = true)
        {
            rule.OnRuleChanged = RuleChanged;
            RawRules.Add(rule);

            if (update)
                RuleChanged();
        }
        public MarkupLineRawRule AddRule(LineStyle lineStyle, bool empty = true, bool update = true)
        {
            var newRule = new MarkupLineRawRule(lineStyle, empty ? null : new EnterSupportPoint(Start), empty ? null : new EnterSupportPoint(End));
            AddRule(newRule, update);
            return newRule;
        }
        public MarkupLineRawRule AddRule() => AddRule(TemplateManager.GetDefault(LineStyle.LineType.Dashed));
        public void RemoveRule(MarkupLineRawRule rule)
        {
            RawRules.Remove(rule);
            RuleChanged();
        }

        public void RemoveRules(MarkupLine intersectLine)
        {
            RawRules.RemoveAll(r => Match(r.From) || Match(r.To));
            bool Match(SupportPointBase supportPoint) => supportPoint is LineSupportPoint lineRuleEdge && lineRuleEdge.Line == intersectLine;

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
        public static bool FromXml(XElement config, Markup makrup, Dictionary<InstanceID, InstanceID> map, out MarkupLine line)
        {
            var lineId = config.GetAttrValue<ulong>(nameof(Id));
            if (!makrup.TryGetLine(lineId, out line) && MarkupPointPair.FromHash(lineId, makrup, map, out MarkupPointPair pointPair))
            {
                line = new MarkupLine(makrup, pointPair);
                return true;
            }
            else
                return false;
        }
        public void FromXml(XElement config, Dictionary<InstanceID, InstanceID> map)
        {
            foreach (var ruleConfig in config.Elements(MarkupLineRawRule.XmlName))
            {
                if (MarkupLineRawRule.FromXml(ruleConfig, Markup, map, out MarkupLineRawRule rule))
                    AddRule(rule, false);
            }
        }
    }
#pragma warning disable CS0660 // Тип определяет оператор == или оператор !=, но не переопределяет Object.Equals(object o)
#pragma warning disable CS0661 // Тип определяет оператор == или оператор !=, но не переопределяет Object.GetHashCode()
    public struct MarkupLinePair
#pragma warning restore CS0661 // Тип определяет оператор == или оператор !=, но не переопределяет Object.GetHashCode()
#pragma warning restore CS0660 // Тип определяет оператор == или оператор !=, но не переопределяет Object.Equals(object o)
    {
        public static MarkupLinePairComparer Comparer { get; } = new MarkupLinePairComparer();
        public static bool operator ==(MarkupLinePair a, MarkupLinePair b) => Comparer.Equals(a, b);
        public static bool operator !=(MarkupLinePair a, MarkupLinePair b) => !Comparer.Equals(a, b);

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
