using NodeMarkup.Utils;
using System;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public interface ISupportPoint : IToXml, IEquatable<ISupportPoint>
    {
        Vector3 Position { get; }
        bool GetT(MarkupLine line, out float t);
        bool IsIntersect(Ray ray);
        void Render(RenderManager.CameraInfo cameraInfo, Color color);
    }
    public enum SupportType
    {
        EnterPoint,
        LinesIntersect,
        CrosswalkBorder
    }

    public abstract class SupportPoint : ISupportPoint
    {
        protected static Vector3 MarkerSize { get; } = Vector3.one * 0.5f;
        Bounds Bounds { get;}
        public Vector3 Position => Bounds.center;

        public abstract string XmlSection { get; }
        public abstract SupportType Type { get; }

        public SupportPoint(Vector3 position)
        {
            Bounds = new Bounds(position, MarkerSize);
        }

        public abstract bool Equals(ISupportPoint other);
        public abstract bool GetT(MarkupLine line, out float t);
        public void Render(RenderManager.CameraInfo cameraInfo, Color color) => NodeMarkupTool.RenderCircle(cameraInfo, color, Position, 0.5f);

        public bool IsIntersect(Ray ray) => Bounds.IntersectRay(ray);

        public virtual XElement ToXml()
        {
            var config = new XElement(XmlSection,
                new XAttribute("T", (int)Type)
            );
            return config;
        }
    }

    public abstract class EnterSupportPoint : SupportPoint
    {
        public override SupportType Type { get; } = SupportType.EnterPoint;
        public MarkupPoint Point { get; }
        public Enter Enter => Point.Enter;

        public EnterSupportPoint(MarkupPoint point) : base(point.Position)
        {
            Point = point;
        }
        public override bool GetT(MarkupLine line, out float t)
        {
            if(line.ContainsPoint(Point))
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

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute(MarkupPoint.XmlName, Point.Id));
            return config;
        }
    }
    public abstract class IntersectSupportPoint : SupportPoint
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
        public new void Render(RenderManager.CameraInfo cameraInfo, Color color)
        {
            First.Render(cameraInfo, color);
            Second.Render(cameraInfo, color);
        }
    }
}
