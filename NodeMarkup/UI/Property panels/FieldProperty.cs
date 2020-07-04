using ColossalFramework.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace NodeMarkup.UI.Editors
{
    public abstract class FieldPropertyPanel<ValueType> : EditorPropertyPanel
    {
        protected UITextField Field { get; set; }

        public event Action<ValueType> OnValueChanged;
        public event Action<ValueType> OnValueSubmitted;

        protected abstract bool CanUseWheel { get; }
        public bool UseWheel { get; set; }
        public ValueType Step { get; set; }
        public float FieldWidth
        {
            get => Field.width;
            set => Field.width = value;
        }

        public ValueType Value
        {
            get
            {
                try
                {
                    return (ValueType)TypeDescriptor.GetConverter(typeof(ValueType)).ConvertFromString(Field.text);
                }
                catch
                {
                    return default;
                }
            }
            set => Field.text = value.ToString();
        }

        public FieldPropertyPanel()
        {
            Field = Control.AddUIComponent<UITextField>();
            Field.atlas = TextureUtil.GetAtlas("Ingame");
            Field.normalBgSprite = "TextFieldPanel";
            Field.hoveredBgSprite = "TextFieldPanelHovered";
            Field.focusedBgSprite = "TextFieldPanel";
            Field.selectionSprite = "EmptySprite";
            Field.allowFloats = true;
            Field.isInteractive = true;
            Field.enabled = true;
            Field.readOnly = false;
            Field.builtinKeyNavigation = true;
            Field.cursorWidth = 1;
            Field.cursorBlinkTime = 0.45f;
            Field.selectOnFocus = true;
            Field.eventTextChanged += FieldTextChanged;
            Field.eventMouseWheel += FieldMouseWheel;
            Field.eventTextSubmitted += FieldTextSubmitted;
            Field.textScale = 0.7f;
            Field.verticalAlignment = UIVerticalAlignment.Middle;

        }
        protected abstract ValueType Increment(ValueType value, ValueType step);
        protected abstract ValueType Decrement(ValueType value, ValueType step);

        protected virtual void FieldTextChanged(UIComponent component, string text) => OnValueChanged?.Invoke(Value);
        protected virtual void FieldTextSubmitted(UIComponent component, string value) => OnValueSubmitted?.Invoke(Value);
        private void FieldMouseWheel(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (CanUseWheel && UseWheel)
            {
                if (eventParam.wheelDelta < 0)
                    Value = Increment(Value, Step);
                else
                    Value = Decrement(Value, Step);
            }
        }
    }
    public class FloatPropertyPanel : FieldPropertyPanel<float>
    {
        protected override bool CanUseWheel => true;

        protected override float Decrement(float value, float step) => value + step;
        protected override float Increment(float value, float step) => value - step;
    }
    public class StringPropertyPanel : FieldPropertyPanel<string>
    {
        protected override bool CanUseWheel => false;

        protected override string Decrement(string value, string step) => throw new NotSupportedException();
        protected override string Increment(string value, string step) => throw new NotSupportedException();
    }
}
