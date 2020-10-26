using ColossalFramework.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI
{
    public interface IUISelector<ValueType>
    {
        event Action<ValueType> OnSelectObjectChanged;

        Func<ValueType, ValueType, bool> IsEqualDelegate { get; set; }
        ValueType SelectedObject { get; set; }

        void AddItem(ValueType item, string label = null);
        void Clear();
        void SetDefaultStyle(Vector2? size = null);
    }
    public abstract class UIDropDown<ValueType> : UIDropDown, IUISelector<ValueType>
    {
        public event Action<ValueType> OnSelectObjectChanged;

        public Func<ValueType, ValueType, bool> IsEqualDelegate { get; set; }
        List<ValueType> Objects { get; } = new List<ValueType>();
        public ValueType SelectedObject
        {
            get => selectedIndex >= 0 ? Objects[selectedIndex] : default;
            set => selectedIndex = Objects.FindIndex(o => IsEqualDelegate?.Invoke(o, value) ?? ReferenceEquals(o, value) || o.Equals(value));
        }

        public UIDropDown()
        {
            eventSelectedIndexChanged += IndexChanged;
        }
        protected virtual void IndexChanged(UIComponent component, int value) => OnSelectObjectChanged?.Invoke(SelectedObject);

        public void AddItem(ValueType item, string label = null)
        {
            Objects.Add(item);
            AddItem(label ?? item.ToString());
        }
        public void Clear()
        {
            selectedIndex = -1;
            Objects.Clear();
            items = new string[0];
        }
        protected override void OnMouseWheel(UIMouseEventParameter p) { }

        public void SetDefaultStyle(Vector2? size = null)
        {
            atlas = EditorItem.EditorItemAtlas;
            this.size = size ?? new Vector2(230, 20);
            listBackground = "TextFieldPanelHovered";
            itemHeight = 20;
            itemHover = "TextFieldPanel";
            itemHighlight = "TextFieldPanelFocus";
            normalBgSprite = "TextFieldPanel";
            hoveredBgSprite = "TextFieldPanelHovered";
            listWidth = (int)width;
            listHeight = 700;
            listPosition = PopupListPosition.Below;
            clampListToScreen = true;
            foregroundSpriteMode = UIForegroundSpriteMode.Stretch;
            popupColor = new Color32(45, 52, 61, 255);
            popupTextColor = new Color32(170, 170, 170, 255);
            textScale = 0.7f;
            textFieldPadding = new RectOffset(8, 0, 6, 0);
            popupColor = Color.white;
            popupTextColor = Color.black;
            verticalAlignment = UIVerticalAlignment.Middle;
            horizontalAlignment = UIHorizontalAlignment.Left;
            itemPadding = new RectOffset(14, 0, 5, 0);

            var button = AddUIComponent<UIButton>();
            button.atlas = TextureUtil.InGameAtlas;
            button.text = string.Empty;
            button.size = this.size;
            button.relativePosition = new Vector3(0f, 0f);
            button.textVerticalAlignment = UIVerticalAlignment.Middle;
            button.textHorizontalAlignment = UIHorizontalAlignment.Left;
            button.normalFgSprite = "IconDownArrow";
            button.hoveredFgSprite = "IconDownArrowHovered";
            button.pressedFgSprite = "IconDownArrowPressed";
            button.focusedFgSprite = "IconDownArrow";
            button.disabledFgSprite = "IconDownArrowDisabled";
            button.foregroundSpriteMode = UIForegroundSpriteMode.Scale;
            button.horizontalAlignment = UIHorizontalAlignment.Right;
            button.verticalAlignment = UIVerticalAlignment.Middle;
            button.textScale = 0.8f;

            triggerButton = button;
        }

        public void SetSettingsStyle(Vector2? size = null)
        {
            atlas = TextureUtil.InGameAtlas;
            this.size = size ?? new Vector2(400, 31);
            listBackground = "OptionsDropboxListbox";
            itemHeight = 24;
            itemHover = "ListItemHover";
            itemHighlight = "ListItemHighlight";
            normalBgSprite = "OptionsDropbox";
            hoveredBgSprite = "OptionsDropboxHovered";
            focusedBgSprite = "OptionsDropboxFocused";
            autoListWidth = true;
            listHeight = 700;
            listPosition = PopupListPosition.Below;
            clampListToScreen = false;
            foregroundSpriteMode = UIForegroundSpriteMode.Stretch;
            popupColor = Color.white;
            popupTextColor = new Color32(170, 170, 170, 255);
            textScale = 1f;
            textFieldPadding = new RectOffset(14, 40, 7, 0);
            popupColor = Color.white;
            popupTextColor = new Color32(170, 170, 170, 255);
            verticalAlignment = UIVerticalAlignment.Middle;
            horizontalAlignment = UIHorizontalAlignment.Left;
            itemPadding = new RectOffset(14, 14, 4, 0);
            triggerButton = this;
        }
    }
}
