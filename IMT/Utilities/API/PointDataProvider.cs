using NodeMarkup.API;
using NodeMarkup.Manager;

namespace NodeMarkup.Utilities.API
{
    public struct EntrancePointDataProvider : IEntrancePointData
    {
        public DataProvider DataProvider { get; }
        IDataProviderV1 IPointData.DataProvider => DataProvider;

        private Manager.MarkingType MarkingType { get; }
        private Manager.EntranceType EntranceType { get; }
        public ushort MarkingId { get; }
        public ushort EntranceId { get; }
        public byte Index { get; }

        IMarkingData IPointData.Marking
        {
            get
            {
                if (DataProvider.TryGetMarking(MarkingId, MarkingType, out IMarkingData marking))
                    return marking;
                else
                    return null;
            }
        }
        IEntranceData IPointData.Entrance
        {
            get
            {
                if (DataProvider.TryGetEntrance(MarkingId, EntranceId, MarkingType, out IEntranceData entrance))
                    return entrance;
                else
                    return null;
            }
        }
        private MarkingEnterPoint Point => APIHelper.GetPoint<MarkingEnterPoint>(MarkingId, EntranceId, EntranceType, Index, MarkingPoint.PointType.Enter);
        public IPointSourceData Source => new PointSourceDataProvider(Point.Source);
        public float Offset
        {
            get => Point.Offset;
            set => Point.Offset.Value = value;
        }

        public float Position => throw new System.NotImplementedException();

        public EntrancePointDataProvider(DataProvider dataProvider, MarkingEnterPoint point)
        {
            DataProvider = dataProvider;
            MarkingType = point.Marking.Type;
            EntranceType = point.Enter.Type;
            MarkingId = point.Marking.Id;
            EntranceId = point.Enter.Id;
            Index = point.Index;
        }

        public override string ToString() => Point.ToString();
    }
    public struct NormalPointDataProvider : INormalPointData
    {
        public DataProvider DataProvider { get; }
        IDataProviderV1 IPointData.DataProvider => DataProvider;

        private Manager.MarkingType MarkingType { get; }
        private Manager.EntranceType EntranceType { get; }
        public ushort MarkingId { get; }
        public ushort EntranceId { get; }
        public byte Index { get; }

        IMarkingData IPointData.Marking
        {
            get
            {
                if (DataProvider.TryGetMarking(MarkingId, MarkingType, out IMarkingData marking))
                    return marking;
                else
                    return null;
            }
        }
        IEntranceData IPointData.Entrance
        {
            get
            {
                if (DataProvider.TryGetEntrance(MarkingId, EntranceId, MarkingType, out IEntranceData entrance))
                    return entrance;
                else
                    return null;
            }
        }
        private MarkingNormalPoint Point => APIHelper.GetPoint<MarkingNormalPoint>(MarkingId, EntranceId, EntranceType, Index, MarkingPoint.PointType.Normal);
        public IEntrancePointData SourcePoint => new EntrancePointDataProvider(DataProvider, Point.SourcePoint);

        public NormalPointDataProvider(DataProvider dataProvider, MarkingNormalPoint point)
        {
            DataProvider = dataProvider;
            MarkingType = point.Marking.Type;
            EntranceType = point.Enter.Type;
            MarkingId = point.Marking.Id;
            EntranceId = point.Enter.Id;
            Index = point.Index;
        }

        public override string ToString() => Point.ToString();
    }
    public struct CrosswalkPointDataProvider : ICrosswalkPointData
    {
        public DataProvider DataProvider { get; }
        IDataProviderV1 IPointData.DataProvider => DataProvider;

        private Manager.MarkingType MarkingType { get; }
        private Manager.EntranceType EntranceType { get; }
        public ushort MarkingId { get; }
        public ushort EntranceId { get; }
        public byte Index { get; }

        IMarkingData IPointData.Marking
        {
            get
            {
                if (DataProvider.TryGetMarking(MarkingId, MarkingType, out IMarkingData marking))
                    return marking;
                else
                    return null;
            }
        }
        IEntranceData IPointData.Entrance
        {
            get
            {
                if (DataProvider.TryGetEntrance(MarkingId, EntranceId, MarkingType, out IEntranceData entrance))
                    return entrance;
                else
                    return null;
            }
        }
        private MarkingCrosswalkPoint Point => APIHelper.GetPoint<MarkingCrosswalkPoint>(MarkingId, EntranceId, EntranceType, Index, MarkingPoint.PointType.Crosswalk);
        public IEntrancePointData SourcePoint => new EntrancePointDataProvider(DataProvider, Point.SourcePoint);

        public CrosswalkPointDataProvider(DataProvider dataProvider, MarkingCrosswalkPoint point)
        {
            DataProvider = dataProvider;
            MarkingType = point.Marking.Type;
            EntranceType = point.Enter.Type;
            MarkingId = point.Marking.Id;
            EntranceId = point.Enter.Id;
            Index = point.Index;
        }

        public override string ToString() => Point.ToString();
    }
    public struct LanePointDataProvider : ILanePointData
    {
        public DataProvider DataProvider { get; }
        IDataProviderV1 IPointData.DataProvider => DataProvider;

        private Manager.MarkingType MarkingType { get; }
        private Manager.EntranceType EntranceType { get; }
        public ushort MarkingId { get; }
        public ushort EntranceId { get; }
        public byte Index { get; }

        IMarkingData IPointData.Marking
        {
            get
            {
                if (DataProvider.TryGetMarking(MarkingId, MarkingType, out IMarkingData marking))
                    return marking;
                else
                    return null;
            }
        }
        IEntranceData IPointData.Entrance
        {
            get
            {
                if (DataProvider.TryGetEntrance(MarkingId, EntranceId, MarkingType, out IEntranceData entrance))
                    return entrance;
                else
                    return null;
            }
        }
        private MarkingLanePoint Point => APIHelper.GetPoint<MarkingLanePoint>(MarkingId, EntranceId, EntranceType, Index, MarkingPoint.PointType.Lane);
        public IEntrancePointData SourcePointA => new EntrancePointDataProvider(DataProvider, Point.SourcePointA);
        public IEntrancePointData SourcePointB => new EntrancePointDataProvider(DataProvider, Point.SourcePointB);

        public LanePointDataProvider(DataProvider dataProvider, MarkingLanePoint point)
        {
            DataProvider = dataProvider;
            MarkingType = point.Marking.Type;
            EntranceType = point.Enter.Type;
            MarkingId = point.Marking.Id;
            EntranceId = point.Enter.Id;
            Index = point.Index;
        }

        public override string ToString() => Point.ToString();
    }

    public struct PointSourceDataProvider : IPointSourceData
    {
        public PointLocation Location { get; }
        public uint LeftLaneId { get; }
        public int LeftIndex { get; }
        public uint RightLaneId { get; }
        public int RightIndex { get; }

        public PointSourceDataProvider(PointLocation location, uint leftLaneId, int leftIndex, uint rightLaneId, int rightIndex)
        {
            Location = location;
            LeftLaneId = leftLaneId;
            LeftIndex = leftIndex;
            RightLaneId = rightLaneId;
            RightIndex = rightIndex;
        }
        public PointSourceDataProvider(NetInfoPointSource source)
        {
            Location = source.Location switch
            {
                MarkingPoint.LocationType.LeftEdge => PointLocation.Left,
                MarkingPoint.LocationType.RightEdge => PointLocation.Rigth,
                MarkingPoint.LocationType.Between => PointLocation.Between,
                _ => PointLocation.None,
            };

            if (source.LeftLane != null)
            {
                LeftLaneId = source.LeftLane.LaneId;
                LeftIndex = source.LeftLane.Index;
            }
            else
            {
                LeftLaneId = default;
                LeftIndex = default;
            }

            if (source.RightLane != null)
            {
                RightLaneId = source.RightLane.LaneId;
                RightIndex = source.RightLane.Index;
            }
            else
            {
                RightLaneId = default;
                RightIndex = default;
            }
        }
    }
}
