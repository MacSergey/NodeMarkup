using System.Collections.Generic;

namespace NodeMarkup.API
{
	public interface IMarkingApi
	{
		int EntranceCount { get; }
		ushort Id { get; }

		ICrosswalkLineData AddCrosswalk(ICrosswalkPointData startPointData, ICrosswalkPointData endPointData, ICrosswalkTemplate crosswalk);
		IFillerData AddFiller(IEnumerable<IEntrancePointData> pointDatas, IFillerTemplate filler);
		ILaneLineData AddLaneLine(ILanePointData startPointData, ILanePointData endPointData, IRegularLineTemplate line);
		INormalLineData AddNormalLine(IEntrancePointData startPointData, IRegularLineTemplate line);
		IRegularLineData AddRegularLine(IEntrancePointData startPointData, IEntrancePointData endPointData, IRegularLineTemplate line);
		IStopLineData AddStopLine(IEntrancePointData startPointData, IEntrancePointData endPointData, IStopLineTemplate line);
		void ClearMarkings();
		void RecalculateDrawData();
		void ResetPointOffsets();
		bool CrosswalkExist(ICrosswalkPointData startPointData, ICrosswalkPointData endPointData);
		bool LaneLineExist(ILanePointData startPointData, ILanePointData endPointData);
		bool NormalLineExist(IEntrancePointData startPointData);
		bool RegularLineExist(IEntrancePointData startPointData, IEntrancePointData endPointData);
		bool RemoveCrosswalk(ICrosswalkPointData startPointData, ICrosswalkPointData endPointData);
		bool RemoveFiller(IFillerData fillerData);
		bool RemoveLaneLine(ILanePointData startPointData, ILanePointData endPointData);
		bool RemoveNormalLine(IEntrancePointData startPointData);
		bool RemoveRegularLine(IEntrancePointData startPointData, IEntrancePointData endPointData);
		bool RemoveStopLine(IEntrancePointData startPointData, IEntrancePointData endPointData);
		bool StopLineExist(IEntrancePointData startPointData, IEntrancePointData endPointData);
		bool TryGetCrosswalk(ICrosswalkPointData startPointData, ICrosswalkPointData endPointData, out ICrosswalkLineData crosswalk);
		bool TryGetLaneLine(ILanePointData startPointData, ILanePointData endPointData, out ILaneLineData laneLine);
		bool TryGetNormalLine(IEntrancePointData startPointData, out INormalLineData regularLine);
		bool TryGetRegularLine(IEntrancePointData startPointData, IEntrancePointData endPointData, out IRegularLineData regularLine);
		bool TryGetStopLine(IEntrancePointData startPointData, IEntrancePointData endPointData, out IStopLineData stopLine);
	}
}