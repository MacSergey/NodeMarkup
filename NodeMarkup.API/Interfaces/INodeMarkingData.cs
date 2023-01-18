using System.Collections.Generic;

namespace NodeMarkup.API
{
	public interface INodeMarkingData : IMarkingData
	{
		IEnumerable<INodeEntranceData> Entrances { get; }
		ushort NodeId { get; }
	}
}