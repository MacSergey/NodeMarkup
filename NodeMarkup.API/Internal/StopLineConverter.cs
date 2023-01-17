using NodeMarkup.API.Styles;
using NodeMarkup.Manager;

using System;

using StopLineStyle = NodeMarkup.API.Styles.StopLineStyle;

namespace NodeMarkup.API.Internal
{
	internal static class StopLineConverter
	{
		internal static Manager.StopLineStyle GetStopLineStyle(StopLine info)
		{
			switch (info.Style)
			{
				case StopLineStyle.Solid:
					return new SolidLineStyle(info.Color, info.Width);

				case StopLineStyle.Dashed:
					return new DashedLineStyle(info.Color, info.Width, info.DashLength, info.SpaceLength);

				case StopLineStyle.DoubleSolid:
					return new DoubleSolidLineStyle(info.Color, info.SecondColor, info.UseSecondColor, info.Width, info.Offset);

				case StopLineStyle.DoubleDashed:
					return new DoubleDashedLineStyle(info.Color, info.SecondColor, info.UseSecondColor, info.Width, info.DashLength, info.SpaceLength, info.Offset);

				case StopLineStyle.SolidAndDashed:
					return new SolidAndDashedLineStyle(info.Color, info.SecondColor, info.UseSecondColor, info.Width, info.DashLength, info.SpaceLength, info.Offset);

				case StopLineStyle.DoubleDashedAsym:
					return new DoubleDashedAsymLineStyle(info.Color, info.SecondColor, info.UseSecondColor, info.Width, info.DashLength, info.AsymDashLength, info.SpaceLength, info.Offset);

				case StopLineStyle.Pavement:
					return new PavementLineStyle(info.Width, info.Elevation);

				case StopLineStyle.SharkTeeth:
					if (info.SharkTeethInfo == null)
						throw new ArgumentNullException(nameof(StopLine.SharkTeethInfo));

					return info.SharkTeethInfo.LineStyle(info.Color);

				case StopLineStyle.ZigZag:
					if (info.ZigZagInfo == null)
						throw new ArgumentNullException(nameof(StopLine.ZigZagInfo));

					return info.ZigZagInfo.LineStyle(info.Color);

				case StopLineStyle.Prop:
					if (info.PropInfo == null)
						throw new ArgumentNullException(nameof(StopLine.PropInfo));

					return info.PropInfo.LineStyle();

				case StopLineStyle.Tree:
					if (info.TreeInfo == null)
						throw new ArgumentNullException(nameof(StopLine.TreeInfo));

					return info.TreeInfo.LineStyle();

				case StopLineStyle.Text:
					if (info.TextInfo == null)
						throw new ArgumentNullException(nameof(StopLine.TextInfo));

					return info.TextInfo.LineStyle(info.Color);

				case StopLineStyle.Network:
					if (info.NetworkInfo == null)
						throw new ArgumentNullException(nameof(StopLine.NetworkInfo));

					return info.NetworkInfo.LineStyle();

				default:
					throw new NotImplementedException();
			}
		}
	}
}
