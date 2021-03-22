using ColossalFramework;
using ColossalFramework.UI;
using ModsCommon.UI;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class RulePanel : PropertyGroupPanel
    {
        public event Action<RulePanel, UIMouseEventParameter> OnHover;
        public event Action<RulePanel, UIMouseEventParameter> OnEnter;

        private LinesEditor Editor { get; set; }
        private MarkupLine Line => Editor.EditObject;
        public MarkupLineRawRule Rule { get; private set; }

        private StyleHeaderPanel Header { get; set; }
        private ErrorTextProperty Error { get; set; }
        private WarningTextProperty Warning { get; set; }
        public RuleEdgeSelectPropertyPanel From { get; private set; }
        public RuleEdgeSelectPropertyPanel To { get; private set; }
        public StylePropertyPanel Style { get; private set; }

        private List<EditorItem> StyleProperties { get; set; } = new List<EditorItem>();

        public RulePanel() { }
        public void Init(LinesEditor editor, MarkupLineRawRule rule)
        {
            Editor = editor;
            Rule = rule;

            StopLayout();

            AddHeader();
            AddError();
            AddWarning();

            From = AddEdgeProperty(EdgePosition.Start, NodeMarkup.Localize.LineRule_From);
            To = AddEdgeProperty(EdgePosition.End, NodeMarkup.Localize.LineRule_To);

            Refresh();

            AddStyleTypeProperty();
            AddStyleProperties();

            StartLayout();

            base.Init();
        }
        public override void DeInit()
        {
            base.DeInit();

            Header = null;
            Error = null;
            Warning = null;
            From = null;
            To = null;
            Style = null;
            StyleProperties.Clear();

            Editor = null;
            Rule = null;

            OnHover = null;
            OnEnter = null;
        }
        private void AddHeader()
        {
            Header = ComponentPool.Get<StyleHeaderPanel>(this);
            Header.Init(Rule.Style.Value.Type, OnSelectTemplate, Line.IsSupportRules);
            Header.OnDelete += () => Editor.DeleteRule(this);
            Header.OnSaveTemplate += OnSaveTemplate;
            Header.OnCopy += CopyStyle;
            Header.OnPaste += PasteStyle;
        }

        private void AddError()
        {
            Error = ComponentPool.Get<ErrorTextProperty>(this);
            Error.Text = NodeMarkup.Localize.LineEditor_RuleOverlappedWarning;
            Error.Init();
        }
        private void AddWarning()
        {
            Warning = ComponentPool.Get<WarningTextProperty>(this);
            Warning.Text = Line.IsSupportRules ? NodeMarkup.Localize.LineEditor_RulesWarning : NodeMarkup.Localize.LineEditor_NotSupportRules;
            Warning.Init();
        }

        private RuleEdgeSelectPropertyPanel AddEdgeProperty(EdgePosition position, string text)
        {
            var edgeProperty = ComponentPool.Get<RuleEdgeSelectPropertyPanel>(this);
            edgeProperty.Text = text;
            edgeProperty.Position = position;
            edgeProperty.Init();
            edgeProperty.OnSelect += OnSelectPanel;
            edgeProperty.OnHover += Editor.HoverRuleEdge;
            edgeProperty.OnLeave += Editor.LeaveRuleEdge;
            return edgeProperty;
        }
        private void OnSelectPanel(RuleEdgeSelectPropertyPanel panel) => Editor.SelectRuleEdge(panel);

        private void FillEdges()
        {
            FillEdge(From, FromChanged, Rule.From);
            FillEdge(To, ToChanged, Rule.To);
            Warning.isVisible = Settings.ShowPanelTip && !Editor.CanDivide;
        }
        private void FillEdge(RuleEdgeSelectPropertyPanel panel, Action<ILinePartEdge> action, ILinePartEdge value)
        {
            if (panel == null)
                return;

            panel.OnValueChanged -= action;
            panel.Clear();
            panel.AddRange(Editor.SupportPoints);
            panel.Value = value;

            if (Settings.ShowPanelTip && Line.IsSupportRules)
            {
                panel.isVisible = true;
                panel.EnableControl = Editor.CanDivide;
            }
            else
            {
                panel.EnableControl = true;
                panel.isVisible = Editor.CanDivide;
            }

            panel.OnValueChanged += action;
        }
        private void AddStyleTypeProperty()
        {
            switch (Line)
            {
                case MarkupRegularLine regularLine:
                    Style = ComponentPool.Get<RegularStylePropertyPanel>(this);
                    break;
                case MarkupStopLine stopLine:
                    Style = ComponentPool.Get<StopStylePropertyPanel>(this);
                    break;
                default:
                    return;
            }

            Style.Text = NodeMarkup.Localize.Editor_Style;
            Style.Init();
            Style.SelectedObject = Rule.Style.Value.Type;
            Style.OnSelectObjectChanged += StyleChanged;
        }
        private void AddStyleProperties()
        {
            StyleProperties = Rule.Style.Value.GetUIComponents(Rule.Line, this);
            if (StyleProperties.OfType<ColorPropertyPanel>().FirstOrDefault() is ColorPropertyPanel colorProperty)
                colorProperty.OnValueChanged += (Color32 c) => Editor.RefreshSelectedItem();
        }

        private void ClearStyleProperties()
        {
            foreach (var property in StyleProperties)
                ComponentPool.Free(property);

            StyleProperties.Clear();
        }

        private void OnSaveTemplate()
        {
            if (TemplateManager.StyleManager.AddTemplate(Rule.Style, out StyleTemplate template))
                Editor.Panel.EditStyleTemplate(template);
        }
        private void ApplyStyle(LineStyle style)
        {
            Rule.Style.Value = style.CopyStyle();
            Style.SelectedObject = Rule.Style.Value.Type;

            AfterStyleChanged();
        }
        private void OnSelectTemplate(StyleTemplate template)
        {
            if (template.Style is LineStyle style)
                ApplyStyle(style);
        }
        private void CopyStyle() => NodeMarkupTool.ToStyleBuffer(Rule.Style.Value.Type.GetGroup(), Rule.Style.Value);
        private void PasteStyle()
        {
            if (NodeMarkupTool.FromStyleBuffer<LineStyle>(Rule.Style.Value.Type.GetGroup(), out var style))
                ApplyStyle(style);
        }
        private void FromChanged(ILinePartEdge from) => Rule.From = from;
        private void ToChanged(ILinePartEdge to) => Rule.To = to;
        private void StyleChanged(Style.StyleType style)
        {
            if (style == Rule.Style.Value.Type)
                return;

            var newStyle = TemplateManager.StyleManager.GetDefault<LineStyle>(style);
            Rule.Style.Value.CopyTo(newStyle);
            Rule.Style.Value = newStyle;

            AfterStyleChanged();
        }
        private void AfterStyleChanged()
        {
            Editor.RefreshSelectedItem();
            StopLayout();
            ClearStyleProperties();
            AddStyleProperties();
            StartLayout();
        }
        public void Refresh()
        {
            Error.isVisible = Rule.IsOverlapped;
            FillEdges();
        }
        protected override void OnMouseEnter(UIMouseEventParameter p)
        {
            base.OnMouseEnter(p);
            OnHover?.Invoke(this, p);
        }
        protected override void OnMouseLeave(UIMouseEventParameter p)
        {
            base.OnMouseLeave(p);
            OnEnter?.Invoke(this, p);
        }
    }
}
