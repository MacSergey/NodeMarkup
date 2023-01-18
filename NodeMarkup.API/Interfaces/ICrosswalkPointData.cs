namespace NodeMarkup.API
{
	public interface ICrosswalkPointData : IPointData
	{
		IEntrancePointData SourcePoint { get; }
	}
}