using System.Collections.Generic;

namespace NodeMarkup.API
{
	public interface INormalLineData : ILineData
	{
		IEnumerable<ILineRuleData> Rules { get; }
		IEntrancePointData StartPoint { get; }
		IPointData EndPoint { get; }

		ILineRuleData AddRule(IRegularLineTemplate line);
	}
}