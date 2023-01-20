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
        bool GetT(MarkingLine line, out float t);
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

        bool IEquatable<ISupportPoint>.Equals(ISupportPoint other) => true;
        public abstract bool GetT(MarkingLine line, out float t);

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

    public abstract class EnterSupportPoint : SupportPoint, ISupportPoint, IEquatable<EnterSupportPoint>
    {
        public override SupportType Type { get; } = SupportType.EnterPoint;
        public MarkingPoint Point { get; }
        public Entrance Enter => Point.Enter;

        public EnterSupportPoint(MarkingPoint point)
        {
            Point = point;
            Update();
        }
        public override bool GetT(MarkingLine line, out float t)
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

        public override void Update() => Init(Point.Position);

        bool IEquatable<ISupportPoint>.Equals(ISupportPoint other) => other is EnterSupportPoint otherEnterPoint && Equals(otherEnterPoint);
        public bool Equals(EnterSupportPoint other) => other.Point == Point;

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.AddAttr(MarkingPoint.XmlName, Point.Id);
            return config;
        }
        public override string ToString() => Point.ToString();
    }
    public abstract class IntersectSupportPoint : SupportPoint, ISupportPoint, IEquatable<IntersectSupportPoint>
    {
        public override SupportType Type { get; } = SupportType.LinesIntersect;
        public MarkingLinePair LinePair { get; set; }
        public MarkingLine First => LinePair.First;
        public MarkingLine Second => LinePair.Second;

        public IntersectSupportPoint(MarkingLinePair linePair)
        {
            LinePair = linePair;
            Update();
        }
        public IntersectSupportPoint(MarkingLine first, MarkingLine second) : this(new MarkingLinePair(first, second)) { }
        public override bool GetT(MarkingLine line, out float t)
        {
            var intersect = line.Marking.GetIntersect(LinePair);
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
        public override void Update() => Init(LinePair.Markup.GetIntersect(LinePair).Position);

        bool IEquatable<ISupportPoint>.Equals(ISupportPoint other) => other is IntersectSupportPoint otherIntersect && Equals(otherIntersect);
        public bool Equals(IntersectSupportPoint other) => other.LinePair == LinePair;

        public new void Render(OverlayData data)
        {
            First.Render(data);
            Second.Render(data);
        }
        public override string ToString() => LinePair.ToString();
    }
}
