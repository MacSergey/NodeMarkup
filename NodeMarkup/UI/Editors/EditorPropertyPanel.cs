using ColossalFramework.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public abstract class EditorPropertyPanel : UIPanel
    {
        private UILabel Label { get; set; }
        protected UIPanel Control { get; set; }

        public string Text
        {
            get => Label.text;
            set => Label.text = value;
        }

        public EditorPropertyPanel()
        {
            Label = AddUIComponent<UILabel>();

            Control = AddUIComponent<UIPanel>();
            Control.autoLayout = true;
            Control.autoLayoutDirection = LayoutDirection.Horizontal;
            Control.autoLayoutStart = LayoutStart.TopRight;
            Control.autoLayoutPadding = new RectOffset(5, 5, 5, 5);
        }
        public override void Awake()
        {
            base.Awake();

            padding = new RectOffset(5, 5, 5, 5);
            height = 36;
        }
        protected override void OnSizeChanged()
        {
            if(Label != null)
            {
                Label.relativePosition = new Vector2(padding.left, (height - Label.height) / 2);
            }
            if(Control != null)
            {
                Control.size = size;
            }
        }
    }

    public class FieldPropertyPanel<ValueType> : EditorPropertyPanel
    {
        private UITextField Field { get; set; }

        public event Action<ValueType> OnValueChanged;

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
            Field.eventTextChanged += FieldTextChanged;
        }

        protected virtual void FieldTextChanged(UIComponent component, string text) => OnValueChanged?.Invoke(Value);
    }
    public class FloatPropertyPanel : FieldPropertyPanel<float> { }
}
