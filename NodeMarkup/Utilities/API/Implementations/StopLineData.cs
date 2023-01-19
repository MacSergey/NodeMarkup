using NodeMarkup.Manager;

namespace NodeMarkup.API.Implementations
{
	public class StopLineData : IStopLineData
	{
		private readonly MarkupStopLine _generatedLine;

		public StopLineData(MarkupStopLine generatedLine, IEntrancePointData startPointData, IEntrancePointData endPointData, IMarkingApi marking)
		{
			_generatedLine = generatedLine;

			StartPoint = startPointData;
			EndPoint = endPointData;
			Marking = marking;
		}

		public ulong Id => _generatedLine.Id;
		public IEntrancePointData StartPoint { get; }
		public IEntrancePointData EndPoint { get; }
		public IMarkingApi Marking { get; }

		public void Remove()
		{
			_generatedLine.Markup.RemoveLine(_generatedLine);
		}
	}
}
