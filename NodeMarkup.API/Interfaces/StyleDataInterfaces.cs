
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeMarkup.API
{
    public interface IStyleData
    {
        string Name { get; }
        IEnumerable<IStylePropertyData> Properties { get; }
        object GetValue(string propertyName);
        void SetValue(string propertyName, object value);
    }
    public interface IStylePropertyData
    {
        Type Type { get; }
        string Name { get; }
        object Value { get; set; }
    }

    public interface IRegularLineStyleData : IStyleData { }
    public interface INormalLineStyleData : IStyleData { }
    public interface IStopLineStyleData : IStyleData { }
    public interface ILaneLineStyleData : IStyleData { }
    public interface ICrosswalkStyleData : IStyleData { }
    public interface IFillerStyleData : IStyleData { }

    public interface ILineStyleProvider : ISolidLineStyle, IDoubleSolidLineStyle, IDashedLineStyle, IDoubleDashedLineStyle, IDoubleDashedAsymLineStyle, ISolidAndDashedLineStyle, ISharkTeethLineStyle, IZigZagLineStyle, IPavementLineStyle, IPropLineStyle, ITreeLineStyle, ITextLineStyle, INetworkLineStyle { }
    public interface IStopLineStyleProvider : ISolidStopLineStyle, IDoubleSolidStopLineStyle, IDashedStopLineStyle, IDoubleDashedStopLineStyle, ISolidAndDashedStopLineStyle, ISharkTeethStopLineStyle, IPavementStopLineStyle { }
    public interface ICrosswalkStyleProvider : IExistentCrosswalkStyle, IZebraCrosswalkStyle, IDoubleZebraCrosswalkStyle, IParallelSolidLinesCrosswalkStyle, IParallelDashedLinesCrosswalkStyle, ILadderCrosswalkStyle, ISolidCrosswalkStyle, IChessBoardCrosswalkStyle { }
    public interface IFillerStyleProvider : IStripeFillerStyle, IGridFillerStyle, ISolidFillerStyle, IChevronFillerStyle, IPavementFillerStyle, IGrassFillerStyle, IGravelFillerStyle, IRuinedFillerStyle, ICliffFillerStyle { }


    public interface ISolidLineStyle : IRegularLineStyleData, INormalLineStyleData
    {
        Color32 Color { get; set; }
        float Width { get; set; }
    }
    public interface IDoubleSolidLineStyle : IRegularLineStyleData, INormalLineStyleData
    {
        bool TwoColors { get; set; }
        Color32 Color { get; set; }
        Color32 SecondColor { get; set; }
        float Width { get; set; }
        float Offset { get; set; }
        Alignment Alignment { get; set; }
    }
    public interface IDashedLineStyle : IRegularLineStyleData, INormalLineStyleData
    {
        Color32 Color { get; set; }
        float Width { get; set; }
        float DashLength { get; set; }
        float SpaceLength { get; set; }
    }
    public interface IDoubleDashedLineStyle : IRegularLineStyleData, INormalLineStyleData
    {
        bool TwoColors { get; set; }
        Color32 Color { get; set; }
        Color32 SecondColor { get; set; }
        float Width { get; set; }
        float DashLength { get; set; }
        float SpaceLength { get; set; }
        float Offset { get; set; }
        Alignment Alignment { get; set; }
    }
    public interface IDoubleDashedAsymLineStyle : IRegularLineStyleData, INormalLineStyleData
    {
        bool TwoColors { get; set; }
        Color32 Color { get; set; }
        Color32 SecondColor { get; set; }
        float Width { get; set; }
        float DashLength { get; set; }
        float SpaceLength { get; set; }
        float Offset { get; set; }
        Alignment Alignment { get; set; }
        bool Invert { get; set; }
    }
    public interface ISolidAndDashedLineStyle : IRegularLineStyleData, INormalLineStyleData
    {
        bool TwoColors { get; set; }
        Color32 Color { get; set; }
        Color32 SecondColor { get; set; }
        float Width { get; set; }
        float DashLength { get; set; }
        float SpaceLength { get; set; }
        float Offset { get; set; }
        bool CenterSolid { get; set; }
        Alignment Alignment { get; set; }
        bool Invert { get; set; }
    }
    public interface ISharkTeethLineStyle : IRegularLineStyleData, INormalLineStyleData
    {
        Color32 Color { get; set; }
        float Width { get; set; }
        float Base { get; set; }
        float Height { get; set; }
        float Space { get; set; }
        float Angle { get; set; }
        bool Invert { get; set; }
    }
    public interface IZigZagLineStyle : IRegularLineStyleData, INormalLineStyleData
    {
        Color32 Color { get; set; }
        float Width { get; set; }
        float Step { get; set; }
        float Offset { get; set; }
        bool Side { get; set; }
        bool StartFrom { get; set; }
    }
    public interface IPavementLineStyle : IRegularLineStyleData, INormalLineStyleData
    {
        float Width { get; set; }
        float Elevation { get; set; }
    }
    public interface IPropLineStyle : IRegularLineStyleData, INormalLineStyleData, ILaneLineStyleData
    {
        PropInfo Prefab { get; set; }
        ColorOptions ColorOption { get; set; }
        Color32 Color { get; set; }
        float Width { get; set; }
        DistributionType Distribution { get; set; }
        int Probability { get; set; }
        float? Step { get; set; }
        Vector2 Angle { get; set; }
        Vector2 Tilt { get; set; }
        Vector2? Slope { get; set; }
        Vector2 Shift { get; set; }
        Vector2 Elevation { get; set; }
        Vector2 Scale { get; set; }
        float OffsetBefore { get; set; }
        float OffsetAfter { get; set; }
    }
    public interface ITreeLineStyle : IRegularLineStyleData, INormalLineStyleData, ILaneLineStyleData
    {
        TreeInfo Prefab { get; set; }
        Color32 Color { get; set; }
        float Width { get; set; }
        DistributionType Distribution { get; set; }
        int Probability { get; set; }
        float? Step { get; set; }
        Vector2 Angle { get; set; }
        Vector2 Tilt { get; set; }
        Vector2? Slope { get; set; }
        Vector2 Shift { get; set; }
        Vector2 Elevation { get; set; }
        Vector2 Scale { get; set; }
        float OffsetBefore { get; set; }
        float OffsetAfter { get; set; }
    }
    public interface ITextLineStyle : IRegularLineStyleData, INormalLineStyleData, ILaneLineStyleData
    {
        string Text { get; set; }
        string Font { get; set; }
        Color32 Color { get; set; }
        float Scale { get; set; }
        TextDirection Direction { get; set; }
        Vector2 Spacing { get; set; }
        TextAlignment Alignment { get; set; }
        float Shift { get; set; }
        float Angle { get; set; }
    }
    public interface INetworkLineStyle : IRegularLineStyleData, INormalLineStyleData, ILaneLineStyleData
    {
        NetInfo Prefab { get; set; }
        float Shift { get; set; }
        float Elevation { get; set; }
        float Scale { get; set; }
        int RepeatDistance { get; set; }
        float OffsetBefore { get; set; }
        float OffsetAfter { get; set; }
        bool Invert { get; set; }
    }


    public interface ISolidStopLineStyle : IStopLineStyleData
    {
        Color32 Color { get; set; }
        float Width { get; set; }
    }
    public interface IDoubleSolidStopLineStyle : IStopLineStyleData
    {
        bool TwoColors { get; set; }
        Color32 Color { get; set; }
        Color32 SecondColor { get; set; }
        float Width { get; set; }
        float Offset { get; set; }
    }
    public interface IDashedStopLineStyle : IStopLineStyleData
    {
        Color32 Color { get; set; }
        float Width { get; set; }
        float DashLength { get; set; }
        float SpaceLength { get; set; }
    }
    public interface IDoubleDashedStopLineStyle : IStopLineStyleData
    {
        bool TwoColors { get; set; }
        Color32 Color { get; set; }
        Color32 SecondColor { get; set; }
        float Width { get; set; }
        float DashLength { get; set; }
        float SpaceLength { get; set; }
        float Offset { get; set; }
    }
    public interface ISolidAndDashedStopLineStyle : IStopLineStyleData
    {
        bool TwoColors { get; set; }
        Color32 Color { get; set; }
        Color32 SecondColor { get; set; }
        float Width { get; set; }
        float DashLength { get; set; }
        float SpaceLength { get; set; }
        float Offset { get; set; }
    }
    public interface ISharkTeethStopLineStyle : IStopLineStyleData
    {
        Color32 Color { get; set; }
        float Width { get; set; }
        float Base { get; set; }
        float Height { get; set; }
        float Space { get; set; }
    }
    public interface IPavementStopLineStyle : IStopLineStyleData
    {
        float Width { get; set; }
        float Elevation { get; set; }
    }


    public interface IExistentCrosswalkStyle : ICrosswalkStyleData
    {
        float Width { get; set; }
    }
    public interface IZebraCrosswalkStyle : ICrosswalkStyleData
    {
        bool TwoColors { get; set; }
        Color32 Color { get; set; }
        Color32 SecondColor { get; set; }
        float Width { get; set; }
        float DashLength { get; set; }
        float SpaceLength { get; set; }
        float OffsetBefore { get; set; }
        float OffsetAfter { get; set; }
        bool Parallel { get; set; }
        bool UseGap { get; set; }
        float GapLength { get; set; }
        int GapPeriod { get; set; }
    }
    public interface IDoubleZebraCrosswalkStyle : ICrosswalkStyleData
    {
        bool TwoColors { get; set; }
        Color32 Color { get; set; }
        Color32 SecondColor { get; set; }
        float Width { get; set; }
        float DashLength { get; set; }
        float SpaceLength { get; set; }
        float OffsetBefore { get; set; }
        float OffsetBetween { get; set; }
        float OffsetAfter { get; set; }
        bool Parallel { get; set; }
        bool UseGap { get; set; }
        float GapLength { get; set; }
        int GapPeriod { get; set; }
    }
    public interface IParallelSolidLinesCrosswalkStyle : ICrosswalkStyleData
    {
        Color32 Color { get; set; }
        float Width { get; set; }
        float LineWidth { get; set; }
        float OffsetBefore { get; set; }
        float OffsetAfter { get; set; }
    }
    public interface IParallelDashedLinesCrosswalkStyle : ICrosswalkStyleData
    {
        Color32 Color { get; set; }
        float Width { get; set; }
        float LineWidth { get; set; }
        float DashLength { get; set; }
        float SpaceLength { get; set; }
        float OffsetBefore { get; set; }
        float OffsetAfter { get; set; }
    }
    public interface ILadderCrosswalkStyle : ICrosswalkStyleData
    {
        Color32 Color { get; set; }
        float Width { get; set; }
        float LineWidth { get; set; }
        float DashLength { get; set; }
        float SpaceLength { get; set; }
        float OffsetBefore { get; set; }
        float OffsetAfter { get; set; }
    }
    public interface ISolidCrosswalkStyle : ICrosswalkStyleData
    {
        Color32 Color { get; set; }
        float Width { get; set; }
        float OffsetBefore { get; set; }
        float OffsetAfter { get; set; }
    }
    public interface IChessBoardCrosswalkStyle : ICrosswalkStyleData
    {
        Color32 Color { get; set; }
        float Width { get; set; }
        float SquareSide { get; set; }
        int LineCount { get; set; }
        float OffsetBefore { get; set; }
        float OffsetAfter { get; set; }
        bool Invert { get; set; }
    }


    public interface IStripeFillerStyle : IFillerStyleData
    {
        Color32 Color { get; set; }
        float Width { get; set; }
        float Step { get; set; }
        float Angle { get; set; }
        float LineOffset { get; set; }
        float MedianOffset { get; set; }
        int LeftGuideA { get; set; }
        int LeftGuideB { get; set; }
        int RightGuideA { get; set; }
        int RightGuideB { get; set; }
        bool FollowGuides { get; set; }
    }
    public interface IGridFillerStyle : IFillerStyleData
    {
        Color32 Color { get; set; }
        float Width { get; set; }
        float Step { get; set; }
        float Angle { get; set; }
        float LineOffset { get; set; }
        float MedianOffset { get; set; }
    }
    public interface ISolidFillerStyle : IFillerStyleData
    {
        Color32 Color { get; set; }
        float LineOffset { get; set; }
        float MedianOffset { get; set; }
        int LeftGuideA { get; set; }
        int LeftGuideB { get; set; }
        int RightGuideA { get; set; }
        int RightGuideB { get; set; }
        bool FollowGuides { get; set; }
    }
    public interface IChevronFillerStyle : IFillerStyleData
    {
        Color32 Color { get; set; }
        float Width { get; set; }
        float Step { get; set; }
        float AngleBetween { get; set; }
        float LineOffset { get; set; }
        float MedianOffset { get; set; }
        int LeftGuideA { get; set; }
        int LeftGuideB { get; set; }
        int RightGuideA { get; set; }
        int RightGuideB { get; set; }
        bool Invert { get; set; }
    }
    public interface IPavementFillerStyle : IFillerStyleData
    {
        float Elevation { get; set; }
        float CornerRadius { get; set; }
        float MedianCornerRadius { get; set; }
        float LineOffset { get; set; }
        float MedianOffset { get; set; }
    }
    public interface IGrassFillerStyle : IFillerStyleData
    {
        float Elevation { get; set; }
        float CornerRadius { get; set; }
        float MedianCornerRadius { get; set; }
        float CurbSize { get; set; }
        float MedianCurbSize { get; set; }
        float LineOffset { get; set; }
        float MedianOffset { get; set; }
    }
    public interface IGravelFillerStyle : IFillerStyleData
    {
        float Elevation { get; set; }
        float CornerRadius { get; set; }
        float MedianCornerRadius { get; set; }
        float CurbSize { get; set; }
        float MedianCurbSize { get; set; }
        float LineOffset { get; set; }
        float MedianOffset { get; set; }
    }
    public interface IRuinedFillerStyle : IFillerStyleData
    {
        float Elevation { get; set; }
        float CornerRadius { get; set; }
        float MedianCornerRadius { get; set; }
        float CurbSize { get; set; }
        float MedianCurbSize { get; set; }
        float LineOffset { get; set; }
        float MedianOffset { get; set; }
    }
    public interface ICliffFillerStyle : IFillerStyleData
    {
        float Elevation { get; set; }
        float CornerRadius { get; set; }
        float MedianCornerRadius { get; set; }
        float CurbSize { get; set; }
        float MedianCurbSize { get; set; }
        float LineOffset { get; set; }
        float MedianOffset { get; set; }
    }
}
