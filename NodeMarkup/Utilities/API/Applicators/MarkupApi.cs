using ModsCommon;

using NodeMarkup.API.Implementations;
using NodeMarkup.Manager;

using System.Collections.Generic;

namespace NodeMarkup.API.Applicators
{
    public sealed class NodeMarkupApi : BaseMarkupApi, INodeMarkingData
    {
        public ushort NodeId { get; }

        public NodeMarkupApi(ushort id) : base(SingletonManager<NodeMarkupManager>.Instance.GetOrCreateMarkup(id))
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

    public sealed class SegmentMarkupApi : BaseMarkupApi, ISegmentMarkingData
    {
        public ushort SegmentId { get; }

        public SegmentMarkupApi(ushort id) : base(SingletonManager<SegmentMarkupManager>.Instance.GetOrCreateMarkup(id))
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
