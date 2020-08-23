using UnityEngine;

namespace NodeMarkup.Utils
{
    public class MarkupColors
    {
        private static byte Alpha => 224;
        public static Color32 White { get; } = new Color32(255, 255, 255, 255);
        public static Color32 Green { get; } = new Color32(0, 200, 81, 255);
        public static Color32 Red { get; } = new Color32(255, 68, 68, 255);
        public static Color32 Blue { get; } = new Color32(2, 117, 216, 255);
        public static Color32 Orange { get; } = new Color32(255, 136, 0, 255);

        public static Color32[] OverlayColors { get; } = new Color32[]
        {
            new Color32(218, 33, 40, Alpha),
            new Color32(72, 184, 94, Alpha),
            new Color32(0, 120, 191, Alpha),

            new Color32(245, 130, 32, Alpha),
            new Color32(142, 71, 155, Alpha),
            new Color32(255, 198, 26, Alpha),

            new Color32(180, 212, 69, Alpha),
            new Color32(0, 193, 243, Alpha),
            new Color32(230, 106, 192, Alpha),

        };

        public static Color32 GetOverlayColor(int index) => OverlayColors[index % OverlayColors.Length];
    }
}
