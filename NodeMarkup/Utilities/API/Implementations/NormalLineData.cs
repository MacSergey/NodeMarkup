using NodeMarkup.API.Applicators;
using NodeMarkup.Manager;

using System.Collections.Generic;
using System.Linq;

namespace NodeMarkup.API.Implementations
{
	public class NormalLineData : INormalLineData
	{
		private readonly MarkupRegularLine _generatedLine;

		public NormalLineData(MarkupRegularLine generatedLine, IEntrancePointData startPointData, IPointData endPointData)
		{
			_generatedLine = generatedLine;

			StartPoint = startPointData;
			EndPoint = endPointData;
		}

		public ulong Id => _generatedLine.Id;
		public IEntrancePointData StartPoint { get; }
		public IPointData EndPoint { get; }
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
