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
        public override string Name { get; } = "Lines";

        private ButtonPanel AddButton { get; set; }

        public List<LineRawRuleEdgeBase> RuleEdges { get; } = new List<LineRawRuleEdgeBase>();
        private List<LineRawRuleEdgeBound> RuleEdgeBounds { get; } = new List<LineRawRuleEdgeBound>();
        public bool SupportRules => RuleEdges.Count > 2;

        private LineRawRuleEdgeBound HoverRuleEdgeBounds { get; set; }
        private bool IsHoverRuleEdgeBounds => IsSelectRuleEdgeMode && HoverRuleEdgeBounds != null;

        private MarkupLineSelectPropertyPanel SelectRuleEdgePanel { get; set; }
        private Action AfterSelectRuleEdgePanel { get; set; }
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
            RuleEdges.Add(new SelfPointRawRuleEdge(EditObject.Start));
            RuleEdges.AddRange(intersectWith.Select(i => new LineRawRuleEdge(i) as LineRawRuleEdgeBase));
            RuleEdges.Add(new SelfPointRawRuleEdge(EditObject.End));

            RuleEdgeBounds.Clear();
            RuleEdgeBounds.AddRange(RuleEdges.Select(r => new LineRawRuleEdgeBound(EditObject, r)));
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
                AddButton.Text = "Add Rule";
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

            if(Settings.QuickRuleSetip)
                SetupRule(rulePanel);
        }
        private void SetupRule(RulePanel rulePanel)
        {
            SelectRuleEdge(rulePanel.From, () => SelectRuleEdge(rulePanel.To));
        }
        public void DeleteRule(RulePanel rulePanel)
        {
            if (Settings.DeleteWarnings)
            {
                var messageBox = MessageBox.ShowModal<YesNoMessageBox>();
                messageBox.CaprionText = $"Delete rule";
                messageBox.MessageText = "Do you really want delete rule?\nThis action cannot be undone";
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
        public void SelectRuleEdge(MarkupLineSelectPropertyPanel selectPanel) => SelectRuleEdge(selectPanel, null);
        public void SelectRuleEdge(MarkupLineSelectPropertyPanel selectPanel, Action afterAction)
        {
            if (IsSelectRuleEdgeMode)
            {
                var isToggle = SelectRuleEdgePanel == selectPanel;
                NodeMarkupPanel.EndEditorAction();
                if (isToggle)
                    return;
            }
            NodeMarkupPanel.StartEditorAction(this, out bool isAccept);
            if (isAccept)
            {
                SelectRuleEdgePanel = selectPanel;
                AfterSelectRuleEdgePanel = afterAction;
                SelectRuleEdgePanel.eventLeaveFocus += SelectPanelLeaveFocus;
                SelectRuleEdgePanel.eventLostFocus += SelectPanelLeaveFocus;
            }
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
                SelectRuleEdgePanel.SelectedObject = HoverRuleEdgeBounds.LineRawRuleEdge;

                if (AfterSelectRuleEdgePanel != null)
                {
                    isDone = false;
                    AfterSelectRuleEdgePanel();
                }
                else
                {
                    isDone = true;
                    NodeMarkupPanel.EndEditorAction();
                }
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
                    var color = (SelectRuleEdgePanel.Position == LineRawRuleEdgeBase.EdgePosition.From ? Color.green : Color.red);
                    NodeMarkupTool.RenderManager.OverlayEffect.DrawCircle(cameraInfo, color, bounds.Position, 0.5f, -1f, 1280f, false, true);
                }

                if (IsHoverRuleEdgeBounds)
                    NodeMarkupTool.RenderManager.OverlayEffect.DrawCircle(cameraInfo, Color.white, HoverRuleEdgeBounds.Position, 1f, -1f, 1280f, false, true);
            }
            else
            {
                if (IsHoverItem)
                    NodeMarkupTool.RenderManager.OverlayEffect.DrawBezier(cameraInfo, Color.white, HoverItem.Object.Trajectory, 2f, 0f, 0f, -1f, 1280f, false, true);
                if (IsHoverRuleEdgePanel &&
                    HoverRuleEdgePanel.SelectedObject is LineRawRuleEdgeBase lineRawRuleEdge &&
                    RuleEdgeBounds.FirstOrDefault(b => b.LineRawRuleEdge == lineRawRuleEdge) is LineRawRuleEdgeBound bounds)
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
                    case LineRawRuleEdgeBase.EdgePosition.From:
                        return "Selet rule`s from point";
                    case LineRawRuleEdgeBase.EdgePosition.To:
                        return "Select rule`s to point";
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

        public override string Description => "line";
    }

    public class RulePanel : UIPanel
    {
        private LinesEditor Editor { get; set; }
        public MarkupLineRawRule Rule { get; private set; }

        public MarkupLineSelectPropertyPanel From { get; private set; }
        public MarkupLineSelectPropertyPanel To { get; private set; }

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

        private void AddStyleProperties()
        {
            AddColorProperty();
            AddStyleTypeProperty();
            AddStyleAdditionalProperties();
        }
        private void AddFromProperty()
        {
            From = AddUIComponent<MarkupLineSelectPropertyPanel>();
            From.Text = "From";
            From.Position = LineRawRuleEdgeBase.EdgePosition.From;
            From.Init();
            From.AddRange(Editor.RuleEdges);
            From.SelectedObject = Rule.From;
            From.OnSelectChanged += FromChanged;
            From.OnSelect += Editor.SelectRuleEdge;
            From.OnHover += Editor.HoverRuleEdge;
            From.OnLeave += Editor.LeaveRuleEdge;
        }

        private void AddToProperty()
        {
            To = AddUIComponent<MarkupLineSelectPropertyPanel>();
            To.Text = "To";
            To.Position = LineRawRuleEdgeBase.EdgePosition.To;
            To.Init();
            To.AddRange(Editor.RuleEdges);
            To.SelectedObject = Rule.To;
            To.OnSelectChanged += ToChanged;
            To.OnSelect += Editor.SelectRuleEdge;
            To.OnHover += Editor.HoverRuleEdge;
            To.OnLeave += Editor.LeaveRuleEdge;
        }
        private void AddColorProperty()
        {
            var colorProperty = AddUIComponent<ColorPropertyPanel>();
            colorProperty.Text = "Color";
            colorProperty.Init();
            colorProperty.Value = Rule.Style.Color;
            colorProperty.OnValueChanged += ColorChanged;
            StyleProperties.Add(colorProperty);
        }
        private void AddStyleTypeProperty()
        {
            var styleProperty = AddUIComponent<StylePropertyPanel>();
            styleProperty.Text = "Style";
            styleProperty.Init();
            styleProperty.SelectedObject = Rule.Style.Type;
            styleProperty.OnSelectObjectChanged += StyleChanged;
            StyleProperties.Add(styleProperty);
        }
        private void AddStyleAdditionalProperties()
        {
            if (Rule.Style is IDashedLine dashedStyle)
            {
                var dashLengthProperty = AddUIComponent<FloatPropertyPanel>();
                dashLengthProperty.Text = "Dashed length";
                dashLengthProperty.UseWheel = true;
                dashLengthProperty.Step = 0.1f;
                dashLengthProperty.CheckMin = true;
                dashLengthProperty.MinValue = 0.1f;
                dashLengthProperty.Init();
                dashLengthProperty.Value = dashedStyle.DashLength;
                dashLengthProperty.OnValueChanged += DashLengthChanged;
                dashLengthProperty.OnHover += PropertyHover;
                dashLengthProperty.OnLeave += PropertyLeave;
                StyleProperties.Add(dashLengthProperty);

                var spaceLengthProperty = AddUIComponent<FloatPropertyPanel>();
                spaceLengthProperty.Text = "Space length";
                spaceLengthProperty.UseWheel = true;
                spaceLengthProperty.Step = 0.1f;
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
                offsetProperty.Text = "Offset";
                offsetProperty.UseWheel = true;
                offsetProperty.Step = 0.1f;
                offsetProperty.Init();
                offsetProperty.Value = doubleStyle.Offset;
                offsetProperty.OnValueChanged += OffsetChanged;
                offsetProperty.OnHover += PropertyHover;
                offsetProperty.OnLeave += PropertyLeave;
                StyleProperties.Add(offsetProperty);
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
            ClearStyleProperties();
            AddStyleProperties();
        }

        private void ColorChanged(Color32 color) => Rule.Style.Color = color;
        private void FromChanged(LineRawRuleEdgeBase from) => Rule.From = from;
        private void ToChanged(LineRawRuleEdgeBase to) => Rule.To = to;
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
            AddStyleProperties();
        }
        private void DashLengthChanged(float value) => (Rule.Style as IDashedLine).DashLength = value;
        private void SpaceLengthChanged(float value) => (Rule.Style as IDashedLine).SpaceLength = value;
        private void OffsetChanged(float value) => (Rule.Style as IDoubleLine).Offset = value;

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
