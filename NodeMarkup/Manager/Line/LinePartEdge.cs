using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public interface ILinePartEdge : ISupportPoint, IEquatable<ILinePartEdge> { }

    public static class LinePartEdge
    {
        public static string XmlName { get; } = "E";
        public static bool FromXml(XElement config, MarkupLine mainLine, Dictionary<ObjectId, ObjectId> map, out ILinePartEdge supportPoint)
        {
            var type = (SupportType)config.GetAttrValue<int>("T");
            switch (type)
            {
                case SupportType.EnterPoint when EnterPointEdge.FromXml(config, mainLine.Markup, map, out EnterPointEdge enterPoint):
                    supportPoint = enterPoint;
                    return true;
                case SupportType.LinesIntersect when LinesIntersectEdge.FromXml(config, mainLine, map, out LinesIntersectEdge linePoint):
                    supportPoint = linePoint;
                    return true;
                case SupportType.CrosswalkBorder when CrosswalkBorderEdge.FromXml(config, mainLine, map, out CrosswalkBorderEdge borderPoint):
                    supportPoint = borderPoint;
                    return true;
                default:
                    supportPoint = null;
                    return false;
            }
        }
    }
    public class EnterPointEdge : EnterSupportPoint, ILinePartEdge
    {
        public static bool FromXml(XElement config, Markup markup, Dictionary<ObjectId, ObjectId> map, out EnterPointEdge enterPoint)
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

        public override string XmlSection => LinePartEdge.XmlName;

        public EnterPointEdge(MarkupPoint point) : base(point) { }

        public bool Equals(ILinePartEdge other) => Equals((ISupportPoint)other);

        public override string ToString() => string.Format(Localize.LineRule_SelfEdgePoint, Point);
    }

    public class LinesIntersectEdge : IntersectSupportPoint, ILinePartEdge
    {
        public static bool FromXml(XElement config, MarkupLine mainLine, Dictionary<ObjectId, ObjectId> map, out LinesIntersectEdge linePoint)
        {
            var lineId = config.GetAttrValue<ulong>(MarkupLine.XmlName);
            if (mainLine.Markup.TryGetLine(lineId, map, out MarkupLine line))
            {
                linePoint = new LinesIntersectEdge(mainLine, line);
                return true;
            }
            else
            {
                linePoint = null;
                return false;
            }
        }

        public override string XmlSection => LinePartEdge.XmlName;
        public MarkupLine Main => First;
        public MarkupLine Slave => Second;

        public LinesIntersectEdge(MarkupLine first, MarkupLine second) : base(first, second) { }

        public bool Equals(ILinePartEdge other) => Equals((ISupportPoint)other);

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute(MarkupLine.XmlName, Slave.Id));
            return config;
        }

        public override string ToString() => string.Format(Localize.LineRule_IntersectWith, Second);
    }
    public class CrosswalkBorderEdge : SupportPoint, ILinePartEdge
    {
        public static bool FromXml(XElement config, MarkupLine line, Dictionary<ObjectId, ObjectId> map, out CrosswalkBorderEdge borderPoint)
        {            
            if (line is MarkupCrosswalkLine crosswalkLine)
            {
                var border = (BorderPosition)config.GetAttrValue("B", (int)BorderPosition.Right);
                borderPoint = new CrosswalkBorderEdge(crosswalkLine, border);
                return true;
            }
            else
            {
                borderPoint = null;
                return false;
            }
        }

        public override string XmlSection => LinePartEdge.XmlName;
        public override SupportType Type => SupportType.CrosswalkBorder;
        public MarkupCrosswalkLine CrosswalkLine { get; }
        public BorderPosition Border { get; }

        public CrosswalkBorderEdge(MarkupCrosswalkLine crosswalkLine, BorderPosition border) : base(crosswalkLine.Trajectory.Position(crosswalkLine.GetT(border)))
        {
            CrosswalkLine = crosswalkLine;
            Border = border;
        }

        public override bool GetT(MarkupLine line, out float t)
        {
            if (line is MarkupCrosswalkLine crosswalkLine)
            {
                t = crosswalkLine.GetT(Border);
                return true;
            }
            else
            {
                t = -1;
                return false;
            }
        }

        public new void Render(RenderManager.CameraInfo cameraInfo, Color color)
        {

        }

        public override bool Equals(ISupportPoint other) => other is CrosswalkBorderEdge otherBorder && otherBorder.Border == Border;
        public bool Equals(ILinePartEdge other) => Equals((ISupportPoint)other);

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.Add(new XAttribute("B", (int)Border));
            return config;
        }
        public override string ToString() => Border == BorderPosition.Right ? Localize.LineRule_RightBorder : Localize.LineRule_LeftBorder;
    }
    public enum EdgePosition
    {
        Start,
        End
    }
    public enum BorderPosition
    {
        Right,
        Left
    }
}
