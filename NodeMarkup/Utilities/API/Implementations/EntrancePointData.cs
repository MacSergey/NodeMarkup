using NodeMarkup.Manager;
using NodeMarkup.Tools;

namespace NodeMarkup.API.Implementations
{
	public struct EntrancePointData : IEntrancePointData, IPointSourceData
	{
		private MarkupEnterPoint Point { get; }
		public IEntranceData Entrance { get; }
		public IPointSourceData Source { get; }
		public float Position { get; }

		public byte Index => Point.Index;
		public ushort EntranceId => Point.Enter.Id;
		public ushort MarkingId => Point.Markup.Id;

		public uint LeftLaneId => Source.LeftLaneId;
		public int LeftIndex => Source.LeftIndex;
		public uint RightLaneId => Source.RightLaneId;
		public int RightIndex => Source.RightIndex;
		public PointLocation Location => Source.Location;

		public float Offset
		{
			get => Point.Offset;
			set => Point.Offset.Value = value;
		}

		public EntrancePointData(MarkupEnterPoint point, IEntranceData entrance)
		{
			Point = point;
			Entrance = entrance;
			Source = new PointSourceData(Point.Source);
			Position = 0F;

			if ((Point.Source.Location & MarkupPoint.LocationType.Between) != MarkupPoint.LocationType.None)
			{
				Position = (Point.Source.Enter.IsLaneInvert ? -Point.Source.RightLane.HalfWidth : Point.Source.RightLane.HalfWidth) + Point.Source.RightLane.Position;
			}
			else if ((Point.Source.Location & MarkupPoint.LocationType.Edge) != MarkupPoint.LocationType.None)
			{
				switch (Point.Source.Location)
				{
					case MarkupPoint.LocationType.LeftEdge:
						Position = (Point.Source.Enter.IsLaneInvert ? -Point.Source.RightLane.HalfWidth : Point.Source.RightLane.HalfWidth) + Point.Source.RightLane.Position;
						break;
					case MarkupPoint.LocationType.RightEdge:
						Position = (Point.Source.Enter.IsLaneInvert ? Point.Source.LeftLane.HalfWidth : -Point.Source.LeftLane.HalfWidth) + Point.Source.LeftLane.Position;
						break;
				}
			}
		}

		public override string ToString() => Point.ToString();
	}
}
