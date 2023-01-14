using System;
using System.Collections.Generic;

namespace NodeMarkup.API
{
    public static class Helper
    {
        public static IDataProviderV1 GetProvider(string id)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (Type type in assembly.GetExportedTypes())
                    {
                        if (type.IsClass && typeof(IDataProviderFactory).IsAssignableFrom(type))
                        {
                            var factory = (IDataProviderFactory)Activator.CreateInstance(type);
                            var provider = factory.GetProvider(id);
                            return provider;
                        }
                    }
                }
                catch { }
            }

            return null;
        }
    }
    public interface IDataProviderFactory
    {
        IDataProviderV1 GetProvider(string id);
    }
    public interface IDataProviderV1
    {
        public Version ModVersion { get; }
        public bool IsBeta { get; }

        public IEnumerable<string> RegularLineStyles { get; }
        public IEnumerable<string> NormalLineStyles { get; }
        public IEnumerable<string> StopLineStyles { get; }
        public IEnumerable<string> LaneLineStyles { get; }
        public IEnumerable<string> CrosswalkStyles { get; }
        public IEnumerable<string> FillerStyles { get; }

        public bool GetNodeMarking(ushort id, out INodeMarkingData nodeMarking);
        public bool GetSegmentMarking(ushort id, out ISegmentMarkingData segmentMarking);

        public bool NodeMarkingExist(ushort id);
        public bool SegmentMarkingExist(ushort id);

        public IRegularLineStyleData GetRegularLineStyle(string name);
        public INormalLineStyleData GetNormalLineStyle(string name);
        public IStopLineStyleData GetStopLineStyle(string name);
        public ILaneLineStyleData GetLaneLineStyle(string name);
        public ICrosswalkStyleData GetCrosswalkStyle(string name);
        public IFillerStyleData GetFillerStyle(string name);
    }

    public interface IMarkingData
    {
        public ushort Id { get; }
        public int EntranceCount { get; }

        public IRegularLineData AddRegularLine(IEntrancePointData startPoint, IEntrancePointData endPoint, IRegularLineStyleData style);
        public ILaneLineData AddLaneLine(ILanePointData startPoint, ILanePointData endPoint, ILaneLineStyleData style);
        public IFillerData AddFiller(IEnumerable<IEntrancePointData> pointDatas, IFillerStyleData style);

        public bool RemoveRegularLine(IEntrancePointData startPoint, IEntrancePointData endPoint);
        public bool RemoveLaneLine(ILanePointData startPoint, ILanePointData endPoint);
        public bool RemoveFiller(IFillerData filler);

        public bool RegularLineExist(IEntrancePointData startPoint, IEntrancePointData endPoint);
        public bool LaneLineExist(ILanePointData startPoint, ILanePointData endPoint);
    }
    public interface INodeMarkingData : IMarkingData
    {
        public IEnumerable<ISegmentEntranceData> Entrances { get; }

        public bool GetEntrance(ushort id, out ISegmentEntranceData entrance);

        public IStopLineData AddStopLine(IEntrancePointData startPoint, IEntrancePointData endPoint, IStopLineStyleData style);
        public INormalLineData AddNormalLine(IEntrancePointData startPoint, INormalPointData endPoint, INormalLineStyleData style);
        public ICrosswalkData AddCrosswalk(ICrosswalkPointData startPoint, ICrosswalkPointData endPoint, ICrosswalkStyleData style);

        public bool RemoveStopLine(IEntrancePointData startPoint, IEntrancePointData endPoint);
        public bool RemoveNormalLine(IEntrancePointData startPoint, INormalPointData endPoint);
        public bool RemoveCrosswalk(ICrosswalkPointData startPoint, ICrosswalkPointData endPoint);

        public bool NormalLineExist(IEntrancePointData startPoint, INormalPointData endPoint);
        public bool StopLineExist(IEntrancePointData startPoint, IEntrancePointData endPoint);
        public bool CrosswalkExist(ICrosswalkPointData startPoint, ICrosswalkPointData endPoint);
    }
    public interface ISegmentMarkingData : IMarkingData
    {
        public IEnumerable<INodeEntranceData> Entrances { get; }

        public bool GetEntrance(ushort id, out INodeEntranceData entrance);
    }
    public interface IEntranceData
    {
        public ushort Id { get; }
        public int PointCount { get; }
        public IEnumerable<IPointData> Points { get; }
    }
    public interface ISegmentEntranceData : IEntranceData
    {
        public IEnumerable<IEntrancePointData> EntrancePoints { get; }
        public IEnumerable<INormalPointData> NormalPoints { get; }
        public IEnumerable<ICrosswalkPointData> CrosswalkPoints { get; }
        public IEnumerable<ILanePointData> LanePoints { get; }

        public bool GetEntrancePoint(byte index, out IEntrancePointData point);
        public bool GetNormalPoint(byte index, out INormalPointData point);
        public bool GetCrosswalkPoint(byte index, out ICrosswalkPointData point);
        public bool GetLanePoint(byte index, out ILanePointData point);
    }
    public interface INodeEntranceData : IEntranceData
    {
        public bool GetEntrancePoint(byte index, out IEntrancePointData point);
        public bool GetLanePoint(byte index, out ILanePointData point);

        public IEnumerable<IEntrancePointData> EntrancePoints { get; }
        public IEnumerable<ILanePointData> LanePoints { get; }
    }

    public interface IPointData
    {
        public byte Index { get; }
        public ushort EntranceId { get; }
        public ushort MarkingId { get; }
        public IPointSource Source { get; }
    }
    public interface IEntrancePointData : IPointData
    {

    }
    public interface INormalPointData : IPointData
    {

    }
    public interface ICrosswalkPointData : IPointData
    {

    }
    public interface ILanePointData : IPointData
    {

    }
    public interface IPointSource
    {

    }

    public interface ILineData
    {
        public ulong Id { get; }
    }
    public interface IRegularLineData : ILineData
    {
        IEntrancePointData StartPoint { get; }
        IEntrancePointData EndPoint { get; }
    }
    public interface IStopLineData : ILineData
    {
        IEntrancePointData StartPoint { get; }
        IEntrancePointData EndPoint { get; }
    }
    public interface INormalLineData : ILineData
    {
        IEntrancePointData StartPoint { get; }
        INormalPointData EndPoint { get; }
    }
    public interface ICrosswalkLineData : ILineData
    {
        ICrosswalkData Crosswalk { get; }
        ICrosswalkPointData StartPoint { get; }
        ICrosswalkPointData EndPoint { get; }
    }
    public interface ILaneLineData : ILineData
    {
        ILanePointData StartPoint { get; }
        ILanePointData EndPoint { get; }
    }

    public interface IStyleData
    {
        string Name { get; }
        IEnumerable<IStyleOptionData> Options { get; }
        object GetValue(IStyleOptionData option);
        void SetValue(IStyleOptionData option, object value);
    }
    public interface IStyleOptionData
    {
        Type Type { get; }
        string Name { get; }
        object DefaultValue { get; }
    }
    public interface IRegularLineStyleData : IStyleData
    {

    }
    public interface INormalLineStyleData : IStyleData
    {

    }
    public interface IStopLineStyleData : IStyleData
    {

    }
    public interface ILaneLineStyleData : IStyleData
    {

    }
    public interface ICrosswalkStyleData : IStyleData
    {

    }
    public interface IFillerStyleData : IStyleData
    {

    }

    public interface IRuleData
    {

    }
    public interface IFillerData
    {
        public ushort MarkingId { get; }
        public int Id { get; }
    }
    public interface ICrosswalkData
    {
        public ushort MarkingId { get; }
        public ICrosswalkLineData Line { get; }
    }
}