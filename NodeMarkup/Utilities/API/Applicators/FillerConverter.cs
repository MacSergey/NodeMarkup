using NodeMarkup.Manager;

using System;

namespace NodeMarkup.API.Applicators
{
	public static class FillerConverter
	{
		public static Manager.FillerStyle GetFillerStyle(IFillerTemplate info)
		{
			switch (info.Style)
			{
				case FillerStyle.Stripe:
					return new StripeFillerStyle(info.Color, info.Width, info.LineOffset, info.MedianOffset, info.Angle, info.Step, info.Guides != null);

				case FillerStyle.Grid:
					return new GridFillerStyle(info.Color, info.Width, info.Angle, info.Step, info.LineOffset, info.MedianOffset);

				case FillerStyle.Solid:
					return new SolidFillerStyle(info.Color, info.LineOffset, info.MedianOffset, info.Guides != null);

				case FillerStyle.Chevron:
					return new ChevronFillerStyle(info.Color, info.Width, info.LineOffset, info.MedianOffset, info.Angle, info.Step);

				case FillerStyle.Pavement:
					return new PavementFillerStyle(info.Color, info.Width, info.LineOffset, info.MedianOffset, info.Elevation, info.CornerRadius, info.MedianCornerRadius);

				case FillerStyle.Grass:
					return new GrassFillerStyle(info.Color, info.Width, info.LineOffset, info.MedianOffset, info.Elevation, info.CornerRadius, info.MedianCornerRadius, info.CurbSize, info.MedianCurbSize);

				case FillerStyle.Gravel:
					return new GravelFillerStyle(info.Color, info.Width, info.LineOffset, info.MedianOffset, info.Elevation, info.CornerRadius, info.MedianCornerRadius, info.CurbSize, info.MedianCurbSize);

				case FillerStyle.Ruined:
					return new RuinedFillerStyle(info.Color, info.Width, info.LineOffset, info.MedianOffset, info.Elevation, info.CornerRadius, info.MedianCornerRadius, info.CurbSize, info.MedianCurbSize);

				case FillerStyle.Cliff:
					return new CliffFillerStyle(info.Color, info.Width, info.LineOffset, info.MedianOffset, info.Elevation, info.CornerRadius, info.MedianCornerRadius, info.CurbSize, info.MedianCurbSize);

				default:
					throw new NotImplementedException();
			}
		}
	}
}
