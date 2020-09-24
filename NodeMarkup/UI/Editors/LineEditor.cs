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
        public bool SupportRules => EditObject is MarkupRegularLine;
        public bool CanDivide => SupportRules && SupportPoints.Count > 2;
        private bool AddRuleAvailable => CanDivide || EditObject?.Rules.Any() == false;

        private MarkupLineSelectPropertyPanel HoverPartEdgePanel { get; set; }
        private bool IsHoverPartEdgePanel => HoverPartEdgePanel != null;
        private RulePanel HoverRulePanel { get; set; }
        private bool IsHoverRulePanel => HoverRulePanel != null;

        private PartEdgeToolMode PartEdgeToolMode { get; }

        public LinesEditor() 
        {
            PartEdgeToolMode = new PartEdgeToolMode(this);
        }

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
            SupportPoints.AddRange(EditObject.RulesEdges);
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
        private void RemoveRulePanel(RulePanel rulePanel)
        {
            SettingsPanel.RemoveUIComponent(rulePanel);
            Destroy(rulePanel);
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
            if (AddButton != null)
            {
                AddButton.OnButtonClick -= AddRule;
                SettingsPanel.RemoveUIComponent(AddButton);
                Destroy(AddButton);
            }
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

            if (CanDivide && Settings.QuickRuleSetup)
                SetupRule(rulePanel);

            RefreshItem();
        }
        private void SetupRule(RulePanel rulePanel) => SelectRuleEdge(rulePanel.From, (_) => SelectRuleEdge(rulePanel.To, (e) => SetStyle(rulePanel, e)));
        private bool SetStyle(RulePanel rulePanel, Event e)
        {
            rulePanel.Style.SelectedObject = e.GetRegularStyle();
            SettingsPanel.ScrollToBottom();
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
                RemoveRulePanel(rulePanel);
                RefreshItem();
                RefreshRulePanels();
                DeleteAddButton();
                AddAddButton();
                return true;
            }
        }
        public bool SelectRuleEdge(MarkupLineSelectPropertyPanel selectPanel) => SelectRuleEdge(selectPanel, null);
        public bool SelectRuleEdge(MarkupLineSelectPropertyPanel selectPanel, Func<Event, bool> afterAction)
        {
            if (Tool.Mode == PartEdgeToolMode && selectPanel == PartEdgeToolMode.SelectPartEdgePanel)
            {
                Tool.SetDefaultMode();
                return true;
            }
            else
            {
                Tool.SetMode(PartEdgeToolMode);
                PartEdgeToolMode.SelectPartEdgePanel = selectPanel;
                PartEdgeToolMode.AfterSelectPartEdgePanel = afterAction;
                selectPanel.Focus();
                return false;
            }
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
        public override bool OnShortcut(Event e)
        {
            if (NodeMarkupTool.AddRuleShortcut.IsPressed(e) && AddRuleAvailable)
            {
                AddRule();
                return true;
            }
            else
                return false;
        }
        public override void Render(RenderManager.CameraInfo cameraInfo)
        {
            {
                if (IsHoverItem)
                    HoverItem.Object.Render(cameraInfo, MarkupColors.White, 2f);
                if (IsHoverRulePanel)
                    HoverRulePanel.Rule.Render(cameraInfo, WhiteAlpha, 2f);
                if (IsHoverPartEdgePanel && HoverPartEdgePanel.SelectedObject is SupportPoint supportPoint)
                    supportPoint.Render(cameraInfo, MarkupColors.White);
            }
        }
        protected override void OnObjectUpdate()
        {
            GetRuleEdges();
            RefreshRulePanels();
        }
        protected override void OnObjectDelete(MarkupLine line) => Markup.RemoveConnect(line);
        public void RefreshItem() => SelectItem.Refresh();
        public void RefreshRulePanels()
        {
            var rulePanels = SettingsPanel.components.OfType<RulePanel>().ToArray();

            foreach(var rulePanel in rulePanels)
            {
                if (EditObject.ContainsRule(rulePanel.Rule))
                    rulePanel.Refresh();
                else
                    RemoveRulePanel(rulePanel);
            }
            foreach(var rule in EditObject.Rules)
            {
                if (!rulePanels.Any(r => r.Rule == rule))
                    AddRulePanel(rule);
            }
        }
    }
    public class PartEdgeToolMode : BaseToolMode
    {
        public override ModeType Type => ModeType.PanelAction;

        private LinesEditor Editor { get; }

        private MarkupLineSelectPropertyPanel _selectPartEdgePanel;
        public MarkupLineSelectPropertyPanel SelectPartEdgePanel
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
                    PointsSelector = new PointsSelector<ILinePartEdge>(Editor.SupportPoints, _selectPartEdgePanel.Position == EdgePosition.Start ? MarkupColors.Green : MarkupColors.Red);
                    _selectPartEdgePanel.eventLeaveFocus += SelectPanelLeaveFocus;
                    _selectPartEdgePanel.eventLostFocus += SelectPanelLeaveFocus;
                }
            }
        }
        public Func<Event, bool> AfterSelectPartEdgePanel { get; set; }
        public PointsSelector<ILinePartEdge> PointsSelector { get; set; }

        public PartEdgeToolMode(LinesEditor editor)
        {
            Editor = editor;
        }

        public override void OnUpdate() => PointsSelector?.OnUpdate();
        public override string GetToolInfo()
        {
            switch (SelectPartEdgePanel.Position)
            {
                case EdgePosition.Start:
                    return Localize.LineEditor_InfoSelectFrom;
                case EdgePosition.End:
                    return Localize.LineEditor_InfoSelectTo;
                default:
                    return null;
            }
        }
        public override void OnPrimaryMouseClicked(Event e)
        {
            if (PointsSelector.IsHoverPoint)
            {
                SelectPartEdgePanel.SelectedObject = PointsSelector.HoverPoint;
                if (AfterSelectPartEdgePanel?.Invoke(e) ?? true)
                    Tool.SetDefaultMode();
            }
        }
        public override void OnSecondaryMouseClicked() => Tool.SetDefaultMode();
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo) => PointsSelector.Render(cameraInfo);

        private void SelectPanelLeaveFocus(UIComponent component, UIFocusEventParameter eventParam) => Tool.SetDefaultMode();
    }

    public class LineItem : EditableItem<MarkupLine, LineIcon>
    {
        public override void Init() => Init(true, true);

        public override string DeleteCaptionDescription => NodeMarkup.Localize.LineEditor_DeleteCaptionDescription;
        public override string DeleteMessageDescription => NodeMarkup.Localize.LineEditor_DeleteMessageDescription;

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
