namespace NodeMarkup.API
{
	public interface ICrosswalkLineData : ILineData
	{
		ICrosswalkData Crosswalk { get; }
		ICrosswalkPointData StartPoint { get; }
		ICrosswalkPointData EndPoint { get; }
	}
}