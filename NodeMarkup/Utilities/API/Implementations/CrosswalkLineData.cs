using NodeMarkup.Manager;

namespace NodeMarkup.API.Implementations
{
	public class CrosswalkLineData : ICrosswalkLineData
	{
		private readonly MarkupCrosswalkLine _generatedLine;

		public CrosswalkLineData(MarkupCrosswalkLine generatedLine, ICrosswalkPointData startPointData, ICrosswalkPointData endPointData, IMarkingApi marking)
		{
			_generatedLine = generatedLine;

			StartPoint = startPointData;
			EndPoint = endPointData;
			Marking = marking;
		}

		public ulong Id => _generatedLine.Id;
		public ICrosswalkPointData StartPoint { get; }
		public ICrosswalkPointData EndPoint { get; }
		public IMarkingApi Marking { get; }

		public void Remove()
		{
			_generatedLine.Markup.RemoveLine(_generatedLine);
		}
	}
}
