using UnityEngine;

namespace NodeMarkup.API
{
	public interface IRegularLineTemplate
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
		public float AsymDashLength { get; }
		public float Elevation { get; }
		public IPropLineTemplate PropTemplate { get; }
		public ITreeLineTemplate TreeTemplate { get; }
		public ITextLineTemplate TextTemplate { get; }
		public INetworkLineTemplate NetworkTemplate { get; }
		public ISharkTeethLineTemplate SharkTeethTemplate { get; }
		public IZigZagLineTemplate ZigZagTemplate { get; }
		bool Invert { get; }
	}

	public interface ISharkTeethLineTemplate
	{
		public float Angle { get; }
		public float Space { get; }
		public float Height { get; }
		public float BaseWidth { get; }
		Color32 Color { get; }
	}

	public interface IZigZagLineTemplate
	{
		public float Width { get; }
		public float Step { get; }
		public float Offset { get; }
		public bool StartFrom { get; }
		public bool Side { get; }
		Color32 Color { get; }
	}

	public interface ITextLineTemplate
	{
		public string Text { get; }
		public string Font { get; }
		public float Scale { get; }
		public float Angle { get; }
		public float Shift { get; }
		public TextDirection Direction { get; }
		public Vector2 Spacing { get; }
		public TextAlignment Alignment { get; }
		Color32 Color { get; }
	}

	public interface INetworkLineTemplate
	{
		public NetInfo Network { get; }
		public int RepeatDistance { get; }
		public bool Invert { get; }
		public float Shift { get; }
		public float Elevation { get; }
		public float Scale { get; }
		public float OffsetBefore { get; }
		public float OffsetAfter { get; }
	}

	public interface IPropLineTemplate : IPrefabLineTemplate
	{
		public PropInfo Prop { get; }
		public ColorOptions ColorOption { get; }
		public Color32 Color { get; }
	}

	public interface ITreeLineTemplate : IPrefabLineTemplate
	{
		public TreeInfo Tree { get; }
	}

	public interface IPrefabLineTemplate
	{
		public int Probability { get; }
		public float? Step { get; }
		public Vector2 Angle { get; }
		public Vector2 Tilt { get; }
		public Vector2 Slope { get; }
		public Vector2 Shift { get; }
		public Vector2 Scale { get; }
		public Vector2 Elevation { get; }
		public float OffsetBefore { get; }
		public float OffsetAfter { get; }
		public DistributionType Distribution { get; }
	}
}
