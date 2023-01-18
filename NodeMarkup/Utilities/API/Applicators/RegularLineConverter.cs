using NodeMarkup.Manager;

using System;

namespace NodeMarkup.API.Applicators
{
	public static class RegularLineConverter
	{
		public static Manager.RegularLineStyle GetRegularLineStyle(IRegularLineTemplate info)
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
					if (info.SharkTeethTemplate == null)
					{
						throw new ArgumentNullException(nameof(IRegularLineTemplate.SharkTeethTemplate));
					}

					return new SharkTeethLineStyle(
						info.SharkTeethTemplate.Color,
						info.SharkTeethTemplate.BaseWidth,
						info.SharkTeethTemplate.Height,
						info.SharkTeethTemplate.Space,
						info.SharkTeethTemplate.Angle);

				case RegularLineStyle.ZigZag:
					if (info.ZigZagTemplate == null)
					{
						throw new ArgumentNullException(nameof(IRegularLineTemplate.ZigZagTemplate));
					}

					return new ZigZagLineStyle(
						info.ZigZagTemplate.Color,
						info.ZigZagTemplate.Width,
						info.ZigZagTemplate.Step,
						info.ZigZagTemplate.Offset,
						info.ZigZagTemplate.Side,
						info.ZigZagTemplate.StartFrom);

				case RegularLineStyle.Prop:
					if (info.PropTemplate == null)
					{
						throw new ArgumentNullException(nameof(IRegularLineTemplate.PropTemplate));
					}

					return new PropLineStyle(
						info.PropTemplate.Prop,
						info.PropTemplate.Probability,
						(PropLineStyle.ColorOptionEnum)info.PropTemplate.ColorOption,
						info.PropTemplate.Color,
						info.PropTemplate.Step,
						info.PropTemplate.Angle,
						info.PropTemplate.Tilt,
						info.PropTemplate.Slope,
						info.PropTemplate.Shift,
						info.PropTemplate.Scale,
						info.PropTemplate.Elevation,
						info.PropTemplate.OffsetBefore,
						info.PropTemplate.OffsetAfter,
						(Manager.DistributionType)(int)info.PropTemplate.Distribution);

				case RegularLineStyle.Tree:
					if (info.TreeTemplate == null)
					{
						throw new ArgumentNullException(nameof(IRegularLineTemplate.TreeTemplate));
					}

					return new TreeLineStyle(
						info.TreeTemplate.Tree,
						info.TreeTemplate.Probability,
						info.TreeTemplate.Step,
						info.TreeTemplate.Angle,
						info.TreeTemplate.Tilt,
						info.TreeTemplate.Slope,
						info.TreeTemplate.Shift,
						info.TreeTemplate.Scale,
						info.TreeTemplate.Elevation,
						info.TreeTemplate.OffsetBefore,
						info.TreeTemplate.OffsetAfter,
						(Manager.DistributionType)(int)info.TreeTemplate.Distribution);

				case RegularLineStyle.Text:
					if (info.TextTemplate == null)
					{
						throw new ArgumentNullException(nameof(IRegularLineTemplate.TextTemplate));
					}

					return new RegularLineStyleText(
						info.TextTemplate.Color,
						info.TextTemplate.Font,
						info.TextTemplate.Text,
						info.TextTemplate.Scale,
						info.TextTemplate.Angle,
						info.TextTemplate.Shift,
						direction: (RegularLineStyleText.TextDirection)(int)info.TextTemplate.Direction,
						info.TextTemplate.Spacing,
						alignment: (RegularLineStyleText.TextAlignment)(int)info.TextTemplate.Alignment);

				case RegularLineStyle.Network:
					if (info.NetworkTemplate == null)
					{
						throw new ArgumentNullException(nameof(IRegularLineTemplate.NetworkTemplate));
					}

					return new NetworkLineStyle(
						info.NetworkTemplate.Network,
						info.NetworkTemplate.Shift,
						info.NetworkTemplate.Elevation,
						info.NetworkTemplate.Scale,
						info.NetworkTemplate.OffsetBefore,
						info.NetworkTemplate.OffsetAfter,
						info.NetworkTemplate.RepeatDistance,
						info.NetworkTemplate.Invert);

				default:
					throw new NotImplementedException();
			}
		}
	}
}
