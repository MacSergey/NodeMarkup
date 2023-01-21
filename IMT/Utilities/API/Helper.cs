using ModsCommon;
using NodeMarkup.API;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static NodeMarkup.Manager.MarkingPoint;

namespace NodeMarkup.Utilities.API
{
    internal static class APIHelper
    {
        private static ObjectsMap EmptyMap { get; } = new ObjectsMap();
        internal static NodeMarking GetNodeMarking(ushort markingId, bool throwEx = true)
        {
            if (SingletonManager<NodeMarkingManager>.Instance.TryGetMarking(markingId, out var nodeMarking))
                return nodeMarking;
            if (!throwEx)
                return null;
            else
                throw new MarkingNotExistException(markingId);
        }
        internal static SegmentMarking GetSegmentMarking(ushort markingId, bool throwEx = true)
        {
            if (SingletonManager<SegmentMarkingManager>.Instance.TryGetMarking(markingId, out var segmentMarking))
                return segmentMarking;
            if (!throwEx)
                return null;
            else
                throw new MarkingNotExistException(markingId);
        }
        internal static Marking GetMarking(ushort markingId, Manager.MarkingType type, bool throwEx = true) 
        { 
            if(type == Manager.MarkingType.Node)
                return GetNodeMarking(markingId, throwEx);
            else if(type == Manager.MarkingType.Segment)
                return GetSegmentMarking(markingId, throwEx);
            else
                throw new IntersectionMarkingToolException($"Unsupported type of marking: {type}");
        }

        internal static SegmentEntrance GetSegmentEntrance(ushort markingId, ushort entranceId, bool throwEx = true) 
        {
            var marking = GetNodeMarking(markingId);

            if(marking.TryGetEnter(entranceId, out SegmentEntrance entrance))
                return entrance;
            if (!throwEx)
                return null;
            else
                throw new EntranceNotExistException(entranceId, markingId);
        }
        internal static NodeEntrance GetNodeEntrance(ushort markingId, ushort entranceId, bool throwEx = true)
        {
            var marking = GetSegmentMarking(markingId);

            if (marking.TryGetEnter(entranceId, out NodeEntrance entrance))
                return entrance;
            if (!throwEx)
                return null;
            else
                throw new EntranceNotExistException(entranceId, markingId);
        }
        internal static Entrance GetEntrance(ushort markingId, ushort entranceId, Manager.EntranceType type, bool throwEx = true)
        {
            if (type == Manager.EntranceType.Segment)
                return GetSegmentEntrance(markingId, entranceId, throwEx);
            else if (type == Manager.EntranceType.Node)
                return GetNodeEntrance(markingId, entranceId, throwEx);
            else
                throw new IntersectionMarkingToolException($"Unsupported type of entrance: {type}");
        }
        internal static PointType GetPoint<PointType>(ushort markingId, ushort entranceId, Manager.EntranceType entranceType, byte index, MarkingPoint.PointType pointType, bool throwEx = true)
            where PointType : MarkingPoint
        {
            if (GetEntrance(markingId, entranceId, entranceType, throwEx) is Entrance entrance)
            {
                if (entrance.TryGetPoint(index, pointType, out var point) && point is PointType)
                    return point as PointType;
                if (!throwEx)
                    return null;
                else
                    throw new PointNotExistException(index, entranceId);
            }
            else
                return null;
        }

        internal static LineType GetLine<LineType>(ushort markingId, Manager.MarkingType type, ulong lineId)
            where LineType : MarkingLine
        {
            var marking = GetMarking(markingId, type);
            if (marking.TryGetLine<LineType>(lineId, EmptyMap, out var line))
                return line;
            else
                throw new LineNotExistException(lineId, markingId);
        }
        internal static MarkingFiller GetFiller(ushort markingId, Manager.MarkingType type, int fillerId)
        {
            var marking = GetMarking(markingId, type);
            if (marking.TryGetFiller(fillerId, out var filler))
                return filler;
            else
                throw new FillerNotExistException(fillerId, markingId);
        }

        internal static void CheckPoints(ushort markingId, IPointData startPointData, IPointData endPointData, bool? same)
        {
            if (startPointData == null)
                throw new ArgumentNullException(nameof(startPointData));

            if (endPointData == null)
                throw new ArgumentNullException(nameof(endPointData));

            if (startPointData.MarkingId != markingId)
                throw new MarkingIdNotMatchException(markingId, startPointData.MarkingId);

            if (endPointData.MarkingId != markingId)
                throw new MarkingIdNotMatchException(markingId, endPointData.MarkingId);

            if (same == true)
            {
                if (startPointData.EntranceId != endPointData.EntranceId)
                    throw new CreateLineException(startPointData, endPointData, "Start point and end point must be from the same entrance");
            }
            else if (same == false)
            {
                if (startPointData.EntranceId == endPointData.EntranceId)
                    throw new CreateLineException(startPointData, endPointData, "Start point and end point must be from different entrances");
            }
        }
        internal static MarkingPoint GetEntrancePoint(Marking marking, IEntrancePointData pointData)
        {
            if (!marking.TryGetEnter(pointData.EntranceId, out var enter))
                throw new EntranceNotExistException(pointData.EntranceId, marking.Id);

            if (!enter.TryGetPoint(pointData.Index, MarkingPoint.PointType.Enter, out var point))
                throw new PointNotExistException(pointData.Index, enter.Id);

            return point;
        }
        internal static MarkingPoint GetNormalPoint(Marking marking, INormalPointData pointData)
        {
            if (!marking.TryGetEnter(pointData.EntranceId, out var enter))
                throw new EntranceNotExistException(pointData.EntranceId, marking.Id);

            if (!enter.TryGetPoint(pointData.Index, MarkingPoint.PointType.Normal, out var point))
                throw new PointNotExistException(pointData.Index, enter.Id);

            return point;
        }
        internal static MarkingPoint GetCrosswalkPoint(Marking marking, ICrosswalkPointData pointData)
        {
            if (!marking.TryGetEnter(pointData.EntranceId, out var enter))
                throw new EntranceNotExistException(pointData.EntranceId, marking.Id);

            if (!enter.TryGetPoint(pointData.Index, MarkingPoint.PointType.Crosswalk, out var point))
                throw new PointNotExistException(pointData.Index, enter.Id);

            return point;
        }
        internal static MarkingPoint GetLanePoint(Marking marking, ILanePointData pointData)
        {
            if (!marking.TryGetEnter(pointData.EntranceId, out var enter))
                throw new EntranceNotExistException(pointData.EntranceId, marking.Id);

            if (!enter.TryGetPoint(pointData.Index, MarkingPoint.PointType.Lane, out var point))
                throw new PointNotExistException(pointData.Index, enter.Id);

            return point;
        }
        internal static FillerContour GetFillerContour(Marking marking, IEnumerable<IEntrancePointData> pointDatas)
        {
            if (pointDatas == null)
                throw new ArgumentNullException(nameof(pointDatas));

            var vertices = new List<IFillerVertex>();
            foreach (var pointData in pointDatas)
            {
                if (pointData.MarkingId != marking.Id)
                    throw new MarkingIdNotMatchException(marking.Id, pointData.MarkingId);

                var point = GetEntrancePoint(marking, pointData);
                vertices.Add(new EnterFillerVertex(point));
            }

            var contour = new FillerContour(marking, vertices);
            if (!contour.IsComplite)
                throw new CreateFillerException("Filler contour is not complited");

            return contour;
        }
        internal static StyleType GetStyleType<StyleType>(string name)
            where StyleType : Enum
        {
            try { return (StyleType)Enum.Parse(typeof(StyleType), name); }
            catch { throw new IntersectionMarkingToolException($"No style with name {name}"); }
        }
    }
}
