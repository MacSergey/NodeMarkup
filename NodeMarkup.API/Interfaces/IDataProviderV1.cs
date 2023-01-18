using System;

namespace NodeMarkup.API
{
	public interface IDataProviderV1
	{
		Version ModVersion { get; }
		bool IsBeta { get; }

		INodeMarkingData GetNodeMarking(ushort id);
		ISegmentMarkingData GetSegmentMarking(ushort id);
		bool NodeMarkingExist(ushort id);
		bool SegmentMarkingExist(ushort id);
	}
}