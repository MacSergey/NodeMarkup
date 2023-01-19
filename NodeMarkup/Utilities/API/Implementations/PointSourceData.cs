using NodeMarkup.Manager;

namespace NodeMarkup.API.Implementations
{
	public struct PointSourceData : IPointSourceData
	{
		public PointLocation Location { get; }
		public uint LeftLaneId { get; }
		public int LeftIndex { get; }
		public uint RightLaneId { get; }
		public int RightIndex { get; }

		public PointSourceData(PointLocation location, uint leftLaneId, int leftIndex, uint rightLaneId, int rightIndex)
		{
			Location = location;
			LeftLaneId = leftLaneId;
			LeftIndex = leftIndex;
			RightLaneId = rightLaneId;
			RightIndex = rightIndex;
		}

		public PointSourceData(NetInfoPointSource source)
		{
			Location = source.Location switch
			{
				MarkupPoint.LocationType.LeftEdge => PointLocation.Left,
				MarkupPoint.LocationType.RightEdge => PointLocation.Rigth,
				MarkupPoint.LocationType.Between => PointLocation.Between,
				_ => PointLocation.None,
			};

			if (source.LeftLane != null)
			{
				LeftLaneId = source.LeftLane.LaneId;
				LeftIndex = source.LeftLane.Index;
			}
			else
			{
				LeftLaneId = default;
				LeftIndex = default;
			}

			if (source.RightLane != null)
			{
				RightLaneId = source.RightLane.LaneId;
				RightIndex = source.RightLane.Index;
			}
			else
			{
				RightLaneId = default;
				RightIndex = default;
			}
		}
	}
}
