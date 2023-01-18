using NodeMarkup.Manager;

namespace NodeMarkup.API.Implementations
{
	public struct CrosswalkPointData : ICrosswalkPointData
	{
		private MarkupCrosswalkPoint Point { get; }
		public IEntranceData Entrance { get; }

		public byte Index => Point.Index;
		public ushort EntranceId => Point.Enter.Id;
		public ushort MarkingId => Point.Markup.Id;
		public IEntrancePointData SourcePoint => new EntrancePointData(Point.SourcePoint, Entrance);

		public CrosswalkPointData(MarkupCrosswalkPoint point, IEntranceData entrance)
		{
			Point = point;
			Entrance = entrance;
		}

		public override string ToString() => Point.ToString();
	}
}
