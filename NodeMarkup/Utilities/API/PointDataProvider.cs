using NodeMarkup.API;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IPointSource = NodeMarkup.API.IPointSource;

namespace NodeMarkup.Utilities.API
{
    public struct EntrancePointDataProvider : IEntrancePointData
    {
        private MarkupEnterPoint Point { get; }

        public byte Index => Point.Index;
        public ushort EntranceId => Point.Enter.Id;
        public ushort MarkingId => Point.Markup.Id;
        public IPointSource Source => throw new NotImplementedException();

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
        public IPointSource Source => throw new NotImplementedException();

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
        public IPointSource Source => throw new NotImplementedException();

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
        public IPointSource Source => throw new NotImplementedException();

        public LanePointDataProvider(MarkupLanePoint point)
        {
            Point = point;
        }

        public override string ToString() => Point.ToString();
    }
}
