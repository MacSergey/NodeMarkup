using NodeMarkup.Manager;

namespace NodeMarkup.API.Implementations
{
	public struct EntrancePointData : IEntrancePointData, IPointSourceData
	{
		private MarkupEnterPoint Point { get; }
		public IEntranceData Entrance { get; }
		public IPointSourceData Source { get; }

		public byte Index => Point.Index;
		public ushort EntranceId => Point.Enter.Id;
		public ushort MarkingId => Point.Markup.Id;

		public uint LeftLaneId => Source.LeftLaneId;
		public int LeftIndex => Source.LeftIndex;
		public uint RightLaneId => Source.RightLaneId;
		public int RightIndex => Source.RightIndex;
		public PointLocation Location => Source.Location;
		public float Position => Source.Position;

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
		}

		public override string ToString() => Point.ToString();
	}
}
