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
    public class MarkupLineRawRule : MarkupLinePart
    {
        public static string XmlName { get; } = "R";

        BaseStyle _style;

        public BaseStyle Style
        {
            get => _style;
            set
            {
                _style = value;
                _style.OnStyleChanged = RuleChanged;
                RuleChanged();
            }
        }
        public override string XmlSection => XmlName;

        public MarkupLineRawRule(MarkupLine line, BaseStyle style, ILinePartEdge from = null, ILinePartEdge to = null) : base(line, from, to)
        {
            Style = style;
        }
        public static MarkupLineRule[] GetRules(List<MarkupLineRawRule> rawRules)
        {
            var rules = new List<MarkupLineRule>();

            foreach (var rawRule in rawRules)
            {
                var rule = new MarkupLineRule(rawRule.Style);

                if (!rawRule.GetFromT(out float first) || !rawRule.GetToT(out float second) || first == second)
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

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(Style.ToXml());
            return config;
        }
        public static bool FromXml(XElement config, MarkupLine line, Dictionary<InstanceID, InstanceID> map, out MarkupLineRawRule rule)
        {
            if (!(config.Element(BaseStyle.XmlName) is XElement styleConfig) || !BaseStyle.FromXml(styleConfig, out BaseStyle style))
            {
                rule = default;
                return false;
            }

            var edges = new List<ILinePartEdge>();
            foreach (var supportConfig in config.Elements(LinePartEdge.XmlName))
            {
                if (LinePartEdge.FromXml(supportConfig, line.Markup, map, out LinePartEdge supportPoint) && supportPoint is ILinePartEdge edge)
                    edges.Add(edge);
            }

            rule = new MarkupLineRawRule(line, style, edges.ElementAtOrDefault(0), edges.ElementAtOrDefault(1));
            return true;
        }
    }
    public struct MarkupLineRule
    {
        public float Start;
        public float End;
        public BaseStyle LineStyle;

        public MarkupLineRule(BaseStyle lineStyle)
        {
            LineStyle = lineStyle;
            Start = 0;
            End = 1;
        }

        public MarkupLineRule(float start, float end, BaseStyle lineStyle)
        {
            Start = start;
            End = end;
            LineStyle = lineStyle;
        }
    }
    public enum RulePosition
    {
        Start,
        End
    }
}
