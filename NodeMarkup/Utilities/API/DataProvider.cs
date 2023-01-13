using ModsCommon;
using NodeMarkup.API;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;

namespace NodeMarkup.Utilities.API
{
    public class DataProvider : IDataProviderV1
    {
        public Version ModVersion => SingletonMod<Mod>.Instance.Version;
        public bool IsBeta => SingletonMod<Mod>.Instance.IsBeta;

        public DataProvider()
        {

        }

        public bool GetNodeMarking(ushort id, out INodeMarkingData nodeMarkingData)
        {
            var nodeMarking = SingletonManager<NodeMarkupManager>.Instance.GetOrCreateMarkup(id);
            nodeMarkingData = new NodeDataProvider(nodeMarking);
            return true;
        }

        public bool GetSegmentMarking(ushort id, out ISegmentMarkingData segmentMarkingData)
        {
            var segmentMarking = SingletonManager<SegmentMarkupManager>.Instance.GetOrCreateMarkup(id);
            segmentMarkingData = new SegmentDataProvider(segmentMarking);
            return true;
        }

        public bool NodeMarkingExist(ushort id) => SingletonManager<NodeMarkupManager>.Instance.Exist(id);
        public bool SegmentMarkingExist(ushort id) => SingletonManager<SegmentMarkupManager>.Instance.Exist(id);

        internal static void CheckPoints(ushort markingId, IPointData startPointData, IPointData endPointData, bool same)
        {
            if (startPointData == null)
                throw new ArgumentNullException(nameof(startPointData));

            if (endPointData == null)
                throw new ArgumentNullException(nameof(endPointData));

            if (startPointData.MarkingId != markingId)
                throw new MarkingIdNotMatchException(markingId, startPointData.MarkingId);

            if (endPointData.MarkingId != markingId)
                throw new MarkingIdNotMatchException(markingId, endPointData.MarkingId);

            if(same)
            {
                if (startPointData.EntranceId == endPointData.EntranceId)
                    throw new CreateLineException(startPointData, endPointData, "Start point and end point must be from different enterances");
            }
            else
            {
                if (startPointData.EntranceId != endPointData.EntranceId)
                    throw new CreateLineException(startPointData, endPointData, "Start point and end point must be from the same enterance");
            }
        }
        internal static void GetPoints(Markup markup, IPointData startPointData, IPointData endPointData, out MarkupPoint startPoint, out MarkupPoint endPoint)
        {
            if (!markup.TryGetEnter(startPointData.EntranceId, out var startEnter))
                throw new EnteranceNotExist(startPointData.EntranceId, markup.Id);

            if (!markup.TryGetEnter(endPointData.EntranceId, out var endEnter))
                throw new EnteranceNotExist(endPointData.EntranceId, markup.Id);

            if (!startEnter.TryGetPoint(startPointData.Index, MarkupPoint.PointType.Enter, out startPoint))
                throw new PointNotExist(startPointData.Index, startEnter.Id);

            if (!endEnter.TryGetPoint(endPointData.Index, MarkupPoint.PointType.Enter, out endPoint))
                throw new PointNotExist(endPointData.Index, endEnter.Id);
        }
    }
    public struct NodeDataProvider : INodeMarkingData
    {
        private Manager.NodeMarkup Markup { get; }
        public ushort Id => Markup.Id;
        public int EntranceCount => Markup.EntersCount;

        public IEnumerable<ISegmentEntranceData> Entrances
        {
            get
            {
                foreach (var enter in Markup.Enters)
                {
                    yield return new SegmentEntranceDataProvider(enter);
                }
            }
        }

        public NodeDataProvider(Manager.NodeMarkup markup)
        {
            Markup = markup;
        }


        public bool AddRegularLine(IEntrancePointData startPointData, IEntrancePointData endPointData, out IRegularLineData lineData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, true);
            DataProvider.GetPoints(Markup, startPointData, endPointData, out var startPoint, out var endPoint);

            var line = Markup.AddRegularLine(new MarkupPointPair(startPoint, endPoint), null);
            lineData = new RegularLineDataProvider(line);
            return true;
        }

        public bool AddStopLine(IEntrancePointData startPointData, IEntrancePointData endPointData, out IStopLineData lineData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, false);
            DataProvider.GetPoints(Markup, startPointData, endPointData, out var startPoint, out var endPoint);

            var line = Markup.AddStopLine(new MarkupPointPair(startPoint, endPoint), null);
            lineData = new StopLineDataProvider(line);
            return true;
        }

        public bool AddNormalLine(IEntrancePointData startPointData, INormalPointData endPointData, out INormalLineData lineData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, true);
            DataProvider.GetPoints(Markup, startPointData, endPointData, out var startPoint, out var endPoint);

            var line = Markup.AddNormalLine(new MarkupPointPair(startPoint, endPoint), null);
            lineData = new NormalLineDataProvider(line);
            return true;
        }

        public bool AddLaneLine(ILanePointData startPointData, ILanePointData endPointData, out ILaneLineData lineData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, false);
            DataProvider.GetPoints(Markup, startPointData, endPointData, out var startPoint, out var endPoint);

            var line = Markup.AddLaneLine(new MarkupPointPair(startPoint, endPoint), null);
            lineData = new LaneLineDataProvider(line);
            return true;
        }

        public bool AddCrosswalk(ICrosswalkPointData startPoint, ICrosswalkPointData endPoint, out ICrosswalkData crosswalk)
        {
            throw new NotImplementedException();
        }
        public bool AddCrosswalk(IPointData startPoint, IPointData endPoint, out ICrosswalkData crosswalk)
        {
            throw new NotImplementedException();
        }
    }
    public struct SegmentDataProvider : ISegmentMarkingData
    {
        private SegmentMarkup Markup { get; }
        public ushort Id => Markup.Id;
        public int EntranceCount => Markup.EntersCount;
        public IEnumerable<INodeEntranceData> Entrances
        {
            get
            {
                foreach (var enter in Markup.Enters)
                {
                    yield return new NodeEntranceDataProvider(enter);
                }
            }
        }

        public SegmentDataProvider(SegmentMarkup markup)
        {
            Markup = markup;
        }

        public bool AddRegularLine(IEntrancePointData startPointData, IEntrancePointData endPointData, out IRegularLineData lineData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, true);
            DataProvider.GetPoints(Markup, startPointData, endPointData, out var startPoint, out var endPoint);

            var line = Markup.AddRegularLine(new MarkupPointPair(startPoint, endPoint), null);
            lineData = new RegularLineDataProvider(line);
            return true;
        }

        public bool AddLaneLine(ILanePointData startPointData, ILanePointData endPointData, out ILaneLineData lineData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, false);
            DataProvider.GetPoints(Markup, startPointData, endPointData, out var startPoint, out var endPoint);

            var line = Markup.AddLaneLine(new MarkupPointPair(startPoint, endPoint), null);
            lineData = new LaneLineDataProvider(line);
            return true;
        }
    }
}
