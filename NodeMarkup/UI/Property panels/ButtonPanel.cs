using ColossalFramework.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            Button = AddUIComponent<UIButton>();

            Button.atlas = NodeMarkupTool.InGameAtlas;
            Button.normalBgSprite = "ButtonWhite";
            Button.disabledBgSprite = "ButtonWhiteDisabled";
            Button.hoveredBgSprite = "ButtonWhiteHovered";
            Button.pressedBgSprite = "ButtonWhitePressed";
            Button.textColor = Color.black;

            Button.eventClick += ButtonClick;
        }

        private void ButtonClick(UIComponent component, UIMouseEventParameter eventParam) => OnButtonClick?.Invoke();

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            Button.size = size;
        }
    }
}
