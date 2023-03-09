using ColossalFramework.UI;
using IMT.UI.Editors;
using ModsCommon.UI;
using System;
using UnityEngine;

namespace IMT.Tools
{
    public abstract class BasePanelMode<EditorType, ButtonType, ObjectType> : IntersectionMarkingToolMode
        where EditorType : Editor
        where ButtonType : SelectPropertyButton<ObjectType>
    {
        public override ToolModeType Type => ToolModeType.PanelAction;

        protected EditorType Editor { get; private set; }
        protected abstract bool IsHover { get; }
        protected abstract ObjectType Hover { get; }

        private ButtonType selectButton;
        public ButtonType SelectButton
        {
            get => selectButton;
            set
            {
                if (selectButton != null)
                {
                    selectButton.eventLeaveFocus -= SelectButtonLeaveFocus;
                    selectButton.isSelected = false;
                }

                selectButton = value;

                if (selectButton != null)
                {
                    OnSetButton();
                    selectButton.eventLeaveFocus += SelectButtonLeaveFocus;
                    selectButton.isSelected = true;
                }
            }
        }

        public Func<Event, bool> AfterSelectButton { get; set; }

        public void Init(EditorType editor) => Editor = editor;

        public void Update()
        {
            if (SelectButton is ButtonType button)
                button.isSelected = true;
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
