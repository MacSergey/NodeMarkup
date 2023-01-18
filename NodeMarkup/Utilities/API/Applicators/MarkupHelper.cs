using NodeMarkup.Manager;

using System;
using System.Collections.Generic;

namespace NodeMarkup.API.Applicators
{
	public static class MarkupHelper
	{
		public static void CheckPoints(ushort markingId, IPointData startPointData, IPointData endPointData, bool? same)
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

		public static MarkupPoint GetEntrancePoint(Markup markup, IEntrancePointData pointData)
		{
			if (!markup.TryGetEnter(pointData.EntranceId, out var enter))
				throw new EntranceNotExist(pointData.EntranceId, markup.Id);

			if (!enter.TryGetPoint(pointData.Index, MarkupPoint.PointType.Enter, out var point))
				throw new PointNotExist(pointData.Index, enter.Id);

			return point;
		}

		public static MarkupPoint GetCrosswalkPoint(Markup markup, ICrosswalkPointData pointData)
		{
			if (!markup.TryGetEnter(pointData.EntranceId, out var enter))
				throw new EntranceNotExist(pointData.EntranceId, markup.Id);

			if (!enter.TryGetPoint(pointData.Index, MarkupPoint.PointType.Crosswalk, out var point))
				throw new PointNotExist(pointData.Index, enter.Id);

			return point;
		}

		public static MarkupPoint GetLanePoint(Markup markup, ILanePointData pointData)
		{
			if (!markup.TryGetEnter(pointData.EntranceId, out var enter))
				throw new EntranceNotExist(pointData.EntranceId, markup.Id);

			if (!enter.TryGetPoint(pointData.Index, MarkupPoint.PointType.Lane, out var point))
				throw new PointNotExist(pointData.Index, enter.Id);

			return point;
		}

		public static FillerContour GetFillerContour(Markup markup, IEnumerable<IEntrancePointData> pointDatas)
		{
			if (pointDatas == null)
				throw new ArgumentNullException(nameof(pointDatas));

			var vertices = new List<IFillerVertex>();
			foreach (var pointData in pointDatas)
			{
				if (pointData.MarkingId != markup.Id)
					throw new MarkingIdNotMatchException(markup.Id, pointData.MarkingId);

				var point = GetEntrancePoint(markup, pointData);
				vertices.Add(new EnterFillerVertex(point));
			}

			var contour = new FillerContour(markup, vertices);
			if (!contour.IsComplite)
				throw new CreateFillerException("Filler contour is not complited");

			return contour;
		}
	}
}
