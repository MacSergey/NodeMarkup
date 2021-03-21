using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Math;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;
using ICities;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using NodeMarkup.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;
using LineAlignment = NodeMarkup.Manager.LineAlignment;

namespace NodeMarkup.Utils
{
    public static class Utilities
    {
        public static void OpenUrl(string url)
        {
            if (PlatformService.IsOverlayEnabled())
                PlatformService.ActivateGameOverlayToWebPage(url);
            else
                Process.Start(url);
        }
        
        public static string Description<T>(this T value)
            where T : Enum
        {
            var description = value.GetAttr<DescriptionAttribute, T>()?.Description ?? value.ToString();
            return Localize.ResourceManager.GetString(description, Localize.Culture);
        }
        public static string Description(this StyleModifier modifier)
        {
            var localeID = "KEYNAME";

            if (modifier.GetAttr<DescriptionAttribute, StyleModifier>() is DescriptionAttribute description)
                return Localize.ResourceManager.GetString(description.Description, Localize.Culture);
            else if (modifier.GetAttr<InputKeyAttribute, StyleModifier>() is InputKeyAttribute inputKey)
            {
                var modifierStrings = new List<string>();
                if (inputKey.Control)
                    modifierStrings.Add(Locale.Get(localeID, KeyCode.LeftControl.ToString()));
                if (inputKey.Shift)
                    modifierStrings.Add(Locale.Get(localeID, KeyCode.LeftShift.ToString()));
                if (inputKey.Alt)
                    modifierStrings.Add(Locale.Get(localeID, KeyCode.LeftAlt.ToString()));
                return string.Join("+", modifierStrings.ToArray());
            }
            else
                return modifier.ToString();
        }
           
        public static LineAlignment Invert(this LineAlignment alignment) => (LineAlignment)(1 - alignment.Sign());
        public static int Sign(this LineAlignment alignment) => (int)alignment - 1;
        public static void Render(this Bounds bounds, RenderManager.CameraInfo cameraInfo, Color? color = null, float? width = null, bool? alphaBlend = null)
            => NodeMarkupTool.RenderCircle(cameraInfo, bounds.center, color, width ?? bounds.Magnitude(), alphaBlend);

        public static LinkedListNode<T> GetPrevious<T>(this LinkedListNode<T> item) => item.Previous ?? item.List.Last;
        public static LinkedListNode<T> GetNext<T>(this LinkedListNode<T> item) => item.Next ?? item.List.First;
    }
}
