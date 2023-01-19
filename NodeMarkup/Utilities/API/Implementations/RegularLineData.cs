using NodeMarkup.API.Applicators;
using NodeMarkup.Manager;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NodeMarkup.API.Implementations
{
	public class RegularLineData : IRegularLineData
	{
		private readonly MarkupRegularLine _generatedLine;

		public RegularLineData(MarkupRegularLine generatedLine, IEntrancePointData startPointData, IEntrancePointData endPointData, IMarkingApi marking)
		{
			_generatedLine = generatedLine;

			StartPoint = startPointData;
			EndPoint = endPointData;
			Marking = marking;
		}

		public ulong Id => _generatedLine.Id;
		public IMarkingApi Marking { get; }
		public IEntrancePointData StartPoint { get; }
		public IEntrancePointData EndPoint { get; }
		public IEnumerable<ILineRuleData> Rules => _generatedLine.Rules.Select(x => (ILineRuleData)new LineRuleData(x));

		public ILineRuleData AddRule(IRegularLineTemplate line)
		{
			var style = RegularLineConverter.GetRegularLineStyle(line);

			if (style != null)
			{
				_generatedLine.AddRule(style, false);

				return new LineRuleData(_generatedLine.Rules.Last());
			}

			return null;
		}

		public void Remove()
		{
			_generatedLine.Markup.RemoveLine(_generatedLine);
		}
	}
}
