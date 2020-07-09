using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public abstract class LineRawRuleEdgeBase
    {
        public static string XmlName { get; } = "E";
        public static bool FromXml(XElement config, Markup markup, out LineRawRuleEdgeBase ruleEdge)
        {
            var type = (EdgeType)config.GetAttrValue<int>("T");
            switch(type)
            {
                case EdgeType.IntersectLine when LineRawRuleEdge.FromXml(config, markup, out LineRawRuleEdge lineEdge):
                    ruleEdge = lineEdge;
                    return true;
                case EdgeType.SelfPoint when SelfPointRawRuleEdge.FromXml(config, markup, out SelfPointRawRuleEdge pointEdge):
                    ruleEdge = pointEdge;
                    return true;
                default:
                    ruleEdge = null;
                    return false;
            }
        }

        public string XmlSection => XmlName;

        public abstract EdgeType Type { get; }

        public abstract bool Equals(LineRawRuleEdgeBase other);
        public abstract void FromXml(XElement config);
        public abstract bool GetT(MarkupLine line, out float t);
        public virtual XElement ToXml()
        {
            var config = new XElement(XmlSection,
                new XAttribute("T", (int)Type)
            );
            return config;
        }

        public enum EdgeType
        {
            SelfPoint,
            IntersectLine
        }
        public enum EdgePosition
        {
            From,
            To
        }
    }
    public class LineRawRuleEdge : LineRawRuleEdgeBase
    {
        public static bool FromXml(XElement config, Markup markup, out LineRawRuleEdge lineEdge)
        {
            var lineId = config.GetAttrValue<ulong>(MarkupLine.XmlName);
            if(markup.TryGetLine(lineId, out MarkupLine line))
            {
                lineEdge = new LineRawRuleEdge(line);
                return true;
            }
            else
            {
                lineEdge = null;
                return false;
            }
        }

        public MarkupLine Line { get; }
        public override EdgeType Type { get; } = EdgeType.IntersectLine;

        public LineRawRuleEdge(MarkupLine line)
        {
            Line = line;
        }
        public override bool GetT(MarkupLine line, out float t)
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
        public override string ToString() => $"Intersect with {Line}";

        public override bool Equals(LineRawRuleEdgeBase other) => other is LineRawRuleEdge otherLine && otherLine.Line == Line;

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute(MarkupLine.XmlName, Line.Id));
            return config;
        }
        public override void FromXml(XElement config)
        {
            throw new NotImplementedException();
        }
    }
    public class SelfPointRawRuleEdge : LineRawRuleEdgeBase
    {
        public static bool FromXml(XElement config, Markup markup, out SelfPointRawRuleEdge pointEdge)
        {
            var pointId = config.GetAttrValue<int>(MarkupPoint.XmlName);
            if (MarkupPoint.FromId(pointId, markup, out MarkupPoint point))
            {
                pointEdge = new SelfPointRawRuleEdge(point);
                return true;
            }
            else
            {
                pointEdge = null;
                return false;
            }
        }

        public MarkupPoint Point { get; }
        public override EdgeType Type { get; } = EdgeType.SelfPoint;

        public SelfPointRawRuleEdge(MarkupPoint point)
        {
            Point = point;
        }
        public override bool GetT(MarkupLine line, out float t)
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
        public override string ToString() => $"Self edge point {Point}";

        public override bool Equals(LineRawRuleEdgeBase other) => other is SelfPointRawRuleEdge otherPoint && otherPoint.Point == Point;

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute(MarkupPoint.XmlName, Point.Id));
            return config;
        }
        public override void FromXml(XElement config)
        {
            throw new NotImplementedException();
        }
    }
    public class LineRawRuleEdgeBound
    {
        public static Vector3 MarkerSize { get; } = Vector3.one * 0.5f;

        public LineRawRuleEdgeBase LineRawRuleEdge { get; private set; }
        Bounds Bounds { get; set; }
        public Vector3 Position => Bounds.center;

        public LineRawRuleEdgeBound(MarkupLine line, LineRawRuleEdgeBase lineRawRuleEdge)
        {
            LineRawRuleEdge = lineRawRuleEdge;
            LineRawRuleEdge.GetT(line, out float t);
            var position = line.Trajectory.Position(t);
            Bounds = new Bounds(position, MarkerSize);
        }

        public bool IsIntersect(Ray ray) => Bounds.IntersectRay(ray);
    }

    public class MarkupLineRawRule : IToXml
    {
        public static string XmlName { get; } = "R";

        LineRawRuleEdgeBase _from;
        LineRawRuleEdgeBase _to;
        LineStyle _style;

        public LineRawRuleEdgeBase From
        {
            get => _from;
            set
            {
                _from = value;
                RuleChanged();
            }
        }
        public LineRawRuleEdgeBase To
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

        public string XmlSection => XmlName;

        public MarkupLineRawRule(LineStyle style, LineRawRuleEdgeBase from = null, LineRawRuleEdgeBase to = null)
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

                var first = 0f;
                if (rawRule.From?.GetT(line, out first) != true)
                    continue;

                var second = 0f;
                if (rawRule.To?.GetT(line, out second) != true)
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

        public XElement ToXml()
        {
            var config = new XElement(XmlSection);

            if (From != null)
                config.Add(From.ToXml());
            if (To != null)
                config.Add(To.ToXml());

            config.Add(Style.ToXml());

            return config;
        }
        public static bool FromXml(XElement config, Markup markup, out MarkupLineRawRule rule)
        {
            if (!(config.Element(LineStyle.XmlName) is XElement styleConfig) || !LineStyle.FromXml(styleConfig, out LineStyle style))
            {
                rule = default;
                return false;
            }

            var edges = new List<LineRawRuleEdgeBase>();
            foreach(var edgeConfig in config.Elements(LineRawRuleEdgeBase.XmlName))
            {
                if (LineRawRuleEdgeBase.FromXml(edgeConfig, markup, out LineRawRuleEdgeBase edge))
                    edges.Add(edge);
            }

            rule = new MarkupLineRawRule(style, edges.ElementAtOrDefault(0), edges.ElementAtOrDefault(1));
            return true;
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
