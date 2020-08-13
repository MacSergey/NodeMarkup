using ColossalFramework.Math;
using ColossalFramework.Threading;
using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using static ToolBase;

namespace NodeMarkup.UI.Editors
{
    public class LinesEditor : GroupedEditor<LineItem, MarkupLine, LineIcon, LineGroup, MarkupLine.LineType>
    {
        public static Color WhiteAlpha { get; } = new Color(1, 1, 1, 0.5f);
        public override string Name => NodeMarkup.Localize.LineEditor_Lines;
        public override string EmptyMessage => NodeMarkup.Localize.LineEditor_EmptyMessage;
        protected override bool GroupingEnabled => Settings.GroupLines.value;

        private ButtonPanel AddButton { get; set; }

        public List<ILinePartEdge> SupportPoints { get; } = new List<ILinePartEdge>();
        public bool CanDivide => EditObject?.SupportRules == true && SupportPoints.Count > 2;
        private bool AddRuleAvailable => CanDivide || EditObject?.Rules.Any() == false;

        private ILinePartEdge HoverSupportPoint { get; set; }
        private bool IsHoverSupportPoint => IsSelectPartEdgeMode && HoverSupportPoint != null;

        private MarkupLineSelectPropertyPanel _selectPartEdgePanel;
        private MarkupLineSelectPropertyPanel SelectPartEdgePanel
        {
            get => _selectPartEdgePanel;
            set
            {
                if (_selectPartEdgePanel != null)
                {
                    _selectPartEdgePanel.eventLeaveFocus -= SelectPanelLeaveFocus;
                    _selectPartEdgePanel.eventLostFocus -= SelectPanelLeaveFocus;
                }

                _selectPartEdgePanel = value;

                if (_selectPartEdgePanel != null)
                {
                    _selectPartEdgePanel.eventLeaveFocus += SelectPanelLeaveFocus;
                    _selectPartEdgePanel.eventLostFocus += SelectPanelLeaveFocus;
                }
            }
        }
        private bool IsSelectPartEdgeMode => SelectPartEdgePanel != null;
        private Func<Event, bool> AfterSelectPartEdgePanel { get; set; }

        private MarkupLineSelectPropertyPanel HoverPartEdgePanel { get; set; }
        private bool IsHoverPartEdgePanel => HoverPartEdgePanel != null;
        private RulePanel HoverRulePanel { get; set; }
        private bool IsHoverRulePanel => HoverRulePanel != null;

        public LinesEditor() { }

        protected override MarkupLine.LineType SelectGroup(MarkupLine editableItem) => editableItem.Type;
        protected override string GroupName(MarkupLine.LineType group) => Utilities.EnumDescription(group);

        protected override void FillItems()
        {
            foreach (var line in Markup.Lines)
                AddItem(line);
        }
        protected override void OnObjectSelect()
        {
            GetRuleEdges();
            AddRulePanels();
            AddAddButton();
        }
        private void GetRuleEdges()
        {
            SupportPoints.Clear();
            SupportPoints.Add(new EnterPointEdge(EditObject.Start));
            foreach (var line in EditObject.IntersectLines)
                SupportPoints.Add(new LinesIntersectEdge(EditObject, line));
            SupportPoints.Add(new EnterPointEdge(EditObject.End));
        }
        private void AddRulePanels()
        {
            foreach (var rule in EditObject.Rules)
                AddRulePanel(rule);
        }
        private RulePanel AddRulePanel(MarkupLineRawRule rule)
        {
            var rulePanel = SettingsPanel.AddUIComponent<RulePanel>();
            rulePanel.Init(this, rule);
            rulePanel.eventMouseEnter += RuleMouseHover;
            rulePanel.eventMouseLeave += RuleMouseLeave;
            return rulePanel;
        }

        private void AddAddButton()
        {
            if (AddRuleAvailable)
            {
                AddButton = SettingsPanel.AddUIComponent<ButtonPanel>();
                AddButton.Text = NodeMarkup.Localize.LineEditor_AddRuleButton;
                AddButton.Init();
                AddButton.OnButtonClick += AddRule;
            }
        }
        private void DeleteAddButton()
        {
            AddButton.OnButtonClick -= AddRule;
            SettingsPanel.RemoveUIComponent(AddButton);
            Destroy(AddButton);
        }

        private void AddRule()
        {
            if (!(EditObject is MarkupRegularLine regularLine))
                return;

            var newRule = regularLine.AddRule(CanDivide);
            DeleteAddButton();
            var rulePanel = AddRulePanel(newRule);
            AddAddButton();

            SettingsPanel.ScrollToBottom();

            if (Settings.QuickRuleSetup)
                SetupRule(rulePanel);

            RefreshItem();
        }
        private void SetupRule(RulePanel rulePanel)
        {
            SelectRuleEdge(rulePanel.From, (_) => SelectRuleEdge(rulePanel.To, (e) => SetStyle(rulePanel, e)));
        }
        private bool SetStyle(RulePanel rulePanel, Event e)
        {
            rulePanel.Style.SelectedObject = e.GetRegularStyle();
            return true;
        }
        public void DeleteRule(RulePanel rulePanel)
        {
            if (!(EditObject is MarkupRegularLine regularLine))
                return;

            if (Settings.DeleteWarnings)
            {
                var messageBox = MessageBoxBase.ShowModal<YesNoMessageBox>();
                messageBox.CaprionText = NodeMarkup.Localize.LineEditor_DeleteRuleCaption;
                messageBox.MessageText = NodeMarkup.Localize.LineEditor_DeleteRuleMessage;
                messageBox.OnButton1Click = Delete;
            }
            else
                Delete();

            bool Delete()
            {
                regularLine.RemoveRule(rulePanel.Rule as MarkupLineRawRule<RegularLineStyle>);
                SettingsPanel.RemoveUIComponent(rulePanel);
                Destroy(rulePanel);
                RefreshItem();
                if (!CanDivide)
                    AddAddButton();
                return true;
            }
        }
        public bool SelectRuleEdge(MarkupLineSelectPropertyPanel selectPanel) => SelectRuleEdge(selectPanel, null);
        public bool SelectRuleEdge(MarkupLineSelectPropertyPanel selectPanel, Func<Event, bool> afterAction)
        {
            if (IsSelectPartEdgeMode)
            {
                var isToggle = SelectPartEdgePanel == selectPanel;
                NodeMarkupPanel.EndEditorAction();
                if (isToggle)
                    return true;
            }
            NodeMarkupPanel.StartEditorAction(this, out bool isAccept);
            if (isAccept)
            {
                selectPanel.Focus();
                SelectPartEdgePanel = selectPanel;
                AfterSelectPartEdgePanel = afterAction;
                return false;
            }
            return true;
        }
        public void HoverRuleEdge(MarkupLineSelectPropertyPanel selectPanel) => HoverPartEdgePanel = selectPanel;
        public void LeaveRuleEdge(MarkupLineSelectPropertyPanel selectPanel) => HoverPartEdgePanel = null;
        private void RuleMouseHover(UIComponent component, UIMouseEventParameter eventParam) => HoverRulePanel = component as RulePanel;
        private void RuleMouseLeave(UIComponent component, UIMouseEventParameter eventParam)
        {
            var uiView = component.GetUIView();
            var mouse = uiView.ScreenPointToGUI((eventParam.position + eventParam.moveDelta) / uiView.inputScale);
            var ruleRect = new Rect(SettingsPanel.absolutePosition + component.relativePosition, component.size);
            var settingsRect = new Rect(SettingsPanel.absolutePosition, SettingsPanel.size);

            if (eventParam.source == component || !ruleRect.Contains(mouse) || !settingsRect.Contains(mouse))
            {
                HoverRulePanel = null;
                return;
            }
        }
        private void SelectPanelLeaveFocus(UIComponent component, UIFocusEventParameter eventParam) => NodeMarkupPanel.EndEditorAction();

        public override void OnUpdate() => HoverSupportPoint = NodeMarkupTool.MouseRayValid ? SupportPoints.FirstOrDefault(i => i.IsIntersect(NodeMarkupTool.MouseRay)) : null;
        public override void OnEvent(Event e)
        {
            if (NodeMarkupTool.AddRuleShortcut.IsPressed(e) && AddRuleAvailable && !IsSelectPartEdgeMode)
                AddRule();
        }
        public override void OnPrimaryMouseClicked(Event e, out bool isDone)
        {
            if (IsHoverSupportPoint)
            {
                SelectPartEdgePanel.SelectedObject = HoverSupportPoint;
                isDone = AfterSelectPartEdgePanel?.Invoke(e) ?? true;
            }
            else
                isDone = false;
        }
        public override void Render(RenderManager.CameraInfo cameraInfo)
        {
            if (IsSelectPartEdgeMode)
            {
                foreach (var supportPoint in SupportPoints)
                {
                    var color = SelectPartEdgePanel.Position == MarkupLineSelectPropertyPanel.RulePosition.Start ? MarkupColors.Green : MarkupColors.Red;
                    NodeMarkupTool.RenderCircle(cameraInfo, color, supportPoint.Position, 0.5f);
                }

                if (IsHoverSupportPoint)
                    NodeMarkupTool.RenderCircle(cameraInfo, MarkupColors.White, HoverSupportPoint.Position, 1f);
            }
            else
            {
                if (IsHoverItem)
                    NodeMarkupTool.RenderBezier(cameraInfo, MarkupColors.White, HoverItem.Object.Trajectory, 2f);
                if (IsHoverRulePanel)
                {
                    if (HoverRulePanel.Rule.GetTrajectory(out Bezier3 bezier))
                        NodeMarkupTool.RenderBezier(cameraInfo, WhiteAlpha, bezier, 2f);
                }
                if (IsHoverPartEdgePanel && HoverPartEdgePanel.SelectedObject is ISupportPoint supportPoint)
                    NodeMarkupTool.RenderCircle(cameraInfo, MarkupColors.White, supportPoint.Position, 0.5f);
            }
        }
        public override string GetInfo()
        {
            if (IsSelectPartEdgeMode)
            {
                switch (SelectPartEdgePanel.Position)
                {
                    case MarkupLineSelectPropertyPanel.RulePosition.Start:
                        return NodeMarkup.Localize.LineEditor_InfoSelectFrom;
                    case MarkupLineSelectPropertyPanel.RulePosition.End:
                        return NodeMarkup.Localize.LineEditor_InfoSelectTo;
                }
            }

            return base.GetInfo();
        }
        public override void EndEditorAction()
        {
            if (IsSelectPartEdgeMode)
            {
                SelectPartEdgePanel = null;
                AfterSelectPartEdgePanel = null;
            }
        }
        protected override void OnObjectDelete(MarkupLine line) => Markup.RemoveConnect(line);
        public void RefreshItem() => SelectItem.Refresh();
    }

    public class LineItem : EditableItem<MarkupLine, LineIcon>
    {
        public override void Init() => Init(true, true);

        public override string Description => NodeMarkup.Localize.LineEditor_ItemDescription;
        protected override void OnObjectSet() => SetIcon();
        public override void Refresh()
        {
            base.Refresh();
            SetIcon();
        }
        private void SetIcon()
        {
            if (!ShowIcon)
                return;

            var rules = Object.Rules.ToArray();
            Icon.Count = rules.Length;
            if (rules.Length == 1)
            {
                Icon.Type = rules[0].Style.Type;
                Icon.StyleColor = rules[0].Style.Color;
            }
        }
    }
    public class LineGroup : EditableGroup<MarkupLine.LineType, LineItem, MarkupLine, LineIcon> { }
}
