using ColossalFramework.Math;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace NodeMarkup.Manager
{
    public abstract class MarkupLine : IToXml
    {
        public static string XmlName { get; } = "L";

        public abstract bool SupportRules { get;}

        public Markup Markup { get; private set; }
        public ulong Id => PointPair.Hash;

        public MarkupPointPair PointPair { get; }
        public MarkupPoint Start => PointPair.First;
        public MarkupPoint End => PointPair.Second;
        public bool IsEnterLine => PointPair.IsSomeEnter;

        public List<MarkupLineRawRule> RawRules { get; } = new List<MarkupLineRawRule>();

        public Bezier3 Trajectory { get; protected set; }
        public MarkupStyleDash[] Dashes { get; private set; } = new MarkupStyleDash[0];

        public string XmlSection => XmlName;

        protected MarkupLine(Markup markup, MarkupPointPair pointPair)
        {
            Markup = markup;
            PointPair = pointPair;

            UpdateTrajectory();
        }
        protected MarkupLine(Markup markup, MarkupPoint first, MarkupPoint second) : this(markup, new MarkupPointPair(first, second)) { }
        protected MarkupLine(Markup markup, MarkupPointPair pointPair, LineStyle lineStyle) : this(markup, pointPair)
        {
            AddRule(lineStyle, false, false);
            RecalculateDashes();
        }
        private void RuleChanged() => Markup.Update(this);
        public virtual void UpdateTrajectory()
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

            var dashes = new List<MarkupStyleDash>();
            foreach (var rule in rules)
            {
                var trajectoryPart = Trajectory.Cut(rule.Start, rule.End);
                var ruleDashes = rule.LineStyle.Calculate(this, trajectoryPart).ToArray();

                dashes.AddRange(ruleDashes);
            }

            Dashes = dashes.ToArray();
        }
        public override string ToString() => PointPair.ToString();

        public bool ContainsPoint(MarkupPoint point) => PointPair.ContainPoint(point);

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
        public MarkupLineRawRule AddRule(bool empty = true) => AddRule(TemplateManager.GetDefault<LineStyle>(Style.StyleType.LineDashed), empty);
        public void RemoveRule(MarkupLineRawRule rule)
        {
            RawRules.Remove(rule);
            RuleChanged();
        }
        public void RemoveRules(MarkupLine intersectLine)
        {
            RawRules.RemoveAll(r => Match(r.From) || Match(r.To));
            bool Match(ISupportPoint supportPoint) => supportPoint is IntersectSupportPoint lineRuleEdge && lineRuleEdge.LinePair.ContainLine(intersectLine);

            if (!RawRules.Any())
                AddRule(false);
        }

        public static MarkupLine FromPointPair(Markup makrup, MarkupPointPair pointPair)
        {
            if (pointPair.IsSomeEnter)
                return new MarkupStopLine(makrup, pointPair);
            else
                return new MarkupRegularLine(makrup, pointPair);
        }
        public static MarkupLine FromStyle(Markup makrup, MarkupPointPair pointPair, Style.StyleType style)
        {
            switch (style & Style.StyleType.GroupMask)
            {
                case Style.StyleType.StopLine:
                    return new MarkupStopLine(makrup, pointPair, (StopLineStyle.StopLineType)(int)style);
                case Style.StyleType.RegularLine:
                default:
                    return new MarkupRegularLine(makrup, pointPair, (RegularLineStyle.RegularLineType)(int)style);
            }
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
        public static bool FromXml(XElement config, Markup makrup, Dictionary<ObjectId, ObjectId> map, out MarkupLine line)
        {
            var lineId = config.GetAttrValue<ulong>(nameof(Id));
            MarkupPointPair.FromHash(lineId, makrup, map, out MarkupPointPair pointPair);
            if (!makrup.TryGetLine(pointPair.Hash, out line))
            {
                line = FromPointPair(makrup, pointPair);
                return true;
            }
            else
                return false;
        }
        public void FromXml(XElement config, Dictionary<ObjectId, ObjectId> map)
        {
            foreach (var ruleConfig in config.Elements(MarkupLineRawRule.XmlName))
            {
                if (MarkupLineRawRule.FromXml(ruleConfig, this, map, out MarkupLineRawRule rule))
                    AddRule(rule, false);
            }
        }
    }
    public class MarkupRegularLine : MarkupLine
    {
        public override bool SupportRules => true;

        public MarkupRegularLine(Markup markup, MarkupPointPair pointPair) : base(markup, pointPair) { }
        public MarkupRegularLine(Markup markup, MarkupPointPair pointPair, RegularLineStyle.RegularLineType lineType) :
            base(markup, pointPair, TemplateManager.GetDefault<RegularLineStyle>((Style.StyleType)(int)lineType))
        { }
    }
    public abstract class MarkupStraightLine : MarkupLine
    {
        protected MarkupStraightLine(Markup markup, MarkupPointPair pointPair) : base(markup, pointPair) { }
        protected MarkupStraightLine(Markup markup, MarkupPoint first, MarkupPoint second) : base(markup, first, second) { }
        protected MarkupStraightLine(Markup markup, MarkupPointPair pointPair, LineStyle lineStyle) : base(markup, pointPair, lineStyle) { }

        public override void UpdateTrajectory()
        {
            var dir = (PointPair.Second.Position - PointPair.First.Position).normalized;
            Trajectory = new Bezier3
            {
                a = PointPair.First.Position,
                b = PointPair.First.Position + dir,
                c = PointPair.Second.Position - dir,
                d = PointPair.Second.Position,
            };
        }
    }
    public class MarkupStopLine : MarkupStraightLine
    {
        public override bool SupportRules => false;

        public MarkupStopLine(Markup markup, MarkupPointPair pointPair) : base(markup, pointPair) { }
        public MarkupStopLine(Markup markup, MarkupPointPair pointPair, StopLineStyle.StopLineType lineType) :
            base(markup, pointPair, TemplateManager.GetDefault<StopLineStyle>((Style.StyleType)(int)lineType))
        { }
    }

    public struct MarkupLinePair
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

        ISupportPoint _from;
        ISupportPoint _to;
        public ISupportPoint From
        {
            get => _from;
            set
            {
                _from = value;
                RuleChanged();
            }
        }
        public ISupportPoint To
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

        public MarkupLinePart(MarkupLine line, ISupportPoint from = null, ISupportPoint to = null)
        {
            Line = line;
            From = from;
            To = to;
        }

        protected void RuleChanged() => OnRuleChanged?.Invoke();
        public bool GetFromT(out float t) => GetT(From, out t);
        public bool GetToT(out float t) => GetT(To, out t);
        private bool GetT(ISupportPoint partEdge, out float t)
        {
            if (partEdge != null)
                return partEdge.GetT(Line, out t);
            else
            {
                t = -1;
                return false;
            }
        }
        public bool GetTrajectory(out Bezier3 bezier)
        {
            var succes = false;
            succes |= GetFromT(out float from);
            succes |= GetToT(out float to);

            if (succes)
            {
                bezier = Line.Trajectory.Cut(from != -1 ? from : to, to != -1 ? to : from);
                return true;
            }
            else
            {
                bezier = default;
                return false;
            }

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
