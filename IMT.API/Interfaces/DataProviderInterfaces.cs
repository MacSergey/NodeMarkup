using System;
using System.Collections.Generic;

namespace NodeMarkup.API
{
    public interface IDataProviderFactory
    {
        IDataProviderV1 GetProvider(string id);
    }
    public interface IDataProviderV1
    {
        Version ModVersion { get; }
        bool IsBeta { get; }

        IEnumerable<string> RegularLineStyles { get; }
        IEnumerable<string> NormalLineStyles { get; }
        IEnumerable<string> StopLineStyles { get; }
        IEnumerable<string> LaneLineStyles { get; }
        IEnumerable<string> CrosswalkStyles { get; }
        IEnumerable<string> FillerStyles { get; }


        ISolidLineStyle SolidLineStyle { get; }
        IDoubleSolidLineStyle DoubleSolidLineStyle { get; }
        IDashedLineStyle DashedLineStyle { get; }
        IDoubleDashedLineStyle DoubleDashedLineStyle { get; }
        IDoubleDashedAsymLineStyle DoubleDashedAsymLineStyle { get; }
        ISolidAndDashedLineStyle SolidAndDashedLineStyle { get; }
        ISharkTeethLineStyle SharkTeethLineStyle { get; }
        IZigZagLineStyle ZigZagLineStyle { get; }
        IPavementLineStyle PavementLineStyle { get; }
        IPropLineStyle PropLineStyle { get; }
        ITreeLineStyle TreeLineStyle { get; }
        ITextLineStyle TextLineStyle { get; }
        INetworkLineStyle NetworkLineStyle { get; }


        ISolidStopLineStyle SolidStopLineStyle { get; }
        IDoubleSolidStopLineStyle DoubleSolidStopLineStyle { get; }
        IDashedStopLineStyle DashedStopLineStyle { get; }
        IDoubleDashedStopLineStyle DoubleDashedStopLineStyle { get; }
        ISolidAndDashedStopLineStyle SolidAndDashedStopLineStyle { get; }
        ISharkTeethStopLineStyle SharkTeethStopLineStyle { get; }
        IPavementStopLineStyle PavementStopLineStyle { get; }


        IExistentCrosswalkStyle ExistentCrosswalkStyle { get; }
        IZebraCrosswalkStyle ZebraCrosswalkStyle { get; }
        IDoubleZebraCrosswalkStyle DoubleZebraCrosswalkStyle { get; }
        IParallelSolidLinesCrosswalkStyle ParallelSolidLinesCrosswalkStyle { get; }
        IParallelDashedLinesCrosswalkStyle ParallelDashedLinesCrosswalkStyle { get; }
        ILadderCrosswalkStyle LadderCrosswalkStyle { get; }
        ISolidCrosswalkStyle SolidCrosswalkStyle { get; }
        IChessBoardCrosswalkStyle ChessBoardCrosswalkStyle { get; }


        IStripeFillerStyle StripeFillerStyle { get; }
        IGridFillerStyle GridFillerStyle { get; }
        ISolidFillerStyle SolidFillerStyle { get; }
        IChevronFillerStyle ChevronFillerStyle { get; }
        IPavementFillerStyle PavementFillerStyle { get; }
        IGrassFillerStyle GrassFillerStyle { get; }
        IGravelFillerStyle GravelFillerStyle { get; }
        IRuinedFillerStyle RuinedFillerStyle { get; }
        ICliffFillerStyle CliffFillerStyle { get; }


        bool TryGetNodeMarking(ushort id, out INodeMarkingData nodeMarking);
        bool TryGetSegmentMarking(ushort id, out ISegmentMarkingData segmentMarking);

        INodeMarkingData GetOrCreateNodeMarking(ushort id);
        ISegmentMarkingData GetOrCreateSegmentMarking(ushort id);

        bool NodeMarkingExist(ushort id);
        bool SegmentMarkingExist(ushort id);

        IRegularLineStyleData GetRegularLineStyle(RegularLineStyleType style);
        INormalLineStyleData GetNormalLineStyle(NormalLineStyleType style);
        IStopLineStyleData GetStopLineStyle(StopLineStyleType style);
        ILaneLineStyleData GetLaneLineStyle(LaneLineStyleType style);
        ICrosswalkStyleData GetCrosswalkStyle(CrosswalkStyleType style);
        IFillerStyleData GetFillerStyle(FillerStyleType style);
    }
}