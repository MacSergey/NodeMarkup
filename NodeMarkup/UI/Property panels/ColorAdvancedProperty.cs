using ColossalFramework.UI;
using ModsCommon.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace IMT.UI
{
    public class ColorAdvancedPropertyPanel : ColorPropertyPanel
    {
        private static Color32? Buffer { get; set; }

        protected override void ColorPickerOpen(UIColorField dropdown, UIColorPicker popup, ref bool overridden)
        {
            base.ColorPickerOpen(dropdown, popup, ref overridden);

            AddCopyButton();
            AddPasteButton();
            AddSetDefaultButton();
        }
        private UIButton CreateButton(UIComponent parent, string text, int count, int of)
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
                Value = Buffer.Value;
                if (Popup != null)
                    Popup.component.Hide();
            }
        }
        private void SetDefault() => Value = Manager.Style.DefaultColor;
    }
}

