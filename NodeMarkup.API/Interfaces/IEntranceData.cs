using System.Collections.Generic;

namespace NodeMarkup.API
{
	public interface IEntranceData
	{
		ushort Id { get; }
		int PointCount { get; }
		IEnumerable<IPointData> Points { get; }
	}
}