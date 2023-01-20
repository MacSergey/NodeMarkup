using System.Collections.Generic;

namespace NodeMarkup.API
{
    public interface IEntranceData
    {
        ushort Id { get; }
        int PointCount { get; }
        IEnumerable<IPointData> Points { get; }
    }
    public interface ISegmentEntranceData : IEntranceData
    {
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
        IEnumerable<IEntrancePointData> EntrancePoints { get; }
        IEnumerable<ILanePointData> LanePoints { get; }

        bool GetEntrancePoint(byte index, out IEntrancePointData point);
        bool GetLanePoint(byte index, out ILanePointData point);
    }
}