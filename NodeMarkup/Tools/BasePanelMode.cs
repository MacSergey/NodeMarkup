using ColossalFramework.UI;
using ModsCommon.UI;
using IMT.UI;
using IMT.UI.Editors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace IMT.Tools
{
    public abstract class BasePanelMode<EditorType, PanelType, ObjectType> : BaseToolMode
        where EditorType : Editor
        where PanelType : SelectPropertyPanel<ObjectType>
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
                    _selectPanel.eventLostFocus -= SelectPanelLeaveFocus;
                    _selectPanel.Selected = false;
                }

                _selectPanel = value;

                if (_selectPanel != null)
                {
                    OnSetPanel();
                    _selectPanel.eventLeaveFocus += SelectPanelLeaveFocus;
                    _selectPanel.eventLostFocus += SelectPanelLeaveFocus;
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
            if (SelectPanel is PanelType panel)
            {
                panel.Selected = true;
                SelectPanel = null;
            }
        }
        protected virtual void OnSetPanel() { }

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
