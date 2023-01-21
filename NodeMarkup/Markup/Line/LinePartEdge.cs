using ModsCommon.Utilities;
using NodeMarkup.Utilities;
using System;
using System.Xml.Linq;

namespace NodeMarkup.Manager
{
    public interface ILinePartEdge : ISupportPoint, IEquatable<ILinePartEdge> { }

    public static class LinePartEdge
    {
        public static string XmlName { get; } = "E";
        public static bool FromXml(XElement config, MarkingLine mainLine, ObjectsMap map, out ILinePartEdge supportPoint)
        {
            var type = (SupportType)config.GetAttrValue<int>("T");
            switch (type)
            {
                case SupportType.EnterPoint when EnterPointEdge.FromXml(config, mainLine.Marking, map, out EnterPointEdge enterPoint):
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
        public static bool FromXml(XElement config, Marking marking, ObjectsMap map, out EnterPointEdge enterPoint)
        {
            var pointId = config.GetAttrValue<int>(MarkingPoint.XmlName);
            if (MarkingPoint.FromId(pointId, marking, map, out MarkingPoint point))
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

        public EnterPointEdge(MarkingPoint point) : base(point) { }

        bool IEquatable<ILinePartEdge>.Equals(ILinePartEdge other) => other is EnterSupportPoint otherEnter && Equals(otherEnter);

        public override string ToString() => string.Format(Localize.LineRule_SelfEdgePoint, Point);
    }

    public class LinesIntersectEdge : IntersectSupportPoint, ILinePartEdge
    {
        public static bool FromXml(XElement config, MarkingLine mainLine, ObjectsMap map, out LinesIntersectEdge linePoint)
        {
            var lineId = config.GetAttrValue<ulong>(MarkingLine.XmlName);
            if (mainLine.Marking.TryGetLine(lineId, map, out MarkingLine line))
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
        public MarkingLine Main => First;
        public MarkingLine Slave => Second;

        public LinesIntersectEdge(MarkingLinePair pair) : base(pair) { }
        public LinesIntersectEdge(MarkingLine first, MarkingLine second) : base(first, second) { }

        bool IEquatable<ILinePartEdge>.Equals(ILinePartEdge other) => other is IntersectSupportPoint otherIntersect && Equals(otherIntersect);

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.AddAttr(MarkingLine.XmlName, Slave.Id);
            return config;
        }

        public override string ToString() => string.Format(Localize.LineRule_IntersectWith, Second);
    }
    public class CrosswalkBorderEdge : SupportPoint, ISupportPoint, ILinePartEdge, IEquatable<CrosswalkBorderEdge>
    {
        public static bool FromXml(XElement config, MarkingLine line, ObjectsMap map, out CrosswalkBorderEdge borderPoint)
        {
            if (line is MarkingCrosswalkLine crosswalkLine)
            {
                var border = (config.GetAttrValue("B", (int)BorderPosition.Right) == (int)BorderPosition.Left) ^ map.Invert ? BorderPosition.Left : BorderPosition.Right;
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
        public MarkingCrosswalkLine CrosswalkLine { get; }
        public MarkingCrosswalk Crosswalk => CrosswalkLine.Crosswalk;
        public BorderPosition Border { get; }

        public CrosswalkBorderEdge(MarkingCrosswalkLine crosswalkLine, BorderPosition border) : base()
        {
            CrosswalkLine = crosswalkLine;
            Border = border;
            Update();
        }

        public override bool GetT(MarkingLine line, out float t)
        {
            if (line is MarkingCrosswalkLine crosswalkLine)
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
        public override void Update() => Init(CrosswalkLine.Trajectory.Position(CrosswalkLine.GetT(Border)));

        bool IEquatable<ILinePartEdge>.Equals(ILinePartEdge other) => other is CrosswalkBorderEdge otherBorder && Equals(otherBorder);
        public bool Equals(CrosswalkBorderEdge other) => other.Border == Border;

        public override XElement ToXml()
        {
            var config = base.ToXml();
            config.AddAttr("B", (int)Border);
            return config;
        }

        public new void Render(OverlayData data)
        {
            Crosswalk.Render(new OverlayData(data.CameraInfo) { Color = Colors.Hover });
            CrosswalkLine.Render(data);

            switch (Border)
            {
                case BorderPosition.Left:
                    Crosswalk.LeftBorderTrajectory.Render(data);
                    break;
                case BorderPosition.Right:
                    Crosswalk.RightBorderTrajectory.Render(data);
                    break;
            }
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
