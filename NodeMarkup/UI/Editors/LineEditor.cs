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

namespace NodeMarkup.UI.Editors
{
    public class LinesEditor : Editor<LineItem, MarkupLine, LineIcon>
    {
        public override string Name => NodeMarkup.Localize.LineEditor_Lines;

        private ButtonPanel AddButton { get; set; }

        public List<ILinePartEdge> SupportPoints { get; } = new List<ILinePartEdge>();
        public bool CanDivide => SupportPoints.Count > 2;

        private ILinePartEdge HoverSupportPoint { get; set; }
        private bool IsHoverSupportPoint => IsSelectPartEdgeMode && HoverSupportPoint != null;

        private MarkupLineSelectPropertyPanel _selectPartEdgePanel;
        private MarkupLineSelectPropertyPanel SelectPartEdgePanel 
        {
            get => _selectPartEdgePanel;
            set
            {
                if(_selectPartEdgePanel != null)
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

        public LinesEditor()
        {

        }
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
            return rulePanel;
        }

        private void AddAddButton()
        {
            if (CanDivide)
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

            var newRule = EditObject.AddRule();
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
                var messageBox = MessageBox.ShowModal<YesNoMessageBox>();
                messageBox.CaprionText = NodeMarkup.Localize.LineEditor_DeleteRuleCaption;
                messageBox.MessageText = NodeMarkup.Localize.LineEditor_DeleteRuleMessage;
                messageBox.OnButton1Click = Delete;
            }
            else
                Delete();

            RefreshItem();

            bool Delete()
            {
                EditObject.RemoveRule(rulePanel.Rule);
                SettingsPanel.RemoveUIComponent(rulePanel);
                Destroy(rulePanel);
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
            if (CanDivide && !IsSelectPartEdgeMode && NodeMarkupTool.AddRuleShortcut.IsPressed(e))
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

    public class RulePanel : UIPanel
    {
        private LinesEditor Editor { get; set; }
        public MarkupLineRawRule Rule { get; private set; }

        public MarkupLineSelectPropertyPanel From { get; private set; }
        public MarkupLineSelectPropertyPanel To { get; private set; }
        public StylePropertyPanel Style { get; private set; }

        private List<UIComponent> StyleProperties { get; set; } = new List<UIComponent>();

        public RulePanel()
        {
            atlas = NodeMarkupPanel.InGameAtlas;
            backgroundSprite = "AssetEditorItemBackground";
            autoLayout = true;
            autoFitChildrenVertically = true;
            autoLayoutDirection = LayoutDirection.Vertical;
            autoLayoutPadding = new RectOffset(5, 5, 0, 0);
        }

        public void Init(LinesEditor editor, MarkupLineRawRule rule)
        {
            Editor = editor;
            Rule = rule;

            SetSize();

            AddHeader();
            if (Editor.CanDivide)
            {
                AddFromProperty();
                AddToProperty();
            }
            AddStyleTypeProperty();
            AddStyleProperties();
        }

        private void SetSize()
        {
            if (parent is UIScrollablePanel scrollablePanel)
                width = scrollablePanel.width - scrollablePanel.autoLayoutPadding.horizontal;
            else if (parent is UIPanel panel)
                width = panel.width - panel.autoLayoutPadding.horizontal;
            else
                width = parent.width;
        }
        private void AddHeader()
        {
            var header = AddUIComponent<StyleHeaderPanel>();
            header.AddRange(TemplateManager.GetTemplates(Rule.Style.Type));
            header.Init(Editor.CanDivide);
            header.OnDelete += () => Editor.DeleteRule(this);
            header.OnSaveTemplate += OnSaveTemplate;
            header.OnSelectTemplate += OnSelectTemplate;
        }
        private void AddFromProperty()
        {
            From = AddUIComponent<MarkupLineSelectPropertyPanel>();
            From.Text = NodeMarkup.Localize.LineEditor_From;
            From.Position = RulePosition.Start;
            From.Init();
            From.AddRange(Editor.SupportPoints);
            From.SelectedObject = Rule.From;
            From.OnSelectChanged += FromChanged;
            From.OnSelect += ((panel) => Editor.SelectRuleEdge(panel));
            From.OnHover += Editor.HoverRuleEdge;
            From.OnLeave += Editor.LeaveRuleEdge;
        }

        private void AddToProperty()
        {
            To = AddUIComponent<MarkupLineSelectPropertyPanel>();
            To.Text = NodeMarkup.Localize.LineEditor_To;
            To.Position = RulePosition.End;
            To.Init();
            To.AddRange(Editor.SupportPoints);
            To.SelectedObject = Rule.To;
            To.OnSelectChanged += ToChanged;
            To.OnSelect += (panel) => Editor.SelectRuleEdge(panel);
            To.OnHover += Editor.HoverRuleEdge;
            To.OnLeave += Editor.LeaveRuleEdge;
        }
        private void AddStyleTypeProperty()
        {
            switch (Rule.Style.Type & Manager.Style.StyleType.GroupMask)
            {
                case Manager.Style.StyleType.RegularLine:
                    Style = AddUIComponent<RegularStylePropertyPanel>();
                    break;
                case Manager.Style.StyleType.StopLine:
                    Style = AddUIComponent<StopStylePropertyPanel>();
                    break;
                default:
                    return;
            }
            Style.Text = NodeMarkup.Localize.LineEditor_Style;
            Style.Init();
            Style.SelectedObject = Rule.Style.Type;
            Style.OnSelectObjectChanged += StyleChanged;
        }
        private void AddStyleProperties()
        {
            StyleProperties = Rule.Style.GetUIComponents(Rule, this, Editor.StopScroll, Editor.StartScroll);
            if (StyleProperties.FirstOrDefault() is ColorPropertyPanel colorProperty)
                colorProperty.OnValueChanged += (Color32 c) => Editor.RefreshItem();
        }

        private void ClearStyleProperties()
        {
            foreach (var property in StyleProperties)
            {
                RemoveUIComponent(property);
                Destroy(property);
            }
        }

        private void OnSaveTemplate()
        {
            if (TemplateManager.AddTemplate(Rule.Style, out StyleTemplate template))
                Editor.NodeMarkupPanel.EditTemplate(template);
        }
        private void OnSelectTemplate(StyleTemplate template)
        {
            if (template.Style.Copy() is LineStyle style)
            {
                Rule.Style = style;
                Style.SelectedObject = Rule.Style.Type;

                Editor.RefreshItem();
                ClearStyleProperties();
                AddStyleProperties();
            }
        }

        private void FromChanged(ILinePartEdge from) => Rule.From = from;
        private void ToChanged(ILinePartEdge to) => Rule.To = to;
        private void StyleChanged(Style.StyleType style)
        {
            if (style == Rule.Style.Type)
                return;

            var newStyle = TemplateManager.GetDefault<LineStyle>(style);
            newStyle.Color = Rule.Style.Color;
            newStyle.Width = Rule.Style.Width;
            if (newStyle is IDashedLine newDashed && Rule.Style is IDashedLine oldDashed)
            {
                newDashed.DashLength = oldDashed.DashLength;
                newDashed.SpaceLength = oldDashed.SpaceLength;
            }
            if (newStyle is IDoubleLine newDouble && Rule.Style is IDoubleLine oldDouble)
                newDouble.Offset = oldDouble.Offset;

            Rule.Style = newStyle;

            Editor.RefreshItem();
            ClearStyleProperties();
            AddStyleProperties();
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            foreach (var item in components)
            {
                item.width = width - autoLayoutPadding.horizontal;
            }
        }
    }
}
