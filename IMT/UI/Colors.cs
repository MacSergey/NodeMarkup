using ModsCommon.UI;
using UnityEngine;

namespace IMT.UI
{
    public static class IMTColors
    {
        public static Color32 GreenColor => new Color32(155, 175, 86, 255);

        public static Color32 TabButtonNormal => ComponentStyle.DarkPrimaryColor25;
        public static Color32 TabButtonHovered => ComponentStyle.DarkPrimaryColor45;
        public static Color32 TabButtonPressed => ComponentStyle.DarkPrimaryColor55;
        public static Color32 TabButtonFocused => GreenColor;
        public static Color32 TabButtonDisabled => ComponentStyle.DarkPrimaryColor20;

        public static Color32 ItemsBackground => ComponentStyle.DarkPrimaryColor15;
        public static Color32 ContentBackground => ComponentStyle.DarkPrimaryColor20;
        public static Color32 Header => ComponentStyle.HeaderColor;

        public static Color32 ItemHovered => new Color32(38, 97, 142, 255);
        public static Color32 ItemPressed => new Color32(79, 143, 192, 255);
        public static Color32 ItemFocused => new Color32(139, 181, 213, 255);
        public static Color32 ItemGroup => new Color32(29, 75, 106, 255);
        public static Color32 ItemGroupBackground => ComponentStyle.DarkPrimaryColor25;

        public static Color32 ItemFavoriteNormal => new Color32(255, 208, 0, 255);
        public static Color32 ItemFavoriteHovered => new Color32(255, 183, 0, 255);
        public static Color32 ItemFavoritePressed => new Color32(255, 170, 0, 255);
        public static Color32 ItemFavoriteFocused => new Color32(255, 162, 0, 255);

        public static Color32 ItemErrorNormal => ComponentStyle.ErrorNormalColor;
        public static Color32 ItemErrorPressed => ComponentStyle.ErrorPressedColor;
        public static Color32 ItemErrorFocused => ComponentStyle.ErrorFocusedColor;

        //public static Color32  => new Color32(, 255);
        //public static Color32  => new Color32(, 255);
        //public static Color32  => new Color32(, 255);
        //public static Color32  => new Color32(, 255);
        //public static Color32  => new Color32(, 255);
        //public static Color32  => new Color32(, 255);
        //public static Color32  => new Color32(, 255);
        //public static Color32  => new Color32(, 255);
    }
}
