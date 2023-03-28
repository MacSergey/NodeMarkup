using ModsCommon.UI;
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
        public static Color32 PropertyNormal => DarkPrimaryColor20;
        public static Color32 PropertyHovered => DarkPrimaryColor30;
        public static Color32 PropertyPressed => DarkPrimaryColor35;
        public static Color32 PropertyFocused => NormalBlue;

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

                BgSprites = new SpriteSet(FieldSingle, FieldSingle, FieldSingle, FieldSingle, BorderSmall),
                BgColors = new ColorSet(PropertyNormal, PropertyHovered, PropertyHovered, PropertyNormal, PropertyNormal),

                FgSprites = new SpriteSet(default, default, default, BorderSmall, default),
                FgColors = new ColorSet(default, default, default, PropertyFocused, default),

                TextColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, Color.black),

                SelectionSprite = Empty,
                SelectionColor = PropertyFocused,
            },
            Segmented = new SegmentedStyle()
            {
                Single = GetSegmentedStyle(FieldSingle, BorderSmall),
                Left = GetSegmentedStyle(FieldLeft, FieldBorderLeft),
                Middle = GetSegmentedStyle(FieldMiddle, FieldBorderMiddle),
                Right = GetSegmentedStyle(FieldRight, FieldBorderRight),
            },
            Button = new ButtonStyle()
            {
                BgAtlas = Atlas,
                FgAtlas = Atlas,

                BgSprites = new SpriteSet(FieldSingle, FieldSingle, FieldSingle, FieldSingle, BorderSmall),
                BgColors = new ColorSet(PropertyNormal, PropertyHovered, PropertyPressed, PropertyNormal, PropertyNormal),
                SelBgColors = new ColorSet(),

                FgSprites = default,
                FgColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, Color.black),
                SelFgColors = default,

                TextColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, Color.black),
                SelTextColors = default,
            },
            DropDown = new DropDownStyle()
            {
                BgAtlas = Atlas,
                FgAtlas = Atlas,

                AllBgSprites = new SpriteSet(FieldSingle, FieldSingle, FieldSingle, FieldSingle, BorderSmall),
                BgColors = new ColorSet(PropertyNormal, PropertyHovered, PropertyHovered, PropertyNormal, PropertyNormal),
                SelBgColors = PropertyFocused,

                FgSprites = new SpriteSet(VectorDown, VectorDown, VectorDown, VectorDown, default),
                FgColors = Color.white,

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
                MarkAtlas = Atlas,

                OnBgSprites = ToggleBackgroundSmall,
                OffBgSprites = new SpriteSet(ToggleBackgroundSmall, ToggleBackgroundSmall, ToggleBackgroundSmall, ToggleBackgroundSmall, ToggleBorderSmall),

                OnMarkSprites = ToggleCircle,
                OffMarkSprites = ToggleCircle,

                OnBgColors = new ColorSet(PropertyFocused, PropertyFocused, PropertyFocused, PropertyFocused, PropertyNormal),
                OffBgColors = new ColorSet(PropertyNormal, PropertyHovered, PropertyHovered, PropertyNormal, PropertyNormal),

                OnMarkColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, PropertyPanel),
                OffMarkColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, PropertyNormal),

                OnTextColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, PropertyPanel),
                OffTextColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, Color.black),
            },
            ColorPicker = new ColorPickerStyle()
            {
                BgAtlas = Atlas,
                FgAtlas = Atlas,

                BgSprites = PanelSmall,
                BgColors = new ColorSet(PropertyNormal, PropertyHovered, PropertyHovered, PropertyNormal, PropertyNormal),

                FgSprites = PanelSmall,
            },
            Label = new LabelStyle()
            {
                NormalTextColor = Color.white,
                DisabledTextColor = Color.black,
            },
        };

        private static ButtonStyle GetSegmentedStyle(string background, string border)
        {
            return new ButtonStyle()
            {
                BgAtlas = Atlas,
                FgAtlas = Atlas,

                BgSprites = new SpriteSet(background, background, background, background, border),
                BgColors = new ColorSet(PropertyNormal, PropertyHovered, PropertyHovered, PropertyFocused, PropertyNormal),
                SelBgSprites = background,
                SelBgColors = new ColorSet(PropertyFocused, PropertyFocused, PropertyFocused, PropertyFocused, PropertyNormal),

                FgColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, PropertyNormal),
                SelFgColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, PropertyPanel),

                TextColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, Color.black),
                SelTextColors = new ColorSet(Color.white, Color.white, Color.white, Color.white, PropertyPanel),
            };
        }
    }
}
