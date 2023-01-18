using System.Collections.Generic;

namespace NodeMarkup.API
{
	public interface ISegmentEntranceData : IEntranceData
	{
		bool GetEntrancePoint(byte index, out IEntrancePointData point);
		bool GetLanePoint(byte index, out ILanePointData point);

		IEnumerable<ILanePointData> LanePoints { get; }
	}
}