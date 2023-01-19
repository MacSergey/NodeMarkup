using NodeMarkup.Manager;

using System.Collections.Generic;

namespace NodeMarkup.API.Implementations
{
	public struct SegmentEntranceData : ISegmentEntranceData
	{
		public NodeEnter Enter { get; }
		public ushort Id => Enter.Id;
		public int PointCount => Enter.PointCount;

		public IEnumerable<IEntrancePointData> Points
		{
			get
			{
				foreach (var point in Enter.EnterPoints)
				{
					yield return new EntrancePointData(point, this);
				}
			}
		}

		public IEnumerable<ILanePointData> LanePoints
		{
			get
			{
				foreach (var point in Enter.LanePoints)
				{
					yield return new LanePointData(point, this);
				}
			}
		}

		public SegmentEntranceData(NodeEnter enter)
		{
			Enter = enter;
		}

		public bool GetEntrancePoint(byte index, out IEntrancePointData pointData)
		{
			if (Enter.TryGetPoint(index, MarkupPoint.PointType.Enter, out var point))
			{
				pointData = new EntrancePointData(point as MarkupEnterPoint, this);
				return true;
			}
			else
			{
				pointData = null;
				return false;
			}
		}
		public bool GetLanePoint(byte index, out ILanePointData pointData)
		{
			if (Enter.TryGetPoint(index, MarkupPoint.PointType.Lane, out var point))
			{
				pointData = new LanePointData(point as MarkupLanePoint, this);
				return true;
			}
			else
			{
				pointData = null;
				return false;
			}
		}

		public override string ToString() => Enter.ToString();
	}
}
