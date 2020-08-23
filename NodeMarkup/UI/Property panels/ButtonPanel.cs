using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class ButtonPanel : EditorItem
    {
        protected UIButton Button { get; set; }

        public string Text
        {
            get => Button.text;
            set => Button.text = value;
        }

        public event Action OnButtonClick;

        public ButtonPanel()
        {
            Button = AddButton(this);
            Button.eventClick += ButtonClick;
        }

        private void ButtonClick(UIComponent component, UIMouseEventParameter eventParam) => OnButtonClick?.Invoke();

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            Button.size = size;
        }
    }
    public class ButtonsPanel : EditorItem
    {
        public event Action<int> OnButtonClick;
        protected List<UIButton> Buttons { get; } = new List<UIButton>();
        public int Count => Buttons.Count;
        private float Padding => 10f;
        private float Height => 20f;

        public int AddButton(string text)
        {
            var button = AddButton(this);

            button.text = text;
            button.textScale = 0.8f;
            button.textPadding = new RectOffset(0, 0, 3, 0);
            button.eventClick += ButtonClick;

            Buttons.Add(button);

            return Count - 1;
        }

        private void ButtonClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (component is UIButton button)
            {
                var index = Buttons.IndexOf(button);
                if (index != -1)
                    OnButtonClick?.Invoke(index);
            }
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            var buttonWidth = (width - Padding * (Count - 1)) / Count;
            for(var i = 0; i < Count; i +=1)
            {
                Buttons[i].size = new Vector2(buttonWidth, Height);
                Buttons[i].relativePosition = new Vector2((buttonWidth + Padding) * i, (height - Height) / 2);
            }
        }
    }
}
