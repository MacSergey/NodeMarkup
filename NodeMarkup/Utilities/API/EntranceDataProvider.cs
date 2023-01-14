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

        public bool GetEntrancePoint(byte index, out IEntrancePointData pointData)
        {
            if(Enter.TryGetPoint(index, MarkupPoint.PointType.Enter, out var point))
            {
                pointData = new EntrancePointDataProvider(point as MarkupEnterPoint);
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
            if (Enter.TryGetPoint(index, MarkupPoint.PointType.Normal, out var point))
            {
                pointData = new NormalPointDataProvider(point as MarkupNormalPoint);
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
            if (Enter.TryGetPoint(index, MarkupPoint.PointType.Crosswalk, out var point))
            {
                pointData = new CrosswalkPointDataProvider(point as MarkupCrosswalkPoint);
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
            if (Enter.TryGetPoint(index, MarkupPoint.PointType.Lane, out var point))
            {
                pointData = new LanePointDataProvider(point as MarkupLanePoint);
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

        public bool GetEntrancePoint(byte index, out IEntrancePointData pointData)
        {
            if (Enter.TryGetPoint(index, MarkupPoint.PointType.Enter, out var point))
            {
                pointData = new EntrancePointDataProvider(point as MarkupEnterPoint);
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
            if (Enter.TryGetPoint(index, MarkupPoint.PointType.Lane, out var point))
            {
                pointData = new LanePointDataProvider(point as MarkupLanePoint);
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
