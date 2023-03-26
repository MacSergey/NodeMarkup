using ModsCommon.UI;
using ModsCommon.Utilities;
using UnityEngine;
using static ModsCommon.UI.ComponentStyle;
using static ModsCommon.Utilities.CommonTextures;

namespace IMT.UI
{
    public static class PropertyStyle
    {
        public static Color32 Normal => new Color32(45, 51, 57, 255);
        public static Color32 Hovered => new Color32(61, 69, 77, 255);
        public static Color32 Disabled => new Color32(116, 139, 164, 255);

        public static ControlStyle Default { get; } = new ControlStyle()
        {
            TextField = new TextFieldStyle()
            {
                BgAtlas = Atlas,
                FgAtlas = Atlas,

                BgSprites = FieldSingle,
                BgColors = new ColorSet(Normal, Hovered, Hovered, NormalBlue, Disabled),

                TextColor = Color.white,

                SelectionSprite = Empty,
                SelectionColor = Color.black,
            },
            Segmented = new ButtonStyle()
            {
                BgColors = new ColorSet(Normal, Hovered, Hovered, NormalBlue, Disabled),
                SelBgColors = new ColorSet(NormalBlue, NormalBlue, NormalBlue, NormalBlue, FieldDisabledFocusedColor),

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

                BgSprites = FieldSingle,
                AllBgColors = new ColorSet(Normal, Hovered, Hovered, Normal, Disabled),

                FgSprites = ArrowDown,
                AllFgColors = Color.white,

                AllTextColors = Color.white,


                PopupAtlas = Atlas,
                PopupSprite = FieldSingle,
                PopupColor = Hovered,


                EntityAtlas = Atlas,

                EntitySprites = new SpriteSet(default, FieldSingle, default, default, default),
                EntitySelSprites = FieldSingle,

                EntityColors = Normal,
                EntitySelColors = NormalBlue,
            },
            Toggle = new ToggleStyle()
            {
                BgAtlas = Atlas,
                MarkAtlas = Atlas,

                OnBgSprites = ToggleBackgroundSmall,
                OffBgSprites = ToggleBackgroundSmall,

                OnMarkSprites = ToggleCircle,
                OffMarkSprites = ToggleCircle,

                OnBgColors = new ColorSet(NormalBlue, NormalBlue, NormalBlue, NormalBlue, FieldDisabledFocusedColor),
                OffBgColors = new ColorSet(Normal, Hovered, Hovered, Normal, Disabled),

                OnMarkColors = Color.white,
                OffMarkColors = Color.white,

                AllTextColors = Color.white,
            },
        };
    }
}
