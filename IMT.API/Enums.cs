namespace NodeMarkup.API
{
    public enum MarkingType
    {
        Node,
        Segment,
    }
    public enum EntranceType
    {
        Node,
        Segment,
    }
    public enum RegularLineStyleType
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
        Network,
    }
    public enum NormalLineStyleType
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
        Network,
    }
    public enum LaneLineStyleType
    {
        Prop,
        Tree,
        Text,
        Network,
    }
    public enum StopLineStyleType
    {
        Solid,
        Dashed,
        DoubleSolid,
        DoubleDashed,
        SolidAndDashed,
        SharkTeeth,
        Pavement,
    }
    public enum CrosswalkStyleType
    {
        Existent,
        Zebra,
        DoubleZebra,
        ParallelSolidLines,
        ParallelDashedLines,
        Ladder,
        Solid,
        ChessBoard,
    }
    public enum FillerStyleType
    {
        Stripe,
        Grid,
        Solid,
        Chevron,
        Pavement,
        Grass,
        Gravel,
        Ruined,
        Cliff,
    }
    public enum Alignment
	{
		Left,
		Centre,
		Right
	}
    public enum ColorOptions
    {
        Color1,
        Color2,
        Color3,
        Color4,
        Random,
        Custom,
    }
    public enum DistributionType
    {
        FixedSpaceFreeEnd,
        FixedSpaceFixedEnd,
        DynamicSpaceFreeEnd,
        DynamicSpaceFixedEnd,
    }
    public enum PointLocation
    {
        None,
        Left,
        Rigth,
        Between,
    }
    public enum TextAlignment
    {
        Start,
        Middle,
        End,
    }
    public enum TextDirection
    {
        LeftToRight,
        TopToBottom,
        BottomToTop,
    }
}
