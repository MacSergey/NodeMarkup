
using UnityEngine;

namespace NodeMarkup.API
{
	public interface ICrosswalkTemplate
	{
		int ChessBoardLineCount { get; }
		Color32 Color { get; }
		float DashLength { get; }
		float GapLength { get; }
		int GapPeriod { get; }
		bool Invert { get; }
		float LineWidth { get; }
		float Offset { get; }
		float OffsetAfter { get; }
		float OffsetBefore { get; }
		bool Parallel { get; }
		Color32 SecondColor { get; }
		float SpaceLength { get; }
		float SquareChessBoardSide { get; }
		CrosswalkStyle Style { get; }
		bool UseGap { get; }
		bool UseSecondColor { get; }
		float Width { get; }
	}
}
