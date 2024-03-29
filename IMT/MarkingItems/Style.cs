﻿using ColossalFramework.DataBinding;
using ColossalFramework.UI;
using IMT.API;
using IMT.UI;
using IMT.UI.Editors;
using IMT.Utilities;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Xml.Linq;
using UnityEngine;
using static RenderManager;

namespace IMT.Manager
{
    public interface IStyle { }
    public interface IColorStyle : IStyle
    {
        PropertyColorValue Color { get; }     
        bool KeepColor { get; }
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

        public static Color32 DefaultMarkingColor => new Color32(136, 136, 136, 224);
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
                StyleType.Filler => BaseFillerStyle.GetDefault(type.ToEnum<BaseFillerStyle.FillerType, StyleType>()) as T,
                StyleType.Crosswalk => BaseCrosswalkStyle.GetDefault(type.ToEnum<BaseCrosswalkStyle.CrosswalkType, StyleType>()) as T,
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
        protected virtual Color32 DefaultColor => GetDefault()?.Color;
        public PropertyStructValue<float> Width { get; }
        public PropertyVector2Value Cracks { get; }
        public PropertyVector2Value Voids { get; }
        public PropertyStructValue<float> Texture { get; }

        public float CracksDensity => Cracks.Value.x;
        public Vector2 CracksTiling => new Vector2(1f / Cracks.Value.y, 1f / Cracks.Value.y);
        public float VoidDensity => Voids.Value.x;
        public Vector2 VoidTiling => new Vector2(1f / Voids.Value.y, 1f / Voids.Value.y);
        public float TextureDensity => Texture.Value;

        public EffectData Effects
        {
            get
            {
                if (this is IEffectStyle)
                    return new EffectData(Texture, Cracks, Voids);
                else
                    return new EffectData(DefaultTexture, DefaultEffect, DefaultEffect);
            }
            set
            {
                if(this is IEffectStyle)
                {
                    Texture.Value = value.texture;
                    Cracks.Value = value.cracks;
                    Voids.Value = value.voids;
                }
            }
        }

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

        protected IPropertyCategoryInfo MainCategory { get; } = new PropertyCategoryInfo<DefaultPropertyCategoryPanel>("Main", Localize.StyleOptionCategory_Main, true);
        protected IPropertyCategoryInfo AdditionalCategory { get; } = new PropertyCategoryInfo<DefaultPropertyCategoryPanel>("Additional", Localize.StyleOptionCategory_Additional, false);
        protected IPropertyCategoryInfo EffectCategory { get; } = new PropertyCategoryInfo<EffectPropertyCategoryPanel>("Effect", Localize.StyleOptionCategory_Effect, false);
#if DEBUG
        protected IPropertyCategoryInfo DebugCategory { get; } = new PropertyCategoryInfo<DefaultPropertyCategoryPanel>("Debug", "Debug", false);
#endif

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
            if (this is IColorStyle colorSource && target is IColorStyle colorTarget && colorTarget.KeepColor)
                colorTarget.Color.Value = colorSource.Color;

            CopyEffectsTo(target);
        }
        public void CopyEffectsTo(Style target)
        {
            if (this is IEffectStyle textureSource && target is IEffectStyle textureTarget)
            {
                textureTarget.Cracks.Value = textureSource.Cracks.Value;
                textureTarget.Voids.Value = textureSource.Voids.Value;
                textureTarget.Texture.Value = textureSource.Texture.Value;
            }
        }

        public virtual void GetUIComponents(EditorProvider provider)
        {
            if (this is IColorStyle)
                provider.AddProperty(new PropertyInfo<IMTColorPropertyPanel>(this, nameof(Color), MainCategory, AddColorProperty, RefreshColorProperty));
            if (this is IWidthStyle)
                provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Width), MainCategory, AddWidthProperty, RefreshWidthProperty));
            if (this is IEffectStyle)
            {
                provider.AddProperty(new PropertyInfo<Vector2PropertyPanel>(this, nameof(Cracks), EffectCategory, AddCracksProperty, RefreshCracksProperty));
                provider.AddProperty(new PropertyInfo<Vector2PropertyPanel>(this, nameof(Voids), EffectCategory, AddVoidsProperty, RefreshVoidsProperty));
                provider.AddProperty(new PropertyInfo<FloatPropertyPanel>(this, nameof(Texture), EffectCategory, AddTextureProperty, RefreshTextureProperty));
            }
        }
        public virtual void GetUICategories(EditorProvider provider)
        {
            provider.AddCategory(MainCategory);
            provider.AddCategory(AdditionalCategory);
            provider.AddCategory(EffectCategory);
#if DEBUG
            provider.AddCategory(DebugCategory);
#endif
        }

        public int GetPropertyIndex(string name)
        {
            if (PropertyIndices.TryGetValue(name, out var index))
                return index;
            else
                return int.MaxValue;
        }

        private void AddColorProperty(IMTColorPropertyPanel colorProperty, EditorProvider provider)
        {
            colorProperty.Label = Localize.StyleOption_Color;
            colorProperty.WheelTip = Settings.ShowToolTip;
            colorProperty.Init(DefaultColor);
            colorProperty.Value = Color;
            colorProperty.OnValueChanged += (Color32 color) => Color.Value = color;
        }
        protected virtual void RefreshColorProperty(IMTColorPropertyPanel colorProperty, EditorProvider provider)
        {
            colorProperty.Value = Color;
        }

        private void AddWidthProperty(FloatPropertyPanel widthProperty, EditorProvider provider)
        {
            widthProperty.Label = Localize.StyleOption_Width;
            widthProperty.Format = Localize.NumberFormat_Meter;
            widthProperty.UseWheel = true;
            widthProperty.WheelStep = WidthWheelStep;
            widthProperty.WheelTip = Settings.ShowToolTip;
            widthProperty.CheckMin = true;
            widthProperty.MinValue = WidthMinValue;
            widthProperty.Init();
            widthProperty.Value = Width;
            widthProperty.OnValueChanged += (float value) => Width.Value = value;
        }
        protected virtual void RefreshWidthProperty(FloatPropertyPanel widthProperty, EditorProvider provider)
        {
            widthProperty.Value = Width;
        }

        private void AddCracksProperty(Vector2PropertyPanel cracksProperty, EditorProvider provider)
        {
            cracksProperty.Label = Localize.StyleOption_Cracks;
            cracksProperty.SetLabels(Localize.StyleOption_Density, Localize.StyleOption_Scale);
            cracksProperty.Format = Localize.NumberFormat_Percent;
            cracksProperty.FieldsWidth = 50f;
            cracksProperty.CheckMax = true;
            cracksProperty.CheckMin = true;
            cracksProperty.MinValue = new Vector2(0f, 10f);
            cracksProperty.MaxValue = new Vector2(100f, 1000f);
            cracksProperty.WheelStep = new Vector2(10f, 10f);
            cracksProperty.UseWheel = true;
            cracksProperty.Init(0, 1);
            cracksProperty.OnValueChanged += (Vector2 value) => Cracks.Value = value * 0.01f;
        }
        protected virtual void RefreshCracksProperty(Vector2PropertyPanel cracksProperty, EditorProvider provider)
        {
            cracksProperty.Value = Cracks.Value * 100f;
        }

        private void AddVoidsProperty(Vector2PropertyPanel voidProperty, EditorProvider provider)
        {
            voidProperty.Label = Localize.StyleOption_Voids;
            voidProperty.SetLabels(Localize.StyleOption_Density, Localize.StyleOption_Scale);
            voidProperty.Format = Localize.NumberFormat_Percent;
            voidProperty.FieldsWidth = 50f;
            voidProperty.CheckMax = true;
            voidProperty.CheckMin = true;
            voidProperty.MinValue = new Vector2(0f, 10f);
            voidProperty.MaxValue = new Vector2(100f, 1000f);
            voidProperty.WheelStep = new Vector2(10f, 10f);
            voidProperty.UseWheel = true;
            voidProperty.Init(0, 1);
            voidProperty.OnValueChanged += (Vector2 value) => Voids.Value = value * 0.01f;
        }
        protected virtual void RefreshVoidsProperty(Vector2PropertyPanel voidProperty, EditorProvider provider)
        {
            voidProperty.Value = Voids.Value * 100f;
        }

        private void AddTextureProperty(FloatPropertyPanel textureProperty, EditorProvider provider)
        {
            textureProperty.Label = Localize.StyleOption_Texture;
            textureProperty.Format = Localize.NumberFormat_Percent;
            textureProperty.CheckMax = true;
            textureProperty.CheckMin = true;
            textureProperty.MinValue = 0f;
            textureProperty.MaxValue = 100f;
            textureProperty.WheelStep = 10f;
            textureProperty.UseWheel = true;
            textureProperty.Init();
            textureProperty.OnValueChanged += (float value) => Texture.Value = value / 100f;
        }
        protected virtual void RefreshTextureProperty(FloatPropertyPanel textureProperty, EditorProvider provider)
        {
            textureProperty.Value = Texture.Value * 100f;
        }

        protected void AddLengthProperty(Vector2PropertyPanel lengthProperty, EditorProvider provider)
        {
            if (this is IDashedLine dashedStyle)
            {
                lengthProperty.Label = Localize.StyleOption_Length;
                lengthProperty.FieldsWidth = 50f;
                lengthProperty.SetLabels(Localize.StyleOption_Dash, Localize.StyleOption_Space);
                lengthProperty.Format = Localize.NumberFormat_Meter;
                lengthProperty.UseWheel = true;
                lengthProperty.WheelStep = new Vector2(0.1f, 0.1f);
                lengthProperty.WheelTip = Settings.ShowToolTip;
                lengthProperty.CheckMin = true;
                lengthProperty.MinValue = new Vector2(0.1f, 0.1f);
                lengthProperty.Init(0, 1);
                lengthProperty.Value = new Vector2(dashedStyle.DashLength, dashedStyle.SpaceLength);
                lengthProperty.OnValueChanged += (Vector2 value) =>
                {
                    dashedStyle.DashLength.Value = value.x;
                    dashedStyle.SpaceLength.Value = value.y;
                };
            }
            else
                throw new NotSupportedException();
        }
        protected virtual void RefreshLengthProperty(Vector2PropertyPanel lengthProperty, EditorProvider provider)
        {
            if (this is IDashedLine dashedStyle)
                lengthProperty.Value = new Vector2(dashedStyle.DashLength, dashedStyle.SpaceLength);
            else
                lengthProperty.IsHidden = true;
        }

        protected void AddSpaceLengthProperty(FloatPropertyPanel spaceLengthProperty, EditorProvider provider)
        {
            if (this is IDashedLine dashedStyle)
            {
                spaceLengthProperty.Label = Localize.StyleOption_SpaceLength;
                spaceLengthProperty.Format = Localize.NumberFormat_Meter;
                spaceLengthProperty.UseWheel = true;
                spaceLengthProperty.WheelStep = 0.1f;
                spaceLengthProperty.WheelTip = Settings.ShowToolTip;
                spaceLengthProperty.CheckMin = true;
                spaceLengthProperty.MinValue = 0.1f;
                spaceLengthProperty.Init();
                spaceLengthProperty.Value = dashedStyle.SpaceLength;
                spaceLengthProperty.OnValueChanged += (float value) => dashedStyle.SpaceLength.Value = value;
            }
            else
                throw new NotSupportedException();
        }
        protected virtual void RefreshSpaceLengthProperty(FloatPropertyPanel spaceLengthProperty, EditorProvider provider)
        {
            if (this is IDashedLine dashedStyle)
                spaceLengthProperty.Value = dashedStyle.SpaceLength;
            else
                spaceLengthProperty.IsHidden = true;
        }

        protected void AddInvertProperty(ButtonPanel buttonsPanel, EditorProvider provider)
        {
            if (this is IAsymLine asymStyle)
            {
                buttonsPanel.Text = Localize.StyleOption_Invert;
                buttonsPanel.Init();
                buttonsPanel.OnButtonClick += OnButtonClick;

                void OnButtonClick() => asymStyle.Invert.Value = !asymStyle.Invert;
            }
            else
                throw new NotSupportedException();
        }
        protected virtual void RefreshInvertProperty(ButtonPanel buttonsPanel, EditorProvider provider)
        {
            buttonsPanel.IsHidden = this is not IAsymLine;
        }

        protected XElement BaseToXml() => new XElement(XmlSection, new XAttribute("T", TypeToInt(Type)));
        public virtual XElement ToXml()
        {
            var config = BaseToXml();
            Color.ToXml(config);
            Width.ToXml(config);
            if (this is IEffectStyle)
            {
                Texture.ToXml(config); 
                Cracks.ToXml(config);
                Voids.ToXml(config);
            }
            return config;
        }
        public virtual void FromXml(XElement config, ObjectsMap map, bool invert, bool typeChanged)
        {
            Color.FromXml(config, DefaultMarkingColor);
            Width.FromXml(config, DefaultWidth);
            if (this is IEffectStyle)
            {
                Texture.FromXml(config, DefaultTexture);
                Cracks.FromXml(config, DefaultEffect);
                Voids.FromXml(config, DefaultEffect);
            }
        }

        public virtual void GetUsedAssets(HashSet<string> networks, HashSet<string> props, HashSet<string> trees) { }

        public override string ToString() => Type.ToString();

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
            [NotVisible]
            ItemMask = 0xFF,

            [NotItem]
            [NotVisible]
            GroupMask = ~ItemMask,

            #region REGULAR

            [NotItem]
            [NotVisible]
            [Description(nameof(Localize.LineStyle_RegularLinesGroup))]
            [Sprite(nameof(RegularLine), "Group")]
            RegularLine = Marking.Item.RegularLine,

            [Description(nameof(Localize.LineStyle_Solid))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            [Sprite(nameof(LineSolid))]
            [Sprite(nameof(LineSolid), "Group")]
            LineSolid,

            [Description(nameof(Localize.LineStyle_Dashed))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            [Sprite(nameof(LineDashed))]
            [Sprite(nameof(LineDashed), "Group")]
            LineDashed,

            [Description(nameof(Localize.LineStyle_DoubleSolid))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            [Sprite(nameof(LineDoubleSolid))]
            [Sprite(nameof(LineDoubleSolid), "Group")]
            LineDoubleSolid,

            [Description(nameof(Localize.LineStyle_DoubleDashed))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            [Sprite(nameof(LineDoubleDashed))]
            [Sprite(nameof(LineDoubleDashed), "Group")]
            LineDoubleDashed,

            [Description(nameof(Localize.LineStyle_SolidAndDashed))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            [Sprite(nameof(LineSolidAndDashed))]
            [Sprite(nameof(LineSolidAndDashed), "Group")]
            LineSolidAndDashed,

            [Description(nameof(Localize.LineStyle_SharkTeeth))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            [Sprite(nameof(LineSharkTeeth))]
            [Sprite(nameof(LineSharkTeeth), "Group")]
            LineSharkTeeth,

            [Description(nameof(Localize.LineStyle_DoubleDashedAsym))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            [Sprite(nameof(LineDoubleDashedAsym))]
            [Sprite(nameof(LineDoubleDashedAsym), "Group")]
            LineDoubleDashedAsym,

            [Description(nameof(Localize.LineStyle_ZigZag))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular)]
            [Sprite(nameof(LineZigZag))]
            [Sprite(nameof(LineZigZag), "Group")]
            LineZigZag,


            [NotItem]
            [NotVisible]
            Regular3DLine = Marking.Item.RegularLine + 0x80,

            [Description(nameof(Localize.LineStyle_Pavement))]
            [NetworkType(NetworkType.All)]
            [LineType(LineType.Regular | LineType.Crosswalk)]
            [Sprite(nameof(LinePavement))]
            [Sprite(nameof(LinePavement), "Group")]
            LinePavement,


            [NotItem]
            [NotVisible]
            RegularObjectLine = Regular3DLine + 0x10,

            [Description(nameof(Localize.LineStyle_Prop))]
            [NetworkType(NetworkType.All)]
            [LineType(LineType.Regular | LineType.Crosswalk | LineType.Lane)]
            [Sprite(nameof(LineProp))]
            [Sprite(nameof(LineProp), "Group")]
            LineProp,

            [Description(nameof(Localize.LineStyle_Tree))]
            [NetworkType(NetworkType.All)]
            [LineType(LineType.Regular | LineType.Crosswalk | LineType.Lane)]
            [Sprite(nameof(LineTree))]
            [Sprite(nameof(LineTree), "Group")]
            LineTree,

            [Description(nameof(Localize.LineStyle_Text))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk | LineType.Lane)]
            [Sprite(nameof(LineText))]
            [Sprite(nameof(LineText), "Group")]
            LineText,

            [Description(nameof(Localize.LineStyle_Decal))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [LineType(LineType.Regular | LineType.Crosswalk | LineType.Lane)]
            [Sprite(nameof(LineDecal))]
            [Sprite(nameof(LineDecal), "Group")]
            LineDecal,


            [NotItem]
            [NotVisible]
            RegularNetworkLine = Regular3DLine + 0x20,

            [Description(nameof(Localize.LineStyle_Network))]
            [NetworkType(NetworkType.All)]
            [LineType(LineType.Regular | LineType.Crosswalk | LineType.Lane)]
            [Sprite(nameof(LineNetwork))]
            [Sprite(nameof(LineNetwork), "Group")]
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
            [NotVisible]
            [Description(nameof(Localize.LineStyle_StopLinesGroup))]
            [NetworkType(NetworkType.Road)]
            [Sprite(nameof(StopLine))]
            [Sprite(nameof(StopLine), "Group")]
            StopLine = Marking.Item.StopLine,

            [Description(nameof(Localize.LineStyle_StopSolid))]
            [NetworkType(NetworkType.Road)]
            [LineType(LineType.Stop)]
            [Sprite(nameof(StopLineSolid))]
            [Sprite(nameof(StopLineSolid), "Group")]
            StopLineSolid,

            [Description(nameof(Localize.LineStyle_StopDashed))]
            [NetworkType(NetworkType.Road)]
            [LineType(LineType.Stop)]
            [Sprite(nameof(StopLineDashed))]
            [Sprite(nameof(StopLineDashed), "Group")]
            StopLineDashed,

            [Description(nameof(Localize.LineStyle_StopDouble))]
            [NetworkType(NetworkType.Road)]
            [LineType(LineType.Stop)]
            [Sprite(nameof(StopLineDoubleSolid))]
            [Sprite(nameof(StopLineDoubleSolid), "Group")]
            StopLineDoubleSolid,

            [Description(nameof(Localize.LineStyle_StopDoubleDashed))]
            [NetworkType(NetworkType.Road)]
            [LineType(LineType.Stop)]
            [Sprite(nameof(StopLineDoubleDashed))]
            [Sprite(nameof(StopLineDoubleDashed), "Group")]
            StopLineDoubleDashed,

            [Description(nameof(Localize.LineStyle_StopSolidAndDashed))]
            [NetworkType(NetworkType.Road)]
            [LineType(LineType.Stop)]
            [Sprite(nameof(StopLineSolidAndDashed))]
            [Sprite(nameof(StopLineSolidAndDashed), "Group")]
            StopLineSolidAndDashed,

            [Description(nameof(Localize.LineStyle_StopSharkTeeth))]
            [NetworkType(NetworkType.Road)]
            [LineType(LineType.Stop)]
            [Sprite(nameof(StopLineSharkTeeth))]
            [Sprite(nameof(StopLineSharkTeeth), "Group")]
            StopLineSharkTeeth,

            [Description(nameof(Localize.LineStyle_StopPavement))]
            [NetworkType(NetworkType.Road)]
            [LineType(LineType.Stop)]
            [Sprite(nameof(StopLinePavement))]
            [Sprite(nameof(StopLinePavement), "Group")]
            StopLinePavement,

            [Description(nameof(Localize.Style_FromClipboard))]
            [NetworkType(NetworkType.Road)]
            [LineType(LineType.Stop)]
            [NotVisible]
            StopLineBuffer = Marking.Item.StopLine + 0x100 - 1,

            #endregion

            #region FILLER

            [NotItem]
            [NotVisible]
            [Description(nameof(Localize.FillerStyle_Group))]
            [Sprite(nameof(Filler), "Group")]
            Filler = Marking.Item.Filler,

            [Description(nameof(Localize.FillerStyle_Stripe))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [Sprite(nameof(FillerStripe))]
            [Sprite(nameof(FillerStripe), "Group")]
            FillerStripe,

            [Description(nameof(Localize.FillerStyle_Grid))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [Sprite(nameof(FillerGrid))]
            [Sprite(nameof(FillerGrid), "Group")]
            FillerGrid,

            [Description(nameof(Localize.FillerStyle_Solid))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [Sprite(nameof(FillerSolid))]
            [Sprite(nameof(FillerSolid), "Group")]
            FillerSolid,

            [Description(nameof(Localize.FillerStyle_Chevron))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [Sprite(nameof(FillerChevron))]
            [Sprite(nameof(FillerChevron), "Group")]
            FillerChevron,

            [Description(nameof(Localize.FillerStyle_Decal))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [Sprite(nameof(FillerDecal))]
            [Sprite(nameof(FillerDecal), "Group")]
            FillerDecal,

            [Description(nameof(Localize.FillerStyle_Asphalt))]
            [NetworkType(NetworkType.Road | NetworkType.Path | NetworkType.Taxiway)]
            [Sprite(nameof(FillerAsphalt))]
            [Sprite(nameof(FillerAsphalt), "Group")]
            FillerAsphalt,

            [NotItem]
            [NotVisible]
            Filler3D = Filler + 0x80,

            [Description(nameof(Localize.FillerStyle_PavementIsland))]
            [NetworkType(NetworkType.All)]
            [Sprite(nameof(FillerPavement))]
            [Sprite(nameof(FillerPavement), "Group")]
            FillerPavement,

            [Description(nameof(Localize.FillerStyle_GrassIsland))]
            [NetworkType(NetworkType.All)]
            [Sprite(nameof(FillerGrass))]
            [Sprite(nameof(FillerGrass), "Group")]
            FillerGrass,

            [Description(nameof(Localize.FillerStyle_GravelIsland))]
            [NetworkType(NetworkType.All)]
            [Sprite(nameof(FillerGravel))]
            [Sprite(nameof(FillerGravel), "Group")]
            FillerGravel,

            [Description(nameof(Localize.FillerStyle_RuinedIsland))]
            [NetworkType(NetworkType.All)]
            [Sprite(nameof(FillerRuined))]
            [Sprite(nameof(FillerRuined), "Group")]
            FillerRuined,

            [Description(nameof(Localize.FillerStyle_CliffIsland))]
            [NetworkType(NetworkType.All)]
            [Sprite(nameof(FillerCliff))]
            [Sprite(nameof(FillerCliff), "Group")]
            FillerCliff,

            [Description(nameof(Localize.FillerStyle_TextureIsland))]
            [NetworkType(NetworkType.All)]
            [Sprite(nameof(FillerTexture))]
            [Sprite(nameof(FillerTexture), "Group")]
            FillerTexture,

            [Description(nameof(Localize.Style_FromClipboard))]
            [NetworkType(NetworkType.All)]
            [NotVisible]
            FillerBuffer = Marking.Item.Filler + 0x100 - 1,

            #endregion

            #region CROSSWALK

            [NotItem]
            [NotVisible]
            [Description(nameof(Localize.CrosswalkStyle_Group))]
            [Sprite(nameof(Crosswalk), "Group")]
            Crosswalk = Marking.Item.Crosswalk,

            [Description(nameof(Localize.CrosswalkStyle_Existent))]
            [NetworkType(NetworkType.Road)]
            [Sprite(nameof(CrosswalkExistent))]
            [Sprite(nameof(CrosswalkExistent), "Group")]
            CrosswalkExistent,

            [Description(nameof(Localize.CrosswalkStyle_Zebra))]
            [NetworkType(NetworkType.Road)]
            [Sprite(nameof(CrosswalkZebra))]
            [Sprite(nameof(CrosswalkZebra), "Group")]
            CrosswalkZebra,

            [Description(nameof(Localize.CrosswalkStyle_DoubleZebra))]
            [NetworkType(NetworkType.Road)]
            [Sprite(nameof(CrosswalkDoubleZebra))]
            [Sprite(nameof(CrosswalkDoubleZebra), "Group")]
            CrosswalkDoubleZebra,

            [Description(nameof(Localize.CrosswalkStyle_ParallelSolidLines))]
            [NetworkType(NetworkType.Road)]
            [Sprite(nameof(CrosswalkParallelSolidLines))]
            [Sprite(nameof(CrosswalkParallelSolidLines), "Group")]
            CrosswalkParallelSolidLines,

            [Description(nameof(Localize.CrosswalkStyle_ParallelDashedLines))]
            [NetworkType(NetworkType.Road)]
            [Sprite(nameof(CrosswalkParallelDashedLines))]
            [Sprite(nameof(CrosswalkParallelDashedLines), "Group")]
            CrosswalkParallelDashedLines,

            [Description(nameof(Localize.CrosswalkStyle_Ladder))]
            [NetworkType(NetworkType.Road)]
            [Sprite(nameof(CrosswalkLadder))]
            [Sprite(nameof(CrosswalkLadder), "Group")]
            CrosswalkLadder,

            [Description(nameof(Localize.CrosswalkStyle_Solid))]
            [NetworkType(NetworkType.Road)]
            [Sprite(nameof(CrosswalkSolid))]
            [Sprite(nameof(CrosswalkSolid), "Group")]
            CrosswalkSolid,

            [Description(nameof(Localize.CrosswalkStyle_ChessBoard))]
            [NetworkType(NetworkType.Road)]
            [Sprite(nameof(CrosswalkChessBoard))]
            [Sprite(nameof(CrosswalkChessBoard), "Group")]
            CrosswalkChessBoard,

            [Description(nameof(Localize.CrosswalkStyle_Decal))]
            [NetworkType(NetworkType.Road)]
            [Sprite(nameof(CrosswalkDecal))]
            [Sprite(nameof(CrosswalkDecal), "Group")]
            CrosswalkDecal,

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

    public interface IPropertyInfo
    {
        string Name { get; }
        IPropertyCategoryInfo Category { get; }
        int SortIndex { get; }

        bool IsCollapsed { get; set; }
        bool IsHidden { get; set; }

        bool EnableControl { get; set; }

        BaseEditorPanel Create(EditorProvider editorProvider);
        void Refresh(EditorProvider editorProvider);
        void Destroy(EditorProvider editorProvider);
    }
    public interface IPropertyCategoryInfo
    {
        string Name { get; }
        string Text { get; }
        bool IsExpand { get; }

        CategoryItem Create(EditorProvider editorProvider);
    }

    public struct PropertyInfo<PropertyType> : IPropertyInfo
        where PropertyType : BaseEditorPanel, IReusable
    {
        public delegate void InitItem(PropertyType property, EditorProvider editorProvider);
        public delegate void RefreshItem(PropertyType property, EditorProvider editorProvider);

        public string Name { get; }
        public IPropertyCategoryInfo Category { get; }
        public int SortIndex { get; }

        public bool IsCollapsed
        {
            get => instance == null || instance.IsCollapsed;
            set
            {
                if (instance != null)
                    instance.IsCollapsed = value;
            }
        }

        public bool IsHidden
        {
            get => instance == null || instance.IsHidden;
            set
            {
                if (instance != null)
                    instance.IsHidden = value;
            }
        }
        public bool EnableControl
        {
            get => instance != null && instance.EnableControl;
            set
            {
                if (instance != null)
                    instance.EnableControl = value;
            }
        }


        private readonly InitItem init;
        private readonly RefreshItem refresh;
        private PropertyType instance;

        public PropertyInfo(Style style, string propertyName, IPropertyCategoryInfo categoryInfo, InitItem init, RefreshItem refresh = null)
        {
            Name = propertyName;
            Category = categoryInfo;
            SortIndex = style.GetPropertyIndex(Name);
            this.init = init;
            this.refresh = refresh;
            this.instance = null;
        }

        public BaseEditorPanel Create(EditorProvider editorProvider)
        {
            if (instance == null)
            {
                var property = editorProvider.GetItem<PropertyType>(Name);
                property.SetStyle(UIStyle.Default);
                init(property, editorProvider);
                instance = property;
            }
            return instance;
        }
        public void Destroy(EditorProvider editorProvider)
        {
            if (instance != null)
            {
                editorProvider.DestroyItem(instance);
                instance = null;
            }
        }
        public void Refresh(EditorProvider editorProvider)
        {
            if (instance != null && refresh != null)
            {
                refresh.Invoke(instance, editorProvider);
            }
        }

        public static int SortPredicate(IPropertyInfo x, IPropertyInfo y) => x.SortIndex - y.SortIndex;
        public override string ToString() => Name;
    }

    public readonly struct PropertyCategoryInfo<CategoryType> : IPropertyCategoryInfo
        where CategoryType : PropertyGroupPanel, IPropertyCategoryPanel, IReusable
    {
        public string Name { get; }
        public string Text { get; }
        public bool IsExpand { get; }

        public PropertyCategoryInfo(string name, string text, bool isExpand)
        {
            Name = name;
            Text = text;
            IsExpand = isExpand;
        }

        public CategoryItem Create(EditorProvider editorProvider)
        {
            var categoryItem = editorProvider.GetItem<CategoryItem>("CategoryItem");
            var categoryPanel = categoryItem.Init<CategoryType>(Name);
            categoryPanel.Init(this, editorProvider.editor);
            return categoryItem;
        }
    }

    public class PropertyInfoComparer : IComparer<IPropertyInfo>
    {
        public static PropertyInfoComparer Instance { get; } = new PropertyInfoComparer();
        public int Compare(IPropertyInfo x, IPropertyInfo y) => x.SortIndex - y.SortIndex;
    }
    public readonly struct EffectData
    {
        public readonly float texture;
        public readonly Vector2 cracks;
        public readonly Vector2 voids;

        public EffectData(float texture, Vector2 cracks, Vector2 voids)
        {
            this.texture = texture;
            this.cracks = cracks;
            this.voids = voids;
        }
    }
}
