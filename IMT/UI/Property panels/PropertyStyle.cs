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
                Colors = new ControlStyle.ColorSet(Normal, Hovered, Hovered, ComponentStyle.NormalBlue, Disabled),
                TextColor = Color.white,
                SelectionColor = Color.black,
            },
            Segmented = new ButtonStyle()
            {
                BgColors = new ControlStyle.ColorSet(Normal, Hovered, Hovered, ComponentStyle.NormalBlue, Disabled),
                SelBgColors = new ControlStyle.ColorSet(ComponentStyle.NormalBlue, ComponentStyle.NormalBlue, ComponentStyle.NormalBlue, ComponentStyle.NormalBlue, ComponentStyle.FieldDisabledFocusedColor),

                FgColors = new ControlStyle.ColorSet(Color.white),
                SelFgColors = new ControlStyle.ColorSet(Color.white),

                TextColors = new ControlStyle.ColorSet(Color.white),
                SelTextColors = new ControlStyle.ColorSet(Color.white),
            },
            Button = new ButtonStyle()
            {
                BgColors = new ControlStyle.ColorSet(ComponentStyle.ButtonNormal, ComponentStyle.ButtonHovered, ComponentStyle.ButtonPressed, ComponentStyle.ButtonNormal, ComponentStyle.ButtonFocused),
                SelBgColors = new ControlStyle.ColorSet(),

                FgColors = new ControlStyle.ColorSet(ComponentStyle.ButtonNormal),
                SelFgColors = new ControlStyle.ColorSet(),

                TextColors = new ControlStyle.ColorSet(Color.black, Color.black, Color.white, Color.black, Color.white),
                SelTextColors = new ControlStyle.ColorSet(),
            },
            DropDown = new DropDownStyle()
            {
                BgColors = new ControlStyle.ColorSet(Normal, Hovered, Hovered, Normal, Disabled),
                FgColors = new ControlStyle.ColorSet(Color.black),
                TextColors = new ControlStyle.ColorSet(Color.white),

                PopupColor = Hovered,

                EntityHoveredColor = Normal,
                EntitySelectedColor = ComponentStyle.NormalBlue,
            },
            Toggle = new ToggleStyle()
            {
                OnColors = new ControlStyle.ColorSet(ComponentStyle.NormalBlue, ComponentStyle.NormalBlue, ComponentStyle.NormalBlue, ComponentStyle.NormalBlue, ComponentStyle.FieldDisabledFocusedColor),
                OffColors = new ControlStyle.ColorSet(Normal, Hovered, Hovered, Normal, Disabled)
            },
        };
    }
}
