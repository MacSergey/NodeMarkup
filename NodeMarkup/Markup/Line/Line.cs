using ColossalFramework.Math;
using ModsCommon.Utilities;
using NodeMarkup.Tools;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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

        public bool HasOverlapped => Rules.Any(r => r.IsOverlapped);

        public abstract IEnumerable<MarkupLineRawRule> Rules { get; }
        public abstract IEnumerable<ILinePartEdge> RulesEdges { get; }

        protected ITrajectory LineTrajectory { get; private set; }
        public ITrajectory Trajectory => LineTrajectory.Copy();
        public LodDictionaryArray<IStyleData> StyleData { get;} = new LodDictionaryArray<IStyleData>();

        public LineBorders Borders => new LineBorders(this);

        public string XmlSection => XmlName;

        protected MarkupLine(Markup markup, MarkupPointPair pointPair, bool update = true)
        {
            Markup = markup;
            PointPair = pointPair;

            if (update)
                Update(true);
        }
        protected MarkupLine(NodeMarkup markup, MarkupPoint first, MarkupPoint second, bool update = true) : this(markup, new MarkupPointPair(first, second), update) { }
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
        public virtual void Render(RenderManager.CameraInfo cameraInfo, Color? color = null, float? width = null, bool? alphaBlend = null, bool? cut = null) 
            => Trajectory.Render(cameraInfo, color, width, alphaBlend, cut);
        public abstract bool ContainsRule(MarkupLineRawRule rule);
        public bool ContainsEnter(Enter enter) => PointPair.ContainsEnter(enter);

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
                    if (regularStyle == RegularLineStyle.RegularLineType.Empty)
                        return pointPair.IsNormal ? new MarkupNormalLine(markup, pointPair) : new MarkupRegularLine(markup, pointPair);
                    else
                        return pointPair.IsNormal ? new MarkupNormalLine(markup, pointPair, regularStyle) : new MarkupRegularLine(markup, pointPair, regularStyle);
            }
        }
        public Dependences GetDependences() => Markup.GetLineDependences(this);

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
    public abstract class MarkupStraightLine<Style, StyleType> : MarkupLine
        where Style : LineStyle
        where StyleType : Enum
    {
        protected abstract bool Visible { get; }
        public MarkupLineRawRule<Style> Rule { get; set; }
        public override IEnumerable<MarkupLineRawRule> Rules
        {
            get
            {
                yield return Rule;
            }
        }

        protected MarkupStraightLine(Markup markup, MarkupPoint first, MarkupPoint second, StyleType styleType) : this(markup, new MarkupPointPair(first, second), styleType) { }
        protected MarkupStraightLine(Markup markup, MarkupPointPair pointPair, StyleType styleType) : base(markup, pointPair, false)
        {
            if (Visible)
            {
                var style = GetDefaultStyle(styleType);
                var rule = new MarkupLineRawRule<Style>(this, style, new EnterPointEdge(Start), new EnterPointEdge(End));
                SetRule(rule);
            }
            Update(true);
            if (Visible)
                RecalculateStyleData();
        }

        protected override ITrajectory CalculateTrajectory() => new StraightTrajectory(PointPair.First.Position, PointPair.Second.Position);

        protected override IEnumerable<IStyleData> GetStyleData(MarkupLOD lod)
        {
            yield return Rule.Style.Calculate(this, LineTrajectory, lod);
        }
        private void SetRule(MarkupLineRawRule<Style> rule)
        {
            rule.OnRuleChanged = RuleChanged;
            Rule = rule;
        }
        protected abstract Style GetDefaultStyle(StyleType styleType);
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
    public class MarkupRegularLine : MarkupLine
    {
        public override LineType Type => LineType.Regular;

        public override bool IsSupportRules => true;
        private List<MarkupLineRawRule<RegularLineStyle>> RawRules { get; } = new List<MarkupLineRawRule<RegularLineStyle>>();
        public override IEnumerable<MarkupLineRawRule> Rules => RawRules.Cast<MarkupLineRawRule>();

        public MarkupRegularLine(Markup markup, MarkupPointPair pointPair) : base(markup, pointPair) 
        {
            RecalculateStyleData();
        }
        protected MarkupRegularLine(Markup markup, MarkupPointPair pointPair, bool update = true) : base(markup, pointPair, update) 
        {
            RecalculateStyleData();
        }
        public MarkupRegularLine(Markup markup, MarkupPointPair pointPair, RegularLineStyle.RegularLineType lineType) :
            base(markup, pointPair)
        {
            var lineStyle = TemplateManager.StyleManager.GetDefault<RegularLineStyle>((Style.StyleType)(int)lineType);
            AddRule(lineStyle, false, false);
            RecalculateStyleData();
        }
        protected override ITrajectory CalculateTrajectory()
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

            foreach (var rule in RawRules)
            {
                var ruleConfig = rule.ToXml();
                config.Add(ruleConfig);
            }

            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            foreach (var ruleConfig in config.Elements(MarkupLineRawRule<RegularLineStyle>.XmlName))
            {
                if (MarkupLineRawRule<RegularLineStyle>.FromXml(ruleConfig, this, map, invert, out MarkupLineRawRule<RegularLineStyle> rule))
                    AddRule(rule, false);
            }
        }
    }
    public class MarkupNormalLine : MarkupRegularLine
    {
        public MarkupNormalLine(Markup markup, MarkupPointPair pointPair) : base(markup, pointPair) { }
        public MarkupNormalLine(Markup markup, MarkupPointPair pointPair, RegularLineStyle.RegularLineType lineType) : base(markup, pointPair, lineType) { }

        protected override ITrajectory CalculateTrajectory() => new StraightTrajectory(PointPair.First.Position, PointPair.Second.Position);
    }
    public class MarkupCrosswalkLine : MarkupRegularLine
    {
        public override LineType Type => LineType.Crosswalk;
        public MarkupCrosswalk Crosswalk { get; set; }
        public Func<StraightTrajectory> TrajectoryGetter { get; set; }

        public MarkupCrosswalkLine(Markup markup, MarkupPointPair pointPair, CrosswalkStyle.CrosswalkType crosswalkType = CrosswalkStyle.CrosswalkType.Existent) : base(markup, pointPair, false)
        {
            Crosswalk = new MarkupCrosswalk(Markup, this, crosswalkType);
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

                if (Crosswalk.LeftBorder is MarkupRegularLine leftBorder)
                    lines[leftBorder.PointPair] = leftBorder;

                if (Crosswalk.RightBorder is MarkupRegularLine rightBorder)
                    lines[rightBorder.PointPair] = rightBorder;

                foreach (var line in lines.Values)
                    yield return new LinesIntersectEdge(this, line);
            }
        }
    }
    public class MarkupStopLine : MarkupStraightLine<StopLineStyle, StopLineStyle.StopLineType>
    {
        protected override bool Visible => true;
        public override LineType Type => LineType.Stop;

        public MarkupStopLine(Markup markup, MarkupPointPair pointPair, StopLineStyle.StopLineType lineType = StopLineStyle.StopLineType.Solid) : base(markup, pointPair, lineType) { }

        protected override StopLineStyle GetDefaultStyle(StopLineStyle.StopLineType lineType)
            => TemplateManager.StyleManager.GetDefault<StopLineStyle>((Style.StyleType)(int)lineType);

        public override IEnumerable<ILinePartEdge> RulesEdges
        {
            get
            {
                foreach (var edge in RulesEnterPointEdge)
                    yield return edge;
            }
        }
    }
    public class MarkupEnterLine : MarkupStraightLine<LineStyle, RegularLineStyle.RegularLineType>
    {
        protected override bool Visible => false;
        public override LineType Type => throw new NotImplementedException();
        public override IEnumerable<ILinePartEdge> RulesEdges => throw new NotImplementedException();
        public MarkupEnterLine(Markup markup, MarkupPoint first, MarkupPoint second) : base(markup, first, second, RegularLineStyle.RegularLineType.Dashed) { }
        protected override LineStyle GetDefaultStyle(RegularLineStyle.RegularLineType styleType) => throw new NotImplementedException();
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
}
