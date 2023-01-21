using System.Collections.Generic;

namespace NodeMarkup.API
{
	public interface IFillerData
	{
        IDataProviderV1 DataProvider { get; }
        IMarkingData Marking { get; }
        ushort MarkingId { get; }
        int Id { get; }
        IEnumerable<IEntrancePointData> PointDatas { get; }

		bool Remove();
	}

    public interface ICrosswalkData
    {
        IDataProviderV1 DataProvider { get; }
        IMarkingData Marking { get; }
        ushort MarkingId { get; }
        ICrosswalkLineData Line { get; }

        bool Remove();
    }
}