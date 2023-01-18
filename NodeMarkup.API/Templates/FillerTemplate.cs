using UnityEngine;

namespace NodeMarkup.API.Templates
{
	public class FillerTemplate : IFillerTemplate
	{
		public float Angle { get; set; }
		public Color32 Color { get; set; }
		public float CornerRadius { get; set; }
		public float CurbSize { get; set; }
		public float Elevation { get; set; }
		public IFillerGuides Guides { get; set; }
		public float LineOffset { get; set; }
		public float MedianCornerRadius { get; set; }
		public float MedianCurbSize { get; set; }
		public float MedianOffset { get; set; }
		public float Step { get; set; }
		public FillerStyle Style { get; set; }
		public float Width { get; set; }
	}
}
