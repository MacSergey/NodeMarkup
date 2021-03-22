using ModsCommon.Utilities;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public abstract class MarkupLineRawRule : MarkupLinePart
    {
        public PropertyValue<LineStyle> Style { get; }
        public new ILinePartEdge From
        {
            get => base.From.Value as ILinePartEdge;
            set => base.From.Value = value;
        }
        public new ILinePartEdge To
        {
            get => base.To.Value as ILinePartEdge;
            set => base.To.Value = value;
        }
        public bool IsOverlapped
        {
            get
            {
                if (!GetFromT(out float thisFromT) || !GetToT(out float thisToT))
                    return false;

                var thisMin = Mathf.Min(thisFromT, thisToT);
                var thisMax = Mathf.Max(thisFromT, thisToT);

                return Line.Rules.Any(r => r != this && r.GetFromT(out float fromT) && r.GetToT(out float toT) && Mathf.Min(fromT, toT) <= thisMin && thisMax <= Mathf.Max(fromT, toT));
            }
        }


        public MarkupLineRawRule(MarkupLine line, LineStyle style, ISupportPoint from = null, ISupportPoint to = null) : base(line, from, to)
        {
            style.OnStyleChanged = RuleChanged;
            Style = new PropertyValue<LineStyle>(StyleChanged, style);
        }
        private void StyleChanged()
        {
            Style.Value.OnStyleChanged = RuleChanged;
            RuleChanged();
        }
    }
    public class MarkupLineRawRule<StyleType> : MarkupLineRawRule
        where StyleType : LineStyle
    {
        public static string XmlName { get; } = "R";

        public new StyleType Style
        {
            get => base.Style.Value as StyleType;
            set => base.Style.Value = value;
        }
        public override string XmlSection => XmlName;

        public MarkupLineRawRule(MarkupLine line, StyleType style, ILinePartEdge from = null, ILinePartEdge to = null) : base(line, style, from, to) { }
        public static MarkupLineRule[] GetRules(List<MarkupLineRawRule<StyleType>> rawRules)
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
        public static bool FromXml(XElement config, MarkupLine line, ObjectsMap map, bool invert, out MarkupLineRawRule<StyleType> rule)
        {
            if (config.Element(Manager.Style.XmlName) is XElement styleConfig && Manager.Style.FromXml(styleConfig, map, invert, out StyleType style))
            {
                var edges = GetEdges(config, line, map).ToArray();
                rule = new MarkupLineRawRule<StyleType>(line, style, edges.ElementAtOrDefault(0), edges.ElementAtOrDefault(1));
                return true;
            }
            else
            {
                rule = default;
                return false;
            }
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
