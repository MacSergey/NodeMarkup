using ColossalFramework.Math;
using IMT.Utilities;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using ObjectId = IMT.Utilities.ObjectId;

namespace IMT.Manager
{
    public abstract class MarkingPoint : IItem, IToXml, ISupport
    {
        public event Action<MarkingPoint> OnUpdate;
        public static int GetId(ushort enter, byte index, PointType type) => enter + (index << 16) + ((int)type >> 1 << 24);
        public static ushort GetEnter(int id) => (ushort)id;
        public static byte GetIndex(int id) => (byte)(id >> 16);
        public static PointType GetType(int id) => (PointType)(id >> 24 == 0 ? (int)PointType.Enter : id >> 24 << 1);
        public static string XmlName { get; } = "P";
        private static Vector3 MarkerSize => Vector3.one;
        protected static float DefaultWidth => 1f;

        public static bool FromId(int id, Marking marking, ObjectsMap map, out MarkingPoint point)
        {
            point = null;

            var enterId = GetEnter(id);
            var index = GetIndex(id);
            var type = GetType(id);

            switch (marking.Type)
            {
                case MarkingType.Node when map.TryGetValue(new ObjectId() { Segment = enterId }, out ObjectId targetSegment):
                    enterId = targetSegment.Segment;
                    break;
                case MarkingType.Segment when map.TryGetValue(new ObjectId() { Node = enterId }, out ObjectId targetNode):
                    enterId = targetNode.Node;
                    break;
            }

            if (map.TryGetValue(new ObjectId() { Point = GetId(enterId, index, type) }, out ObjectId targetPoint))
                index = GetIndex(targetPoint.Point);

            return marking.TryGetEnter(enterId, out Entrance enter) && enter.TryGetPoint(index, type, out point);
        }

        public string DeleteCaptionDescription => Localize.PointEditor_DeleteCaptionDescription;
        public string DeleteMessageDescription => Localize.PointEditor_DeleteMessageDescription;
        public Marking.SupportType Support => Marking.SupportType.Points;

        public PropertyValue<float> Offset { get; }

        public byte Index { get; }
        public int Id { get; }
        public abstract PointType Type { get; }
        public NetworkType NetworkType => Source.NetworkType;
        public virtual Color32 Color => Colors.GetOverlayColor(Index - 1, byte.MaxValue);
        public virtual Vector3 Position
        {
            get => MarkerPosition;
            protected set => MarkerPosition = value;
        }
        public virtual Vector3 MarkerPosition
        {
            get => Bounds.center;
            set => Bounds = new Bounds(value, MarkerSize);
        }
        public Vector3 Direction { get; protected set; }
        protected Bounds Bounds { get; set; }

        public IPointSource Source { get; }
        public Entrance Enter => Source.Enter;
        public IEnumerable<MarkingLine> Lines => Marking.GetPointLines(this);
        public Marking Marking => Enter.Marking;

        public bool IsFirst => Index == 1;
        public bool IsLast => Index == Enter.PointCount;
        public virtual bool IsSplit => false;
        public virtual float SplitOffsetValue => 0f;


        public string XmlSection => XmlName;


        protected MarkingPoint(byte index, IPointSource source, bool update)
        {
            Offset = new PropertyStructValue<float>("O", PointChanged, 0);
            Source = source;

            Index = index;
            Id = GetId(Enter.Id, Index, Type);

            if (update)
                Update();
        }

        public void Update(bool onlySelfUpdate = false)
        {
            UpdateProcess();
            OnUpdate?.Invoke(this);
        }
        public abstract void UpdateProcess();
        public virtual void Reset()
        {
            Offset.Value = 0f;
        }

        public virtual bool IsHover(Ray ray) => Bounds.IntersectRay(ray);
        public override string ToString() => $"{Enter}:{Index}";
        public override int GetHashCode() => Id;
        protected void PointChanged() => Marking.Update(this, true, true);

        public virtual Vector3 GetAbsolutePosition(Alignment alignment)
        {
            if (IsSplit && alignment != Alignment.Centre)
            {
                var direction = -Enter.CornerDir / Enter.TranformRatio;
                var shift = SplitOffsetValue * alignment.Sign();
                return Position + direction * shift;
            }
            else
                return Position;
        }
        public virtual float GetRelativePosition()
        {
            return Source.GetRelativePosition(Offset);
        }
        public virtual float GetRelativePosition(Alignment alignment)
        {
            if (IsSplit && alignment != Alignment.Centre)
                return Source.GetRelativePosition(Offset + SplitOffsetValue * alignment.Sign());
            else
                return Source.GetRelativePosition(Offset);
        }

        public Dependences GetDependences() => throw new NotSupportedException();
        public virtual void Render(OverlayData data)
        {
            data.Color ??= Color;
            data.Width ??= DefaultWidth;
            Position.RenderCircle(data);
        }

        public virtual XElement ToXml()
        {
            var config = new XElement(XmlSection);
            config.AddAttr(nameof(Id), Id);
            Offset.ToXml(config);
            return config;
        }
        public static void FromXml(XElement config, Marking marking, ObjectsMap map)
        {
            var id = config.GetAttrValue<int>(nameof(Id));
            if (FromId(id, marking, map, out MarkingPoint point))
                point.FromXml(config, map);
        }
        public virtual void FromXml(XElement config, ObjectsMap map)
        {
            Offset.FromXml(config, 0);
            Offset.Value *= (map.Invert ? -1 : 1);
        }

        public enum PointType
        {
            Enter = 1,
            Crosswalk = 2,
            Normal = 4,
            Lane = 8,

            [NotItem]
            All = Enter | Crosswalk | Normal | Lane,
        }
        public enum LocationType
        {
            None = 0,
            Edge = 1,
            LeftEdge = 2 | Edge,
            RightEdge = 4 | Edge,
            Between = 8,
            BetweenSomeDir = 16 | Between,
            BetweenDiffDir = 32 | Between,
        }
    }

    public class MarkingEnterPoint : MarkingPoint
    {
        public static float DefaultSplitOffse => 0.5f;

        public override PointType Type => PointType.Enter;
        public new NetInfoPointSource Source => (NetInfoPointSource)base.Source;

        public override bool IsSplit => Split;
        public override float SplitOffsetValue => SplitOffset;
        public Color32 SplitColor => Colors.GetOverlayColor(Index - 1, byte.MaxValue, 128);

        public PropertyBoolValue Split { get; }
        public PropertyValue<float> SplitOffset { get; }

        public MarkingEnterPoint(byte index, IPointSource source) : base(index, source, true)
        {
            Split = new PropertyBoolValue("S", PointChanged, false);
            SplitOffset = new PropertyStructValue<float>("SO", PointChanged, DefaultSplitOffse);
        }
        public override void UpdateProcess()
        {
            Source.GetAbsolutePositionAndDirection(Offset, out Vector3 position, out Vector3 direction);
            Position = position;
            Direction = direction;

            Enter.SortPoints();
        }
        public Vector3 ZeroPosition
        {
            get
            {
                Source.GetAbsolutePositionAndDirection(0, out Vector3 position, out _);
                return position;
            }
        }
        public override void Reset()
        {
            base.Reset();
            Split.Value = false;
            SplitOffset.Value = DefaultSplitOffse;
        }
        public override void Render(OverlayData data)
        {
            base.Render(data);

            if (Split && data.SplitPoint)
            {
                var normal = -Enter.CornerDir / Enter.TranformRatio;

                var leftPos = Position - normal * SplitOffset + Direction;
                var rightPos = Position + normal * SplitOffset + Direction;

                var dataWhite = new OverlayData(data.CameraInfo);
                leftPos.RenderCircle(dataWhite);
                rightPos.RenderCircle(dataWhite);

                data.Color ??= Color;
                data.Width = 0.1f;
                leftPos.RenderCircle(data);
                rightPos.RenderCircle(data);
            }
        }

        public override void FromXml(XElement config, ObjectsMap map)
        {
            base.FromXml(config, map);
            Split.FromXml(config, false);
            SplitOffset.FromXml(config, DefaultSplitOffse);
        }
        public override XElement ToXml()
        {
            var config = base.ToXml();
            Split.ToXml(config);
            SplitOffset.ToXml(config);
            return config;
        }
    }
    public class MarkingCrosswalkPoint : MarkingPoint
    {
        private static Vector3 MarkerSize { get; } = Vector3.one * 2f;
        public static float Shift => 1f;
        public override PointType Type => PointType.Crosswalk;
        public MarkingEnterPoint SourcePoint { get; }
        public override Vector3 Position
        {
            get => base.Position;
            protected set => Bounds = new Bounds(value, MarkerSize);
        }

        public MarkingCrosswalkPoint(MarkingEnterPoint sourcePoint) : base(sourcePoint.Index, sourcePoint.Source, false)
        {
            SourcePoint = sourcePoint;
            SourcePoint.OnUpdate += SourcePointUpdate;
        }
        private void SourcePointUpdate(MarkingPoint point) => UpdateProcess();
        public override void UpdateProcess()
        {
            Position = SourcePoint.Position + SourcePoint.Direction * (Shift / Enter.TranformRatio);
            Direction = SourcePoint.Direction;
        }
        public override void Render(OverlayData data)
        {
            var shift = Enter.CornerDir.Turn90(true) * Shift;
            var bezier = new Line3(Position - shift, Position + shift).GetBezier();

            data.Width ??= DefaultWidth;
            data.Color ??= Color;
            bezier.RenderBezier(data);
        }
        public override string ToString() => $"{base.ToString()}C";
    }
    public class MarkingNormalPoint : MarkingPoint
    {
        public override PointType Type => PointType.Normal;
        public new NodeMarking Marking => (NodeMarking)base.Marking;
        public MarkingEnterPoint SourcePoint { get; }
        public override bool IsSplit => SourcePoint.IsSplit;
        public override float SplitOffsetValue => SourcePoint.SplitOffsetValue;

        public MarkingNormalPoint(MarkingEnterPoint sourcePoint) : base(sourcePoint.Index, sourcePoint.Source, false)
        {
            SourcePoint = sourcePoint;
            SourcePoint.OnUpdate += SourcePointUpdate;
        }

        private void SourcePointUpdate(MarkingPoint point) => UpdateProcess();

        public override void UpdateProcess()
        {
            var tSet = new HashSet<float>();

            var line = new StraightTrajectory(SourcePoint.Position, SourcePoint.Position + SourcePoint.Direction, false);
            foreach (var contour in Marking.Contour)
                tSet.AddRange(Intersection.Calculate(line, contour).Where(i => i.isIntersect).Select(i => i.firstT));

            var tSetSort = tSet.OrderBy(i => i).ToArray();

            var t = tSetSort.Length == 0 ? 0 : tSetSort.Length == 1 ? tSetSort.First() : tSetSort.Skip(1).First();

            Position = SourcePoint.Position + SourcePoint.Direction * t;
            Direction = -SourcePoint.Direction;
        }
        public override Vector3 GetAbsolutePosition(Alignment alignment) => base.GetAbsolutePosition(alignment.Invert());
        public override string ToString() => $"{base.ToString()}N";
    }
    public class MarkingLanePoint : MarkingPoint
    {
        public override PointType Type => PointType.Lane;

        public MarkingEnterPoint SourcePointA { get; private set; }
        public MarkingEnterPoint SourcePointB { get; private set; }
        public new NetLanePointSource Source => (NetLanePointSource)base.Source;
        public override Vector3 Position
        {
            get => MarkerPosition + Direction * (1.5f / Enter.TranformRatio);
            protected set => MarkerPosition = value - Direction * (1.5f / Enter.TranformRatio);
        }
        public override Vector3 MarkerPosition
        {
            get => Bounds.center;
            set => Bounds = new Bounds(value, new Vector3(1f, 1f, Width));
        }
        public float Width => (SourcePointB.Position - SourcePointA.Position).MakeFlat().magnitude;
        public override Color32 Color => Colors.GetOverlayColor(Index + Colors.OverlayColors.Length / 2, byte.MaxValue);

        public MarkingLanePoint(byte index, NetLanePointSource source) : base(index, source, false)
        {
            source.OnPointOrderChanged += PointOrderChanged;
            PointOrderChanged();
        }

        private void PointOrderChanged()
        {
            Source.GetPoints(out var pointA, out var pointB);

            if (SourcePointA != null)
                SourcePointA.OnUpdate -= SourcePointUpdate;
            if (SourcePointB != null)
                SourcePointB.OnUpdate -= SourcePointUpdate;

            pointA.OnUpdate += SourcePointUpdate;
            SourcePointA = pointA;
            pointB.OnUpdate += SourcePointUpdate;
            SourcePointB = pointB;

            UpdateProcess();
        }

        private void SourcePointUpdate(MarkingPoint point)
        {
            UpdateProcess();
            PointChanged();
        }
        public override void UpdateProcess()
        {
            Direction = (SourcePointA.Direction + SourcePointB.Direction) * 0.5f;
            Position = (SourcePointA.Position + SourcePointB.Position) * 0.5f;
        }
        public override bool IsHover(Ray ray)
        {
            var bounds = Bounds;
            var plane = new Plane(Vector3.up, -bounds.center.y);
            plane.Raycast(ray, out var hit);
            ray.origin += ray.direction * hit;
            var angle = Vector3.Angle(Direction.MakeFlat(), Vector3.forward);
            ray.direction = Quaternion.AngleAxis(angle, Vector3.up) * ray.direction;

            return bounds.IntersectRay(ray);
        }
        public override void Render(OverlayData data)
        {
            var width = Width;
            data.Width ??= DefaultWidth;

            if (width >= 0.2f)
            {
                data.Color ??= Color;

                var dir = (SourcePointB.Position - SourcePointA.Position).normalized;
                var dx = data.Width.Value * 0.5f;
                var dy = 0.15f + (DefaultWidth - data.Width.Value) * 0.5f;
                var area = new Quad3()
                {
                    a = SourcePointA.Position + dir * dy - SourcePointA.Direction * ((1.5f - dx) / Enter.TranformRatio),
                    b = SourcePointA.Position + dir * dy - SourcePointA.Direction * ((1.5f + dx) / Enter.TranformRatio),
                    c = SourcePointB.Position - dir * dy - SourcePointB.Direction * ((1.5f + dx) / Enter.TranformRatio),
                    d = SourcePointB.Position - dir * dy - SourcePointB.Direction * ((1.5f - dx) / Enter.TranformRatio),
                };
                area.RenderQuad(data);
            }
        }
        public override string ToString() => $"{base.ToString()}L";
    }

    public struct MarkingPointPair
    {
        public static string XmlName { get; } = "PP";
        public static string XmlName1 { get; } = "L1";
        public static string XmlName2 { get; } = "L2";
        public static bool FromHash(ulong hash, Marking marking, ObjectsMap map, out MarkingPointPair pair, out bool invert)
        {
            var firstId = (int)(hash >> 32);
            var secondId = (int)hash;

            if (MarkingPoint.FromId(firstId, marking, map, out MarkingPoint first) && MarkingPoint.FromId(secondId, marking, map, out MarkingPoint second))
            {
                pair = new MarkingPointPair(first, second);
                invert = first.Id >= second.Id;
                return true;
            }
            else
            {
                pair = default;
                invert = false;
                return false;
            }
        }
        public static MarkingPointPair FromPoints(MarkingPoint first, MarkingPoint second, out bool invert)
        {
            var pair = new MarkingPointPair(first, second);
            invert = first.Id >= second.Id;
            return pair;
        }

        public ulong Hash { get; }
        public MarkingPoint First { get; }
        public MarkingPoint Second { get; }
        public bool IsSameEnter => First.Enter == Second.Enter;
        public bool IsSame => First == Second;
        public bool IsStopLine => IsSameEnter && First.Type == MarkingPoint.PointType.Enter && Second.Type == MarkingPoint.PointType.Enter;
        public bool IsNormal => First.Type == MarkingPoint.PointType.Normal || Second.Type == MarkingPoint.PointType.Normal;
        public bool IsLane => First.Type == MarkingPoint.PointType.Lane || Second.Type == MarkingPoint.PointType.Lane;
        public bool IsCrosswalk => First.Type == MarkingPoint.PointType.Crosswalk && Second.Type == MarkingPoint.PointType.Crosswalk;
        public bool IsSplit => First.IsSplit || Second.IsSplit;
        public bool IsSideLine => !IsSameEnter && ((First.IsFirst && Second.IsLast) || (First.IsLast && Second.IsFirst));
        public NetworkType NetworkType => First.NetworkType & Second.NetworkType;
        public LineType LineType
        {
            get
            {
                if (IsStopLine)
                    return LineType.Stop;
                else if (IsCrosswalk)
                    return LineType.Crosswalk;
                else if (IsLane)
                    return LineType.Lane;
                else
                    return LineType.Regular;
            }
        }

        public MarkingPointPair(MarkingPoint first, MarkingPoint second)
        {
            First = first.Id > second.Id ? second : first;
            Second = first.Id > second.Id ? first : second;

            Hash = (((ulong)First.Id) << 32) + (ulong)Second.Id;
        }

        public string XmlSection => XmlName;

        public bool ContainsPoint(MarkingPoint point) => First == point || Second == point;
        public bool ContainsEnter(Entrance enter) => First.Enter == enter || Second.Enter == enter;
        public MarkingPoint GetOther(MarkingPoint point) => ContainsPoint(point) ? (point == First ? Second : First) : null;
        public LineType DefaultType => IsSameEnter ? LineType.Stop : LineType.Regular;

        public override string ToString() => $"{First}—{Second}";

        public override bool Equals(object obj) => obj is MarkingPointPair other && other == this;

        public override int GetHashCode() => Hash.GetHashCode();
        public static bool operator ==(MarkingPointPair x, MarkingPointPair y) => x.Hash == y.Hash;
        public static bool operator !=(MarkingPointPair x, MarkingPointPair y) => x.Hash != y.Hash;
    }
    public class MarkingPointPairComparer : IEqualityComparer<MarkingPointPair>
    {
        public bool Equals(MarkingPointPair x, MarkingPointPair y) => x.Hash == y.Hash;
        public int GetHashCode(MarkingPointPair pair) => pair.GetHashCode();
    }
}
