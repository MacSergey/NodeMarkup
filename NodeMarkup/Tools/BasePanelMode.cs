using ColossalFramework.UI;
using ModsCommon.UI;
using NodeMarkup.UI.Editors;
using System;
using UnityEngine;

namespace NodeMarkup.Tools
{
    public abstract class BasePanelMode<EditorType, PanelType, ObjectType> : NodeMarkupToolMode
        where EditorType : Editor
        where PanelType : SelectPropertyPanel<ObjectType, PanelType>
    {
        public override ToolModeType Type => ToolModeType.PanelAction;

        protected EditorType Editor { get; private set; }
        protected abstract bool IsHover { get; }
        protected abstract ObjectType Hover { get; }

        private PanelType _selectPanel;
        public PanelType SelectPanel
        {
            get => _selectPanel;
            set
            {
                if (_selectPanel != null)
                {
                    _selectPanel.eventLeaveFocus -= SelectPanelLeaveFocus;
                    _selectPanel.Selected = false;
                }

                _selectPanel = value;

                if (_selectPanel != null)
                {
                    OnSetPanel();
                    _selectPanel.eventLeaveFocus += SelectPanelLeaveFocus;
                    _selectPanel.Selected = true;
                }
            }
        }

        public Func<Event, bool> AfterSelectPanel { get; set; }

        public void Init(EditorType editor) => Editor = editor;

        public override void Update()
        {
            base.Update();
            if (SelectPanel is PanelType panel)
                panel.Selected = true;
        }
        public override void Deactivate()
        {
            base.Deactivate();
            SelectPanel = null;
        }
        protected virtual void OnSetPanel() { }

        public override void OnPrimaryMouseClicked(Event e)
        {
            if (IsHover)
            {
                SelectPanel.Value = Hover;
                if (AfterSelectPanel?.Invoke(e) ?? true)
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

        private void SelectPanelLeaveFocus(UIComponent component, UIFocusEventParameter eventParam) => Tool.SetDefaultMode();
    }
}
