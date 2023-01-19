using System.Collections.Generic;

namespace NodeMarkup.API
{
	public interface ISegmentMarkingApi : IMarkingApi
	{
		ushort SegmentId { get; }
		ISegmentEntranceData StartEntrance { get; }
		ISegmentEntranceData EndEntrance { get; }

		bool TryGetEntrance(ushort nodeId, out ISegmentEntranceData entrance);
	}
}