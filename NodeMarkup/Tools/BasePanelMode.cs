using ColossalFramework.UI;
using ModsCommon.UI;
using NodeMarkup.UI.Editors;
using System;
using UnityEngine;

namespace NodeMarkup.Tools
{
    public abstract class BasePanelMode<EditorType, ButtonType, ObjectType> : IntersectionMarkingToolMode
        where EditorType : Editor
        where ButtonType : SelectPropertyButton<ObjectType>
    {
        public override ToolModeType Type => ToolModeType.PanelAction;

        protected EditorType Editor { get; private set; }
        protected abstract bool IsHover { get; }
        protected abstract ObjectType Hover { get; }

        private ButtonType _selectButton;
        public ButtonType SelectButton
        {
            get => _selectButton;
            set
            {
                if (_selectButton != null)
                {
                    _selectButton.eventLeaveFocus -= SelectButtonLeaveFocus;
                    _selectButton.Selected = false;
                }

                _selectButton = value;

                if (_selectButton != null)
                {
                    OnSetButton();
                    _selectButton.eventLeaveFocus += SelectButtonLeaveFocus;
                    _selectButton.Selected = true;
                }
            }
        }

        public Func<Event, bool> AfterSelectButton { get; set; }

        public void Init(EditorType editor) => Editor = editor;

        public void Update()
        {
            if (SelectButton is ButtonType button)
                button.Selected = true;
        }
        public override void Deactivate()
        {
            base.Deactivate();
            SelectButton = null;
        }
        protected virtual void OnSetButton() { }

        public override void OnPrimaryMouseClicked(Event e)
        {
            if (IsHover)
            {
                SelectButton.Value = Hover;
                if (AfterSelectButton?.Invoke(e) ?? true)
                    Tool.SetDefaultMode();
            }
        }
        public override void OnSecondaryMouseClicked() => Exit();
        public override bool OnEscape()
        {
            Exit();
            return true;
        }
        private void Exit() => Tool.SetDefaultMode();

        private void SelectButtonLeaveFocus(UIComponent component, UIFocusEventParameter eventParam) => Tool.SetDefaultMode();
    }
}
