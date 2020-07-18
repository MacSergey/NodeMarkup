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

            Update();
        }
        public MarkupLine(Markup markup, MarkupPoint first, MarkupPoint second) : this(markup, new MarkupPointPair(first, second)) { }
        public MarkupLine(Markup markup, MarkupPointPair pointPair, LineStyle lineStyle) : this(markup, pointPair)
        {
            AddRule(lineStyle, false, false);
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
            var rules = MarkupLineRawRule.GetRules(RawRules);

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

        public IEnumerable<MarkupLine> IntersectLines => Markup.GetIntersects(this).Where(i => i.IsIntersect).Select(i => i.Pair.GetOther(this)).ToArray();
        private void AddRule(MarkupLineRawRule rule, bool update = true)
        {
            rule.OnRuleChanged = RuleChanged;
            RawRules.Add(rule);

            if (update)
                RuleChanged();
        }
        public MarkupLineRawRule AddRule(LineStyle lineStyle, bool empty = true, bool update = true)
        {
            var newRule = new MarkupLineRawRule(this, lineStyle, empty ? null : new EnterPointEdge(Start), empty ? null : new EnterPointEdge(End));
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
            bool Match(ILinePartEdge supportPoint) => supportPoint is LinesIntersectEdge lineRuleEdge && lineRuleEdge.Line == intersectLine;

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
                if (MarkupLineRawRule.FromXml(ruleConfig, this, map, out MarkupLineRawRule rule))
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

        public Markup Markup => First.Markup == Second.Markup ? First.Markup : null;
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
    public abstract class MarkupLinePart : IToXml
    {
        public Action OnRuleChanged { private get; set; }

        ILinePartEdge _from;
        ILinePartEdge _to;
        public ILinePartEdge From
        {
            get => _from;
            set
            {
                _from = value;
                RuleChanged();
            }
        }
        public ILinePartEdge To
        {
            get => _to;
            set
            {
                _to = value;
                RuleChanged();
            }
        }
        public MarkupLine Line { get; }
        public abstract string XmlSection { get; }

        public MarkupLinePart(MarkupLine line, ILinePartEdge from = null, ILinePartEdge to = null)
        {
            Line = line;
            From = from;
            To = to;
        }

        protected void RuleChanged() => OnRuleChanged?.Invoke();
        public bool GetFromT(out float t) => GetT(From, out t);
        public bool GetToT(out float t) => GetT(To, out t);
        private bool GetT(ILinePartEdge partEdge, out float t)
        {
            if (partEdge != null)
                return partEdge.GetT(Line, out t);
            else
            {
                t = -1;
                return false;
            }
        }
        public Bezier3 GetTrajectory()
        {
            GetFromT(out float from);
            GetToT(out float to);
            return Line.Trajectory.Cut(from, to);

        }

        public virtual XElement ToXml()
        {
            var config = new XElement(XmlSection);

            if (From != null)
                config.Add(From.ToXml());
            if (To != null)
                config.Add(To.ToXml());

            return config;
        }
    }
}
