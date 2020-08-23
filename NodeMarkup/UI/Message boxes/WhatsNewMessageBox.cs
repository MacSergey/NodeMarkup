using ColossalFramework.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeMarkup.UI
{
    public class WhatsNewMessageBox : MessageBoxBase
    {
        private UIButton OkButton { get; set; }
        private UIButton GetEarlyAccessButton { get; set; }
        public Func<bool> OnButtonClick { get; set; }

        public WhatsNewMessageBox()
        {
            OkButton = AddButton(1, 1, OkClick);
            OkButton.text = NodeMarkup.Localize.MessageBox_OK;
        }
        protected virtual void OkClick()
        {
            if (OnButtonClick?.Invoke() != false)
                Cancel();
        }

        public void Init(Dictionary<Version, string> messages)
        {
            var first = default(VersionMessage);
            foreach (var message in messages)
            {
                var versionMessage = ScrollableContent.AddUIComponent<VersionMessage>();
                versionMessage.width = ScrollableContent.width;
                versionMessage.Init(message.Key, message.Value);

                if (first == null)
                    first = versionMessage;
            }
            first.IsMinimize = false;
        }

        public class VersionMessage : UIPanel
        {
            public bool IsMinimize
            {
                get => !Message.isVisible;
                set => Message.isVisible = !value;
            }
            UIButton Button { get; set; }
            UILabel Message { get; set; }
            string Label { get; set; }
            public VersionMessage()
            {
                autoLayout = true;
                autoLayoutDirection = LayoutDirection.Vertical;
                autoFitChildrenVertically = true;
                autoLayoutPadding = new RectOffset(0, 0, (int)Padding / 2, (int)Padding / 2);

                AddButton();
                AddText();
            }

            public void AddButton()
            {
                Button = AddUIComponent<UIButton>();
                Button.height = 20;
                Button.horizontalAlignment = UIHorizontalAlignment.Left;
                Button.color = Color.white;
                Button.textHorizontalAlignment = UIHorizontalAlignment.Left;
                Button.eventClick += (UIComponent component, UIMouseEventParameter eventParam) => IsMinimize = !IsMinimize;
            }

            public void AddText()
            {
                Message = AddUIComponent<UILabel>();
                Message.textAlignment = UIHorizontalAlignment.Left;
                Message.verticalAlignment = UIVerticalAlignment.Middle;
                Message.textScale = 0.8f;
                Message.wordWrap = true;
                Message.autoHeight = true;
                Message.size = new Vector2(width - 2 * Padding, 0);
                Message.relativePosition = new Vector3(17, 7);
                Message.anchor = UIAnchorStyle.CenterHorizontal | UIAnchorStyle.CenterVertical;
                Message.eventTextChanged += (UIComponent component, string value) => Message.PerformLayout();
                Message.eventVisibilityChanged += (UIComponent component, bool value) => SetLabel();
            }

            public void Init(Version version, string message)
            {
                Label = string.Format(NodeMarkup.Localize.Mod_WhatsNewVersion, version);
                Message.text = message;
                IsMinimize = true;

                SetLabel();
            }
            private void SetLabel() => Button.text = $"{(IsMinimize ? "►" : "▼")} {Label}";

            protected override void OnSizeChanged()
            {
                base.OnSizeChanged();
                if (Button != null)
                    Button.width = width;
                if (Message != null)
                    Message.width = width;
            }
        }
    }
}
