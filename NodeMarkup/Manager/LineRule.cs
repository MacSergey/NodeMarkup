using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace NodeMarkup.Manager
{
    public class MarkupLineRawRule
    {
        MarkupLine From { get; }
        MarkupLine To { get; }
        LineStyle LineStyle { get; }

        public MarkupLineRawRule(LineStyle lineStyle, MarkupLine from = null, MarkupLine to = null)
        {
            LineStyle = lineStyle;
            From = from;
            To = to;
        }

        public static MarkupLineRule[] GetRules(MarkupLine line, List<MarkupLineRawRule> rawRules)
        {
            var rules = new List<MarkupLineRule>();

            foreach(var rawRule in rawRules)
            {
                var start = rawRule.From == null ? 0 : line.Intersection(rawRule.From);
                var end = rawRule.To == null ? 1 : line.Intersection(rawRule.To);
                var rule = new MarkupLineRule(start, end, rawRule.LineStyle);
                Add(rules, rule);
            }

            return rules.ToArray();
        }
        private static void Add(List<MarkupLineRule> rules, MarkupLineRule newRule)
        {
            var i = 0;
            while(i < rules.Count)
            {
                var rule = rules[i];
                if (newRule.End <= rule.Start)
                {
                    rules.Insert(i, newRule);
                    return;
                }
                else if (newRule.Start < rule.Start && newRule.End < rule.End)
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
                else if (rule.Start < newRule.Start && rule.End < newRule.End)
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
        public float Start { get; set; }
        public float End { get; set; }
        public LineStyle LineStyle { get; }

        public MarkupLineRule(float start, float end, LineStyle lineStyle)
        {
            Start = start;
            End = end;
            LineStyle = lineStyle;
        }
    }
}
