using ColossalFramework.UI;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using UnityEngine;

namespace IMT.UI
{
    public class ColorAdvancedPropertyPanel : ColorPropertyPanel
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

        protected override Color32 PopupColor => IMTColors.ItemsBackground;

        protected override void FillContent()
        {
            base.FillContent();

            CopyButton = Content.AddUIComponent<CustomUIButton>();
            CopyButton.name = nameof(CopyButton);
            CopyButton.SetDefaultStyle();
            CopyButton.size = new Vector2(20f, 20f);
            CopyButton.atlasForeground = IMTTextures.Atlas;
            CopyButton.normalFgSprite = IMTTextures.CopyButtonIcon;
            CopyButton.tooltip = IMT.Localize.Editor_ColorCopy;
            CopyButton.eventClick += Copy;

            PasteButton = Content.AddUIComponent<CustomUIButton>();
            PasteButton.name = nameof(PasteButton);
            PasteButton.SetDefaultStyle();
            PasteButton.size = new Vector2(20f, 20f);
            PasteButton.atlasForeground = IMTTextures.Atlas;
            PasteButton.normalFgSprite = IMTTextures.PasteButtonIcon;
            PasteButton.tooltip = IMT.Localize.Editor_ColorPaste;
            PasteButton.eventClick += Paste;
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

        protected override void ColorPickerOpen(UIColorField dropdown, UIColorPicker popup, ref bool overridden)
        {
            base.ColorPickerOpen(dropdown, popup, ref overridden);

            Popup.component.size += new Vector2(0, 30);

            AddCopyButton();
            AddPasteButton();
            AddSetDefaultButton();
        }
        private CustomUIButton CreateButton(UIComponent parent, string text, int count, int of)
        {
            var width = (parent.width - (10 * (of + 1))) / of;

            var button = parent.AddUIComponent<CustomUIButton>();
            button.SetDefaultStyle();
            button.size = new Vector2(width, 20f);
            button.relativePosition = new Vector2(10 * count + width * (count - 1), 258f);
            button.textPadding = new RectOffset(0, 0, 5, 0);
            button.textScale = 0.6f;
            button.text = text;
            return button;
        }

        private void AddCopyButton()
        {
            var button = CreateButton(Popup.component, IMT.Localize.Editor_ColorCopy, 1, 3);
            button.eventClick += Copy;
        }
        private void AddPasteButton()
        {
            var button = CreateButton(Popup.component, IMT.Localize.Editor_ColorPaste, 2, 3);
            button.isEnabled = Buffer.HasValue;
            button.eventClick += Paste;
        }
        private void AddSetDefaultButton()
        {
            var button = CreateButton(Popup.component, IMT.Localize.Editor_ColorDefault, 3, 3);
            button.eventClick += SetDefault;
        }

        private void Copy(UIComponent component, UIMouseEventParameter eventParam)
        {
            Buffer = Value;
            if (Popup != null)
                Popup.component.Hide();
        }
        private void Paste(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (Buffer != null)
            {
                ValueChanged(Buffer.Value, true, OnChangedValue);
                if (Popup != null)
                    Popup.component.Hide();
            }
        }
        private void SetDefault(UIComponent component, UIMouseEventParameter eventParam) => ValueChanged(DefaultColor, true, OnChangedValue);
    }
}

