using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public abstract class EditorItem : UIPanel
    {
        public virtual void Init()
        {
            if (parent is UIScrollablePanel scrollablePanel)
                width = scrollablePanel.width - scrollablePanel.autoLayoutPadding.horizontal;
            else if (parent is UIPanel panel)
                width = panel.width - panel.autoLayoutPadding.horizontal;
            else
                width = parent.width;

            height = 30;
        }
    }
    public abstract class EditorPropertyPanel : EditorItem
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
            Label.textScale = 0.8f;

            Control = AddUIComponent<UIPanel>();
            Control.autoLayout = true;
            Control.autoLayoutDirection = LayoutDirection.Horizontal;
            Control.autoLayoutStart = LayoutStart.TopRight;
            Control.autoLayoutPadding = new RectOffset(5, 0, 0, 0);
        }

        public override void Init()
        {
            base.Init();

            Label.relativePosition = new Vector2(0, (height - Label.height) / 2);
            Control.size = size;

            foreach (var item in Control.components)
            {
                var pos = item.relativePosition;
                pos.y = (Control.height - item.height) / 2;
                item.relativePosition = pos;
            }
        }
    }
    public class ButtonPanel : EditorItem
    {
        protected UIButton Button { get; set; }

        public string Text
        {
            get => Button.text;
            set => Button.text = value;
        }

        public event Action OnButtonClick;

        public ButtonPanel()
        {
            Button = AddUIComponent<UIButton>();

            Button.atlas = TextureUtil.GetAtlas("Ingame");
            Button.normalBgSprite = "ButtonWhite";
            Button.disabledBgSprite = "ButtonWhiteDisabled";
            Button.focusedBgSprite = "ButtonWhiteFocused";
            Button.hoveredBgSprite = "ButtonWhiteHovered";
            Button.pressedBgSprite = "ButtonWhitePressed";
            Button.textColor = Color.black;

            Button.eventClick += ButtonClick;
        }

        private void ButtonClick(UIComponent component, UIMouseEventParameter eventParam) => OnButtonClick?.Invoke();

        public override void Init()
        {
            base.Init();

            Button.size = size;
        }
    }
    public class CloseButtonPanel : EditorPropertyPanel
    {
        public event Action OnButtonClick;
        public CloseButtonPanel()
        {
            var button = Control.AddUIComponent<UIButton>();

            button.atlas = TextureUtil.GetAtlas("Ingame");
            button.normalBgSprite = "buttonclose";
            button.hoveredBgSprite = "buttonclosehover";
            button.pressedBgSprite = "buttonclosepressed";

            button.size = new Vector2(20, 20);

            button.eventClick += ButtonClick;
        }
        private void ButtonClick(UIComponent component, UIMouseEventParameter eventParam) => OnButtonClick?.Invoke();
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

    public class ColorPropertyPanel : EditorPropertyPanel
    {
        public event Action<Color32> OnValueChanged;

        private UITextField R { get; set; }
        private UITextField G { get; set; }
        private UITextField B { get; set; }
        private UITextField A { get; set; }

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
            }
        }
        private byte CetComponent(string text) => byte.TryParse(text, out byte value) ? value : byte.MaxValue;

        public ColorPropertyPanel()
        {
            R = AddField(nameof(R));
            G = AddField(nameof(G));
            B = AddField(nameof(B));
            A = AddField(nameof(A));
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
            field.text = 0.ToString();

            return field;
        }

        protected virtual void FieldTextChanged(UIComponent component, string text) => OnValueChanged?.Invoke(Value);
    }

    public abstract class CustomUIDropDown<Type> : UIDropDown
    {
        public Func<Type, Type, bool> IsEqualDelegate { get; set; }
        List<Type> Objects { get; } = new List<Type>();
        public Type SelectedObject
        {
            get => selectedIndex >= 0 ? Objects[selectedIndex] : default;
            set => selectedIndex = Objects.FindIndex(o => IsEqualDelegate?.Invoke(o, value) ?? ReferenceEquals(o, value) || o.Equals(value));
        }

        public void AddItem(Type item, string label = null)
        {
            Objects.Add(item);
            AddItem(label ?? item.ToString());
        }
    }

    public abstract class ListPropertyPanel<Type, DropDownType> : EditorPropertyPanel
        where DropDownType : CustomUIDropDown<Type>
    {
        public event Action<Type> OnSelectObjectChanged;

        protected DropDownType DropDown { get; set; }

        public bool AllowNull { get; set; } = true;
        public string NullText { get; set; }

        public Type SelectedObject
        {
            get => DropDown.SelectedObject;
            set => DropDown.SelectedObject = value;
        }

        public ListPropertyPanel()
        {
            AddDropDown();
            DropDown.IsEqualDelegate = IsEqual;
        }
        private void AddDropDown()
        {
            DropDown = Control.AddUIComponent<DropDownType>();

            DropDown.atlas = TextureUtil.GetAtlas("Ingame");
            DropDown.height = 20;
            DropDown.width = 200;
            DropDown.listBackground = "TextFieldPanel";
            DropDown.itemHeight = 20;
            DropDown.itemHover = "TextFieldPanelHovered";
            DropDown.itemHighlight = "ListItemHighlight";
            DropDown.normalBgSprite = "TextFieldPanel";
            DropDown.hoveredBgSprite = "TextFieldPanelHovered";
            DropDown.listWidth = 200;
            DropDown.listHeight = 700;
            DropDown.listPosition = UIDropDown.PopupListPosition.Below;
            DropDown.clampListToScreen = true;
            DropDown.builtinKeyNavigation = true;
            DropDown.foregroundSpriteMode = UIForegroundSpriteMode.Stretch;
            DropDown.popupColor = new Color32(45, 52, 61, 255);
            DropDown.popupTextColor = new Color32(170, 170, 170, 255);
            DropDown.textScale = 0.7f;
            DropDown.verticalAlignment = UIVerticalAlignment.Middle;
            DropDown.horizontalAlignment = UIHorizontalAlignment.Left;
            DropDown.textFieldPadding = new RectOffset(8, 0, 8, 0);
            DropDown.itemPadding = new RectOffset(14, 0, 8, 0);

            DropDown.eventSelectedIndexChanged += DropDownIndexChanged;

            var button = DropDown.AddUIComponent<UIButton>();
            button.atlas = TextureUtil.GetAtlas("Ingame");
            button.text = "";
            button.size = DropDown.size;
            button.relativePosition = new Vector3(0f, 0f);
            button.textVerticalAlignment = UIVerticalAlignment.Middle;
            button.textHorizontalAlignment = UIHorizontalAlignment.Left;
            button.normalFgSprite = "IconDownArrow";
            button.hoveredFgSprite = "IconDownArrowHovered";
            button.pressedFgSprite = "IconDownArrowPressed";
            button.focusedFgSprite = "IconDownArrowFocused";
            button.disabledFgSprite = "IconDownArrowDisabled";
            button.foregroundSpriteMode = UIForegroundSpriteMode.Scale;
            button.horizontalAlignment = UIHorizontalAlignment.Right;
            button.verticalAlignment = UIVerticalAlignment.Middle;
            button.textScale = 0.8f;

            DropDown.triggerButton = button;
        }

        private void DropDownIndexChanged(UIComponent component, int value)
        {
            OnSelectObjectChanged?.Invoke(DropDown.SelectedObject);
        }

        public override void Init()
        {
            base.Init();

            if (AllowNull)
                DropDown.AddItem(default, NullText ?? string.Empty);
        }
        public void Add(Type item)
        {
            DropDown.AddItem(item);
        }
        public void AddRange(IEnumerable<Type> items)
        {
            foreach (var item in items)
            {
                DropDown.AddItem(item);
            }
        }
        protected abstract bool IsEqual(Type first, Type second);
    }
    public abstract class EnumPropertyPanel<EnumType, DropDownType> : ListPropertyPanel<EnumType, DropDownType>
        where EnumType : Enum
        where DropDownType : CustomUIDropDown<EnumType>
    {
        private new bool AllowNull
        {
            set => base.AllowNull = value;
        }
        public override void Init()
        {
            AllowNull = false;
            base.Init();

            foreach (var value in Enum.GetValues(typeof(EnumType)).OfType<EnumType>())
            {
                DropDown.AddItem(value);
            }
        }
    }
    public class StylePropertyPanel : EnumPropertyPanel<LineStyle.Type, StyleDropDown>
    {
        protected override bool IsEqual(LineStyle.Type first, LineStyle.Type second) => first == second;
    }
    public class StyleDropDown : CustomUIDropDown<LineStyle.Type> { }
    public class MarkupLineListPropertyPanel : ListPropertyPanel<MarkupLine, MarkupLineDropDown>
    {
        protected override bool IsEqual(MarkupLine first, MarkupLine second) => System.Object.ReferenceEquals(first, second);
    }
    public class MarkupLineDropDown : CustomUIDropDown<MarkupLine> { }
}
