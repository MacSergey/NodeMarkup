using System.Collections.Generic;

namespace NodeMarkup.API
{
	public interface INodeEntranceData : IEntranceData
	{
		IEnumerable<INormalPointData> NormalPoints { get; }
		IEnumerable<ICrosswalkPointData> CrosswalkPoints { get; }
		IEnumerable<ILanePointData> LanePoints { get; }

		bool GetEntrancePoint(byte index, out IEntrancePointData point);
		bool GetNormalPoint(byte index, out INormalPointData point);
		bool GetCrosswalkPoint(byte index, out ICrosswalkPointData point);
		bool GetLanePoint(byte index, out ILanePointData point);
	}
}