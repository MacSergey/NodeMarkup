using System.Collections.Generic;

namespace NodeMarkup.API
{
	public interface IFillerData
	{
		IMarkingApi Marking { get; }
		int Id { get; }
		IEnumerable<IEntrancePointData> PointDatas { get; }

		void Remove();
	}
}