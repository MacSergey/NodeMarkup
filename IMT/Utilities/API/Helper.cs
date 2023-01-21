using NodeMarkup.API;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.Utilities.API
{
    internal static class APIHelper
    {
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
                throw new EntranceNotExist(pointData.EntranceId, marking.Id);

            if (!enter.TryGetPoint(pointData.Index, MarkingPoint.PointType.Enter, out var point))
                throw new PointNotExist(pointData.Index, enter.Id);

            return point;
        }
        internal static MarkingPoint GetNormalPoint(Marking marking, INormalPointData pointData)
        {
            if (!marking.TryGetEnter(pointData.EntranceId, out var enter))
                throw new EntranceNotExist(pointData.EntranceId, marking.Id);

            if (!enter.TryGetPoint(pointData.Index, MarkingPoint.PointType.Normal, out var point))
                throw new PointNotExist(pointData.Index, enter.Id);

            return point;
        }
        internal static MarkingPoint GetCrosswalkPoint(Marking marking, ICrosswalkPointData pointData)
        {
            if (!marking.TryGetEnter(pointData.EntranceId, out var enter))
                throw new EntranceNotExist(pointData.EntranceId, marking.Id);

            if (!enter.TryGetPoint(pointData.Index, MarkingPoint.PointType.Crosswalk, out var point))
                throw new PointNotExist(pointData.Index, enter.Id);

            return point;
        }
        internal static MarkingPoint GetLanePoint(Marking marking, ILanePointData pointData)
        {
            if (!marking.TryGetEnter(pointData.EntranceId, out var enter))
                throw new EntranceNotExist(pointData.EntranceId, marking.Id);

            if (!enter.TryGetPoint(pointData.Index, MarkingPoint.PointType.Lane, out var point))
                throw new PointNotExist(pointData.Index, enter.Id);

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
