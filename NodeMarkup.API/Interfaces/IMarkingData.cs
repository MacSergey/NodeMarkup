using System.Collections.Generic;

namespace NodeMarkup.API
{
	public interface IMarkingData
	{
		int EntranceCount { get; }
		ushort Id { get; }

		ICrosswalkData AddCrosswalk(ICrosswalkPointData startPointData, ICrosswalkPointData endPointData, ICrosswalkTemplate crosswalk);
		IFillerData AddFiller(IEnumerable<IEntrancePointData> pointDatas, IFillerTemplate filler);
		ILaneLineData AddLaneLine(ILanePointData startPointData, ILanePointData endPointData, IRegularLineTemplate line);
		IRegularLineData AddNormalLine(IEntrancePointData startPointData, IRegularLineTemplate line);
		IRegularLineData AddRegularLine(IEntrancePointData startPointData, IEntrancePointData endPointData, IRegularLineTemplate line);
		IStopLineData AddStopLine(IEntrancePointData startPointData, IEntrancePointData endPointData, IStopLineTemplate line);
		void ClearMarkings();
		void ResetPointOffsets();
	}
}