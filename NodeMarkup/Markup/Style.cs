using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.UI;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
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
        PropertyValue<float> Width { get; }
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

            if (TemplateManager.StyleManager.GetDefault<T>(type) is T defaultStyle)
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
                StyleType.RegularLine when RegularLineStyle.GetDefault(type.ToEnum<RegularLineStyle.RegularLineType, StyleType>()) is T tStyle => tStyle,
                StyleType.StopLine when StopLineStyle.GetDefault(type.ToEnum<StopLineStyle.StopLineType, StyleType>()) is T tStyle => tStyle,
                StyleType.Filler when FillerStyle.GetDefault(type.ToEnum<FillerStyle.FillerType, StyleType>()) is T tStyle => tStyle,
                StyleType.Crosswalk when CrosswalkStyle.GetDefault(type.ToEnum<CrosswalkStyle.CrosswalkType, StyleType>()) is T tStyle => tStyle,
                _ => null,
            };
        }

        public static string XmlName { get; } = "S";

        public Action OnStyleChanged { private get; set; }
        public string XmlSection => XmlName;
        public abstract StyleType Type { get; }

        protected virtual void StyleChanged() => OnStyleChanged?.Invoke();

        public PropertyColorValue Color { get; }
        public PropertyValue<float> Width { get; }
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
            colorProperty.WheelTip = Editor.WheelTip;
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
            widthProperty.WheelTip = Editor.WheelTip;
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
            dashLengthProperty.WheelTip = Editor.WheelTip;
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
            spaceLengthProperty.WheelTip = Editor.WheelTip;
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
        protected PropertyValue<float> GetWidthProperty(float defaultValue) => new PropertyValue<float>("W", StyleChanged, defaultValue);
        protected PropertyValue<float> GetOffsetProperty(float defaultValue) => new PropertyValue<float>("O", StyleChanged, defaultValue);
        protected PropertyValue<float> GetMedianOffsetProperty(float defaultValue) => new PropertyValue<float>("MO", StyleChanged, defaultValue);
        protected PropertyEnumValue<LineAlignment> GetAlignmentProperty(LineAlignment defaultValue) => new PropertyEnumValue<LineAlignment>("A", StyleChanged, defaultValue);
        protected PropertyValue<float> GetDashLengthProperty(float defaultValue) => new PropertyValue<float>("DL", StyleChanged, defaultValue);
        protected PropertyValue<float> GetSpaceLengthProperty(float defaultValue) => new PropertyValue<float>("SL", StyleChanged, defaultValue);
        protected PropertyBoolValue GetInvertProperty(bool defaultValue) => new PropertyBoolValue("I", StyleChanged, defaultValue);
        protected PropertyBoolValue GetCenterSolidProperty(bool defaultValue) => new PropertyBoolValue("CS", StyleChanged, defaultValue);
        protected PropertyValue<float> GetBaseProperty(float defaultValue) => new PropertyValue<float>("B", StyleChanged, defaultValue);
        protected PropertyValue<float> GetHeightProperty(float defaultValue) => new PropertyValue<float>("H", StyleChanged, defaultValue);
        protected PropertyValue<float> GetSpaceProperty(float defaultValue) => new PropertyValue<float>("S", StyleChanged, defaultValue);
        protected PropertyValue<float> GetOffsetBeforeProperty(float defaultValue) => new PropertyValue<float>("OB", StyleChanged, defaultValue);
        protected PropertyValue<float> GetOffsetAfterProperty(float defaultValue) => new PropertyValue<float>("OA", StyleChanged, defaultValue);
        protected PropertyValue<float> GetLineWidthProperty(float defaultValue) => new PropertyValue<float>("LW", StyleChanged, defaultValue);
        protected PropertyBoolValue GetParallelProperty(bool defaultValue) => new PropertyBoolValue("P", StyleChanged, defaultValue);
        protected PropertyBoolValue GetUseSecondColorProperty(bool defaultValue) => new PropertyBoolValue("USC", StyleChanged, defaultValue);
        protected PropertyValue<float> GetSquareSideProperty(float defaultValue) => new PropertyValue<float>("SS", StyleChanged, defaultValue);
        protected PropertyValue<int> GetLineCountProperty(int defaultValue) => new PropertyValue<int>("LC", StyleChanged, defaultValue);
        protected PropertyValue<float> GetAngleProperty(float defaultValue) => new PropertyValue<float>("A", StyleChanged, defaultValue);
        protected PropertyValue<float> GetStepProperty(float defaultValue) => new PropertyValue<float>("S", StyleChanged, defaultValue);
        protected PropertyValue<int> GetOutputProperty(int defaultValue) => new PropertyValue<int>("O", StyleChanged, defaultValue);
        protected PropertyValue<float> GetAngleBetweenProperty(float defaultValue) => new PropertyValue<float>("A", StyleChanged, defaultValue);
        protected PropertyEnumValue<ChevronFillerStyle.From> GetStartingFromProperty(ChevronFillerStyle.From defaultValue) => new PropertyEnumValue<ChevronFillerStyle.From>("SF", StyleChanged, defaultValue);
        protected PropertyValue<float> GetElevationProperty(float defaultValue) => new PropertyValue<float>("E", StyleChanged, defaultValue);
        protected PropertyValue<int> GetLeftRailAProperty(int defaultValue) => new PropertyValue<int>("LRA", StyleChanged, defaultValue);
        protected PropertyValue<int> GetLeftRailBProperty(int defaultValue) => new PropertyValue<int>("LRB", StyleChanged, defaultValue);
        protected PropertyValue<int> GetRightRailAProperty(int defaultValue) => new PropertyValue<int>("RRA", StyleChanged, defaultValue);
        protected PropertyValue<int> GetRightRailBProperty(int defaultValue) => new PropertyValue<int>("RRB", StyleChanged, defaultValue);
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

            [NotVisible]
            LineBuffer = EmptyLine - 1,

            [Description(nameof(Localize.LineStyle_Empty))]
            [NotVisible]
            EmptyLine = Markup.Item.RegularLine * 2 - 1,

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

            [NotVisible]
            StopLineBuffer = Markup.Item.StopLine * 2 - 1,

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
            Filler3D = Filler | 0x80,

            [Description(nameof(Localize.FillerStyle_Pavement))]
            FillerPavement,

            [Description(nameof(Localize.FillerStyle_Grass))]
            FillerGrass,

            [NotVisible]
            FillerBuffer = Markup.Item.Filler * 2 - 1,

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

            [NotVisible]
            CrosswalkBuffer = Markup.Item.Crosswalk * 2 - 1,

            #endregion
        }
    }
    public abstract class Style<StyleType> : Style
        where StyleType : Style<StyleType>
    {
        public Style(Color32 color, float width) : base(color, width) { }

        public virtual void CopyTo(StyleType target) => base.CopyTo(target);
        public override sealed Style Copy() => CopyStyle();
        public abstract StyleType CopyStyle();
    }
}
