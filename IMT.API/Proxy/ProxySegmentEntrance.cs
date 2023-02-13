using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IMT.API.Proxy
{
    internal class ProxySegmentEntrance : ISegmentEntranceData
    {
        private object realProvider;
        public ProxySegmentEntrance(object realProvider)
        {
            this.realProvider = realProvider;
        }

        public INodeMarkingData Marking => throw new NotImplementedException();

        public IEnumerable<IEntrancePointData> EntrancePoints => throw new NotImplementedException();

        public IEnumerable<INormalPointData> NormalPoints => throw new NotImplementedException();

        public IEnumerable<ICrosswalkPointData> CrosswalkPoints => throw new NotImplementedException();

        public IEnumerable<ILanePointData> LanePoints => throw new NotImplementedException();

        public IDataProviderV1 DataProvider => throw new NotImplementedException();

        public ushort MarkingId => throw new NotImplementedException();

        public EntranceType Type => throw new NotImplementedException();

        public ushort Id => throw new NotImplementedException();

        public int PointCount => throw new NotImplementedException();

        public IEnumerable<IPointData> Points => throw new NotImplementedException();

        IMarkingData IEntranceData.Marking => throw new NotImplementedException();

        public bool GetCrosswalkPoint(byte index, out ICrosswalkPointData point)
        {
            throw new NotImplementedException();
        }

        public bool GetEntrancePoint(byte index, out IEntrancePointData point)
        {
            throw new NotImplementedException();
        }

        public bool GetLanePoint(byte index, out ILanePointData point)
        {
            throw new NotImplementedException();
        }

        public bool GetNormalPoint(byte index, out INormalPointData point)
        {
            throw new NotImplementedException();
        }
    }
}
