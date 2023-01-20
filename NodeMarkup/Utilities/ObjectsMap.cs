using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static NodeMarkup.Manager.MarkingPoint;

namespace NodeMarkup.Utilities
{
    public class ObjectsMap : NetObjectsMap<ObjectId>
    {
        public bool Invert { get; }

        public ObjectsMap(bool invert = false, bool isSimple = false) : base(isSimple)
        {
            Invert = invert;
        }
        public void AddInvertEnter(Entrance enter)
        {
            var count = enter.PointCount + 1;
            for (byte i = 1; i < count; i += 1)
                AddPoint(enter.Id, i, (byte)(count - i));
        }

        public void AddPoint(ushort enter, byte source, byte target)
        {
            foreach (var pointType in EnumExtension.GetEnumValues<PointType>(i => i.IsItem()))
            {
                if (pointType == PointType.Lane && Invert)
                    target = (byte)(target - 1);

                if (source > 0 && target > 0)
                    AddPoint(GetId(enter, source, pointType), GetId(enter, target, pointType));
            }
        }
        public void AddPoint(int source, int target)
        {
            var sourceId = new ObjectId() { Point = source };
            var targetId = new ObjectId() { Point = target };
            this[sourceId] = targetId;
        }
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
                return $"{nameof(Point)}: {GetEnter(Point)}-{GetIndex(Point)}{MarkingPoint.GetType(Point).ToString().FirstOrDefault()}";
            else
                return base.ToString();
        }
    }
}
