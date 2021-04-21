using ModsCommon.Utilities;
using NodeMarkup.Tools;
using NodeMarkup.Utilities;
using System;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public interface ISupportPoint : IToXml, IEquatable<ISupportPoint>, IOverlay
    {
        Vector3 Position { get; }
        bool GetT(MarkupLine line, out float t);
        bool IsIntersect(Ray ray);
        void Update();
    }
    public enum SupportType
    {
        EnterPoint,
        LinesIntersect,
        CrosswalkBorder
    }

    public abstract class SupportPoint : ISupportPoint
    {
        protected static float DefaultWidth => 0.5f;
        protected static Vector3 MarkerSize { get; } = Vector3.one * DefaultWidth;
        private Bounds Bounds { get; set; }
        public Vector3 Position => Bounds.center;

        public abstract string XmlSection { get; }
        public abstract SupportType Type { get; }

        public abstract bool Equals(ISupportPoint other);
        public abstract bool GetT(MarkupLine line, out float t);

        public void Render(OverlayData data)
        {
            data.Width ??= DefaultWidth;
            Position.RenderCircle(data);
        }

        public bool IsIntersect(Ray ray) => Bounds.IntersectRay(ray);
        public abstract void Update();
        protected void Init(Vector3 position) => Bounds = new Bounds(position, MarkerSize);

        public virtual XElement ToXml()
        {
            var config = new XElement(XmlSection);
            config.AddAttr("T", (int)Type);
            return config;
        }
    }

    public abstract class EnterSupportPoint : SupportPoint, IEquatable<EnterSupportPoint>
    {
        public override SupportType Type { get; } = SupportType.EnterPoint;
        public MarkupPoint Point { get; }
        public Enter Enter => Point.Enter;

        public EnterSupportPoint(MarkupPoint point)
        {
            Point = point;
            Update();
        }
        public override bool GetT(MarkupLine line, out float t)
        {
            if (line.IsStart(Point))
            {
                t = 0;
                return true;
            }
            else if (line.IsEnd(Point))
            {
                t = 1;
                return true;
            }
            else
            {
                t = -1;
                return false;
            }  
        }
        public override bool Equals(ISupportPoint other) => other is EnterSupportPoint otherEnterPoint && Equals(otherEnterPoint);
        public virtual bool Equals(EnterSupportPoint other) => other.Point == Point;
        public override void Update() => Init(Point.Position);

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.AddAttr(MarkupPoint.XmlName, Point.Id);
            return config;
        }
        public override string ToString() => Point.ToString();
    }
    public abstract class IntersectSupportPoint : SupportPoint
    {
        public override SupportType Type { get; } = SupportType.LinesIntersect;
        public MarkupLinePair LinePair { get; set; }
        public MarkupLine First => LinePair.First;
        public MarkupLine Second => LinePair.Second;

        public IntersectSupportPoint(MarkupLinePair linePair)
        {
            LinePair = linePair;
            Update();
        }
        public IntersectSupportPoint(MarkupLine first, MarkupLine second) : this(new MarkupLinePair(first, second)) { }
        public override bool GetT(MarkupLine line, out float t)
        {
            var intersect = line.Markup.GetIntersect(LinePair);
            if (intersect.IsIntersect)
            {
                t = Mathf.Clamp01(intersect[line]);
                return true;
            }
            else
            {
                t = -1;
                return false;
            }
        }
        public override bool Equals(ISupportPoint other) => other is IntersectSupportPoint otherIntersect && otherIntersect.LinePair == LinePair;
        public override void Update() => Init(LinePair.Markup.GetIntersect(LinePair).Position);
        public new void Render(OverlayData data)
        {
            First.Render(data);
            Second.Render(data);
        }
        public override string ToString() => LinePair.ToString();
    }
}
