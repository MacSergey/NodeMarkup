using ColossalFramework.UI;
using NodeMarkup.Tools;
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

        private ByteUITextField R { get; set; }
        private ByteUITextField G { get; set; }
        private ByteUITextField B { get; set; }
        private ByteUITextField A { get; set; }
        private UIColorField ColorSample { get; set; }
        private UIColorPicker Popup { get; set; }
        private UISlider Opacity { get; set; }

        public Color32 Value
        {
            get => new Color32(R, G, B, A);
            set => ValueChanged(value, (c) =>
            {
                SetFields(c);
                SetSample(c);
                SetOpacity(c);
            });
        }

        public ColorPropertyPanel()
        {
            R = AddField(nameof(R));
            G = AddField(nameof(G));
            B = AddField(nameof(B));
            A = AddField(nameof(A));

            AddColorSample();
        }
        private void ValueChanged(Color32 color, Action<Color32> action)
        {
            if (!InProcess)
            {
                InProcess = true;

                action(color);
                OnValueChanged?.Invoke(Value);

                InProcess = false;
            }
        }

        private void FieldChanged(byte value) => ValueChanged(Value, (c) =>
        {
            SetSample(c);
            SetOpacity(c);
        });
        private void SelectedColorChanged(UIComponent component, Color value)
        {
            var color = (Color32)value;
            color.a = A;

            ValueChanged(color, (c) =>
            {
                SetFields(c);
                SetOpacity(color);
            });
        }
        private void OpacityChanged(UIComponent component, float value) => A.Value = (byte)value;

        private void SetFields(Color32 color)
        {
            R.Value = color.r;
            G.Value = color.g;
            B.Value = color.b;
            A.Value = color.a;
        }
        private void SetSample(Color32 color)
        {
            color.a = byte.MaxValue;

            if (ColorSample != null)
                ColorSample.selectedColor = color;
            if (Popup != null)
                Popup.color = color;
        }
        private void SetOpacity(Color32 color)
        {
            if (Opacity != null)
            {
                Opacity.value = color.a;
                color.a = byte.MaxValue;
                Opacity.Find<UISlicedSprite>("color").color = color;
            }
        }

        public override void DeInit()
        {
            base.DeInit();

            OnValueChanged = null;
        }
        private ByteUITextField AddField(string name)
        {
            var lable = Control.AddUIComponent<UILabel>();
            lable.text = name;
            lable.textScale = 0.7f;

            var field = AddTextField<byte, ByteUITextField>(Control);
            field.MinValue = byte.MinValue;
            field.MaxValue = byte.MaxValue;
            field.CheckMax = true;
            field.CheckMin = true;
            field.UseWheel = true;
            field.WheelStep = 10;
            field.width = 30;
            field.OnValueChanged += FieldChanged;

            return field;
        }

        private void AddColorSample()
        {
            if (!(UITemplateManager.Get("LineTemplate") is UIComponent template))
                return;

            var panel = Control.AddUIComponent<UIPanel>();
            panel.atlas = TextureUtil.Atlas;
            panel.backgroundSprite = TextureUtil.ColorPickerBoard;

            ColorSample = Instantiate(template.Find<UIColorField>("LineColor").gameObject).GetComponent<UIColorField>();
            panel.AttachUIComponent(ColorSample.gameObject);
            ColorSample.size = panel.size = new Vector2(26f, 28f);
            ColorSample.relativePosition = new Vector2(0, 0);
            ColorSample.anchor = UIAnchorStyle.None;
            ColorSample.atlas = TextureUtil.Atlas;
            ColorSample.normalBgSprite = TextureUtil.ColorPickerNormal;
            ColorSample.hoveredBgSprite = TextureUtil.ColorPickerHover;
            ColorSample.hoveredFgSprite = TextureUtil.ColorPickerColor;

            ColorSample.eventSelectedColorChanged += SelectedColorChanged;
            ColorSample.eventColorPickerOpen += ColorPickerOpen;
            ColorSample.eventColorPickerClose += ColorPickerClose;
            ColorSample.eventDoubleClick += ColorSampleDoubleClick;

            ColorSample.tooltip = NodeMarkup.Localize.Editor_ColorSampleTooltip;
        }

        private void ColorSampleDoubleClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (NodeMarkupTool.OnlyShiftIsPressed)
                Copy();
            else
                Paste();
        }

        private void ColorPickerOpen(UIColorField dropdown, UIColorPicker popup, ref bool overridden)
        {
            Popup = popup;

            Popup.component.size += new Vector2(31, 31);
            Popup.component.relativePosition -= new Vector3(31, 0);

            if (Popup.component is UIPanel panel)
            {
                panel.atlas = TextureUtil.Atlas;
                panel.backgroundSprite = TextureUtil.FieldNormal;
            }

            Opacity = AddOpacitySlider(popup.component);
            Opacity.value = A;

            AddCopyButton();
            AddPasteButton();
            AddSetDefaultButton();
        }
        private void ColorPickerClose(UIColorField dropdown, UIColorPicker popup, ref bool overridden)
        {
            Popup = null;
            Opacity = null;
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

            var opacityBoard = opacitySlider.AddUIComponent<UISlicedSprite>();
            opacityBoard.atlas = TextureUtil.Atlas;
            opacityBoard.spriteName = TextureUtil.OpacitySliderBoard;
            opacityBoard.relativePosition = Vector2.zero;
            opacityBoard.size = opacitySlider.size;
            opacityBoard.fillDirection = UIFillDirection.Vertical;

            var opacityColor = opacitySlider.AddUIComponent<UISlicedSprite>();
            opacityColor.name = "color";
            opacityColor.atlas = TextureUtil.Atlas;
            opacityColor.spriteName = TextureUtil.OpacitySliderColor;
            opacityColor.relativePosition = Vector2.zero;
            opacityColor.size = opacitySlider.size;
            opacityColor.fillDirection = UIFillDirection.Vertical;

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

        private void AddCopyButton()
        {
            var button = CreateButton(Popup.component, NodeMarkup.Localize.Editor_ColorCopy, 1, 3);
            button.eventClick += (UIComponent component, UIMouseEventParameter eventParam) => Copy();
        }
        private void AddPasteButton()
        {
            var button = CreateButton(Popup.component, NodeMarkup.Localize.Editor_ColorPaste, 2, 3);
            button.isEnabled = Buffer.HasValue;
            button.eventClick += (UIComponent component, UIMouseEventParameter eventParam) => Paste();
        }
        private void AddSetDefaultButton()
        {
            var button = CreateButton(Popup.component, NodeMarkup.Localize.Editor_ColorDefault, 3, 3);
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

        public override string ToString() => Value.ToString();
        public static implicit operator Color32(ColorPropertyPanel property) => property.Value;
    }
}

