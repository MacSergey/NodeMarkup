using IMT.API;
using IMT.Manager;
using System.Collections.Generic;
using EntranceType = IMT.API.EntranceType;

namespace IMT.Utilities.API
{
    public struct SegmentEntranceDataProvider : ISegmentEntranceData
    {
        public DataProvider DataProvider { get; }
        IDataProviderV1 IEntranceData.DataProvider => DataProvider;

        public EntranceType Type => EntranceType.Segment;
        public ushort MarkingId { get; }
        public ushort Id { get; }

        private INodeMarkingData MarkingData
        {
            get
            {
                if (DataProvider.TryGetNodeMarking(MarkingId, out INodeMarkingData marking))
                    return marking;
                else
                    return null;
            }
        }

        IMarkingData IEntranceData.Marking => MarkingData;
        INodeMarkingData ISegmentEntranceData.Marking => MarkingData;

        private SegmentEntrance Entrance => APIHelper.GetSegmentEntrance(MarkingId, Id);
        public int PointCount => Entrance.PointCount;

        public IEnumerable<IPointData> Points
        {
            get
            {
                foreach (var point in Entrance.EnterPoints)
                {
                    yield return new EntrancePointDataProvider(DataProvider, point);
                }
            }
        }
        public IEnumerable<IEntrancePointData> EntrancePoints
        {
            get
            {
                foreach (var point in Entrance.EnterPoints)
                {
                    yield return new EntrancePointDataProvider(DataProvider, point);
                }
            }
        }
        public IEnumerable<INormalPointData> NormalPoints
        {
            get
            {
                foreach (var point in Entrance.NormalPoints)
                {
                    yield return new NormalPointDataProvider(DataProvider, point);
                }
            }
        }
        public IEnumerable<ICrosswalkPointData> CrosswalkPoints
        {
            get
            {
                foreach (var point in Entrance.CrosswalkPoints)
                {
                    yield return new CrosswalkPointDataProvider(DataProvider, point);
                }
            }
        }
        public IEnumerable<ILanePointData> LanePoints
        {
            get
            {
                foreach (var point in Entrance.LanePoints)
                {
                    yield return new LanePointDataProvider(DataProvider, point);
                }
            }
        }

        public SegmentEntranceDataProvider(DataProvider dataProvider, SegmentEntrance enter)
        {
            DataProvider = dataProvider;
            Id = enter.Id;
            MarkingId = enter.Marking.Id;
        }

        public bool GetEntrancePoint(byte index, out IEntrancePointData pointData)
        {
            if (Entrance.TryGetPoint(index, MarkingPoint.PointType.Enter, out var point))
            {
                pointData = new EntrancePointDataProvider(DataProvider, point as MarkingEnterPoint);
                return true;
            }
            else
            {
                pointData = null;
                return false;
            }
        }
        public bool GetNormalPoint(byte index, out INormalPointData pointData)
        {
            if (Entrance.TryGetPoint(index, MarkingPoint.PointType.Normal, out var point))
            {
                pointData = new NormalPointDataProvider(DataProvider, point as MarkingNormalPoint);
                return true;
            }
            else
            {
                pointData = null;
                return false;
            }
        }
        public bool GetCrosswalkPoint(byte index, out ICrosswalkPointData pointData)
        {
            if (Entrance.TryGetPoint(index, MarkingPoint.PointType.Crosswalk, out var point))
            {
                pointData = new CrosswalkPointDataProvider(DataProvider, point as MarkingCrosswalkPoint);
                return true;
            }
            else
            {
                pointData = null;
                return false;
            }
        }
        public bool GetLanePoint(byte index, out ILanePointData pointData)
        {
            if (Entrance.TryGetPoint(index, MarkingPoint.PointType.Lane, out var point))
            {
                pointData = new LanePointDataProvider(DataProvider, point as MarkingLanePoint);
                return true;
            }
            else
            {
                pointData = null;
                return false;
            }
        }

        public override string ToString() => Id.ToString();
    }
    public struct NodeEntranceDataProvider : INodeEntranceData
    {
        public DataProvider DataProvider { get; }
        IDataProviderV1 IEntranceData.DataProvider => DataProvider;

        public EntranceType Type => EntranceType.Node;
        public ushort MarkingId { get; }
        public ushort Id { get; }

        private ISegmentMarkingData MarkingData
        {
            get
            {
                if (DataProvider.TryGetSegmentMarking(MarkingId, out ISegmentMarkingData marking))
                    return marking;
                else
                    return null;
            }
        }

        IMarkingData IEntranceData.Marking => MarkingData;
        ISegmentMarkingData INodeEntranceData.Marking => MarkingData;

        private NodeEntrance Entrance => APIHelper.GetNodeEntrance(MarkingId, Id);
        public int PointCount => Entrance.PointCount;

        public IEnumerable<IPointData> Points
        {
            get
            {
                foreach (var point in Entrance.EnterPoints)
                {
                    yield return new EntrancePointDataProvider(DataProvider, point);
                }
            }
        }
        public IEnumerable<IEntrancePointData> EntrancePoints
        {
            get
            {
                foreach (var point in Entrance.EnterPoints)
                {
                    yield return new EntrancePointDataProvider(DataProvider, point);
                }
            }
        }

        public IEnumerable<ILanePointData> LanePoints
        {
            get
            {
                foreach (var point in Entrance.LanePoints)
                {
                    yield return new LanePointDataProvider(DataProvider, point);
                }
            }
        }

        public NodeEntranceDataProvider(DataProvider dataProvider, NodeEntrance enter)
        {
            DataProvider = dataProvider;
            Id = enter.Id;
            MarkingId = enter.Marking.Id;
        }

        public bool GetEntrancePoint(byte index, out IEntrancePointData pointData)
        {
            if (Entrance.TryGetPoint(index, MarkingPoint.PointType.Enter, out var point))
            {
                pointData = new EntrancePointDataProvider(DataProvider, point as MarkingEnterPoint);
                return true;
            }
            else
            {
                pointData = null;
                return false;
            }
        }
        public bool GetLanePoint(byte index, out ILanePointData pointData)
        {
            if (Entrance.TryGetPoint(index, MarkingPoint.PointType.Lane, out var point))
            {
                pointData = new LanePointDataProvider(DataProvider, point as MarkingLanePoint);
                return true;
            }
            else
            {
                pointData = null;
                return false;
            }
        }

        public override string ToString() => Id.ToString();
    }
}
