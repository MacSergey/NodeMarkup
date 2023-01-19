using ModsCommon;

using NodeMarkup.API.Applicators;
using NodeMarkup.Manager;

using System.Linq;

namespace NodeMarkup.API.Implementations
{
	public class SegmentMarkupApi : BaseMarkupApi, ISegmentMarkingApi
	{
		public ushort SegmentId { get; }

		public SegmentMarkupApi(ushort id, IDataProvider provider)
			: base(provider, SingletonManager<SegmentMarkupManager>.Instance.GetOrCreateMarkup(id))
		{
			SegmentId = id; 
		}

		public ISegmentEntranceData StartEntrance => new SegmentEntranceData(Markup.Enters.First(x => x.IsStartSide) as NodeEnter);
		public ISegmentEntranceData EndEntrance => new SegmentEntranceData(Markup.Enters.First(x => !x.IsStartSide) as NodeEnter);

		public bool TryGetEntrance(ushort nodeId, out ISegmentEntranceData entrance)
		{
			foreach (var item in (Markup as SegmentMarkup).Enters)
			{
				if (nodeId == item.Id)
				{
					entrance = new SegmentEntranceData(item);
					return true;
				}
			}

			entrance = null;
			return false;
		}
	}
}
