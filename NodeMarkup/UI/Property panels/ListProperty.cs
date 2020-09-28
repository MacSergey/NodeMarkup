using ColossalFramework.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            DropDown.SetDefaultStyle();
            DropDown.OnSelectObjectChanged += DropDownValueChanged;
            DropDown.eventDropdownOpen += DropDownOpen;
            DropDown.eventDropdownClose += DropDownClose;
        }
        private void DropDownValueChanged(Type value) => OnSelectObjectChanged?.Invoke(value);
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
        public event Action<ValueType> OnSelectObjectChanged;

        public Func<ValueType, ValueType, bool> IsEqualDelegate { get; set; }
        List<ValueType> Objects { get; } = new List<ValueType>();
        public ValueType SelectedObject
        {
            get => selectedIndex >= 0 ? Objects[selectedIndex] : default;
            set => selectedIndex = Objects.FindIndex(o => IsEqualDelegate?.Invoke(o, value) ?? ReferenceEquals(o, value) || o.Equals(value));
        }

        public CustomUIDropDown()
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
            Objects.Clear();
            items = new string[0];
        }
        protected override void OnMouseWheel(UIMouseEventParameter p) { }

        public UIButton SetDefaultStyle(Vector2? size = null)
        {
            atlas = EditorItem.EditorItemAtlas;
            this.size = size ?? new Vector2(230, 20);
            listBackground = "TextFieldPanelHovered";
            itemHeight = 20;
            itemHover = "TextFieldPanel";
            itemHighlight = "TextFieldPanelFocus";
            normalBgSprite = "TextFieldPanel";
            hoveredBgSprite = "TextFieldPanelHovered";
            listWidth = 230;
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

            return button;
        }

        public void SetSettingsStyle(Vector2? size = null)
        {
            atlas = TextureUtil.InGameAtlas;
            this.size = size ?? new Vector2(400, 38);
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
            textScale = 1.25f;
            textFieldPadding = new RectOffset(14, 40, 7, 0);
            popupColor = Color.white;
            popupTextColor = new Color32(170, 170, 170, 255);
            verticalAlignment = UIVerticalAlignment.Middle;
            horizontalAlignment = UIHorizontalAlignment.Left;
            itemPadding = new RectOffset(14, 14, 0, 0);
            triggerButton = this;
        }
    }
}
