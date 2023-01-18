namespace NodeMarkup.API
{
	public interface ILaneLineData : ILineData
	{
		ILanePointData StartPoint { get; }
		ILanePointData EndPoint { get; }
	}
}