using ColossalFramework.PlatformServices;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public class MarkupPoint : IToXml, IFromXml
    {
        public static string XmlName { get; } = "P";
        public static bool FromId(int id, Markup markup, Dictionary<InstanceID, InstanceID> map, out MarkupPoint point)
        {
            point = null;

            var enterId = (ushort)id;
            var num = (byte)(id >> 16);

            if(map != null)
            {
                var source = new InstanceID() { Type = InstanceType.NetSegment, NetSegment = enterId };
                if (map.TryGetValue(source, out InstanceID target))
                    enterId = target.NetSegment;
                else
                    return false;
            }

            return markup.TryGetEnter(enterId, out Enter enter) && enter.TryGetPoint(num, out point);
        }

        float _offset = 0;

        public byte Num { get; }
        public int Id { get; }
        public Color32 Color => Markup.OverlayColors[(Num - 1) % Markup.OverlayColors.Length];

        public static Vector3 MarkerSize { get; } = Vector3.one * 1f;
        public Vector3 Position { get; private set; }
        public Vector3 Direction { get; private set; }
        public Type PointType { get; private set; }
        public Bounds Bounds { get; private set; }

        SegmentMarkupLine MarkupLine { get; }
        public Enter Enter => MarkupLine.SegmentEnter;
        public IEnumerable<MarkupLine> Lines => Markup.GetLinesFromPoint(this);
        public Markup Markup => Enter.Markup;

        public bool IsFirst => Num == 1;
        public bool IsLast => Num == Enter.PointCount;
        public bool IsEdge => IsFirst || IsLast;

        public float Offset
        {
            get => _offset;
            set
            {
                _offset = value;
                Markup.Update(this);
            }
        }

        public string XmlSection => XmlName;

        public MarkupPoint(SegmentMarkupLine markupLine, Type pointType)
        {
            MarkupLine = markupLine;
            PointType = pointType;
            Num = Enter.PointNum;
            Id = Enter.Id + (Num << 16);

            Update();
        }

        public void Update()
        {
            MarkupLine.GetPositionAndDirection(PointType, Offset, out Vector3 position, out Vector3 direction);
            Position = position;
            Direction = direction;
            Bounds = new Bounds(Position, MarkerSize);
        }
        public bool IsIntersect(Ray ray) => Bounds.IntersectRay(ray);

        public enum Type
        {
            None = 0,
            Edge = 1,
            LeftEdge = 2 + Edge,
            RightEdge = 4 + Edge,
            Between = 8,
            BetweenSomeDir = 16 + Between,
            BetweenDiffDir = 32 + Between,
        }

        public override string ToString() => $"{Enter}-{Num}";
        public override int GetHashCode() => Id;

        public XElement ToXml()
        {
            var config = new XElement(XmlSection,
                new XAttribute(nameof(Id), Id),
                new XAttribute("O", Offset)
            );
            return config;
        }
        public static void FromXml(XElement config, Markup markup, Dictionary<InstanceID, InstanceID> map)
        {
            var id = config.GetAttrValue<int>(nameof(Id));
            if (FromId(id, markup, map, out MarkupPoint point))
                point.FromXml(config);
        }
        public void FromXml(XElement config)
        {
            _offset = config.GetAttrValue<float>("O");
        }
    }
    public struct MarkupPointPair
    {
        public static string XmlName { get; } = "PP";
        public static string XmlName1 { get; } = "L1";
        public static string XmlName2 { get; } = "L2";
        public static bool FromHash(ulong hash, Markup markup, Dictionary<InstanceID, InstanceID> map, out MarkupPointPair pair)
        {
            var firstId = (int)hash;
            var secondId = (int)(hash >> 32);

            if (MarkupPoint.FromId(firstId, markup, map, out MarkupPoint first) && MarkupPoint.FromId(secondId, markup, map, out MarkupPoint second))
            {
                pair = new MarkupPointPair(first, second);
                return true;
            }
            else
            {
                pair = default;
                return false;
            }
        }

        public ulong Hash { get; }
        public MarkupPoint First { get; }
        public MarkupPoint Second { get; }
        public bool IsSomeEnter => First.Enter == Second.Enter;

        public MarkupPointPair(MarkupPoint first, MarkupPoint second)
        {
            First = first;
            Second = second;
            Hash = ((ulong)Math.Max(First.Id, Second.Id)) + (((ulong)Math.Min(First.Id, Second.Id)) << 32);
        }

        public string XmlSection => XmlName;

        public bool ContainPoint(MarkupPoint point) => First == point || Second == point;
        public MarkupPoint GetOther(MarkupPoint point)
        {
            if (!ContainPoint(point))
                return null;
            else
                return point == First ? Second : First;
        }

        public override string ToString() => $"{First}—{Second}";


    }
    public class MarkupPointPairComparer : IEqualityComparer<MarkupPointPair>
    {
        public bool Equals(MarkupPointPair x, MarkupPointPair y) => x.Hash == y.Hash;

        public int GetHashCode(MarkupPointPair pair) => pair.GetHashCode();
    }
}
