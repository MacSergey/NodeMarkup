using ColossalFramework.UI;
using NodeMarkup.UI;
using NodeMarkup.UI.Editors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.Tools
{
    public abstract class BasePanelMode<EditorType, PanelType, ObjectType> : BaseToolMode
        where EditorType : Editor
        where PanelType : SelectPropertyPanel<ObjectType>
    {
        public override ToolModeType Type => ToolModeType.PanelAction;

        protected EditorType Editor { get; }
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
                    _selectPanel.eventLostFocus -= SelectPanelLeaveFocus;
                }

                _selectPanel = value;

                if (_selectPanel != null)
                {
                    OnSetPanel();
                    _selectPanel.eventLeaveFocus += SelectPanelLeaveFocus;
                    _selectPanel.eventLostFocus += SelectPanelLeaveFocus;
                }
            }
        }

        public Func<Event, bool> AfterSelectPanel { get; set; }

        public BasePanelMode(EditorType editor)
        {
            Editor = editor;
        }

        protected virtual void OnSetPanel() { }

        public override void OnMouseUp(Event e) => OnPrimaryMouseClicked(e);
        public override void OnSecondaryMouseClicked() => Tool.SetDefaultMode();
        public override void OnPrimaryMouseClicked(Event e)
        {
            if (IsHover)
            {
                SelectPanel.SelectedObject = Hover;
                if (AfterSelectPanel?.Invoke(e) ?? true)
                    Tool.SetDefaultMode();
            }
        }

        private void SelectPanelLeaveFocus(UIComponent component, UIFocusEventParameter eventParam) => Tool.SetDefaultMode();
    }
}
