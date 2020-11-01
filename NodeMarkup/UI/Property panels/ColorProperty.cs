using ColossalFramework.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class ColorPropertyPanel : EditorPropertyPanel, IReusable
    {
        private static Color32? Buffer { get; set; }

        public event Action<Color32> OnValueChanged;

        private bool InProcess { get; set; } = false;

        private static string OpacitySlider { get; } = nameof(OpacitySlider);
        private static string ColorPickerNormal { get; } = nameof(ColorPickerNormal);
        private static string ColorPickerHover { get; } = nameof(ColorPickerHover);
        private static string ColorPickerColor { get; } = nameof(ColorPickerColor);
        private static UITextureAtlas OpacitySliderAtlas { get; } = TextureUtil.CreateTextureAtlas("OpacitySlider.png", nameof(OpacitySliderAtlas), 18, 200, new string[] { OpacitySlider });
        private static UITextureAtlas ColorPickerAtlas { get; } = TextureUtil.CreateTextureAtlas("ColorPicker.png", nameof(ColorPickerAtlas), 43, 49, new string[] { ColorPickerNormal, ColorPickerHover, ColorPickerColor });

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
                if (!InProcess)
                {
                    InProcess = true;

                    R.text = value.r.ToString();
                    G.text = value.g.ToString();
                    B.text = value.b.ToString();
                    A.text = value.a.ToString();

                    if (ColorSample != null)
                        ColorSample.selectedColor = value;

                    OnValueChanged?.Invoke(value);

                    InProcess = false;
                }
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
        public override void DeInit()
        {
            base.DeInit();

            OnValueChanged = null;
        }
        private UITextField AddField(string name)
        {
            var lable = Control.AddUIComponent<UILabel>();
            lable.text = name;
            lable.textScale = 0.7f;

            var field = AddTextField(Control);
            field.width = 30;
            field.eventTextSubmitted += FieldTextSubmitted;

            return field;
        }

        private void AddColorSample()
        {
            if (!(UITemplateManager.Get("LineTemplate") is UIComponent template))
                return;

            var colorFieldTemplate = template.Find<UIColorField>("LineColor");

            ColorSample = Instantiate(colorFieldTemplate.gameObject).GetComponent<UIColorField>();
            Control.AttachUIComponent(ColorSample.gameObject);
            ColorSample.anchor = UIAnchorStyle.None;
            ColorSample.size = new Vector2(26f, 28f);
            ColorSample.atlas = ColorPickerAtlas;
            ColorSample.normalBgSprite = ColorPickerNormal;
            ColorSample.hoveredBgSprite = ColorPickerHover;
            ColorSample.hoveredFgSprite = ColorPickerColor;

            ColorSample.eventSelectedColorChanged += SelectedColorChanged;
            ColorSample.eventColorPickerOpen += ColorPickerOpen;
        }

        private void ColorPickerOpen(UIColorField dropdown, UIColorPicker popup, ref bool overridden)
        {
            popup.component.size += new Vector2(31, 31);
            popup.component.relativePosition -= new Vector3(31, 0);
            var slider = AddOpacitySlider(popup.component);
            slider.value = Value.a;

            AddCopyButton(popup);
            AddPasteButton(popup);
            AddSetDefaultButton(popup);
        }
        private UISlider AddOpacitySlider(UIComponent parent)
        {
            var opacitySlider = parent.AddUIComponent<UISlider>();

            opacitySlider.atlas = TextureUtil.InGameAtlas;
            opacitySlider.size = new Vector2(18, 200);
            opacitySlider.relativePosition = new Vector3(254, 12);
            opacitySlider.orientation = UIOrientation.Vertical;
            opacitySlider.minValue = 0f;
            opacitySlider.maxValue = 255f;
            opacitySlider.stepSize = 1f;
            opacitySlider.eventValueChanged += OpacityChanged;

            var opacity = opacitySlider.AddUIComponent<UISlicedSprite>();
            opacity.atlas = OpacitySliderAtlas;
            opacity.spriteName = OpacitySlider;
            opacity.relativePosition = Vector2.zero;
            opacity.size = opacitySlider.size;
            opacity.fillDirection = UIFillDirection.Vertical;

            UISlicedSprite thumbSprite = opacitySlider.AddUIComponent<UISlicedSprite>();
            thumbSprite.relativePosition = Vector2.zero;
            thumbSprite.fillDirection = UIFillDirection.Horizontal;
            thumbSprite.size = new Vector2(29, 7);
            thumbSprite.spriteName = "ScrollbarThumb";

            opacitySlider.thumbObject = thumbSprite;

            return opacitySlider;
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
        private void AddCopyButton(UIColorPicker popup)
        {
            var button = CreateButton(popup.component, NodeMarkup.Localize.Editor_ColorCopy, 1, 3);
            button.eventClick += (UIComponent component, UIMouseEventParameter eventParam) => Copy(popup);
        }

        private void AddPasteButton(UIColorPicker popup)
        {
            var button = CreateButton(popup.component, NodeMarkup.Localize.Editor_ColorPaste, 2, 3);
            button.isEnabled = Buffer.HasValue;
            button.eventClick += (UIComponent component, UIMouseEventParameter eventParam) => Paste(popup);
        }
        private void AddSetDefaultButton(UIColorPicker popup)
        {
            var button = CreateButton(popup.component, NodeMarkup.Localize.Editor_ColorDefault, 3, 3);
            button.eventClick += (UIComponent component, UIMouseEventParameter eventParam) => Value = Manager.Style.DefaultColor;
        }

        private void Copy(UIColorPicker popup)
        {
            Buffer = Value;
            popup.component.Hide();
        }
        private void Paste(UIColorPicker popup)
        {
            if (Buffer != null)
            {
                Value = Buffer.Value;
                popup.component.Hide();
            }
        }


        private void SelectedColorChanged(UIComponent component, Color value)
        {
            value.a = ((Color)Value).a;
            Value = value;
        }
        private void OpacityChanged(UIComponent component, float value)
        {
            var color = Value;
            color.a = (byte)value;
            Value = color;
        }
        protected virtual void FieldTextSubmitted(UIComponent component, string text)
        {
            Value = Value;
        }
    }
}

