using NodeMarkup.API.Styles;
using NodeMarkup.Manager;

using System;

using CrosswalkStyle = NodeMarkup.API.Styles.CrosswalkStyle;

namespace NodeMarkup.API.Internal
{
	internal static class CrosswalkConverter
	{
		internal static Manager.CrosswalkStyle GetCrosswalkStyle(Crosswalk info)
		{
			switch (info.Style)
			{
				case CrosswalkStyle.Solid:
					return new SolidLineStyle(info.Color, info.Width);

				case CrosswalkStyle.Dashed:
					return new DashedLineStyle(info.Color, info.Width, info.DashLength, info.SpaceLength);

				case CrosswalkStyle.DoubleSolid:
					return new DoubleSolidLineStyle(info.Color, info.SecondColor, info.UseSecondColor, info.Width, info.Offset);

				case CrosswalkStyle.DoubleDashed:
					return new DoubleDashedLineStyle(info.Color, info.SecondColor, info.UseSecondColor, info.Width, info.DashLength, info.SpaceLength, info.Offset);

				case CrosswalkStyle.SolidAndDashed:
					return new SolidAndDashedLineStyle(info.Color, info.SecondColor, info.UseSecondColor, info.Width, info.DashLength, info.SpaceLength, info.Offset);

				case CrosswalkStyle.DoubleDashedAsym:
					return new DoubleDashedAsymLineStyle(info.Color, info.SecondColor, info.UseSecondColor, info.Width, info.DashLength, info.AsymDashLength, info.SpaceLength, info.Offset);

				case CrosswalkStyle.Pavement:
					return new PavementLineStyle(info.Width, info.Elevation);

				case CrosswalkStyle.SharkTeeth:
					if (info.SharkTeethInfo == null)
						throw new ArgumentNullException(nameof(Crosswalk.SharkTeethInfo));

					return info.SharkTeethInfo.LineStyle(info.Color);

				case CrosswalkStyle.ZigZag:
					if (info.ZigZagInfo == null)
						throw new ArgumentNullException(nameof(Crosswalk.ZigZagInfo));

					return info.ZigZagInfo.LineStyle(info.Color);

				case CrosswalkStyle.Prop:
					if (info.PropInfo == null)
						throw new ArgumentNullException(nameof(Crosswalk.PropInfo));

					return info.PropInfo.LineStyle();

				case CrosswalkStyle.Tree:
					if (info.TreeInfo == null)
						throw new ArgumentNullException(nameof(Crosswalk.TreeInfo));

					return info.TreeInfo.LineStyle();

				case CrosswalkStyle.Text:
					if (info.TextInfo == null)
						throw new ArgumentNullException(nameof(Crosswalk.TextInfo));

					return info.TextInfo.LineStyle(info.Color);

				case CrosswalkStyle.Network:
					if (info.NetworkInfo == null)
						throw new ArgumentNullException(nameof(Crosswalk.NetworkInfo));

					return info.NetworkInfo.LineStyle();

				default:
					throw new NotImplementedException();
			}
		}
	}
}
