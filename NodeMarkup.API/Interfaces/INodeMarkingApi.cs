using System.Collections.Generic;

namespace NodeMarkup.API
{
	public interface INodeMarkingApi : IMarkingApi
	{
		IEnumerable<INodeEntranceData> Entrances { get; }
		ushort NodeId { get; }

		bool TryGetEntrance(ushort segmentId, out INodeEntranceData entrance);
	}
}