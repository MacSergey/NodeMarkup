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
        public override string ToString() => $"{Type}: {Id}";
    }
    public class PasteMap : IEnumerable<KeyValuePair<ObjectId, ObjectId>>
    {
        public bool IsMirror { get; set; }
        private Dictionary<ObjectId, ObjectId> Map { get; } = new Dictionary<ObjectId, ObjectId>();

        public ObjectId this[ObjectId key]
        {
            get => Map[key];
            set => Map[key] = value;
        }

        public PasteMap(bool isMirror = false)
        {
            IsMirror = isMirror;
        }
        public bool TryGetValue(ObjectId key, out ObjectId value) => Map.TryGetValue(key, out value);

        public IEnumerator<KeyValuePair<ObjectId, ObjectId>> GetEnumerator() => Map.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Add(ObjectId key, ObjectId value) => Map.Add(key, value);
    }
    public enum ObjectType : long
    {
        Data = 0xFFFFFFFFL,
        Type = Data << 32,
        Segment = 1L << 32,
        Point = 2L << 32,

    }
}
