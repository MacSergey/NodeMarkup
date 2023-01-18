using UnityEngine;

namespace NodeMarkup.API.Templates
{
	public class StopLineTemplate : IStopLineTemplate
	{
		public Color32 Color { get; set; }
		public float DashLength { get; set; }
		public float Elevation { get; set; }
		public float Offset { get; set; }
		public Color32 SecondColor { get; set; }
		public float SpaceLength { get; set; }
		public StopLineStyle Style { get; set; }
		public bool UseSecondColor { get; set; }
		public float Width { get; set; }
		public ISharkTeethStopLineTemplate SharkTeethTemplate { get; }
	}

	public class SharkTeethtopLineTemplate : ISharkTeethStopLineTemplate
	{
		public Color32 Color { get; set; }
		public float Space { get; }
		public float Height { get; }
		public float BaseWidth { get; }
	}
}
