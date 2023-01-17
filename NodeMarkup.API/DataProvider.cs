using ModsCommon;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using static NodeMarkup.Manager.CrosswalkStyle;
using static NodeMarkup.Manager.FillerStyle;
using static NodeMarkup.Manager.RegularLineStyle;
using static NodeMarkup.Manager.StopLineStyle;
using static NodeMarkup.Manager.Style;

namespace NodeMarkup.API
{
    public class DataProviderFactory : IDataProviderFactory
    {
        public IDataProviderV1 GetProvider(string id) => new DataProvider(id);
    }

    public class DataProvider : IDataProviderV1
    {
        public string Id { get; }
        public Version ModVersion => SingletonMod<Mod>.Instance.Version;
        public bool IsBeta => SingletonMod<Mod>.Instance.IsBeta;

        public IEnumerable<string> RegularLineStyles => GetStyles<RegularLineType>(LineType.Regular);
        public IEnumerable<string> NormalLineStyles => GetStyles<RegularLineType>(LineType.Regular);
        public IEnumerable<string> StopLineStyles => GetStyles<StopLineType>(LineType.Stop);
        public IEnumerable<string> LaneLineStyles => GetStyles<RegularLineType>(LineType.Lane);
        public IEnumerable<string> CrosswalkStyles
        {
            get
            {
                foreach (var type in EnumExtension.GetEnumValues<CrosswalkType>())
                {
                    if (type.IsVisible())
                        yield return type.ToString();
                }
            }
        }
        public IEnumerable<string> FillerStyles
        {
            get
            {
                foreach (var type in EnumExtension.GetEnumValues<FillerType>())
                {
                    if (type.IsVisible())
                        yield return type.ToString();
                }
            }
        }

        private IEnumerable<string> GetStyles<StyleType>(LineType lineType)
            where StyleType : Enum
        {
            foreach (var type in EnumExtension.GetEnumValues<StyleType>())
            {
                if (type.IsVisible() && (type.GetLineType() & lineType) != 0)
                    yield return type.ToString();
            }
        }

        public DataProvider(string id)
        {
            Id = id;
            Log("Created");
        }

        public bool GetNodeMarking(ushort id, out INodeMarkingData nodeMarkingData)
        {
            var nodeMarking = SingletonManager<NodeMarkupManager>.Instance.GetOrCreateMarkup(id);
            nodeMarkingData = new NodeDataProvider(this, nodeMarking);
            return true;
        }

        public bool GetSegmentMarking(ushort id, out ISegmentMarkingData segmentMarkingData)
        {
            var segmentMarking = SingletonManager<SegmentMarkupManager>.Instance.GetOrCreateMarkup(id);
            segmentMarkingData = new SegmentDataProvider(this, segmentMarking);
            return true;
        }

        public bool NodeMarkingExist(ushort id) => SingletonManager<NodeMarkupManager>.Instance.Exist(id);
        public bool SegmentMarkingExist(ushort id) => SingletonManager<SegmentMarkupManager>.Instance.Exist(id);

        public IRegularLineStyleData GetRegularLineStyle(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            var type = GetStyleType<RegularLineType>(name);

            if (!type.IsVisible() || (type.GetLineType() & LineType.Regular) == 0)
                throw new IntersectionMarkingToolException($"No style with name {name}");

            var style = GetDefault(type);
            var styleData = new StyleDataProvider(style, name);
            return styleData;
        }

        public INormalLineStyleData GetNormalLineStyle(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            var type = GetStyleType<RegularLineType>(name);

            if (!type.IsVisible() || (type.GetLineType() & LineType.Regular) == 0)
                throw new IntersectionMarkingToolException($"No style with name {name}");

            var style = GetDefault(type);
            var styleData = new StyleDataProvider(style, name);
            return styleData;
        }

        public IStopLineStyleData GetStopLineStyle(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            var type = GetStyleType<StopLineType>(name);

            if (!type.IsVisible())
                throw new IntersectionMarkingToolException($"No style with name {name}");

            var style = GetDefault(type);
            var styleData = new StyleDataProvider(style, name);
            return styleData;
        }

        public ILaneLineStyleData GetLaneLineStyle(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            var type = GetStyleType<RegularLineType>(name);

            if (!type.IsVisible() || (type.GetLineType() & LineType.Lane) == 0)
                throw new IntersectionMarkingToolException($"No style with name {name}");

            var style = GetDefault(type);
            var styleData = new StyleDataProvider(style, name);
            return styleData;
        }

        public ICrosswalkStyleData GetCrosswalkStyle(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            var type = GetStyleType<CrosswalkType>(name);

            if (!type.IsVisible())
                throw new IntersectionMarkingToolException($"No style with name {name}");

            var style = GetDefault(type);
            var styleData = new StyleDataProvider(style, name);
            return styleData;
        }

        public IFillerStyleData GetFillerStyle(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            var type = GetStyleType<FillerType>(name);

            if (!type.IsVisible())
                throw new IntersectionMarkingToolException($"No style with name {name}");

            var style = GetDefault(type);
            var styleData = new StyleDataProvider(style, name);
            return styleData;
        }

        internal static void CheckPoints(ushort markingId, IPointData startPointData, IPointData endPointData, bool? same)
        {
            if (startPointData == null)
                throw new ArgumentNullException(nameof(startPointData));

            if (endPointData == null)
                throw new ArgumentNullException(nameof(endPointData));

            if (startPointData.MarkingId != markingId)
                throw new MarkingIdNotMatchException(markingId, startPointData.MarkingId);

            if (endPointData.MarkingId != markingId)
                throw new MarkingIdNotMatchException(markingId, endPointData.MarkingId);

            if (same == true)
            {
                if (startPointData.EntranceId != endPointData.EntranceId)
                    throw new CreateLineException(startPointData, endPointData, "Start point and end point must be from the same entrance");
            }
            else if (same == false)
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
        internal static StyleType GetStyleType<StyleType>(string name)
            where StyleType : Enum
        {
            try { return (StyleType)Enum.Parse(typeof(StyleType), name); }
            catch { throw new IntersectionMarkingToolException($"No style with name {name}"); }
        }

        internal void Log(string message) => SingletonMod<Mod>.Logger.Debug($"[{Id} Provider] {message}");
    }
    public struct NodeDataProvider : INodeMarkingData
    {
        private DataProvider Provider { get; }
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

        public NodeDataProvider(DataProvider provider, Manager.NodeMarkup markup)
        {
            Provider = provider;
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

        public IRegularLineData AddRegularLine(IEntrancePointData startPointData, IEntrancePointData endPointData, IRegularLineStyleData styleData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, false);
            var startPoint = DataProvider.GetEntrancePoint(Markup, startPointData);
            var endPoint = DataProvider.GetEntrancePoint(Markup, endPointData);

            var pair = new MarkupPointPair(startPoint, endPoint);
            if (Markup.ExistLine(pair))
                throw new IntersectionMarkingToolException($"Line {pair} already exist");

            var type = DataProvider.GetStyleType<RegularLineType>(styleData.Name);
            var style = GetDefault(type);
            var line = Markup.AddRegularLine(pair, style);
            Provider.Log($"Line {line} added");
            var lineData = new RegularLineDataProvider(line);
            return lineData;
        }

        public IStopLineData AddStopLine(IEntrancePointData startPointData, IEntrancePointData endPointData, IStopLineStyleData styleData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, true);
            if (startPointData.Index == endPointData.Index)
                throw new CreateLineException(startPointData, endPointData, "Start and end of stop line must have differen index");
            var startPoint = DataProvider.GetEntrancePoint(Markup, startPointData);
            var endPoint = DataProvider.GetEntrancePoint(Markup, endPointData);

            var pair = new MarkupPointPair(startPoint, endPoint);
            if (Markup.ExistLine(pair))
                throw new IntersectionMarkingToolException($"Line {pair} already exist");

            var type = DataProvider.GetStyleType<StopLineType>(styleData.Name);
            var style = GetDefault(type);
            var line = Markup.AddStopLine(pair, style);
            Provider.Log($"Line {line} added");
            var lineData = new StopLineDataProvider(line);
            return lineData;
        }

        public INormalLineData AddNormalLine(IEntrancePointData startPointData, INormalPointData endPointData, INormalLineStyleData styleData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, true);
            if (startPointData.Index != endPointData.Index)
                throw new CreateLineException(startPointData, endPointData, "Start and end of normal line must have the same index");
            var startPoint = DataProvider.GetEntrancePoint(Markup, startPointData);
            var endPoint = DataProvider.GetNormalPoint(Markup, endPointData);

            var pair = new MarkupPointPair(startPoint, endPoint);
            if (Markup.ExistLine(pair))
                throw new IntersectionMarkingToolException($"Line {pair} already exist");

            var type = DataProvider.GetStyleType<RegularLineType>(styleData.Name);
            var style = GetDefault(type);
            var line = Markup.AddNormalLine(pair, style);
            Provider.Log($"Line {line} added");
            var lineData = new NormalLineDataProvider(line);
            return lineData;
        }

        public ILaneLineData AddLaneLine(ILanePointData startPointData, ILanePointData endPointData, ILaneLineStyleData styleData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, false);
            var startPoint = DataProvider.GetLanePoint(Markup, startPointData);
            var endPoint = DataProvider.GetLanePoint(Markup, endPointData);

            var pair = new MarkupPointPair(startPoint, endPoint);
            if (Markup.ExistLine(pair))
                throw new IntersectionMarkingToolException($"Line {pair} already exist");

            var type = DataProvider.GetStyleType<RegularLineType>(styleData.Name);
            var style = GetDefault(type);
            var line = Markup.AddLaneLine(pair, style);
            Provider.Log($"Line {line} added");
            var lineData = new LaneLineDataProvider(line);
            return lineData;
        }

        public ICrosswalkData AddCrosswalk(ICrosswalkPointData startPointData, ICrosswalkPointData endPointData, ICrosswalkStyleData styleData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, true);
            if (startPointData.Index == endPointData.Index)
                throw new CreateLineException(startPointData, endPointData, "Start and end of crosswalk must have differen index");
            var startPoint = DataProvider.GetCrosswalkPoint(Markup, startPointData);
            var endPoint = DataProvider.GetCrosswalkPoint(Markup, endPointData);

            var pair = new MarkupPointPair(startPoint, endPoint);
            if (Markup.ExistLine(pair))
                throw new IntersectionMarkingToolException($"Crosswalk {pair} already exist");

            var type = DataProvider.GetStyleType<CrosswalkType>(styleData.Name);
            var style = GetDefault(type);
            var line = Markup.AddCrosswalkLine(pair, style);
            Provider.Log($"Added crosswalk {line.Crosswalk}");
            var crosswalkData = new CrosswalkDataProvider(line.Crosswalk);
            return crosswalkData;
        }

        public IFillerData AddFiller(IEnumerable<IEntrancePointData> pointDatas, IFillerStyleData styleData)
        {
            var contour = DataProvider.GetFillerContour(Markup, pointDatas);
            var type = DataProvider.GetStyleType<FillerType>(styleData.Name);
            var style = GetDefault(type);

            var filler = Markup.AddFiller(contour, style, out var lines);
            Provider.Log($"Filler {filler} added");
            var fillerData = new FillerDataProvider(filler);
            return fillerData;
        }


        private bool RemoveLine(MarkupPoint startPoint, MarkupPoint endPoint)
        {
            if (Markup.TryGetLine(new MarkupPointPair(startPoint, endPoint), out var line))
            {
                Markup.RemoveLine(line);
                Provider.Log($"Line {line} removed");
                return true;
            }
            else
                return false;
        }
        public bool RemoveRegularLine(IEntrancePointData startPointData, IEntrancePointData endPointData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = DataProvider.GetEntrancePoint(Markup, startPointData);
            var endPoint = DataProvider.GetEntrancePoint(Markup, endPointData);
            return RemoveLine(startPoint, endPoint);
        }
        public bool RemoveNormalLine(IEntrancePointData startPointData, INormalPointData endPointData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = DataProvider.GetEntrancePoint(Markup, startPointData);
            var endPoint = DataProvider.GetNormalPoint(Markup, endPointData);
            return RemoveLine(startPoint, endPoint);
        }
        public bool RemoveStopLine(IEntrancePointData startPointData, IEntrancePointData endPointData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = DataProvider.GetEntrancePoint(Markup, startPointData);
            var endPoint = DataProvider.GetEntrancePoint(Markup, endPointData);
            return RemoveLine(startPoint, endPoint);
        }
        public bool RemoveLaneLine(ILanePointData startPointData, ILanePointData endPointData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = DataProvider.GetLanePoint(Markup, startPointData);
            var endPoint = DataProvider.GetLanePoint(Markup, endPointData);
            return RemoveLine(startPoint, endPoint);
        }
        public bool RemoveCrosswalk(ICrosswalkPointData startPointData, ICrosswalkPointData endPointData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = DataProvider.GetCrosswalkPoint(Markup, startPointData);
            var endPoint = DataProvider.GetCrosswalkPoint(Markup, endPointData);
            return RemoveLine(startPoint, endPoint);
        }
        public bool RemoveFiller(IFillerData fillerData)
        {
            if (fillerData.MarkingId != Markup.Id)
                throw new MarkingIdNotMatchException(Markup.Id, fillerData.MarkingId);

            if (Markup.TryGetFiller(fillerData.Id, out var filler))
            {
                Markup.RemoveFiller(filler);
                Provider.Log($"Filler {filler} removed");
                return true;
            }
            else
                return false;
        }

        public bool RegularLineExist(IEntrancePointData startPointData, IEntrancePointData endPointData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = DataProvider.GetEntrancePoint(Markup, startPointData);
            var endPoint = DataProvider.GetEntrancePoint(Markup, endPointData);
            return Markup.ExistLine(new MarkupPointPair(startPoint, endPoint));
        }
        public bool NormalLineExist(IEntrancePointData startPointData, INormalPointData endPointData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = DataProvider.GetEntrancePoint(Markup, startPointData);
            var endPoint = DataProvider.GetNormalPoint(Markup, endPointData);
            return Markup.ExistLine(new MarkupPointPair(startPoint, endPoint));
        }
        public bool StopLineExist(IEntrancePointData startPointData, IEntrancePointData endPointData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = DataProvider.GetEntrancePoint(Markup, startPointData);
            var endPoint = DataProvider.GetEntrancePoint(Markup, endPointData);
            return Markup.ExistLine(new MarkupPointPair(startPoint, endPoint));
        }
        public bool LaneLineExist(ILanePointData startPointData, ILanePointData endPointData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = DataProvider.GetLanePoint(Markup, startPointData);
            var endPoint = DataProvider.GetLanePoint(Markup, endPointData);
            return Markup.ExistLine(new MarkupPointPair(startPoint, endPoint));
        }
        public bool CrosswalkExist(ICrosswalkPointData startPointData, ICrosswalkPointData endPointData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = DataProvider.GetCrosswalkPoint(Markup, startPointData);
            var endPoint = DataProvider.GetCrosswalkPoint(Markup, endPointData);
            return Markup.ExistLine(new MarkupPointPair(startPoint, endPoint));
        }

        public override string ToString() => Markup.ToString();
    }
    public struct SegmentDataProvider : ISegmentMarkingData
    {
        private DataProvider Provider { get; }
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

        public SegmentDataProvider(DataProvider provider, SegmentMarkup markup)
        {
            Provider = provider;
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

        public IRegularLineData AddRegularLine(IEntrancePointData startPointData, IEntrancePointData endPointData, IRegularLineStyleData styleData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, true);
            var startPoint = DataProvider.GetEntrancePoint(Markup, startPointData);
            var endPoint = DataProvider.GetEntrancePoint(Markup, endPointData);

            var pair = new MarkupPointPair(startPoint, endPoint);
            if (Markup.ExistLine(pair))
                throw new IntersectionMarkingToolException($"Line {pair} already exist");

            var type = DataProvider.GetStyleType<RegularLineType>(styleData.Name);
            var style = GetDefault(type);
            var line = Markup.AddRegularLine(pair, style);
            Provider.Log($"Line {line} added");
            var lineData = new RegularLineDataProvider(line);
            return lineData;
        }

        public ILaneLineData AddLaneLine(ILanePointData startPointData, ILanePointData endPointData, ILaneLineStyleData styleData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, false);
            var startPoint = DataProvider.GetLanePoint(Markup, startPointData);
            var endPoint = DataProvider.GetLanePoint(Markup, endPointData);

            var pair = new MarkupPointPair(startPoint, endPoint);
            if (Markup.ExistLine(pair))
                throw new IntersectionMarkingToolException($"Line {pair} already exist");

            var type = DataProvider.GetStyleType<RegularLineType>(styleData.Name);
            var style = GetDefault(type);
            var line = Markup.AddLaneLine(pair, style);
            Provider.Log($"Line {line} added");
            var lineData = new LaneLineDataProvider(line);
            return lineData;
        }
        public IFillerData AddFiller(IEnumerable<IEntrancePointData> pointDatas, IFillerStyleData styleData)
        {
            var contour = DataProvider.GetFillerContour(Markup, pointDatas);
            var style = SingletonManager<StyleTemplateManager>.Instance.GetDefault<FillerStyle>(StyleType.FillerStripe);

            var filler = Markup.AddFiller(contour, style, out var lines);
            Provider.Log($"Filler {filler} added");
            var fillerData = new FillerDataProvider(filler);
            return fillerData;
        }

        private bool RemoveLine(MarkupPoint startPoint, MarkupPoint endPoint)
        {
            if (Markup.TryGetLine(new MarkupPointPair(startPoint, endPoint), out var line))
            {
                Markup.RemoveLine(line);
                Provider.Log($"Line {line} removed");
                return true;
            }
            else
                return false;
        }
        public bool RemoveRegularLine(IEntrancePointData startPointData, IEntrancePointData endPointData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = DataProvider.GetEntrancePoint(Markup, startPointData);
            var endPoint = DataProvider.GetEntrancePoint(Markup, endPointData);
            return RemoveLine(startPoint, endPoint);
        }
        public bool RemoveLaneLine(ILanePointData startPointData, ILanePointData endPointData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = DataProvider.GetLanePoint(Markup, startPointData);
            var endPoint = DataProvider.GetLanePoint(Markup, endPointData);
            return RemoveLine(startPoint, endPoint);
        }
        public bool RemoveFiller(IFillerData fillerData)
        {
            if (fillerData.MarkingId != Markup.Id)
                throw new MarkingIdNotMatchException(Markup.Id, fillerData.MarkingId);

            if (Markup.TryGetFiller(fillerData.Id, out var filler))
            {
                Markup.RemoveFiller(filler);
                Provider.Log($"Filler {filler} removed");
                return true;
            }
            else
                return false;
        }

        public bool RegularLineExist(IEntrancePointData startPointData, IEntrancePointData endPointData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = DataProvider.GetEntrancePoint(Markup, startPointData);
            var endPoint = DataProvider.GetEntrancePoint(Markup, endPointData);
            return Markup.ExistLine(new MarkupPointPair(startPoint, endPoint));
        }
        public bool LaneLineExist(ILanePointData startPointData, ILanePointData endPointData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = DataProvider.GetLanePoint(Markup, startPointData);
            var endPoint = DataProvider.GetLanePoint(Markup, endPointData);
            return Markup.ExistLine(new MarkupPointPair(startPoint, endPoint));
        }
        public bool CrosswalkExist(ICrosswalkPointData startPointData, ICrosswalkPointData endPointData)
        {
            DataProvider.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = DataProvider.GetCrosswalkPoint(Markup, startPointData);
            var endPoint = DataProvider.GetCrosswalkPoint(Markup, endPointData);
            return Markup.ExistLine(new MarkupPointPair(startPoint, endPoint));
        }

        public override string ToString() => Markup.ToString();
    }
}
