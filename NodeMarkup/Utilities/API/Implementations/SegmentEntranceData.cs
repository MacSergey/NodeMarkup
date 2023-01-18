using NodeMarkup.Manager;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.API.Implementations
{
	public struct NodeEntranceData : INodeEntranceData
	{
		public SegmentEnter Enter { get; }
		public ushort Id => Enter.Id;
		public int PointCount => Enter.PointCount;

		public IEnumerable<IPointData> Points
		{
			get
			{
				foreach (var point in Enter.EnterPoints)
				{
					yield return new EntrancePointData(point, this);
				}
			}
		}

		public IEnumerable<INormalPointData> NormalPoints
		{
			get
			{
				foreach (var point in Enter.NormalPoints)
				{
					yield return new NormalPointData(point, this);
				}
			}
		}

		public IEnumerable<ICrosswalkPointData> CrosswalkPoints
		{
			get
			{
				foreach (var point in Enter.CrosswalkPoints)
				{
					yield return new CrosswalkPointData(point, this);
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

		public NodeEntranceData(SegmentEnter enter)
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
		public bool GetNormalPoint(byte index, out INormalPointData pointData)
		{
			if (Enter.TryGetPoint(index, MarkupPoint.PointType.Normal, out var point))
			{
				pointData = new NormalPointData(point as MarkupNormalPoint, this);
				return true;
			}
			else
			{
				pointData = null;
				return false;
			}
		}
		public bool GetCrosswalkPoint(byte index, out ICrosswalkPointData pointData)
		{
			if (Enter.TryGetPoint(index, MarkupPoint.PointType.Crosswalk, out var point))
			{
				pointData = new CrosswalkPointData(point as MarkupCrosswalkPoint, this);
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
