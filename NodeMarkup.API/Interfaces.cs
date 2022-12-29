using System.Collections.Generic;

namespace NodeMarkup.API
{
    public interface IIMTAPIFactory
    {
        public IDataProviderV1 GetInstance();
    }
    public interface IDataProviderV1
    {
        public IEnumerable<INodeMarkingData> NodeMarkings { get; }
        public IEnumerable<ISegmentMarkingData> SegmentMarkings { get; }

        public bool IsNodeMarkingExist { get; }
        public bool IsSegmentMarkingExist { get;}

        public INodeMarkingData GetNodeMarking(ushort id);
        public ISegmentMarkingData GetSegmentMarking(ushort id);

        public bool CreateNodeMarking(ushort id, out INodeMarkingData nodeMarking);
        public bool CreateSegmentMarking(ushort id, out ISegmentMarkingData segmentMarking);
    }

    public interface INodeMarkingData
    {
        public ushort Id { get; }
        public int EntranceCount { get; }

        public IEnumerable<IEntranceData> Entrances { get; }
    }
    public interface ISegmentMarkingData
    {
        public ushort Id { get; }
        public int EntranceCount { get; }
        public IEnumerable<IEntranceData> Entrances { get; }
    }
    public interface IEntranceData
    {
        public ushort Id { get; }
        public int PointCount { get; }
        public IEnumerable<IPointData> Points { get; }
    }
    public interface IPointData
    {

    }
}