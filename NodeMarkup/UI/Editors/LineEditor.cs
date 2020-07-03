using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class LinesEditor : Editor<LineItem, MarkupLine, UIPanel>
    {
        public override string Name { get; } = "Lines";

        public List<LineRawRuleEdgeBase> RuleEdges { get; } = new List<LineRawRuleEdgeBase>();
        private List<LineRawRuleEdgeBound> RuleEdgeBounds { get; } = new List<LineRawRuleEdgeBound>();

        private LineRawRuleEdgeBound HoverRuleEdgeBounds { get; set; }
        private bool IsHoverRuleEdgeBounds => IsSelectRuleEdgeMode && HoverRuleEdgeBounds != null;

        private MarkupLineSelectPropertyPanel SelectRuleEdgePanel { get; set; }
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
            AddAddButton();
            AddRulePanels();
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
        private void AddRulePanel(MarkupLineRawRule rule)
        {
            var rulePanel = SettingsPanel.AddUIComponent<RulePanel>();
            rulePanel.Init(this, rule);
        }

        private void AddAddButton()
        {
            if (RuleEdges.Count > 2)
            {
                var button = SettingsPanel.AddUIComponent<ButtonPanel>();
                button.Text = "Add Rule";
                button.Init();
                button.OnButtonClick += AddButtonClick;
            }
        }

        private void AddButtonClick()
        {
            var newRule = EditObject.AddRule();
            AddRulePanel(newRule);
        }
        public void DeleteRule(RulePanel rulePanel)
        {
            EditObject.RemoveRule(rulePanel.Rule);
            SettingsPanel.RemoveUIComponent(rulePanel);
            Destroy(rulePanel);
        }
        public void SelectRuleEdge(MarkupLineSelectPropertyPanel selectPanel)
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
        public override void OnPrimaryMouseClicked(Event e, out bool isDone)
        {
            if (IsHoverRuleEdgeBounds)
            {
                SelectRuleEdgePanel.SelectedObject = HoverRuleEdgeBounds.LineRawRuleEdge;
                isDone = true;
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
                    NodeMarkupTool.RenderManager.OverlayEffect.DrawCircle(cameraInfo, Markup.OverlayColors[0], bounds.Position, 0.5f, -1f, 1280f, false, true);

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
        public override void EndEditorAction()
        {
            if (IsSelectRuleEdgeMode)
            {
                SelectRuleEdgePanel.eventLeaveFocus -= SelectPanelLeaveFocus;
                SelectRuleEdgePanel.eventLostFocus -= SelectPanelLeaveFocus;
                SelectRuleEdgePanel = null;
            }
        }
    }

    public class LineItem : EditableItem<MarkupLine, UIPanel> { }

    public class RulePanel : UIPanel
    {
        private LinesEditor Editor { get; set; }
        public MarkupLineRawRule Rule { get; private set; }


        private List<UIComponent> StyleProperties { get; } = new List<UIComponent>();

        public RulePanel()
        {
            atlas = TextureUtil.GetAtlas("Ingame");
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

            var manyRules = Editor.RuleEdges.Count > 2;
            AddHeader(manyRules);
            if (manyRules)
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
        private void AddHeader(bool isDeletable)
        {
            var header = AddUIComponent<RuleHeaderPanel>();
            header.AddRange(Settings.Templates);
            header.Init(isDeletable);
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
            var fromProperty = AddUIComponent<MarkupLineSelectPropertyPanel>();
            fromProperty.Text = "From";
            fromProperty.Init();
            fromProperty.AddRange(Editor.RuleEdges);
            fromProperty.SelectedObject = Rule.From;
            fromProperty.OnSelectChanged += FromChanged;
            fromProperty.OnSelect += Editor.SelectRuleEdge;
            fromProperty.OnHover += Editor.HoverRuleEdge;
            fromProperty.OnLeave += Editor.LeaveRuleEdge;
        }

        private void AddToProperty()
        {
            var toProperty = AddUIComponent<MarkupLineSelectPropertyPanel>();
            toProperty.Text = "To";
            toProperty.Init();
            toProperty.AddRange(Editor.RuleEdges);
            toProperty.SelectedObject = Rule.To;
            toProperty.OnSelectChanged += ToChanged;
            toProperty.OnSelect += Editor.SelectRuleEdge;
            toProperty.OnHover += Editor.HoverRuleEdge;
            toProperty.OnLeave += Editor.LeaveRuleEdge;
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
                dashLengthProperty.Text = "Dashed lenght";
                dashLengthProperty.UseWheel = true;
                dashLengthProperty.Step = 0.1f;
                dashLengthProperty.Init();
                dashLengthProperty.Value = dashedStyle.DashLength;
                dashLengthProperty.OnValueChanged += DashLengthChanged;
                StyleProperties.Add(dashLengthProperty);

                var spaceLengthProperty = AddUIComponent<FloatPropertyPanel>();
                spaceLengthProperty.Text = "Space lenght";
                spaceLengthProperty.UseWheel = true;
                spaceLengthProperty.Step = 0.1f;
                spaceLengthProperty.Init();
                spaceLengthProperty.Value = dashedStyle.SpaceLength;
                spaceLengthProperty.OnValueChanged += SpaceLengthChanged;
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
                StyleProperties.Add(offsetProperty);
            }
        }
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
            var template = Settings.AddTemplate(Rule.Style);
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
            var newStyle = LineStyle.GetDefault(style);
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
