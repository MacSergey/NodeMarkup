using ModsCommon.Utilities;
using NodeMarkup.Manager;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NodeMarkup.Utilities
{
    public class ObjectsMap : NetObjectsMap<ObjectId>
    {
        public bool IsMirror { get; }

        public ObjectsMap(bool isMirror = false, bool isSimple = false) : base(isSimple)
        {
            IsMirror = isMirror;
        }
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
    }
    public class ObjectId : NetObjectId
    {
        public static long PointType = 4L << 32;

        public int Point
        {
            get => (Id & PointType) == 0 ? 0 : (int)(Id & DataMask);
            set => Id = PointType | value;
        }

        public override string ToString()
        {
            if (Type == PointType)
                return $"{nameof(Point)}: {MarkupPoint.GetEnter(Point)}-{MarkupPoint.GetIndex(Point)}{MarkupPoint.GetType(Point).ToString().FirstOrDefault()}";
            else
                return base.ToString();
        }
    }
}
