using ColossalFramework.Math;
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
    public abstract class MarkupPoint : IToXml, IFromXml
    {
        public event Action<MarkupPoint> OnUpdate;
        static int GetId(ushort enter, byte num, PointType type) => enter + (num << 16) + ((int)type << 24);
        static ushort GetEnter(int id) => (ushort)id;
        static byte GetNum(int id) => (byte)(id >> 16);
        static PointType GetType(int id) => (PointType)(id >> 24);
        public static string XmlName { get; } = "P";
        public static bool FromId(int id, Markup markup, Dictionary<ObjectId, ObjectId> map, out MarkupPoint point)
        {
            point = null;

            var enterId = GetEnter(id);
            var num = GetNum(id);
            var type = GetType(id);

            if (map != null)
            {
                if (map.TryGetValue(new ObjectId() { Segment = enterId }, out ObjectId targetSegment))
                    enterId = targetSegment.Segment;
                if (map.TryGetValue(new ObjectId() { Point = GetId(enterId, num, type) }, out ObjectId targetPoint))
                    num = GetNum(targetPoint.Point);
            }

            return markup.TryGetEnter(enterId, out Enter enter) && enter.TryGetPoint(num, type, out point);
        }

        float _offset = 0;
        public float Offset
        {
            get => _offset;
            set
            {
                _offset = value;
                Markup.Update(this);
            }
        }

        public byte Num { get; }
        public int Id { get; }
        public abstract PointType Type { get; }
        public Color32 Color => Markup.OverlayColors[(Num - 1) % Markup.OverlayColors.Length];

        public static Vector3 MarkerSize { get; } = Vector3.one * 1f;
        public Vector3 Position
        {
            get => Bounds.center;
            protected set => Bounds = new Bounds(value, MarkerSize);
        }
        public Vector3 Direction { get; protected set; }
        public LocationType Location { get; private set; }
        public Bounds Bounds { get; protected set; }

        public SegmentMarkupLine SegmentLine { get; }
        public Enter Enter => SegmentLine.Enter;
        public IEnumerable<MarkupLine> Lines => Markup.GetPointLines(this);
        public Markup Markup => Enter.Markup;

        public bool IsFirst => Num == 1;
        public bool IsLast => Num == Enter.PointCount;
        public bool IsEdge => IsFirst || IsLast;


        public string XmlSection => XmlName;


        protected MarkupPoint(byte num, SegmentMarkupLine markupLine, LocationType location, bool update = true)
        {
            SegmentLine = markupLine;
            Location = location;
            Num = num;
            Id = GetId(Enter.Id, Num, Type);

            if (update)
                Update();
        }
        public MarkupPoint(SegmentMarkupLine segmentLine, LocationType location) : this(segmentLine.Enter.PointNum, segmentLine, location) { }

        public void Update()
        {
            UpdateProcess();
            OnUpdate?.Invoke(this);
        }
        public abstract void UpdateProcess();
        public bool IsIntersect(Ray ray) => Bounds.IntersectRay(ray);
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
        public static void FromXml(XElement config, Markup markup, Dictionary<ObjectId, ObjectId> map)
        {
            var id = config.GetAttrValue<int>(nameof(Id));
            if (FromId(id, markup, map, out MarkupPoint point))
                point.FromXml(config);
        }
        public void FromXml(XElement config)
        {
            _offset = config.GetAttrValue<float>("O");
        }

        public enum PointType
        {
            Enter = 0,
            Crosswalk = 1,
            Normal = 2,
        }
        public enum LocationType
        {
            None = 0,
            Edge = 1,
            LeftEdge = 2 + Edge,
            RightEdge = 4 + Edge,
            Between = 8,
            BetweenSomeDir = 16 + Between,
            BetweenDiffDir = 32 + Between,
        }
    }

    public class MarkupEnterPoint : MarkupPoint
    {
        public override PointType Type => PointType.Enter;
        public MarkupEnterPoint(SegmentMarkupLine markupLine, LocationType location) : base(markupLine, location)
        {
        }
        public override void UpdateProcess()
        {
            SegmentLine.GetPositionAndDirection(Location, Offset, out Vector3 position, out Vector3 direction);
            Position = position;
            Direction = direction;
        }
    }
    public class MarkupCrosswalkPoint : MarkupPoint
    {
        public override PointType Type => PointType.Crosswalk;
        public MarkupCrosswalkPoint(byte num, SegmentMarkupLine markupLine, LocationType location) : base(num, markupLine, location) { }

        public override void UpdateProcess()
        {
            SegmentLine.GetPositionAndDirection(Location, Offset, out Vector3 position, out Vector3 direction);
            Position = position + direction * 2;
            Direction = direction;
        }
    }
    public class MarkupNormalPoint : MarkupPoint
    {
        public override PointType Type => PointType.Normal;
        public MarkupEnterPoint SourcePoint { get; }
        public MarkupNormalPoint(MarkupEnterPoint sourcePoint) : base(sourcePoint.Num, sourcePoint.SegmentLine, sourcePoint.Location, false)
        {
            SourcePoint = sourcePoint;
            SourcePoint.OnUpdate += SourcePointUpdate;
        }

        private void SourcePointUpdate(MarkupPoint point) => UpdateProcess();

        public override void UpdateProcess()
        {
            var ts = new HashSet<float>();
            foreach(var enter in Markup.Enters)
            {
                if (Line2.Intersect(enter.LeftSide.XZ(), enter.RightSide.XZ(), SourcePoint.Position.XZ(), (SourcePoint.Position + SourcePoint.Direction).XZ(), out float u, out float v)
                    && 0 <= u && u <= 1)
                    ts.Add(v);
            }
            foreach(var prev in Markup.Enters)
            {
                var next = Markup.GetNextEnter(prev);
                var betweenBezier = new Bezier3()
                {
                    a = prev.RightSide,
                    d = next.LeftSide
                };
                NetSegment.CalculateMiddlePoints(betweenBezier.a, prev.NormalDir, betweenBezier.d, next.NormalDir, true, true, out betweenBezier.b, out betweenBezier.c);
                var intersects = MarkupFillerIntersect.Intersect(betweenBezier, SourcePoint.Position, SourcePoint.Position + SourcePoint.Direction);
                foreach(var intersect in intersects)
                    ts.Add(intersect.FirstT);
            }

            var t = ts.Count == 0 ? 0 : ts.Count == 1 ? ts.First() : ts.OrderBy(i => i).Skip(1).First();

            Position = SourcePoint.Position + SourcePoint.Direction * t;
            Direction = -SourcePoint.Direction;
        }
    }

    public struct MarkupPointPair
    {
        public static string XmlName { get; } = "PP";
        public static string XmlName1 { get; } = "L1";
        public static string XmlName2 { get; } = "L2";
        public static bool FromHash(ulong hash, Markup markup, Dictionary<ObjectId, ObjectId> map, out MarkupPointPair pair)
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
        public bool IsNormal => First.Type == MarkupPoint.PointType.Normal || Second.Type == MarkupPoint.PointType.Normal;

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
        public MarkupLine.LineType DefaultType => IsSomeEnter ? MarkupLine.LineType.Stop : MarkupLine.LineType.Regular;

        public override string ToString() => $"{First}—{Second}";

        public override bool Equals(object obj) => obj is MarkupPointPair other && other == this;

        public override int GetHashCode() => Hash.GetHashCode();
        public static bool operator ==(MarkupPointPair x, MarkupPointPair y) => x.Hash == y.Hash;
        public static bool operator !=(MarkupPointPair x, MarkupPointPair y) => x.Hash != y.Hash;
    }
    public class MarkupPointPairComparer : IEqualityComparer<MarkupPointPair>
    {
        public bool Equals(MarkupPointPair x, MarkupPointPair y) => x.Hash == y.Hash;

        public int GetHashCode(MarkupPointPair pair) => pair.GetHashCode();
    }
}
