using UnityEngine;

namespace NodeMarkup.API.Templates
{
	public class RegularLineTemplate : IRegularLineTemplate
	{
		public RegularLineStyle Style { get; set; }
		public Color32 Color { get; set; }
		public float Width { get; set; }
		public Alignment Alignment { get; set; }
		public float SpaceLength { get; set; }
		public float DashLength { get; set; }
		public float Offset { get; set; }
		public bool UseSecondColor { get; set; }
		public Color32 SecondColor { get; set; }
		public float AsymDashLength { get; set; }
		public float Elevation { get; set; }
		public bool Invert { get; set; }
		public IPropLineTemplate PropTemplate { get; set; }
		public ITreeLineTemplate TreeTemplate { get; set; }
		public ITextLineTemplate TextTemplate { get; set; }
		public INetworkLineTemplate NetworkTemplate { get; set; }
		public ISharkTeethLineTemplate SharkTeethTemplate { get; set; }
		public IZigZagLineTemplate ZigZagTemplate { get; set; }
	}

	public class SharkTeethLineTemplate : ISharkTeethLineTemplate
	{
		public Color32 Color { get; set; }
		public float Angle { get; set; }
		public float Space { get; set; }
		public float Height { get; set; }
		public float BaseWidth { get; set; }
	}

	public class ZigZagLineTemplate : IZigZagLineTemplate
	{
		public Color32 Color { get; set; }
		public float Width { get; set; }
		public float Step { get; set; }
		public float Offset { get; set; }
		public bool StartFrom { get; set; }
		public bool Side { get; set; }
	}

	public class TextLineTemplate : ITextLineTemplate
	{
		public Color32 Color { get; set; }
		public string Text { get; set; }
		public string Font { get; set; }
		public float Scale { get; set; }
		public float Angle { get; set; }
		public float Shift { get; set; }
		public TextDirection Direction { get; set; }
		public Vector2 Spacing { get; set; }
		public TextAlignment Alignment { get; set; }
	}

	public class NetworkLineTemplate : INetworkLineTemplate
	{
		public NetInfo Network { get; set; }
		public int RepeatDistance { get; set; }
		public bool Invert { get; set; }
		public float Shift { get; set; }
		public float Elevation { get; set; }
		public float Scale { get; set; }
		public float OffsetBefore { get; set; }
		public float OffsetAfter { get; set; }
	}

	public class PropLineTemplate : PrefabLineTemplate, IPropLineTemplate
	{
		public PropInfo Prop { get; set; }
		public ColorOptions ColorOption { get; set; }
		public Color32 Color { get; set; }
	}

	public class TreeLineTemplate : PrefabLineTemplate, ITreeLineTemplate
	{
		public TreeInfo Tree { get; set; }
	}

	public class PrefabLineTemplate : IPrefabLineTemplate
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
		public DistributionType Distribution { get; set; }
	}
}
