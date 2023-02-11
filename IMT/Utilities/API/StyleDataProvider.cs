using IMT.API;
using IMT.Manager;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using IStyleData = IMT.API.IStyleData;

namespace IMT.Utilities.API
{
    public struct StyleDataProvider : IStyleData, ILineStyleProvider, IStopLineStyleProvider, ICrosswalkStyleProvider, IFillerStyleProvider
    {
        public Style Style { get; }
        public string Name => Style.Type.ToString();

        private Dictionary<string, IStylePropertyData> properties;
        private Dictionary<string, IStylePropertyData> PropertiesDic
        {
            get
            {
                if (properties == null)
                    properties = Style.Properties.ToDictionary(i => i.Name, i => i);

                return properties;
            }
        }
        public IEnumerable<IStylePropertyData> Properties => PropertiesDic.Values;

        public StyleDataProvider(Style style)
        {
            Style = style;
            properties = null;
        }

        public object GetValue(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (!PropertiesDic.TryGetValue(name, out var property))
                throw new IntersectionMarkingToolException($"No option with name {name}");

            return property.Value;
        }

        public void SetValue(string name, object value)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (!PropertiesDic.TryGetValue(name, out var property))
                throw new IntersectionMarkingToolException($"No option with name {name}");

            property.Value = value;
        }

        public override string ToString() => Name;


        public Color32 Color { get => (Color32)GetValue(nameof(Color)); set => SetValue(nameof(Color), value); }
        public float Width { get => (float)GetValue(nameof(Width)); set => SetValue(nameof(Width), value); }
        public bool TwoColors { get => (bool)GetValue(nameof(TwoColors)); set => SetValue(nameof(TwoColors), value); }
        public Color32 SecondColor { get => (Color32)GetValue(nameof(SecondColor)); set => SetValue(nameof(SecondColor), value); }
        public float Offset { get => (float)GetValue(nameof(Offset)); set => SetValue(nameof(Offset), value); }
        public IMT.API.Alignment Alignment { get => (IMT.API.Alignment)GetValue(nameof(Alignment)); set => SetValue(nameof(Alignment), value); }
        public float DashLength { get => (float)GetValue(nameof(DashLength)); set => SetValue(nameof(DashLength), value); }
        public float SpaceLength { get => (float)GetValue(nameof(SpaceLength)); set => SetValue(nameof(SpaceLength), value); }
        public bool Invert { get => (bool)GetValue(nameof(Invert)); set => SetValue(nameof(Invert), value); }
        public bool CenterSolid { get => (bool)GetValue(nameof(CenterSolid)); set => SetValue(nameof(CenterSolid), value); }
        public float Base { get => (float)GetValue(nameof(Base)); set => SetValue(nameof(Base), value); }
        public float Height { get => (float)GetValue(nameof(Height)); set => SetValue(nameof(Height), value); }
        public float Space { get => (float)GetValue(nameof(Space)); set => SetValue(nameof(Space), value); }
        public float Angle { get => (float)GetValue(nameof(Angle)); set => SetValue(nameof(Angle), value); }
        public float Step { get => (float)GetValue(nameof(Step)); set => SetValue(nameof(Step), value); }
        public bool Side { get => (bool)GetValue(nameof(Side)); set => SetValue(nameof(Side), value); }
        public bool StartFrom { get => (bool)GetValue(nameof(StartFrom)); set => SetValue(nameof(StartFrom), value); }
        public float Elevation { get => (float)GetValue(nameof(Elevation)); set => SetValue(nameof(Elevation), value); }
        PropInfo IPropLineStyle.Prefab { get => (PropInfo)GetValue(nameof(IPropLineStyle.Prefab)); set => SetValue(nameof(IPropLineStyle.Prefab), value); }
        public ColorOptions ColorOption { get => (ColorOptions)GetValue(nameof(ColorOption)); set => SetValue(nameof(ColorOption), value); }
        public IMT.API.DistributionType Distribution { get => (IMT.API.DistributionType)GetValue(nameof(Distribution)); set => SetValue(nameof(Distribution), value); }
        public int Probability { get => (int)GetValue(nameof(Probability)); set => SetValue(nameof(Probability), value); }
        Vector2 IPropLineStyle.Angle { get => (Vector2)GetValue(nameof(Angle)); set => SetValue(nameof(Angle), value); }
        public Vector2 Tilt { get => (Vector2)GetValue(nameof(Tilt)); set => SetValue(nameof(Tilt), value); }
        public Vector2? Slope { get => (Vector2?)GetValue(nameof(Slope)); set => SetValue(nameof(Slope), value); }
        public Vector2 Shift { get => (Vector2)GetValue(nameof(Shift)); set => SetValue(nameof(Shift), value); }
        Vector2 IPropLineStyle.Elevation { get => (Vector2)GetValue(nameof(Elevation)); set => SetValue(nameof(Elevation), value); }
        public Vector2 Scale { get => (Vector2)GetValue(nameof(Scale)); set => SetValue(nameof(Scale), value); }
        public float OffsetBefore { get => (float)GetValue(nameof(OffsetBefore)); set => SetValue(nameof(OffsetBefore), value); }
        public float OffsetAfter { get => (float)GetValue(nameof(OffsetAfter)); set => SetValue(nameof(OffsetAfter), value); }
        TreeInfo ITreeLineStyle.Prefab { get => (TreeInfo)GetValue(nameof(ITreeLineStyle.Prefab)); set => SetValue(nameof(ITreeLineStyle.Prefab), value); }
        float? ITreeLineStyle.Step { get => (float?)GetValue(nameof(Step)); set => SetValue(nameof(Step), value); }
        Vector2 ITreeLineStyle.Angle { get => (Vector2)GetValue(nameof(Angle)); set => SetValue(nameof(Angle), value); }
        Vector2 ITreeLineStyle.Elevation { get => (Vector2)GetValue(nameof(Elevation)); set => SetValue(nameof(Elevation), value); }
        public string Text { get => (string)GetValue(nameof(Text)); set => SetValue(nameof(Text), value); }
        public string Font { get => (string)GetValue(nameof(Font)); set => SetValue(nameof(Font), value); }
        float ITextLineStyle.Scale { get => (float)GetValue(nameof(Scale)); set => SetValue(nameof(Scale), value); }
        public TextDirection Direction { get => (TextDirection)GetValue(nameof(Direction)); set => SetValue(nameof(Direction), value); }
        public Vector2 Spacing { get => (Vector2)GetValue(nameof(Spacing)); set => SetValue(nameof(Spacing), value); }
        IMT.API.TextAlignment ITextLineStyle.Alignment { get => (IMT.API.TextAlignment)GetValue(nameof(Alignment)); set => SetValue(nameof(Alignment), value); }
        float ITextLineStyle.Shift { get => (float)GetValue(nameof(Shift)); set => SetValue(nameof(Shift), value); }
        NetInfo INetworkLineStyle.Prefab { get => (NetInfo)GetValue(nameof(INetworkLineStyle.Prefab)); set => SetValue(nameof(INetworkLineStyle.Prefab), value); }
        Vector2 INetworkLineStyle.Shift { get => (Vector2)GetValue(nameof(Shift)); set => SetValue(nameof(Shift), value); }
        float INetworkLineStyle.Scale { get => (float)GetValue(nameof(Scale)); set => SetValue(nameof(Scale), value); }
        public int RepeatDistance { get => (int)GetValue(nameof(RepeatDistance)); set => SetValue(nameof(RepeatDistance), value); }
        float? IPropLineStyle.Step { get => (float?)GetValue(nameof(IPropLineStyle.Step)); set => SetValue(nameof(IPropLineStyle.Step), value); }
        public bool Parallel { get => (bool)GetValue(nameof(Parallel)); set => SetValue(nameof(Parallel), value); }
        public bool UseGap { get => (bool)GetValue(nameof(UseGap)); set => SetValue(nameof(UseGap), value); }
        public float GapLength { get => (float)GetValue(nameof(GapLength)); set => SetValue(nameof(GapLength), value); }
        public int GapPeriod { get => (int)GetValue(nameof(GapPeriod)); set => SetValue(nameof(GapPeriod), value); }
        public float OffsetBetween { get => (float)GetValue(nameof(OffsetBetween)); set => SetValue(nameof(OffsetBetween), value); }
        public float LineWidth { get => (float)GetValue(nameof(LineWidth)); set => SetValue(nameof(LineWidth), value); }
        public float SquareSide { get => (float)GetValue(nameof(SquareSide)); set => SetValue(nameof(SquareSide), value); }
        public int LineCount { get => (int)GetValue(nameof(LineCount)); set => SetValue(nameof(LineCount), value); }
        public float LineOffset { get => (float)GetValue(nameof(LineOffset)); set => SetValue(nameof(LineOffset), value); }
        public float MedianOffset { get => (float)GetValue(nameof(MedianOffset)); set => SetValue(nameof(MedianOffset), value); }
        public int LeftGuideA { get => (int)GetValue(nameof(LeftGuideA)); set => SetValue(nameof(LeftGuideA), value); }
        public int LeftGuideB { get => (int)GetValue(nameof(LeftGuideB)); set => SetValue(nameof(LeftGuideB), value); }
        public int RightGuideA { get => (int)GetValue(nameof(RightGuideA)); set => SetValue(nameof(RightGuideA), value); }
        public int RightGuideB { get => (int)GetValue(nameof(RightGuideB)); set => SetValue(nameof(RightGuideB), value); }
        public bool FollowGuides { get => (bool)GetValue(nameof(FollowGuides)); set => SetValue(nameof(FollowGuides), value); }
        public float AngleBetween { get => (float)GetValue(nameof(AngleBetween)); set => SetValue(nameof(AngleBetween), value); }
        public float CornerRadius { get => (float)GetValue(nameof(CornerRadius)); set => SetValue(nameof(CornerRadius), value); }
        public float MedianCornerRadius { get => (float)GetValue(nameof(MedianCornerRadius)); set => SetValue(nameof(MedianCornerRadius), value); }
        public float CurbSize { get => (float)GetValue(nameof(CurbSize)); set => SetValue(nameof(CurbSize), value); }
        public float MedianCurbSize { get => (float)GetValue(nameof(MedianCurbSize)); set => SetValue(nameof(MedianCurbSize), value); }
        public float Texture { get => (float)GetValue(nameof(Texture)); set => SetValue(nameof(Texture), value); }
        public Vector2 Cracks { get => (Vector2)GetValue(nameof(Cracks)); set => SetValue(nameof(Cracks), value); }
        public Vector2 Voids { get => (Vector2)GetValue(nameof(Voids)); set => SetValue(nameof(Voids), value); }
        public Color32? NetworkColor { get => (Color32?)GetValue(nameof(NetworkColor)); set => SetValue(nameof(NetworkColor), value); }
        public DashEnd DashEnd { get => (DashEnd)GetValue(nameof(DashEnd)); set => SetValue(nameof(DashEnd), value); }
    }
    public struct StylePropertyDataProvider<T> : IStylePropertyData
    {
        public Type Type => typeof(T).IsEnum ? Enum.GetUnderlyingType(typeof(T)) : typeof(T);
        public string Name { get; }
        BasePropertyValue<T> Property { get; }

        public object Value
        {
            get
            {
                if (Property.Value.GetType() == Type)
                    return Property.Value;
                else
                    return Convert.ChangeType(Property.Value, Type);
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (value.GetType() != Type)
                    throw new IntersectionMarkingToolException($"Wrong type of option {Name} value");

                Property.Value = (T)value;
            }
        }
        public StylePropertyDataProvider(string name, BasePropertyValue<T> property)
        {
            Name = name;
            Property = property;
        }

        public override string ToString() => $"{Name} ({Type}): {Value}";
    }
}
