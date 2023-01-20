using ModsCommon.Utilities;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace NodeMarkup.Manager
{
    public abstract class MarkingLinePart : IToXml, IOverlay
    {
        public Action OnRuleChanged { private get; set; }

        public PropertyValue<ISupportPoint> From { get; }
        public PropertyValue<ISupportPoint> To { get; }

        public MarkingLine Line { get; }
        public abstract string XmlSection { get; }

        public MarkingLinePart(MarkingLine line, ISupportPoint from = null, ISupportPoint to = null)
        {
            Line = line;
            From = new PropertyClassValue<ISupportPoint>(RuleChanged, from);
            To = new PropertyClassValue<ISupportPoint>(RuleChanged, to);
        }

        protected void RuleChanged() => OnRuleChanged?.Invoke();
        public bool GetT(out float from, out float to)
        {
            var isFrom = GetFromT(out from);
            var isTo = GetToT(out to);

            if (isFrom && isTo)
                return true;
            else if (isFrom)
            {
                to = from;
                return true;
            }
            else if (isTo)
            {
                from = to;
                return true;
            }
            else
                return false;
        }
        public bool GetFromT(out float t) => GetT(From.Value, out t);
        public bool GetToT(out float t) => GetT(To.Value, out t);
        private bool GetT(ISupportPoint partEdge, out float t)
        {
            if (partEdge != null)
                return partEdge.GetT(Line, out t);
            else
            {
                t = -1;
                return false;
            }
        }
        public bool GetTrajectory(out ITrajectory bezier)
        {
            if (GetT(out float from, out float to))
            {
                bezier = Line.Trajectory.Cut(from, to);
                return true;
            }
            else
            {
                bezier = default;
                return false;
            }

        }
        public virtual void Render(OverlayData data)
        {
            if (GetTrajectory(out ITrajectory trajectory))
                trajectory.Render(data);
        }

        public override string ToString() => $"From [{From}] To [{To}]";
        public virtual XElement ToXml()
        {
            var config = new XElement(XmlSection);

            if (From.Value != null)
                config.Add(From.Value.ToXml());
            if (To.Value != null)
                config.Add(To.Value.ToXml());

            return config;
        }
        protected static IEnumerable<ILinePartEdge> GetEdges(XElement config, MarkingLine line, ObjectsMap map)
        {
            foreach (var supportConfig in config.Elements(LinePartEdge.XmlName))
            {
                if (LinePartEdge.FromXml(supportConfig, line, map, out ILinePartEdge edge))
                    yield return edge;
            }
        }
    }

    public class MarkupLineBound : TrajectoryBound
    {
        public MarkingRegularLine Line { get; }
        public MarkupLineBound(MarkingRegularLine line, float size) : base(line.Trajectory, size)
        {
            Line = line;
        }
    }
}
