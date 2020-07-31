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
    public class LinesEditor : Editor<LineItem, MarkupLine, LineIcon>
    {
        public static Color WhiteAlpha { get; } = new Color(1, 1, 1, 0.5f);
        public override string Name => NodeMarkup.Localize.LineEditor_Lines;

        private ButtonPanel AddButton { get; set; }

        public List<ILinePartEdge> SupportPoints { get; } = new List<ILinePartEdge>();
        public bool CanDivide => SupportPoints.Count > 2;
        private bool AddRuleAvailable => CanDivide || EditObject?.RawRules.Any() == false;

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

        protected override void FillItems()
        {
            foreach (var line in Markup.Lines)
            {
                AddItem(line);
            }
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
            SupportPoints.AddRange(EditObject.IntersectLines.Select(l => (ILinePartEdge)new LinesIntersectEdge(EditObject, l)));
            SupportPoints.Add(new EnterPointEdge(EditObject.End));
        }
        private void AddRulePanels()
        {
            foreach (var rule in EditObject.RawRules)
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
            if (EditObject == null)
                return;

            var newRule = EditObject.AddRule(CanDivide);
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
            rulePanel.Style.SelectedObject = e.GetSimpleStyle();
            return true;
        }
        public void DeleteRule(RulePanel rulePanel)
        {
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
                EditObject.RemoveRule(rulePanel.Rule);
                SettingsPanel.RemoveUIComponent(rulePanel);
                Destroy(rulePanel);
                RefreshItem();
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

        public override void OnUpdate()
        {
            if (!UIView.IsInsideUI() && Cursor.visible)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                foreach (var supportPoint in SupportPoints)
                {
                    if (supportPoint.IsIntersect(ray))
                    {
                        HoverSupportPoint = supportPoint;
                        return;
                    }
                }
            }

            HoverSupportPoint = null;
        }
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
                    var color = (SelectPartEdgePanel.Position == RulePosition.Start ? Color.green : Color.red);
                    NodeMarkupTool.RenderManager.OverlayEffect.DrawCircle(cameraInfo, color, supportPoint.Position, 0.5f, -1f, 1280f, false, true);
                }

                if (IsHoverSupportPoint)
                    NodeMarkupTool.RenderManager.OverlayEffect.DrawCircle(cameraInfo, Color.white, HoverSupportPoint.Position, 1f, -1f, 1280f, false, true);
            }
            else
            {
                if (IsHoverItem)
                {
                    var bezier = HoverItem.Object.Trajectory;
                    if (HoverItem.Object.IsEnterLine)
                    {
                        bezier.b = bezier.a + (bezier.d - bezier.a).normalized;
                        bezier.c = bezier.d + (bezier.a - bezier.d).normalized;
                    }
                    NodeMarkupTool.RenderManager.OverlayEffect.DrawBezier(cameraInfo, Color.white, bezier, 2f, 0f, 0f, -1f, 1280f, false, true);
                }
                if (IsHoverRulePanel)
                {
                    if (HoverRulePanel.Rule.GetTrajectory(out Bezier3 bezier))
                        NodeMarkupTool.RenderManager.OverlayEffect.DrawBezier(cameraInfo, WhiteAlpha, bezier, 2f, 0f, 0f, -1f, 1280f, false, true);
                }
                if (IsHoverPartEdgePanel && HoverPartEdgePanel.SelectedObject is ISupportPoint supportPoint)
                {
                    NodeMarkupTool.RenderManager.OverlayEffect.DrawCircle(cameraInfo, Color.white, supportPoint.Position, 0.5f, -1f, 1280f, false, true);
                }
            }
        }
        public override string GetInfo()
        {
            if (IsSelectPartEdgeMode)
            {
                switch (SelectPartEdgePanel.Position)
                {
                    case RulePosition.Start:
                        return NodeMarkup.Localize.LineEditor_InfoSelectFrom;
                    case RulePosition.End:
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
        public LineItem() : base(true, true) { }

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

            Icon.Count = Object.RawRules.Count;
            if (Object.RawRules.Count == 1)
            {
                Icon.Type = Object.RawRules[0].Style.Type;
                Icon.StyleColor = Object.RawRules[0].Style.Color;
            }
        }
    }
    public class LineIcon : StyleIcon
    {
        protected UILabel CountLabel { get; }
        public int Count
        {
            set
            {
                CountLabel.isVisible = value > 1;
                Thumbnail.isVisible = value == 1;
                CountLabel.text = value.ToString();
            }
        }

        public LineIcon()
        {
            CountLabel = AddUIComponent<UILabel>();
            CountLabel.textColor = Color.white;
            CountLabel.textScale = 0.7f;
            CountLabel.relativePosition = new Vector3(0, 0);
            CountLabel.autoSize = false;
            CountLabel.textAlignment = UIHorizontalAlignment.Center;
            CountLabel.verticalAlignment = UIVerticalAlignment.Middle;
            CountLabel.padding = new RectOffset(0, 0, 5, 0);
        }
        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            if (CountLabel != null)
                CountLabel.size = size;
        }
    }
}
