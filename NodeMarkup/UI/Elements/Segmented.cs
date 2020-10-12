using ColossalFramework.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI
{
    public class UISegmented<ValueType> : UIPanel, IUISelector<ValueType>
    {
        public event Action<ValueType> OnSelectObjectChanged;

        public Func<ValueType, ValueType, bool> IsEqualDelegate { get; set; }
        List<ValueType> Objects { get; } = new List<ValueType>();
        List<UIButton> Buttons { get; } = new List<UIButton>();

        int _selectedIndex = -1;
        int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex == value)
                    return;

                if (_selectedIndex != -1)
                    SetSprite(Buttons[_selectedIndex], false);

                _selectedIndex = value;

                if (_selectedIndex != -1)
                {
                    SetSprite(Buttons[_selectedIndex], true);
                    OnSelectObjectChanged.Invoke(SelectedObject);
                }
            }
        }

        public ValueType SelectedObject
        {
            get => SelectedIndex >= 0 ? Objects[SelectedIndex] : default;
            set => SelectedIndex = Objects.FindIndex(o => IsEqualDelegate?.Invoke(o, value) ?? ReferenceEquals(o, value) || o.Equals(value));
        }

        public UISegmented()
        {
            autoLayout = true;
            autoLayoutDirection = LayoutDirection.Horizontal;
            autoFitChildrenHorizontally = true;
            autoFitChildrenVertically = true;
        }

        public void AddItem(ValueType item, string label = null)
        {
            Objects.Add(item);

            var button = AddUIComponent<UIButton>();

            button.atlas = EditorItem.EditorItemAtlas;
            SetSprite(button, false);
            button.text = label ?? item.ToString();
            button.textScale = 0.8f;
            button.textPadding = new RectOffset(8, 8, 4, 0);
            button.autoSize = true;
            button.autoSize = false;
            button.height = 20;
            button.eventClick += ButtonClick;

            Buttons.Add(button);
        }
        private void SetSprite(UIButton button, bool isSelect)
        {
            if(isSelect)
            {
                button.normalBgSprite = "TextFieldPanelFocus";
                button.hoveredBgSprite = "TextFieldPanelFocus";
                button.pressedBgSprite = "TextFieldPanelFocus";
            }
            else
            {
                button.normalBgSprite = "TextFieldPanel";
                button.hoveredBgSprite = "TextFieldPanelHovered";
                button.pressedBgSprite = "TextFieldPanelHovered";
            }
        }

        private void ButtonClick(UIComponent component, UIMouseEventParameter eventParam) => SelectedIndex = Buttons.FindIndex(b => b == component);

        public void Clear()
        {
            Objects.Clear();

            var components = this.components.ToArray();
            foreach(var component in components)
            {
                RemoveUIComponent(component);
                Destroy(component);
            }
        }

        public void SetDefaultStyle(Vector2? size = null) { }
    }
}
