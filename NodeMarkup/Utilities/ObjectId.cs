using NodeMarkup.Manager;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.Utils
{
    public struct ObjectId
    {
        private long Id;

        public ushort Node
        {
            get => (Id & (long)ObjectType.Node) == 0 ? (ushort)0 : (ushort)(Id & (long)ObjectType.Data);
            set => Id = (long)ObjectType.Node | value;
        }
        public ushort Segment
        {
            get => (Id & (long)ObjectType.Segment) == 0 ? (ushort)0 : (ushort)(Id & (long)ObjectType.Data);
            set => Id = (long)ObjectType.Segment | value;
        }
        public int Point
        {
            get => (Id & (long)ObjectType.Point) == 0 ? 0 : (int)(Id & (long)ObjectType.Data);
            set => Id = (long)ObjectType.Point | value;
        }
        public ObjectType Type => (ObjectType)(Id & (long)ObjectType.Type);

        public static bool operator ==(ObjectId x, ObjectId y) => x.Id == y.Id;
        public static bool operator !=(ObjectId x, ObjectId y) => x.Id != y.Id;

        public override bool Equals(object obj) => obj is ObjectId objectId && objectId == this;
        public override int GetHashCode() => Id.GetHashCode();
        public override string ToString()
        {
            return Type switch
            {
                ObjectType.Node => $"{Type}: {Node}",
                ObjectType.Segment => $"{Type}: {Segment}",
                ObjectType.Point => $"{Type}: {MarkupPoint.GetEnter(Point)}-{MarkupPoint.GetNum(Point)}{MarkupPoint.GetType(Point).ToString().FirstOrDefault()}",
                _ => $"{Type}: {Id}",
            };
        }
    }
    public class ObjectsMap : IEnumerable<KeyValuePair<ObjectId, ObjectId>>
    {
        public bool IsMirror { get; set; }
        public bool IsEmpty => !Map.Any();
        private Dictionary<ObjectId, ObjectId> Map { get; } = new Dictionary<ObjectId, ObjectId>();

        public ObjectId this[ObjectId key]
        {
            get => Map[key];
            private set
            {
                if (key == value)
                    return;

                Map[key] = value;
            }
        }

        public ObjectsMap(bool isMirror = false)
        {
            IsMirror = isMirror;
        }
        public bool TryGetValue(ObjectId key, out ObjectId value) => Map.TryGetValue(key, out value);
        public bool TryGetNode(ushort nodeIdKey, out ushort nodeIdValue)
        {
            if (Map.TryGetValue(new ObjectId() { Node = nodeIdKey }, out ObjectId value))
            {
                nodeIdValue = value.Node;
                return true;
            }
            else
            {
                nodeIdValue = default;
                return false;
            }
        }
        public bool TryGetSegment(ushort segmentIdKey, out ushort segmentIdValue)
        {
            if (Map.TryGetValue(new ObjectId() { Segment = segmentIdKey }, out ObjectId value))
            {
                segmentIdValue = value.Node;
                return true;
            }
            else
            {
                segmentIdValue = default;
                return false;
            }
        }

        public IEnumerator<KeyValuePair<ObjectId, ObjectId>> GetEnumerator() => Map.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void AddMirrorEnter(Enter enter)
        {
            var count = enter.PointCount + 1;
            for (byte i = 1; i < count; i += 1)
                AddPoint(enter.Id, i, (byte)(count - i));
        }
        public void AddPoint(ushort enter, byte source, byte target)
        {
            foreach (var pointType in Enum.GetValues(typeof(MarkupPoint.PointType)).OfType<MarkupPoint.PointType>())
                AddPoint(MarkupPoint.GetId(enter, source, pointType), MarkupPoint.GetId(enter, target, pointType));
        }
        public void AddPoint(int source, int target) => this[new ObjectId() { Point = source }] = new ObjectId() { Point = target };
        public void AddSegment(ushort source, ushort target) => this[new ObjectId() { Segment = source }] = new ObjectId() { Segment = target };
        public void AddNode(ushort source, ushort target) => this[new ObjectId() { Node = source }] = new ObjectId() { Node = target };

        public void Remove(ObjectId key) => Map.Remove(key);

        public delegate bool TryGetDelegate<T>(T key, out T value);
    }
    public enum ObjectType : long
    {
        Data = 0xFFFFFFFFL,
        Type = Data << 32,
        Node = 1L << 32,
        Segment = 2L << 32,
        Point = 4L << 32,
    }
}
