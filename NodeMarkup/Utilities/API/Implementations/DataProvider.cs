using ModsCommon;
using NodeMarkup.API.Applicators;
using NodeMarkup.Manager;
using System;

namespace NodeMarkup.API.Implementations
{
    public class DataProviderFactory : IDataProviderFactory
    {
        public IDataProviderV1 GetProviderV1() => new DataProviderV1();
    }

    public class DataProviderV1 : IDataProviderV1
    {
        public Version ModVersion => SingletonMod<Mod>.Instance.Version;
        public bool IsBeta => SingletonMod<Mod>.Instance.IsBeta;

        public INodeMarkingApi GetNodeMarking(ushort id) => new NodeMarkupApi(id, this);
        public ISegmentMarkingApi GetSegmentMarking(ushort id) => new SegmentMarkupApi(id, this);
		public bool NodeMarkingExist(ushort id) => SingletonManager<NodeMarkupManager>.Instance.Exist(id);
        public bool SegmentMarkingExist(ushort id) => SingletonManager<SegmentMarkupManager>.Instance.Exist(id);

		public void Log(string message) => SingletonMod<Mod>.Logger.Debug($"[API] {message}");
	}
}
