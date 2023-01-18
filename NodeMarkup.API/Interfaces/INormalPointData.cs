namespace NodeMarkup.API
{
	public interface INormalPointData : IPointData
	{
		IEntrancePointData SourcePoint { get; }
	}
}