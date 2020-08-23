using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public abstract class MarkupLinePart : IToXml
    {
        public Action OnRuleChanged { private get; set; }

        ISupportPoint _from;
        ISupportPoint _to;
        public ISupportPoint From
        {
            get => _from;
            set
            {
                _from = value;
                RuleChanged();
            }
        }
        public ISupportPoint To
        {
            get => _to;
            set
            {
                _to = value;
                RuleChanged();
            }
        }
        public MarkupLine Line { get; }
        public abstract string XmlSection { get; }

        public MarkupLinePart(MarkupLine line, ISupportPoint from = null, ISupportPoint to = null)
        {
            Line = line;
            From = from;
            To = to;
        }

        protected void RuleChanged() => OnRuleChanged?.Invoke();
        public bool GetFromT(out float t) => GetT(From, out t);
        public bool GetToT(out float t) => GetT(To, out t);
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
        public bool GetTrajectory(out ILineTrajectory bezier)
        {
            var succes = false;
            succes |= GetFromT(out float from);
            succes |= GetToT(out float to);

            if (succes)
            {
                bezier = Line.Trajectory.Cut(from != -1 ? from : to, to != -1 ? to : from);
                return true;
            }
            else
            {
                bezier = default;
                return false;
            }

        }
        public virtual void Render(RenderManager.CameraInfo cameraInfo, Color color, float width)
        {
            if (GetTrajectory(out ILineTrajectory trajectory))
                NodeMarkupTool.RenderTrajectory(cameraInfo, color, trajectory, width);
        }

        public virtual XElement ToXml()
        {
            var config = new XElement(XmlSection);

            if (From != null)
                config.Add(From.ToXml());
            if (To != null)
                config.Add(To.ToXml());

            return config;
        }
        protected static IEnumerable<ILinePartEdge> GetEdges(XElement config, MarkupLine line, Dictionary<ObjectId, ObjectId> map)
        {
            foreach (var supportConfig in config.Elements(LinePartEdge.XmlName))
            {
                if (LinePartEdge.FromXml(supportConfig, line, map, out ILinePartEdge edge))
                    yield return edge;
            }
        }
    }
    public class MarkupLineBound
    {
        private static float Coef { get; } = Mathf.Sin(45 * Mathf.Deg2Rad);
        public MarkupLine Line { get; }
        public ILineTrajectory Trajectory => Line.Trajectory;
        public float Size { get; }
        private List<Bounds> BoundsList { get; } = new List<Bounds>();
        public IEnumerable<Bounds> Bounds => BoundsList;
        public MarkupLineBound(MarkupLine line, float size)
        {
            Line = line;
            Size = size;
            CalculateBounds();
        }

        private void CalculateBounds()
        {
            var size = Size * Coef;
            var t = 0f;
            while (t < 1f)
            {
                t = Line.Trajectory.Travel(t, size / 2);
                var bounds = new Bounds(Trajectory.Position(t), Vector3.one * size);
                BoundsList.Add(bounds);
            }
        }

        public bool IntersectRay(Ray ray) => BoundsList.Any(b => b.IntersectRay(ray));
        public bool Intersects(Bounds bounds) => BoundsList.Any(b => b.Intersects(bounds));
    }
}
