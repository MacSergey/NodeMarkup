using System.Collections.Generic;

namespace NodeMarkup.API
{
	public interface IRegularLineData : ILineData
	{
		IEnumerable<ILineRuleData> Rules { get; }
		IEntrancePointData StartPoint { get; }
		IEntrancePointData EndPoint { get; }

		ILineRuleData AddRule(IRegularLineTemplate line);
	}
}