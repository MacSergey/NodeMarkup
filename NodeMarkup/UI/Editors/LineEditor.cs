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

        public List<SupportPointBase> RuleEdges { get; } = new List<SupportPointBase>();
        private List<RuleSupportPointBound> RuleEdgeBounds { get; } = new List<RuleSupportPointBound>();
        public bool SupportRules => RuleEdges.Count > 2;

        private RuleSupportPointBound HoverRuleEdgeBounds { get; set; }
        private bool IsHoverRuleEdgeBounds => IsSelectRuleEdgeMode && HoverRuleEdgeBounds != null;

        private MarkupLineSelectPropertyPanel SelectRuleEdgePanel { get; set; }
        private Func<Event, bool> AfterSelectRuleEdgePanel { get; set; }
        private bool IsSelectRuleEdgeMode => SelectRuleEdgePanel != null;

        private MarkupLineSelectPropertyPanel HoverRuleEdgePanel { get; set; }
        private bool IsHoverRuleEdgePanel => HoverRuleEdgePanel != null;

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
            var intersectWith = EditObject.IntersectWith();

            RuleEdges.Clear();
            RuleEdges.Add(new EnterSupportPoint(EditObject.Start));
            RuleEdges.AddRange(intersectWith.Select(i => new LineSupportPoint(i) as SupportPointBase));
            RuleEdges.Add(new EnterSupportPoint(EditObject.End));

            RuleEdgeBounds.Clear();
            RuleEdgeBounds.AddRange(RuleEdges.Select(r => new RuleSupportPointBound(EditObject, r)));
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
            if (SupportRules)
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
            rulePanel.Style.SelectedObject = e.GetStyle();
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
            if (IsSelectRuleEdgeMode)
            {
                var isToggle = SelectRuleEdgePanel == selectPanel;
                NodeMarkupPanel.EndEditorAction();
                if (isToggle)
                    return true;
            }
            NodeMarkupPanel.StartEditorAction(this, out bool isAccept);
            if (isAccept)
            {
                SelectRuleEdgePanel = selectPanel;
                AfterSelectRuleEdgePanel = afterAction;
                SelectRuleEdgePanel.eventLeaveFocus += SelectPanelLeaveFocus;
                SelectRuleEdgePanel.eventLostFocus += SelectPanelLeaveFocus;
                return false;
            }
            return true;
        }
        public void HoverRuleEdge(MarkupLineSelectPropertyPanel selectPanel) => HoverRuleEdgePanel = selectPanel;
        public void LeaveRuleEdge(MarkupLineSelectPropertyPanel selectPanel) => HoverRuleEdgePanel = null;

        private void SelectPanelLeaveFocus(UIComponent component, UIFocusEventParameter eventParam) => NodeMarkupPanel.EndEditorAction();

        public override void OnUpdate()
        {
            if (!UIView.IsInsideUI() && Cursor.visible)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                foreach (var ruleEdgeBound in RuleEdgeBounds)
                {
                    if (ruleEdgeBound.IsIntersect(ray))
                    {
                        HoverRuleEdgeBounds = ruleEdgeBound;
                        return;
                    }
                }
            }

            HoverRuleEdgeBounds = null;
        }
        public override void OnEvent(Event e)
        {
            if (SupportRules && !IsSelectRuleEdgeMode && NodeMarkupTool.AddRuleShortcut.IsPressed(e))
                AddRule();
        }
        public override void OnPrimaryMouseClicked(Event e, out bool isDone)
        {
            if (IsHoverRuleEdgeBounds)
            {
                SelectRuleEdgePanel.SelectedObject = (SupportPointBase)HoverRuleEdgeBounds.SupportPoint;

                if (isDone = AfterSelectRuleEdgePanel?.Invoke(e) ?? true)
                    NodeMarkupPanel.EndEditorAction();
            }
            else
                isDone = false;
        }
        public override void Render(RenderManager.CameraInfo cameraInfo)
        {
            if (IsSelectRuleEdgeMode)
            {
                foreach (var bounds in RuleEdgeBounds)
                {
                    var color = (SelectRuleEdgePanel.Position == RulePosition.Start ? Color.green : Color.red);
                    NodeMarkupTool.RenderManager.OverlayEffect.DrawCircle(cameraInfo, color, bounds.Position, 0.5f, -1f, 1280f, false, true);
                }

                if (IsHoverRuleEdgeBounds)
                    NodeMarkupTool.RenderManager.OverlayEffect.DrawCircle(cameraInfo, Color.white, HoverRuleEdgeBounds.Position, 1f, -1f, 1280f, false, true);
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
                if (IsHoverRuleEdgePanel &&
                    HoverRuleEdgePanel.SelectedObject is SupportPointBase lineRawRuleEdge &&
                    RuleEdgeBounds.FirstOrDefault(b => b.SupportPoint == lineRawRuleEdge) is RuleSupportPointBound bounds)
                {
                    NodeMarkupTool.RenderManager.OverlayEffect.DrawCircle(cameraInfo, Color.white, bounds.Position, 0.5f, -1f, 1280f, false, true);
                }
            }
        }
        public override string GetInfo()
        {
            if (IsSelectRuleEdgeMode)
            {
                switch (SelectRuleEdgePanel.Position)
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
            if (IsSelectRuleEdgeMode)
            {
                SelectRuleEdgePanel.eventLeaveFocus -= SelectPanelLeaveFocus;
                SelectRuleEdgePanel.eventLostFocus -= SelectPanelLeaveFocus;
                SelectRuleEdgePanel = null;
                AfterSelectRuleEdgePanel = null;
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
            if (Editor.SupportRules)
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
            header.Init(Editor.SupportRules);
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
            From.AddRange(Editor.RuleEdges);
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
            To.AddRange(Editor.RuleEdges);
            To.SelectedObject = Rule.To;
            To.OnSelectChanged += ToChanged;
            To.OnSelect += ((panel) => Editor.SelectRuleEdge(panel));
            To.OnHover += Editor.HoverRuleEdge;
            To.OnLeave += Editor.LeaveRuleEdge;
        }
        private void AddStyleTypeProperty()
        {
            if (Rule.Style.Type == LineStyle.LineType.Stop)
                return;

            Style = AddUIComponent<StylePropertyPanel>();
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
        private void FromChanged(SupportPointBase from) => Rule.From = from;
        private void ToChanged(SupportPointBase to) => Rule.To = to;
        private void StyleChanged(LineStyle.LineType style)
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
            AddStyleAdditionalProperties();
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
