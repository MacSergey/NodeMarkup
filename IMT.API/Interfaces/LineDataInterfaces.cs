using System.Collections.Generic;

namespace IMT.API
{
    public interface ILineData
    {
        IDataProviderV1 DataProvider { get; }
        IMarkingData Marking { get; }
        ushort MarkingId { get; }
        ulong Id { get; }

        bool Remove();
    }
    public interface IRegularLineData : ILineData
    {
        IEnumerable<ILineRuleData> Rules { get; }
        IEntrancePointData StartPoint { get; }
        IEntrancePointData EndPoint { get; }

        ILineRuleData AddRule(IRegularLineStyleData line);
    }
    public interface INormalLineData : ILineData
    {
        IEnumerable<ILineRuleData> Rules { get; }
        IEntrancePointData StartPoint { get; }
        INormalPointData EndPoint { get; }

        ILineRuleData AddRule(INormalLineStyleData line);
    }
    public interface ICrosswalkLineData : ILineData
    {
        ICrosswalkPointData StartPoint { get; }
        ICrosswalkPointData EndPoint { get; }

        ILineRuleData AddRule(IRegularLineStyleData line);
    }
    public interface ILaneLineData : ILineData
    {
        ILanePointData EndPoint { get; }
        ILanePointData StartPoint { get; }
        IEnumerable<ILineRuleData> Rules { get; }

        ILineRuleData AddRule(ILaneLineStyleData line);
    }
    public interface IStopLineData : ILineData
    {
        IEntrancePointData StartPoint { get; }
        IEntrancePointData EndPoint { get; }
    }

    public interface ILineRuleData
    {
    }
}