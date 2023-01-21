using ColossalFramework.UI;
using ModsCommon.UI;
using UnityEngine;

namespace IMT.UI
{
    public class ColorAdvancedPropertyPanel : ColorPropertyPanel
    {
        private static Color32? Buffer { get; set; }

        Color32? _defaultColor;
        private Color32 DefaultColor
        {
            get => _defaultColor ?? Manager.Style.DefaultColor;
            set => _defaultColor = value;
        }

        protected override void ColorPickerOpen(UIColorField dropdown, UIColorPicker popup, ref bool overridden)
        {
            base.ColorPickerOpen(dropdown, popup, ref overridden);

            AddCopyButton();
            AddPasteButton();
            AddSetDefaultButton();
        }
        private CustomUIButton CreateButton(UIComponent parent, string text, int count, int of)
        {
            var width = (parent.width - (10 * (of + 1))) / of;

            var button = AddButton(parent);
            button.size = new Vector2(width, 20f);
            button.relativePosition = new Vector2(10 * count + width * (count - 1), 223f);
            button.textPadding = new RectOffset(0, 0, 5, 0);
            button.textScale = 0.6f;
            button.text = text;
            return button;
        }

        private void AddCopyButton()
        {
            var button = CreateButton(Popup.component, IMT.Localize.Editor_ColorCopy, 1, 3);
            button.eventClick += (UIComponent component, UIMouseEventParameter eventParam) => Copy();
        }
        private void AddPasteButton()
        {
            var button = CreateButton(Popup.component, IMT.Localize.Editor_ColorPaste, 2, 3);
            button.isEnabled = Buffer.HasValue;
            button.eventClick += (UIComponent component, UIMouseEventParameter eventParam) => Paste();
        }
        private void AddSetDefaultButton()
        {
            var button = CreateButton(Popup.component, IMT.Localize.Editor_ColorDefault, 3, 3);
            button.eventClick += (UIComponent component, UIMouseEventParameter eventParam) => SetDefault();
        }

        private void Copy()
        {
            Buffer = Value;
            if (Popup != null)
                Popup.component.Hide();
        }
        private void Paste()
        {
            if (Buffer != null)
            {
                ValueChanged(Buffer.Value, true, OnChangedValue);
                if (Popup != null)
                    Popup.component.Hide();
            }
        }
        private void SetDefault() => ValueChanged(DefaultColor, true, OnChangedValue);

        public void Init(Color32? defaultColor = null)
        {
            _defaultColor = defaultColor;
            base.Init();
        }
        public override void DeInit()
        {
            base.DeInit();
            _defaultColor = null;
        }
    }
}

