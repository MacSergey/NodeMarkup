using IMT.API;
using IMT.Manager;
using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using static IMT.Manager.CrosswalkStyle;
using static IMT.Manager.FillerStyle;
using static IMT.Manager.RegularLineStyle;
using static IMT.Manager.StopLineStyle;

namespace IMT.Utilities.API
{
    public class DataProviderFactory : IDataProviderFactory
    {
        public IDataProviderV1 GetProvider(string id) => new DataProvider(id);
    }
    public class DataProvider : IDataProviderV1
    {
        public string Id { get; }
        public Version ModVersion => SingletonMod<Mod>.Instance.Version;
        public bool IsBeta => SingletonMod<Mod>.Instance.IsBeta;

        public IEnumerable<string> RegularLineStyles => GetStyles<RegularLineType>(LineType.Regular);
        public IEnumerable<string> NormalLineStyles => GetStyles<RegularLineType>(LineType.Regular);
        public IEnumerable<string> StopLineStyles => GetStyles<StopLineType>(LineType.Stop);
        public IEnumerable<string> LaneLineStyles => GetStyles<RegularLineType>(LineType.Lane);
        public IEnumerable<string> CrosswalkStyles
        {
            get
            {
                foreach (var type in EnumExtension.GetEnumValues<CrosswalkType>())
                {
                    if (type.IsVisible())
                        yield return type.ToString();
                }
            }
        }
        public IEnumerable<string> FillerStyles
        {
            get
            {
                foreach (var type in EnumExtension.GetEnumValues<FillerType>())
                {
                    if (type.IsVisible())
                        yield return type.ToString();
                }
            }
        }

        public ISolidLineStyle SolidLineStyle => GetLineStyleProvider(RegularLineType.Solid);
        public IDoubleSolidLineStyle DoubleSolidLineStyle => GetLineStyleProvider(RegularLineType.DoubleSolid);
        public IDashedLineStyle DashedLineStyle => GetLineStyleProvider(RegularLineType.Dashed);
        public IDoubleDashedLineStyle DoubleDashedLineStyle => GetLineStyleProvider(RegularLineType.DoubleDashed);
        public IDoubleDashedAsymLineStyle DoubleDashedAsymLineStyle => GetLineStyleProvider(RegularLineType.DoubleDashedAsym);
        public ISolidAndDashedLineStyle SolidAndDashedLineStyle => GetLineStyleProvider(RegularLineType.SolidAndDashed);
        public ISharkTeethLineStyle SharkTeethLineStyle => GetLineStyleProvider(RegularLineType.SharkTeeth);
        public IZigZagLineStyle ZigZagLineStyle => GetLineStyleProvider(RegularLineType.ZigZag);
        public IPavementLineStyle PavementLineStyle => GetLineStyleProvider(RegularLineType.Pavement);
        public IPropLineStyle PropLineStyle => GetLineStyleProvider(RegularLineType.Prop);
        public ITreeLineStyle TreeLineStyle => GetLineStyleProvider(RegularLineType.Tree);
        public ITextLineStyle TextLineStyle => GetLineStyleProvider(RegularLineType.Text);
        public INetworkLineStyle NetworkLineStyle => GetLineStyleProvider(RegularLineType.Network);


        public ISolidStopLineStyle SolidStopLineStyle => GetStopLineStyleProvider(StopLineType.Solid);
        public IDoubleSolidStopLineStyle DoubleSolidStopLineStyle => GetStopLineStyleProvider(StopLineType.DoubleSolid);
        public IDashedStopLineStyle DashedStopLineStyle => GetStopLineStyleProvider(StopLineType.Dashed);
        public IDoubleDashedStopLineStyle DoubleDashedStopLineStyle => GetStopLineStyleProvider(StopLineType.DoubleDashed);
        public ISolidAndDashedStopLineStyle SolidAndDashedStopLineStyle => GetStopLineStyleProvider(StopLineType.SolidAndDashed);
        public ISharkTeethStopLineStyle SharkTeethStopLineStyle => GetStopLineStyleProvider(StopLineType.SharkTeeth);
        public IPavementStopLineStyle PavementStopLineStyle => GetStopLineStyleProvider(StopLineType.Pavement);


        public IExistentCrosswalkStyle ExistentCrosswalkStyle => GetCrosswalkStyleProvider(CrosswalkType.Existent);
        public IZebraCrosswalkStyle ZebraCrosswalkStyle => GetCrosswalkStyleProvider(CrosswalkType.Zebra);
        public IDoubleZebraCrosswalkStyle DoubleZebraCrosswalkStyle => GetCrosswalkStyleProvider(CrosswalkType.DoubleZebra);
        public IParallelSolidLinesCrosswalkStyle ParallelSolidLinesCrosswalkStyle => GetCrosswalkStyleProvider(CrosswalkType.ParallelSolidLines);
        public IParallelDashedLinesCrosswalkStyle ParallelDashedLinesCrosswalkStyle => GetCrosswalkStyleProvider(CrosswalkType.ParallelDashedLines);
        public ILadderCrosswalkStyle LadderCrosswalkStyle => GetCrosswalkStyleProvider(CrosswalkType.Ladder);
        public ISolidCrosswalkStyle SolidCrosswalkStyle => GetCrosswalkStyleProvider(CrosswalkType.Solid);
        public IChessBoardCrosswalkStyle ChessBoardCrosswalkStyle => GetCrosswalkStyleProvider(CrosswalkType.ChessBoard);


        public IStripeFillerStyle StripeFillerStyle => GetFillerStyleProvider(FillerType.Stripe);
        public IGridFillerStyle GridFillerStyle => GetFillerStyleProvider(FillerType.Grid);
        public ISolidFillerStyle SolidFillerStyle => GetFillerStyleProvider(FillerType.Solid);
        public IChevronFillerStyle ChevronFillerStyle => GetFillerStyleProvider(FillerType.Chevron);
        public IPavementFillerStyle PavementFillerStyle => GetFillerStyleProvider(FillerType.Pavement);
        public IGrassFillerStyle GrassFillerStyle => GetFillerStyleProvider(FillerType.Grass);
        public IGravelFillerStyle GravelFillerStyle => GetFillerStyleProvider(FillerType.Gravel);
        public IRuinedFillerStyle RuinedFillerStyle => GetFillerStyleProvider(FillerType.Ruined);
        public ICliffFillerStyle CliffFillerStyle => GetFillerStyleProvider(FillerType.Cliff);

        private IEnumerable<string> GetStyles<StyleType>(LineType lineType)
            where StyleType : Enum
        {
            foreach (var type in EnumExtension.GetEnumValues<StyleType>())
            {
                if (type.IsVisible() && (type.GetLineType() & lineType) != 0)
                    yield return type.ToString();
            }
        }

        public DataProvider(string id)
        {
            Id = id;
            Log("Created");
        }

        public bool TryGetMarking(ushort id, Manager.MarkingType type, out IMarkingData marking)
        {
            if (type == Manager.MarkingType.Node)
            {
                if (TryGetNodeMarking(id, out var nodeMarking))
                {
                    marking = nodeMarking;
                    return true;
                }
                else
                {
                    marking = null;
                    return false;
                }
            }
            else if (type == Manager.MarkingType.Segment)
            {
                if (TryGetSegmentMarking(id, out var segmentMarking))
                {
                    marking = segmentMarking;
                    return true;
                }
                else
                {
                    marking = null;
                    return false;
                }
            }
            else
                throw new IntersectionMarkingToolException($"Unsupported type of marking: {type}");
        }

        public bool TryGetNodeMarking(ushort id, out INodeMarkingData nodeMarkingData)
        {
            if (APIHelper.GetNodeMarking(id, false) is NodeMarking nodeMarking)
            {
                nodeMarkingData = new NodeMarkingDataProvider(this, nodeMarking);
                return true;
            }
            else
            {
                nodeMarkingData = default;
                return false;
            }
        }
        public bool TryGetSegmentMarking(ushort id, out ISegmentMarkingData segmentMarkingData)
        {
            if (APIHelper.GetSegmentMarking(id, false) is SegmentMarking segmentMarking)
            {
                segmentMarkingData = new SegmentMarkingDataProvider(this, segmentMarking);
                return true;
            }
            else
            {
                segmentMarkingData = default;
                return false;
            }
        }

        public bool TryGetEntrance(ushort markingId, ushort entranceId, Manager.MarkingType type, out IEntranceData entranceData)
        {
            if (type == Manager.MarkingType.Node)
            {
                if (TryGetSegmentEntrance(markingId, entranceId, out var segmentEntrance))
                {
                    entranceData = segmentEntrance;
                    return true;
                }
                else
                {
                    entranceData = null;
                    return false;
                }
            }
            else if (type == Manager.MarkingType.Segment)
            {
                if (TryGetNodeEntrance(markingId, entranceId, out var nodeEntrance))
                {
                    entranceData = nodeEntrance;
                    return true;
                }
                else
                {
                    entranceData = null;
                    return false;
                }
            }
            else
                throw new IntersectionMarkingToolException($"Unsupported type of marking: {type}");
        }
        public bool TryGetSegmentEntrance(ushort markingId, ushort entranceId, out ISegmentEntranceData entranceData)
        {
            if (APIHelper.GetSegmentEntrance(markingId, entranceId, false) is SegmentEntrance segmentEntrance)
            {
                entranceData = new SegmentEntranceDataProvider(this, segmentEntrance);
                return true;
            }
            else
            {
                entranceData = default;
                return false;
            }
        }
        public bool TryGetNodeEntrance(ushort markingId, ushort entranceId, out INodeEntranceData entranceData)
        {
            if (APIHelper.GetNodeEntrance(markingId, entranceId, false) is NodeEntrance nodeEntrance)
            {
                entranceData = new NodeEntranceDataProvider(this, nodeEntrance);
                return true;
            }
            else
            {
                entranceData = default;
                return false;
            }
        }

        public INodeMarkingData GetOrCreateNodeMarking(ushort id)
        {
            var nodeMarking = SingletonManager<NodeMarkingManager>.Instance.GetOrCreateMarking(id);
            return new NodeMarkingDataProvider(this, nodeMarking);
        }
        public ISegmentMarkingData GetOrCreateSegmentMarking(ushort id)
        {
            var segmentMarking = SingletonManager<SegmentMarkingManager>.Instance.GetOrCreateMarking(id);
            return new SegmentMarkingDataProvider(this, segmentMarking);
        }

        public bool NodeMarkingExist(ushort id) => SingletonManager<NodeMarkingManager>.Instance.Exist(id);
        public bool SegmentMarkingExist(ushort id) => SingletonManager<SegmentMarkingManager>.Instance.Exist(id);

        private StyleDataProvider GetLineStyleProvider(RegularLineStyle.RegularLineType type)
        {
            var style = RegularLineStyle.GetDefault(type);
            var styleData = new StyleDataProvider(style);
            return styleData;
        }
        private StyleDataProvider GetStopLineStyleProvider(StopLineStyle.StopLineType type)
        {
            var style = StopLineStyle.GetDefault(type);
            var styleData = new StyleDataProvider(style);
            return styleData;
        }
        private StyleDataProvider GetCrosswalkStyleProvider(CrosswalkStyle.CrosswalkType type)
        {
            var style = CrosswalkStyle.GetDefault(type);
            var styleData = new StyleDataProvider(style);
            return styleData;
        }
        private StyleDataProvider GetFillerStyleProvider(FillerStyle.FillerType type)
        {
            var style = FillerStyle.GetDefault(type);
            var styleData = new StyleDataProvider(style);
            return styleData;
        }

        public IRegularLineStyleData GetRegularLineStyle(RegularLineStyleType regularType)
        {
            var type = APIHelper.GetStyleType<RegularLineType>(regularType.ToString());

            if (!type.IsVisible() || (type.GetLineType() & LineType.Regular) == 0)
                throw new IntersectionMarkingToolException($"No style with name {regularType}");

            return GetLineStyleProvider(type);
        }
        public INormalLineStyleData GetNormalLineStyle(NormalLineStyleType normalType)
        {
            var type = APIHelper.GetStyleType<RegularLineType>(normalType.ToString());

            if (!type.IsVisible() || (type.GetLineType() & LineType.Regular) == 0)
                throw new IntersectionMarkingToolException($"No style with name {normalType}");

            return GetLineStyleProvider(type);
        }
        public IStopLineStyleData GetStopLineStyle(StopLineStyleType stopType)
        {
            var type = APIHelper.GetStyleType<StopLineType>(stopType.ToString());

            if (!type.IsVisible())
                throw new IntersectionMarkingToolException($"No style with name {stopType}");

            var style = StopLineStyle.GetDefault(type);
            var styleData = new StyleDataProvider(style);
            return styleData;
        }
        public ILaneLineStyleData GetLaneLineStyle(LaneLineStyleType laneType)
        {
            var type = APIHelper.GetStyleType<RegularLineType>(laneType.ToString());

            if (!type.IsVisible() || (type.GetLineType() & LineType.Lane) == 0)
                throw new IntersectionMarkingToolException($"No style with name {laneType}");

            return GetLineStyleProvider(type);
        }
        public ICrosswalkStyleData GetCrosswalkStyle(CrosswalkStyleType crosswalkType)
        {
            var type = APIHelper.GetStyleType<CrosswalkType>(crosswalkType.ToString());

            if (!type.IsVisible())
                throw new IntersectionMarkingToolException($"No style with name {crosswalkType}");

            return GetCrosswalkStyleProvider(type);
        }
        public IFillerStyleData GetFillerStyle(FillerStyleType fillerType)
        {
            var type = APIHelper.GetStyleType<FillerType>(fillerType.ToString());

            if (!type.IsVisible())
                throw new IntersectionMarkingToolException($"No style with name {fillerType}");

            return GetFillerStyleProvider(type);
        }

        internal void Log(string message) => SingletonMod<Mod>.Logger.Debug($"[{Id} Provider] {message}");
    }
}
