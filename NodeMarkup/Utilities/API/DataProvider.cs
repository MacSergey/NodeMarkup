using ModsCommon;
using NodeMarkup.API;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using IStyleData = NodeMarkup.API.IStyleData;

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

            if (same)
            {
                if (startPointData.EntranceId != endPointData.EntranceId)
                    throw new CreateLineException(startPointData, endPointData, "Start point and end point must be from the same entrance");
            }
            else
            {
                if (startPointData.EntranceId == endPointData.EntranceId)
                    throw new CreateLineException(startPointData, endPointData, "Start point and end point must be from different entrances");
            }
        }
        internal static MarkupPoint GetEntrancePoint(Markup markup, IEntrancePointData pointData)
        {
            if (!markup.TryGetEnter(pointData.EntranceId, out var enter))
                throw new EntranceNotExist(pointData.EntranceId, markup.Id);

            if (!enter.TryGetPoint(pointData.Index, MarkupPoint.PointType.Enter, out var point))
                throw new PointNotExist(pointData.Index, enter.Id);

            return point;
        }
        internal static MarkupPoint GetNormalPoint(Markup markup, INormalPointData pointData)
        {
            if (!markup.TryGetEnter(pointData.EntranceId, out var enter))
                throw new EntranceNotExist(pointData.EntranceId, markup.Id);

            if (!enter.TryGetPoint(pointData.Index, MarkupPoint.PointType.Normal, out var point))
                throw new PointNotExist(pointData.Index, enter.Id);

            return point;
        }
        internal static MarkupPoint GetCrosswalkPoint(Markup markup, ICrosswalkPointData pointData)
        {
            if (!markup.TryGetEnter(pointData.EntranceId, out var enter))
                throw new EntranceNotExist(pointData.EntranceId, markup.Id);

            if (!enter.TryGetPoint(pointData.Index, MarkupPoint.PointType.Crosswalk, out var point))
                throw new PointNotExist(pointData.Index, enter.Id);

            return point;
        }
        internal static MarkupPoint GetLanePoint(Markup markup, ILanePointData pointData)
        {
            if (!markup.TryGetEnter(pointData.EntranceId, out var enter))
                throw new EntranceNotExist(pointData.EntranceId, markup.Id);

            if (!enter.TryGetPoint(pointData.Index, MarkupPoint.PointType.Lane, out var point))
                throw new PointNotExist(pointData.Index, enter.Id);

            return point;
        }
        internal static FillerContour GetFillerContour(Markup markup, IEnumerable<IEntrancePointData> pointDatas)
        {
            if (pointDatas == null)
                throw new ArgumentNullException(nameof(pointDatas));

            var vertices = new List<IFillerVertex>();
            foreach (var pointData in pointDatas)
            {
                if (pointData.MarkingId != markup.Id)
                    throw new MarkingIdNotMatchException(markup.Id, pointData.MarkingId);

                var point = GetEntrancePoint(markup, pointData);
                vertices.Add(new EnterFillerVertex(point));
            }

            var contour = new FillerContour(markup, vertices);
            if (!contour.IsComplite)
                throw new CreateFillerException("Filler contour is not complited");

            return contour;
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

        public bool GetEntrance(ushort id, out ISegmentEntranceData entrance)
        {
            if (Markup.TryGetEnter(id, out var enter))
            {
                entrance = new SegmentEntranceDataProvider(enter);
                return true;
            }
            else
            {
                entrance = null;
                return false;
            }
        }

        public bool AddRegularLine(IEntrancePointData startPointData, IEntrancePointData endPointData, IStyleData styleData, out IRegularLineData lineData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, false);
            var startPoint = DataProvider.GetEntrancePoint(Markup, startPointData);
            var endPoint = DataProvider.GetEntrancePoint(Markup, endPointData);

            var style = SingletonManager<StyleTemplateManager>.Instance.GetDefault<RegularLineStyle>(Style.StyleType.LineSolid);
            var line = Markup.AddRegularLine(new MarkupPointPair(startPoint, endPoint), style);
            lineData = new RegularLineDataProvider(line);
            return true;
        }

        public bool AddStopLine(IEntrancePointData startPointData, IEntrancePointData endPointData, IStyleData styleData, out IStopLineData lineData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, true);
            var startPoint = DataProvider.GetEntrancePoint(Markup, startPointData);
            var endPoint = DataProvider.GetEntrancePoint(Markup, endPointData);

            var style = SingletonManager<StyleTemplateManager>.Instance.GetDefault<StopLineStyle>(Style.StyleType.StopLineSolid);
            var line = Markup.AddStopLine(new MarkupPointPair(startPoint, endPoint), style);
            lineData = new StopLineDataProvider(line);
            return true;
        }

        public bool AddNormalLine(IEntrancePointData startPointData, INormalPointData endPointData, IStyleData styleData, out INormalLineData lineData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, true);
            if (startPointData.Index != endPointData.Index)
                throw new CreateLineException(startPointData, endPointData, "Start and end of normal line must have the same index");
            var startPoint = DataProvider.GetEntrancePoint(Markup, startPointData);
            var endPoint = DataProvider.GetNormalPoint(Markup, endPointData);

            var style = SingletonManager<StyleTemplateManager>.Instance.GetDefault<RegularLineStyle>(Style.StyleType.LineSolid);
            var line = Markup.AddNormalLine(new MarkupPointPair(startPoint, endPoint), style);
            lineData = new NormalLineDataProvider(line);
            return true;
        }

        public bool AddLaneLine(ILanePointData startPointData, ILanePointData endPointData, IStyleData styleData, out ILaneLineData lineData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, false);
            var startPoint = DataProvider.GetLanePoint(Markup, startPointData);
            var endPoint = DataProvider.GetLanePoint(Markup, endPointData);

            var style = SingletonManager<StyleTemplateManager>.Instance.GetDefault<RegularLineStyle>(Style.StyleType.LineSolid);
            var line = Markup.AddLaneLine(new MarkupPointPair(startPoint, endPoint), style);
            lineData = new LaneLineDataProvider(line);
            return true;
        }

        public bool AddCrosswalk(ICrosswalkPointData startPointData, ICrosswalkPointData endPointData, IStyleData styleData, out ICrosswalkData crosswalkData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, true);
            var startPoint = DataProvider.GetCrosswalkPoint(Markup, startPointData);
            var endPoint = DataProvider.GetCrosswalkPoint(Markup, endPointData);

            var style = SingletonManager<StyleTemplateManager>.Instance.GetDefault<CrosswalkStyle>(Style.StyleType.CrosswalkZebra);
            var line = Markup.AddCrosswalkLine(new MarkupPointPair(startPoint, endPoint), style);
            crosswalkData = new CrosswalkDataProvider(line.Crosswalk);
            return true;
        }

        public bool AddFiller(IEnumerable<IEntrancePointData> pointDatas, out IFillerData fillerData)
        {
            var contour = DataProvider.GetFillerContour(Markup, pointDatas);
            var style = SingletonManager<StyleTemplateManager>.Instance.GetDefault<FillerStyle>(Style.StyleType.FillerStripe);

            var filler = Markup.AddFiller(contour, style, out var lines);
            fillerData = new FillerDataProvider(filler);
            return true;
        }

        public override string ToString() => Markup.ToString();
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

        public bool GetEntrance(ushort id, out INodeEntranceData entrance)
        {
            if (Markup.TryGetEnter(id, out var enter))
            {
                entrance = new NodeEntranceDataProvider(enter);
                return true;
            }
            else
            {
                entrance = null;
                return false;
            }
        }

        public bool AddRegularLine(IEntrancePointData startPointData, IEntrancePointData endPointData, IStyleData styleData, out IRegularLineData lineData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, true);
            var startPoint = DataProvider.GetEntrancePoint(Markup, startPointData);
            var endPoint = DataProvider.GetEntrancePoint(Markup, endPointData);

            var style = SingletonManager<StyleTemplateManager>.Instance.GetDefault<RegularLineStyle>(Style.StyleType.LineSolid);
            var line = Markup.AddRegularLine(new MarkupPointPair(startPoint, endPoint), style);
            lineData = new RegularLineDataProvider(line);
            return true;
        }

        public bool AddLaneLine(ILanePointData startPointData, ILanePointData endPointData, IStyleData styleData, out ILaneLineData lineData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, false);
            var startPoint = DataProvider.GetLanePoint(Markup, startPointData);
            var endPoint = DataProvider.GetLanePoint(Markup, endPointData);

            var style = SingletonManager<StyleTemplateManager>.Instance.GetDefault<RegularLineStyle>(Style.StyleType.LineSolid);
            var line = Markup.AddLaneLine(new MarkupPointPair(startPoint, endPoint), style);
            lineData = new LaneLineDataProvider(line);
            return true;
        }
        public bool AddFiller(IEnumerable<IEntrancePointData> pointDatas, out IFillerData fillerData)
        {
            var contour = DataProvider.GetFillerContour(Markup, pointDatas);
            var style = SingletonManager<StyleTemplateManager>.Instance.GetDefault<FillerStyle>(Style.StyleType.FillerStripe);

            var filler = Markup.AddFiller(contour, style, out var lines);
            fillerData = new FillerDataProvider(filler);
            return true;
        }

        public override string ToString() => Markup.ToString();
    }
}
