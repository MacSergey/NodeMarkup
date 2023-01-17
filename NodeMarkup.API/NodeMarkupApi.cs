using ModsCommon;

using NodeMarkup.API.Internal;
using NodeMarkup.Manager;

namespace NodeMarkup.API
{
	public sealed class NodeMarkupApi : BaseMarkupApi
	{
		public NodeMarkupApi(ushort id) : base(SingletonManager<NodeMarkupManager>.Instance.GetOrCreateMarkup(id))
		{
		}
	}

	public sealed class SegmentMarkupApi : BaseMarkupApi
	{
		public SegmentMarkupApi(ushort id) : base(SingletonManager<SegmentMarkupManager>.Instance.GetOrCreateMarkup(id))
		{
		}
	}
}
