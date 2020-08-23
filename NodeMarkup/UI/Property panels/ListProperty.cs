using ColossalFramework.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public abstract class ListPropertyPanel<Type, DropDownType> : EditorPropertyPanel
        where DropDownType : CustomUIDropDown<Type>
    {
        public event Action<Type> OnSelectObjectChanged;
        public event Action<bool> OnDropDownStateChange;

        protected DropDownType DropDown { get; set; }

        public bool AllowNull { get; set; } = true;
        public string NullText { get; set; } = string.Empty;
        public bool IsOpen { get; private set; } = false;

        public Type SelectedObject
        {
            get => DropDown.SelectedObject;
            set => DropDown.SelectedObject = value;
        }

        public ListPropertyPanel()
        {
            AddDropDown();
            DropDown.IsEqualDelegate = IsEqual;
        }
        private void AddDropDown()
        {
            DropDown = Control.AddUIComponent<DropDownType>();

            CustomUIDropDown<Type>.SetDefault(DropDown);
            DropDown.eventSelectedIndexChanged += DropDownIndexChanged;
            DropDown.eventDropdownOpen += DropDownOpen;
            DropDown.eventDropdownClose += DropDownClose;
        }

        private void DropDownClose(UIDropDown dropdown, UIListBox popup, ref bool overridden)
        {
            IsOpen = false;
            OnDropDownStateChange?.Invoke(false);
        }

        private void DropDownOpen(UIDropDown dropdown, UIListBox popup, ref bool overridden)
        {
            IsOpen = true;
            OnDropDownStateChange?.Invoke(true);
        }
        private void DropDownIndexChanged(UIComponent component, int value) => OnSelectObjectChanged?.Invoke(DropDown.SelectedObject);

        public override void Init()
        {
            base.Init();
            DropDown.Clear();

            if (AllowNull)
                DropDown.AddItem(default, NullText ?? string.Empty);
        }
        public void Add(Type item)
        {
            DropDown.AddItem(item);
        }
        public void AddRange(IEnumerable<Type> items)
        {
            foreach (var item in items)
            {
                DropDown.AddItem(item);
            }
        }
        protected abstract bool IsEqual(Type first, Type second);
    }
    public abstract class CustomUIDropDown<ValueType> : UIDropDown
    {
        public Func<ValueType, ValueType, bool> IsEqualDelegate { get; set; }
        List<ValueType> Objects { get; } = new List<ValueType>();
        public ValueType SelectedObject
        {
            get => selectedIndex >= 0 ? Objects[selectedIndex] : default;
            set => selectedIndex = Objects.FindIndex(o => IsEqualDelegate?.Invoke(o, value) ?? ReferenceEquals(o, value) || o.Equals(value));
        }

        public void AddItem(ValueType item, string label = null)
        {
            Objects.Add(item);
            AddItem(label ?? item.ToString());
        }
        public void Clear()
        {
            Objects.Clear();
            items = new string[0];
        }
        protected override void OnMouseWheel(UIMouseEventParameter p) { }

        public static UIButton SetDefault(UIDropDown dropDown, Vector2? size = null)
        {
            dropDown.atlas = EditorItem.EditorItemAtlas;
            dropDown.size = size ?? new Vector2(230, 20);
            dropDown.listBackground = "TextFieldPanelHovered";
            dropDown.itemHeight = 20;
            dropDown.itemHover = "TextFieldPanel";
            dropDown.itemHighlight = "TextFieldPanelFocus";
            dropDown.normalBgSprite = "TextFieldPanel";
            dropDown.hoveredBgSprite = "TextFieldPanelHovered";
            dropDown.listWidth = 230;
            dropDown.listHeight = 700;
            dropDown.listPosition = PopupListPosition.Below;
            dropDown.clampListToScreen = true;
            dropDown.foregroundSpriteMode = UIForegroundSpriteMode.Stretch;
            dropDown.popupColor = new Color32(45, 52, 61, 255);
            dropDown.popupTextColor = new Color32(170, 170, 170, 255);
            dropDown.textScale = 0.7f;
            dropDown.textFieldPadding = new RectOffset(8, 0, 6, 0);
            dropDown.popupColor = Color.white;
            dropDown.popupTextColor = Color.black;
            dropDown.verticalAlignment = UIVerticalAlignment.Middle;
            dropDown.horizontalAlignment = UIHorizontalAlignment.Left;
            dropDown.itemPadding = new RectOffset(14, 0, 8, 0);

            var button = dropDown.AddUIComponent<UIButton>();
            button.atlas = TextureUtil.InGameAtlas;
            button.text = string.Empty;
            button.size = dropDown.size;
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

            dropDown.triggerButton = button;

            return button;
        }
    }
}
