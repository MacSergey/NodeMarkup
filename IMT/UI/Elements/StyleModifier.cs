using ColossalFramework;
using ColossalFramework.UI;
using IMT.Manager;
using IMT.MarkingItems.Crosswalk.Styles.Base;
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
    public abstract class StyleModifierPanel : UICustomControl
    {
        public event Action<Style.StyleType, StyleModifier> OnModifierChanged;

        private int count;

        private Dictionary<ModifierDropDown, Style.StyleType> Modifiers { get; } = new Dictionary<ModifierDropDown, Style.StyleType>();

        protected void Add(Style.StyleType style, string label = null)
        {
            var modifier = AddKeymapping((StyleModifier)IntersectionMarkingTool.StylesModifier[style].value, label ?? style.Description());
            Modifiers[modifier] = style;
        }
        public ModifierDropDown AddKeymapping(StyleModifier value, string description)
        {
            var panel = component.AttachUIComponent(UITemplateManager.GetAsGameObject("KeyBindingTemplate")) as UIPanel;

            if (count % 2 == 1)
                panel.backgroundSprite = null;

            count += 1;

            var button = panel.Find<UIButton>("Binding");
            panel.RemoveUIComponent(button);
            Destroy(button);

            var modifier = panel.AddUIComponent<ModifierDropDown>();
            modifier.relativePosition = new Vector2(380, 6);
            modifier.SelectedObject = value;
            modifier.OnSelectObjectChanged += ModifierChanged;

            var label = panel.Find<UILabel>("Name");
            label.text = description;

            return modifier;
        }

        private void ModifierChanged(ModifierDropDown changedModifier, StyleModifier value)
        {
            if (value != StyleModifier.NotSet)
            {
                foreach (var modifier in Modifiers.Keys.Where(m => m != changedModifier && m.SelectedObject == value))
                    modifier.SelectedObject = StyleModifier.NotSet;
            }

            OnModifierChanged?.Invoke(Modifiers[changedModifier], value);
        }
    }
    public abstract class StyleModifierPanel<StyleType> : StyleModifierPanel
        where StyleType : Enum
    {
        public StyleModifierPanel()
        {
            foreach (var style in EnumExtension.GetEnumValues<StyleType>(v => true))
                Add((Style.StyleType)(object)style);
        }

    }

    public class RegularLineModifierPanel : StyleModifierPanel<RegularLineStyle.RegularLineType> { }
    public class StopLineModifierPanel : StyleModifierPanel<StopLineStyle.StopLineType> { }
    public class CrosswalkModifierPanel : StyleModifierPanel<CrosswalkStyle.CrosswalkType> { }
    public class FillerModifierPanel : StyleModifierPanel<FillerStyle.FillerType> { }

    public class ModifierDropDown : UIDropDown<StyleModifier>
    {
        public new event Action<ModifierDropDown, StyleModifier> OnSelectObjectChanged;

        public ModifierDropDown()
        {
            ComponentStyle.CustomSettingsStyle(this, new Vector2(278, 31));

            foreach (var modifier in EnumExtension.GetEnumValues<StyleModifier>())
                AddItem(modifier, modifier.Description());

            SelectedObject = StyleModifier.NotSet;
        }

        protected override void IndexChanged(UIComponent component, int value) => OnSelectObjectChanged?.Invoke(this, SelectedObject);
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
