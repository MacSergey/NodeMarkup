using System;
using System.Collections.Generic;

namespace NodeMarkup.API
{
    public static class Helper
    {
        public static IDataProviderV1 GetInstance()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (Type type in assembly.GetExportedTypes())
                    {
                        if (type.IsClass && typeof(IDataProviderV1).IsAssignableFrom(type))
                        {
                            var instance = (IDataProviderV1)Activator.CreateInstance(type);
                            return instance;
                        }
                    }
                }
                catch { }
            }

            return null;
        }
    }
    public interface IDataProviderV1
    {
        public Version ModVersion { get; }
        public bool IsBeta { get; }

        public bool GetNodeMarking(ushort id, out INodeMarkingData nodeMarking);
        public bool GetSegmentMarking(ushort id, out ISegmentMarkingData segmentMarking);

        public bool NodeMarkingExist(ushort id);
        public bool SegmentMarkingExist(ushort id);
    }

    public interface INodeMarkingData
    {
        public ushort Id { get; }
        public int EntranceCount { get; }
        public IEnumerable<ISegmentEntranceData> Entrances { get; }

        public bool GetEntrance(ushort id, out ISegmentEntranceData entrance);

        public bool AddRegularLine(IEntrancePointData startPoint, IEntrancePointData endPoint, IStyleData style, out IRegularLineData line);
        public bool AddStopLine(IEntrancePointData startPoint, IEntrancePointData endPoint, IStyleData style, out IStopLineData line);
        public bool AddNormalLine(IEntrancePointData startPoint, INormalPointData endPoint, IStyleData style, out INormalLineData line);
        public bool AddLaneLine(ILanePointData startPoint, ILanePointData endPoint, IStyleData style, out ILaneLineData line);
        public bool AddCrosswalk(ICrosswalkPointData startPoint, ICrosswalkPointData endPoint, IStyleData style, out ICrosswalkData crosswalk);
        public bool AddFiller(IEnumerable<IEntrancePointData> pointDatas, out IFillerData filler);
    }
    public interface ISegmentMarkingData
    {
        public ushort Id { get; }
        public int EntranceCount { get; }
        public IEnumerable<INodeEntranceData> Entrances { get; }

        public bool GetEntrance(ushort id, out INodeEntranceData entrance);

        public bool AddRegularLine(IEntrancePointData startPoint, IEntrancePointData endPoint, IStyleData style, out IRegularLineData line);
        public bool AddLaneLine(ILanePointData startPoint, ILanePointData endPoint, IStyleData style, out ILaneLineData line);
        public bool AddFiller(IEnumerable<IEntrancePointData> pointDatas, out IFillerData filler);
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

    }
    public interface IRegularLineData : ILineData
    {

    }
    public interface IStopLineData : ILineData
    {

    }
    public interface INormalLineData : ILineData
    {

    }
    public interface ICrosswalkLineData : ILineData
    {
        ICrosswalkData Crosswalk { get; }
    }
    public interface ILaneLineData : ILineData
    {

    }

    public interface IStyleData
    {

    }

    public interface IRuleData
    {

    }
    public interface IFillerData
    {

    }
    public interface ICrosswalkData
    {
        public ICrosswalkLineData Line { get; }
    }
}