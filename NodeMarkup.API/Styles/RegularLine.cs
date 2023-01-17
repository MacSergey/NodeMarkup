using ModsCommon.Utilities;
using NodeMarkup.Manager;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;

using static NodeMarkup.Manager.Style;

namespace NodeMarkup.API.Styles
{
	public enum RegularLineStyle
	{
		Solid,
		Dashed,
		DoubleSolid,
		DoubleDashed,
		SolidAndDashed,
		SharkTeeth,
		DoubleDashedAsym,
		ZigZag,
		Pavement,
		Prop,
		Tree,
		Text,
		Network
	}

	public class RegularLine : IRegularLine
	{
		private readonly MarkupRegularLine _generatedLine;

		public RegularLine(MarkupRegularLine generatedLine)
		{
			_generatedLine = generatedLine;
		}

		public RegularLine(RegularLineStyle style)
		{
			Style = style;
		}

		public RegularLineStyle Style { get; }
		public Color32 Color { get; set; }
		public float Width { get; set; } = 0.15F;
		public Alignment Alignment { get; set; }
		public float SpaceLength { get; set; } = 0.5F;
		public float DashLength { get; set; } = 0.5F;
		public float Offset { get; set; }
		public bool UseSecondColor { get; set; }
		public Color32 SecondColor { get; set; }
		public float Angle { get; set; }
		public float SharkTeethSpace { get; set; }
		public float SharkTeethHeight { get; set; }
		public float BaseSharkTeethWidth { get; set; }
		public float AsymDashLength { get; set; }
		public float Elevation { get; set; }
		public PropLineInfo PropInfo { get; set; }
		public TreeLineInfo TreeInfo { get; set; }
		public TextLineInfo TextInfo { get; internal set; }
		public NetworkLineInfo NetworkInfo { get; internal set; }
		public SharkTeethLineInfo SharkTeethInfo { get; internal set; }
		public ZigZagLineInfo ZigZagInfo { get; internal set; }
	}

	public interface IRegularLine
	{
		public RegularLineStyle Style { get; }
		public Color32 Color { get; }
		public float Width { get; }
		public Alignment Alignment { get; }
		public float SpaceLength { get; }
		public float DashLength { get; }
		public float Offset { get; }
		public bool UseSecondColor { get; }
		public Color32 SecondColor { get; }
		public float Angle { get; }
		public float SharkTeethSpace { get; }
		public float SharkTeethHeight { get; }
		public float BaseSharkTeethWidth { get; }
		public float AsymDashLength { get; }
		public float Elevation { get; }
	}

	public class SharkTeethLineInfo
	{
		public float Angle { get; set; }
		public float SharkTeethSpace { get; set; }
		public float SharkTeethHeight { get; set; }
		public float BaseSharkTeethWidth { get; set; }

		internal SharkTeethLineStyle LineStyle(Color32 color)
			=> new SharkTeethLineStyle(color, BaseSharkTeethWidth, SharkTeethHeight, SharkTeethSpace, Angle);
	}

	public class ZigZagLineInfo
	{
		public float Width { get; set; }
		public float Step { get; set; }
		public float Offset { get; set; }
		public bool StartFrom { get; set; }
		public bool Side { get; set; }

		internal ZigZagLineStyle LineStyle(Color32 color)
			=> new ZigZagLineStyle(color, Width, Step, Offset, Side, StartFrom);
	}

	public class TextLineInfo
	{
		public string Text { get; set; }
		public string Font { get; set; }
		public float Scale { get; set; }
		public float Angle { get; set; }
		public float Shift { get; set; }
		public TextDirection Direction { get; set; }
		public Vector2 Spacing { get; set; }
		public TextAlignment Alignment { get; set; }

		internal RegularLineStyleText LineStyle(Color32 color) => new RegularLineStyleText(
			color: color,
			font: Font,
			text: Text,
			scale: Scale,
			angle: Angle,
			shift: Shift,
			direction: (RegularLineStyleText.TextDirection)(int)Direction,
			spacing: Spacing,
			alignment: (RegularLineStyleText.TextAlignment)(int)Alignment);
	}

	public class NetworkLineInfo
	{
		public NetInfo Network { get; set; }
		public int RepeatDistance { get; set; }
		public bool Invert { get; set; }
		public float Shift { get; set; }
		public float Elevation { get; set; }
		public float Scale { get; set; }
		public float OffsetBefore { get; set; }
		public float OffsetAfter { get; set; }

		internal PropLineStyle LineStyle() => new NetworkLineStyle(
			prefab: Network,
			shift: Shift,
			elevation: Elevation,
			scale: Scale,
			offsetBefore: OffsetBefore,
			offsetAfter: OffsetAfter,
			repeatDistance: RepeatDistance,
			invert: Invert);
	}

	public class PropLineInfo : PrefabLineInfo
	{
		public PropInfo Prop { get; set; }
		public int ColorOption { get; set; }
		public Color32 Color { get; set; }

		internal PropLineStyle LineStyle() => new PropLineStyle(
			prop: Prop,
			probability: Probability,
			colorOption: (PropLineStyle.ColorOptionEnum)(int)ColorOption,
			color: Color,
			step: Step,
			angle: Angle,
			tilt: Tilt,
			slope: Slope,
			shift: Shift,
			scale: Scale,
			elevation: Elevation,
			offsetBefore: OffsetBefore,
			offsetAfter: OffsetAfter,
			distribution: Distribution);
	}

	public class TreeLineInfo : PrefabLineInfo
	{
		public TreeInfo Tree { get; set; }

		internal PropLineStyle LineStyle() => new TreeLineStyle(
			tree: Tree,
			probability: Probability,
			step: Step,
			angle: Angle,
			tilt: Tilt,
			slope: Slope,
			shift: Shift,
			scale: Scale,
			elevation: Elevation,
			offsetBefore: OffsetBefore,
			offsetAfter: OffsetAfter,
			distribution: Distribution);
	}

	public class PrefabLineInfo
	{
		public int Probability { get; set; }
		public float? Step { get; set; }
		public Vector2 Angle { get; set; }
		public Vector2 Tilt { get; set; }
		public Vector2 Slope { get; set; }
		public Vector2 Shift { get; set; }
		public Vector2 Scale { get; set; }
		public Vector2 Elevation { get; set; }
		public float OffsetBefore { get; set; }
		public float OffsetAfter { get; set; }
		public Manager.DistributionType Distribution { get; set; }
	}
}
