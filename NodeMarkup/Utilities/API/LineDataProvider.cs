using NodeMarkup.API;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace NodeMarkup.Utilities.API
{
    public struct RegularLineDataProvider : IRegularLineData
    {
        private MarkupRegularLine Line { get; }
        public ulong Id => Line.Id;

        public IEntrancePointData StartPoint => new EntrancePointDataProvider(Line.Start as MarkupEnterPoint);
        public IEntrancePointData EndPoint => new EntrancePointDataProvider(Line.End as MarkupEnterPoint);

        public RegularLineDataProvider(MarkupRegularLine line)
        {
            Line = line;
        }

        public override string ToString() => Line.ToString();
    }
    public struct StopLineDataProvider : IStopLineData
    {
        private MarkupStopLine Line { get; }
        public ulong Id => Line.Id;

        public IEntrancePointData StartPoint => new EntrancePointDataProvider(Line.Start as MarkupEnterPoint);
        public IEntrancePointData EndPoint => new EntrancePointDataProvider(Line.End as MarkupEnterPoint);

        public StopLineDataProvider(MarkupStopLine line)
        {
            Line = line;
        }

        public override string ToString() => Line.ToString();
    }
    public struct NormalLineDataProvider : INormalLineData
    {
        private MarkupNormalLine Line { get; }
        public ulong Id => Line.Id;

        public IEntrancePointData StartPoint => new EntrancePointDataProvider((Line.Start is MarkupEnterPoint ? Line.Start : Line.End) as MarkupEnterPoint);
        public INormalPointData EndPoint => new NormalPointDataProvider((Line.Start is MarkupNormalPoint ? Line.Start : Line.End) as MarkupNormalPoint);

        public NormalLineDataProvider(MarkupNormalLine line)
        {
            Line = line;
        }

        public override string ToString() => Line.ToString();
    }
    public struct LaneLineDataProvider : ILaneLineData
    {
        private MarkupLaneLine Line { get; }
        public ulong Id => Line.Id;

        public ILanePointData StartPoint => new LanePointDataProvider(Line.Start as MarkupLanePoint);
        public ILanePointData EndPoint => new LanePointDataProvider(Line.End as MarkupLanePoint);

        public LaneLineDataProvider(MarkupLaneLine line)
        {
            Line = line;
        }

        public override string ToString() => Line.ToString();
    }
    public struct CrosswalkLineDataProvider : ICrosswalkLineData
    {
        private MarkupCrosswalkLine Line { get; }
        public ulong Id => Line.Id;

        public ICrosswalkData Crosswalk => new CrosswalkDataProvider(Line.Crosswalk);

        public ICrosswalkPointData StartPoint => new CrosswalkPointDataProvider(Line.Start as MarkupCrosswalkPoint);
        public ICrosswalkPointData EndPoint => new CrosswalkPointDataProvider(Line.End as MarkupCrosswalkPoint);

        public CrosswalkLineDataProvider(MarkupCrosswalkLine line)
        {
            Line = line;
        }

        public override string ToString() => Line.ToString();
    }
}
