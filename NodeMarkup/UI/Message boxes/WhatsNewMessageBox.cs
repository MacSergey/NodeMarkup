using ColossalFramework.UI;
using ModsCommon.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI
{
    public class WhatsNewMessageBox : MessageBoxBase
    {
        public Func<bool> OnButtonClick { get; set; }

        public WhatsNewMessageBox()
        {
            var okButton = AddButton(1, 1, OkClick);
            okButton.text = NodeMarkupMessageBox.Ok;
        }
        protected virtual void OkClick()
        {
            if (OnButtonClick?.Invoke() != false)
                Close();
        }

        public virtual void Init(Dictionary<Version, string> messages)
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
                Message.relativePosition = new Vector3(17, 7);
                Message.anchor = UIAnchorStyle.CenterHorizontal | UIAnchorStyle.CenterVertical;
                Message.eventTextChanged += (UIComponent component, string value) => Message.PerformLayout();
                Message.eventVisibilityChanged += (UIComponent component, bool value) => SetLabel();
            }

            public void Init(Version version, string message)
            {
                Label = string.Format(NodeMarkup.Localize.Mod_WhatsNewVersion, Mod.IsBeta && version == Mod.Version ? $"{version} [BETA]" : version.ToString());
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
    public class BetaWhatsNewMessageBox : WhatsNewMessageBox
    {
        public BetaWhatsNewMessageBox()
        {
            var getStableButton = AddButton(1, 1, OnGetStable);
            getStableButton.text = NodeMarkup.Localize.Mod_BetaWarningGetStable;
            SetButtonsRatio(1, 2);
        }
        private void OnGetStable() => Mod.GetStable();

        public override void Init(Dictionary<Version, string> messages)
        {
            var betaMessage = ScrollableContent.AddUIComponent<UILabel>();
            betaMessage.wordWrap = true;
            betaMessage.autoHeight = true;
            betaMessage.textColor = Color.red;
            betaMessage.text = string.Format(NodeMarkup.Localize.Mod_BetaWarningMessage, Mod.StaticName);

            base.Init(messages);
        }
    }
}
