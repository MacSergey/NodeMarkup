namespace NodeMarkup.API
{
	public interface ICrosswalkData
	{
		public ushort MarkingId { get; }
		public ICrosswalkLineData Line { get; }
	}
}