using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.Utils
{
    public static class Colors
    {
        private const byte Alpha = 224;
        public static Color32 White { get; } = new Color32(255, 255, 255, 255);
        public static Color32 Green { get; } = new Color32(0, 200, 81, 255);
        public static Color32 Red { get; } = new Color32(255, 68, 68, 255);
        public static Color32 Blue { get; } = new Color32(2, 117, 216, 255);
        public static Color32 Orange { get; } = new Color32(255, 136, 0, 255);
        public static Color32 Gray { get; } = new Color32(192, 192, 192, 255);
        public static Color32 Purple { get; } = new Color32(122, 71, 209, 255);
        public static Color32 Hover { get; } = new Color32(217, 251, 255, 255);

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

        public static Color32 GetOverlayColor(int index, byte alpha = Alpha, byte hue = 255)
        {
            var color = OverlayColors[index % OverlayColors.Length];
            return new Color32(SetHue(color.r, hue), SetHue(color.g, hue), SetHue(color.b, hue), alpha);
        }
        private static byte SetHue(byte value, byte hue) => (byte)(byte.MaxValue - ((byte.MaxValue - value) / 255f * hue));
    }
}
