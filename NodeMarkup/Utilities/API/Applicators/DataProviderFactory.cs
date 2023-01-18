using ModsCommon;
using NodeMarkup.Manager;
using System;

namespace NodeMarkup.API.Applicators
{
	public class DataProviderFactory : IDataProviderFactory
	{
		public IDataProviderV1 GetProviderV1() => new DataProvider();
	}

	public class DataProvider : IDataProviderV1
	{
		public Version ModVersion => SingletonMod<Mod>.Instance.Version;
		public bool IsBeta => SingletonMod<Mod>.Instance.IsBeta;

		public INodeMarkingData GetNodeMarking(ushort id) => new NodeMarkupApi(id);
		public ISegmentMarkingData GetSegmentMarking(ushort id) => new SegmentMarkupApi(id);
		public bool NodeMarkingExist(ushort id) => SingletonManager<NodeMarkupManager>.Instance.Exist(id);
		public bool SegmentMarkingExist(ushort id) => SingletonManager<SegmentMarkupManager>.Instance.Exist(id);
	}
}
