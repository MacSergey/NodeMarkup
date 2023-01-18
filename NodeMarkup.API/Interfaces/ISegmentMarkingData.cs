using System.Collections.Generic;

namespace NodeMarkup.API
{
	public interface ISegmentMarkingData : IMarkingData
	{
        ushort SegmentId { get; }
		IEnumerable<ISegmentEntranceData> Entrances { get; }
	}
}