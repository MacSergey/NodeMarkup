using ModsCommon;
using NodeMarkup.API;
using NodeMarkup.Manager;
using System.Collections.Generic;
using static NodeMarkup.Manager.CrosswalkStyle;
using static NodeMarkup.Manager.FillerStyle;
using static NodeMarkup.Manager.RegularLineStyle;
using static NodeMarkup.Manager.StopLineStyle;
using System;

namespace NodeMarkup.Utilities.API
{
    public interface IMarkingDataProvider
    {
        void RemoveLine(MarkingLine line);
        void RemoveFiller(MarkingFiller filler);
    }

    public struct NodeMarkingDataProvider : INodeMarkingData, IMarkingDataProvider
    {
        private DataProvider DataProvider { get; }
        IDataProviderV1 IMarkingData.DataProvider => DataProvider;
        public NodeMarkup.API.MarkingType Type => NodeMarkup.API.MarkingType.Node;
        public ushort Id { get; }

        private NodeMarking Marking => APIHelper.GetNodeMarking(Id);
        public int EntranceCount => Marking.EntersCount;

        public IEnumerable<ISegmentEntranceData> Entrances
        {
            get
            {
                foreach (var enter in Marking.Enters)
                {
                    yield return new SegmentEntranceDataProvider(DataProvider, enter);
                }
            }
        }

        public NodeMarkingDataProvider(DataProvider dataProvider, NodeMarking marking)
        {
            DataProvider = dataProvider;
            Id = marking.Id;
        }

        public void ClearMarkings()
        {
            Marking.Clear();
            DataProvider.Log($"Clear Node #{Id} marking");
        }
        public void ResetPointOffsets()
        {
            Marking.ResetOffsets();
            DataProvider.Log($"Reset Node #{Id} point offsets");
        }

        #region GET

        public bool TryGetEntrance(ushort id, out ISegmentEntranceData entrance)
        {
            if (Marking.TryGetEnter(id, out var enter))
            {
                entrance = new SegmentEntranceDataProvider(DataProvider, enter);
                return true;
            }
            else
            {
                entrance = null;
                return false;
            }
        }

        public bool TryGetRegularLine(IEntrancePointData startPointData, IEntrancePointData endPointData, out IRegularLineData regularLineData)
        {
            APIHelper.CheckPoints(Id, startPointData, endPointData, true);
            var startPoint = APIHelper.GetEntrancePoint(Marking, startPointData);
            var endPoint = APIHelper.GetEntrancePoint(Marking, endPointData);
            if (Marking.TryGetLine<MarkingRegularLine>(startPoint, endPoint, out var line))
            {
                regularLineData = new RegularLineDataProvider(DataProvider, line);
                return true;
            }
            else
            {
                regularLineData = null;
                return false;
            }
        }
        public bool TryGetLaneLine(ILanePointData startPointData, ILanePointData endPointData, out ILaneLineData laneLineData)
        {
            APIHelper.CheckPoints(Id, startPointData, endPointData, false);
            var startPoint = APIHelper.GetLanePoint(Marking, startPointData);
            var endPoint = APIHelper.GetLanePoint(Marking, endPointData);
            if (Marking.TryGetLine<MarkingLaneLine>(startPoint, endPoint, out var line))
            {
                laneLineData = new LaneLineDataProvider(DataProvider, line);
                return true;
            }
            else
            {
                laneLineData = null;
                return false;
            }
        }
        public bool TryGetNormalLine(IEntrancePointData startPointData, INormalPointData endPointData, out INormalLineData normalLineData)
        {
            APIHelper.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = APIHelper.GetEntrancePoint(Marking, startPointData);
            var endPoint = APIHelper.GetNormalPoint(Marking, endPointData);
            if (Marking.TryGetLine<MarkingNormalLine>(startPoint, endPoint, out var line))
            {
                normalLineData = new NormalLineDataProvider(DataProvider, line);
                return true;
            }
            else
            {
                normalLineData = null;
                return false;
            }
        }
        public bool TryGetStopLine(IEntrancePointData startPointData, IEntrancePointData endPointData, out IStopLineData stopLineData)
        {
            APIHelper.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = APIHelper.GetEntrancePoint(Marking, startPointData);
            var endPoint = APIHelper.GetEntrancePoint(Marking, endPointData);
            if (Marking.TryGetLine<MarkingStopLine>(startPoint, endPoint, out var line))
            {
                stopLineData = new StopLineDataProvider(DataProvider, line);
                return true;
            }
            else
            {
                stopLineData = null;
                return false;
            }
        }
        public bool TryGetCrosswalk(ICrosswalkPointData startPointData, ICrosswalkPointData endPointData, out ICrosswalkData crosswalkData)
        {
            APIHelper.CheckPoints(Id, startPointData, endPointData, false);
            var startPoint = APIHelper.GetCrosswalkPoint(Marking, startPointData);
            var endPoint = APIHelper.GetCrosswalkPoint(Marking, endPointData);
            if (Marking.TryGetLine<MarkingCrosswalkLine>(startPoint, endPoint, out var line))
            {
                crosswalkData = new CrosswalkDataProvider(DataProvider, line.Crosswalk);
                return true;
            }
            else
            {
                crosswalkData = null;
                return false;
            }
        }

        #endregion

        #region ADD

        public IRegularLineData AddRegularLine(IEntrancePointData startPointData, IEntrancePointData endPointData, IRegularLineStyleData styleData = null)
        {
            APIHelper.CheckPoints(Id, startPointData, endPointData, false);
            var startPoint = APIHelper.GetEntrancePoint(Marking, startPointData);
            var endPoint = APIHelper.GetEntrancePoint(Marking, endPointData);

            var pair = new MarkingPointPair(startPoint, endPoint);
            if (Marking.ExistLine(pair))
                throw new IntersectionMarkingToolException($"Line {pair} already exist");

            MarkingRegularLine line;
            if (styleData is StyleDataProvider dataProvider && dataProvider.Style is RegularLineStyle regularStyle)
                line = Marking.AddRegularLine(pair, regularStyle.CopyLineStyle());
            else if (styleData == null)
                line = Marking.AddRegularLine(pair, null);
            else
                throw new IntersectionMarkingToolException($"Unsupported lane line style: {styleData.Name}");

            DataProvider.Log($"Line {line} added");
            var lineData = new RegularLineDataProvider(DataProvider, line);
            return lineData;
        }

        public IStopLineData AddStopLine(IEntrancePointData startPointData, IEntrancePointData endPointData, IStopLineStyleData styleData)
        {
            if (styleData == null)
                throw new ArgumentNullException(nameof(styleData));

            APIHelper.CheckPoints(Id, startPointData, endPointData, true);
            if (startPointData.Index == endPointData.Index)
                throw new CreateLineException(startPointData, endPointData, "Start and end of stop line must have differen index");
            var startPoint = APIHelper.GetEntrancePoint(Marking, startPointData);
            var endPoint = APIHelper.GetEntrancePoint(Marking, endPointData);

            var pair = new MarkingPointPair(startPoint, endPoint);
            if (Marking.ExistLine(pair))
                throw new IntersectionMarkingToolException($"Line {pair} already exist");

            MarkingStopLine line;
            if (styleData is StyleDataProvider dataProvider && dataProvider.Style is StopLineStyle stopStyle)
                line = Marking.AddStopLine(pair, stopStyle.CopyLineStyle());
            else
                throw new IntersectionMarkingToolException($"Unsupported stop line style: {styleData.Name}");

            DataProvider.Log($"Line {line} added");
            var lineData = new StopLineDataProvider(DataProvider, line);
            return lineData;
        }

        public INormalLineData AddNormalLine(IEntrancePointData startPointData, INormalPointData endPointData, INormalLineStyleData styleData = null)
        {
            APIHelper.CheckPoints(Id, startPointData, endPointData, true);
            if (startPointData.Index != endPointData.Index)
                throw new CreateLineException(startPointData, endPointData, "Start and end of normal line must have the same index");
            var startPoint = APIHelper.GetEntrancePoint(Marking, startPointData);
            var endPoint = APIHelper.GetNormalPoint(Marking, endPointData);

            var pair = new MarkingPointPair(startPoint, endPoint);
            if (Marking.ExistLine(pair))
                throw new IntersectionMarkingToolException($"Line {pair} already exist");

            MarkingNormalLine line;
            if (styleData is StyleDataProvider dataProvider && dataProvider.Style is RegularLineStyle regularStyle)
                line = Marking.AddNormalLine(pair, regularStyle.CopyLineStyle());
            else if (styleData == null)
                line = Marking.AddNormalLine(pair, null);
            else
                throw new IntersectionMarkingToolException($"Unsupported normal line style: {styleData.Name}");

            DataProvider.Log($"Line {line} added");
            var lineData = new NormalLineDataProvider(DataProvider, line);
            return lineData;
        }

        public ILaneLineData AddLaneLine(ILanePointData startPointData, ILanePointData endPointData, ILaneLineStyleData styleData = null)
        {
            APIHelper.CheckPoints(Id, startPointData, endPointData, false);
            var startPoint = APIHelper.GetLanePoint(Marking, startPointData);
            var endPoint = APIHelper.GetLanePoint(Marking, endPointData);

            var pair = new MarkingPointPair(startPoint, endPoint);
            if (Marking.ExistLine(pair))
                throw new IntersectionMarkingToolException($"Line {pair} already exist");

            MarkingLaneLine line;
            if (styleData is StyleDataProvider dataProvider && dataProvider.Style is RegularLineStyle regularStyle)
                line = Marking.AddLaneLine(pair, regularStyle.CopyLineStyle());
            else if (styleData == null)
                line = Marking.AddLaneLine(pair, null);
            else
                throw new IntersectionMarkingToolException($"Unsupported lane line style: {styleData.Name}");

            DataProvider.Log($"Line {line} added");
            var lineData = new LaneLineDataProvider(DataProvider, line);
            return lineData;
        }

        public ICrosswalkData AddCrosswalk(ICrosswalkPointData startPointData, ICrosswalkPointData endPointData, ICrosswalkStyleData styleData)
        {
            if (styleData == null)
                throw new ArgumentNullException(nameof(styleData));

            APIHelper.CheckPoints(Id, startPointData, endPointData, true);
            if (startPointData.Index == endPointData.Index)
                throw new CreateLineException(startPointData, endPointData, "Start and end of crosswalk must have differen index");
            var startPoint = APIHelper.GetCrosswalkPoint(Marking, startPointData);
            var endPoint = APIHelper.GetCrosswalkPoint(Marking, endPointData);

            var pair = new MarkingPointPair(startPoint, endPoint);
            if (Marking.ExistLine(pair))
                throw new IntersectionMarkingToolException($"Crosswalk {pair} already exist");

            MarkingCrosswalkLine line;
            if (styleData is StyleDataProvider dataProvider && dataProvider.Style is CrosswalkStyle crosswalkStyle)
                line = Marking.AddCrosswalkLine(pair, crosswalkStyle.CopyStyle());
            else
                throw new IntersectionMarkingToolException($"Unsupported crosswalk style: {styleData.Name}");

            DataProvider.Log($"Added crosswalk {line.Crosswalk}");
            var crosswalkData = new CrosswalkDataProvider(DataProvider, line.Crosswalk);
            return crosswalkData;
        }

        public IFillerData AddFiller(IEnumerable<IEntrancePointData> pointDatas, IFillerStyleData styleData)
        {
            if (styleData == null)
                throw new ArgumentNullException(nameof(styleData));

            var contour = APIHelper.GetFillerContour(Marking, pointDatas);
            if (styleData is StyleDataProvider dataProvider && dataProvider.Style is FillerStyle fillerStyle)
            {
                var filler = Marking.AddFiller(contour, fillerStyle.CopyStyle(), out _);
                DataProvider.Log($"Filler {filler} added");
                var fillerData = new FillerDataProvider(DataProvider, filler);
                return fillerData;
            }
            else
                throw new IntersectionMarkingToolException($"Unsupported filler style: {styleData.Name}");
        }

        #endregion

        #region REMOVE

        public void RemoveLine(MarkingLine line)
        {
            Marking.RemoveLine(line);
            DataProvider.Log($"Line {line} removed");
        }
        public bool RemoveLine(MarkingPoint startPoint, MarkingPoint endPoint)
        {
            if (Marking.TryGetLine(new MarkingPointPair(startPoint, endPoint), out var line))
            {
                RemoveLine(line);
                return true;
            }
            else
                return false;
        }
        public bool RemoveRegularLine(IEntrancePointData startPointData, IEntrancePointData endPointData)
        {
            APIHelper.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = APIHelper.GetEntrancePoint(Marking, startPointData);
            var endPoint = APIHelper.GetEntrancePoint(Marking, endPointData);
            return RemoveLine(startPoint, endPoint);
        }
        public bool RemoveNormalLine(IEntrancePointData startPointData, INormalPointData endPointData)
        {
            APIHelper.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = APIHelper.GetEntrancePoint(Marking, startPointData);
            var endPoint = APIHelper.GetNormalPoint(Marking, endPointData);
            return RemoveLine(startPoint, endPoint);
        }
        public bool RemoveStopLine(IEntrancePointData startPointData, IEntrancePointData endPointData)
        {
            APIHelper.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = APIHelper.GetEntrancePoint(Marking, startPointData);
            var endPoint = APIHelper.GetEntrancePoint(Marking, endPointData);
            return RemoveLine(startPoint, endPoint);
        }
        public bool RemoveLaneLine(ILanePointData startPointData, ILanePointData endPointData)
        {
            APIHelper.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = APIHelper.GetLanePoint(Marking, startPointData);
            var endPoint = APIHelper.GetLanePoint(Marking, endPointData);
            return RemoveLine(startPoint, endPoint);
        }

        public bool RemoveCrosswalk(ICrosswalkPointData startPointData, ICrosswalkPointData endPointData)
        {
            APIHelper.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = APIHelper.GetCrosswalkPoint(Marking, startPointData);
            var endPoint = APIHelper.GetCrosswalkPoint(Marking, endPointData);
            return RemoveLine(startPoint, endPoint);
        }
        public bool RemoveCrosswalk(ICrosswalkData crosswalk) => RemoveCrosswalk(crosswalk.Line.StartPoint, crosswalk.Line.EndPoint);

        public void RemoveFiller(MarkingFiller filler)
        {
            Marking.RemoveFiller(filler);
            DataProvider.Log($"Filler {filler} removed");
        }
        public bool RemoveFiller(IFillerData fillerData)
        {
            if (fillerData.MarkingId != Marking.Id)
                throw new MarkingIdNotMatchException(Marking.Id, fillerData.MarkingId);

            if (Marking.TryGetFiller(fillerData.Id, out var filler))
            {
                RemoveFiller(filler);
                return true;
            }
            else
                return false;
        }

        #endregion

        #region EXISTS

        public bool RegularLineExist(IEntrancePointData startPointData, IEntrancePointData endPointData)
        {
            APIHelper.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = APIHelper.GetEntrancePoint(Marking, startPointData);
            var endPoint = APIHelper.GetEntrancePoint(Marking, endPointData);
            return Marking.ExistLine(new MarkingPointPair(startPoint, endPoint));
        }
        public bool NormalLineExist(IEntrancePointData startPointData, INormalPointData endPointData)
        {
            APIHelper.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = APIHelper.GetEntrancePoint(Marking, startPointData);
            var endPoint = APIHelper.GetNormalPoint(Marking, endPointData);
            return Marking.ExistLine(new MarkingPointPair(startPoint, endPoint));
        }
        public bool StopLineExist(IEntrancePointData startPointData, IEntrancePointData endPointData)
        {
            APIHelper.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = APIHelper.GetEntrancePoint(Marking, startPointData);
            var endPoint = APIHelper.GetEntrancePoint(Marking, endPointData);
            return Marking.ExistLine(new MarkingPointPair(startPoint, endPoint));
        }
        public bool LaneLineExist(ILanePointData startPointData, ILanePointData endPointData)
        {
            APIHelper.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = APIHelper.GetLanePoint(Marking, startPointData);
            var endPoint = APIHelper.GetLanePoint(Marking, endPointData);
            return Marking.ExistLine(new MarkingPointPair(startPoint, endPoint));
        }
        public bool CrosswalkExist(ICrosswalkPointData startPointData, ICrosswalkPointData endPointData)
        {
            APIHelper.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = APIHelper.GetCrosswalkPoint(Marking, startPointData);
            var endPoint = APIHelper.GetCrosswalkPoint(Marking, endPointData);
            return Marking.ExistLine(new MarkingPointPair(startPoint, endPoint));
        }

        #endregion

        public override string ToString() => Marking.ToString();
    }
    public struct SegmentMarkingDataProvider : ISegmentMarkingData
    {
        private DataProvider DataProvider { get; }
        IDataProviderV1 IMarkingData.DataProvider => DataProvider;
        public NodeMarkup.API.MarkingType Type => NodeMarkup.API.MarkingType.Segment;
        public ushort Id { get; }

        private SegmentMarking Marking => APIHelper.GetSegmentMarking(Id);
        public int EntranceCount => Marking.EntersCount;
        public IEnumerable<INodeEntranceData> Entrances
        {
            get
            {
                foreach (var enter in Marking.Enters)
                {
                    yield return new NodeEntranceDataProvider(DataProvider, enter);
                }
            }
        }

        public INodeEntranceData StartEntrance => throw new NotImplementedException();
        public INodeEntranceData EndEntrance => throw new NotImplementedException();

        public SegmentMarkingDataProvider(DataProvider dataProvider, SegmentMarking marking)
        {
            DataProvider = dataProvider;
            Id = marking.Id;
        }

        public void ClearMarkings()
        {
            Marking.Clear();
            DataProvider.Log($"Clear Segment #{Id} marking");
        }
        public void ResetPointOffsets()
        {
            Marking.ResetOffsets();
            DataProvider.Log($"Reset Segment #{Id} point offsets");
        }

        #region GET

        public bool TryGetEntrance(ushort id, out INodeEntranceData entrance)
        {
            if (Marking.TryGetEnter(id, out var enter))
            {
                entrance = new NodeEntranceDataProvider(DataProvider, enter);
                return true;
            }
            else
            {
                entrance = null;
                return false;
            }
        }

        public bool TryGetRegularLine(IEntrancePointData startPointData, IEntrancePointData endPointData, out IRegularLineData regularLineData)
        {
            APIHelper.CheckPoints(Id, startPointData, endPointData, true);
            var startPoint = APIHelper.GetEntrancePoint(Marking, startPointData);
            var endPoint = APIHelper.GetEntrancePoint(Marking, endPointData);
            if(Marking.TryGetLine<MarkingRegularLine>(startPoint, endPoint, out var line))
            {
                regularLineData = new RegularLineDataProvider(DataProvider, line);
                return true;
            }
            else
            {
                regularLineData = null;
                return false;
            }
        }
        public bool TryGetLaneLine(ILanePointData startPointData, ILanePointData endPointData, out ILaneLineData laneLineData)
        {
            APIHelper.CheckPoints(Id, startPointData, endPointData, false);
            var startPoint = APIHelper.GetLanePoint(Marking, startPointData);
            var endPoint = APIHelper.GetLanePoint(Marking, endPointData);
            if (Marking.TryGetLine<MarkingLaneLine>(startPoint, endPoint, out var line))
            {
                laneLineData = new LaneLineDataProvider(DataProvider, line);
                return true;
            }
            else
            {
                laneLineData = null;
                return false;
            }
        }

        #endregion

        #region ADD

        public IRegularLineData AddRegularLine(IEntrancePointData startPointData, IEntrancePointData endPointData, IRegularLineStyleData styleData = null)
        {
            APIHelper.CheckPoints(Id, startPointData, endPointData, true);
            var startPoint = APIHelper.GetEntrancePoint(Marking, startPointData);
            var endPoint = APIHelper.GetEntrancePoint(Marking, endPointData);

            var pair = new MarkingPointPair(startPoint, endPoint);
            if (Marking.ExistLine(pair))
                throw new IntersectionMarkingToolException($"Line {pair} already exist");

            MarkingRegularLine line;
            if(styleData is StyleDataProvider dataProvider && dataProvider.Style is RegularLineStyle regularStyle)
                line = Marking.AddRegularLine(pair, regularStyle.CopyLineStyle());
            else if(styleData == null)
                line = Marking.AddRegularLine(pair, null);
            else
                throw new IntersectionMarkingToolException($"Unsupported lane line style: {styleData.Name}");

            DataProvider.Log($"Line {line} added");
            var lineData = new RegularLineDataProvider(DataProvider, line);
            return lineData;
        }
        public ILaneLineData AddLaneLine(ILanePointData startPointData, ILanePointData endPointData, ILaneLineStyleData styleData = null)
        {
            APIHelper.CheckPoints(Id, startPointData, endPointData, false);
            var startPoint = APIHelper.GetLanePoint(Marking, startPointData);
            var endPoint = APIHelper.GetLanePoint(Marking, endPointData);

            var pair = new MarkingPointPair(startPoint, endPoint);
            if (Marking.ExistLine(pair))
                throw new IntersectionMarkingToolException($"Line {pair} already exist");

            MarkingLaneLine line;
            if (styleData is StyleDataProvider dataProvider && dataProvider.Style is RegularLineStyle regularStyle)
                line = Marking.AddLaneLine(pair, regularStyle.CopyLineStyle());
            else if (styleData == null)
                line = Marking.AddLaneLine(pair, null);
            else
                throw new IntersectionMarkingToolException($"Unsupported lane line style: {styleData.Name}");

            DataProvider.Log($"Line {line} added");
            var lineData = new LaneLineDataProvider(DataProvider, line);
            return lineData;
        }
        public IFillerData AddFiller(IEnumerable<IEntrancePointData> pointDatas, IFillerStyleData styleData)
        {
            if (styleData == null)
                throw new ArgumentNullException(nameof(styleData));

            var contour = APIHelper.GetFillerContour(Marking, pointDatas);
            if (styleData is StyleDataProvider dataProvider && dataProvider.Style is FillerStyle fillerStyle)
            {
                var filler = Marking.AddFiller(contour, fillerStyle.CopyStyle(), out _);
                DataProvider.Log($"Filler {filler} added");
                var fillerData = new FillerDataProvider(DataProvider, filler);
                return fillerData;
            }
            else
                throw new IntersectionMarkingToolException($"Unsupported filler style: {styleData.Name}");
        }

        #endregion

        #region REMOVE

        public void RemoveLine(MarkingLine line)
        {
            Marking.RemoveLine(line);
            DataProvider.Log($"Line {line} removed");
        }
        public bool RemoveLine(MarkingPoint startPoint, MarkingPoint endPoint)
        {
            if (Marking.TryGetLine(new MarkingPointPair(startPoint, endPoint), out var line))
            {
                RemoveLine(line);
                return true;
            }
            else
                return false;
        }
        public bool RemoveRegularLine(IEntrancePointData startPointData, IEntrancePointData endPointData)
        {
            APIHelper.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = APIHelper.GetEntrancePoint(Marking, startPointData);
            var endPoint = APIHelper.GetEntrancePoint(Marking, endPointData);
            return RemoveLine(startPoint, endPoint);
        }
        public bool RemoveLaneLine(ILanePointData startPointData, ILanePointData endPointData)
        {
            APIHelper.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = APIHelper.GetLanePoint(Marking, startPointData);
            var endPoint = APIHelper.GetLanePoint(Marking, endPointData);
            return RemoveLine(startPoint, endPoint);
        }
        public bool RemoveFiller(IFillerData fillerData)
        {
            if (fillerData.MarkingId != Marking.Id)
                throw new MarkingIdNotMatchException(Marking.Id, fillerData.MarkingId);

            if (Marking.TryGetFiller(fillerData.Id, out var filler))
            {
                Marking.RemoveFiller(filler);
                DataProvider.Log($"Filler {filler} removed");
                return true;
            }
            else
                return false;
        }

        #endregion

        #region EXISTS

        public bool RegularLineExist(IEntrancePointData startPointData, IEntrancePointData endPointData)
        {
            APIHelper.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = APIHelper.GetEntrancePoint(Marking, startPointData);
            var endPoint = APIHelper.GetEntrancePoint(Marking, endPointData);
            return Marking.ExistLine(new MarkingPointPair(startPoint, endPoint));
        }
        public bool LaneLineExist(ILanePointData startPointData, ILanePointData endPointData)
        {
            APIHelper.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = APIHelper.GetLanePoint(Marking, startPointData);
            var endPoint = APIHelper.GetLanePoint(Marking, endPointData);
            return Marking.ExistLine(new MarkingPointPair(startPoint, endPoint));
        }
        public bool CrosswalkExist(ICrosswalkPointData startPointData, ICrosswalkPointData endPointData)
        {
            APIHelper.CheckPoints(Id, startPointData, endPointData, null);
            var startPoint = APIHelper.GetCrosswalkPoint(Marking, startPointData);
            var endPoint = APIHelper.GetCrosswalkPoint(Marking, endPointData);
            return Marking.ExistLine(new MarkingPointPair(startPoint, endPoint));
        }

        #endregion

        public override string ToString() => Marking.ToString();
    }
}
