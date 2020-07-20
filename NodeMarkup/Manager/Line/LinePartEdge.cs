using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public interface ILinePartEdge : IToXml, IEquatable<ILinePartEdge>
    {
        bool GetT(MarkupLine line, out float t);
        ISupportPoint GetSupport(MarkupLine line);
    }
    public enum EdgeType
    {
        EnterPoint,
        LinesIntersect
    }

    public abstract class LinePartEdge : ILinePartEdge
    {
        public static string XmlName { get; } = "E";
        public static bool FromXml(XElement config, Markup markup, Dictionary<InstanceID, InstanceID> map, out LinePartEdge supportPoint)
        {
            var type = (EdgeType)config.GetAttrValue<int>("T");
            switch (type)
            {
                case EdgeType.LinesIntersect when LinesIntersectEdge.FromXml(config, markup, map, out LinesIntersectEdge linePoint):
                    supportPoint = linePoint;
                    return true;
                case EdgeType.EnterPoint when EnterPointEdge.FromXml(config, markup, map, out EnterPointEdge enterPoint):
                    supportPoint = enterPoint;
                    return true;
                default:
                    supportPoint = null;
                    return false;
            }
        }

        public string XmlSection => XmlName;
        public abstract EdgeType Type { get; }

        public abstract bool GetT(MarkupLine line, out float t);
        public abstract bool Equals(ILinePartEdge other);
        public virtual XElement ToXml()
        {
            var config = new XElement(XmlSection,
                new XAttribute("T", (int)Type)
            );
            return config;
        }

        public abstract ISupportPoint GetSupport(MarkupLine line);
    }
    public class EnterPointEdge : LinePartEdge
    {
        public static bool FromXml(XElement config, Markup markup, Dictionary<InstanceID, InstanceID> map, out EnterPointEdge enterPoint)
        {
            var pointId = config.GetAttrValue<int>(MarkupPoint.XmlName);
            if (MarkupPoint.FromId(pointId, markup, map, out MarkupPoint point))
            {
                enterPoint = new EnterPointEdge(point);
                return true;
            }
            else
            {
                enterPoint = null;
                return false;
            }
        }

        public override EdgeType Type { get; } = EdgeType.EnterPoint;
        public MarkupPoint Point { get; }
        public Vector3 Position => Point.Position;

        public EnterPointEdge(MarkupPoint point)
        {
            Point = point;
        }
        public override bool GetT(MarkupLine line, out float t)
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

        public override bool Equals(ILinePartEdge other) => other is EnterPointEdge otherPoint && otherPoint.Point == Point;

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute(MarkupPoint.XmlName, Point.Id));
            return config;
        }

        public override ISupportPoint GetSupport(MarkupLine line) => new EnterSupportPoint(Point);
    }
    public class LinesIntersectEdge : LinePartEdge
    {
        public static bool FromXml(XElement config, Markup markup, Dictionary<InstanceID, InstanceID> map, out LinesIntersectEdge linePoint)
        {
            var lineId = config.GetAttrValue<ulong>(MarkupLine.XmlName);
            MarkupPointPair.FromHash(lineId, markup, map, out MarkupPointPair pair);
            if (markup.TryGetLine(pair.Hash, out MarkupLine line))
            {
                linePoint = new LinesIntersectEdge(line);
                return true;
            }
            else
            {
                linePoint = null;
                return false;
            }
        }

        public override EdgeType Type { get; } = EdgeType.LinesIntersect;
        public MarkupLine Line { get; }

        public LinesIntersectEdge(MarkupLine line)
        {
            Line = line;
        }
        public override bool GetT(MarkupLine line, out float t)
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

        public override bool Equals(ILinePartEdge other) => other is LinesIntersectEdge otherLine && otherLine.Line == Line;

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute(MarkupLine.XmlName, Line.Id));
            return config;
        }

        public override ISupportPoint GetSupport(MarkupLine line) => new IntersectSupportPoint(Line, line);
    }
}
