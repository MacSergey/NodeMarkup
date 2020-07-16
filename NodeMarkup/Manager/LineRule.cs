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
    public class MarkupLineRawRule : IToXml
    {
        public static string XmlName { get; } = "R";

        IRuleEdge _from;
        IRuleEdge _to;
        LineStyle _style;

        public IRuleEdge From
        {
            get => _from;
            set
            {
                _from = value;
                RuleChanged();
            }
        }
        public IRuleEdge To
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

        public MarkupLineRawRule(LineStyle style, IRuleEdge from = null, IRuleEdge to = null)
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
                if ((rawRule.From as IRuleEdge)?.GetT(line, out first) != true)
                    continue;

                var second = 0f;
                if ((rawRule.To as IRuleEdge)?.GetT(line, out second) != true)
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
        public static bool FromXml(XElement config, Markup markup, Dictionary<InstanceID, InstanceID> map, out MarkupLineRawRule rule)
        {
            if (!(config.Element(LineStyle.XmlName) is XElement styleConfig) || !LineStyle.FromXml(styleConfig, out LineStyle style))
            {
                rule = default;
                return false;
            }

            var edges = new List<IRuleEdge>();
            foreach (var supportConfig in config.Elements(SupportPointBase.XmlName))
            {
                if (SupportPointBase.FromXml(supportConfig, markup, map, out SupportPointBase supportPoint) && supportPoint is IRuleEdge edge)
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
    public enum RulePosition
    {
        Start,
        End
    }
}
