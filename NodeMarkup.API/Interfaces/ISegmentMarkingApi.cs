using System.Collections.Generic;

namespace NodeMarkup.API
{
	public interface ISegmentMarkingApi : IMarkingApi
	{
		ushort SegmentId { get; }
		IEnumerable<ISegmentEntranceData> Entrances { get; }
	}
}