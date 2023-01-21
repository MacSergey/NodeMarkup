namespace NodeMarkup.API
{
	public interface IPointData
	{
		byte Index { get; }
		ushort EntranceId { get; }
		ushort MarkingId { get; }
		IEntranceData Entrance { get; }
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