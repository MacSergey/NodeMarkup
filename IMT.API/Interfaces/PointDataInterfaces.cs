namespace NodeMarkup.API
{
	public interface IPointData
	{
        IDataProviderV1 DataProvider { get; }
        IMarkingData Marking { get; }
        IEntranceData Entrance { get; }
        ushort MarkingId { get; }
        ushort EntranceId { get; }
        byte Index { get; }
    }
    public interface IEntrancePointData : IPointData
    {
        IPointSourceData Source { get; }
        float Offset { get; set; }
        float Position { get; }
    }
    public interface INormalPointData : IPointData
    {
        IEntrancePointData SourcePoint { get; }
    }
    public interface ICrosswalkPointData : IPointData
    {
        IEntrancePointData SourcePoint { get; }
    }
    public interface ILanePointData : IPointData
    {
        IEntrancePointData SourcePointA { get; }
        IEntrancePointData SourcePointB { get; }
    }
    public interface IPointSourceData
    {
        uint LeftLaneId { get; }
        int LeftIndex { get; }
        uint RightLaneId { get; }
        int RightIndex { get; }
        PointLocation Location { get; }
    }
}