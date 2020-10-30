using ColossalFramework.Math;
using ColossalFramework.Threading;
using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;
using static ToolBase;

namespace NodeMarkup.UI.Editors
{
    public class LinesEditor : GroupedEditor<LineItem, MarkupLine, LineIcon, LineGroup, MarkupLine.LineType>
    {
        public static Color HoverAlpha
        {
            get
            {
                var color = Colors.Hover;
                color.a = 128;
                return color;
            }
        }
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
            SettingsPanel.autoLayoutPadding = new RectOffset(10, 10, 10, 10);
            PartEdgeToolMode = new PartEdgeToolMode(this);
        }

        protected override MarkupLine.LineType SelectGroup(MarkupLine editableItem) => editableItem.Type;
        protected override string GroupName(MarkupLine.LineType group) => group.Description();

        protected override void FillItems()
        {
            foreach (var line in Markup.Lines)
                AddItem(line);
        }
        protected override void ClearSettings()
        {
            HoverRulePanel = null;
            base.ClearSettings();
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
            var rulePanel = ComponentPool.Get<RulePanel>(SettingsPanel);
            rulePanel.Init(this, rule);
            rulePanel.OnHover += RuleMouseHover;
            rulePanel.OnEnter += RuleMouseLeave;
            return rulePanel;
        }
        private void RemoveRulePanel(RulePanel rulePanel)
        {
            if (HoverRulePanel == rulePanel)
                HoverRulePanel = null;

            ComponentPool.Free(rulePanel);
        }
        private void AddAddButton()
        {
            if (AddRuleAvailable)
            {
                AddButton = ComponentPool.Get<ButtonPanel>(SettingsPanel);
                AddButton.Text = NodeMarkup.Localize.LineEditor_AddRuleButton;
                AddButton.Init();
                AddButton.OnButtonClick += AddRule;
            }
        }
        private void DeleteAddButton()
        {
            ComponentPool.Free(AddButton);
            AddButton = null;
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
        private void SetupRule(RulePanel rulePanel) => SelectRuleEdge(rulePanel.From, (_) => SelectRuleEdge(rulePanel.To, (_) => SetStyle(rulePanel)));
        private bool SetStyle(RulePanel rulePanel)
        {
            var style = NodeMarkupTool.GetStyle(RegularLineStyle.RegularLineType.Dashed);
            rulePanel.Style.SelectedObject = style != Style.StyleType.EmptyLine ? style : (Style.StyleType)(int)RegularLineStyle.RegularLineType.Dashed;
            SettingsPanel.ScrollToBottom();
            return true;
        }
        public void DeleteRule(RulePanel rulePanel)
        {
            if (!(EditObject is MarkupRegularLine regularLine))
                return;

            if (Settings.DeleteWarnings && Settings.DeleteWarningsType == 0)
            {
                var messageBox = MessageBoxBase.ShowModal<YesNoMessageBox>();
                messageBox.CaprionText = NodeMarkup.Localize.LineEditor_DeleteRuleCaption;
                messageBox.MessageText = $"{NodeMarkup.Localize.LineEditor_DeleteRuleMessage}\n{NodeMarkup.Localize.MessageBox_CantUndone}";
                messageBox.OnButton1Click = Delete;
            }
            else
                Delete();

            bool Delete()
            {
                regularLine.RemoveRule(rulePanel.Rule as MarkupLineRawRule<RegularLineStyle>);
                RemoveRulePanel(rulePanel);
                Refresh();
                DeleteAddButton();
                AddAddButton();
                return true;
            }
        }
        public bool SelectRuleEdge(MarkupLineSelectPropertyPanel selectPanel) => SelectRuleEdge(selectPanel, null);
        public bool SelectRuleEdge(MarkupLineSelectPropertyPanel selectPanel, Func<Event, bool> afterAction)
        {
            if (Tool.Mode == PartEdgeToolMode && selectPanel == PartEdgeToolMode.SelectPanel)
            {
                Tool.SetDefaultMode();
                return true;
            }
            else
            {
                Tool.SetMode(PartEdgeToolMode);
                PartEdgeToolMode.SelectPanel = selectPanel;
                PartEdgeToolMode.AfterSelectPanel = afterAction;
                selectPanel.Focus();
                return false;
            }
        }
        public void HoverRuleEdge(MarkupLineSelectPropertyPanel selectPanel) => HoverPartEdgePanel = selectPanel;
        public void LeaveRuleEdge(MarkupLineSelectPropertyPanel selectPanel) => HoverPartEdgePanel = null;
        private void RuleMouseHover(RulePanel rulePanel, UIMouseEventParameter eventParam) => HoverRulePanel = rulePanel;
        private void RuleMouseLeave(RulePanel rulePanel, UIMouseEventParameter eventParam)
        {
            var uiView = rulePanel.GetUIView();
            var mouse = uiView.ScreenPointToGUI((eventParam.position + eventParam.moveDelta) / uiView.inputScale);
            var ruleRect = new Rect(SettingsPanel.absolutePosition + rulePanel.relativePosition, rulePanel.size);
            var settingsRect = new Rect(SettingsPanel.absolutePosition, SettingsPanel.size);

            if (eventParam.source == rulePanel || !ruleRect.Contains(mouse) || !settingsRect.Contains(mouse))
                HoverRulePanel = null;
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
                    HoverItem.Object.Render(cameraInfo, Colors.Hover, 2f);

                if (IsHoverRulePanel)
                    HoverRulePanel.Rule.Render(cameraInfo, HoverAlpha, 2f);

                if (IsHoverPartEdgePanel && HoverPartEdgePanel.SelectedObject is SupportPoint supportPoint)
                    supportPoint.Render(cameraInfo, Colors.Hover);
            }
        }
        protected override void OnObjectUpdate()
        {
            GetRuleEdges();
            RefreshRulePanels();
        }
        protected override void OnObjectDelete(MarkupLine line) => Markup.RemoveConnect(line);
        public void Refresh()
        {
            RefreshItem();
            RefreshRulePanels();
        }
        public void RefreshItem() => SelectItem.Refresh();
        private void RefreshRulePanels()
        {
            var rulePanels = SettingsPanel.components.OfType<RulePanel>().ToArray();

            foreach (var rulePanel in rulePanels)
            {
                if (EditObject.ContainsRule(rulePanel.Rule))
                    rulePanel.Refresh();
                else
                    RemoveRulePanel(rulePanel);
            }
            foreach (var rule in EditObject.Rules)
            {
                if (!rulePanels.Any(r => r.Rule == rule))
                    AddRulePanel(rule);
            }
        }
    }
    public class PartEdgeToolMode : BasePanelMode<LinesEditor, MarkupLineSelectPropertyPanel, ILinePartEdge>
    {
        protected override bool IsHover => PointsSelector.IsHoverPoint;
        protected override ILinePartEdge Hover => PointsSelector.HoverPoint;
        public PointsSelector<ILinePartEdge> PointsSelector { get; set; }

        public PartEdgeToolMode(LinesEditor editor) : base(editor) { }

        protected override void OnSetPanel()
            => PointsSelector = new PointsSelector<ILinePartEdge>(Editor.SupportPoints, SelectPanel.Position == EdgePosition.Start ? Colors.Green : Colors.Red);

        public override void End() => Editor.Refresh();
        public override void OnUpdate() => PointsSelector?.OnUpdate();
        public override string GetToolInfo()
        {
            var info = SelectPanel.Position switch
            {
                EdgePosition.Start => Localize.LineEditor_InfoSelectFrom,
                EdgePosition.End => Localize.LineEditor_InfoSelectTo,
                _ => string.Empty,
            };

            return info;
            //return IsHover ? $"{info}\n({Hover})" : info;
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo) => PointsSelector.Render(cameraInfo);
    }

    public class LineItem : EditableItem<MarkupLine, LineIcon>
    {
        private bool HasOverlapped { get; set; }
        public override Color32 NormalColor => HasOverlapped ? new Color32(246, 85, 85, 255) : base.NormalColor;
        public override Color32 HoveredColor => HasOverlapped ? new Color32(247, 100, 100, 255) : base.HoveredColor;
        public override Color32 PressedColor => HasOverlapped ? new Color32(248, 114, 114, 255) : base.PressedColor;
        public override Color32 FocusColor => HasOverlapped ? new Color32(249, 127, 127, 255) : base.FocusColor;
        public override void Refresh()
        {
            base.Refresh();

            SetIcon();

            HasOverlapped = Object.HasOverlapped;
            SetColors();
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
