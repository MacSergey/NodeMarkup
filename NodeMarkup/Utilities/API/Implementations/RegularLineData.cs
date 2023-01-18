using NodeMarkup.API.Applicators;
using NodeMarkup.Manager;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.API.Implementations
{
	public class RegularLineData
	{
		private readonly MarkupRegularLine _generatedLine;
		private readonly IMarkingApi _api;

		public RegularLineData(MarkupRegularLine generatedLine, IEntrancePointData startPointData, IEntrancePointData endPointData, IMarkingApi api)
		{
			_generatedLine = generatedLine;
			_api = api;

			StartPoint = startPointData;
			EndPoint = endPointData;
		}

		public IEntrancePointData StartPoint { get; }
		public IEntrancePointData EndPoint { get; }
		public IEnumerable<LineRuleData> Rules => _generatedLine.Rules.Select(x => new LineRuleData(x));

		public LineRuleData AddRule(IRegularLineTemplate line)
		{
			var style = RegularLineConverter.GetRegularLineStyle(line);

			if (style != null)
			{ 
				_generatedLine.AddRule(style, false);

				return new LineRuleData(_generatedLine.Rules.Last());
			}

			return null;
		}
	}
}
