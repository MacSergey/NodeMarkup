using System.Collections.Generic;

namespace NodeMarkup.API
{
    public interface IMarkingData
    {
        IDataProviderV1 DataProvider { get; }
        MarkingType Type { get; }
        ushort Id { get; }
        int EntranceCount { get; }

        IRegularLineData AddRegularLine(IEntrancePointData startPoint, IEntrancePointData endPoint, IRegularLineStyleData style);
        ILaneLineData AddLaneLine(ILanePointData startPoint, ILanePointData endPoint, ILaneLineStyleData style);
        IFillerData AddFiller(IEnumerable<IEntrancePointData> pointDatas, IFillerStyleData style);

        bool TryGetRegularLine(IEntrancePointData startPointData, IEntrancePointData endPointData, out IRegularLineData regularLine);
        bool TryGetLaneLine(ILanePointData startPointData, ILanePointData endPointData, out ILaneLineData laneLine);

        bool RemoveRegularLine(IEntrancePointData startPoint, IEntrancePointData endPoint);
        bool RemoveLaneLine(ILanePointData startPoint, ILanePointData endPoint);
        bool RemoveFiller(IFillerData filler);

        bool RegularLineExist(IEntrancePointData startPoint, IEntrancePointData endPoint);
        bool LaneLineExist(ILanePointData startPoint, ILanePointData endPoint);

        void ClearMarkings();
        void ResetPointOffsets();
    }
    public interface INodeMarkingData : IMarkingData
    {
        IEnumerable<ISegmentEntranceData> Entrances { get; }

        bool TryGetEntrance(ushort segmentId, out ISegmentEntranceData entrance);

        IStopLineData AddStopLine(IEntrancePointData startPoint, IEntrancePointData endPoint, IStopLineStyleData style);
        INormalLineData AddNormalLine(IEntrancePointData startPoint, INormalPointData endPoint, INormalLineStyleData style);
        ICrosswalkData AddCrosswalk(ICrosswalkPointData startPoint, ICrosswalkPointData endPoint, ICrosswalkStyleData style);

        bool TryGetNormalLine(IEntrancePointData startPointData, INormalPointData endPointData, out INormalLineData normalLine);
        bool TryGetStopLine(IEntrancePointData startPointData, IEntrancePointData endPointData, out IStopLineData stopLine);
        bool TryGetCrosswalk(ICrosswalkPointData startPointData, ICrosswalkPointData endPointData, out ICrosswalkData crosswalk);

        bool RemoveStopLine(IEntrancePointData startPoint, IEntrancePointData endPoint);
        bool RemoveNormalLine(IEntrancePointData startPoint, INormalPointData endPoint);
        bool RemoveCrosswalk(ICrosswalkPointData startPoint, ICrosswalkPointData endPoint);

        bool NormalLineExist(IEntrancePointData startPoint, INormalPointData endPoint);
        bool StopLineExist(IEntrancePointData startPoint, IEntrancePointData endPoint);
        bool CrosswalkExist(ICrosswalkPointData startPoint, ICrosswalkPointData endPoint);
    }
    public interface ISegmentMarkingData : IMarkingData
    {
        IEnumerable<INodeEntranceData> Entrances { get; }
        INodeEntranceData StartEntrance { get; }
        INodeEntranceData EndEntrance { get; }

        bool TryGetEntrance(ushort nodeId, out INodeEntranceData entrance);
    }
}