using System.Collections.Generic;

namespace NodeMarkup.API
{
	public interface IEntranceData
	{
		ushort Id { get; }
		int PointCount { get; }
		IEnumerable<IPointData> Points { get; }

		bool GetEntrancePoint(byte index, out IEntrancePointData point);
		bool GetLanePoint(byte index, out ILanePointData point);
	}
}