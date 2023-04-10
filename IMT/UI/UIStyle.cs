using ModsCommon.UI;
using ModsCommon.Utilities;
using UnityEngine;
using static ModsCommon.UI.ComponentStyle;
using static ModsCommon.Utilities.CommonTextures;

namespace IMT.UI
{
    public static class UIStyle
    {
        public static Color32 GreenColor => new Color32(155, 175, 86, 255);

        public static Color32 TabButtonNormal => DarkPrimaryColor25;
        public static Color32 TabButtonHovered => DarkPrimaryColor45;
        public static Color32 TabButtonPressed => DarkPrimaryColor55;
        public static Color32 TabButtonFocused => GreenColor;
        public static Color32 TabButtonDisabled => DarkPrimaryColor20;

        public static Color32 ItemsBackground => DarkPrimaryColor15;
        public static Color32 ContentBackground => DarkPrimaryColor20;
        public static Color32 Header => HeaderColor;

        public static Color32 ItemHovered => new Color32(38, 97, 142, 255);
        public static Color32 ItemPressed => new Color32(79, 143, 192, 255);
        public static Color32 ItemFocused => new Color32(139, 181, 213, 255);
        public static Color32 ItemGroup => new Color32(29, 75, 106, 255);
        public static Color32 ItemGroupBackground => DarkPrimaryColor25;

        public static Color32 ItemFavoriteNormal => new Color32(255, 208, 0, 255);
        public static Color32 ItemFavoriteHovered => new Color32(255, 183, 0, 255);
        public static Color32 ItemFavoritePressed => new Color32(255, 170, 0, 255);
        public static Color32 ItemFavoriteFocused => new Color32(255, 162, 0, 255);

        public static Color32 ItemErrorNormal => ErrorNormalColor;
        public static Color32 ItemErrorPressed => ErrorPressedColor;
        public static Color32 ItemErrorFocused => ErrorFocusedColor;


        public static Color32 PropertyPanel => new Color32(132, 152, 90, 255);
        public static Color32 PropertyPanelDisabled => PropertyPanel.Overlap(PropertyDisabled);

        public static Color32 PropertyNormal => DarkPrimaryColor20;
        public static Color32 PropertyHovered => DarkPrimaryColor30;
        public static Color32 PropertyPressed => DarkPrimaryColor35;
        public static Color32 PropertyFocused => NormalBlue;
        public static Color32 PropertyDisabled => new Color32(255, 255, 255, 32);

        public static Color32 PopupBackground => DarkPrimaryColor15;
        public static Color32 PopupEntitySelected => NormalBlue;
        public static Color32 PopupEntityHovered => DarkPrimaryColor50;
        public static Color32 PopupEntityPressed => DarkPrimaryColor60;

        public static ControlStyle Default { get; } = new ControlStyle()
        {
            TextField = new TextFieldStyle()
            {
                BgAtlas = Atlas,
                FgAtlas = Atlas,

                BgSprites = FieldSingle,
                BgColors = new ColorSet(PropertyNormal, PropertyHovered, PropertyHovered, PropertyNormal, PropertyDisabled),

                FgSprites = new SpriteSet(default, default, default, BorderSmall, BorderSmall),
                FgColors = new ColorSet(default, default, default, PropertyFocused, PropertyNormal),

                TextColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, Color.black),

                SelectionSprite = Empty,
                SelectionColor = new Color32(255, 64, 0, 255),
            },
            Segmented = new SegmentedStyle()
            {
                Single = GetSegmentedStyle(FieldSingle, BorderSmall),
                Left = GetSegmentedStyle(FieldLeft, FieldBorderLeft),
                Middle = GetSegmentedStyle(FieldMiddle, FieldBorderMiddle),
                Right = GetSegmentedStyle(FieldRight, FieldBorderRight),
            },
            SmallButton = new ButtonStyle()
            {
                BgAtlas = Atlas,
                FgAtlas = Atlas,

                BgSprites = FieldSingle,
                BgColors = new ColorSet(PropertyNormal, PropertyHovered, PropertyPressed, PropertyNormal, PropertyDisabled),
                SelBgColors = new ColorSet(),

                FgSprites = new SpriteSet(default, default, default, default, BorderSmall),
                FgColors = new ColorSet(default, default, default, default, PropertyNormal),
                SelFgColors = default,

                IconSprites = default,
                IconColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, Color.black),
                SelIconColors = default,

                TextColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, Color.black),
                SelTextColors = default,
            },
            LargeButton = new ButtonStyle()
            {
                BgAtlas = Atlas,
                FgAtlas = Atlas,

                BgSprites = PanelLarge,
                BgColors = new ColorSet(PropertyNormal, PropertyHovered, PropertyPressed, PropertyNormal, PropertyDisabled),
                SelBgColors = new ColorSet(),

                FgSprites = new SpriteSet(default, default, default, default, BorderLarge),
                FgColors = new ColorSet(default, default, default, default, PropertyNormal),
                SelFgColors = default,

                IconSprites = default,
                IconColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, Color.black),
                SelIconColors = default,

                TextColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, Color.black),
                SelTextColors = default,
            },
            DropDown = new DropDownStyle()
            {
                BgAtlas = Atlas,
                FgAtlas = Atlas,
                IconAtlas = Atlas,

                AllBgSprites = FieldSingle,
                BgColors = new ColorSet(PropertyNormal, PropertyHovered, PropertyHovered, PropertyNormal, PropertyDisabled),
                SelBgColors = PropertyFocused,

                FgSprites = new SpriteSet(default, default, default, default, BorderSmall),
                FgColors = new ColorSet(default, default, default, default, PropertyNormal),
                SelFgSprites = BorderSmall,
                SelFgColors = PropertyNormal,

                AllIconSprites = new SpriteSet(VectorDown, VectorDown, VectorDown, VectorDown, default),
                AllIconColors = Color.white,

                AllTextColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, Color.black),


                PopupAtlas = Atlas,
                PopupSprite = FieldSingle,
                PopupColor = PopupBackground,
                PopupItemsPadding = new RectOffset(4, 4, 4, 4),


                EntityAtlas = Atlas,

                EntitySprites = new SpriteSet(default, FieldSingle, FieldSingle, default, default),
                EntitySelSprites = FieldSingle,

                EntityColors = new ColorSet(default, PopupEntityHovered, PopupEntityPressed, default, default),
                EntitySelColors = PopupEntitySelected,
            },
            Toggle = new ToggleStyle()
            {
                BgAtlas = Atlas,
                FgAtlas = Atlas,
                MarkAtlas = Atlas,

                OnBgSprites = ToggleBackgroundSmall,
                OnBgColors = new ColorSet(PropertyFocused, PropertyFocused, PropertyFocused, PropertyFocused, PropertyNormal),

                OnFgSprites = ToggleBorderSmall,
                OnFgColors = new ColorSet(PropertyNormal, PropertyNormal, PropertyNormal, PropertyNormal, PropertyNormal),

                OnMarkSprites = ToggleCircle,
                OnMarkColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, PropertyPanelDisabled),

                OffBgSprites = ToggleBackgroundSmall,
                OffBgColors = new ColorSet(PropertyNormal, PropertyHovered, PropertyHovered, PropertyNormal, PropertyDisabled),

                OffFgSprites = ToggleBackgroundSmall,
                OffFgColors = new ColorSet(PropertyNormal, PropertyHovered, PropertyHovered, PropertyNormal, PropertyNormal),

                OffMarkSprites = ToggleCircle,
                OffMarkColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, PropertyNormal),

                OnTextColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, PropertyPanelDisabled),
                OffTextColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, Color.black),
            },
            ColorPicker = new ColorPickerStyle()
            {
                BgAtlas = Atlas,
                FgAtlas = Atlas,

                BgSprites = PanelSmall,
                BgColors = new ColorSet(PropertyNormal, PropertyHovered, PropertyHovered, PropertyNormal, PropertyNormal),

                FgSprites = PanelSmall,

                TextField = new TextFieldStyle()
                {
                    BgColors = new ColorSet(DarkPrimaryColor60, DarkPrimaryColor70, DarkPrimaryColor70, PropertyFocused, DarkPrimaryColor60),
                    TextColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, Color.black),

                    SelectionColor = Color.black,
                },
                Button = new ButtonStyle()
                {
                    BgAtlas = Atlas,
                    FgAtlas = Atlas,

                    BgSprites = new SpriteSet(FieldSingle, FieldSingle, FieldSingle, FieldSingle, BorderSmall),
                    BgColors = new ColorSet(DarkPrimaryColor60, DarkPrimaryColor70, DarkPrimaryColor75, DarkPrimaryColor60, DarkPrimaryColor40),

                    TextColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, Color.white),
                },
            },
            Label = new LabelStyle()
            {
                NormalTextColor = Color.white,
                DisabledTextColor = Color.black,
            },
            PropertyPanel = new PropertyPanelStyle()
            {
                BgAtlas = Atlas,
                BgSprites = PanelLarge,
                BgColors = PropertyPanel,
                MaskSprite = OpacitySliderMask,
            },
            HeaderContent = new HeaderStyle()
            {
                MainBgColors = new ColorSet(default, DarkPrimaryColor10, Color.black, default, default),
                MainIconColors = new ColorSet(Color.white, Color.white, DarkPrimaryColor90, Color.white, DarkPrimaryColor10),
                AdditionalBgColors = new ColorSet(default, DarkPrimaryColor45, DarkPrimaryColor45, default, default),
                AdditionalIconColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, DarkPrimaryColor55),
            }
        };

        private static ButtonStyle GetSegmentedStyle(string background, string border)
        {
            return new ButtonStyle()
            {
                BgAtlas = Atlas,
                FgAtlas = Atlas,

                BgSprites = background,
                BgColors = new ColorSet(PropertyNormal, PropertyHovered, PropertyHovered, PropertyFocused, PropertyDisabled),
                SelBgSprites = background,
                SelBgColors = new ColorSet(PropertyFocused, PropertyFocused, PropertyFocused, PropertyFocused, PropertyNormal),

                FgSprites = new SpriteSet(default, default, default, default, border),
                FgColors = new ColorSet(default, default, default, default, PropertyNormal),
                SelFgSprites = new SpriteSet(border, border, border, border, default),
                SelFgColors = new ColorSet(PropertyNormal, PropertyNormal, PropertyNormal, PropertyNormal, default),

                IconColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, PropertyNormal),
                SelIconColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, PropertyPanelDisabled),

                TextColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, Color.black),
                SelTextColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, PropertyPanelDisabled),
            };
        }
    }
}
