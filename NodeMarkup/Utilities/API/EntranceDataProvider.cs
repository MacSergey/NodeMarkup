using NodeMarkup.API;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.Utilities.API
{
    public struct SegmentEntranceDataProvider : ISegmentEntranceData
    {
        public SegmentEnter Enter { get; }
        public ushort Id => Enter.Id;
        public int PointCount => Enter.PointCount;

        public IEnumerable<IPointData> Points
        {
            get
            {
                foreach (var point in Enter.EnterPoints)
                {
                    yield return new EntrancePointDataProvider(point);
                }
            }
        }
        public IEnumerable<IEntrancePointData> EntrancePoints
        {
            get
            {
                foreach (var point in Enter.EnterPoints)
                {
                    yield return new EntrancePointDataProvider(point);
                }
            }
        }

        public IEnumerable<INormalPointData> NormalPoints
        {
            get
            {
                foreach (var point in Enter.NormalPoints)
                {
                    yield return new NormalPointDataProvider(point);
                }
            }
        }

        public IEnumerable<ICrosswalkPointData> CrosswalkPoints
        {
            get
            {
                foreach (var point in Enter.CrosswalkPoints)
                {
                    yield return new CrosswalkPointDataProvider(point);
                }
            }
        }

        public IEnumerable<ILanePointData> LanePoints
        {
            get
            {
                foreach (var point in Enter.LanePoints)
                {
                    yield return new LanePointDataProvider(point);
                }
            }
        }

        public SegmentEntranceDataProvider(SegmentEnter enter)
        {
            Enter = enter;
        }
    }
    public struct NodeEntranceDataProvider : INodeEntranceData
    {
        public NodeEnter Enter { get; }
        public ushort Id => Enter.Id;
        public int PointCount => Enter.PointCount;

        public IEnumerable<IPointData> Points
        {
            get
            {
                foreach (var point in Enter.EnterPoints)
                {
                    yield return new EntrancePointDataProvider(point);
                }
            }
        }
        public IEnumerable<IEntrancePointData> EntrancePoints
        {
            get
            {
                foreach (var point in Enter.EnterPoints)
                {
                    yield return new EntrancePointDataProvider(point);
                }
            }
        }

        public IEnumerable<ILanePointData> LanePoints
        {
            get
            {
                foreach (var point in Enter.LanePoints)
                {
                    yield return new LanePointDataProvider(point);
                }
            }
        }

        public NodeEntranceDataProvider(NodeEnter enter)
        {
            Enter = enter;
        }
    }
}
