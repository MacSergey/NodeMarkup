using System.Collections.Generic;

namespace IMT.API
{
    public interface IEntranceData
    {
        IDataProviderV1 DataProvider { get; }
        IMarkingData Marking { get; }
        ushort MarkingId { get; }

        EntranceType Type { get; }
        ushort Id { get; }
        int PointCount { get; }

        IEnumerable<IPointData> Points { get; }
    }
    public interface ISegmentEntranceData : IEntranceData
    {
        new INodeMarkingData Marking { get; }
        IEnumerable<IEntrancePointData> EntrancePoints { get; }
        IEnumerable<INormalPointData> NormalPoints { get; }
        IEnumerable<ICrosswalkPointData> CrosswalkPoints { get; }
        IEnumerable<ILanePointData> LanePoints { get; }

        bool GetEntrancePoint(byte index, out IEntrancePointData point);
        bool GetNormalPoint(byte index, out INormalPointData point);
        bool GetCrosswalkPoint(byte index, out ICrosswalkPointData point);
        bool GetLanePoint(byte index, out ILanePointData point);
    }
    public interface INodeEntranceData : IEntranceData
    {
        new ISegmentMarkingData Marking { get; }
        IEnumerable<IEntrancePointData> EntrancePoints { get; }
        IEnumerable<ILanePointData> LanePoints { get; }

        bool GetEntrancePoint(byte index, out IEntrancePointData point);
        bool GetLanePoint(byte index, out ILanePointData point);
    }
}