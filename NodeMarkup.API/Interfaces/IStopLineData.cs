namespace NodeMarkup.API
{
	public interface IStopLineData : ILineData
	{
		IEntrancePointData StartPoint { get; }
		IEntrancePointData EndPoint { get; }
	}
}