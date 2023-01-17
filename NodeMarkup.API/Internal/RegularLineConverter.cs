using NodeMarkup.API.Styles;
using NodeMarkup.Manager;

using System;

using RegularLineStyle = NodeMarkup.API.Styles.RegularLineStyle;

namespace NodeMarkup.API.Internal
{
	internal static class RegularLineConverter
	{
		internal static Manager.RegularLineStyle GetRegularLineStyle(RegularLine info)
		{
			switch (info.Style)
			{
				case RegularLineStyle.Solid:
					return new SolidLineStyle(info.Color, info.Width);

				case RegularLineStyle.Dashed:
					return new DashedLineStyle(info.Color, info.Width, info.DashLength, info.SpaceLength);

				case RegularLineStyle.DoubleSolid:
					return new DoubleSolidLineStyle(info.Color, info.SecondColor, info.UseSecondColor, info.Width, info.Offset);

				case RegularLineStyle.DoubleDashed:
					return new DoubleDashedLineStyle(info.Color, info.SecondColor, info.UseSecondColor, info.Width, info.DashLength, info.SpaceLength, info.Offset);

				case RegularLineStyle.SolidAndDashed:
					return new SolidAndDashedLineStyle(info.Color, info.SecondColor, info.UseSecondColor, info.Width, info.DashLength, info.SpaceLength, info.Offset);

				case RegularLineStyle.DoubleDashedAsym:
					return new DoubleDashedAsymLineStyle(info.Color, info.SecondColor, info.UseSecondColor, info.Width, info.DashLength, info.AsymDashLength, info.SpaceLength, info.Offset);

				case RegularLineStyle.Pavement:
					return new PavementLineStyle(info.Width, info.Elevation);

				case RegularLineStyle.SharkTeeth:
					if (info.SharkTeethInfo == null)
						throw new ArgumentNullException(nameof(RegularLine.SharkTeethInfo));

					return info.SharkTeethInfo.LineStyle(info.Color);

				case RegularLineStyle.ZigZag:
					if (info.ZigZagInfo == null)
						throw new ArgumentNullException(nameof(RegularLine.ZigZagInfo));

					return info.ZigZagInfo.LineStyle(info.Color);

				case RegularLineStyle.Prop:
					if (info.PropInfo == null)
						throw new ArgumentNullException(nameof(RegularLine.PropInfo));

					return info.PropInfo.LineStyle();

				case RegularLineStyle.Tree:
					if (info.TreeInfo == null)
						throw new ArgumentNullException(nameof(RegularLine.TreeInfo));

					return info.TreeInfo.LineStyle();

				case RegularLineStyle.Text:
					if (info.TextInfo == null)
						throw new ArgumentNullException(nameof(RegularLine.TextInfo));

					return info.TextInfo.LineStyle(info.Color);

				case RegularLineStyle.Network:
					if (info.NetworkInfo == null)
						throw new ArgumentNullException(nameof(RegularLine.NetworkInfo));

					return info.NetworkInfo.LineStyle();

				default:
					throw new NotImplementedException();
			}
		}
	}
}
