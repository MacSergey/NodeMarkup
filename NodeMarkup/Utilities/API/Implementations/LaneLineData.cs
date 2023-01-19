using NodeMarkup.API.Applicators;
using NodeMarkup.Manager;

using System.Collections.Generic;
using System.Linq;

namespace NodeMarkup.API.Implementations
{
	public class LaneLineData : ILaneLineData
	{
		private readonly MarkupLaneLine _generatedLine;

		public LaneLineData(MarkupLaneLine generatedLine, ILanePointData startPointData, ILanePointData endPointData, IMarkingApi marking)
		{
			_generatedLine = generatedLine;
			StartPoint = startPointData;
			EndPoint = endPointData;
			Marking = marking;
		}

		public ulong Id => _generatedLine.Id;
		public ILanePointData StartPoint { get; }
		public ILanePointData EndPoint { get; }
		public IEnumerable<ILineRuleData> Rules => _generatedLine.Rules.Select(x => (ILineRuleData)new LineRuleData(x));
		public IMarkingApi Marking { get; }

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
