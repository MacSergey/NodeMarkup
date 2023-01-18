namespace NodeMarkup.API
{
	public interface IEntrancePointData : IPointData
	{
		IPointSourceData Source { get; }
		float Offset { get; set; }
	}
}