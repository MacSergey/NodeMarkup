using ColossalFramework.Math;
using ModsCommon;
using ModsCommon.Utilities;
using NodeMarkup.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using static ColossalFramework.IO.EncodedArray;
using static RenderManager;

namespace NodeMarkup.Manager
{
    public abstract class MarkupLine : IStyleItem, IToXml, ISupport
    {
        public static string XmlName { get; } = "L";

        public string DeleteCaptionDescription => Localize.LineEditor_DeleteCaptionDescription;
        public string DeleteMessageDescription => Localize.LineEditor_DeleteMessageDescription;
        public Markup.SupportType Support => Markup.SupportType.Lines;

        public abstract LineType Type { get; }

        public Markup Markup { get; private set; }
        public ulong Id => PointPair.Hash;

        public MarkupPointPair PointPair { get; }
        public MarkupPoint Start => PointPair.First;
        public MarkupPoint End => PointPair.Second;
        public virtual bool IsSupportRules => false;
        public bool IsEnterLine => PointPair.IsSameEnter;
        public bool IsSame => PointPair.IsSame;
        public bool IsNormal => PointPair.IsNormal;
        public bool IsStopLine => PointPair.IsStopLine;
        public bool IsCrosswalk => PointPair.IsCrosswalk;
        public virtual Alignment Alignment => Alignment.Centre;

        public bool HasOverlapped => Rules.Any(r => r.IsOverlapped);

        public abstract IEnumerable<MarkupLineRawRule> Rules { get; }
        public abstract IEnumerable<ILinePartEdge> RulesEdges { get; }

        public ITrajectory Trajectory { get; private set; }
        public List<IStyleData> StyleData { get; } = new List<IStyleData>();

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
            Trajectory = CalculateTrajectory();
            if (!onlySelfUpdate)
                Markup.Update(this);
        }
        protected abstract ITrajectory CalculateTrajectory();

        public void RecalculateStyleData()
        {
#if DEBUG_RECALCULATE
            Mod.Logger.Debug($"Recalculate line {this}");
#endif
            StyleData.Clear();
            foreach (var lod in EnumExtension.GetEnumValues<MarkupLOD>())
            {
                StyleData.AddRange(GetStyleData(lod));
            }
        }

        protected abstract IEnumerable<IStyleData> GetStyleData(MarkupLOD lod);

        public bool ContainsPoint(MarkupPoint point) => PointPair.ContainsPoint(point);

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
        public virtual void RenderRule(MarkupLineRawRule rule, OverlayData data)
        {
            if (rule.GetTrajectory(out var trajectory))
                trajectory.Render(data);
        }
        public abstract bool ContainsRule(MarkupLineRawRule rule);
        public bool ContainsEnter(Enter enter) => PointPair.ContainsEnter(enter);

        public Dependences GetDependences() => Markup.GetLineDependences(this);
        public bool IsStart(MarkupPoint point) => Start == point;
        public bool IsEnd(MarkupPoint point) => End == point;
        public Alignment GetAlignment(MarkupPoint point) => PointPair.ContainsPoint(point) && point.IsSplit ? (IsStart(point) ? Alignment : Alignment.Invert()) : Alignment.Centre;


        public virtual XElement ToXml()
        {
            var config = new XElement(XmlSection);
            config.AddAttr(nameof(Id), Id);
            config.AddAttr("T", (int)Type);

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
                    case LineType.Lane:
                        line = new MarkupLaneLine(markup, pointPair);
                        break;
                    default:
                        return false;
                }
            }

            return true;
        }
        public abstract void FromXml(XElement config, ObjectsMap map, bool invert);

        public override string ToString() => PointPair.ToString();
    }
    public class MarkupRegularLine : MarkupLine
    {
        public override LineType Type => LineType.Regular;
        public override Alignment Alignment => RawAlignment;
        public PropertyEnumValue<Alignment> RawAlignment { get; private set; }
        public PropertyBoolValue ClipSidewalk { get; private set; }

        public override bool IsSupportRules => true;
        private List<MarkupLineRawRule<RegularLineStyle>> RawRules { get; } = new List<MarkupLineRawRule<RegularLineStyle>>();
        public override IEnumerable<MarkupLineRawRule> Rules => RawRules.Cast<MarkupLineRawRule>();

        public LineBorders Borders => new LineBorders(this);
        private bool DefaultClipSidewalk => Markup.Type == MarkupType.Node && PointPair.IsSideLine && PointPair.NetworkType == NetworkType.Road;

        public MarkupRegularLine(Markup markup, MarkupPoint first, MarkupPoint second, RegularLineStyle style = null, Alignment alignment = Alignment.Centre, bool update = true) : this(markup, MarkupPointPair.FromPoints(first, second, out bool invert), style, !invert ? alignment : alignment.Invert(), update) { }
        public MarkupRegularLine(Markup markup, MarkupPointPair pointPair, RegularLineStyle style = null, Alignment alignment = Alignment.Centre, bool update = true) : base(markup, pointPair, false)
        {
            RawAlignment = new PropertyEnumValue<Alignment>("A", AlignmentChanged, alignment);
            ClipSidewalk = new PropertyBoolValue("CS", ClipSidewalkChanged, DefaultClipSidewalk);

            if (update)
                Update(true);

            if (style != null)
            {
                AddRule(style, false, false);
                Markup.RecalculateStyleData(this);
            }
        }

        protected override ITrajectory CalculateTrajectory()
        {
            var trajectory = new Bezier3
            {
                a = PointPair.First.GetAbsolutePosition(RawAlignment),
                d = PointPair.Second.GetAbsolutePosition(RawAlignment.Value.Invert()),
            };
            NetSegment.CalculateMiddlePoints(trajectory.a, PointPair.First.Direction, trajectory.d, PointPair.Second.Direction, true, true, out trajectory.b, out trajectory.c);

            return new BezierTrajectory(trajectory);
        }
        private void AlignmentChanged() => Markup.Update(this, true, true);
        private void ClipSidewalkChanged() => Markup.Update(this, true, false);

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
        {
            var defaultStyle = Style.StyleType.LineDashed;

            if ((defaultStyle.GetNetworkType() & PointPair.NetworkType) == 0 || (defaultStyle.GetLineType() & Type) == 0)
            {
                foreach (var style in EnumExtension.GetEnumValues<RegularLineStyle.RegularLineType>(i => true).Select(i => i.ToEnum<Style.StyleType, RegularLineStyle.RegularLineType>()))
                {
                    if ((style.GetNetworkType() & PointPair.NetworkType) != 0 && (style.GetLineType() & Type) != 0)
                    {
                        defaultStyle = style;
                        break;
                    }
                }
            }

            return AddRule(SingletonManager<StyleTemplateManager>.Instance.GetDefault<RegularLineStyle>(defaultStyle), empty, update);
        }
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
                var trajectoryPart = Trajectory.Cut(rule.Start, rule.End);
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
            ClipSidewalk.ToXml(config);
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
            ClipSidewalk.FromXml(config, DefaultClipSidewalk);
            foreach (var ruleConfig in config.Elements(MarkupLineRawRule<RegularLineStyle>.XmlName))
            {
                if (MarkupLineRawRule<RegularLineStyle>.FromXml(ruleConfig, this, map, invert, out MarkupLineRawRule<RegularLineStyle> rule))
                    AddRule(rule, false);
            }
        }
    }
    public class MarkupNormalLine : MarkupRegularLine
    {
        public MarkupNormalLine(Markup markup, MarkupPointPair pointPair, RegularLineStyle style = null, Alignment alignment = Alignment.Centre) : base(markup, pointPair, style, alignment) { }
        protected override ITrajectory CalculateTrajectory() => new StraightTrajectory(Start.GetAbsolutePosition(RawAlignment), End.GetAbsolutePosition(RawAlignment.Value.Invert()));
    }
    public class MarkupFillerTempLine : MarkupRegularLine
    {
        public MarkupFillerTempLine(Markup markup, MarkupPoint first, MarkupPoint second, Alignment alignment) : base(markup, first, second, null, alignment) { }
        public MarkupFillerTempLine(Markup markup, MarkupPointPair pair, Alignment alignment) : base(markup, pair, null, alignment) { }
    }
    public class MarkupCrosswalkLine : MarkupRegularLine
    {
        public override LineType Type => LineType.Crosswalk;
        public MarkupCrosswalk Crosswalk { get; set; }
        public Func<StraightTrajectory> TrajectoryGetter { get; set; }

        public MarkupCrosswalkLine(Markup markup, MarkupPointPair pointPair, CrosswalkStyle style = null) : base(markup, pointPair, update: false)
        {
            if (style == null)
                style = SingletonManager<StyleTemplateManager>.Instance.GetDefault<CrosswalkStyle>(Style.StyleType.CrosswalkExistent);

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
    public class MarkupLaneLine : MarkupRegularLine
    {
        public override LineType Type => LineType.Lane;

        public MarkupLaneLine(Markup markup, MarkupPointPair pointPair, RegularLineStyle style = null) : base(markup, pointPair, style) { }

        public override void Render(OverlayData data)
        {
            var lanePointS = PointPair.First as MarkupLanePoint;
            var lanePointE = PointPair.Second as MarkupLanePoint;

            ITrajectory[] trajectories;
            if (lanePointS != null && lanePointE != null)
            {
                lanePointS.Source.GetPoints(out var leftPointS, out var rightPointS);
                lanePointE.Source.GetPoints(out var leftPointE, out var rightPointE);
                trajectories = new ITrajectory[]
                {
                    new BezierTrajectory(leftPointS.Position, leftPointS.Direction, rightPointE.Position, rightPointE.Direction),
                    new StraightTrajectory(rightPointE.Position, leftPointE.Position),
                    new BezierTrajectory(leftPointE.Position, leftPointE.Direction, rightPointS.Position, rightPointS.Direction),
                    new StraightTrajectory(rightPointS.Position, leftPointS.Position),
                };
            }
            else if (lanePointS != null)
            {
                lanePointS.Source.GetPoints(out var leftPointS, out var rightPointS);
                trajectories = new ITrajectory[]
                {
                    new BezierTrajectory(leftPointS.Position, leftPointS.Direction, PointPair.Second.Position, PointPair.Second.Direction),
                    new BezierTrajectory(PointPair.Second.Position, PointPair.Second.Direction, rightPointS.Position, rightPointS.Direction),
                    new StraightTrajectory(rightPointS.Position, leftPointS.Position),
                };
            }
            else if (lanePointE != null)
            {
                lanePointE.Source.GetPoints(out var leftPointE, out var rightPointE);
                trajectories = new ITrajectory[]
                {
                    new BezierTrajectory(PointPair.First.Position, PointPair.First.Direction, rightPointE.Position, rightPointE.Direction),
                    new StraightTrajectory(rightPointE.Position, leftPointE.Position),
                    new BezierTrajectory(leftPointE.Position, leftPointE.Direction, PointPair.First.Position, PointPair.First.Direction),
                };
            }
            else
                return;

            data.AlphaBlend = false;
            var triangles = Triangulator.TriangulateSimple(trajectories, out var points, minAngle: 5, maxLength: 10f);
            points.RenderArea(triangles, data);
        }
        public override void RenderRule(MarkupLineRawRule rule, OverlayData data)
        {
            if (!rule.GetT(out var fromT, out var toT) || fromT == toT)
                return;

            var lanePointS = PointPair.First as MarkupLanePoint;
            var lanePointE = PointPair.Second as MarkupLanePoint;

            ITrajectory[] trajectories;
            if (lanePointS != null && lanePointE != null)
            {
                lanePointS.Source.GetPoints(out var leftPointS, out var rightPointS);
                lanePointE.Source.GetPoints(out var leftPointE, out var rightPointE);

                trajectories = new ITrajectory[4];
                trajectories[0] = new BezierTrajectory(leftPointS.Position, leftPointS.Direction, rightPointE.Position, rightPointE.Direction).Cut(fromT, toT);
                trajectories[2] = new BezierTrajectory(leftPointE.Position, leftPointE.Direction, rightPointS.Position, rightPointS.Direction).Cut(1f - toT, 1f - fromT);
                trajectories[1] = new StraightTrajectory(trajectories[0].EndPosition, trajectories[2].StartPosition);
                trajectories[3] = new StraightTrajectory(trajectories[2].EndPosition, trajectories[0].StartPosition);
            }
            //else if (lanePointA != null)
            //{
            //    lanePointA.Source.GetPoints(out var leftPointA, out var rightPointA);
            //    trajectories = new List<ITrajectory>()
            //    {
            //        new BezierTrajectory(leftPointA.Position, leftPointA.Direction, PointPair.Second.Position, PointPair.Second.Direction),
            //        new BezierTrajectory(PointPair.Second.Position, PointPair.Second.Direction, rightPointA.Position, rightPointA.Direction),
            //        new StraightTrajectory(rightPointA.Position, leftPointA.Position),
            //    };
            //}
            //else if (lanePointB != null)
            //{
            //    lanePointB.Source.GetPoints(out var leftPointB, out var rightPointB);
            //    trajectories = new List<ITrajectory>()
            //    {
            //        new BezierTrajectory(PointPair.First.Position, PointPair.First.Direction, rightPointB.Position, rightPointB.Direction),
            //        new StraightTrajectory(rightPointB.Position, leftPointB.Position),
            //        new BezierTrajectory(leftPointB.Position, leftPointB.Direction, PointPair.First.Position, PointPair.First.Direction),
            //    };
            //}
            else
                return;

            data.AlphaBlend = false;
            var triangles = Triangulator.TriangulateSimple(trajectories, out var points, minAngle: 5, maxLength: 10f);
            points.RenderArea(triangles, data);
        }
    }

    public class MarkupStopLine : MarkupLine
    {
        public override LineType Type => LineType.Stop;

        public MarkupLineRawRule<StopLineStyle> Rule { get; set; }
        public override IEnumerable<MarkupLineRawRule> Rules { get { yield return Rule; } }
        public override IEnumerable<ILinePartEdge> RulesEdges => RulesEnterPointEdge;

        public PropertyEnumValue<Alignment> RawStartAlignment { get; private set; }
        public PropertyEnumValue<Alignment> RawEndAlignment { get; private set; }

        public MarkupStopLine(Markup markup, MarkupPointPair pointPair, StopLineStyle style = null, Alignment firstAlignment = Alignment.Centre, Alignment secondAlignment = Alignment.Centre) : base(markup, pointPair, false)
        {
            RawStartAlignment = new PropertyEnumValue<Alignment>("AL", AlignmentChanged, firstAlignment);
            RawEndAlignment = new PropertyEnumValue<Alignment>("AR", AlignmentChanged, secondAlignment);

            style ??= SingletonManager<StyleTemplateManager>.Instance.GetDefault<StopLineStyle>(Style.StyleType.StopLineSolid);
            var rule = new MarkupLineRawRule<StopLineStyle>(this, style, new EnterPointEdge(Start), new EnterPointEdge(End));
            SetRule(rule);

            Update(true);
            Markup.RecalculateStyleData(this);
        }

        private void AlignmentChanged() => Markup.Update(this, true, true);
        protected override ITrajectory CalculateTrajectory() => new StraightTrajectory(PointPair.First.GetAbsolutePosition(RawStartAlignment), PointPair.Second.GetAbsolutePosition(RawEndAlignment));
        protected void SetRule(MarkupLineRawRule<StopLineStyle> rule)
        {
            rule.OnRuleChanged = RuleChanged;
            Rule = rule;
        }
        public override bool ContainsRule(MarkupLineRawRule rule) => rule != null && rule == Rule;
        protected override IEnumerable<IStyleData> GetStyleData(MarkupLOD lod)
        {
            yield return Rule.Style.Calculate(this, Trajectory, lod);
        }

        public override XElement ToXml()
        {
            var config = base.ToXml();

            config.Add(Rule.ToXml());
            RawStartAlignment.ToXml(config);
            RawEndAlignment.ToXml(config);

            return config;
        }
        public override void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            if (config.Element(MarkupLineRawRule<StopLineStyle>.XmlName) is XElement ruleConfig && MarkupLineRawRule<StopLineStyle>.FromXml(ruleConfig, this, map, invert, out MarkupLineRawRule<StopLineStyle> rule))
                SetRule(rule);

            RawStartAlignment.FromXml(config);
            RawEndAlignment.FromXml(config);
        }
    }
    public class MarkupEnterLine : MarkupLine
    {
        public override LineType Type => throw new NotImplementedException();
        public override IEnumerable<ILinePartEdge> RulesEdges => throw new NotImplementedException();
        public override IEnumerable<MarkupLineRawRule> Rules { get { yield break; } }

        public virtual Alignment StartAlignment { get; private set; } = Alignment.Centre;
        public virtual Alignment EndAlignment { get; private set; } = Alignment.Centre;

        public bool IsDot => IsSame && StartAlignment == EndAlignment;


        public MarkupEnterLine(Markup markup, MarkupPoint first, MarkupPoint second, Alignment firstAlignment = Alignment.Centre, Alignment secondAlignment = Alignment.Centre) : base(markup, MarkupPointPair.FromPoints(first, second, out bool invert), false)
        {
            StartAlignment = !invert ? firstAlignment : secondAlignment;
            EndAlignment = !invert ? secondAlignment : firstAlignment;

            Update(true);
        }

        public void Update(Alignment startAlignment, Alignment endAlignment, bool onlySelfUpdate = false)
        {
            StartAlignment = startAlignment;
            EndAlignment = endAlignment;

            Update(onlySelfUpdate);
        }

        protected override ITrajectory CalculateTrajectory() => new StraightTrajectory(Start.GetAbsolutePosition(StartAlignment), End.GetAbsolutePosition(EndAlignment));
        public override bool ContainsRule(MarkupLineRawRule rule) => false;
        protected override IEnumerable<IStyleData> GetStyleData(MarkupLOD lod) { yield break; }

        public override void FromXml(XElement config, ObjectsMap map, bool invert) { }
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

                if (First.Start == Second.Start && First.GetAlignment(First.Start) == Second.GetAlignment(Second.Start))
                    return false;
                if (First.Start == Second.End && First.GetAlignment(First.Start) == Second.GetAlignment(Second.End))
                    return false;
                if (First.End == Second.Start && First.GetAlignment(First.End) == Second.GetAlignment(Second.Start))
                    return false;
                if (First.End == Second.End && First.GetAlignment(First.End) == Second.GetAlignment(Second.End))
                    return false;

                //if (First.ContainsPoint(Second.Start) || First.ContainsPoint(Second.End))
                //    return false;

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
        public LineBorders(MarkupRegularLine line)
        {
            Center = line.Markup.Position;
            Borders = GetBorders(line).ToList();
        }
        public IEnumerable<ITrajectory> GetBorders(MarkupRegularLine line)
        {
            if (line.ClipSidewalk)
                return line.Markup.Contour;
            else
                return Enumerable.Empty<ITrajectory>();
        }

        public IEnumerator<ITrajectory> GetEnumerator() => Borders.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public StraightTrajectory[] GetVertex(MarkupPartData dash)
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

    public enum LineType
    {
        [Description(nameof(Localize.LineStyle_RegularLinesGroup))]
        Regular = Markup.Item.RegularLine,

        [Description(nameof(Localize.LineStyle_StopLinesGroup))]
        Stop = Markup.Item.StopLine,

        [Description(nameof(Localize.LineStyle_CrosswalkLinesGroup))]
        Crosswalk = Markup.Item.Crosswalk,

        [Description(nameof(Localize.LineStyle_LaneGroup))]
        Lane = Markup.Item.Lane,

        [NotVisible]
        All = Regular | Stop | Crosswalk | Lane,
    }
    [AttributeUsage(AttributeTargets.Field)]
    public class LineTypeAttribute : Attribute
    {
        public LineType Type { get; }

        public LineTypeAttribute(LineType type)
        {
            Type = type;
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
