namespace NodeMarkup.API
{
	public interface ICrosswalkLineData : ILineData
	{
		ICrosswalkPointData StartPoint { get; }
		ICrosswalkPointData EndPoint { get; }
	}
}