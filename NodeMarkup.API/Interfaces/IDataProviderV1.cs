using System;

namespace NodeMarkup.API
{
	public interface IDataProvider
	{
		Version ModVersion { get; }
		bool IsBeta { get; }

		void Log(string message);
	}

	public interface IDataProviderV1 : IDataProvider
	{
		INodeMarkingApi GetNodeMarking(ushort id);
		ISegmentMarkingApi GetSegmentMarking(ushort id);
		bool NodeMarkingExist(ushort id);
		bool SegmentMarkingExist(ushort id);
	}
}