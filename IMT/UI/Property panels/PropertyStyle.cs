using ModsCommon.UI;
using UnityEngine;

namespace IMT.UI
{
    public static class PropertyStyle
    {
        //public static Color32 Normal => new Color32(169, 179, 188, 255);
        //public static Color32 Hovered => new Color32(184, 192, 199, 255);
        //public static Color32 Disabled => new Color32(116, 139, 164, 255);
        public static Color32 Normal => new Color32(45, 51, 57, 255);
        public static Color32 Hovered => new Color32(61, 69, 77, 255);
        public static Color32 Disabled => new Color32(116, 139, 164, 255);

        public static ControlStyle Default { get; } = new ControlStyle()
        {
            TextField = new TextFieldStyle()
            {
                Colors = new ColorSet(Normal, Hovered, Hovered, ComponentStyle.NormalBlue, Disabled),
                TextColor = Color.white,
                SelectionColor = Color.black,
            },
            Segmented = new ButtonStyle()
            {
                BgColors = new ColorSet(Normal, Hovered, Hovered, ComponentStyle.NormalBlue, Disabled),
                SelBgColors = new ColorSet(ComponentStyle.NormalBlue, ComponentStyle.NormalBlue, ComponentStyle.NormalBlue, ComponentStyle.NormalBlue, ComponentStyle.FieldDisabledFocusedColor),

                FgColors = new ColorSet(Color.white),
                SelFgColors = new ColorSet(Color.white),

                TextColors = new ColorSet(Color.white),
                SelTextColors = new ColorSet(Color.white),
            },
            Button = new ButtonStyle()
            {
                BgColors = new ColorSet(ComponentStyle.ButtonNormal, ComponentStyle.ButtonHovered, ComponentStyle.ButtonPressed, ComponentStyle.ButtonNormal, ComponentStyle.ButtonFocused),
                SelBgColors = new ColorSet(),

                FgColors = new ColorSet(ComponentStyle.ButtonNormal),
                SelFgColors = new ColorSet(),

                TextColors = new ColorSet(Color.black, Color.black, Color.white, Color.black, Color.white),
                SelTextColors = new ColorSet(),
            },
            DropDown = new DropDownStyle()
            {
                BgColors = new ColorSet(Normal, Hovered, Hovered, Normal, Disabled),
                FgColors = new ColorSet(Color.black),
                TextColors = new ColorSet(Color.white),

                PopupColor = Hovered,

                EntityHoveredColor = Normal,
                EntitySelectedColor = ComponentStyle.NormalBlue,
            },
            Toggle = new ToggleStyle()
            {
                OnColors = new ColorSet(ComponentStyle.NormalBlue, ComponentStyle.NormalBlue, ComponentStyle.NormalBlue, ComponentStyle.NormalBlue, ComponentStyle.FieldDisabledFocusedColor),
                OffColors = new ColorSet(Normal, Hovered, Hovered, Normal, Disabled)
            },
        };
    }
}
