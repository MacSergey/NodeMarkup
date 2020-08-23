using ColossalFramework;
using ColossalFramework.UI;
using System.Reflection;
using UnityEngine;

namespace NodeMarkup.UI
{
    public class KeymappingsPanel : UICustomControl
    {
        private static readonly string kKeyBindingTemplate = "KeyBindingTemplate";
        private SavedInputKey m_EditingBinding;
        private string m_EditingBindingCategory;
        private int count;

        internal void AddKeymapping(string label, SavedInputKey savedInputKey)
        {
            UIPanel uipanel = base.component.AttachUIComponent(UITemplateManager.GetAsGameObject(KeymappingsPanel.kKeyBindingTemplate)) as UIPanel;
            int num = count;
            count = num + 1;
            if (num % 2 == 1)
            {
                uipanel.backgroundSprite = null;
            }
            UILabel uilabel = uipanel.Find<UILabel>("Name");
            UIButton uibutton = uipanel.Find<UIButton>("Binding");
            uibutton.eventKeyDown += OnBindingKeyDown;
            uibutton.eventMouseDown += OnBindingMouseDown;
            uilabel.text = label;
            uibutton.text = savedInputKey.ToLocalizedString("KEYNAME");
            uibutton.objectUserData = savedInputKey;
        }
        private void OnEnable() { }
        private void OnDisable() { }
        private void OnLocaleChanged() => RefreshBindableInputs();
        private bool IsModifierKey(KeyCode code) => code == KeyCode.LeftControl || code == KeyCode.RightControl || code == KeyCode.LeftShift || code == KeyCode.RightShift || code == KeyCode.LeftAlt || code == KeyCode.RightAlt;
        private bool IsControlDown() => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        private bool IsShiftDown() => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        private bool IsAltDown() => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        private bool IsUnbindableMouseButton(UIMouseButton code) => code == UIMouseButton.Left || code == UIMouseButton.Right;
        private KeyCode ButtonToKeycode(UIMouseButton button)
        {
            switch (button)
            {
                case UIMouseButton.Left: return KeyCode.Mouse0;
                case UIMouseButton.Right: return KeyCode.Mouse1;
                case UIMouseButton.Middle: return KeyCode.Mouse2;
                case UIMouseButton.Special0: return KeyCode.Mouse3;
                case UIMouseButton.Special1: return KeyCode.Mouse4;
                case UIMouseButton.Special2: return KeyCode.Mouse5;
                case UIMouseButton.Special3: return KeyCode.Mouse6;
                default: return KeyCode.None;
            }
        }
        private void OnBindingKeyDown(UIComponent comp, UIKeyEventParameter p)
        {
            if (m_EditingBinding != null && !IsModifierKey(p.keycode))
            {
                p.Use();
                UIView.PopModal();
                KeyCode keycode = p.keycode;
                InputKey value = (p.keycode == KeyCode.Escape) ? m_EditingBinding.value : SavedInputKey.Encode(keycode, p.control, p.shift, p.alt);
                if (p.keycode == KeyCode.Backspace)
                {
                    value = SavedInputKey.Empty;
                }
                m_EditingBinding.value = value;
                (p.source as UITextComponent).text = m_EditingBinding.ToLocalizedString("KEYNAME");
                m_EditingBinding = null;
                m_EditingBindingCategory = string.Empty;
            }
        }
        private void OnBindingMouseDown(UIComponent comp, UIMouseEventParameter p)
        {
            if (m_EditingBinding == null)
            {
                p.Use();
                m_EditingBinding = (SavedInputKey)p.source.objectUserData;
                m_EditingBindingCategory = p.source.stringUserData;
                UIButton uibutton = p.source as UIButton;
                uibutton.buttonsMask = (UIMouseButton.Left | UIMouseButton.Right | UIMouseButton.Middle | UIMouseButton.Special0 | UIMouseButton.Special1 | UIMouseButton.Special2 | UIMouseButton.Special3);
                uibutton.text = "Press any key";
                p.source.Focus();
                UIView.PushModal(p.source);
                return;
            }
            if (!IsUnbindableMouseButton(p.buttons))
            {
                p.Use();
                UIView.PopModal();
                InputKey value = SavedInputKey.Encode(ButtonToKeycode(p.buttons), IsControlDown(), IsShiftDown(), IsAltDown());
                m_EditingBinding.value = value;
                UIButton uibutton2 = p.source as UIButton;
                uibutton2.text = m_EditingBinding.ToLocalizedString("KEYNAME");
                uibutton2.buttonsMask = UIMouseButton.Left;
                m_EditingBinding = null;
                m_EditingBindingCategory = string.Empty;
            }
        }
        private void RefreshBindableInputs()
        {
            foreach (UIComponent uicomponent in base.component.GetComponentsInChildren<UIComponent>())
            {
                UITextComponent uitextComponent = uicomponent.Find<UITextComponent>("Binding");
                if (uitextComponent != null)
                {
                    SavedInputKey savedInputKey = uitextComponent.objectUserData as SavedInputKey;
                    if (savedInputKey != null)
                    {
                        uitextComponent.text = savedInputKey.ToLocalizedString("KEYNAME");
                    }
                }
                UILabel uilabel = uicomponent.Find<UILabel>("Name");
                if (uilabel != null)
                {
                    uilabel.text = uilabel.stringUserData;
                }
            }
        }
        internal InputKey GetDefaultEntry(string entryName)
        {
            FieldInfo field = typeof(DefaultSettings).GetField(entryName, BindingFlags.Static | BindingFlags.Public);
            if (field == null)
            {
                return 0;
            }
            object value = field.GetValue(null);
            if (value is InputKey)
            {
                return (InputKey)value;
            }
            return 0;
        }
        private void RefreshKeyMapping()
        {
            UIComponent[] componentsInChildren = base.component.GetComponentsInChildren<UIComponent>();
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                UITextComponent uitextComponent = componentsInChildren[i].Find<UITextComponent>("Binding");
                SavedInputKey savedInputKey = (SavedInputKey)uitextComponent.objectUserData;
                if (m_EditingBinding != savedInputKey)
                {
                    uitextComponent.text = savedInputKey.ToLocalizedString("KEYNAME");
                }
            }
        }
    }
}
