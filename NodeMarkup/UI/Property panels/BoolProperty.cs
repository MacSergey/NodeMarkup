using ColossalFramework.UI;
using System;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class BoolPropertyPanel : EditorPropertyPanel
    {
        private UICheckBox CheckBox { get; set; }
        public event Action<bool> OnValueChanged;

        public bool Value { get => CheckBox.isChecked; set => CheckBox.isChecked = value; }

        public BoolPropertyPanel()
        {
            CheckBox = Control.AddUIComponent<UICheckBox>();
            CheckBox.size = new Vector2(16, 16);
            CheckBox.eventCheckChanged += CheckBox_eventCheckChanged;

            AddUncheck();
            AddCheck();
        }
        private void AddUncheck()
        {
            var uncheck = CheckBox.AddUIComponent<UISprite>();
            uncheck.spriteName = "check-unchecked";
            uncheck.size = new Vector2(16, 16);
            uncheck.relativePosition = new Vector2(0, 0);
        }
        private void AddCheck()
        {
            var check = CheckBox.AddUIComponent<UISprite>();
            check.spriteName = "check-checked";
            check.size = new Vector2(16, 16);
            check.relativePosition = new Vector2(0, 0);
            CheckBox.checkedBoxObject = check;
        }

        private void CheckBox_eventCheckChanged(UIComponent component, bool value) => OnValueChanged?.Invoke(value);
    }
}
