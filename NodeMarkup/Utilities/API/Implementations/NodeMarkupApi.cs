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
		{ 
			NodeId = id; 
		}

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

		public bool TryGetEntrance(ushort segmentId, out INodeEntranceData entrance)
		{
			foreach (var item in (Markup as Manager.NodeMarkup).Enters)
			{
				if (segmentId == item.Id)
				{
					entrance = new NodeEntranceData(item);
					return true;
				}
			}

			entrance = null;
			return false;
		}
	}
}
