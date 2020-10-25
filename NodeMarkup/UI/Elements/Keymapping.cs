using ColossalFramework;
using ColossalFramework.UI;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI
{
    public class KeymappingsPanel : UICustomControl
    {
        private static readonly string keyBindingTemplate = "KeyBindingTemplate";
        private SavedInputKey m_EditingBinding;
        private int count;

        public void AddKeymapping(Shortcut shortcut)
        {
            UIPanel uipanel = component.AttachUIComponent(UITemplateManager.GetAsGameObject(keyBindingTemplate)) as UIPanel;

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
            uilabel.text = shortcut.Label;
            uibutton.text = shortcut.ToString();
            uibutton.objectUserData = shortcut.InputKey;
        }
        private void OnLocaleChanged() => RefreshBindableInputs();
        private bool IsModifierKey(KeyCode code) => code == KeyCode.LeftControl || code == KeyCode.RightControl || code == KeyCode.LeftShift || code == KeyCode.RightShift || code == KeyCode.LeftAlt || code == KeyCode.RightAlt;
        private bool IsControlDown() => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        private bool IsShiftDown() => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        private bool IsAltDown() => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        private bool IsUnbindableMouseButton(UIMouseButton code) => code == UIMouseButton.Left || code == UIMouseButton.Right;
        private KeyCode ButtonToKeycode(UIMouseButton button)
        {
            return button switch
            {
                UIMouseButton.Left => KeyCode.Mouse0,
                UIMouseButton.Right => KeyCode.Mouse1,
                UIMouseButton.Middle => KeyCode.Mouse2,
                UIMouseButton.Special0 => KeyCode.Mouse3,
                UIMouseButton.Special1 => KeyCode.Mouse4,
                UIMouseButton.Special2 => KeyCode.Mouse5,
                UIMouseButton.Special3 => KeyCode.Mouse6,
                _ => KeyCode.None,
            };
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
            }
        }
        private void OnBindingMouseDown(UIComponent comp, UIMouseEventParameter p)
        {
            if (m_EditingBinding == null)
            {
                p.Use();
                m_EditingBinding = (SavedInputKey)p.source.objectUserData;
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
    }
}
