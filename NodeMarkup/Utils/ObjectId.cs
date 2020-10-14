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
            switch (Type)
            {
                case ObjectType.Node:
                    return $"{Type}: {Node}";
                case ObjectType.Segment:
                    return $"{Type}: {Segment}";
                case ObjectType.Point:
                    return $"{Type}: {Point}";
                default:
                    return $"{Type}: {Id}";
            }
        }
    }
    public class ObjectsMap : IEnumerable<KeyValuePair<ObjectId, ObjectId>>
    {
        public bool IsMirror { get; set; }
        private Dictionary<ObjectId, ObjectId> Map { get; } = new Dictionary<ObjectId, ObjectId>();

        public ObjectId this[ObjectId key]
        {
            get => Map[key];
            set => Map[key] = value;
        }

        public ObjectsMap(bool isMirror = false)
        {
            IsMirror = isMirror;
        }
        public bool TryGetValue(ObjectId key, out ObjectId value) => Map.TryGetValue(key, out value);

        public IEnumerator<KeyValuePair<ObjectId, ObjectId>> GetEnumerator() => Map.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Add(ObjectId key, ObjectId value) => Map.Add(key, value);
        public void AddMirrorEnter(Enter enter)
        {
            var count = enter.PointCount + 1;
            for (var i = 1; i < count; i += 1)
                AddPoint(enter.Id, i, count - i);
        }
        public void AddPoint(ushort enter, int source, int target)
        {
            foreach (var pointType in Enum.GetValues(typeof(MarkupPoint.PointType)).OfType<MarkupPoint.PointType>())
            {
                var sourcePoint = new ObjectId() { Point = MarkupPoint.GetId(enter, (byte)source, pointType) };
                var targetPoint = new ObjectId() { Point = MarkupPoint.GetId(enter, (byte)target, pointType) };
                this[sourcePoint] = targetPoint;
            }
        }
        public void AddEnter(ushort source, ushort target) => this[new ObjectId() { Segment = source }] = new ObjectId() { Segment = target };
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
