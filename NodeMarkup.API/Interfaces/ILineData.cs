namespace NodeMarkup.API
{
	public interface ILineData
	{
		IMarkingApi Marking { get; }
		ulong Id { get; }

		void Remove();
	}
}