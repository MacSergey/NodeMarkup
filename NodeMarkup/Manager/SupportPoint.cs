using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public interface ISupportPoint : IEquatable<ISupportPoint>
    {
        Vector3 Position { get; }
        ILinePartEdge GetPartEdge(MarkupLine line);
        bool IsIntersect(Ray ray);
    }

    public abstract class SupportPoint : ISupportPoint
    {
        protected static Vector3 MarkerSize { get; } = Vector3.one * 0.5f;
        Bounds Bounds { get;}
        public Vector3 Position => Bounds.center;

        public SupportPoint(Vector3 position)
        {
            Bounds = new Bounds(position, MarkerSize);
        }

        public abstract bool Equals(ISupportPoint other);
        public abstract ILinePartEdge GetPartEdge(MarkupLine line);

        public bool IsIntersect(Ray ray) => Bounds.IntersectRay(ray);
    }

    public class EnterSupportPoint : SupportPoint
    {
        public MarkupPoint Point { get; }

        public EnterSupportPoint(MarkupPoint point) : base(point.Position)
        {
            Point = point;
        }

        public override bool Equals(ISupportPoint other) => other is EnterSupportPoint otherEnterPoint && otherEnterPoint.Point == Point;

        public override ILinePartEdge GetPartEdge(MarkupLine line) => new EnterPointEdge(Point);

        public override string ToString() => string.Format(Localize.LineRule_SelfEdgePoint, Point);
    }
    public class IntersectSupportPoint : SupportPoint
    {
        public MarkupLinePair LinePair { get; set; }

        public IntersectSupportPoint(MarkupLinePair linePair) : base(linePair.Markup.GetIntersect(linePair).Position)
        {
            LinePair = linePair;
        }
        public IntersectSupportPoint(MarkupLine first, MarkupLine second) : this(new MarkupLinePair(first, second)) { }

        public override bool Equals(ISupportPoint other) => other is IntersectSupportPoint otherIntersect && otherIntersect.LinePair == LinePair;

        public override ILinePartEdge GetPartEdge(MarkupLine line) => new LinesIntersectEdge(LinePair.GetOther(line));


    }
}
