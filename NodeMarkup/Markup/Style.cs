using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.Manager
{
    public interface IStyle { }
    public interface IColorStyle : IStyle
    {
        PropertyColorValue Color { get; }
    }
    public interface IWidthStyle : IStyle
    {
        PropertyStructValue<float> Width { get; }
    }
    public abstract class Style : IToXml
    {
        public static float DefaultDashLength => 1.5f;
        public static float DefaultSpaceLength => 1.5f;
        public static float DefaultDoubleOffset => 0.15f;

        public static float DefaultSharkBaseLength => 0.5f;
        public static float DefaultSharkSpaceLength => 0.5f;
        public static float DefaultSharkHeight => 0.6f;

        public static float Default3DWidth => 0.3f;
        public static float Default3DHeigth => 0.3f;

        public static bool FromXml<T>(XElement config, ObjectsMap map, bool invert, out T style) where T : Style
        {
            var type = IntToType(config.GetAttrValue<int>("T"));

            if (SingletonManager<StyleTemplateManager>.Instance.GetDefault<T>(type) is T defaultStyle)
            {
                style = defaultStyle;
                style.FromXml(config, map, invert);
                return true;
            }
            else
            {
                style = default;
                return false;
            }
        }
        private static StyleType IntToType(int rawType)
        {
            var typeGroup = rawType & (int)StyleType.GroupMask;
            var typeNum = (rawType & (int)StyleType.ItemMask) + 1;
            var type = (StyleType)((typeGroup == 0 ? (int)StyleType.RegularLine : typeGroup << 1) + typeNum);
            return type;
        }
        private static int TypeToInt(StyleType type)
        {
            var typeGroup = (int)type & (int)StyleType.GroupMask;
            var typeNum = ((int)type & (int)StyleType.ItemMask) - 1;
            var rawType = ((typeGroup >> 1) & (int)StyleType.GroupMask) + typeNum;
            return rawType;
        }

        public static Color32 DefaultColor { get; } = new Color32(136, 136, 136, 224);
        public static float DefaultWidth { get; } = 0.15f;

        protected virtual float WidthWheelStep { get; } = 0.01f;
        protected virtual float WidthMinValue { get; } = 0.05f;

        public static T GetDefault<T>(StyleType type) where T : Style
        {
            return type.GetGroup() switch
            {
                StyleType.RegularLine => GetDefault(RegularLineStyle.Defaults, type.ToEnum<RegularLineStyle.RegularLineType, StyleType>()) as T,
                StyleType.StopLine => GetDefault(StopLineStyle.Defaults, type.ToEnum<StopLineStyle.StopLineType, StyleType>()) as T,
                StyleType.Filler => GetDefault(FillerStyle.Defaults, type.ToEnum<FillerStyle.FillerType, StyleType>()) as T,
                StyleType.Crosswalk => GetDefault(CrosswalkStyle.Defaults, type.ToEnum<CrosswalkStyle.CrosswalkType, StyleType>()) as T,
                _ => null,
            };
        }
        private static TypeStyle GetDefault<TypeEnum, TypeStyle>(Dictionary<TypeEnum, TypeStyle> dic, TypeEnum type)
            where TypeEnum : Enum
            where TypeStyle : Style
        {
            return dic.TryGetValue(type, out var style) ? (TypeStyle)style.Copy() : null;
        }

        public static string XmlName { get; } = "S";

        public Action OnStyleChanged { private get; set; }
        public string XmlSection => XmlName;
        public abstract StyleType Type { get; }

        protected virtual void StyleChanged() => OnStyleChanged?.Invoke();

        public PropertyColorValue Color { get; }
        public PropertyStructValue<float> Width { get; }
        public Style(Color32 color, float width)
        {
            Color = GetColorProperty(color);
            Width = GetWidthProperty(width);
        }
        protected XElement BaseToXml() => new XElement(XmlSection, new XAttribute("T", TypeToInt(Type)));
        public virtual XElement ToXml()
        {
            var config = BaseToXml();
            Color.ToXml(config);
            Width.ToXml(config);
            return config;
        }
        public virtual void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            Color.FromXml(config, DefaultColor);
            Width.FromXml(config, DefaultWidth);
        }

        public abstract Style Copy();
        protected void CopyTo(Style target)
        {
            if (this is IWidthStyle widthSource && target is IWidthStyle widthTarget)
                widthTarget.Width.Value = widthSource.Width;
            if (this is IColorStyle colorSource && target is IColorStyle colorTarget)
                colorTarget.Color.Value = colorSource.Color;
        }

        public virtual List<EditorItem> GetUIComponents(object editObject, UIComponent parent, bool isTemplate = false)
        {
            var components = new List<EditorItem>();

            if (this is IColorStyle)
                components.Add(AddColorProperty(parent));
            if (this is IWidthStyle)
                components.Add(AddWidthProperty(parent));

            return components;
        }
        private ColorAdvancedPropertyPanel AddColorProperty(UIComponent parent)
        {
            var colorProperty = ComponentPool.Get<ColorAdvancedPropertyPanel>(parent, nameof(Color));
            colorProperty.Text = Localize.StyleOption_Color;
            colorProperty.WheelTip = Settings.ShowToolTip;
            colorProperty.Init();
            colorProperty.Value = Color;
            colorProperty.OnValueChanged += (Color32 color) => Color.Value = color;

            return colorProperty;
        }
        private FloatPropertyPanel AddWidthProperty(UIComponent parent)
        {
            var widthProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(Width));
            widthProperty.Text = Localize.StyleOption_Width;
            widthProperty.UseWheel = true;
            widthProperty.WheelStep = WidthWheelStep;
            widthProperty.WheelTip = Settings.ShowToolTip;
            widthProperty.CheckMin = true;
            widthProperty.MinValue = WidthMinValue;
            widthProperty.Init();
            widthProperty.Value = Width;
            widthProperty.OnValueChanged += (float value) => Width.Value = value;

            return widthProperty;
        }
        protected FloatPropertyPanel AddDashLengthProperty(IDashedLine dashedStyle, UIComponent parent)
        {
            var dashLengthProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(dashedStyle.DashLength));
            dashLengthProperty.Text = Localize.StyleOption_DashedLength;
            dashLengthProperty.UseWheel = true;
            dashLengthProperty.WheelStep = 0.1f;
            dashLengthProperty.WheelTip = Settings.ShowToolTip;
            dashLengthProperty.CheckMin = true;
            dashLengthProperty.MinValue = 0.1f;
            dashLengthProperty.Init();
            dashLengthProperty.Value = dashedStyle.DashLength;
            dashLengthProperty.OnValueChanged += (float value) => dashedStyle.DashLength.Value = value;

            return dashLengthProperty;
        }
        protected FloatPropertyPanel AddSpaceLengthProperty(IDashedLine dashedStyle, UIComponent parent)
        {
            var spaceLengthProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(dashedStyle.SpaceLength));
            spaceLengthProperty.Text = Localize.StyleOption_SpaceLength;
            spaceLengthProperty.UseWheel = true;
            spaceLengthProperty.WheelStep = 0.1f;
            spaceLengthProperty.WheelTip = Settings.ShowToolTip;
            spaceLengthProperty.CheckMin = true;
            spaceLengthProperty.MinValue = 0.1f;
            spaceLengthProperty.Init();
            spaceLengthProperty.Value = dashedStyle.SpaceLength;
            spaceLengthProperty.OnValueChanged += (float value) => dashedStyle.SpaceLength.Value = value;

            return spaceLengthProperty;
        }
        protected ButtonPanel AddInvertProperty(IAsymLine asymStyle, UIComponent parent)
        {
            var buttonsPanel = ComponentPool.Get<ButtonPanel>(parent, nameof(asymStyle.Invert));
            buttonsPanel.Text = Localize.StyleOption_Invert;
            buttonsPanel.Init();
            buttonsPanel.OnButtonClick += OnButtonClick;

            void OnButtonClick() => asymStyle.Invert.Value = !asymStyle.Invert;

            return buttonsPanel;
        }

        protected PropertyColorValue GetColorProperty(Color32 defaultValue) => new PropertyColorValue("C", StyleChanged, defaultValue);
        protected PropertyColorValue GetSecondColorProperty(Color32 defaultValue) => new PropertyColorValue("SC", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetWidthProperty(float defaultValue) => new PropertyStructValue<float>("W", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetLineOffsetProperty(float defaultValue) => new PropertyStructValue<float>("O", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetOffsetProperty(float defaultValue) => new PropertyStructValue<float>("O", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetMedianOffsetProperty(float defaultValue) => new PropertyStructValue<float>("MO", StyleChanged, defaultValue);
        protected PropertyEnumValue<Alignment> GetAlignmentProperty(Alignment defaultValue) => new PropertyEnumValue<Alignment>("A", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetDashLengthProperty(float defaultValue) => new PropertyStructValue<float>("DL", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetSpaceLengthProperty(float defaultValue) => new PropertyStructValue<float>("SL", StyleChanged, defaultValue);
        protected PropertyBoolValue GetInvertProperty(bool defaultValue) => new PropertyBoolValue("I", StyleChanged, defaultValue);
        protected PropertyBoolValue GetCenterSolidProperty(bool defaultValue) => new PropertyBoolValue("CS", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetBaseProperty(float defaultValue) => new PropertyStructValue<float>("B", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetHeightProperty(float defaultValue) => new PropertyStructValue<float>("H", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetSpaceProperty(float defaultValue) => new PropertyStructValue<float>("S", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetOffsetBeforeProperty(float defaultValue) => new PropertyStructValue<float>("OB", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetOffsetAfterProperty(float defaultValue) => new PropertyStructValue<float>("OA", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetLineWidthProperty(float defaultValue) => new PropertyStructValue<float>("LW", StyleChanged, defaultValue);
        protected PropertyBoolValue GetParallelProperty(bool defaultValue) => new PropertyBoolValue("P", StyleChanged, defaultValue);
        protected PropertyBoolValue GetUseSecondColorProperty(bool defaultValue) => new PropertyBoolValue("USC", StyleChanged, defaultValue);
        protected PropertyBoolValue GetUseGapProperty(bool defaultValue) => new PropertyBoolValue("UG", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetGapLengthProperty(float defaultValue) => new PropertyStructValue<float>("GL", StyleChanged, defaultValue);
        protected PropertyStructValue<int> GetGapPeriodProperty(int defaultValue) => new PropertyStructValue<int>("GP", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetSquareSideProperty(float defaultValue) => new PropertyStructValue<float>("SS", StyleChanged, defaultValue);
        protected PropertyStructValue<int> GetLineCountProperty(int defaultValue) => new PropertyStructValue<int>("LC", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetAngleProperty(float defaultValue) => new PropertyStructValue<float>("A", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetStepProperty(float defaultValue) => new PropertyStructValue<float>("S", StyleChanged, defaultValue);
        protected PropertyStructValue<int> GetOutputProperty(int defaultValue) => new PropertyStructValue<int>("O", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetAngleBetweenProperty(float defaultValue) => new PropertyStructValue<float>("A", StyleChanged, defaultValue);
        protected PropertyEnumValue<ChevronFillerStyle.From> GetStartingFromProperty(ChevronFillerStyle.From defaultValue) => new PropertyEnumValue<ChevronFillerStyle.From>("SF", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetElevationProperty(float defaultValue) => new PropertyStructValue<float>("E", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetCornerRadiusProperty(float defaultValue) => new PropertyStructValue<float>("CR", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetMedianCornerRadiusProperty(float defaultValue) => new PropertyStructValue<float>("MCR", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetCurbSizeProperty(float defaultValue) => new PropertyStructValue<float>("CS", StyleChanged, defaultValue);
        protected PropertyStructValue<float> GetMedianCurbSizeProperty(float defaultValue) => new PropertyStructValue<float>("MCS", StyleChanged, defaultValue);
        protected PropertyStructValue<int> GetLeftRailAProperty(int defaultValue) => new PropertyStructValue<int>("LRA", StyleChanged, defaultValue);
        protected PropertyStructValue<int> GetLeftRailBProperty(int defaultValue) => new PropertyStructValue<int>("LRB", StyleChanged, defaultValue);
        protected PropertyStructValue<int> GetRightRailAProperty(int defaultValue) => new PropertyStructValue<int>("RRA", StyleChanged, defaultValue);
        protected PropertyStructValue<int> GetRightRailBProperty(int defaultValue) => new PropertyStructValue<int>("RRB", StyleChanged, defaultValue);
        protected PropertyBoolValue GetFollowRailsProperty(bool defaultValue) => new PropertyBoolValue("FR", StyleChanged, defaultValue);

        public enum StyleType
        {
            [NotItem]
            ItemMask = 0xFF,
            [NotItem]
            GroupMask = ~ItemMask,

            #region REGULAR

            [NotItem]
            [Description(nameof(Localize.LineStyle_RegularLinesGroup))]
            RegularLine = Markup.Item.RegularLine,

            [Description(nameof(Localize.LineStyle_Solid))]
            LineSolid,

            [Description(nameof(Localize.LineStyle_Dashed))]
            LineDashed,

            [Description(nameof(Localize.LineStyle_DoubleSolid))]
            LineDoubleSolid,

            [Description(nameof(Localize.LineStyle_DoubleDashed))]
            LineDoubleDashed,

            [Description(nameof(Localize.LineStyle_SolidAndDashed))]
            LineSolidAndDashed,

            [Description(nameof(Localize.LineStyle_SharkTeeth))]
            LineSharkTeeth,

            [NotItem]
            Regular3DLine = Markup.Item.RegularLine + 0x80,

            [Description(nameof(Localize.LineStyle_Pavement))]
            LinePavement,

            [Description(nameof(Localize.LineStyle_Empty))]
            [NotVisible]
            EmptyLine = LineBuffer - 1,

            [Description(nameof(Localize.Style_FromClipboard))]
            [NotVisible]
            LineBuffer = Markup.Item.RegularLine + 0x100 - 1,

            #endregion

            #region STOP

            [NotItem]
            [Description(nameof(Localize.LineStyle_StopLinesGroup))]
            StopLine = Markup.Item.StopLine,

            [Description(nameof(Localize.LineStyle_StopSolid))]
            StopLineSolid,

            [Description(nameof(Localize.LineStyle_StopDashed))]
            StopLineDashed,

            [Description(nameof(Localize.LineStyle_StopDouble))]
            StopLineDoubleSolid,

            [Description(nameof(Localize.LineStyle_StopDoubleDashed))]
            StopLineDoubleDashed,

            [Description(nameof(Localize.LineStyle_StopSolidAndDashed))]
            StopLineSolidAndDashed,

            [Description(nameof(Localize.LineStyle_StopSharkTeeth))]
            StopLineSharkTeeth,

            [Description(nameof(Localize.LineStyle_StopPavement))]
            StopLinePavement,

            [Description(nameof(Localize.Style_FromClipboard))]
            [NotVisible]
            StopLineBuffer = Markup.Item.StopLine + 0x100 - 1,

            #endregion

            #region FILLER

            [NotItem]
            [Description(nameof(Localize.FillerStyle_Group))]
            Filler = Markup.Item.Filler,

            [Description(nameof(Localize.FillerStyle_Stripe))]
            FillerStripe,

            [Description(nameof(Localize.FillerStyle_Grid))]
            FillerGrid,

            [Description(nameof(Localize.FillerStyle_Solid))]
            FillerSolid,

            [Description(nameof(Localize.FillerStyle_Chevron))]
            FillerChevron,

            [NotItem]
            Filler3D = Filler + 0x80,

            [Description(nameof(Localize.FillerStyle_Pavement))]
            FillerPavement,

            [Description(nameof(Localize.FillerStyle_Grass))]
            FillerGrass,

            [Description(nameof(Localize.FillerStyle_Gravel))]
            FillerGravel,

            [Description(nameof(Localize.FillerStyle_Ruined))]
            FillerRuined,

            [Description(nameof(Localize.FillerStyle_Cliff))]
            FillerCliff,

            [Description(nameof(Localize.Style_FromClipboard))]
            [NotVisible]
            FillerBuffer = Markup.Item.Filler + 0x100 - 1,

            #endregion

            #region CROSSWALK

            [NotItem]
            [Description(nameof(Localize.CrosswalkStyle_Group))]
            Crosswalk = Markup.Item.Crosswalk,

            [Description(nameof(Localize.CrosswalkStyle_Existent))]
            CrosswalkExistent,

            [Description(nameof(Localize.CrosswalkStyle_Zebra))]
            CrosswalkZebra,

            [Description(nameof(Localize.CrosswalkStyle_DoubleZebra))]
            CrosswalkDoubleZebra,

            [Description(nameof(Localize.CrosswalkStyle_ParallelSolidLines))]
            CrosswalkParallelSolidLines,

            [Description(nameof(Localize.CrosswalkStyle_ParallelDashedLines))]
            CrosswalkParallelDashedLines,

            [Description(nameof(Localize.CrosswalkStyle_Ladder))]
            CrosswalkLadder,

            [Description(nameof(Localize.CrosswalkStyle_Solid))]
            CrosswalkSolid,

            [Description(nameof(Localize.CrosswalkStyle_ChessBoard))]
            CrosswalkChessBoard,

            [Description(nameof(Localize.Style_FromClipboard))]
            [NotVisible]
            CrosswalkBuffer = Markup.Item.Crosswalk + 0x100 - 1,

            #endregion
        }
    }
    public abstract class Style<StyleType> : Style
        where StyleType : Style<StyleType>
    {
        public Style(Color32 color, float width) : base(color, width) { }

        public virtual void CopyTo(StyleType target) => base.CopyTo(target);
        public sealed override Style Copy() => CopyStyle();
        public abstract StyleType CopyStyle();
    }
}
