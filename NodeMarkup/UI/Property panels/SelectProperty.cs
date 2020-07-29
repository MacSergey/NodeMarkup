using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public abstract class SelectPropertyPanel<Type> : EditorPropertyPanel
    {
        public event Action<Type> OnSelectChanged;
        public event Action OnSelect;
        public event Action OnHover;
        public event Action OnLeave;

        int _selectIndex = -1;

        UILabel Label { get; set; }
        UIButton Button { get; set; }

        int SelectIndex
        {
            get => _selectIndex;
            set
            {
                if (value != _selectIndex)
                {
                    _selectIndex = value;
                    OnSelectChanged?.Invoke(SelectedObject);
                    Label.text = SelectedObject?.ToString() ?? NodeMarkup.Localize.SelectPanel_NotSet;
                }
            }
        }
        List<Type> Objects { get; set; } = new List<Type>();
        public Type SelectedObject
        {
            get => SelectIndex == -1 ? default : Objects[SelectIndex];
            set => SelectIndex = Objects.FindIndex(o => IsEqual(value, o));
        }

        public SelectPropertyPanel()
        {
            AddLable();
            AddButton();
        }
        private void AddLable()
        {
            Label = Control.AddUIComponent<UILabel>();
            Label.text = NodeMarkup.Localize.SelectPanel_NotSet;
            Label.atlas = NodeMarkupTool.InGameAtlas;
            Label.backgroundSprite = "TextFieldPanel";
            Label.isInteractive = true;
            Label.enabled = true;
            Label.autoSize = false;
            Label.textAlignment = UIHorizontalAlignment.Left;
            Label.verticalAlignment = UIVerticalAlignment.Middle;
            Label.height = 20;
            Label.width = 230;
            Label.textScale = 0.7f;
            Label.padding = new RectOffset(8, 0, 2, 0);
        }

        private void AddButton()
        {
            Button = Label.AddUIComponent<UIButton>();
            Button.atlas = NodeMarkupTool.InGameAtlas;
            Button.text = string.Empty;
            Button.size = Label.size;
            Button.relativePosition = new Vector3(0f, 0f);
            Button.textVerticalAlignment = UIVerticalAlignment.Middle;
            Button.textHorizontalAlignment = UIHorizontalAlignment.Left;
            Button.normalFgSprite = "IconDownArrow";
            Button.hoveredFgSprite = "IconDownArrowHovered";
            Button.pressedFgSprite = "IconDownArrowPressed";
            Button.focusedFgSprite = "IconDownArrowFocused";
            Button.disabledFgSprite = "IconDownArrowDisabled";
            Button.foregroundSpriteMode = UIForegroundSpriteMode.Scale;
            Button.horizontalAlignment = UIHorizontalAlignment.Right;
            Button.verticalAlignment = UIVerticalAlignment.Middle;
            Button.textScale = 0.8f;

            Button.eventClick += ButtonClick;
            Button.eventMouseEnter += ButtonMouseEnter;
            Button.eventMouseLeave += ButtonMouseLeave;
        }


        protected virtual void ButtonClick(UIComponent component, UIMouseEventParameter eventParam) => OnSelect?.Invoke();
        protected virtual void ButtonMouseEnter(UIComponent component, UIMouseEventParameter eventParam) => OnHover?.Invoke();
        protected virtual void ButtonMouseLeave(UIComponent component, UIMouseEventParameter eventParam) => OnLeave?.Invoke();

        public void Add(Type item)
        {
            Objects.Add(item);
        }
        public void AddRange(IEnumerable<Type> items)
        {
            Objects.AddRange(items);
        }

        protected abstract bool IsEqual(Type first, Type second);
        public new void Focus() => Button.Focus();
    }

    public class MarkupLineSelectPropertyPanel : SelectPropertyPanel<ILinePartEdge>
    {
        public new event Action<MarkupLineSelectPropertyPanel> OnSelect;
        public new event Action<MarkupLineSelectPropertyPanel> OnHover;
        public new event Action<MarkupLineSelectPropertyPanel> OnLeave;

        public RulePosition Position { get; set; }

        protected override void ButtonClick(UIComponent component, UIMouseEventParameter eventParam) => OnSelect?.Invoke(this);
        protected override void ButtonMouseEnter(UIComponent component, UIMouseEventParameter eventParam) => OnHover?.Invoke(this);
        protected override void ButtonMouseLeave(UIComponent component, UIMouseEventParameter eventParam) => OnLeave?.Invoke(this);

        protected override bool IsEqual(ILinePartEdge first, ILinePartEdge second) => (first == null && second == null) || first?.Equals(second) == true;
    }
}
