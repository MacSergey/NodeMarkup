using ColossalFramework.Math;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public abstract class MarkupLine : IToXml
    {
        public static string XmlName { get; } = "L";

        public abstract LineType Type { get; }

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
        public abstract IEnumerable<ILinePartEdge> RulesEdges { get; }

        protected ILineTrajectory LineTrajectory { get; private set; }
        public ILineTrajectory Trajectory => LineTrajectory.Copy();
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

        public void UpdateTrajectory() => LineTrajectory = CalculateTrajectory();
        protected abstract ILineTrajectory CalculateTrajectory();

        public void RecalculateDashes() => Dashes = GetDashes().ToArray();
        protected abstract IEnumerable<MarkupStyleDash> GetDashes();

        public bool ContainsPoint(MarkupPoint point) => PointPair.ContainPoint(point);

        protected IEnumerable<ILinePartEdge> RulesEnterPointEdge
        {
            get
            {
                yield return new EnterPointEdge(Start);
                yield return new EnterPointEdge(End);
            }
        }
        protected IEnumerable<ILinePartEdge> RulesLinesIntersectEdge
        {
            get
            {
                foreach (var line in IntersectLines)
                    yield return new LinesIntersectEdge(this, line);
            }
        }

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
        public virtual void Render(RenderManager.CameraInfo cameraInfo, Color color, float width = 0.2f) => NodeMarkupTool.RenderTrajectory(cameraInfo, color, Trajectory, width);

        public static MarkupLine FromStyle(Markup markup, MarkupPointPair pointPair, Style.StyleType style)
        {
            switch (style & Style.StyleType.GroupMask)
            {
                case Style.StyleType.StopLine:
                    return new MarkupStopLine(markup, pointPair, (StopLineStyle.StopLineType)(int)style);
                case Style.StyleType.Crosswalk:
                    return new MarkupCrosswalkLine(markup, pointPair, (CrosswalkStyle.CrosswalkType)(int)style);
                case Style.StyleType.RegularLine:
                default:
                    var regularStyle = (RegularLineStyle.RegularLineType)(int)style;
                    if (pointPair.IsNormal)
                        return new MarkupNormalLine(markup, pointPair, regularStyle);
                    else
                        return new MarkupRegularLine(markup, pointPair, regularStyle);
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

            if (!makrup.TryGetLine(pointPair.Hash, out line))
            {
                var type = (LineType)config.GetAttrValue("T", (int)pointPair.DefaultType);
                switch (type)
                {
                    case LineType.Regular:
                        line = new MarkupRegularLine(makrup, pointPair);
                        break;
                    case LineType.Stop:
                        line = new MarkupStopLine(makrup, pointPair);
                        break;
                    case LineType.Crosswalk:
                        line = new MarkupCrosswalkLine(makrup, pointPair);
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
        public override string ToString() => PointPair.ToString();
    }
    public abstract class MarkupStraightLine<StyleType> : MarkupLine
    where StyleType : LineStyle
    {
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

        protected override ILineTrajectory CalculateTrajectory() => new StraightTrajectory(PointPair.First.Position, PointPair.Second.Position);

        protected override IEnumerable<MarkupStyleDash> GetDashes() => Rule.Style.Calculate(this, LineTrajectory);
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
    public class MarkupRegularLine : MarkupLine
    {
        public override LineType Type => LineType.Regular;

        private List<MarkupLineRawRule<RegularLineStyle>> RawRules { get; } = new List<MarkupLineRawRule<RegularLineStyle>>();
        public override IEnumerable<MarkupLineRawRule> Rules => RawRules.Cast<MarkupLineRawRule>();

        public MarkupRegularLine(Markup markup, MarkupPointPair pointPair) : base(markup, pointPair) { }
        protected MarkupRegularLine(Markup markup, MarkupPointPair pointPair, bool update = true) : base(markup, pointPair, update) { }
        public MarkupRegularLine(Markup markup, MarkupPointPair pointPair, RegularLineStyle.RegularLineType lineType) :
            base(markup, pointPair)
        {
            var lineStyle = TemplateManager.GetDefault<RegularLineStyle>((Style.StyleType)(int)lineType);
            AddRule(lineStyle, false, false);
            RecalculateDashes();
        }
        protected override ILineTrajectory CalculateTrajectory()
        {
            var trajectory = new Bezier3
            {
                a = PointPair.First.Position,
                d = PointPair.Second.Position,
            };
            NetSegment.CalculateMiddlePoints(trajectory.a, PointPair.First.Direction, trajectory.d, PointPair.Second.Direction, true, true, out trajectory.b, out trajectory.c);

            return new BezierTrajectory(trajectory);
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
            var newRule = GetDefaultRule(lineStyle, empty);
            AddRule(newRule, update);
            return newRule;
        }
        protected virtual MarkupLineRawRule<RegularLineStyle> GetDefaultRule(RegularLineStyle lineStyle, bool empty = true)
        {
            var from = empty ? null : new EnterPointEdge(Start);
            var to = empty ? null : new EnterPointEdge(End);
            return new MarkupLineRawRule<RegularLineStyle>(this, lineStyle, from, to);
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
                var trajectoryPart = LineTrajectory.Cut(rule.Start, rule.End);
                var ruleDashes = rule.LineStyle.Calculate(this, trajectoryPart).ToArray();

                dashes.AddRange(ruleDashes);
            }

            return dashes;
        }
        public override IEnumerable<ILinePartEdge> RulesEdges
        {
            get
            {
                foreach (var edge in RulesEnterPointEdge)
                    yield return edge;

                foreach (var edge in RulesLinesIntersectEdge)
                    yield return edge;
            }
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
    public class MarkupNormalLine : MarkupRegularLine
    {
        public MarkupNormalLine(Markup markup, MarkupPointPair pointPair) : base(markup, pointPair) { }
        public MarkupNormalLine(Markup markup, MarkupPointPair pointPair, RegularLineStyle.RegularLineType lineType) : base(markup, pointPair, lineType) { }

        protected override ILineTrajectory CalculateTrajectory() => new StraightTrajectory(PointPair.First.Position, PointPair.Second.Position);
    }
    public class MarkupCrosswalkLine : MarkupRegularLine
    {
        public override LineType Type => LineType.Crosswalk;
        public MarkupCrosswalk Crosswalk { get; set; }
        public bool IsInvert => End.Num < Start.Num;
        public Func<StraightTrajectory> TrajectoryGetter { get; set; }

        public MarkupCrosswalkLine(Markup markup, MarkupPointPair pointPair, CrosswalkStyle.CrosswalkType crosswalkType = CrosswalkStyle.CrosswalkType.Existent) : base(markup, pointPair, false) 
        {
            Crosswalk = new MarkupCrosswalk(Markup, this, crosswalkType);
            UpdateTrajectory();
            Markup.AddCrosswalk(Crosswalk);
        }
        protected override MarkupLineRawRule<RegularLineStyle> GetDefaultRule(RegularLineStyle lineStyle, bool empty = true)
        {
            var from = empty ? null : new CrosswalkBorderEdge(this, BorderPosition.Right);
            var to = empty ? null : new CrosswalkBorderEdge(this, BorderPosition.Left);
            return new MarkupLineRawRule<RegularLineStyle>(this, lineStyle, from, to);
        }
        protected override void RuleChanged() => Markup.Update(this, true, true);

        protected override ILineTrajectory CalculateTrajectory() => TrajectoryGetter();
        public float GetT(BorderPosition border) => (int)border;
        public override IEnumerable<ILinePartEdge> RulesEdges
        {
            get
            {
                yield return new CrosswalkBorderEdge(this, BorderPosition.Right);
                yield return new CrosswalkBorderEdge(this, BorderPosition.Left);

                foreach (var edge in RulesLinesIntersectEdge)
                {
                    if (edge.GetT(this, out float t) && 0 < t && t < 1)
                        yield return edge;
                }
            }
        }
        public override void Render(RenderManager.CameraInfo cameraInfo, Color color, float width) => NodeMarkupTool.RenderTrajectory(cameraInfo, color, Trajectory, width);
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
        public override IEnumerable<ILinePartEdge> RulesEdges
        {
            get
            {
                foreach (var edge in RulesEnterPointEdge)
                    yield return edge;
            }
        }
    }
    public class MarkupEnterLine : MarkupStraightLine<LineStyle>
    {
        public override LineType Type => throw new NotImplementedException();

        public override IEnumerable<ILinePartEdge> RulesEdges => throw new NotImplementedException();

        public MarkupEnterLine(Markup markup, MarkupPoint first, MarkupPoint second) : base(markup, first, second) { }

        protected override void AddDefaultRule()
        {
            throw new NotImplementedException();
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

        public class MarkupLinePairComparer : IEqualityComparer<MarkupLinePair>
        {
            public bool Equals(MarkupLinePair x, MarkupLinePair y) => (x.First == y.First && x.Second == y.Second) || (x.First == y.Second && x.Second == y.First);
            public int GetHashCode(MarkupLinePair pair) => pair.GetHashCode();
        }
    }
}
