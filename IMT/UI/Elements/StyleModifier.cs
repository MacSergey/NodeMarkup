using ColossalFramework;
using ColossalFramework.UI;
using IMT.Manager;
using IMT.Tools;
using IMT.Utilities;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace IMT.UI
{
    public class StyleModifierSettingsItem : ContentSettingsItem
    {
        public event Action<Style.StyleType, StyleModifier> OnModifierChanged;
        private ModifierDropDown DropDown { get; }


        Style.StyleType style;
        public Style.StyleType Style
        {
            get => style;
            set
            {
                if(value != style)
                {
                    style = value;
                    DropDown.SelectedObject = Value;
                }
            }
        }
        public StyleModifier Value
        {
            get => IntersectionMarkingTool.StylesModifier.TryGetValue(Style, out var modifier) ? (StyleModifier)modifier.value : StyleModifier.NotSet;
            set
            {
                if(IntersectionMarkingTool.StylesModifier.ContainsKey(Style))
                {
                    IntersectionMarkingTool.StylesModifier[Style].value = (int)value;
                    DropDown.SelectedObject = value;
                }
            }
        }

        public StyleModifierSettingsItem()
        {
            DropDown = Content.AddUIComponent<ModifierDropDown>();
            DropDown.OnValueChanged += ModifierChanged;

            SetHeightBasedOn(DropDown);
        }

        private void ModifierChanged(ModifierDropDown changedModifier, StyleModifier value)
        {
            Value = value;
            OnModifierChanged?.Invoke(Style, value);
        }
    }

    public class ModifierDropDown : SimpleDropDown<StyleModifier, ModifierDropDown.ModifierEntity, ModifierDropDown.ModifierPopup>
    {
        public new event Action<ModifierDropDown, StyleModifier> OnValueChanged;

        public ModifierDropDown()
        {
            ComponentStyle.CustomSettingsStyle(this, new Vector2(278, 31));

            foreach (var modifier in EnumExtension.GetEnumValues<StyleModifier>())
                AddItem(modifier, modifier.Description());

            SelectedObject = StyleModifier.NotSet;
        }

        protected override void ValueChanged(DropDownItem<StyleModifier> item) => OnValueChanged?.Invoke(this, item.value);
        protected override void InitPopup()
        {
            Popup.CustomSettingsStyle(height);
            base.InitPopup();
        }

        public class ModifierEntity : SimpleEntity<StyleModifier> { }
        public class ModifierPopup : SimplePopup<StyleModifier, ModifierEntity> { }
    }

    public enum StyleModifier
    {
        [Description(nameof(Localize.Settings_StyleModifierNotSet))]
        NotSet = 0,

        [Description(nameof(Localize.Settings_StyleModifierWithout))]
        [InputKey(false, false, false)]
        Without = 1,


        [InputKey(true, false, false)]
        Ctrl = 2,

        [InputKey(false, true, false)]
        Shift = 3,

        [InputKey(false, false, true)]
        Alt = 4,


        [InputKey(true, true, false)]
        CtrlShift = 5,

        [InputKey(true, false, true)]
        CtrlAlt = 6,

        [InputKey(false, true, true)]
        ShiftAlt = 7,

        [InputKey(true, true, true)]
        CtrlShiftAlt = 8,
    }
    public class InputKeyAttribute : Attribute
    {
        public bool Control { get; set; }
        public bool Shift { get; set; }
        public bool Alt { get; set; }
        public bool IsPressed => Control == Utility.CtrlIsPressed && Shift == Utility.ShiftIsPressed && Alt == Utility.AltIsPressed;

        public InputKeyAttribute(bool control, bool shift, bool alt)
        {
            Control = control;
            Shift = shift;
            Alt = alt;
        }
    }
}
