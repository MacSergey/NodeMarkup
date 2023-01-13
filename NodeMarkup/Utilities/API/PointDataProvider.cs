using NodeMarkup.API;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.Utilities.API
{
    public struct EntrancePointDataProvider : IEntrancePointData
    {
        private MarkupEnterPoint Point { get; }

        public byte Index => Point.Index;
        public ushort EntranceId => Point.Enter.Id;
        public ushort MarkingId => Point.Markup.Id;

        public EntrancePointDataProvider(MarkupEnterPoint point)
        {
            Point = point;
        }
    }
    public struct NormalPointDataProvider : INormalPointData
    {
        private MarkupNormalPoint Point { get; }

        public byte Index => Point.Index;
        public ushort EntranceId => Point.Enter.Id;
        public ushort MarkingId => Point.Markup.Id;

        public NormalPointDataProvider(MarkupNormalPoint point)
        {
            Point = point;
        }
    }
    public struct CrosswalkPointDataProvider : ICrosswalkPointData
    {
        private MarkupCrosswalkPoint Point { get; }

        public byte Index => Point.Index;
        public ushort EntranceId => Point.Enter.Id;
        public ushort MarkingId => Point.Markup.Id;

        public CrosswalkPointDataProvider(MarkupCrosswalkPoint point)
        {
            Point = point;
        }
    }
    public struct LanePointDataProvider : ILanePointData
    {
        private MarkupLanePoint Point { get; }
        public byte Index => Point.Index;
        public ushort EntranceId => Point.Enter.Id;
        public ushort MarkingId => Point.Markup.Id;

        public LanePointDataProvider(MarkupLanePoint point)
        {
            Point = point;
        }
    }
}
