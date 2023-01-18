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
		public float Position { get; }

		public PointSourceData(PointLocation location, uint leftLaneId, int leftIndex, uint rightLaneId, int rightIndex, float position)
		{
			Location = location;
			LeftLaneId = leftLaneId;
			LeftIndex = leftIndex;
			RightLaneId = rightLaneId;
			RightIndex = rightIndex;
			Position = position;
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

			if ((source.Location & MarkupPoint.LocationType.Between) != MarkupPoint.LocationType.None)
			{
				Position = (source.Enter.IsLaneInvert ? -source.RightLane.HalfWidth : source.RightLane.HalfWidth) + source.RightLane.Position;
			}
			else if ((source.Location & MarkupPoint.LocationType.Edge) != MarkupPoint.LocationType.None)
			{
				switch (source.Location)
				{
					case MarkupPoint.LocationType.LeftEdge:
						Position = (source.Enter.IsLaneInvert ? -source.RightLane.HalfWidth : source.RightLane.HalfWidth) + source.RightLane.Position;
						break;

					case MarkupPoint.LocationType.RightEdge:
						Position = (source.Enter.IsLaneInvert ? source.LeftLane.HalfWidth : -source.LeftLane.HalfWidth) + source.LeftLane.Position;
						break;
				}
			}

			Position = 0F;
		}
	}
}
