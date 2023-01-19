using NodeMarkup.Manager;

using System.Collections.Generic;

namespace NodeMarkup.API.Implementations
{
	public class FillerData : IFillerData
	{
		private readonly MarkupFiller _generatedFiller;

		public FillerData(MarkupFiller generatedFiller, IEnumerable<IEntrancePointData> pointDatas)
		{
			_generatedFiller = generatedFiller;

			PointDatas = pointDatas;
		}

		public int Id => _generatedFiller.Id;
		public IEnumerable<IEntrancePointData> PointDatas { get; }

		public void Remove()
		{
			_generatedFiller.Markup.RemoveFiller(_generatedFiller);
		}
	}
}
