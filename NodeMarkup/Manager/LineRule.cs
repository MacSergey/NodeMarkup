using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public interface IMarkupLineRawRuleEdge : IEquatable<IMarkupLineRawRuleEdge>
    {
        bool GetT(MarkupLine line, out float t);
    }
    public class LineRawRuleEdge : IMarkupLineRawRuleEdge
    {
        public MarkupLine Line { get; }
        public LineRawRuleEdge(MarkupLine line)
        {
            Line = line;
        }
        public bool GetT(MarkupLine line, out float t)
        {
            var pair = new MarkupLinePair(line, Line);
            var intersect = line.Markup.GetIntersect(pair);

            if (intersect.IsIntersect)
            {
                t = intersect[line];
                return true;
            }
            else
            {
                t = default;
                return false;
            }
        }
        public override string ToString() => Line.ToString();

        public bool Equals(IMarkupLineRawRuleEdge other) => other is LineRawRuleEdge otherLine && otherLine.Line == Line;
    }
    public class SelfPointRawRuleEdge : IMarkupLineRawRuleEdge
    {
        public MarkupPoint Point { get; }
        public SelfPointRawRuleEdge(MarkupPoint point)
        {
            Point = point;
        }
        public bool GetT(MarkupLine line, out float t)
        {
            if (line.ContainPoint(Point))
            {
                t = line.PointPair.First == Point ? 0 : 1;
                return true;
            }
            else
            {
                t = default;
                return false;
            }
        }
        public override string ToString() => Point.ToString();

        public bool Equals(IMarkupLineRawRuleEdge other) => other is SelfPointRawRuleEdge otherPoint && otherPoint.Point == Point;
    }
    public class LineRawRuleEdgeBound
    {
        public static Vector3 MarkerSize { get; } = Vector3.one * 0.5f;

        public IMarkupLineRawRuleEdge LineRawRuleEdge { get; private set; }
        Bounds Bounds { get; set; }
        public Vector3 Position => Bounds.center;

        public LineRawRuleEdgeBound(MarkupLine line, IMarkupLineRawRuleEdge lineRawRuleEdge)
        {
            LineRawRuleEdge = lineRawRuleEdge;
            LineRawRuleEdge.GetT(line, out float t);
            var position = line.Trajectory.Position(t);
            Bounds = new Bounds(position, MarkerSize);
        }

        public bool IsIntersect(Ray ray) => Bounds.IntersectRay(ray);
    }

    public class MarkupLineRawRule
    {
        IMarkupLineRawRuleEdge _from;
        IMarkupLineRawRuleEdge _to;
        LineStyle _style;

        public IMarkupLineRawRuleEdge From
        {
            get => _from;
            set
            {
                _from = value;
                RuleChanged();
            }
        }
        public IMarkupLineRawRuleEdge To
        {
            get => _to;
            set
            {
                _to = value;
                RuleChanged();
            }
        }
        public LineStyle Style
        {
            get => _style;
            set
            {
                _style = value;
                _style.OnStyleChanged = RuleChanged;
                RuleChanged();
            }
        }

        public Action OnRuleChanged { private get; set; }

        public MarkupLineRawRule(LineStyle style, IMarkupLineRawRuleEdge from, IMarkupLineRawRuleEdge to)
        {
            Style = style;
            From = from;
            To = to;
        }

        private void RuleChanged() => OnRuleChanged?.Invoke();

        public static MarkupLineRule[] GetRules(MarkupLine line, List<MarkupLineRawRule> rawRules)
        {
            var rules = new List<MarkupLineRule>();

            foreach (var rawRule in rawRules)
            {
                var rule = new MarkupLineRule(rawRule.Style);

                if (!rawRule.From.GetT(line, out float first))
                    continue;

                if (!rawRule.To.GetT(line, out float second))
                    continue;

                if (first == second)
                    continue;

                if (first < second)
                {
                    rule.Start = first;
                    rule.End = second;
                }
                else
                {
                    rule.Start = second;
                    rule.End = first;
                }

                Add(rules, rule);
            }

            return rules.ToArray();
        }
        private static void Add(List<MarkupLineRule> rules, MarkupLineRule newRule)
        {
            var i = 0;
            while (i < rules.Count)
            {
                var rule = rules[i];
                if (newRule.End <= rule.Start)
                {
                    rules.Insert(i, newRule);
                    return;
                }
                else if (newRule.Start < rule.Start && newRule.End < rule.End && rule.Start <= newRule.End)
                {
                    var middle = (rule.Start + newRule.End) / 2;
                    rule.Start = middle;
                    newRule.End = middle;
                    rules.Insert(i, newRule);
                    return;
                }
                else if (newRule.Start <= rule.Start && newRule.End == rule.End)
                {
                    rules[i] = newRule;
                    return;
                }
                else if (rule.Start <= newRule.Start && newRule.End <= rule.End)
                {
                    return;
                }
                else if (newRule.Start == rule.Start && rule.End < newRule.End)
                {
                    rules.RemoveAt(i);
                    continue;
                }
                else if (rule.Start < newRule.Start && rule.End < newRule.End && newRule.Start <= rule.End)
                {
                    var middle = (newRule.Start + rule.End) / 2;
                    rule.End = middle;
                    newRule.Start = middle;
                    i += 1;
                    continue;
                }
                else
                    i += 1;
            }

            rules.Add(newRule);
        }
    }

    public struct MarkupLineRule
    {
        public float Start;
        public float End;
        public LineStyle LineStyle;

        public MarkupLineRule(LineStyle lineStyle)
        {
            LineStyle = lineStyle;
            Start = 0;
            End = 1;
        }

        public MarkupLineRule(float start, float end, LineStyle lineStyle)
        {
            Start = start;
            End = end;
            LineStyle = lineStyle;
        }
    }
}
