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

namespace NodeMarkup.Manager
{
    public abstract class MarkingLine : IStyleItem, IToXml, ISupport
    {
        public static string XmlName { get; } = "L";

        public string DeleteCaptionDescription => Localize.LineEditor_DeleteCaptionDescription;
        public string DeleteMessageDescription => Localize.LineEditor_DeleteMessageDescription;
        public Marking.SupportType Support => Marking.SupportType.Lines;

        public abstract LineType Type { get; }

        public Marking Marking { get; private set; }
        public ulong Id => PointPair.Hash;

        public MarkingPointPair PointPair { get; }
        public MarkingPoint Start => PointPair.First;
        public MarkingPoint End => PointPair.Second;
        public virtual bool IsSupportRules => false;
        public bool IsEnterLine => PointPair.IsSameEnter;
        public bool IsSame => PointPair.IsSame;
        public bool IsNormal => PointPair.IsNormal;
        public bool IsStopLine => PointPair.IsStopLine;
        public bool IsCrosswalk => PointPair.IsCrosswalk;
        public virtual Alignment Alignment => Alignment.Centre;

        public bool HasOverlapped => Rules.Any(r => r.IsOverlapped);

        public abstract IEnumerable<MarkingLineRawRule> Rules { get; }
        public abstract IEnumerable<ILinePartEdge> RulesEdges { get; }

        public ITrajectory Trajectory { get; private set; }
        public List<IStyleData> StyleData { get; } = new List<IStyleData>();

        public string XmlSection => XmlName;

        protected MarkingLine(Marking marking, MarkingPointPair pointPair, bool update = true)
        {
            Marking = marking;
            PointPair = pointPair;

            if (update)
                Update(true);
        }
        protected virtual void RuleChanged() => Marking.Update(this, true);

        public void Update(bool onlySelfUpdate = false)
        {
            Trajectory = CalculateTrajectory();
            if (!onlySelfUpdate)
                Marking.Update(this);
        }
        protected abstract ITrajectory CalculateTrajectory();

        public void RecalculateStyleData()
        {
#if DEBUG_RECALCULATE
            Mod.Logger.Debug($"Recalculate line {this}");
#endif
            StyleData.Clear();
            foreach (var lod in EnumExtension.GetEnumValues<MarkingLOD>())
            {
                StyleData.AddRange(GetStyleData(lod));
            }
        }

        protected abstract IEnumerable<IStyleData> GetStyleData(MarkingLOD lod);

        public bool ContainsPoint(MarkingPoint point) => PointPair.ContainsPoint(point);

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

        public IEnumerable<MarkingLine> IntersectLines
        {
            get
            {
                foreach (var intersect in Marking.GetIntersects(this))
                {
                    if (intersect.IsIntersect)
                        yield return intersect.Pair.GetOther(this);
                }
            }
        }
        public virtual void Render(OverlayData data) => Trajectory.Render(data);
        public virtual void RenderRule(MarkingLineRawRule rule, OverlayData data)
        {
            if (rule.GetTrajectory(out var trajectory))
                trajectory.Render(data);
        }
        public abstract bool ContainsRule(MarkingLineRawRule rule);
        public bool ContainsEnter(Entrance enter) => PointPair.ContainsEnter(enter);

        public Dependences GetDependences() => Marking.GetLineDependences(this);
        public bool IsStart(MarkingPoint point) => Start == point;
        public bool IsEnd(MarkingPoint point) => End == point;
        public Alignment GetAlignment(MarkingPoint point) => PointPair.ContainsPoint(point) && point.IsSplit ? (IsStart(point) ? Alignment : Alignment.Invert()) : Alignment.Centre;


        public virtual XElement ToXml()
        {
            var config = new XElement(XmlSection);
            config.AddAttr(nameof(Id), Id);
            config.AddAttr("T", (int)Type);

            return config;
        }
        public static bool FromXml(XElement config, Marking marking, ObjectsMap map, out MarkingLine line, out bool invert)
        {
            var lineId = config.GetAttrValue<ulong>(nameof(Id));
            if (!MarkingPointPair.FromHash(lineId, marking, map, out MarkingPointPair pointPair, out invert))
            {
                line = null;
                return false;
            }

            if (!marking.TryGetLine(pointPair, out line))
            {
                var type = (LineType)config.GetAttrValue("T", (int)pointPair.DefaultType);
                if ((type & marking.SupportLines) == 0)
                    return false;

                switch (type)
                {
                    case LineType.Regular:
                        line = pointPair.IsNormal ? new MarkingNormalLine(marking, pointPair) : new MarkingRegularLine(marking, pointPair);
                        break;
                    case LineType.Stop:
                        line = new MarkingStopLine(marking, pointPair);
                        break;
                    case LineType.Crosswalk:
                        line = new MarkingCrosswalkLine(marking, pointPair);
                        break;
                    case LineType.Lane:
                        line = new MarkingLaneLine(marking, pointPair);
                        break;
                    default:
                        return false;
                }
            }

            return true;
        }
        public abstract void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged);

        public override string ToString() => PointPair.ToString();
    }
    public class MarkingRegularLine : MarkingLine
    {
        public override LineType Type => LineType.Regular;
        public override Alignment Alignment => RawAlignment;
        public PropertyEnumValue<Alignment> RawAlignment { get; private set; }
        public PropertyBoolValue ClipSidewalk { get; private set; }
        public override bool IsSupportRules => true;
        private List<MarkingLineRawRule<RegularLineStyle>> RawRules { get; } = new List<MarkingLineRawRule<RegularLineStyle>>();
        public override IEnumerable<MarkingLineRawRule> Rules => RawRules.Cast<MarkingLineRawRule>();

        public LineBorders Borders => new LineBorders(this);
        private bool DefaultClipSidewalk => Marking.Type == MarkingType.Node && PointPair.IsSideLine && PointPair.NetworkType == NetworkType.Road;

        public MarkingRegularLine(Marking marking, MarkingPoint first, MarkingPoint second, RegularLineStyle style = null, Alignment alignment = Alignment.Centre, bool update = true) : this(marking, MarkingPointPair.FromPoints(first, second, out bool invert), style, !invert ? alignment : alignment.Invert(), update) { }
        public MarkingRegularLine(Marking marking, MarkingPointPair pointPair, RegularLineStyle style = null, Alignment alignment = Alignment.Centre, bool update = true) : base(marking, pointPair, false)
        {
            RawAlignment = new PropertyEnumValue<Alignment>("A", AlignmentChanged, alignment);
            ClipSidewalk = new PropertyBoolValue("CS", ClipSidewalkChanged, DefaultClipSidewalk);

            if (update)
                Update(true);

            if (style != null)
            {
                AddRule(style, false, false);
                Marking.RecalculateStyleData(this);
            }
        }

        protected override ITrajectory CalculateTrajectory()
        {
            var startPos = PointPair.First.GetAbsolutePosition(RawAlignment);
            var endPos = PointPair.Second.GetAbsolutePosition(RawAlignment.Value.Invert());
            var startDir = PointPair.First.Direction;
            var endDir = PointPair.Second.Direction;

            float startT;
            float endT;
            var isStraight = Marking.Type == MarkingType.Segment && NetSegment.IsStraight(PointPair.First.Enter.Position, PointPair.First.Enter.NormalDir, PointPair.Second.Enter.Position, PointPair.Second.Enter.NormalDir);
            if (isStraight)
            {
                startT = PointPair.First.Enter.IsSmooth ? BezierTrajectory.curveT : BezierTrajectory.straightT;
                endT = PointPair.Second.Enter.IsSmooth ? BezierTrajectory.curveT : BezierTrajectory.straightT;
            }
            else
            {
                startT = BezierTrajectory.curveT;
                endT = BezierTrajectory.curveT;
            }

            var trajectory = new BezierTrajectory(startPos, startDir, endPos, endDir, startT, endT);

            if(Marking.Type == MarkingType.Node || isStraight)
                return trajectory;

            var deltaH = Mathf.Abs(trajectory.StartPosition.y - trajectory.EndPosition.y) / trajectory.Length;
            var startRelPos = PointPair.First.GetRelativePosition(RawAlignment);
            var endRelPos = PointPair.Second.GetRelativePosition(RawAlignment.Value.Invert());
            var deltaX = Mathf.Abs(startRelPos + endRelPos);
            if (deltaX < 0.5f || (deltaH < 0.1f && deltaX < 2f))
                return trajectory;

            var startPosF = PointPair.First.Enter.GetPosition(-endRelPos);
            var endPosF = PointPair.Second.Enter.GetPosition(-startRelPos);

            var bezier = new Bezier3()
            {
                a = startPos,
                d = endPos,
            };
            var bezierL = new Bezier3()
            {
                a = startPos,
                d = endPosF,
            };
            var bezierR = new Bezier3()
            {
                a = startPosF,
                d = endPos,
            };

            BezierTrajectory.GetMiddlePoints(startPos, startDir, endPos, endDir, startT, endT, startT, endT, out bezier.b, out bezier.c, out _, out _);
            BezierTrajectory.GetMiddlePoints(startPos, startDir, endPosF, endDir, startT, endT, startT, endT, out bezierL.b, out bezierL.c, out _, out _);
            BezierTrajectory.GetMiddlePoints(startPosF, startDir, endPos, endDir, startT, endT, startT, endT, out bezierR.b, out bezierR.c, out _, out _);

            var middlePos = (bezierL.Position(0.5f) + bezierR.Position(0.5f)) * 0.5f;
            var middleDir = VectorUtils.NormalizeXZ(bezier.Tangent(0.5f));
            var middleDirLR = VectorUtils.NormalizeXZ(bezierL.Tangent(0.5f) + bezierR.Tangent(0.5f));
            middleDir.y = middleDirLR.y;
            middleDir.Normalize();

            BezierTrajectory.GetMiddleDistance(startPos, startDir, middlePos, -middleDir, startT, BezierTrajectory.curveT, startT, BezierTrajectory.curveT, out var startDis, out var middleDis1, out _, out _);
            BezierTrajectory.GetMiddleDistance(middlePos, middleDir, endPos, endDir, BezierTrajectory.curveT, endT, BezierTrajectory.curveT, endT, out var middleDis2, out var endDis, out _, out _);

            var middleDis = (middleDis1 + middleDis2) * 0.5f;

            var bezier1 = new Bezier3()
            {
                a = startPos,
                b = startPos + startDir * startDis,
                c = middlePos - middleDir * middleDis,
                d = middlePos,
            };
            var bezier2 = new Bezier3()
            {
                a = middlePos,
                b = middlePos + middleDir * middleDis,
                c = endPos + endDir * endDis,
                d = endPos,
            };

            return new CombinedTrajectory(new BezierTrajectory(bezier1), new BezierTrajectory(bezier2));
        }
        private void AlignmentChanged() => Marking.Update(this, true, true);
        private void ClipSidewalkChanged() => Marking.Update(this, true, false);

        private void AddRule(MarkingLineRawRule<RegularLineStyle> rule, bool update = true)
        {
            rule.OnRuleChanged = RuleChanged;
            RawRules.Add(rule);

            if (update)
                RuleChanged();
        }
        public MarkingLineRawRule<RegularLineStyle> AddRule(RegularLineStyle lineStyle, bool empty = true, bool update = true)
        {
            var newRule = GetDefaultRule(lineStyle, empty);
            AddRule(newRule, update);
            return newRule;
        }
        protected virtual MarkingLineRawRule<RegularLineStyle> GetDefaultRule(RegularLineStyle lineStyle, bool empty = true)
        {
            var from = empty ? null : GetDefaultEdge(Start);
            var to = empty ? null : GetDefaultEdge(End);
            return new MarkingLineRawRule<RegularLineStyle>(this, lineStyle, from, to);
        }
        private ILinePartEdge GetDefaultEdge(MarkingPoint point)
        {
            if (!Settings.CutLineByCrosswalk || point.Type == MarkingPoint.PointType.Normal)
                return new EnterPointEdge(point);

            var intersects = Marking.GetIntersects(this).Where(i => i.IsIntersect && i.Pair.GetOther(this) is MarkingCrosswalkLine line && line.PointPair.ContainsEnter(point.Enter)).ToArray();
            if (!intersects.Any())
                return new EnterPointEdge(point);

            var intersect = intersects.Aggregate((i, j) => point == End ^ (i.FirstT > i.SecondT) ? i : j);
            return new LinesIntersectEdge(intersect.Pair);
        }

        public MarkingLineRawRule<RegularLineStyle> AddRule(bool empty = true, bool update = true)
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
        public void RemoveRule(MarkingLineRawRule<RegularLineStyle> rule)
        {
            RawRules.Remove(rule);
            RuleChanged();
        }
        public bool RemoveRules(MarkingLine intersectLine)
        {
            if (!RawRules.Any())
                return false;

            var removed = RawRules.RemoveAll(r => Match(intersectLine, r.From) || Match(intersectLine, r.To));

            if (!RawRules.Any())
                AddRule(false, false);

            return removed != 0;
        }
        private bool Match(MarkingLine intersectLine, ISupportPoint supportPoint) => supportPoint is IntersectSupportPoint lineRuleEdge && lineRuleEdge.LinePair.ContainLine(intersectLine);
        public int GetLineDependences(MarkingLine intersectLine) => RawRules.Count(r => Match(intersectLine, r.From) || Match(intersectLine, r.To));
        public override bool ContainsRule(MarkingLineRawRule rule) => rule != null && RawRules.Any(r => r == rule);

        protected override IEnumerable<IStyleData> GetStyleData(MarkingLOD lod)
        {
            var rules = MarkingLineRawRule<RegularLineStyle>.GetRules(RawRules);

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
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            RawAlignment.FromXml(config);
            ClipSidewalk.FromXml(config, DefaultClipSidewalk);
            foreach (var ruleConfig in config.Elements(MarkingLineRawRule<RegularLineStyle>.XmlName))
            {
                if (MarkingLineRawRule<RegularLineStyle>.FromXml(ruleConfig, this, map, invert, typeChanged, out MarkingLineRawRule<RegularLineStyle> rule))
                    AddRule(rule, false);
            }
        }
    }
    public class MarkingNormalLine : MarkingRegularLine
    {
        public MarkingNormalLine(Marking marking, MarkingPointPair pointPair, RegularLineStyle style = null, Alignment alignment = Alignment.Centre) : base(marking, pointPair, style, alignment) { }
        protected override ITrajectory CalculateTrajectory() => new StraightTrajectory(Start.GetAbsolutePosition(RawAlignment), End.GetAbsolutePosition(RawAlignment.Value.Invert()));
    }
    public class MarkingFillerTempLine : MarkingRegularLine
    {
        public MarkingFillerTempLine(Marking marking, MarkingPoint first, MarkingPoint second, Alignment alignment) : base(marking, first, second, null, alignment) { }
        public MarkingFillerTempLine(Marking marking, MarkingPointPair pair, Alignment alignment) : base(marking, pair, null, alignment) { }
    }
    public class MarkingCrosswalkLine : MarkingRegularLine
    {
        public override LineType Type => LineType.Crosswalk;
        public MarkingCrosswalk Crosswalk { get; set; }
        public Func<StraightTrajectory> TrajectoryGetter { get; set; }

        public MarkingCrosswalkLine(Marking marking, MarkingPointPair pointPair, CrosswalkStyle style = null) : base(marking, pointPair, update: false)
        {
            if (style == null)
                style = SingletonManager<StyleTemplateManager>.Instance.GetDefault<CrosswalkStyle>(Style.StyleType.CrosswalkExistent);

            Crosswalk = new MarkingCrosswalk(Marking, this, style);
            Update(true);
            Marking.AddCrosswalk(Crosswalk);
        }
        protected override MarkingLineRawRule<RegularLineStyle> GetDefaultRule(RegularLineStyle lineStyle, bool empty = true)
        {
            var from = empty ? null : new CrosswalkBorderEdge(this, BorderPosition.Right);
            var to = empty ? null : new CrosswalkBorderEdge(this, BorderPosition.Left);
            return new MarkingLineRawRule<RegularLineStyle>(this, lineStyle, from, to);
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

                if (Crosswalk.LeftBorder.Value is MarkingRegularLine leftBorder)
                    lines[leftBorder.PointPair] = leftBorder;

                if (Crosswalk.RightBorder.Value is MarkingRegularLine rightBorder)
                    lines[rightBorder.PointPair] = rightBorder;

                foreach (var line in lines.Values)
                    yield return new LinesIntersectEdge(this, line);
            }
        }
    }
    public class MarkingLaneLine : MarkingRegularLine
    {
        public override LineType Type => LineType.Lane;

        public MarkingLaneLine(Marking marking, MarkingPointPair pointPair, RegularLineStyle style = null) : base(marking, pointPair, style) { }

        public override void Render(OverlayData data)
        {
            var lanePointS = PointPair.First as MarkingLanePoint;
            var lanePointE = PointPair.Second as MarkingLanePoint;

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
        public override void RenderRule(MarkingLineRawRule rule, OverlayData data)
        {
            if (!rule.GetT(out var fromT, out var toT) || fromT == toT)
                return;

            var lanePointS = PointPair.First as MarkingLanePoint;
            var lanePointE = PointPair.Second as MarkingLanePoint;

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
    public class MarkingStopLine : MarkingLine
    {
        public override LineType Type => LineType.Stop;

        public MarkingLineRawRule<StopLineStyle> Rule { get; set; }
        public override IEnumerable<MarkingLineRawRule> Rules { get { yield return Rule; } }
        public override IEnumerable<ILinePartEdge> RulesEdges => RulesEnterPointEdge;

        public PropertyEnumValue<Alignment> RawStartAlignment { get; private set; }
        public PropertyEnumValue<Alignment> RawEndAlignment { get; private set; }

        public MarkingStopLine(Marking marking, MarkingPointPair pointPair, StopLineStyle style = null, Alignment firstAlignment = Alignment.Centre, Alignment secondAlignment = Alignment.Centre) : base(marking, pointPair, false)
        {
            RawStartAlignment = new PropertyEnumValue<Alignment>("AL", AlignmentChanged, firstAlignment);
            RawEndAlignment = new PropertyEnumValue<Alignment>("AR", AlignmentChanged, secondAlignment);

            style ??= SingletonManager<StyleTemplateManager>.Instance.GetDefault<StopLineStyle>(Style.StyleType.StopLineSolid);
            var rule = new MarkingLineRawRule<StopLineStyle>(this, style, new EnterPointEdge(Start), new EnterPointEdge(End));
            SetRule(rule);

            Update(true);
            Marking.RecalculateStyleData(this);
        }

        private void AlignmentChanged() => Marking.Update(this, true, true);
        protected override ITrajectory CalculateTrajectory() => new StraightTrajectory(PointPair.First.GetAbsolutePosition(RawStartAlignment), PointPair.Second.GetAbsolutePosition(RawEndAlignment));
        protected void SetRule(MarkingLineRawRule<StopLineStyle> rule)
        {
            rule.OnRuleChanged = RuleChanged;
            Rule = rule;
        }
        public override bool ContainsRule(MarkingLineRawRule rule) => rule != null && rule == Rule;
        protected override IEnumerable<IStyleData> GetStyleData(MarkingLOD lod)
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
        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            if (config.Element(MarkingLineRawRule<StopLineStyle>.XmlName) is XElement ruleConfig && MarkingLineRawRule<StopLineStyle>.FromXml(ruleConfig, this, map, invert, typeChanged, out MarkingLineRawRule<StopLineStyle> rule))
                SetRule(rule);

            RawStartAlignment.FromXml(config);
            RawEndAlignment.FromXml(config);
        }
    }
    public class MarkingEnterLine : MarkingLine
    {
        public override LineType Type => throw new NotImplementedException();
        public override IEnumerable<ILinePartEdge> RulesEdges => throw new NotImplementedException();
        public override IEnumerable<MarkingLineRawRule> Rules { get { yield break; } }

        public virtual Alignment StartAlignment { get; private set; } = Alignment.Centre;
        public virtual Alignment EndAlignment { get; private set; } = Alignment.Centre;

        public bool IsDot => IsSame && StartAlignment == EndAlignment;


        public MarkingEnterLine(Marking marking, MarkingPoint first, MarkingPoint second, Alignment firstAlignment = Alignment.Centre, Alignment secondAlignment = Alignment.Centre) : base(marking, MarkingPointPair.FromPoints(first, second, out bool invert), false)
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
        public override bool ContainsRule(MarkingLineRawRule rule) => false;
        protected override IEnumerable<IStyleData> GetStyleData(MarkingLOD lod) { yield break; }

        public override void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged) { }
    }

    public struct MarkingLinePair
    {
        public static MarkupLinePairComparer Comparer { get; } = new MarkupLinePairComparer();
        public static bool operator ==(MarkingLinePair a, MarkingLinePair b) => Comparer.Equals(a, b);
        public static bool operator !=(MarkingLinePair a, MarkingLinePair b) => !Comparer.Equals(a, b);

        public MarkingLine First;
        public MarkingLine Second;

        public Marking Marking => First.Marking == Second.Marking ? First.Marking : null;
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

                static bool IsBorder(MarkingLine line1, MarkingLine line2) => line1 is MarkingCrosswalkLine crosswalkLine && crosswalkLine.Crosswalk.IsBorder(line2);
            }
        }

        public MarkingLinePair(MarkingLine first, MarkingLine second)
        {
            First = first;
            Second = second;
        }
        public bool ContainLine(MarkingLine line) => First == line || Second == line;
        public MarkingLine GetOther(MarkingLine line)
        {
            if (ContainLine(line))
                return line == First ? Second : First;
            else
                return null;
        }
        public MarkingLine GetLine(MarkingPoint point)
        {
            if (First.ContainsPoint(point))
                return First;
            else if (Second.ContainsPoint(point))
                return Second;
            else
                return null;
        }

        public override string ToString() => $"{First} × {Second}";

        public class MarkupLinePairComparer : IEqualityComparer<MarkingLinePair>
        {
            public bool Equals(MarkingLinePair x, MarkingLinePair y) => (x.First == y.First && x.Second == y.Second) || (x.First == y.Second && x.Second == y.First);
            public int GetHashCode(MarkingLinePair pair) => pair.GetHashCode();
        }
    }
    public class LineBorders : IEnumerable<ITrajectory>
    {
        public Vector3 Center { get; }
        public List<ITrajectory> Borders { get; }
        public bool IsEmpty => !Borders.Any();
        public LineBorders(MarkingRegularLine line)
        {
            Center = line.Marking.Position;
            Borders = GetBorders(line).ToList();
        }
        public IEnumerable<ITrajectory> GetBorders(MarkingRegularLine line)
        {
            if (line.ClipSidewalk)
                return line.Marking.Contour;
            else
                return Enumerable.Empty<ITrajectory>();
        }

        public IEnumerator<ITrajectory> GetEnumerator() => Borders.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public StraightTrajectory[] GetVertex(MarkingPartData dash)
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
        Regular = Marking.Item.RegularLine,

        [Description(nameof(Localize.LineStyle_StopLinesGroup))]
        Stop = Marking.Item.StopLine,

        [Description(nameof(Localize.LineStyle_CrosswalkLinesGroup))]
        Crosswalk = Marking.Item.Crosswalk,

        [Description(nameof(Localize.LineStyle_LaneGroup))]
        Lane = Marking.Item.Lane,

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
