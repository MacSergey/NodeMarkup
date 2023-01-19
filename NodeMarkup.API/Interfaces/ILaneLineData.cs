using System.Collections.Generic;

namespace NodeMarkup.API
{
	public interface ILaneLineData : ILineData
	{
		ILanePointData EndPoint { get; }
		IEnumerable<ILineRuleData> Rules { get; }
		ILanePointData StartPoint { get; }

		ILineRuleData AddRule(IRegularLineTemplate line);
	}
}