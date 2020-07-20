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
    public class LinesEditor : Editor<LineItem, MarkupLine, UIPanel>
    {
        public override string Name => NodeMarkup.Localize.LineEditor_Lines;

        private ButtonPanel AddButton { get; set; }

        public List<ISupportPoint> SupportPoints { get; } = new List<ISupportPoint>();
        public bool CanDivide => SupportPoints.Count > 2;

        private ISupportPoint HoverSupportPoint { get; set; }
        private bool IsHoverSupportPoint => IsSelectPartEdgeMode && HoverSupportPoint != null;

        private MarkupLineSelectPropertyPanel SelectPartEdgePanel { get; set; }
        private Func<Event, bool> AfterSelectPartEdgePanel { get; set; }
        private bool IsSelectPartEdgeMode => SelectPartEdgePanel != null;

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
            SupportPoints.Add(new EnterSupportPoint(EditObject.Start));
            SupportPoints.AddRange(EditObject.IntersectLines.Select(l => (ISupportPoint)new IntersectSupportPoint(EditObject, l)));
            SupportPoints.Add(new EnterSupportPoint(EditObject.End));
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
            var newRule = EditObject.AddRule();
            DeleteAddButton();
            var rulePanel = AddRulePanel(newRule);
            AddAddButton();

            SettingsPanel.ScrollToBottom();

            if (Settings.QuickRuleSetup)
                SetupRule(rulePanel);
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
                SelectPartEdgePanel = selectPanel;
                AfterSelectPartEdgePanel = afterAction;
                SelectPartEdgePanel.eventLeaveFocus += SelectPanelLeaveFocus;
                SelectPartEdgePanel.eventLostFocus += SelectPanelLeaveFocus;
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
                SelectPartEdgePanel.eventLeaveFocus -= SelectPanelLeaveFocus;
                SelectPartEdgePanel.eventLostFocus -= SelectPanelLeaveFocus;
                SelectPartEdgePanel = null;
                AfterSelectPartEdgePanel = null;
            }
        }
        protected override void OnObjectDelete(MarkupLine line)
        {
            Markup.RemoveConnect(line.PointPair);
        }
    }

    public class LineItem : EditableItem<MarkupLine, UIPanel>
    {
        public LineItem() : base(false, true) { }

        public override string Description => NodeMarkup.Localize.LineEditor_ItemDescription;
    }

    public class RulePanel : UIPanel
    {
        private LinesEditor Editor { get; set; }
        public MarkupLineRawRule Rule { get; private set; }

        public MarkupLineSelectPropertyPanel From { get; private set; }
        public MarkupLineSelectPropertyPanel To { get; private set; }
        public StylePropertyPanel Style { get; private set; }

        private List<UIComponent> StyleProperties { get; } = new List<UIComponent>();

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
            var header = AddUIComponent<RuleHeaderPanel>();
            header.AddRange(TemplateManager.Templates);
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
            From.SelectedObject = Rule.From?.GetSupport(Editor.EditObject);
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
            To.SelectedObject = Rule.To?.GetSupport(Editor.EditObject);
            To.OnSelectChanged += ToChanged;
            To.OnSelect += ((panel) => Editor.SelectRuleEdge(panel));
            To.OnHover += Editor.HoverRuleEdge;
            To.OnLeave += Editor.LeaveRuleEdge;
        }
        private void AddStyleTypeProperty()
        {
            switch (Rule.Style)
            {
                case ISimpleLine _:
                    Style = AddUIComponent<SimpleStylePropertyPanel>();
                    break;
                case IStopLine _:
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
            AddColorProperty();
            AddWidthProperty();
            AddStyleAdditionalProperties();
        }
        private void AddColorProperty()
        {
            var colorProperty = AddUIComponent<ColorPropertyPanel>();
            colorProperty.Text = NodeMarkup.Localize.LineEditor_Color;
            colorProperty.Init();
            colorProperty.Value = Rule.Style.Color;
            colorProperty.OnValueChanged += ColorChanged;
            StyleProperties.Add(colorProperty);
        }
        private void AddWidthProperty()
        {
            var widthProperty = AddUIComponent<FloatPropertyPanel>();
            widthProperty.Text = NodeMarkup.Localize.LineEditor_Width;
            widthProperty.UseWheel = true;
            widthProperty.WheelStep = 0.01f;
            widthProperty.CheckMin = true;
            widthProperty.MinValue = 0.05f;
            widthProperty.Init();
            widthProperty.Value = Rule.Style.Width;
            widthProperty.OnValueChanged += WidthChanged;
            widthProperty.OnHover += PropertyHover;
            widthProperty.OnLeave += PropertyLeave;
            StyleProperties.Add(widthProperty);
        }
        private void AddStyleAdditionalProperties()
        {
            if (Rule.Style is IDashedLine dashedStyle)
            {
                var dashLengthProperty = AddUIComponent<FloatPropertyPanel>();
                dashLengthProperty.Text = NodeMarkup.Localize.LineEditor_DashedLength;
                dashLengthProperty.UseWheel = true;
                dashLengthProperty.WheelStep = 0.1f;
                dashLengthProperty.CheckMin = true;
                dashLengthProperty.MinValue = 0.1f;
                dashLengthProperty.Init();
                dashLengthProperty.Value = dashedStyle.DashLength;
                dashLengthProperty.OnValueChanged += DashLengthChanged;
                dashLengthProperty.OnHover += PropertyHover;
                dashLengthProperty.OnLeave += PropertyLeave;
                StyleProperties.Add(dashLengthProperty);

                var spaceLengthProperty = AddUIComponent<FloatPropertyPanel>();
                spaceLengthProperty.Text = NodeMarkup.Localize.LineEditor_SpaceLength;
                spaceLengthProperty.UseWheel = true;
                spaceLengthProperty.WheelStep = 0.1f;
                spaceLengthProperty.CheckMin = true;
                spaceLengthProperty.MinValue = 0.1f;
                spaceLengthProperty.Init();
                spaceLengthProperty.Value = dashedStyle.SpaceLength;
                spaceLengthProperty.OnValueChanged += SpaceLengthChanged;
                spaceLengthProperty.OnHover += PropertyHover;
                spaceLengthProperty.OnLeave += PropertyLeave;
                StyleProperties.Add(spaceLengthProperty);
            }
            if (Rule.Style is IDoubleLine doubleStyle)
            {
                var offsetProperty = AddUIComponent<FloatPropertyPanel>();
                offsetProperty.Text = NodeMarkup.Localize.LineEditor_Offset;
                offsetProperty.UseWheel = true;
                offsetProperty.WheelStep = 0.1f;
                offsetProperty.Init();
                offsetProperty.Value = doubleStyle.Offset;
                offsetProperty.OnValueChanged += OffsetChanged;
                offsetProperty.OnHover += PropertyHover;
                offsetProperty.OnLeave += PropertyLeave;
                StyleProperties.Add(offsetProperty);
            }
            if (Rule.Style is IAsymLine asymStyle)
            {
                var invertProperty = AddUIComponent<BoolPropertyPanel>();
                invertProperty.Text = NodeMarkup.Localize.LineEditor_Invert;
                invertProperty.Init();
                invertProperty.Value = asymStyle.Invert;
                invertProperty.OnValueChanged += InvertChanged;
                StyleProperties.Add(invertProperty);
            }
        }

        private void PropertyHover() => Editor.StopScroll();
        private void PropertyLeave() => Editor.StartScroll();

        private void ClearStyleProperties()
        {
            foreach (var property in StyleProperties)
            {
                RemoveUIComponent(property);
                Destroy(property);
            }

            StyleProperties.Clear();
        }

        private void OnSaveTemplate()
        {
            if (TemplateManager.AddTemplate(Rule.Style, out LineStyleTemplate template)) ;
            Editor.NodeMarkupPanel.EditTemplate(template);
        }
        private void OnSelectTemplate(LineStyleTemplate template)
        {
            Rule.Style = template.Style.Copy();
            Style.SelectedObject = Rule.Style.Type;
            ClearStyleProperties();
            AddStyleProperties();
        }

        private void ColorChanged(Color32 color) => Rule.Style.Color = color;
        private void FromChanged(ISupportPoint from) => Rule.From = from.GetPartEdge(Editor.EditObject);
        private void ToChanged(ISupportPoint to) => Rule.To = to.GetPartEdge(Editor.EditObject);
        private void StyleChanged(BaseStyle.LineType style)
        {
            var newStyle = TemplateManager.GetDefault(style);
            newStyle.Color = Rule.Style.Color;
            if (newStyle is IDashedLine newDashed && Rule.Style is IDashedLine oldDashed)
            {
                newDashed.DashLength = oldDashed.DashLength;
                newDashed.SpaceLength = oldDashed.SpaceLength;
            }
            if (newStyle is IDoubleLine newDouble && Rule.Style is IDoubleLine oldDouble)
                newDouble.Offset = oldDouble.Offset;

            Rule.Style = newStyle;

            ClearStyleProperties();
            AddStyleProperties();
        }
        private void WidthChanged(float value) => Rule.Style.Width = value;
        private void DashLengthChanged(float value) => (Rule.Style as IDashedLine).DashLength = value;
        private void SpaceLengthChanged(float value) => (Rule.Style as IDashedLine).SpaceLength = value;
        private void OffsetChanged(float value) => (Rule.Style as IDoubleLine).Offset = value;
        private void InvertChanged(bool value) => (Rule.Style as IAsymLine).Invert = value;

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
