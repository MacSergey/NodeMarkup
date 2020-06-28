using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace NodeMarkup.Manager
{
    public class MarkupLineRawRule
    {
        MarkupLine _from;
        MarkupLine _to;
        LineStyle _style;

        public MarkupLine From
        {
            get => _from;
            set
            {
                _from = value;
                RuleChanged();
            }
        }
        public MarkupLine To
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

        public MarkupLineRawRule(LineStyle style, MarkupLine from = null, MarkupLine to = null)
        {
            Style = style;
            From = from;
            To = to;
        }

        private void RuleChanged() => OnRuleChanged?.Invoke();

        public static MarkupLineRule[] GetRules(MarkupLine line, List<MarkupLineRawRule> rawRules)
        {
            var markup = line.Markup;
            var rules = new List<MarkupLineRule>();

            foreach (var rawRule in rawRules)
            {
                var rule = new MarkupLineRule(rawRule.Style);

                if(rawRule.From != null)
                {
                    var pair = new MarkupLinePair(line, rawRule.From);
                    var intersect = markup.GetIntersect(pair);
                    if (intersect.IsIntersect)
                        rule.Start = intersect[line];
                    else
                        continue;
                }

                if (rawRule.To != null)
                {
                    var pair = new MarkupLinePair(line, rawRule.To);
                    var intersect = markup.GetIntersect(pair);
                    if (intersect.IsIntersect)
                        rule.End = intersect[line];
                    else
                        continue;
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
