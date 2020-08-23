using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeMarkup.UI
{
    public static class UIUtils
    {
        private static UIView uiRoot { get; set; } = null;
        private static void FindUIRoot()
        {
            uiRoot = null;
            foreach (UIView uiview in UnityEngine.Object.FindObjectsOfType<UIView>())
            {
                bool flag = uiview.transform.parent == null && uiview.name == "UIView";
                if (flag)
                {
                    uiRoot = uiview;
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
                text = parent.name + "/" + text;
                parent = parent.parent;
            }
            return text;
        }
        public static T FindComponent<T>(string name, UIComponent parent = null, FindOptions options = FindOptions.None) where T : UIComponent
        {
            if (uiRoot == null)
            {
                FindUIRoot();
                if (uiRoot == null)
                    return default;
            }
            foreach (T t in UnityEngine.Object.FindObjectsOfType<T>())
            {
                bool flag4 = (options & FindOptions.NameContains) > FindOptions.None ? t.name.Contains(name) : t.name == name;
                if (flag4)
                {
                    Transform transform = parent != null ? parent.transform : uiRoot.transform;
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

        public static void AddScrollbar(UIComponent parent, UIScrollablePanel scrollablePanel)
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
    }
}
