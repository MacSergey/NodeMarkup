using ColossalFramework.UI;
using IMT.API;
using IMT.UI;
using IMT.Utilities;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Linq;
using UnityEngine;

namespace IMT.Manager
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
    public interface IEffectStyle : IStyle
    {
        public PropertyVector2Value Cracks { get; }
        public PropertyVector2Value Voids { get; }
        public PropertyStructValue<float> Texture { get; }

        public float CracksDensity { get; }
        public Vector2 CracksTiling { get; }
        public float VoidDensity { get; }
        public Vector2 VoidTiling { get; }
        public float TextureDensity { get; }
    }

    public abstract class Style : IToXml
    {
        public static float DefaultDashLength => 1.5f;
        public static float DefaultSpaceLength => 1.5f;

        protected static string Length => string.Empty;
        protected static string Offset => string.Empty;

        public static bool FromXml<T>(XElement config, ObjectsMap map, bool invert, bool typeChanged, out T style) where T : Style
        {
            var type = IntToType(config.GetAttrValue<int>("T"));

            if (SingletonManager<StyleTemplateManager>.Instance.GetDefault<T>(type) is T defaultStyle)
            {
                style = defaultStyle;
                style.FromXml(config, map, invert, typeChanged);
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

        public static Color32 DefaultColor => new Color32(136, 136, 136, 224);
        public static float DefaultWidth => 0.15f;
        protected static Vector2 DefaultEffect => new Vector2(0f, 1f);
        protected static float DefaultTexture => 0f;

        protected virtual float WidthWheelStep => 0.01f;
        protected virtual float WidthMinValue => 0.05f;

        protected abstract Style GetDefault();
        public static T GetDefault<T>(StyleType type) where T : Style
        {
            return type.GetGroup() switch
            {
                StyleType.RegularLine => RegularLineStyle.GetDefault(type.ToEnum<RegularLineStyle.RegularLineType, StyleType>()) as T,
                StyleType.StopLine => StopLineStyle.GetDefault(type.ToEnum<StopLineStyle.StopLineType, StyleType>()) as T,
                StyleType.Filler => FillerStyle.GetDefault(type.ToEnum<FillerStyle.FillerType, StyleType>()) as T,
                StyleType.Crosswalk => CrosswalkStyle.GetDefault(type.ToEnum<CrosswalkStyle.CrosswalkType, StyleType>()) as T,
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
        public abstract MarkingLOD SupportLOD { get; }

        protected virtual void StyleChanged() => OnStyleChanged?.Invoke();

        public PropertyColorValue Color { get; }
        public PropertyStructValue<float> Width { get; }
        public PropertyVector2Value Cracks { get; }
        public PropertyVector2Value Voids { get; }
        public PropertyStructValue<float> Texture { get; }

        public float CracksDensity => Cracks.Value.x;
        public Vector2 CracksTiling => new Vector2(1f / Cracks.Value.y, 1f / Cracks.Value.y);
        public float VoidDensity => Voids.Value.x;
        public Vector2 VoidTiling => new Vector2(1f / Voids.Value.y, 1f / Voids.Value.y);
        public float TextureDensity => Texture.Value;

        public abstract IEnumerable<IStylePropertyData> Properties { get; }
        public abstract Dictionary<string, int> PropertyIndices { get; }
        protected static Dictionary<string, int> CreatePropertyIndices(IEnumerable<string> names)
        {
            var dic = new Dictionary<string, int>();
            foreach (var name in names)
            {
                dic[name] = dic.Count;
            }
            return dic;
        }

        public Style(Color32 color, float width, Vector2 cracks, Vector2 voids, float texture)
        {
            Color = GetColorProperty(color);
            Width = GetWidthProperty(width);
            Cracks = new PropertyVector2Value(StyleChanged, cracks, "ST", "SS");
            Voids = new PropertyVector2Value(StyleChanged, voids, "VT", "VS");
            Texture = new PropertyStructValue<float>("TEX", StyleChanged, texture);
        }
        public Style(Color32 color, float width) : this(color, width, DefaultEffect, DefaultEffect, DefaultTexture) { }

        public abstract Style Copy();
        protected void CopyTo(Style target)
        {
            if (this is IWidthStyle widthSource && target is IWidthStyle widthTarget)
                widthTarget.Width.Value = widthSource.Width;
            if (this is IColorStyle colorSource && target is IColorStyle colorTarget)
                colorTarget.Color.Value = colorSource.Color;
            if (this is IEffectStyle textureSource && target is IEffectStyle textureTarget)
            {
                textureTarget.Cracks.Value = textureSource.Cracks.Value;
                textureTarget.Voids.Value = textureSource.Voids.Value;
                textureTarget.Texture.Value = textureSource.Texture.Value;
            }
        }

        public virtual List<EditorItem> GetUIComponents(object editObject, UIComponent parent, bool isTemplate = false)
        {
            var components = new List<EditorItem>();

            if (this is IColorStyle)
                components.Add(AddColorProperty(parent, false));
            if (this is IWidthStyle)
                components.Add(AddWidthProperty(parent, false));
            if (this is IEffectStyle)
            {
                components.Add(GetCracks(parent, true));
                components.Add(GetVoids(parent, true));
                components.Add(GetTexture(parent, true));
            }

            return components;
        }
        public int GetUIComponentSortIndex(EditorItem item)
        {
            if (PropertyIndices.TryGetValue(item.name, out var index))
                return index;
            else
                return int.MaxValue;
        }
        private ColorAdvancedPropertyPanel AddColorProperty(UIComponent parent, bool canCollapse)
        {
            var colorProperty = ComponentPool.Get<ColorAdvancedPropertyPanel>(parent, nameof(Color));
            colorProperty.Text = Localize.StyleOption_Color;
            colorProperty.WheelTip = Settings.ShowToolTip;
            colorProperty.CanCollapse = canCollapse;
            colorProperty.Init(GetDefault()?.Color);
            colorProperty.Value = Color;
            colorProperty.OnValueChanged += (Color32 color) => Color.Value = color;

            return colorProperty;
        }
        private FloatPropertyPanel AddWidthProperty(UIComponent parent, bool canCollapse)
        {
            var widthProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(Width));
            widthProperty.Text = Localize.StyleOption_Width;
            widthProperty.Format = Localize.NumberFormat_Meter;
            widthProperty.UseWheel = true;
            widthProperty.WheelStep = WidthWheelStep;
            widthProperty.WheelTip = Settings.ShowToolTip;
            widthProperty.CheckMin = true;
            widthProperty.MinValue = WidthMinValue;
            widthProperty.CanCollapse = canCollapse;
            widthProperty.Init();
            widthProperty.Value = Width;
            widthProperty.OnValueChanged += (float value) => Width.Value = value;

            return widthProperty;
        }
        private Vector2PropertyPanel GetCracks(UIComponent parent, bool canCollapse)
        {
            var cracksProperty = ComponentPool.Get<Vector2PropertyPanel>(parent, nameof(Cracks));
            cracksProperty.Text = Localize.StyleOption_Cracks;
            cracksProperty.SetLabels(Localize.StyleOption_Density, Localize.StyleOption_Scale);
            cracksProperty.Format = Localize.NumberFormat_Percent;
            cracksProperty.FieldsWidth = 50f;
            cracksProperty.CanCollapse = canCollapse;
            cracksProperty.CheckMax = true;
            cracksProperty.CheckMin = true;
            cracksProperty.MinValue = new Vector2(0f, 10f);
            cracksProperty.MaxValue = new Vector2(100f, 1000f);
            cracksProperty.WheelStep = new Vector2(10f, 10f);
            cracksProperty.UseWheel = true;
            cracksProperty.Init(0, 1);
            cracksProperty.Value = Cracks.Value * 100f;
            cracksProperty.OnValueChanged += (Vector2 value) => Cracks.Value = value * 0.01f;
            return cracksProperty;
        }
        private Vector2PropertyPanel GetVoids(UIComponent parent, bool canCollapse)
        {
            var voidProperty = ComponentPool.Get<Vector2PropertyPanel>(parent, nameof(Voids));
            voidProperty.Text = Localize.StyleOption_Voids;
            voidProperty.SetLabels(Localize.StyleOption_Density, Localize.StyleOption_Scale);
            voidProperty.Format = Localize.NumberFormat_Percent;
            voidProperty.FieldsWidth = 50f;
            voidProperty.CanCollapse = canCollapse;
            voidProperty.CheckMax = true;
            voidProperty.CheckMin = true;
            voidProperty.MinValue = new Vector2(0f, 10f);
            voidProperty.MaxValue = new Vector2(100f, 1000f);
            voidProperty.WheelStep = new Vector2(10f, 10f);
            voidProperty.UseWheel = true;
            voidProperty.Init(0, 1);
            voidProperty.Value = Voids.Value * 100f;
            voidProperty.OnValueChanged += (Vector2 value) => Voids.Value = value * 0.01f;
            return voidProperty;
        }
        private FloatPropertyPanel GetTexture(UIComponent parent, bool canCollapse)
        {
            var textureProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(Texture));
            textureProperty.Text = Localize.StyleOption_Texture;
            textureProperty.Format = Localize.NumberFormat_Percent;
            textureProperty.CanCollapse = canCollapse;
            textureProperty.CheckMax = true;
            textureProperty.CheckMin = true;
            textureProperty.MinValue = 0f;
            textureProperty.MaxValue = 100f;
            textureProperty.WheelStep = 10f;
            textureProperty.UseWheel = true;
            textureProperty.Init();
            textureProperty.Value = Texture.Value * 100f;
            textureProperty.OnValueChanged += (float value) => Texture.Value = value / 100f;
            return textureProperty;
        }

        protected Vector2PropertyPanel AddLengthProperty(IDashedLine dashedStyle, UIComponent parent, bool canCollapse)
        {
            var lengthProperty = ComponentPool.Get<Vector2PropertyPanel>(parent, nameof(Length));
            lengthProperty.Text = Localize.StyleOption_Length;
            lengthProperty.FieldsWidth = 50f;
            lengthProperty.SetLabels(Localize.StyleOption_Dash, Localize.StyleOption_Space);
            lengthProperty.Format = Localize.NumberFormat_Meter;
            lengthProperty.UseWheel = true;
            lengthProperty.WheelStep = new Vector2(0.1f, 0.1f);
            lengthProperty.WheelTip = Settings.ShowToolTip;
            lengthProperty.CheckMin = true;
            lengthProperty.MinValue = new Vector2(0.1f, 0.1f);
            lengthProperty.CanCollapse = canCollapse;
            lengthProperty.Init(0, 1);
            lengthProperty.Value = new Vector2(dashedStyle.DashLength, dashedStyle.SpaceLength);
            lengthProperty.OnValueChanged += (Vector2 value) =>
            {
                dashedStyle.DashLength.Value = value.x;
                dashedStyle.SpaceLength.Value = value.y;
            };

            return lengthProperty;
        }
        protected FloatPropertyPanel AddDashLengthProperty(IDashedLine dashedStyle, UIComponent parent, bool canCollapse)
        {
            var dashLengthProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(dashedStyle.DashLength));
            dashLengthProperty.Text = Localize.StyleOption_DashedLength;
            dashLengthProperty.Format = Localize.NumberFormat_Meter;
            dashLengthProperty.UseWheel = true;
            dashLengthProperty.WheelStep = 0.1f;
            dashLengthProperty.WheelTip = Settings.ShowToolTip;
            dashLengthProperty.CheckMin = true;
            dashLengthProperty.MinValue = 0.1f;
            dashLengthProperty.CanCollapse = canCollapse;
            dashLengthProperty.Init();
            dashLengthProperty.Value = dashedStyle.DashLength;
            dashLengthProperty.OnValueChanged += (float value) => dashedStyle.DashLength.Value = value;

            return dashLengthProperty;
        }
        protected FloatPropertyPanel AddSpaceLengthProperty(IDashedLine dashedStyle, UIComponent parent, bool canCollapse)
        {
            var spaceLengthProperty = ComponentPool.Get<FloatPropertyPanel>(parent, nameof(dashedStyle.SpaceLength));
            spaceLengthProperty.Text = Localize.StyleOption_SpaceLength;
            spaceLengthProperty.Format = Localize.NumberFormat_Meter;
            spaceLengthProperty.UseWheel = true;
            spaceLengthProperty.WheelStep = 0.1f;
            spaceLengthProperty.WheelTip = Settings.ShowToolTip;
            spaceLengthProperty.CheckMin = true;
            spaceLengthProperty.MinValue = 0.1f;
            spaceLengthProperty.CanCollapse = canCollapse;
            spaceLengthProperty.Init();
            spaceLengthProperty.Value = dashedStyle.SpaceLength;
            spaceLengthProperty.OnValueChanged += (float value) => dashedStyle.SpaceLength.Value = value;

            return spaceLengthProperty;
        }
        protected ButtonPanel AddInvertProperty(IAsymLine asymStyle, UIComponent parent, bool canCollapse)
        {
            var buttonsPanel = ComponentPool.Get<ButtonPanel>(parent, nameof(asymStyle.Invert));
            buttonsPanel.Text = Localize.StyleOption_Invert;
            buttonsPanel.CanCollapse = canCollapse;
            buttonsPanel.Init();
            buttonsPanel.OnButtonClick += OnButtonClick;

            void OnButtonClick() => asymStyle.Invert.Value = !asymStyle.Invert;

            return buttonsPanel;
        }

        protected XElement BaseToXml() => new XElement(XmlSection, new XAttribute("T", TypeToInt(Type)));
        public virtual XElement ToXml()
        {
            var config = BaseToXml();
            Color.ToXml(config);
            Width.ToXml(config);
            if (this is IEffectStyle)
            {
                Cracks.ToXml(config);
                Voids.ToXml(config);
            }
            return config;
        }
        public virtual void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            Color.FromXml(config, DefaultColor);
            Width.FromXml(config, DefaultWidth);
            if (this is IEffectStyle)
            {
                Cracks.FromXml(config, DefaultEffect);
                Voids.FromXml(config, DefaultEffect);
            }
        }

        protected enum PropertyNames
        {
            C, //Color
            SC, //SecondColor
            W, //Width
            O, //Offset
            MO, //MedianOffset
            A, //Alignment, Angle
            DL, //Dash length
            SL, //Space length
            I, //Invert
            CS, //Solid in center
            B, //Base
            H, //Height
            S, //Space
            OB, //OffsetBefore
            OA, //OffsetAfter
            LW, //Crosswalk line width
            P, //Parallel
            USC, //Use second color
            UG, //Use gap
            GL, //Gap length
            GP, //Gap period
            SS, //Square side
            LC, //Line count

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
        protected PropertyBoolValue GetTwoColorsProperty(bool defaultValue) => new PropertyBoolValue("USC", StyleChanged, defaultValue);
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
        protected PropertyStructValue<int> GetLeftGuideAProperty(int defaultValue) => new PropertyStructValue<int>("LRA", StyleChanged, defaultValue);
        protected PropertyStructValue<int> GetLeftGuideBProperty(int defaultValue) => new PropertyStructValue<int>("LRB", StyleChanged, defaultValue);
        protected PropertyStructValue<int> GetRightGuideAProperty(int defaultValue) => new PropertyStructValue<int>("RRA", StyleChanged, defaultValue);
        protected PropertyStructValue<int> GetRightGuideBProperty(int defaultValue) => new PropertyStructValue<int>("RRB", StyleChanged, defaultValue);
        protected PropertyBoolValue GetFollowGuidesProperty(bool defaultValue) => new PropertyBoolValue("FR", StyleChanged, defaultValue);

        public enum StyleType
        {
            [NotItem]
            ItemMask = 0xFF,
            [NotItem]
            GroupMask = ~ItemMask,

#region REGULAR

            [NotItem]
            [Description(nameof(Localize.LineStyle_RegularLinesGroup))]
            RegularLine = Marking.Item.RegularLine,

            [Description(nameof(Localize.LineStyle_Solid))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            LineSolid,

            [Description(nameof(Localize.LineStyle_Dashed))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            LineDashed,

            [Description(nameof(Localize.LineStyle_DoubleSolid))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            LineDoubleSolid,

            [Description(nameof(Localize.LineStyle_DoubleDashed))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            LineDoubleDashed,

            [Description(nameof(Localize.LineStyle_SolidAndDashed))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            LineSolidAndDashed,

            [Description(nameof(Localize.LineStyle_SharkTeeth))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            LineSharkTeeth,

            [Description(nameof(Localize.LineStyle_DoubleDashedAsym))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            LineDoubleDashedAsym,

            [Description(nameof(Localize.LineStyle_ZigZag))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular)]
            LineZigZag,

            [NotItem]
            Regular3DLine = Marking.Item.RegularLine + 0x80,

            [Description(nameof(Localize.LineStyle_Pavement))]
            [NetworkType(NetworkType.All)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            LinePavement,

            [NotItem]
            RegularPropLine = Regular3DLine + 0x10,

            [Description(nameof(Localize.LineStyle_Prop))]
            [NetworkType(NetworkType.All)]
            [LineType(LineType.Regular | LineType.Crosswalk | LineType.Lane)]
            LineProp,

            [Description(nameof(Localize.LineStyle_Tree))]
            [NetworkType(NetworkType.All)]
            [LineType(LineType.Regular | LineType.Crosswalk | LineType.Lane)]
            LineTree,

            [Description(nameof(Localize.LineStyle_Text))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk | LineType.Lane)]
            LineText,


            [NotItem]
            RegularNetworkLine = Regular3DLine + 0x20,

            [Description(nameof(Localize.LineStyle_Network))]
            [NetworkType(NetworkType.All)]
            [LineType(LineType.Regular | LineType.Crosswalk | LineType.Lane)]
            LineNetwork,

            [Description(nameof(Localize.LineStyle_Empty))]
            [NetworkType(NetworkType.All)]
            [LineType(LineType.Regular | LineType.Crosswalk | LineType.Lane)]
            [NotVisible]
            EmptyLine = LineBuffer - 1,

            [Description(nameof(Localize.Style_FromClipboard))]
            [NetworkType(NetworkType.All)]
            [LineType(LineType.Regular | LineType.Crosswalk | LineType.Lane)]
            [NotVisible]
            LineBuffer = Marking.Item.RegularLine + 0x100 - 1,

#endregion

#region STOP

            [NotItem]
            [Description(nameof(Localize.LineStyle_StopLinesGroup))]
            [NetworkType(NetworkType.Road)]
            StopLine = Marking.Item.StopLine,

            [Description(nameof(Localize.LineStyle_StopSolid))]
            [NetworkType(NetworkType.Road)]
            StopLineSolid,

            [Description(nameof(Localize.LineStyle_StopDashed))]
            [NetworkType(NetworkType.Road)]
            StopLineDashed,

            [Description(nameof(Localize.LineStyle_StopDouble))]
            [NetworkType(NetworkType.Road)]
            StopLineDoubleSolid,

            [Description(nameof(Localize.LineStyle_StopDoubleDashed))]
            [NetworkType(NetworkType.Road)]
            StopLineDoubleDashed,

            [Description(nameof(Localize.LineStyle_StopSolidAndDashed))]
            [NetworkType(NetworkType.Road)]
            StopLineSolidAndDashed,

            [Description(nameof(Localize.LineStyle_StopSharkTeeth))]
            [NetworkType(NetworkType.Road)]
            StopLineSharkTeeth,

            [Description(nameof(Localize.LineStyle_StopPavement))]
            [NetworkType(NetworkType.Road)]
            StopLinePavement,

            [Description(nameof(Localize.Style_FromClipboard))]
            [NetworkType(NetworkType.Road)]
            [NotVisible]
            StopLineBuffer = Marking.Item.StopLine + 0x100 - 1,

#endregion

#region FILLER

            [NotItem]
            [Description(nameof(Localize.FillerStyle_Group))]
            Filler = Marking.Item.Filler,

            [Description(nameof(Localize.FillerStyle_Stripe))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            FillerStripe,

            [Description(nameof(Localize.FillerStyle_Grid))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            FillerGrid,

            [Description(nameof(Localize.FillerStyle_Solid))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            FillerSolid,

            [Description(nameof(Localize.FillerStyle_Chevron))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            FillerChevron,

            [NotItem]
            Filler3D = Filler + 0x80,

            [Description(nameof(Localize.FillerStyle_Pavement))]
            [NetworkType(NetworkType.All)]
            FillerPavement,

            [Description(nameof(Localize.FillerStyle_Grass))]
            [NetworkType(NetworkType.All)]
            FillerGrass,

            [Description(nameof(Localize.FillerStyle_Gravel))]
            [NetworkType(NetworkType.All)]
            FillerGravel,

            [Description(nameof(Localize.FillerStyle_Ruined))]
            [NetworkType(NetworkType.All)]
            FillerRuined,

            [Description(nameof(Localize.FillerStyle_Cliff))]
            [NetworkType(NetworkType.All)]
            FillerCliff,

            [Description(nameof(Localize.Style_FromClipboard))]
            [NetworkType(NetworkType.All)]
            [NotVisible]
            FillerBuffer = Marking.Item.Filler + 0x100 - 1,

#endregion

#region CROSSWALK

            [NotItem]
            [Description(nameof(Localize.CrosswalkStyle_Group))]
            Crosswalk = Marking.Item.Crosswalk,

            [Description(nameof(Localize.CrosswalkStyle_Existent))]
            [NetworkType(NetworkType.Road)]
            CrosswalkExistent,

            [Description(nameof(Localize.CrosswalkStyle_Zebra))]
            [NetworkType(NetworkType.Road)]
            CrosswalkZebra,

            [Description(nameof(Localize.CrosswalkStyle_DoubleZebra))]
            [NetworkType(NetworkType.Road)]
            CrosswalkDoubleZebra,

            [Description(nameof(Localize.CrosswalkStyle_ParallelSolidLines))]
            [NetworkType(NetworkType.Road)]
            CrosswalkParallelSolidLines,

            [Description(nameof(Localize.CrosswalkStyle_ParallelDashedLines))]
            [NetworkType(NetworkType.Road)]
            CrosswalkParallelDashedLines,

            [Description(nameof(Localize.CrosswalkStyle_Ladder))]
            [NetworkType(NetworkType.Road)]
            CrosswalkLadder,

            [Description(nameof(Localize.CrosswalkStyle_Solid))]
            [NetworkType(NetworkType.Road)]
            CrosswalkSolid,

            [Description(nameof(Localize.CrosswalkStyle_ChessBoard))]
            [NetworkType(NetworkType.Road)]
            CrosswalkChessBoard,

            [Description(nameof(Localize.Style_FromClipboard))]
            [NetworkType(NetworkType.Road)]
            [NotVisible]
            CrosswalkBuffer = Marking.Item.Crosswalk + 0x100 - 1,

#endregion
        }
    }
    public abstract class Style<StyleType> : Style
        where StyleType : Style<StyleType>
    {
        public Style(Color32 color, float width, Vector2 cracks, Vector2 voids, float texture) : base(color, width, cracks, voids, texture) { }
        public Style(Color32 color, float width) : base(color, width) { }

        public virtual void CopyTo(StyleType target) => base.CopyTo(target);
        public sealed override Style Copy() => CopyStyle();
        public abstract StyleType CopyStyle();

        protected sealed override Style GetDefault() => GetDefaultStyle();
        protected StyleType GetDefaultStyle() => SingletonManager<StyleTemplateManager>.Instance.GetDefault<StyleType>(Type);
    }

}
