using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IMT.API.Proxy
{
    internal struct ProxyNodeMarking : INodeMarkingData
    {
        private readonly ProxyDataProvider proxyProvider;
        private readonly object realProvider;
        public ProxyNodeMarking(ProxyDataProvider proxyProvider, object realProvider)
        {
            this.proxyProvider = proxyProvider;
            this.realProvider = realProvider;
        }
        public IEnumerable<ISegmentEntranceData> Entrances => throw new NotImplementedException();

        public IDataProviderV1 DataProvider => proxyProvider;

        public MarkingType Type => throw new NotImplementedException();

        public ushort Id => throw new NotImplementedException();

        public int EntranceCount => throw new NotImplementedException();

        public ICrosswalkData AddCrosswalk(ICrosswalkPointData startPoint, ICrosswalkPointData endPoint, ICrosswalkStyleData style)
        {
            throw new NotImplementedException();
        }

        public IFillerData AddFiller(IEnumerable<IEntrancePointData> pointDatas, IFillerStyleData style)
        {
            throw new NotImplementedException();
        }

        public ILaneLineData AddLaneLine(ILanePointData startPoint, ILanePointData endPoint, ILaneLineStyleData style)
        {
            throw new NotImplementedException();
        }

        public INormalLineData AddNormalLine(IEntrancePointData startPoint, INormalPointData endPoint, INormalLineStyleData style)
        {
            throw new NotImplementedException();
        }

        public IRegularLineData AddRegularLine(IEntrancePointData startPoint, IEntrancePointData endPoint, IRegularLineStyleData style)
        {
            throw new NotImplementedException();
        }

        public IStopLineData AddStopLine(IEntrancePointData startPoint, IEntrancePointData endPoint, IStopLineStyleData style)
        {
            throw new NotImplementedException();
        }

        public void ClearMarkings()
        {
            throw new NotImplementedException();
        }

        public bool CrosswalkExist(ICrosswalkPointData startPoint, ICrosswalkPointData endPoint)
        {
            throw new NotImplementedException();
        }

        public bool LaneLineExist(ILanePointData startPoint, ILanePointData endPoint)
        {
            throw new NotImplementedException();
        }

        public bool NormalLineExist(IEntrancePointData startPoint, INormalPointData endPoint)
        {
            throw new NotImplementedException();
        }

        public bool RegularLineExist(IEntrancePointData startPoint, IEntrancePointData endPoint)
        {
            throw new NotImplementedException();
        }

        public bool RemoveCrosswalk(ICrosswalkPointData startPoint, ICrosswalkPointData endPoint)
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

        public bool RemoveNormalLine(IEntrancePointData startPoint, INormalPointData endPoint)
        {
            throw new NotImplementedException();
        }

        public bool RemoveRegularLine(IEntrancePointData startPoint, IEntrancePointData endPoint)
        {
            throw new NotImplementedException();
        }

        public bool RemoveStopLine(IEntrancePointData startPoint, IEntrancePointData endPoint)
        {
            throw new NotImplementedException();
        }

        public void ResetPointOffsets()
        {
            throw new NotImplementedException();
        }

        public bool StopLineExist(IEntrancePointData startPoint, IEntrancePointData endPoint)
        {
            throw new NotImplementedException();
        }

        public bool TryGetCrosswalk(ICrosswalkPointData startPointData, ICrosswalkPointData endPointData, out ICrosswalkData crosswalk)
        {
            throw new NotImplementedException();
        }

        public bool TryGetEntrance(ushort segmentId, out ISegmentEntranceData entrance)
        {
            var types = new Type[] { typeof(ushort), Helper.ConvertType(typeof(ISegmentEntranceData), proxyProvider.assembly).MakeByRefType() };
            var parameters = new object[] { segmentId, null};
            var result = (bool)Helper.InvokeMethod(realProvider, nameof(TryGetEntrance), types, parameters);
            entrance = new ProxySegmentEntrance(parameters[1]);
            return result;
        }

        public bool TryGetLaneLine(ILanePointData startPointData, ILanePointData endPointData, out ILaneLineData laneLine)
        {
            throw new NotImplementedException();
        }

        public bool TryGetNormalLine(IEntrancePointData startPointData, INormalPointData endPointData, out INormalLineData normalLine)
        {
            throw new NotImplementedException();
        }

        public bool TryGetRegularLine(IEntrancePointData startPointData, IEntrancePointData endPointData, out IRegularLineData regularLine)
        {
            throw new NotImplementedException();
        }

        public bool TryGetStopLine(IEntrancePointData startPointData, IEntrancePointData endPointData, out IStopLineData stopLine)
        {
            throw new NotImplementedException();
        }
    }
}
