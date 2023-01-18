namespace NodeMarkup.API
{
	public interface IPointData
	{
		byte Index { get; }
		ushort EntranceId { get; }
		ushort MarkingId { get; }
		IEntranceData Entrance { get; }
	}
}