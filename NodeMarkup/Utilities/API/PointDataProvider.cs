using NodeMarkup.API;
using NodeMarkup.Manager;

namespace NodeMarkup.Utilities.API
{
    public struct EntrancePointDataProvider : IEntrancePointData
    {
        private MarkupEnterPoint Point { get; }

        public byte Index => Point.Index;
        public ushort EntranceId => Point.Enter.Id;
        public ushort MarkingId => Point.Markup.Id;

        public IPointSourceData Source => new PointSourceDataProvider(Point.Source);
        public float Offset
        {
            get => Point.Offset;
            set => Point.Offset.Value = value;
        }

        public EntrancePointDataProvider(MarkupEnterPoint point)
        {
            Point = point;
        }

        public override string ToString() => Point.ToString();
    }
    public struct NormalPointDataProvider : INormalPointData
    {
        private MarkupNormalPoint Point { get; }

        public byte Index => Point.Index;
        public ushort EntranceId => Point.Enter.Id;
        public ushort MarkingId => Point.Markup.Id;
        public IEntrancePointData SourcePoint => new EntrancePointDataProvider(Point.SourcePoint);

        public NormalPointDataProvider(MarkupNormalPoint point)
        {
            Point = point;
        }

        public override string ToString() => Point.ToString();
    }
    public struct CrosswalkPointDataProvider : ICrosswalkPointData
    {
        private MarkupCrosswalkPoint Point { get; }

        public byte Index => Point.Index;
        public ushort EntranceId => Point.Enter.Id;
        public ushort MarkingId => Point.Markup.Id;
        public IEntrancePointData SourcePoint => new EntrancePointDataProvider(Point.SourcePoint);

        public CrosswalkPointDataProvider(MarkupCrosswalkPoint point)
        {
            Point = point;
        }

        public override string ToString() => Point.ToString();
    }
    public struct LanePointDataProvider : ILanePointData
    {
        private MarkupLanePoint Point { get; }
        public byte Index => Point.Index;
        public ushort EntranceId => Point.Enter.Id;
        public ushort MarkingId => Point.Markup.Id;
        public IEntrancePointData SourcePointA => new EntrancePointDataProvider(Point.SourcePointA);
        public IEntrancePointData SourcePointB => new EntrancePointDataProvider(Point.SourcePointB);

        public LanePointDataProvider(MarkupLanePoint point)
        {
            Point = point;
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
                MarkupPoint.LocationType.LeftEdge => PointLocation.Left,
                MarkupPoint.LocationType.RightEdge => PointLocation.Rigth,
                MarkupPoint.LocationType.Between => PointLocation.Between,
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
