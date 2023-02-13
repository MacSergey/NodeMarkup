using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace IMT.API.Proxy
{
    internal class ProxyDataProvider : IDataProviderV1
    {
        internal readonly object realProvider;
        internal readonly Assembly assembly;
        public ProxyDataProvider(object realProvider, Assembly assembly)
        {
            this.realProvider = realProvider;
            this.assembly = assembly;
        }

        public Version ModVersion => (Version)Helper.GetPropertyValue(realProvider, nameof(ModVersion));

        public bool IsBeta => (bool)Helper.GetPropertyValue(realProvider, nameof(IsBeta));

        public IEnumerable<string> RegularLineStyles => (IEnumerable<string>)Helper.GetPropertyValue(realProvider, nameof(RegularLineStyles));

        public IEnumerable<string> NormalLineStyles => (IEnumerable<string>)Helper.GetPropertyValue(realProvider, nameof(NormalLineStyles));

        public IEnumerable<string> StopLineStyles => (IEnumerable<string>)Helper.GetPropertyValue(realProvider, nameof(StopLineStyles));

        public IEnumerable<string> LaneLineStyles => (IEnumerable<string>)Helper.GetPropertyValue(realProvider, nameof(LaneLineStyles));

        public IEnumerable<string> CrosswalkStyles => (IEnumerable<string>)Helper.GetPropertyValue(realProvider, nameof(CrosswalkStyles));

        public IEnumerable<string> FillerStyles => (IEnumerable<string>)Helper.GetPropertyValue(realProvider, nameof(FillerStyles));

        public ISolidLineStyle SolidLineStyle => throw new NotImplementedException();

        public IDoubleSolidLineStyle DoubleSolidLineStyle => throw new NotImplementedException();

        public IDashedLineStyle DashedLineStyle => throw new NotImplementedException();

        public IDoubleDashedLineStyle DoubleDashedLineStyle => throw new NotImplementedException();

        public IDoubleDashedAsymLineStyle DoubleDashedAsymLineStyle => throw new NotImplementedException();

        public ISolidAndDashedLineStyle SolidAndDashedLineStyle => throw new NotImplementedException();

        public ISharkTeethLineStyle SharkTeethLineStyle => throw new NotImplementedException();

        public IZigZagLineStyle ZigZagLineStyle => throw new NotImplementedException();

        public IPavementLineStyle PavementLineStyle => throw new NotImplementedException();

        public IPropLineStyle PropLineStyle => throw new NotImplementedException();

        public ITreeLineStyle TreeLineStyle => throw new NotImplementedException();

        public ITextLineStyle TextLineStyle => throw new NotImplementedException();

        public INetworkLineStyle NetworkLineStyle => throw new NotImplementedException();

        public ISolidStopLineStyle SolidStopLineStyle => throw new NotImplementedException();

        public IDoubleSolidStopLineStyle DoubleSolidStopLineStyle => throw new NotImplementedException();

        public IDashedStopLineStyle DashedStopLineStyle => throw new NotImplementedException();

        public IDoubleDashedStopLineStyle DoubleDashedStopLineStyle => throw new NotImplementedException();

        public ISolidAndDashedStopLineStyle SolidAndDashedStopLineStyle => throw new NotImplementedException();

        public ISharkTeethStopLineStyle SharkTeethStopLineStyle => throw new NotImplementedException();

        public IPavementStopLineStyle PavementStopLineStyle => throw new NotImplementedException();

        public IExistentCrosswalkStyle ExistentCrosswalkStyle => throw new NotImplementedException();

        public IZebraCrosswalkStyle ZebraCrosswalkStyle => throw new NotImplementedException();

        public IDoubleZebraCrosswalkStyle DoubleZebraCrosswalkStyle => throw new NotImplementedException();

        public IParallelSolidLinesCrosswalkStyle ParallelSolidLinesCrosswalkStyle => throw new NotImplementedException();

        public IParallelDashedLinesCrosswalkStyle ParallelDashedLinesCrosswalkStyle => throw new NotImplementedException();

        public ILadderCrosswalkStyle LadderCrosswalkStyle => throw new NotImplementedException();

        public ISolidCrosswalkStyle SolidCrosswalkStyle => throw new NotImplementedException();

        public IChessBoardCrosswalkStyle ChessBoardCrosswalkStyle => throw new NotImplementedException();

        public IStripeFillerStyle StripeFillerStyle => throw new NotImplementedException();

        public IGridFillerStyle GridFillerStyle => throw new NotImplementedException();

        public ISolidFillerStyle SolidFillerStyle => throw new NotImplementedException();

        public IChevronFillerStyle ChevronFillerStyle => throw new NotImplementedException();

        public IPavementFillerStyle PavementFillerStyle => throw new NotImplementedException();

        public IGrassFillerStyle GrassFillerStyle => throw new NotImplementedException();

        public IGravelFillerStyle GravelFillerStyle => throw new NotImplementedException();

        public IRuinedFillerStyle RuinedFillerStyle => throw new NotImplementedException();

        public ICliffFillerStyle CliffFillerStyle => throw new NotImplementedException();

        public ICrosswalkStyleData GetCrosswalkStyle(CrosswalkStyleType style)
        {
            throw new NotImplementedException();
        }

        public IFillerStyleData GetFillerStyle(FillerStyleType style)
        {
            throw new NotImplementedException();
        }

        public ILaneLineStyleData GetLaneLineStyle(LaneLineStyleType style)
        {
            throw new NotImplementedException();
        }

        public INormalLineStyleData GetNormalLineStyle(NormalLineStyleType style)
        {
            throw new NotImplementedException();
        }

        public INodeMarkingData GetOrCreateNodeMarking(ushort id)
        {
            var nodeProvider = Helper.InvokeMethod(realProvider, nameof(GetOrCreateNodeMarking), new Type[] { typeof(ushort) }, new object[] { id });
            return new ProxyNodeMarking(this, nodeProvider);
        }

        public ISegmentMarkingData GetOrCreateSegmentMarking(ushort id)
        {
            var segmentProvider = Helper.InvokeMethod(realProvider, nameof(GetOrCreateSegmentMarking), new Type[] { typeof(ushort) }, new object[] { id });
            return new ProxySegmentMarking(segmentProvider);
        }

        public IRegularLineStyleData GetRegularLineStyle(RegularLineStyleType style)
        {
            throw new NotImplementedException();
            //Helper.InvokeMethod(realProvider, nameof(GetRegularLineStyle), new Type[] { typeof(ushort) }, new object[] { id });
        }

        public IStopLineStyleData GetStopLineStyle(StopLineStyleType style)
        {
            throw new NotImplementedException();
            //Helper.InvokeMethod(realProvider, nameof(GetStopLineStyle), new Type[] { typeof(ushort) }, new object[] { id });
        }

        public bool NodeMarkingExist(ushort id)
        {
            return (bool)Helper.InvokeMethod(realProvider, nameof(NodeMarkingExist), new Type[] { typeof(ushort) }, new object[] { id });
        }

        public bool SegmentMarkingExist(ushort id)
        {
            return (bool)Helper.InvokeMethod(realProvider, nameof(SegmentMarkingExist), new Type[] { typeof(ushort) }, new object[] { id });
        }

        public bool TryGetNodeMarking(ushort id, out INodeMarkingData nodeMarking)
        {
            throw new NotImplementedException();
            //return (bool)Helper.InvokeMethod(realProvider, nameof(TryGetNodeMarking), new Type[] { typeof(ushort) }, new object[] { id });
        }

        public bool TryGetSegmentMarking(ushort id, out ISegmentMarkingData segmentMarking)
        {
            throw new NotImplementedException();
            //return (bool)Helper.InvokeMethod(realProvider, nameof(TryGetSegmentMarking), new Type[] { typeof(ushort) }, new object[] { id });
        }
    }
}
