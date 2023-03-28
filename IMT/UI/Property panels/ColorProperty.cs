using ColossalFramework.UI;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using UnityEngine;

namespace IMT.UI
{
    public class IMTColorPropertyPanel : ColorPropertyPanel<IMTColorPicker, IMTColorPickerPopup>
    {
        private static Color32? Buffer { get; set; }

        Color32? defaultColor;
        public Color32 DefaultColor
        {
            get => defaultColor ?? Manager.Style.DefaultMarkingColor;
            set => defaultColor = value;
        }

        private CustomUIButton CopyButton { get; set; }
        private CustomUIButton PasteButton { get; set; }

        protected override void FillContent()
        {
            base.FillContent();

            CopyButton = Content.AddUIComponent<CustomUIButton>();
            CopyButton.name = nameof(CopyButton);
            CopyButton.SetDefaultStyle();
            CopyButton.size = new Vector2(20f, 20f);
            CopyButton.FgAtlas = IMTTextures.Atlas;
            CopyButton.FgSprites = IMTTextures.CopyButtonIcon;
            CopyButton.tooltip = IMT.Localize.Editor_ColorCopy;
            CopyButton.eventClick += (_, _) => Copy();

            PasteButton = Content.AddUIComponent<CustomUIButton>();
            PasteButton.name = nameof(PasteButton);
            PasteButton.SetDefaultStyle();
            PasteButton.size = new Vector2(20f, 20f);
            PasteButton.FgAtlas = IMTTextures.Atlas;
            PasteButton.FgSprites = IMTTextures.PasteButtonIcon;
            PasteButton.tooltip = IMT.Localize.Editor_ColorPaste;
            PasteButton.eventClick += (_, _) => Paste();

            ColorPicker.OnAfterPopupOpen += ColorPickerPopupOpen;
        }

        private void ColorPickerPopupOpen(IMTColorPickerPopup popup)
        {
            popup.OnCopy += Copy;
            popup.OnPaste += Paste;
            popup.OnDefault += SetDefault;

            popup.CanPaste = Buffer.HasValue;
        }

        public void Init(Color32? defaultColor = null)
        {
            this.defaultColor = defaultColor;
            base.Init();
        }
        public override void DeInit()
        {
            base.DeInit();
            defaultColor = null;
        }

        private void Copy()
        {
            Buffer = Value;
            if (ColorPicker.Popup != null)
                ColorPicker.Popup.CanPaste = true;
        }
        private void Paste()
        {
            if (Buffer != null)
                ValueChanged(Buffer.Value, true, OnChangedValue);
        }
        private void SetDefault() => ValueChanged(DefaultColor, true, OnChangedValue);
    }

    public class IMTColorPicker : ColorPickerButton<IMTColorPickerPopup> { }
    public class IMTColorPickerPopup : ColorPickerPopup
    {
        public event Action OnCopy;
        public event Action OnPaste;
        public event Action OnDefault;

        private CustomUIButton PasteButton { get; set; }
        public bool CanPaste
        {
            set => PasteButton.isEnabled = value;
        }

        protected override void FillPopup()
        {
            base.FillPopup();

            var buttonPanel = AddUIComponent<CustomUIPanel>();
            buttonPanel.PauseLayout(() =>
            {
                buttonPanel.AutoLayout = AutoLayout.Horizontal;
                buttonPanel.AutoChildrenHorizontally = AutoLayoutChildren.Fit;
                buttonPanel.AutoChildrenVertically = AutoLayoutChildren.Fit;
                buttonPanel.AutoLayoutSpace = 10;

                var copyButton = CreateButton(buttonPanel, IMT.Localize.Editor_ColorCopy);
                copyButton.eventClick += Copy;

                PasteButton = CreateButton(buttonPanel, IMT.Localize.Editor_ColorPaste);
                PasteButton.eventClick += Paste;

                var defaultButton = CreateButton(buttonPanel, IMT.Localize.Editor_ColorDefault);
                defaultButton.eventClick += SetDefault;
            });
        }
        public override void DeInit()
        {
            base.DeInit();

            OnCopy = null;
            OnPaste = null;
            OnDefault = null;
        }

        private CustomUIButton CreateButton(UIComponent parent, string text)
        {
            var button = parent.AddUIComponent<CustomUIButton>();
            button.SetDefaultStyle();
            button.size = new Vector2(70f, 20f);
            button.TextPadding = new RectOffset(0, 0, 5, 0);
            button.textScale = 0.6f;
            button.text = text;
            return button;
        }

        private void Copy(UIComponent component, UIMouseEventParameter eventParam) => OnCopy?.Invoke();
        private void Paste(UIComponent component, UIMouseEventParameter eventParam) => OnPaste?.Invoke();
        private void SetDefault(UIComponent component, UIMouseEventParameter eventParam) => OnDefault?.Invoke();
    }
}

