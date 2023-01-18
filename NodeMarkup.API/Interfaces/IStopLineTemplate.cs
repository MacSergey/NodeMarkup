using UnityEngine;

namespace NodeMarkup.API
{
	public interface IStopLineTemplate
	{
		Color32 Color { get; }
		float DashLength { get; }
		float Elevation { get; }
		float Offset { get; }
		Color32 SecondColor { get; }
		float SpaceLength { get; }
		StopLineStyle Style { get; }
		bool UseSecondColor { get; }
		float Width { get; }
		public ISharkTeethStopLineTemplate SharkTeethTemplate { get; }
	}

	public interface ISharkTeethStopLineTemplate
	{
		public float Space { get; }
		public float Height { get; }
		public float BaseWidth { get; }
		Color32 Color { get; }
	}
}
