using ModsCommon;
using NodeMarkup.API;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NodeMarkup.Utilities.API
{
    public struct RegularLineDataProvider : IRegularLineData
    {
        public DataProvider DataProvider { get; }
        IDataProviderV1 ILineData.DataProvider => DataProvider;

        public Manager.MarkingType MarkingType { get; }
        public ushort MarkingId { get; }
        public ulong Id { get; }

        IMarkingData ILineData.Marking
        {
            get
            {
                if(DataProvider.TryGetMarking(MarkingId, MarkingType, out IMarkingData marking))
                    return marking;
                else
                    return null;
            }
        }

        private Marking Marking => APIHelper.GetMarking(MarkingId, MarkingType);
        private MarkingRegularLine Line => APIHelper.GetLine<MarkingRegularLine>(MarkingId, MarkingType, Id);

        public IEntrancePointData StartPoint => new EntrancePointDataProvider(DataProvider, Line.Start as MarkingEnterPoint);
        public IEntrancePointData EndPoint => new EntrancePointDataProvider(DataProvider, Line.End as MarkingEnterPoint);
        public IEnumerable<ILineRuleData> Rules => Line.Rules.Select(x => (ILineRuleData)new RuleDataProvider(x));

        public RegularLineDataProvider(DataProvider dataProvider, MarkingRegularLine line)
        {
            DataProvider = dataProvider;
            MarkingId = line.Marking.Id;
            MarkingType = line.Marking.Type;
            Id = line.Id;
        }

        public ILineRuleData AddRule(IRegularLineStyleData styleData)
        {
            var type = APIHelper.GetStyleType<RegularLineStyle.RegularLineType>(styleData.Name);
            var style = RegularLineStyle.GetDefault(type);
            var rule = Line.AddRule(style, false);
            DataProvider.Log($"Rule added to {Line}");
            return new RuleDataProvider(rule);
        }

        public bool Remove()
        {
            var line = Line;
            Marking.RemoveLine(line);
            DataProvider.Log($"Line {line} removed");
            return true;
        }

        public override string ToString() => Line.ToString();
    }
    public struct StopLineDataProvider : IStopLineData
    {
        public DataProvider DataProvider { get; }
        IDataProviderV1 ILineData.DataProvider => DataProvider;
        public Manager.MarkingType MarkingType { get; }
        public ushort MarkingId { get; }
        public ulong Id { get; }

        IMarkingData ILineData.Marking
        {
            get
            {
                if (DataProvider.TryGetMarking(MarkingId, MarkingType, out IMarkingData marking))
                    return marking;
                else
                    return null;
            }
        }

        private Marking Marking => APIHelper.GetMarking(MarkingId, MarkingType);
        private MarkingStopLine Line => APIHelper.GetLine<MarkingStopLine>(MarkingId, MarkingType, Id);

        public IEntrancePointData StartPoint => new EntrancePointDataProvider(DataProvider, Line.Start as MarkingEnterPoint);
        public IEntrancePointData EndPoint => new EntrancePointDataProvider(DataProvider, Line.End as MarkingEnterPoint);

        public StopLineDataProvider(DataProvider dataProvider, MarkingStopLine line)
        {
            DataProvider = dataProvider;
            MarkingId = line.Marking.Id;
            MarkingType = line.Marking.Type;
            Id = line.Id;
        }

        public bool Remove()
        {
            var line = Line;
            Marking.RemoveLine(line);
            DataProvider.Log($"Line {line} removed");
            return true;
        }

        public override string ToString() => Line.ToString();
    }
    public struct NormalLineDataProvider : INormalLineData
    {
        public DataProvider DataProvider { get; }
        IDataProviderV1 ILineData.DataProvider => DataProvider;
        public Manager.MarkingType MarkingType { get; }
        public ushort MarkingId { get; }
        public ulong Id { get; }

        IMarkingData ILineData.Marking
        {
            get
            {
                if (DataProvider.TryGetMarking(MarkingId, MarkingType, out IMarkingData marking))
                    return marking;
                else
                    return null;
            }
        }

        private Marking Marking => APIHelper.GetMarking(MarkingId, MarkingType);
        private MarkingNormalLine Line => APIHelper.GetLine<MarkingNormalLine>(MarkingId, MarkingType, Id);

        public IEntrancePointData StartPoint
        {
            get
            {
                var line = Line;
                return new EntrancePointDataProvider(DataProvider, (line.Start is MarkingEnterPoint ? line.Start : line.End) as MarkingEnterPoint);
            }
        }
        public INormalPointData EndPoint
        {
            get
            {
                var line = Line;
                return new NormalPointDataProvider(DataProvider, (line.Start is MarkingNormalPoint ? line.Start : line.End) as MarkingNormalPoint);
            }
        }
        public IEnumerable<ILineRuleData> Rules => Line.Rules.Select(x => (ILineRuleData)new RuleDataProvider(x));

        public NormalLineDataProvider(DataProvider dataProvider, MarkingNormalLine line)
        {
            DataProvider = dataProvider;
            MarkingId = line.Marking.Id;
            MarkingType = line.Marking.Type;
            Id = line.Id;
        }

        public ILineRuleData AddRule(INormalLineStyleData styleData)
        {
            var type = APIHelper.GetStyleType<RegularLineStyle.RegularLineType>(styleData.Name);
            var style = RegularLineStyle.GetDefault(type);
            var rule = Line.AddRule(style, false);
            DataProvider.Log($"Rule added to {Line}");
            return new RuleDataProvider(rule);
        }
        public bool Remove()
        {
            var line = Line;
            Marking.RemoveLine(line);
            DataProvider.Log($"Line {line} removed");
            return true;
        }

        public override string ToString() => Line.ToString();
    }
    public struct LaneLineDataProvider : ILaneLineData
    {
        public DataProvider DataProvider { get; }
        IDataProviderV1 ILineData.DataProvider => DataProvider;
        public Manager.MarkingType MarkingType { get; }
        public ushort MarkingId { get; }
        public ulong Id { get; }

        IMarkingData ILineData.Marking
        {
            get
            {
                if (DataProvider.TryGetMarking(MarkingId, MarkingType, out IMarkingData marking))
                    return marking;
                else
                    return null;
            }
        }

        private Marking Marking => APIHelper.GetMarking(MarkingId, MarkingType);
        private MarkingLaneLine Line => APIHelper.GetLine<MarkingLaneLine>(MarkingId, MarkingType, Id);

        public ILanePointData StartPoint => new LanePointDataProvider(DataProvider, Line.Start as MarkingLanePoint);
        public ILanePointData EndPoint => new LanePointDataProvider(DataProvider, Line.End as MarkingLanePoint);
        public IEnumerable<ILineRuleData> Rules => Line.Rules.Select(x => (ILineRuleData)new RuleDataProvider(x));

        public LaneLineDataProvider(DataProvider dataProvider, MarkingLaneLine line)
        {
            DataProvider = dataProvider;
            MarkingId = line.Marking.Id;
            MarkingType = line.Marking.Type;
            Id = line.Id;
        }

        public ILineRuleData AddRule(ILaneLineStyleData styleData)
        {
            var type = APIHelper.GetStyleType<RegularLineStyle.RegularLineType>(styleData.Name);
            var style = RegularLineStyle.GetDefault(type);
            var rule = Line.AddRule(style, false);
            DataProvider.Log($"Rule added to {Line}");
            return new RuleDataProvider(rule);
        }
        public bool Remove()
        {
            var line = Line;
            Marking.RemoveLine(line);
            DataProvider.Log($"Line {line} removed");
            return true;
        }

        public override string ToString() => Line.ToString();
    }
    public struct CrosswalkLineDataProvider : ICrosswalkLineData
    {
        public DataProvider DataProvider { get; }
        IDataProviderV1 ILineData.DataProvider => DataProvider;
        public Manager.MarkingType MarkingType { get; }
        public ushort MarkingId { get; }
        public ulong Id { get; }

        IMarkingData ILineData.Marking
        {
            get
            {
                if (DataProvider.TryGetMarking(MarkingId, MarkingType, out IMarkingData marking))
                    return marking;
                else
                    return null;
            }
        }

        private Marking Marking => APIHelper.GetMarking(MarkingId, MarkingType);
        private MarkingCrosswalkLine Line => APIHelper.GetLine<MarkingCrosswalkLine>(MarkingId, MarkingType, Id);

        public ICrosswalkData Crosswalk => new CrosswalkDataProvider(DataProvider, Line.Crosswalk);

        public ICrosswalkPointData StartPoint => new CrosswalkPointDataProvider(DataProvider, Line.Start as MarkingCrosswalkPoint);
        public ICrosswalkPointData EndPoint => new CrosswalkPointDataProvider(DataProvider, Line.End as MarkingCrosswalkPoint);

        public CrosswalkLineDataProvider(DataProvider dataProvider, MarkingCrosswalkLine line)
        {
            DataProvider = dataProvider;
            MarkingId = line.Marking.Id;
            MarkingType = line.Marking.Type;
            Id = line.Id;
        }

        public ILineRuleData AddRule(IRegularLineStyleData styleData)
        {
            var type = APIHelper.GetStyleType<RegularLineStyle.RegularLineType>(styleData.Name);
            var style = RegularLineStyle.GetDefault(type);
            var rule = Line.AddRule(style, false);
            DataProvider.Log($"Rule added to {Line}");
            return new RuleDataProvider(rule);
        }
        public bool Remove()
        {
            var line = Line;
            Marking.RemoveLine(line);
            DataProvider.Log($"Line {line} removed");
            return true;
        }

        public override string ToString() => Line.ToString();
    }

    public struct CrosswalkDataProvider : ICrosswalkData
    {
        public DataProvider DataProvider { get; }
        IDataProviderV1 ICrosswalkData.DataProvider => DataProvider;
        public Manager.MarkingType MarkingType { get; }
        public ushort MarkingId { get; }
        public ulong Id { get; }
        private Marking Marking => APIHelper.GetMarking(MarkingId, MarkingType);
        private MarkingCrosswalkLine Line => APIHelper.GetLine<MarkingCrosswalkLine>(MarkingId, MarkingType, Id);

        ICrosswalkLineData ICrosswalkData.Line => new CrosswalkLineDataProvider(DataProvider, Line);

        IMarkingData ICrosswalkData.Marking
        {
            get
            {
                if (DataProvider.TryGetMarking(MarkingId, MarkingType, out IMarkingData marking))
                    return marking;
                else
                    return null;
            }
        }

        public CrosswalkDataProvider(DataProvider dataProvider, MarkingCrosswalk crosswalk)
        {
            DataProvider = dataProvider;
            MarkingId = crosswalk.Marking.Id;
            MarkingType = crosswalk.Marking.Type;
            Id = crosswalk.CrosswalkLine.Id;
        }

        public bool Remove()
        {
            var line = Line;
            Marking.RemoveLine(line);
            DataProvider.Log($"Line {line} removed");
            return true;
        }

        public override string ToString() => Line.ToString();
    }
    public struct FillerDataProvider : IFillerData
    {
        public DataProvider DataProvider { get; }
        IDataProviderV1 IFillerData.DataProvider => DataProvider;
        public Manager.MarkingType MarkingType { get; }
        public ushort MarkingId { get; }
        public int Id { get; }
        private Marking Marking => APIHelper.GetMarking(MarkingId, MarkingType);
        private MarkingFiller Filler => APIHelper.GetFiller(MarkingId, MarkingType, Id);

        IMarkingData IFillerData.Marking
        {
            get
            {
                if (DataProvider.TryGetMarking(MarkingId, MarkingType, out IMarkingData marking))
                    return marking;
                else
                    return null;
            }
        }

        public IEnumerable<IEntrancePointData> PointDatas
        {
            get
            {
                foreach (var supportPoint in Filler.Contour.RawVertices.OfType<EnterSupportPoint>())
                {
                    if (supportPoint.Point is MarkingEnterPoint enterPoint)
                        yield return new EntrancePointDataProvider(DataProvider, enterPoint);
                }
            }
        }

        public FillerDataProvider(DataProvider dataProvider, MarkingFiller filler)
        {
            DataProvider = dataProvider;
            MarkingId = filler.Marking.Id;
            MarkingType = filler.Marking.Type;
            Id = filler.Id;
        }

        public bool Remove()
        {
            var filler = Filler;
            Marking.RemoveFiller(filler);
            DataProvider.Log($"Filler {filler} removed");
            return true;
        }

        public override string ToString() => Filler.ToString();
    }

    public struct RuleDataProvider : ILineRuleData
    {
        private MarkingLineRawRule Rule { get; }

        public RuleDataProvider(MarkingLineRawRule markingLineRawRule)
        {
            Rule = markingLineRawRule;
        }
    }
}
