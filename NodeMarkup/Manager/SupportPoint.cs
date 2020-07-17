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
        SupportType Type { get; }
    }
    public interface IRuleEdge : ISupportPoint
    {
        bool GetT(MarkupLine line, out float t);
    }
    public interface IFillerVertex : ISupportPoint
    {
        List<IFillerVertex> Next(IFillerVertex prev);
    }
    public enum SupportType
    {
        EnterPoint,
        Line,
        Intersect
    }
    public abstract class SupportPointBase : ISupportPoint
    {
        public static string XmlName { get; } = "E";
        public static bool FromXml(XElement config, Markup markup, Dictionary<InstanceID, InstanceID> map, out SupportPointBase supportPoint)
        {
            var type = (SupportType)config.GetAttrValue<int>("T");
            switch (type)
            {
                case SupportType.Intersect when IntersectSupportPoint.FromXml(config, markup, map, out IntersectSupportPoint intersectPoint):
                    supportPoint = intersectPoint;
                    return true;
                case SupportType.Line when LineSupportPoint.FromXml(config, markup, map, out LineSupportPoint linePoint):
                    supportPoint = linePoint;
                    return true;
                case SupportType.EnterPoint when EnterSupportPoint.FromXml(config, markup, map, out EnterSupportPoint enterPoint):
                    supportPoint = enterPoint;
                    return true;
                default:
                    supportPoint = null;
                    return false;
            }
        }

        public string XmlSection => XmlName;
        public abstract SupportType Type { get; }

        public abstract bool Equals(ISupportPoint other);
        public virtual XElement ToXml()
        {
            var config = new XElement(XmlSection,
                new XAttribute("T", (int)Type)
            );
            return config;
        }

    }
    public class LineSupportPoint : SupportPointBase, IRuleEdge
    {
        public static bool FromXml(XElement config, Markup markup, Dictionary<InstanceID, InstanceID> map, out LineSupportPoint linePoint)
        {
            var lineId = config.GetAttrValue<ulong>(MarkupLine.XmlName);
            MarkupPointPair.FromHash(lineId, markup, map, out MarkupPointPair pair);
            if (markup.TryGetLine(pair.Hash, out MarkupLine line))
            {
                linePoint = new LineSupportPoint(line);
                return true;
            }
            else
            {
                linePoint = null;
                return false;
            }
        }

        public override SupportType Type { get; } = SupportType.Line;
        public MarkupLine Line { get; }

        public LineSupportPoint(MarkupLine line)
        {
            Line = line;
        }
        public bool GetT(MarkupLine line, out float t)
        {
            var pair = new MarkupLinePair(line, Line);
            var intersect = line.Markup.GetIntersect(pair);

            if (intersect.IsIntersect)
            {
                t = intersect[line];
                return true;
            }
            else
            {
                t = default;
                return false;
            }
        }
        public override string ToString() => string.Format(Localize.LineRule_IntersectWith, Line);

        public override bool Equals(ISupportPoint other) => other is LineSupportPoint otherLine && otherLine.Line == Line;

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute(MarkupLine.XmlName, Line.Id));
            return config;
        }
    }
    public class EnterSupportPoint : SupportPointBase, IRuleEdge, IFillerVertex
    {
        public static bool FromXml(XElement config, Markup markup, Dictionary<InstanceID, InstanceID> map, out EnterSupportPoint enterPoint)
        {
            var pointId = config.GetAttrValue<int>(MarkupPoint.XmlName);
            if (MarkupPoint.FromId(pointId, markup, map, out MarkupPoint point))
            {
                enterPoint = new EnterSupportPoint(point);
                return true;
            }
            else
            {
                enterPoint = null;
                return false;
            }
        }

        public override SupportType Type { get; } = SupportType.EnterPoint;
        public MarkupPoint Point { get; }

        public EnterSupportPoint(MarkupPoint point)
        {
            Point = point;
        }
        public bool GetT(MarkupLine line, out float t)
        {
            if (line.ContainPoint(Point))
            {
                t = line.PointPair.First == Point ? 0 : 1;
                return true;
            }
            else
            {
                t = default;
                return false;
            }
        }
        public override string ToString() => string.Format(Localize.LineRule_SelfEdgePoint, Point);

        public override bool Equals(ISupportPoint other) => other is EnterSupportPoint otherPoint && otherPoint.Point == Point;

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute(MarkupPoint.XmlName, Point.Id));
            return config;
        }

        public List<IFillerVertex> Next(IFillerVertex prev)
        {
            if(prev is EnterSupportPoint)
            {

            }
            else if(prev is IntersectSupportPoint)
            {

            }
            throw new NotImplementedException();
        }
    }
    public class IntersectSupportPoint : SupportPointBase, IFillerVertex
    {
        public static bool FromXml(XElement config, Markup markup, Dictionary<InstanceID, InstanceID> map, out IntersectSupportPoint intersectPoint)
        {
            throw new NotImplementedException();
        }

        public override SupportType Type { get; } = SupportType.Intersect;
        public MarkupLinePair LinePair { get; }

        public IntersectSupportPoint(MarkupLinePair linePair)
        {
            LinePair = linePair;
        }

        public override bool Equals(ISupportPoint other) => other is IntersectSupportPoint otherIntersect && otherIntersect.LinePair == LinePair;

        public List<IFillerVertex> Next(IFillerVertex prev)
        {
            throw new NotImplementedException();
        }
    }
    public class SupportPointBound
    {
        public static Vector3 MarkerSize { get; } = Vector3.one * 0.5f;
        public ISupportPoint SupportPoint { get; private set; }
        Bounds Bounds { get; set; }
        public Vector3 Position => Bounds.center;

        public SupportPointBound(ISupportPoint supportPoint, Bounds bounds)
        {
            SupportPoint = supportPoint;
            Bounds = bounds;
        }

        public bool IsIntersect(Ray ray) => Bounds.IntersectRay(ray);
    }
    public class RuleSupportPointBound : SupportPointBound
    {
        private static Bounds GetBounds(MarkupLine line, IRuleEdge ruleEdge)
        {
            ruleEdge.GetT(line, out float t);
            var position = line.Trajectory.Position(t);
            return new Bounds(position, MarkerSize);
        }
        public new IRuleEdge SupportPoint => (IRuleEdge)base.SupportPoint;
        public RuleSupportPointBound(MarkupLine line, IRuleEdge supportPoint) : base(supportPoint, GetBounds(line, supportPoint)) { }

    }
}
