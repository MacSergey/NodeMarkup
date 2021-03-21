using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using NodeMarkup.UI.Editors;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI
{
    public class StyleModifierPanel<StyleType> : UICustomControl
        where StyleType : Enum
    {
        public event Action<Style.StyleType, StyleModifier> OnModifierChanged;

        private static readonly string keyBindingTemplate = "KeyBindingTemplate";
        private int count;

        private Dictionary<ModifierDropDown, Style.StyleType> Modifiers { get; } = new Dictionary<ModifierDropDown, Style.StyleType>();

        public StyleModifierPanel()
        {
            Init();
        }
        protected virtual void Init()
        {
            foreach (var style in EnumExtension.GetEnumValues<StyleType>(v => true))
                Add((Style.StyleType)(object)style);
        }
        protected void Add(Style.StyleType style, string label = null)
        {
            var modifier = AddKeymapping((StyleModifier)NodeMarkupTool.StylesModifier[style].value, label ?? style.Description());
            Modifiers[modifier] = style;
        }

        public ModifierDropDown AddKeymapping(StyleModifier value, string description)
        {
            UIPanel uiPanel = component.AttachUIComponent(UITemplateManager.GetAsGameObject(keyBindingTemplate)) as UIPanel;

            int num = count;
            count = num + 1;
            if (num % 2 == 1)
                uiPanel.backgroundSprite = null;

            UILabel uilabel = uiPanel.Find<UILabel>("Name");
            UIButton uibutton = uiPanel.Find<UIButton>("Binding");
            uiPanel.RemoveUIComponent(uibutton);
            Destroy(uibutton);
            var modifier = uiPanel.AddUIComponent<ModifierDropDown>();
            modifier.relativePosition = new Vector2(380, 6);
            modifier.SelectedObject = value;
            modifier.OnSelectObjectChanged += ModifierChanged;

            uilabel.text = description;

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

    public class RegularLineModifierPanel : StyleModifierPanel<RegularLineStyle.RegularLineType> { }
    public class StopLineModifierPanel : StyleModifierPanel<StopLineStyle.StopLineType> { }
    public class CrosswalkModifierPanel : StyleModifierPanel<CrosswalkStyle.CrosswalkType> { }
    public class FillerModifierPanel : StyleModifierPanel<FillerStyle.FillerType> { }

    public class ModifierDropDown : UIDropDown<StyleModifier>
    {
        public new event Action<ModifierDropDown, StyleModifier> OnSelectObjectChanged;

        public ModifierDropDown()
        {
            SetSettingsStyle(new Vector2(278, 31));

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

        public InputKeyAttribute(bool control, bool shift, bool alt)
        {
            Control = control;
            Shift = shift;
            Alt = alt;
        }
    }
}
