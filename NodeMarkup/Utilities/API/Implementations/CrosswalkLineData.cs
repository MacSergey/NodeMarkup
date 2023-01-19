using NodeMarkup.Manager;

namespace NodeMarkup.API.Implementations
{
	public class CrosswalkLineData : ICrosswalkLineData
	{
		private readonly MarkupCrosswalkLine _generatedLine;

		public CrosswalkLineData(MarkupCrosswalkLine generatedLine, ICrosswalkPointData startPointData, ICrosswalkPointData endPointData)
		{
			_generatedLine = generatedLine;

			StartPoint = startPointData;
			EndPoint = endPointData;
		}

		public ulong Id => _generatedLine.Id;
		public ICrosswalkPointData StartPoint { get; }
		public ICrosswalkPointData EndPoint { get; }

		public void Remove()
		{
			_generatedLine.Markup.RemoveLine(_generatedLine);
		}
	}
}
