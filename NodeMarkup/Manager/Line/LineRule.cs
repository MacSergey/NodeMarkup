using NodeMarkup.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public abstract class MarkupLineRawRule : MarkupLinePart
    {
        LineStyle _style;

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
        public new ILinePartEdge From
        {
            get => base.From as ILinePartEdge;
            set => base.From = value;
        }
        public new ILinePartEdge To
        {
            get => base.To as ILinePartEdge;
            set => base.To = value;
        }
        public bool IsOverlapped
        {
            get
            {
                if (!GetFromT(out float thisFromT) || !GetToT(out float thisToT))
                    return false;


                foreach (var rule in Line.Rules)
                {
                    if (rule == this)
                        continue;

                    if (rule.GetFromT(out float fromT) && rule.GetToT(out float toT))
                    {
                        var min = Mathf.Min(fromT, toT);
                        var max = Mathf.Max(fromT, toT);
                        if ((min < thisFromT && thisFromT < max) || (min < thisToT && thisToT < max))
                            return true;
                    }
                }

                return false;
            }
        }


        public MarkupLineRawRule(MarkupLine line, LineStyle style, ISupportPoint from = null, ISupportPoint to = null) : base(line, from, to)
        {
            Style = style;
        }
    }
    public class MarkupLineRawRule<StyleType> : MarkupLineRawRule
        where StyleType : LineStyle
    {
        public static string XmlName { get; } = "R";

        public new StyleType Style
        {
            get => base.Style as StyleType;
            set => base.Style = value;
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
        public static bool FromXml(XElement config, MarkupLine line, Dictionary<ObjectId, ObjectId> map, out MarkupLineRawRule<StyleType> rule)
        {
            if (config.Element(Manager.Style.XmlName) is XElement styleConfig && Manager.Style.FromXml(styleConfig, out StyleType style))
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
