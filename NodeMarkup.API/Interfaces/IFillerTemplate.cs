
using UnityEngine;

namespace NodeMarkup.API
{
	public interface IFillerTemplate
	{
		float Angle { get; }
		Color32 Color { get; }
		float CornerRadius { get; }
		float CurbSize { get; }
		float Elevation { get; }
		IFillerGuides Guides { get; }
		float LineOffset { get; }
		float MedianCornerRadius { get; }
		float MedianCurbSize { get; }
		float MedianOffset { get; }
		float Step { get; }
		FillerStyle Style { get; }
		float Width { get; }
	}

	public interface IFillerGuides
	{
		public int LeftGuideA { get; }
		public int LeftGuideB { get; }
		public int RightGuideA { get; }
		public int RightGuideB { get; }
	}
}
