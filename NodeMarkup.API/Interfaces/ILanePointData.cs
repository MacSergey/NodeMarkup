namespace NodeMarkup.API
{
	public interface ILanePointData : IPointData
	{
		IEntrancePointData SourcePointA { get; }
		IEntrancePointData SourcePointB { get; }
	}
}