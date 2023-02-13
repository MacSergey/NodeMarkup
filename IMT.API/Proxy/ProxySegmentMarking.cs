using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IMT.API.Proxy
{
    internal class ProxySegmentMarking : ISegmentMarkingData
    {
        private object realProvider;
        public ProxySegmentMarking(object realProvider)
        {
            this.realProvider = realProvider;
        }

        public IEnumerable<INodeEntranceData> Entrances => throw new NotImplementedException();

        public INodeEntranceData StartEntrance => throw new NotImplementedException();

        public INodeEntranceData EndEntrance => throw new NotImplementedException();

        public IDataProviderV1 DataProvider => throw new NotImplementedException();

        public MarkingType Type => throw new NotImplementedException();

        public ushort Id => throw new NotImplementedException();

        public int EntranceCount => throw new NotImplementedException();

        public IFillerData AddFiller(IEnumerable<IEntrancePointData> pointDatas, IFillerStyleData style)
        {
            throw new NotImplementedException();
        }

        public ILaneLineData AddLaneLine(ILanePointData startPoint, ILanePointData endPoint, ILaneLineStyleData style)
        {
            throw new NotImplementedException();
        }

        public IRegularLineData AddRegularLine(IEntrancePointData startPoint, IEntrancePointData endPoint, IRegularLineStyleData style)
        {
            throw new NotImplementedException();
        }

        public void ClearMarkings()
        {
            throw new NotImplementedException();
        }

        public bool LaneLineExist(ILanePointData startPoint, ILanePointData endPoint)
        {
            throw new NotImplementedException();
        }

        public bool RegularLineExist(IEntrancePointData startPoint, IEntrancePointData endPoint)
        {
            throw new NotImplementedException();
        }

        public bool RemoveFiller(IFillerData filler)
        {
            throw new NotImplementedException();
        }

        public bool RemoveLaneLine(ILanePointData startPoint, ILanePointData endPoint)
        {
            throw new NotImplementedException();
        }

        public bool RemoveRegularLine(IEntrancePointData startPoint, IEntrancePointData endPoint)
        {
            throw new NotImplementedException();
        }

        public void ResetPointOffsets()
        {
            throw new NotImplementedException();
        }

        public bool TryGetEntrance(ushort nodeId, out INodeEntranceData entrance)
        {
            throw new NotImplementedException();
        }

        public bool TryGetLaneLine(ILanePointData startPointData, ILanePointData endPointData, out ILaneLineData laneLine)
        {
            throw new NotImplementedException();
        }

        public bool TryGetRegularLine(IEntrancePointData startPointData, IEntrancePointData endPointData, out IRegularLineData regularLine)
        {
            throw new NotImplementedException();
        }
    }
}
