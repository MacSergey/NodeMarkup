using NodeMarkup.Manager;

using System;

namespace NodeMarkup.API.Applicators
{
	public static class StopLineConverter
	{
		public static Manager.StopLineStyle GetStopLineStyle(IStopLineTemplate info)
		{
			switch (info.Style)
			{
				case StopLineStyle.Solid:
					return new SolidStopLineStyle(info.Color, info.Width);

				case StopLineStyle.Dashed:
					return new DashedStopLineStyle(info.Color, info.Width, info.DashLength, info.SpaceLength);

				case StopLineStyle.DoubleSolid:
					return new DoubleSolidStopLineStyle(info.Color, info.SecondColor, info.UseSecondColor, info.Width, info.Offset);

				case StopLineStyle.DoubleDashed:
					return new DoubleDashedStopLineStyle(info.Color, info.SecondColor, info.UseSecondColor, info.Width, info.DashLength, info.SpaceLength, info.Offset);

				case StopLineStyle.SolidAndDashed:
					return new SolidAndDashedStopLineStyle(info.Color, info.SecondColor, info.UseSecondColor, info.Width, info.DashLength, info.SpaceLength, info.Offset);

				case StopLineStyle.Pavement:
					return new PavementStopLineStyle(info.Width, info.Elevation);

				case StopLineStyle.SharkTeeth:
					if (info.SharkTeethTemplate == null)
						throw new ArgumentNullException(nameof(IStopLineTemplate.SharkTeethTemplate));

					return new SharkTeethStopLineStyle(
						info.SharkTeethTemplate.Color,
						info.SharkTeethTemplate.BaseWidth,
						info.SharkTeethTemplate.Height,
						info.SharkTeethTemplate.Space);

				default:
					throw new NotImplementedException();
			}
		}
	}
}
