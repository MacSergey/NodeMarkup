using NodeMarkup.Manager;

using System;

namespace NodeMarkup.API.Applicators
{
	public static class CrosswalkConverter
	{
		public static Manager.CrosswalkStyle GetCrosswalkStyle(ICrosswalkTemplate info)
		{
			switch (info.Style)
			{
				case CrosswalkStyle.Solid:
					return new SolidCrosswalkStyle(info.Color, info.Width, info.OffsetBefore, info.OffsetAfter);

				case CrosswalkStyle.Zebra:
					return new ZebraCrosswalkStyle(info.Color, info.SecondColor, info.UseSecondColor, info.Width, info.OffsetBefore, info.OffsetAfter, info.DashLength, info.SpaceLength, info.UseGap, info.GapLength, info.GapPeriod, info.Parallel);

				case CrosswalkStyle.DoubleZebra:
					return new DoubleZebraCrosswalkStyle(info.Color, info.SecondColor, info.UseSecondColor, info.Width, info.OffsetBefore, info.OffsetAfter, info.DashLength, info.SpaceLength, info.UseGap, info.GapLength, info.GapPeriod, info.Parallel, info.Offset);

				case CrosswalkStyle.ParallelDashedLines:
					return new ParallelDashedLinesCrosswalkStyle(info.Color, info.Width, info.OffsetBefore, info.OffsetAfter, info.LineWidth, info.DashLength, info.SpaceLength);

				case CrosswalkStyle.ParallelSolidLines:
					return new ParallelSolidLinesCrosswalkStyle(info.Color, info.Width, info.OffsetBefore, info.OffsetAfter, info.LineWidth);

				case CrosswalkStyle.ChessBoard:
					return new ChessBoardCrosswalkStyle(info.Color, info.OffsetBefore, info.OffsetAfter, info.SquareChessBoardSide, info.ChessBoardLineCount, info.Invert);

				case CrosswalkStyle.Existent:
					return new ExistCrosswalkStyle(info.Width);

				case CrosswalkStyle.Ladder:
					return new LadderCrosswalkStyle(info.Color, info.Width, info.OffsetBefore, info.OffsetAfter, info.DashLength, info.SpaceLength, info.LineWidth);

				default:
					throw new NotImplementedException();
			}
		}
	}
}
