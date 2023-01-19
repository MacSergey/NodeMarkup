using NodeMarkup.Manager;

namespace NodeMarkup.API.Implementations
{
	public class LineRuleData : ILineRuleData
	{
		private MarkupLineRawRule _markupLineRawRule;

		public LineRuleData(MarkupLineRawRule markupLineRawRule)
		{
			_markupLineRawRule = markupLineRawRule;
		}
	}
}