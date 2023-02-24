using ColossalFramework.Math;
using IMT.Utilities;
using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
{
    public class MarkingRegularLine : MarkingLine
    {
        public override LineType Type => LineType.Regular;
        public override Alignment Alignment => RawAlignment;
        public PropertyEnumValue<Alignment> RawAlignment { get; private set; }
        public PropertyBoolValue ClipSidewalk { get; private set; }
        public override bool IsSupportRules => true;
        private List<MarkingLineRawRule<RegularLineStyle>> RawRules { get; } = new List<MarkingLineRawRule<RegularLineStyle>>();
        public override IEnumerable<MarkingLineRawRule> Rules => RawRules.Cast<MarkingLineRawRule>();
        public override int RuleCount => RawRules.Count;

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

            if (Marking.Type == MarkingType.Node || isStraight)
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

            var intersects = Marking.GetIntersects(this).Where(i => i.IsIntersect && i.pair.GetOther(this) is MarkingCrosswalkLine line && line.PointPair.ContainsEnter(point.Enter)).ToArray();
            if (!intersects.Any())
                return new EnterPointEdge(point);

            var intersect = intersects.Aggregate((i, j) => point == End ^ (i.FirstT > i.SecondT) ? i : j);
            return new LinesIntersectEdge(intersect.pair);
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

        protected override void GetStyleData(Action<IStyleData> addData)
        {
            var rules = MarkingLineRawRule<RegularLineStyle>.GetRules(RawRules);

            foreach (var rule in rules)
            {
                var trajectoryPart = Trajectory.Cut(rule.Start, rule.End);
                rule.LineStyle.Calculate(this, trajectoryPart, addData);
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

            if (invert)
                RawAlignment.Value = RawAlignment.Value.Invert();
        }

        public override void GetUsedAssets(HashSet<string> networks, HashSet<string> props, HashSet<string> trees)
        {
            foreach(var rule in Rules)
                rule.Style.Value.GetUsedAssets(networks, props, trees);
        }
    }
}
