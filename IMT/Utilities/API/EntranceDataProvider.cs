using NodeMarkup.API;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;

namespace NodeMarkup.Utilities.API
{
    public struct SegmentEntranceDataProvider : ISegmentEntranceData
    {
        public SegmentEntrance Enter { get; }
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

        public SegmentEntranceDataProvider(SegmentEntrance enter)
        {
            Enter = enter;
        }

        public bool GetEntrancePoint(byte index, out IEntrancePointData pointData)
        {
            if(Enter.TryGetPoint(index, MarkingPoint.PointType.Enter, out var point))
            {
                pointData = new EntrancePointDataProvider(point as MarkingEnterPoint);
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
            if (Enter.TryGetPoint(index, MarkingPoint.PointType.Normal, out var point))
            {
                pointData = new NormalPointDataProvider(point as MarkingNormalPoint);
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
            if (Enter.TryGetPoint(index, MarkingPoint.PointType.Crosswalk, out var point))
            {
                pointData = new CrosswalkPointDataProvider(point as MarkingCrosswalkPoint);
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
            if (Enter.TryGetPoint(index, MarkingPoint.PointType.Lane, out var point))
            {
                pointData = new LanePointDataProvider(point as MarkingLanePoint);
                return true;
            }
            else
            {
                pointData = null;
                return false;
            }
        }

        public override string ToString() => Enter.ToString();
    }
    public struct NodeEntranceDataProvider : INodeEntranceData
    {
        public NodeEntrance Enter { get; }
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

        public NodeEntranceDataProvider(NodeEntrance enter)
        {
            Enter = enter;
        }

        public bool GetEntrancePoint(byte index, out IEntrancePointData pointData)
        {
            if (Enter.TryGetPoint(index, MarkingPoint.PointType.Enter, out var point))
            {
                pointData = new EntrancePointDataProvider(point as MarkingEnterPoint);
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
            if (Enter.TryGetPoint(index, MarkingPoint.PointType.Lane, out var point))
            {
                pointData = new LanePointDataProvider(point as MarkingLanePoint);
                return true;
            }
            else
            {
                pointData = null;
                return false;
            }
        }

        public override string ToString() => Enter.ToString();
    }
}
