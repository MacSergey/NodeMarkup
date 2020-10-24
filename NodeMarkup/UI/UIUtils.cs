using ColossalFramework.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace NodeMarkup.UI
{
    public static class UIUtils
    {
        private static UIView UIRoot { get; set; } = null;
        private static void FindUIRoot()
        {
            UIRoot = null;
            foreach (UIView uiview in UnityEngine.Object.FindObjectsOfType<UIView>())
            {
                bool flag = uiview.transform.parent == null && uiview.name == "UIView";
                if (flag)
                {
                    UIRoot = uiview;
                    break;
                }
            }
        }
        public static string GetTransformPath(Transform transform)
        {
            string text = transform.name;
            Transform parent = transform.parent;
            while (parent != null)
            {
                text = $"{parent.name}/{text}";
                parent = parent.parent;
            }
            return text;
        }
        public static T FindComponent<T>(string name, UIComponent parent = null, FindOptions options = FindOptions.None) where T : UIComponent
        {
            if (UIRoot == null)
            {
                FindUIRoot();
                if (UIRoot == null)
                    return default;
            }
            foreach (T t in UnityEngine.Object.FindObjectsOfType<T>())
            {
                bool flag4 = (options & FindOptions.NameContains) > FindOptions.None ? t.name.Contains(name) : t.name == name;
                if (flag4)
                {
                    Transform transform = parent != null ? parent.transform : UIRoot.transform;
                    Transform parent2 = t.transform.parent;
                    while (parent2 != null && parent2 != transform)
                        parent2 = parent2.parent;

                    if (parent2 != null)
                        return t;
                }
            }
            return default;
        }

        public static IEnumerable<T> GetCompenentsWithName<T>(string name) where T : UIComponent
        {
            T[] components = GameObject.FindObjectsOfType<T>();
            foreach (T component in components)
            {
                if (component.name == name)
                    yield return component;
            }
        }


        [Flags]
        public enum FindOptions
        {
            None = 0,
            NameContains = 1
        }

        public static void AddScrollbar(this UIComponent parent, UIScrollablePanel scrollablePanel)
        {
            var scrollbar = parent.AddUIComponent<UIScrollbar>();
            scrollbar.orientation = UIOrientation.Vertical;
            scrollbar.pivot = UIPivotPoint.TopLeft;
            scrollbar.minValue = 0;
            scrollbar.value = 0;
            scrollbar.incrementAmount = 50;
            scrollbar.autoHide = true;
            scrollbar.width = 10;

            UISlicedSprite trackSprite = scrollbar.AddUIComponent<UISlicedSprite>();
            trackSprite.relativePosition = Vector2.zero;
            trackSprite.autoSize = true;
            trackSprite.anchor = UIAnchorStyle.All;
            trackSprite.size = trackSprite.parent.size;
            trackSprite.fillDirection = UIFillDirection.Vertical;
            trackSprite.spriteName = "ScrollbarTrack";
            scrollbar.trackObject = trackSprite;

            UISlicedSprite thumbSprite = trackSprite.AddUIComponent<UISlicedSprite>();
            thumbSprite.relativePosition = Vector2.zero;
            thumbSprite.fillDirection = UIFillDirection.Vertical;
            thumbSprite.autoSize = true;
            thumbSprite.width = thumbSprite.parent.width;
            thumbSprite.spriteName = "ScrollbarThumb";
            scrollbar.thumbObject = thumbSprite;

            scrollbar.eventValueChanged += (component, value) => scrollablePanel.scrollPosition = new Vector2(0, value);

            parent.eventMouseWheel += (component, eventParam) =>
            {
                scrollbar.value -= (int)eventParam.wheelDelta * scrollbar.incrementAmount;
            };

            scrollablePanel.eventMouseWheel += (component, eventParam) =>
            {
                scrollbar.value -= (int)eventParam.wheelDelta * scrollbar.incrementAmount;
            };

            scrollablePanel.eventSizeChanged += (component, eventParam) =>
            {
                scrollbar.relativePosition = scrollablePanel.relativePosition + new Vector3(scrollablePanel.width, 0);
                scrollbar.height = scrollablePanel.height;
            };

            scrollablePanel.verticalScrollbar = scrollbar;
        }
        public static void ScrollIntoViewRecursive(this UIScrollablePanel panel, UIComponent component)
        {
            var rect = new Rect(panel.scrollPosition.x + panel.scrollPadding.left, panel.scrollPosition.y + panel.scrollPadding.top, panel.size.x - panel.scrollPadding.horizontal, panel.size.y - panel.scrollPadding.vertical).RoundToInt();
      
            var relativePosition = Vector3.zero;
            for (var current = component; current != null && current != panel; current = current.parent)
            {
                relativePosition += current.relativePosition;
            }

            var size = component.size;
            var other = new Rect(panel.scrollPosition.x + relativePosition.x, panel.scrollPosition.y + relativePosition.y, size.x, size.y).RoundToInt();
            if (!rect.Intersects(other))
            {
                Vector2 scrollPosition = panel.scrollPosition;
                if (other.xMin < rect.xMin)
                    scrollPosition.x = other.xMin - panel.scrollPadding.left;
                else if (other.xMax > rect.xMax)
                    scrollPosition.x = other.xMax - Mathf.Max(panel.size.x, size.x) + panel.scrollPadding.horizontal;

                if (other.y < rect.y)
                    scrollPosition.y = other.yMin - panel.scrollPadding.top;
                else if (other.yMax > rect.yMax)
                    scrollPosition.y = other.yMax - Mathf.Max(panel.size.y, size.y) + panel.scrollPadding.vertical;

                panel.scrollPosition = scrollPosition;
            }
        }
        public static void AddLabel(this UIHelper helper, string text, float size = 1.125f, Color? color = null, int padding = 0)
        {
            var component = helper.self as UIComponent;

            var label = component.AddUIComponent<UILabel>();
            label.text = text;
            label.textScale = size;
            label.textColor = color ?? Color.white;
            label.padding = new RectOffset(padding, 0, 0, 0);
        }

        public static Color32 ButtonNormal = Color.white;
        public static Color32 ButtonHovered = new Color32(224, 224, 224, 255);
        public static Color32 ButtonPressed = new Color32(192, 192, 192, 255);
        public static Color32 ButtonFocused = new Color32(160, 160, 160, 255);
        public static void SetDefaultStyle(this UIButton button)
        {
            button.atlas = TextureUtil.InGameAtlas;
            button.normalBgSprite = "ButtonWhite";
            button.disabledBgSprite = "ButtonWhite";
            button.hoveredBgSprite = "ButtonWhite";
            button.pressedBgSprite = "ButtonWhite";
            button.color = ButtonNormal;
            button.hoveredColor = ButtonHovered;
            button.pressedColor = ButtonPressed;
            button.textColor = button.hoveredTextColor = button.focusedTextColor = Color.black;
            button.pressedTextColor = Color.white;
        }
    }
}
