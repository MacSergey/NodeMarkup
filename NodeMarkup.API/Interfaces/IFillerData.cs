using System.Collections.Generic;

namespace NodeMarkup.API
{
	public interface IFillerData
	{
		int Id { get; }
		IEnumerable<IEntrancePointData> PointDatas { get; }

		void Remove();
	}
}