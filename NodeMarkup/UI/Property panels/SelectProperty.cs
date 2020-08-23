using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
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

        UIButton Selector { get; set; }
        UIButton Button { get; set; }
        protected abstract float Width {get;}

        int SelectIndex
        {
            get => _selectIndex;
            set
            {
                if (value != _selectIndex)
                {
                    _selectIndex = value;
                    OnSelectChanged?.Invoke(SelectedObject);
                    Selector.text = SelectedObject?.ToString() ?? NodeMarkup.Localize.SelectPanel_NotSet;
                }
            }
        }
        List<Type> ObjectsList { get; set; } = new List<Type>();
        public IEnumerable<Type> Objects => ObjectsList;
        public Type SelectedObject
        {
            get => SelectIndex == -1 ? default : ObjectsList[SelectIndex];
            set => SelectIndex = ObjectsList.FindIndex(o => IsEqual(value, o));
        }

        public SelectPropertyPanel()
        {
            AddSelector();
        }
        private void AddSelector()
        {
            Selector = Control.AddUIComponent<UIButton>();
            Selector.text = NodeMarkup.Localize.SelectPanel_NotSet;
            Selector.atlas = EditorItemAtlas;
            Selector.normalBgSprite = "TextFieldPanel";
            Selector.hoveredBgSprite = "TextFieldPanelHovered";
            Selector.isInteractive = true;
            Selector.enabled = true;
            Selector.autoSize = false;
            Selector.textHorizontalAlignment = UIHorizontalAlignment.Left;
            Selector.textVerticalAlignment = UIVerticalAlignment.Middle;
            Selector.height = 20;
            Selector.width = Width;
            Selector.textScale = 0.6f;
            Selector.textPadding = new RectOffset(8, 0, 4, 0);

            Button = Selector.AddUIComponent<UIButton>();
            Button.atlas = TextureUtil.InGameAtlas;
            Button.text = string.Empty;
            Button.size = Selector.size;
            Button.relativePosition = new Vector3(0f, 0f);
            Button.textVerticalAlignment = UIVerticalAlignment.Middle;
            Button.textHorizontalAlignment = UIHorizontalAlignment.Left;
            Button.normalFgSprite = "IconDownArrow";
            Button.hoveredFgSprite = "IconDownArrowHovered";
            Button.pressedFgSprite = "IconDownArrowPressed";
            Button.focusedFgSprite = "IconDownArrow";
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
            ObjectsList.Add(item);
        }
        public void AddRange(IEnumerable<Type> items)
        {
            ObjectsList.AddRange(items);
        }
        public void Clear()
        {
            ObjectsList.Clear();
            SelectedObject = default;
        }

        protected abstract bool IsEqual(Type first, Type second);
        public new void Focus() => Button.Focus();
    }

    public class MarkupLineSelectPropertyPanel : SelectPropertyPanel<ILinePartEdge>
    {
        public new event Action<MarkupLineSelectPropertyPanel> OnSelect;
        public new event Action<MarkupLineSelectPropertyPanel> OnHover;
        public new event Action<MarkupLineSelectPropertyPanel> OnLeave;

        public EdgePosition Position { get; set; }
        protected override float Width => 230f;

        protected override void ButtonClick(UIComponent component, UIMouseEventParameter eventParam) => OnSelect?.Invoke(this);
        protected override void ButtonMouseEnter(UIComponent component, UIMouseEventParameter eventParam) => OnHover?.Invoke(this);
        protected override void ButtonMouseLeave(UIComponent component, UIMouseEventParameter eventParam) => OnLeave?.Invoke(this);

        protected override bool IsEqual(ILinePartEdge first, ILinePartEdge second) => (first == null && second == null) || first?.Equals(second) == true;
    }
    public class MarkupCrosswalkSelectPropertyPanel : SelectPropertyPanel<MarkupRegularLine>
    {
        public new event Action<MarkupCrosswalkSelectPropertyPanel> OnSelect;
        public new event Action<MarkupCrosswalkSelectPropertyPanel> OnHover;
        public new event Action<MarkupCrosswalkSelectPropertyPanel> OnLeave;

        public BorderPosition Position { get; set; }
        protected override float Width => 150f;

        public MarkupCrosswalkSelectPropertyPanel()
        {
            AddReset();
        }
        private void AddReset()
        {
            var button = AddButton(Control);

            button.size = new Vector2(20f, 20f);
            button.text = "×";
            button.tooltip = NodeMarkup.Localize.CrosswalkStyle_ResetBorder;
            button.textScale = 1.3f;
            button.textPadding = new RectOffset(0, 0, 0, 0);
            button.eventClick += ResetClick;
        }
        private void ResetClick(UIComponent component, UIMouseEventParameter eventParam) => SelectedObject = null;

        protected override void ButtonClick(UIComponent component, UIMouseEventParameter eventParam) => OnSelect?.Invoke(this);
        protected override void ButtonMouseEnter(UIComponent component, UIMouseEventParameter eventParam) => OnHover?.Invoke(this);
        protected override void ButtonMouseLeave(UIComponent component, UIMouseEventParameter eventParam) => OnLeave?.Invoke(this);

        protected override bool IsEqual(MarkupRegularLine first, MarkupRegularLine second) => ReferenceEquals(first, second);
    }
}
