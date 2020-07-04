using ColossalFramework.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class ColorPropertyPanel : EditorPropertyPanel
    {
        public event Action<Color32> OnValueChanged;

        private UITextField R { get; set; }
        private UITextField G { get; set; }
        private UITextField B { get; set; }
        private UITextField A { get; set; }
        private UIColorField ColorSample { get; set; }

        public Color32 Value
        {
            get
            {
                var color = new Color32(CetComponent(R.text), CetComponent(G.text), CetComponent(B.text), CetComponent(A.text));
                return color;
            }
            set
            {
                R.text = value.r.ToString();
                G.text = value.g.ToString();
                B.text = value.b.ToString();
                A.text = value.a.ToString();

                SetSampleColor();
            }
        }
        private byte CetComponent(string text) => byte.TryParse(text, out byte value) ? value : byte.MaxValue;

        public ColorPropertyPanel()
        {
            R = AddField(nameof(R));
            G = AddField(nameof(G));
            B = AddField(nameof(B));
            A = AddField(nameof(A));

            AddColorSample();
        }

        private UITextField AddField(string name)
        {
            var lable = Control.AddUIComponent<UILabel>();
            lable.text = name;
            lable.textScale = 0.7f;

            var field = Control.AddUIComponent<UITextField>();
            field.atlas = TextureUtil.GetAtlas("Ingame");
            field.normalBgSprite = "TextFieldPanel";
            field.hoveredBgSprite = "TextFieldPanelHovered";
            field.focusedBgSprite = "TextFieldPanel";
            field.selectionSprite = "EmptySprite";
            field.allowFloats = true;
            field.isInteractive = true;
            field.enabled = true;
            field.readOnly = false;
            field.builtinKeyNavigation = true;
            field.cursorWidth = 1;
            field.cursorBlinkTime = 0.45f;
            field.eventTextChanged += FieldTextChanged;
            field.width = 30;
            field.textScale = 0.7f;
            //field.text = 0.ToString();
            field.selectOnFocus = true;
            field.verticalAlignment = UIVerticalAlignment.Middle;

            return field;
        }
        private void AddColorSample()
        {
            ColorSample = Control.AddUIComponent<UIColorField>();
            ColorSample.size = new Vector2(26, 28);
            ColorSample.normalBgSprite = "ColorPickerOutlineHovered";
            ColorSample.normalFgSprite = "ColorPickerColor";
            ColorSample.hoveredBgSprite = "ColorPickerOutline";

            var button = ColorSample.AddUIComponent<UIButton>();
            button.size = ColorSample.size;
            button.relativePosition = new Vector3(0, 0);

            ColorSample.triggerButton = button;


            GameObject gameObject = new GameObject(typeof(UIColorPicker).Name);
            gameObject.transform.parent = cachedTransform;
            gameObject.layer = base.gameObject.layer;
            var colorPicker = gameObject.AddComponent<UIColorPicker>();

            ColorSample.colorPicker = colorPicker;

            if (!(UITemplateManager.Get("LineTemplate") is UIComponent template))
                return;

            var colorFieldTemplate = template.Find<UIColorField>("LineColor");

            ColorSample = Instantiate(colorFieldTemplate.gameObject).GetComponent<UIColorField>();
            Control.AttachUIComponent(ColorSample.gameObject);
            ColorSample.size = new Vector2(26f, 28f);
            ColorSample.eventSelectedColorChanged += SelectedColorChanged;

            //if (!(UITemplateManager.Get("LineTemplate") is UIComponent template))
            //    return;

            //var colorFieldTemplate = template.Find<UIColorField>("LineColor");
        }

        private void SelectedColorChanged(UIComponent component, Color value)
        {
            value.a = Value.a;
            Value = value;
        }
        private void SetSampleColor()
        {
            ColorSample.selectedColor = Value;
        }
        protected virtual void FieldTextChanged(UIComponent component, string text)
        {
            SetSampleColor();
            OnValueChanged?.Invoke(Value);
        }
    }
}
