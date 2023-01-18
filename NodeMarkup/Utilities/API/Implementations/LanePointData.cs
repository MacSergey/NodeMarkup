using NodeMarkup.Manager;

namespace NodeMarkup.API.Implementations
{
	public struct LanePointData : ILanePointData
	{
		private MarkupLanePoint Point { get; }
		public IEntranceData Entrance { get; }
		public byte Index => Point.Index;
		public ushort EntranceId => Point.Enter.Id;
		public ushort MarkingId => Point.Markup.Id;
		public IEntrancePointData SourcePointA => new EntrancePointData(Point.SourcePointA, Entrance);
		public IEntrancePointData SourcePointB => new EntrancePointData(Point.SourcePointB, Entrance);


		public LanePointData(MarkupLanePoint point, IEntranceData entrance)
		{
			Point = point;
			Entrance = entrance;
		}

		public override string ToString() => Point.ToString();
	}
}
