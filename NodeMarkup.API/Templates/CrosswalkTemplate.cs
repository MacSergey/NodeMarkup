using UnityEngine;

namespace NodeMarkup.API.Templates
{
	public class CrosswalkTemplate : ICrosswalkTemplate
	{
		public int ChessBoardLineCount { get; set; }
		public Color32 Color { get; set; }
		public float DashLength { get; set; }
		public float GapLength { get; set; }
		public int GapPeriod { get; set; }
		public bool Invert { get; set; }
		public float LineWidth { get; set; }
		public float Offset { get; set; }
		public float OffsetAfter { get; set; }
		public float OffsetBefore { get; set; }
		public bool Parallel { get; set; }
		public Color32 SecondColor { get; set; }
		public float SpaceLength { get; set; }
		public float SquareChessBoardSide { get; set; }
		public CrosswalkStyle Style { get; set; }
		public bool UseGap { get; set; }
		public bool UseSecondColor { get; set; }
		public float Width { get; set; }
	}
}
