using ColossalFramework.Math;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public abstract class MarkupLine : IToXml
    {
        public static string XmlName { get; } = "L";

        public abstract LineType Type { get; }
        public abstract bool SupportRules { get; }

        public Markup Markup { get; private set; }
        public ulong Id => PointPair.Hash;

        public MarkupPointPair PointPair { get; }
        public MarkupPoint Start => PointPair.First;
        public MarkupPoint End => PointPair.Second;
        public bool IsEnterLine => PointPair.IsSomeEnter;
        public bool IsNormal => PointPair.IsNormal;
        public bool IsStopLine => PointPair.IsStopLine;
        public bool IsCrosswalk => PointPair.IsCrosswalk;

        public abstract IEnumerable<MarkupLineRawRule> Rules { get; }

        public Bezier3 Trajectory { get; private set; }
        public MarkupStyleDash[] Dashes { get; private set; } = new MarkupStyleDash[0];

        public string XmlSection => XmlName;

        protected MarkupLine(Markup markup, MarkupPointPair pointPair, bool update = true)
        {
            Markup = markup;
            PointPair = pointPair;

            if (update)
                UpdateTrajectory();
        }
        protected MarkupLine(Markup markup, MarkupPoint first, MarkupPoint second, bool update = true) : this(markup, new MarkupPointPair(first, second), update) { }
        protected virtual void RuleChanged() => Markup.Update(this);

        public void UpdateTrajectory() => Trajectory = GetTrajectory();
        public virtual Bezier3 GetTrajectory()
        {
            var trajectory = new Bezier3
            {
                a = PointPair.First.Position,
                d = PointPair.Second.Position,
            };
            NetSegment.CalculateMiddlePoints(trajectory.a, PointPair.First.Direction, trajectory.d, PointPair.Second.Direction, true, true, out trajectory.b, out trajectory.c);

            return trajectory;
        }

        public void RecalculateDashes() => Dashes = GetDashes().ToArray();
        protected abstract IEnumerable<MarkupStyleDash> GetDashes();

        public override string ToString() => PointPair.ToString();

        public bool ContainsPoint(MarkupPoint point) => PointPair.ContainPoint(point);

        public IEnumerable<MarkupLine> IntersectLines
        {
            get
            {
                foreach (var intersect in Markup.GetIntersects(this))
                {
                    if (intersect.IsIntersect)
                        yield return intersect.Pair.GetOther(this);
                }
            }
        }

        public static MarkupLine FromStyle(Markup makrup, MarkupPointPair pointPair, Style.StyleType style)
        {
            switch (style & Style.StyleType.GroupMask)
            {
                case Style.StyleType.StopLine:
                    return new MarkupStopLine(makrup, pointPair, (StopLineStyle.StopLineType)(int)style);
                case Style.StyleType.Crosswalk:
                    return new MarkupCrosswalk(makrup, pointPair, (CrosswalkStyle.CrosswalkType)(int)style);
                case Style.StyleType.RegularLine:
                default:
                    return new MarkupRegularLine(makrup, pointPair, (RegularLineStyle.RegularLineType)(int)style);
            }
        }
        public virtual XElement ToXml()
        {
            var config = new XElement(XmlSection,
                new XAttribute(nameof(Id), Id),
                new XAttribute("T", (int)Type)
            );

            return config;
        }
        public static bool FromXml(XElement config, Markup makrup, Dictionary<ObjectId, ObjectId> map, out MarkupLine line)
        {

            var lineId = config.GetAttrValue<ulong>(nameof(Id));
            MarkupPointPair.FromHash(lineId, makrup, map, out MarkupPointPair pointPair);

            var type = (LineType)config.GetAttrValue("T", (int)pointPair.DefaultType);

            if (!makrup.TryGetLine(pointPair.Hash, out line))
            {
                switch (type)
                {
                    case LineType.Regular:
                        line = new MarkupRegularLine(makrup, pointPair);
                        break;
                    case LineType.Stop:
                        line = new MarkupStopLine(makrup, pointPair);
                        break;
                    case LineType.Crosswalk:
                        line = new MarkupCrosswalk(makrup, pointPair);
                        break;
                    default:
                        return false;
                }
            }

            return true;
        }
        public abstract void FromXml(XElement config, Dictionary<ObjectId, ObjectId> map);

        public enum LineType
        {
            [Description(nameof(Localize.LineStyle_RegularGroup))]
            Regular = Markup.Item.RegularLine,

            [Description(nameof(Localize.LineStyle_StopGroup))]
            Stop = Markup.Item.StopLine,

            [Description(nameof(Localize.CrosswalkStyle_Group))]
            Crosswalk = Markup.Item.Crosswalk,
        }
    }
    public class MarkupRegularLine : MarkupLine
    {
        public override LineType Type => LineType.Regular;
        public override bool SupportRules => true;

        private List<MarkupLineRawRule<RegularLineStyle>> RawRules { get; } = new List<MarkupLineRawRule<RegularLineStyle>>();
        public override IEnumerable<MarkupLineRawRule> Rules => RawRules.Cast<MarkupLineRawRule>();

        public MarkupRegularLine(Markup markup, MarkupPointPair pointPair) : base(markup, pointPair) { }
        public MarkupRegularLine(Markup markup, MarkupPointPair pointPair, RegularLineStyle.RegularLineType lineType) :
            base(markup, pointPair)
        {
            var lineStyle = TemplateManager.GetDefault<RegularLineStyle>((Style.StyleType)(int)lineType);
            AddRule(lineStyle, false, false);
            RecalculateDashes();
        }

        private void AddRule(MarkupLineRawRule<RegularLineStyle> rule, bool update = true)
        {
            rule.OnRuleChanged = RuleChanged;
            RawRules.Add(rule);

            if (update)
                RuleChanged();
        }
        public MarkupLineRawRule<RegularLineStyle> AddRule(RegularLineStyle lineStyle, bool empty = true, bool update = true)
        {
            var newRule = new MarkupLineRawRule<RegularLineStyle>(this, lineStyle, empty ? null : new EnterPointEdge(Start), empty ? null : new EnterPointEdge(End));
            AddRule(newRule, update);
            return newRule;
        }
        public MarkupLineRawRule<RegularLineStyle> AddRule(bool empty = true) => AddRule(TemplateManager.GetDefault<RegularLineStyle>(Style.StyleType.LineDashed), empty);
        public void RemoveRule(MarkupLineRawRule<RegularLineStyle> rule)
        {
            RawRules.Remove(rule);
            RuleChanged();
        }
        public void RemoveRules(MarkupLine intersectLine)
        {
            if (!RawRules.Any())
                return;

            RawRules.RemoveAll(r => Match(r.From) || Match(r.To));
            bool Match(ISupportPoint supportPoint) => supportPoint is IntersectSupportPoint lineRuleEdge && lineRuleEdge.LinePair.ContainLine(intersectLine);

            if (!RawRules.Any())
                AddRule(false);
        }

        protected override IEnumerable<MarkupStyleDash> GetDashes()
        {
            var rules = MarkupLineRawRule<RegularLineStyle>.GetRules(RawRules);

            var dashes = new List<MarkupStyleDash>();
            foreach (var rule in rules)
            {
                var trajectoryPart = Trajectory.Cut(rule.Start, rule.End);
                var ruleDashes = rule.LineStyle.Calculate(this, trajectoryPart).ToArray();

                dashes.AddRange(ruleDashes);
            }

            return dashes;
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();

            foreach (var rule in RawRules)
            {
                var ruleConfig = rule.ToXml();
                config.Add(ruleConfig);
            }

            return config;
        }
        public override void FromXml(XElement config, Dictionary<ObjectId, ObjectId> map)
        {
            foreach (var ruleConfig in config.Elements(MarkupLineRawRule<RegularLineStyle>.XmlName))
            {
                if (MarkupLineRawRule<RegularLineStyle>.FromXml(ruleConfig, this, map, out MarkupLineRawRule<RegularLineStyle> rule))
                    AddRule(rule, false);
            }
        }
    }
    public abstract class MarkupStraightLine<StyleType> : MarkupLine
        where StyleType: LineStyle
    {
        public override bool SupportRules => false;
        public MarkupLineRawRule<StyleType> Rule { get; set; }
        public override IEnumerable<MarkupLineRawRule> Rules
        {
            get
            {
                yield return Rule;
            }
        }

        protected MarkupStraightLine(Markup markup, MarkupPointPair pointPair, bool update = true) : base(markup, pointPair, update) { }
        protected MarkupStraightLine(Markup markup, MarkupPoint first, MarkupPoint second, bool update = true) : base(markup, first, second, update) { }

        public override Bezier3 GetTrajectory()
        {
            var dir = (PointPair.Second.Position - PointPair.First.Position).normalized;
            var trajectory = new Bezier3
            {
                a = PointPair.First.Position,
                d = PointPair.Second.Position,
            };
            NetSegment.CalculateMiddlePoints(trajectory.a, dir, trajectory.d, -dir, true, true, out trajectory.b, out trajectory.c);
            return trajectory;
        }
        protected override IEnumerable<MarkupStyleDash> GetDashes() => Rule.Style.Calculate(this, Trajectory);
        protected void SetRule(MarkupLineRawRule<StyleType> rule)
        {
            rule.OnRuleChanged = RuleChanged;
            Rule = rule;

            RuleChanged();
        }
        protected abstract void AddDefaultRule();
        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(Rule.ToXml());
            return config;
        }
        public override void FromXml(XElement config, Dictionary<ObjectId, ObjectId> map)
        {
            if (config.Element(MarkupLineRawRule<StyleType>.XmlName) is XElement ruleConfig && MarkupLineRawRule<StyleType>.FromXml(ruleConfig, this, map, out MarkupLineRawRule<StyleType> rule))
                SetRule(rule);
            else
                AddDefaultRule();
        }
    }
    public class MarkupStopLine : MarkupStraightLine<StopLineStyle>
    {
        public override LineType Type => LineType.Stop;

        public MarkupStopLine(Markup markup, MarkupPointPair pointPair) : base(markup, pointPair) { }
        public MarkupStopLine(Markup markup, MarkupPointPair pointPair, StopLineStyle.StopLineType lineType) : base(markup, pointPair)
        {
            AddDefaultRule(lineType);
        }
        protected override void AddDefaultRule() => AddDefaultRule();
        private void AddDefaultRule(StopLineStyle.StopLineType lineType = StopLineStyle.StopLineType.Solid)
        {
            var style = TemplateManager.GetDefault<StopLineStyle>((Style.StyleType)(int)lineType);
            SetRule(new MarkupLineRawRule<StopLineStyle>(this, style, new EnterPointEdge(Start), new EnterPointEdge(End)));
        }
    }
    public class MarkupCrosswalk : MarkupStraightLine<CrosswalkStyle>
    {
        public override LineType Type => LineType.Crosswalk;
        public float CornerAndNormalAngle => Start.Enter.CornerAndNormalAngle;
        public Vector3 NormalDir => Start.Enter.NormalDir;

        public MarkupCrosswalk(Markup markup, MarkupPointPair pointPair) : base(markup, pointPair) { }
        public MarkupCrosswalk(Markup markup, MarkupPointPair pointPair, CrosswalkStyle.CrosswalkType crosswalkType) : base(markup, pointPair)
        {
            AddDefaultRule(crosswalkType);
        }
        protected override void RuleChanged() => Markup.Update(this, true);
        protected override void AddDefaultRule() => AddDefaultRule();
        private void AddDefaultRule(CrosswalkStyle.CrosswalkType crosswalkType = CrosswalkStyle.CrosswalkType.Zebra)
        {
            var style = TemplateManager.GetDefault<CrosswalkStyle>((Style.StyleType)(int)crosswalkType);
            SetRule(new MarkupLineRawRule<CrosswalkStyle>(this, style, new EnterPointEdge(Start), new EnterPointEdge(End)));
        }

        public override Bezier3 GetTrajectory()
        {
            var dir = (PointPair.Second.Position - PointPair.First.Position).normalized;
            var offset = NormalDir * ((Rule?.Style.GetTotalWidth(this) ?? CrosswalkStyle.DefaultCrosswalkWidth) - MarkupCrosswalkPoint.Shift);

            var trajectory = new Bezier3
            {
                a = PointPair.First.Position + offset,
                d = PointPair.Second.Position + offset,
            };
            NetSegment.CalculateMiddlePoints(trajectory.a, dir, trajectory.d, -dir, true, true, out trajectory.b, out trajectory.c);

            return trajectory;
        }
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
        public bool CanIntersect
        {
            get
            {
                if (IsSelf || First.IsStopLine || Second.IsStopLine)
                    return false;

                if (First.ContainsPoint(Second.Start) || First.ContainsPoint(Second.End))
                    return false;

                return true;
            }
        }

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
