using ModsCommon;

using NodeMarkup.API.Applicators;
using NodeMarkup.Manager;

using System.Collections.Generic;

namespace NodeMarkup.API.Implementations
{
	public sealed class NodeMarkupApi : BaseMarkupApi, INodeMarkingApi
	{
		public ushort NodeId { get; }

		public NodeMarkupApi(ushort id, IDataProvider provider) 
			: base(provider, SingletonManager<NodeMarkupManager>.Instance.GetOrCreateMarkup(id))
		{ NodeId = id; }

		public IEnumerable<INodeEntranceData> Entrances
		{
			get
			{
				foreach (var item in (Markup as Manager.NodeMarkup).Enters)
				{
					yield return new NodeEntranceData(item);
				}
			}
		}
	}

	public sealed class SegmentMarkupApi : BaseMarkupApi, ISegmentMarkingApi
	{
		public ushort SegmentId { get; }

		public SegmentMarkupApi(ushort id, IDataProvider provider)
			: base(provider, SingletonManager<SegmentMarkupManager>.Instance.GetOrCreateMarkup(id))
		{ SegmentId = id; }

		public IEnumerable<ISegmentEntranceData> Entrances
		{
			get
			{
				foreach (var item in (Markup as SegmentMarkup).Enters)
				{
					yield return new SegmentEntranceData(item);
				}
			}
		}
	}
}
