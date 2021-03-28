using ColossalFramework.Math;
using ModsCommon.Utilities;
using NodeMarkup.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public abstract class MarkupLine : IStyleItem, IToXml
    {
        public static string XmlName { get; } = "L";

        public string DeleteCaptionDescription => Localize.LineEditor_DeleteCaptionDescription;
        public string DeleteMessageDescription => Localize.LineEditor_DeleteMessageDescription;

        public abstract LineType Type { get; }

        public Markup Markup { get; private set; }
        public ulong Id => PointPair.Hash;

        public MarkupPointPair PointPair { get; }
        public MarkupPoint Start => PointPair.First;
        public MarkupPoint End => PointPair.Second;
        public virtual bool IsSupportRules => false;
        public bool IsEnterLine => PointPair.IsSomeEnter;
        public bool IsNormal => PointPair.IsNormal;
        public bool IsStopLine => PointPair.IsStopLine;
        public bool IsCrosswalk => PointPair.IsCrosswalk;
        public virtual Alignment Alignment => Alignment.Centre;

        public bool HasOverlapped => Rules.Any(r => r.IsOverlapped);

        public abstract IEnumerable<MarkupLineRawRule> Rules { get; }
        public abstract IEnumerable<ILinePartEdge> RulesEdges { get; }

        protected ITrajectory LineTrajectory { get; private set; }
        public ITrajectory Trajectory => LineTrajectory.Copy();
        public LodDictionaryArray<IStyleData> StyleData { get; } = new LodDictionaryArray<IStyleData>();

        public LineBorders Borders => new LineBorders(this);

        public string XmlSection => XmlName;

        protected MarkupLine(Markup markup, MarkupPointPair pointPair, bool update = true)
        {
            Markup = markup;
            PointPair = pointPair;

            if (update)
                Update(true);
        }
        protected virtual void RuleChanged() => Markup.Update(this, true);

        public void Update(bool onlySelfUpdate = false)
        {
            LineTrajectory = CalculateTrajectory();
            if (!onlySelfUpdate)
                Markup.Update(this);
        }
        protected abstract ITrajectory CalculateTrajectory();

        public void RecalculateStyleData()
        {
#if DEBUG_RECALCULATE
            Mod.Logger.Debug($"Recalculate line {this}");
#endif
            foreach (var lod in EnumExtension.GetEnumValues<MarkupLOD>())
                RecalculateStyleData(lod);
        }
        private void RecalculateStyleData(MarkupLOD lod) => StyleData[lod] = GetStyleData(lod).ToArray();
        protected abstract IEnumerable<IStyleData> GetStyleData(MarkupLOD lod);

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
        public virtual void Render(OverlayData data) => Trajectory.Render(data);
        public abstract bool ContainsRule(MarkupLineRawRule rule);
        public bool ContainsEnter(Enter enter) => PointPair.ContainsEnter(enter);

        public Dependences GetDependences() => Markup.GetLineDependences(this);
        public bool IsStart(MarkupPoint point) => Start == point;
        public bool IsEnd(MarkupPoint point) => End == point;
        public Alignment GetAlignment(MarkupPoint point) => PointPair.ContainPoint(point) ? (IsStart(point) ? Alignment : Alignment.Invert()) : Alignment.Centre;


        public virtual XElement ToXml()
        {
            var config = new XElement(XmlSection,
                new XAttribute(nameof(Id), Id),
                new XAttribute("T", (int)Type)
            );

            return config;
        }
        public static bool FromXml(XElement config, Markup markup, ObjectsMap map, out MarkupLine line, out bool invert)
        {
            var lineId = config.GetAttrValue<ulong>(nameof(Id));
            if (!MarkupPointPair.FromHash(lineId, markup, map, out MarkupPointPair pointPair, out invert))
            {
                line = null;
                return false;
            }

            if (!markup.TryGetLine(pointPair, out line))
            {
                var type = (LineType)config.GetAttrValue("T", (int)pointPair.DefaultType);
                if ((type & markup.SupportLines) == 0)
                    return false;

                switch (type)
                {
                    case LineType.Regular:
                        line = pointPair.IsNormal ? new MarkupNormalLine(markup, pointPair) : new MarkupRegularLine(markup, pointPair);
                        break;
                    case LineType.Stop:
                        line = new MarkupStopLine(markup, pointPair);
                        break;
                    case LineType.Crosswalk:
                        line = new MarkupCrosswalkLine(markup, pointPair);
                        break;
                    default:
                        return false;
                }
            }

            return true;
        }
        public abstract void FromXml(XElement config, ObjectsMap map, bool invert);

        public enum LineType
        {
            [Description(nameof(Localize.LineStyle_RegularLinesGroup))]
            Regular = Markup.Item.RegularLine,

            [Description(nameof(Localize.LineStyle_StopLinesGroup))]
            Stop = Markup.Item.StopLine,

            [Description(nameof(Localize.LineStyle_CrosswalkLinesGroup))]
            Crosswalk = Markup.Item.Crosswalk,


            [NotVisible]
            All = Regular | Stop | Crosswalk,
        }
        public override string ToString() => PointPair.ToString();
    }
    public class MarkupRegularLine : MarkupLine
    {
        public override LineType Type => LineType.Regular;
        public override Alignment Alignment => RawAlignment;
        public PropertyEnumValue<Alignment> RawAlignment { get; private set; }

        public override bool IsSupportRules => true;
        private List<MarkupLineRawRule<RegularLineStyle>> RawRules { get; } = new List<MarkupLineRawRule<RegularLineStyle>>();
        public override IEnumerable<MarkupLineRawRule> Rules => RawRules.Cast<MarkupLineRawRule>();

        public MarkupRegularLine(Markup markup, MarkupPointPair pointPair, RegularLineStyle style = null, bool update = true) : base(markup, pointPair, false)
        {
            RawAlignment = new PropertyEnumValue<Alignment>("A", AlignmentChanged, Alignment.Centre);

            if (update)
                Update(true);

            if (style != null)
            {
                AddRule(style, false, false);
                RecalculateStyleData();
            }
        }

        protected override ITrajectory CalculateTrajectory()
        {
            var trajectory = new Bezier3
            {
                a = PointPair.First.GetPosition(RawAlignment),
                d = PointPair.Second.GetPosition(RawAlignment.Value.Invert()),
            };
            NetSegment.CalculateMiddlePoints(trajectory.a, PointPair.First.Direction, trajectory.d, PointPair.Second.Direction, true, true, out trajectory.b, out trajectory.c);

            return new BezierTrajectory(trajectory);
        }
        private void AlignmentChanged() => Markup.Update(this, true, true);

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
            var from = empty ? null : GetDefaultEdge(Start);
            var to = empty ? null : GetDefaultEdge(End);
            return new MarkupLineRawRule<RegularLineStyle>(this, lineStyle, from, to);
        }
        private ILinePartEdge GetDefaultEdge(MarkupPoint point)
        {
            if (!Settings.CutLineByCrosswalk || point.Type == MarkupPoint.PointType.Normal)
                return new EnterPointEdge(point);

            var intersects = Markup.GetIntersects(this).Where(i => i.IsIntersect && i.Pair.GetOther(this) is MarkupCrosswalkLine line && line.PointPair.ContainsEnter(point.Enter)).ToArray();
            if (!intersects.Any())
                return new EnterPointEdge(point);

            var intersect = intersects.Aggregate((i, j) => point == End ^ (i.FirstT > i.SecondT) ? i : j);
            return new LinesIntersectEdge(intersect.Pair);
        }

        public MarkupLineRawRule<RegularLineStyle> AddRule(bool empty = true, bool update = true)
            => AddRule(TemplateManager.StyleManager.GetDefault<RegularLineStyle>(Style.StyleType.LineDashed), empty, update);
        public void RemoveRule(MarkupLineRawRule<RegularLineStyle> rule)
        {
            RawRules.Remove(rule);
            RuleChanged();
        }
        public bool RemoveRules(MarkupLine intersectLine)
        {
            if (!RawRules.Any())
                return false;

            var removed = RawRules.RemoveAll(r => Match(intersectLine, r.From) || Match(intersectLine, r.To));

            if (!RawRules.Any())
                AddRule(false, false);

            return removed != 0;
        }
        private bool Match(MarkupLine intersectLine, ISupportPoint supportPoint) => supportPoint is IntersectSupportPoint lineRuleEdge && lineRuleEdge.LinePair.ContainLine(intersectLine);
        public int GetLineDependences(MarkupLine intersectLine) => RawRules.Count(r => Match(intersectLine, r.From) || Match(intersectLine, r.To));
        public override bool ContainsRule(MarkupLineRawRule rule) => rule != null && RawRules.Any(r => r == rule);

        protected override IEnumerable<IStyleData> GetStyleData(MarkupLOD lod)
        {
            var rules = MarkupLineRawRule<RegularLineStyle>.GetRules(RawRules);

            foreach (var rule in rules)
            {
                var trajectoryPart = LineTrajectory.Cut(rule.Start, rule.End);
                yield return rule.LineStyle.Calculate(this, trajectoryPart, lod);
            }
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

            RawAlignment.ToXml(config);
            foreach (var rule in RawRules)
            {
                var ruleConfig = rule.ToXml();
                config.Add(ruleConfig);
            }

            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            RawAlignment.FromXml(config);
            foreach (var ruleConfig in config.Elements(MarkupLineRawRule<RegularLineStyle>.XmlName))
            {
                if (MarkupLineRawRule<RegularLineStyle>.FromXml(ruleConfig, this, map, invert, out MarkupLineRawRule<RegularLineStyle> rule))
                    AddRule(rule, false);
            }
        }
    }
    public class MarkupNormalLine : MarkupRegularLine
    {
        public MarkupNormalLine(Markup markup, MarkupPointPair pointPair, RegularLineStyle style = null) : base(markup, pointPair, style) { }
        protected override ITrajectory CalculateTrajectory() => new StraightTrajectory(Start.GetPosition(RawAlignment), End.GetPosition(RawAlignment.Value.Invert()));
    }
    public class MarkupCrosswalkLine : MarkupRegularLine
    {
        public override LineType Type => LineType.Crosswalk;
        public MarkupCrosswalk Crosswalk { get; set; }
        public Func<StraightTrajectory> TrajectoryGetter { get; set; }

        public MarkupCrosswalkLine(Markup markup, MarkupPointPair pointPair, CrosswalkStyle style = null) : base(markup, pointPair, null, false)
        {
            if (style == null)
                style = TemplateManager.StyleManager.GetDefault<CrosswalkStyle>(Style.StyleType.CrosswalkExistent);

            Crosswalk = new MarkupCrosswalk(Markup, this, style);
            Update(true);
            Markup.AddCrosswalk(Crosswalk);
        }
        protected override MarkupLineRawRule<RegularLineStyle> GetDefaultRule(RegularLineStyle lineStyle, bool empty = true)
        {
            var from = empty ? null : new CrosswalkBorderEdge(this, BorderPosition.Right);
            var to = empty ? null : new CrosswalkBorderEdge(this, BorderPosition.Left);
            return new MarkupLineRawRule<RegularLineStyle>(this, lineStyle, from, to);
        }

        protected override ITrajectory CalculateTrajectory() => TrajectoryGetter();
        public float GetT(BorderPosition border) => (int)border;
        public override IEnumerable<ILinePartEdge> RulesEdges
        {
            get
            {
                yield return new CrosswalkBorderEdge(this, BorderPosition.Left);
                yield return new CrosswalkBorderEdge(this, BorderPosition.Right);

                var lines = IntersectLines.ToDictionary(i => i.PointPair, i => i);

                if (Crosswalk.LeftBorder.Value is MarkupRegularLine leftBorder)
                    lines[leftBorder.PointPair] = leftBorder;

                if (Crosswalk.RightBorder.Value is MarkupRegularLine rightBorder)
                    lines[rightBorder.PointPair] = rightBorder;

                foreach (var line in lines.Values)
                    yield return new LinesIntersectEdge(this, line);
            }
        }
    }

    public abstract class MarkupEnterLine<Style, StyleType> : MarkupLine
    where Style : LineStyle
    where StyleType : Enum
    {
        public virtual Alignment StartAlignment { get; private set; } = Alignment.Centre;
        public virtual Alignment EndAlignment { get; private set; } = Alignment.Centre;

        protected abstract bool Visible { get; }
        public MarkupLineRawRule<Style> Rule { get; set; }
        public override IEnumerable<MarkupLineRawRule> Rules { get { yield return Rule; } }

        protected MarkupEnterLine(Markup markup, MarkupPoint first, MarkupPoint second, Style style, Alignment firstAlignment, Alignment secondAlignment) : this(markup, new MarkupPointPair(first, second), style, firstAlignment, secondAlignment)
        {
            if (IsStart(first))
                Update(firstAlignment, secondAlignment);
            else
                Update(secondAlignment, firstAlignment);
        }
        protected MarkupEnterLine(Markup markup, MarkupPointPair pointPair, Style style, Alignment firstAlignment, Alignment secondAlignment) : base(markup, pointPair, false)
        {
            if (Visible)
            {
                var rule = new MarkupLineRawRule<Style>(this, style, new EnterPointEdge(Start), new EnterPointEdge(End));
                SetRule(rule);
            }

            StartAlignment = firstAlignment;
            EndAlignment = secondAlignment;

            Update(true);
            if (Visible)
                RecalculateStyleData();
        }

        protected override ITrajectory CalculateTrajectory() => new StraightTrajectory(PointPair.First.Position, PointPair.Second.Position);
        public void Update(Alignment startAlignment, Alignment endAlignment, bool onlySelfUpdate = false)
        {
            StartAlignment = startAlignment;
            EndAlignment = endAlignment;

            Update(onlySelfUpdate);
        }

        protected override IEnumerable<IStyleData> GetStyleData(MarkupLOD lod)
        {
            if (Visible)
                yield return Rule.Style.Calculate(this, LineTrajectory, lod);
        }
        private void SetRule(MarkupLineRawRule<Style> rule)
        {
            rule.OnRuleChanged = RuleChanged;
            Rule = rule;
        }
        public override bool ContainsRule(MarkupLineRawRule rule) => rule != null && rule == Rule;

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(Rule.ToXml());
            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            if (config.Element(MarkupLineRawRule<Style>.XmlName) is XElement ruleConfig && MarkupLineRawRule<Style>.FromXml(ruleConfig, this, map, invert, out MarkupLineRawRule<Style> rule))
                SetRule(rule);
        }
    }
    public class MarkupStopLine : MarkupEnterLine<StopLineStyle, StopLineStyle.StopLineType>
    {
        protected override bool Visible => true;
        public override LineType Type => LineType.Stop;

        public MarkupStopLine(Markup markup, MarkupPointPair pointPair, StopLineStyle style = null, Alignment firstAlignment = Alignment.Centre, Alignment secondAlignment = Alignment.Centre) : base(markup, pointPair, style ?? TemplateManager.StyleManager.GetDefault<StopLineStyle>(Style.StyleType.StopLineSolid), firstAlignment, secondAlignment) { }

        public override IEnumerable<ILinePartEdge> RulesEdges => RulesEnterPointEdge;
    }
    public class MarkupEnterLine : MarkupEnterLine<LineStyle, RegularLineStyle.RegularLineType>
    {
        protected override bool Visible => false;
        public override LineType Type => throw new NotImplementedException();
        public override IEnumerable<ILinePartEdge> RulesEdges => throw new NotImplementedException();
        public MarkupEnterLine(Markup markup, MarkupPoint first, MarkupPoint second, Alignment firstAlignment = Alignment.Centre, Alignment secondAlignment = Alignment.Centre) : base(markup, first, second, null, firstAlignment, secondAlignment) { }
        protected override ITrajectory CalculateTrajectory() => new StraightTrajectory(Start.GetPosition(StartAlignment), End.GetPosition(EndAlignment));
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
        public bool? MustIntersect
        {
            get
            {
                if (IsSelf || First.IsStopLine || Second.IsStopLine)
                    return false;

                if (First.IsCrosswalk && Second.IsCrosswalk && First.Start.Enter == Second.Start.Enter)
                    return false;

                if (First.ContainsPoint(Second.Start) || First.ContainsPoint(Second.End))
                    return false;

                if (IsBorder(First, Second) || IsBorder(Second, First))
                    return true;

                return null;

                static bool IsBorder(MarkupLine line1, MarkupLine line2) => line1 is MarkupCrosswalkLine crosswalkLine && crosswalkLine.Crosswalk.IsBorder(line2);
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
            if (ContainLine(line))
                return line == First ? Second : First;
            else
                return null;
        }
        public MarkupLine GetLine(MarkupPoint point)
        {
            if (First.ContainsPoint(point))
                return First;
            else if (Second.ContainsPoint(point))
                return Second;
            else
                return null;
        }

        public override string ToString() => $"{First} × {Second}";

        public class MarkupLinePairComparer : IEqualityComparer<MarkupLinePair>
        {
            public bool Equals(MarkupLinePair x, MarkupLinePair y) => (x.First == y.First && x.Second == y.Second) || (x.First == y.Second && x.Second == y.First);
            public int GetHashCode(MarkupLinePair pair) => pair.GetHashCode();
        }
    }
    public class LineBorders : IEnumerable<ITrajectory>
    {
        public Vector3 Center { get; }
        public List<ITrajectory> Borders { get; }
        public bool IsEmpty => !Borders.Any();
        public LineBorders(MarkupLine line)
        {
            Center = line.Markup.Position;
            Borders = GetBorders(line).ToList();
        }
        public IEnumerable<ITrajectory> GetBorders(MarkupLine line)
        {
            if (line.Start.GetBorder(out ITrajectory startTrajectory))
                yield return startTrajectory;
            if (line.End.GetBorder(out ITrajectory endTrajectory))
                yield return endTrajectory;
        }

        public IEnumerator<ITrajectory> GetEnumerator() => Borders.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public StraightTrajectory[] GetVertex(MarkupStylePart dash)
        {
            var dirX = dash.Angle.Direction();
            var dirY = dirX.Turn90(true);

            dirX *= (dash.Length / 2);
            dirY *= (dash.Width / 2);

            return new StraightTrajectory[]
            {
                new StraightTrajectory(Center, dash.Position + dirX + dirY),
                new StraightTrajectory(Center, dash.Position - dirX + dirY),
                new StraightTrajectory(Center, dash.Position + dirX - dirY),
                new StraightTrajectory(Center, dash.Position - dirX - dirY),
            };
        }
    }
    public enum Alignment
    {
        [Description(nameof(Localize.StyleOption_AlignmentLeft))]
        Left,

        [Description(nameof(Localize.StyleOption_AlignmentCenter))]
        Centre,

        [Description(nameof(Localize.StyleOption_AlignmentRight))]
        Right
    }
}
