namespace NodeMarkup.API
{
	public interface INormalLineData : ILineData
	{
		IEntrancePointData StartPoint { get; }
		INormalPointData EndPoint { get; }
	}
}