namespace NodeMarkup.API
{
	public interface IRegularLineData : ILineData
	{
		IEntrancePointData StartPoint { get; }
		IEntrancePointData EndPoint { get; }
	}
}