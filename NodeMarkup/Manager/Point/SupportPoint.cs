using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public interface ISupportPoint : IToXml, IEquatable<ISupportPoint>
    {
        Vector3 Position { get; }
        ILinePartEdge GetPartEdge(MarkupLine line);
        bool GetT(MarkupLine line, out float t);
        bool IsIntersect(Ray ray);
    }
    public enum SupportType
    {
        EnterPoint,
        LinesIntersect
    }

    public abstract class SupportPoint : ISupportPoint
    {
        public static string XmlName { get; } = "S";
        protected static Vector3 MarkerSize { get; } = Vector3.one * 0.5f;
        Bounds Bounds { get;}
        public Vector3 Position => Bounds.center;

        public string XmlSection => XmlName;
        public abstract SupportType Type { get; }

        public SupportPoint(Vector3 position)
        {
            Bounds = new Bounds(position, MarkerSize);
        }

        public abstract bool Equals(ISupportPoint other);
        public abstract ILinePartEdge GetPartEdge(MarkupLine line);
        public abstract bool GetT(MarkupLine line, out float t);

        public bool IsIntersect(Ray ray) => Bounds.IntersectRay(ray);

        public virtual XElement ToXml()
        {
            var config = new XElement(XmlSection,
                new XAttribute("T", (int)Type)
            );
            return config;
        }
    }

    public class EnterSupportPoint : SupportPoint
    {
        public override SupportType Type { get; } = SupportType.EnterPoint;
        public MarkupPoint Point { get; }
        public Enter Enter => Point.Enter;

        public EnterSupportPoint(MarkupPoint point) : base(point.Position)
        {
            Point = point;
        }

        public override ILinePartEdge GetPartEdge(MarkupLine line) => new EnterPointEdge(Point);
        public override bool GetT(MarkupLine line, out float t)
        {
            if(line.ContainPoint(Point))
            {
                t = line.Start == Point ? 0 : 1;
                return true;
            }
            else
            {
                t = -1;
                return false;
            }
        }
        public override bool Equals(ISupportPoint other) => other is EnterSupportPoint otherEnterPoint && otherEnterPoint.Point == Point;

        public override string ToString() => string.Format(Localize.LineRule_SelfEdgePoint, Point);
    }
    public class IntersectSupportPoint : SupportPoint
    {
        public override SupportType Type { get; } = SupportType.LinesIntersect;
        public MarkupLinePair LinePair { get; set; }
        public MarkupLine First => LinePair.First;
        public MarkupLine Second => LinePair.Second;

        public IntersectSupportPoint(MarkupLinePair linePair) : base(linePair.Markup.GetIntersect(linePair).Position)
        {
            LinePair = linePair;
        }
        public IntersectSupportPoint(MarkupLine first, MarkupLine second) : this(new MarkupLinePair(first, second)) { }

        public override ILinePartEdge GetPartEdge(MarkupLine line) => new LinesIntersectEdge(LinePair.GetOther(line));
        public override bool GetT(MarkupLine line, out float t)
        {
            var intersect = line.Markup.GetIntersect(LinePair);
            if(intersect.IsIntersect)
            {
                t = intersect[line];
                return true;
            }
            else
            {
                t = -1;
                return false;
            }
        }
        public override bool Equals(ISupportPoint other) => other is IntersectSupportPoint otherIntersect && otherIntersect.LinePair == LinePair;
        

        public override string ToString() => string.Format(Localize.LineRule_IntersectWith, Second);
    }
}
