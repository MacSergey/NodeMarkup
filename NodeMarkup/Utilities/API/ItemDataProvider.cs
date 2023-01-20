using NodeMarkup.API;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using static NodeMarkup.Manager.RegularLineStyle;

namespace NodeMarkup.Utilities.API
{
    public struct RegularLineDataProvider : IRegularLineData
    {
        public DataProvider DataProvider { get; }
        IDataProviderV1 ILineData.DataProvider => DataProvider;
        public ushort MarkingId => Line.Marking.Id;
        public IMarkingData Marking
        {
            get
            {
                if(DataProvider.TryGetMarking(Line.Marking.Id, Line.Marking.Type, out IMarkingData marking))
                    return marking;
                else
                    return null;
            }
        }
        private MarkingRegularLine Line { get; }
        public ulong Id => Line.Id;

        public IEntrancePointData StartPoint => new EntrancePointDataProvider(Line.Start as MarkingEnterPoint);
        public IEntrancePointData EndPoint => new EntrancePointDataProvider(Line.End as MarkingEnterPoint);
        public IEnumerable<ILineRuleData> Rules => Line.Rules.Select(x => (ILineRuleData)new RuleDataProvider(x));

        public RegularLineDataProvider(DataProvider dataProvider, MarkingRegularLine line)
        {
            DataProvider = dataProvider;
            Line = line;
        }

        public ILineRuleData AddRule(IRegularLineStyleData styleData)
        {
            var type = APIHelper.GetStyleType<RegularLineType>(styleData.Name);
            var style = RegularLineStyle.GetDefault(type);
            var rule = Line.AddRule(style, false);
            DataProvider.Log($"Rule added to {Line}");
            return new RuleDataProvider(rule);
        }

        public bool Remove()
        {
            if (Marking is IMarkingDataProvider markingDataProvider)
            {
                markingDataProvider.RemoveLine(Line);
                return true;
            }
            else
                throw new MarkingNotExistException();
        }

        public override string ToString() => Line.ToString();
    }
    public struct StopLineDataProvider : IStopLineData
    {
        public DataProvider DataProvider { get; }
        IDataProviderV1 ILineData.DataProvider => DataProvider;
        public ushort MarkingId => Line.Marking.Id;
        public IMarkingData Marking
        {
            get
            {
                if (DataProvider.TryGetMarking(Line.Marking.Id, Line.Marking.Type, out IMarkingData marking))
                    return marking;
                else
                    return null;
            }
        }
        private MarkingStopLine Line { get; }
        public ulong Id => Line.Id;

        public IEntrancePointData StartPoint => new EntrancePointDataProvider(Line.Start as MarkingEnterPoint);
        public IEntrancePointData EndPoint => new EntrancePointDataProvider(Line.End as MarkingEnterPoint);

        public StopLineDataProvider(DataProvider dataProvider, MarkingStopLine line)
        {
            DataProvider = dataProvider;
            Line = line;
        }

        public bool Remove()
        {
            if (Marking is IMarkingDataProvider markingDataProvider)
            {
                markingDataProvider.RemoveLine(Line);
                return true;
            }
            else
                throw new MarkingNotExistException();
        }

        public override string ToString() => Line.ToString();
    }
    public struct NormalLineDataProvider : INormalLineData
    {
        public DataProvider DataProvider { get; }
        IDataProviderV1 ILineData.DataProvider => DataProvider;
        public ushort MarkingId => Line.Marking.Id;
        public IMarkingData Marking
        {
            get
            {
                if (DataProvider.TryGetMarking(Line.Marking.Id, Line.Marking.Type, out IMarkingData marking))
                    return marking;
                else
                    return null;
            }
        }
        private MarkingNormalLine Line { get; }
        public ulong Id => Line.Id;

        public IEntrancePointData StartPoint => new EntrancePointDataProvider((Line.Start is MarkingEnterPoint ? Line.Start : Line.End) as MarkingEnterPoint);
        public INormalPointData EndPoint => new NormalPointDataProvider((Line.Start is MarkingNormalPoint ? Line.Start : Line.End) as MarkingNormalPoint);
        public IEnumerable<ILineRuleData> Rules => Line.Rules.Select(x => (ILineRuleData)new RuleDataProvider(x));

        public NormalLineDataProvider(DataProvider dataProvider, MarkingNormalLine line)
        {
            DataProvider = dataProvider;
            Line = line;
        }

        public ILineRuleData AddRule(INormalLineStyleData styleData)
        {
            var type = APIHelper.GetStyleType<RegularLineType>(styleData.Name);
            var style = RegularLineStyle.GetDefault(type);
            var rule = Line.AddRule(style, false);
            DataProvider.Log($"Rule added to {Line}");
            return new RuleDataProvider(rule);
        }
        public bool Remove()
        {
            if (Marking is IMarkingDataProvider markingDataProvider)
            {
                markingDataProvider.RemoveLine(Line);
                return true;
            }
            else
                throw new MarkingNotExistException();
        }

        public override string ToString() => Line.ToString();
    }
    public struct LaneLineDataProvider : ILaneLineData
    {
        public DataProvider DataProvider { get; }
        IDataProviderV1 ILineData.DataProvider => DataProvider;
        public ushort MarkingId => Line.Marking.Id;
        public IMarkingData Marking
        {
            get
            {
                if (DataProvider.TryGetMarking(Line.Marking.Id, Line.Marking.Type, out IMarkingData marking))
                    return marking;
                else
                    return null;
            }
        }
        private MarkingLaneLine Line { get; }
        public ulong Id => Line.Id;

        public ILanePointData StartPoint => new LanePointDataProvider(Line.Start as MarkingLanePoint);
        public ILanePointData EndPoint => new LanePointDataProvider(Line.End as MarkingLanePoint);
        public IEnumerable<ILineRuleData> Rules => Line.Rules.Select(x => (ILineRuleData)new RuleDataProvider(x));

        public LaneLineDataProvider(DataProvider dataProvider, MarkingLaneLine line)
        {
            DataProvider = dataProvider;
            Line = line;
        }

        public ILineRuleData AddRule(ILaneLineStyleData styleData)
        {
            var type = APIHelper.GetStyleType<RegularLineType>(styleData.Name);
            var style = RegularLineStyle.GetDefault(type);
            var rule = Line.AddRule(style, false);
            DataProvider.Log($"Rule added to {Line}");
            return new RuleDataProvider(rule);
        }
        public bool Remove()
        {
            if (Marking is IMarkingDataProvider markingDataProvider)
            {
                markingDataProvider.RemoveLine(Line);
                return true;
            }
            else
                throw new MarkingNotExistException();
        }

        public override string ToString() => Line.ToString();
    }
    public struct CrosswalkLineDataProvider : ICrosswalkLineData
    {
        public DataProvider DataProvider { get; }
        IDataProviderV1 ILineData.DataProvider => DataProvider;
        public ushort MarkingId => Line.Marking.Id;
        public IMarkingData Marking
        {
            get
            {
                if (DataProvider.TryGetMarking(Line.Marking.Id, Line.Marking.Type, out IMarkingData marking))
                    return marking;
                else
                    return null;
            }
        }
        private MarkingCrosswalkLine Line { get; }
        public ulong Id => Line.Id;

        public ICrosswalkData Crosswalk => new CrosswalkDataProvider(DataProvider, Line.Crosswalk);

        public ICrosswalkPointData StartPoint => new CrosswalkPointDataProvider(Line.Start as MarkingCrosswalkPoint);
        public ICrosswalkPointData EndPoint => new CrosswalkPointDataProvider(Line.End as MarkingCrosswalkPoint);

        public CrosswalkLineDataProvider(DataProvider dataProvider, MarkingCrosswalkLine line)
        {
            DataProvider = dataProvider;
            Line = line;
        }

        public ILineRuleData AddRule(IRegularLineStyleData styleData)
        {
            var type = APIHelper.GetStyleType<RegularLineType>(styleData.Name);
            var style = RegularLineStyle.GetDefault(type);
            var rule = Line.AddRule(style, false);
            DataProvider.Log($"Rule added to {Line}");
            return new RuleDataProvider(rule);
        }
        public bool Remove()
        {
            if (Marking is IMarkingDataProvider markingDataProvider)
            {
                markingDataProvider.RemoveLine(Line);
                return true;
            }
            else
                throw new MarkingNotExistException();
        }

        public override string ToString() => Line.ToString();
    }

    public struct CrosswalkDataProvider : ICrosswalkData
    {
        public DataProvider DataProvider { get; }
        IDataProviderV1 ICrosswalkData.DataProvider => DataProvider;
        private MarkupCrosswalk Crosswalk { get; }
        public ICrosswalkLineData Line => new CrosswalkLineDataProvider(DataProvider, Crosswalk.CrosswalkLine);

        public ushort MarkingId => Crosswalk.Marking.Id;
        public IMarkingData Marking
        {
            get
            {
                if (DataProvider.TryGetMarking(Crosswalk.Marking.Id, Crosswalk.Marking.Type, out IMarkingData marking))
                    return marking;
                else
                    return null;
            }
        }

        public CrosswalkDataProvider(DataProvider dataProvider, MarkupCrosswalk crosswalk)
        {
            DataProvider = dataProvider;
            Crosswalk = crosswalk;
        }

        public bool Remove()
        {
            if (Marking is IMarkingDataProvider markingDataProvider)
            {
                markingDataProvider.RemoveLine(Crosswalk.CrosswalkLine);
                return true;
            }
            else
                throw new MarkingNotExistException();
        }

        public override string ToString() => Crosswalk.ToString();
    }
    public struct FillerDataProvider : IFillerData
    {
        public DataProvider DataProvider { get; }
        IDataProviderV1 IFillerData.DataProvider => DataProvider;
        private MarkingFiller Filler { get; }
        public int Id => Filler.Id;

        public ushort MarkingId => Filler.Marking.Id;
        public IMarkingData Marking
        {
            get
            {
                if (DataProvider.TryGetMarking(Filler.Marking.Id, Filler.Marking.Type, out IMarkingData marking))
                    return marking;
                else
                    return null;
            }
        }

        public IEnumerable<IEntrancePointData> PointDatas => throw new NotImplementedException();

        public FillerDataProvider(DataProvider dataProvider, MarkingFiller filler)
        {
            DataProvider = dataProvider;
            Filler = filler;
        }

        public bool Remove()
        {
            if (Marking is IMarkingDataProvider markingDataProvider)
            {
                markingDataProvider.RemoveFiller(Filler);
                return true;
            }
            else
                throw new MarkingNotExistException();
        }

        public override string ToString() => Filler.ToString();
    }

    public struct RuleDataProvider : ILineRuleData
    {
        private MarkupLineRawRule Rule { get; }

        public RuleDataProvider(MarkupLineRawRule markupLineRawRule)
        {
            Rule = markupLineRawRule;
        }
    }
}
