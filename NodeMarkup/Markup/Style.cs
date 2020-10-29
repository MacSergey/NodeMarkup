using ColossalFramework.UI;
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
        Color32 Color { get; set; }
    }
    public interface IWidthStyle : IStyle
    {
        float Width { get; set; }
    }
    public abstract class Style : IToXml
    {
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
            return (type & StyleType.GroupMask) switch
            {
                StyleType.RegularLine when RegularLineStyle.GetDefault((RegularLineStyle.RegularLineType)(int)type) is T tStyle => tStyle,
                StyleType.StopLine when StopLineStyle.GetDefault((StopLineStyle.StopLineType)(int)type) is T tStyle => tStyle,
                StyleType.Filler when FillerStyle.GetDefault((FillerStyle.FillerType)(int)type) is T tStyle => tStyle,
                StyleType.Crosswalk when CrosswalkStyle.GetDefault((CrosswalkStyle.CrosswalkType)(int)type) is T tStyle => tStyle,
                _ => null,
            };
        }

        public static string XmlName { get; } = "S";

        public Action OnStyleChanged { private get; set; }
        public string XmlSection => XmlName;
        public abstract StyleType Type { get; }

        protected virtual void StyleChanged() => OnStyleChanged?.Invoke();

        Color32 _color;
        float _width;

        public Color32 Color
        {
            get => _color;
            set
            {
                _color = value;
                StyleChanged();
            }
        }
        public float Width
        {
            get => _width;
            set
            {
                _width = value;
                StyleChanged();
            }
        }
        public Style(Color32 color, float width)
        {
            Color = color;
            Width = width;
        }
        protected XElement BaseToXml() => new XElement(XmlSection, new XAttribute("T", TypeToInt(Type)));
        public virtual XElement ToXml()
        {
            var config = BaseToXml();
            config.Add(new XAttribute("C", Color.ToInt()));
            config.Add(new XAttribute("W", Width));
            return config;
        }
        public virtual void FromXml(XElement config, ObjectsMap map, bool invert)
        {
            var colorInt = config.GetAttrValue<int>("C");
            Color = colorInt != 0 ? colorInt.ToColor() : DefaultColor;
            Width = config.GetAttrValue("W", DefaultWidth);
        }

        public abstract Style Copy();
        public virtual void CopyTo(Style target)
        {
            if (this is IWidthStyle widthSource && target is IWidthStyle widthTarget)
                widthTarget.Width = widthSource.Width;
            if (this is IColorStyle colorSource && target is IColorStyle colorTarget)
                colorTarget.Color = colorSource.Color;
        }

        public virtual List<UIComponent> GetUIComponents(object editObject, UIComponent parent, Action onHover = null, Action onLeave = null, bool isTemplate = false)
        {
            var components = new List<UIComponent>();

            if (this is IColorStyle)
                components.Add(AddColorProperty(parent));
            if (this is IWidthStyle)
                components.Add(AddWidthProperty(parent, onHover, onLeave));

            return components;
        }
        protected ColorPropertyPanel AddColorProperty(UIComponent parent)
        {
            var colorProperty = ComponentPool.Get<ColorPropertyPanel>(parent);
            colorProperty.Text = Localize.StyleOption_Color;
            colorProperty.Init();
            colorProperty.Value = Color;
            colorProperty.OnValueChanged += (Color32 color) => Color = color;
            return colorProperty;
        }
        protected FloatPropertyPanel AddWidthProperty(UIComponent parent, Action onHover, Action onLeave)
        {
            var widthProperty = ComponentPool.Get<FloatPropertyPanel>(parent);
            widthProperty.Text = Localize.StyleOption_Width;
            widthProperty.UseWheel = true;
            widthProperty.WheelStep = WidthWheelStep;
            widthProperty.CheckMin = true;
            widthProperty.MinValue = WidthMinValue;
            widthProperty.Init();
            widthProperty.Value = Width;
            widthProperty.OnValueChanged += (float value) => Width = value;
            AddOnHoverLeave(widthProperty, onHover, onLeave);

            return widthProperty;
        }
        protected static FloatPropertyPanel AddDashLengthProperty(IDashedLine dashedStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var dashLengthProperty = ComponentPool.Get<FloatPropertyPanel>(parent);
            dashLengthProperty.Text = Localize.StyleOption_DashedLength;
            dashLengthProperty.UseWheel = true;
            dashLengthProperty.WheelStep = 0.1f;
            dashLengthProperty.CheckMin = true;
            dashLengthProperty.MinValue = 0.1f;
            dashLengthProperty.Init();
            dashLengthProperty.Value = dashedStyle.DashLength;
            dashLengthProperty.OnValueChanged += (float value) => dashedStyle.DashLength = value;
            AddOnHoverLeave(dashLengthProperty, onHover, onLeave);
            return dashLengthProperty;
        }
        protected static FloatPropertyPanel AddSpaceLengthProperty(IDashedLine dashedStyle, UIComponent parent, Action onHover, Action onLeave)
        {
            var spaceLengthProperty = ComponentPool.Get<FloatPropertyPanel>(parent);
            spaceLengthProperty.Text = Localize.StyleOption_SpaceLength;
            spaceLengthProperty.UseWheel = true;
            spaceLengthProperty.WheelStep = 0.1f;
            spaceLengthProperty.CheckMin = true;
            spaceLengthProperty.MinValue = 0.1f;
            spaceLengthProperty.Init();
            spaceLengthProperty.Value = dashedStyle.SpaceLength;
            spaceLengthProperty.OnValueChanged += (float value) => dashedStyle.SpaceLength = value;
            AddOnHoverLeave(spaceLengthProperty, onHover, onLeave);
            return spaceLengthProperty;
        }
        protected static ButtonsPanel AddInvertProperty(IAsymLine asymStyle, UIComponent parent)
        {
            var buttonsPanel = ComponentPool.Get<ButtonsPanel>(parent);
            var invertIndex = buttonsPanel.AddButton(Localize.StyleOption_Invert);
            buttonsPanel.Init();
            buttonsPanel.OnButtonClick += OnButtonClick;

            void OnButtonClick(int index)
            {
                if (index == invertIndex)
                    asymStyle.Invert = !asymStyle.Invert;
            }

            return buttonsPanel;
        }
        protected static void AddOnHoverLeave<T>(FieldPropertyPanel<T> fieldPanel, Action onHover, Action onLeave)
        {
            if (onHover != null)
                fieldPanel.OnHover += onHover;
            if (onLeave != null)
                fieldPanel.OnLeave += onLeave;
        }


        public enum StyleType
        {
            ItemMask = 0xFF,
            GroupMask = ~ItemMask,

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

            [Description(nameof(Localize.LineStyle_Empty))]
            [NotVisible]
            EmptyLine,


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


            //Filler3D = Filler | 0x80,

            //[Description("Pavement")]
            //FillerPavement,


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
        }
    }
}
