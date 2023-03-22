using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using UnityEngine;

namespace IMT.UI
{
    public class MoreOptionsPanel : EditorItem, IReusable
    {
        bool IReusable.InCache { get; set; }
        protected CustomUIButton Button { get; set; }
        protected override float DefaultHeight => 24f;


        public event Action OnButtonClick;

        public string Text
        {
            get => Button.text;
            set => Button.text = value;
        }

        public MoreOptionsPanel()
        {
            Button = AddUIComponent<CustomUIButton>();
            Button.textScale = 0.8f;
            Button.textColor = CommonColors.White;
            Button.hoveredTextColor = CommonColors.Gray224;
            Button.pressedTextColor = CommonColors.Gray192;
            Button.disabledTextColor = CommonColors.Gray128;
            Button.textPadding = new RectOffset(0, 0, 3, 0);
            Button.isEnabled = EnableControl;
            Button.eventClick += ButtonClick;
        }

        protected override void Init(float? height)
        {
            base.Init(height);
            SetSize();
        }
        public override void DeInit()
        {
            base.DeInit();

            Text = string.Empty;
            OnButtonClick = null;
        }

        private void ButtonClick(UIComponent component, UIMouseEventParameter eventParam) => OnButtonClick?.Invoke();

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            SetSize();
        }
        protected virtual void SetSize()
        {
            Button.size = size;
        }
    }
}
