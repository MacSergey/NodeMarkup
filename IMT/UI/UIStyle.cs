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
        public static Color32 PropertyDisabled => new Color32(116, 139, 164, 255);
        public static Color32 PropertyFocused => NormalBlue;

        public static Color32 PopupBackground => DarkPrimaryColor15;
        public static Color32 PopupEntitySelected => ItemFocused;
        public static Color32 PopupEntityHovered => ItemHovered;
        public static Color32 PopupEntityPressed => ItemPressed;

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
            Segmented = new ButtonStyle()
            {
                BgColors = new ColorSet(PropertyNormal, PropertyHovered, PropertyHovered, PropertyFocused, PropertyDisabled),
                SelBgColors = new ColorSet(PropertyFocused, PropertyFocused, PropertyFocused, PropertyFocused, FieldDisabledFocusedColor),

                FgColors = new ColorSet(Color.white),
                SelFgColors = new ColorSet(Color.white),

                TextColors = new ColorSet(Color.white),
                SelTextColors = new ColorSet(Color.white),
            },
            Button = new ButtonStyle()
            {
                BgColors = new ColorSet(ButtonNormal, ButtonHovered, ButtonPressed, ButtonNormal, ButtonFocused),
                SelBgColors = new ColorSet(),

                FgColors = new ColorSet(ButtonNormal),
                SelFgColors = new ColorSet(),

                TextColors = new ColorSet(Color.black, Color.black, Color.white, Color.black, Color.white),
                SelTextColors = new ColorSet(),
            },
            DropDown = new DropDownStyle()
            {
                BgAtlas = Atlas,
                FgAtlas = Atlas,

                BgSprites = new SpriteSet(FieldSingle, FieldSingle, FieldSingle, FieldSingle, BorderSmall),
                BgColors = new ColorSet(PropertyNormal, PropertyHovered, PropertyHovered, PropertyNormal, PropertyNormal),

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
                OffBgSprites = ToggleBackgroundSmall,

                OnMarkSprites = ToggleCircle,
                OffMarkSprites = ToggleCircle,

                OnBgColors = new ColorSet(PropertyFocused, PropertyFocused, PropertyFocused, PropertyFocused, FieldDisabledFocusedColor),
                OffBgColors = new ColorSet(PropertyNormal, PropertyHovered, PropertyHovered, PropertyNormal, PropertyDisabled),

                OnMarkColors = Color.white,
                OffMarkColors = Color.white,

                AllTextColors = Color.white,
            },
            ColorPicker = new ColorPickerStyle()
            {
                BgAtlas = Atlas,
                FgAtlas = Atlas,

                BgSprites = PanelSmall,
                BgColors = new ColorSet(PropertyNormal, PropertyHovered, PropertyHovered, PropertyNormal, PropertyNormal),

                FgSprites = PanelSmall,
            }
        };
    }
}
