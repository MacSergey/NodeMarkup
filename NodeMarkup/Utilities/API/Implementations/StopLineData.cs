using NodeMarkup.Manager;

namespace NodeMarkup.API.Implementations
{
	public class StopLineData : IStopLineData
	{
		private readonly MarkupStopLine _generatedLine;

		public StopLineData(MarkupStopLine generatedLine, IEntrancePointData startPointData, IEntrancePointData endPointData)
		{
			_generatedLine = generatedLine;

			StartPoint = startPointData;
			EndPoint = endPointData;
		}

		public ulong Id => _generatedLine.Id;
		public IEntrancePointData StartPoint { get; }
		public IEntrancePointData EndPoint { get; }

		public void Remove()
		{
			_generatedLine.Markup.RemoveLine(_generatedLine);
		}
	}
}
