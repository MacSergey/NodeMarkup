namespace NodeMarkup.API
{
	public interface IPointSourceData
	{
		uint LeftLaneId { get; }
		int LeftIndex { get; }
		uint RightLaneId { get; }
		int RightIndex { get; }
		PointLocation Location { get; }
	}
}