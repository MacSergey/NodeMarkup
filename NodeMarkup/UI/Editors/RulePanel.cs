using ColossalFramework;
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
    public class RulePanel : PropertyGroupPanel
    {
        public event Action<RulePanel, UIMouseEventParameter> OnHover;
        public event Action<RulePanel, UIMouseEventParameter> OnEnter;

        protected override Color32 Color => new Color32(90, 123, 135, 255);

        private static LineStyle Buffer { get; set; }
        private LinesEditor Editor { get; set; }
        public MarkupLineRawRule Rule { get; private set; }

        private StyleHeaderPanel Header { get; set; }
        private ErrorTextProperty Error { get; set; }
        private WarningTextProperty Warning { get; set; }
        public MarkupLineSelectPropertyPanel From { get; private set; }
        public MarkupLineSelectPropertyPanel To { get; private set; }
        public StylePropertyPanel Style { get; private set; }

        private List<UIComponent> StyleProperties { get; set; } = new List<UIComponent>();

        public RulePanel() { }
        public void Init(LinesEditor editor, MarkupLineRawRule rule)
        {
            Editor = editor;
            Rule = rule;

            AddHeader();
            AddError();
            From = AddEdgeProperty(EdgePosition.Start, NodeMarkup.Localize.LineRule_From);
            To = AddEdgeProperty(EdgePosition.End, NodeMarkup.Localize.LineRule_To);
            AddWarning();

            Refresh();

            AddStyleTypeProperty();
            AddStyleProperties();

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
            Header.Init(Rule.Style.Type, OnSelectTemplate, Editor.SupportRules);
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
            Warning.Text = NodeMarkup.Localize.LineEditor_RulesWarning;
            Warning.Init();
        }

        private MarkupLineSelectPropertyPanel AddEdgeProperty(EdgePosition position, string text)
        {
            var edgeProperty = ComponentPool.Get<MarkupLineSelectPropertyPanel>(this);
            edgeProperty.Text = text;
            edgeProperty.Position = position;
            edgeProperty.Init();
            edgeProperty.OnSelect += OnSelectPanel;
            edgeProperty.OnHover += Editor.HoverRuleEdge;
            edgeProperty.OnLeave += Editor.LeaveRuleEdge;
            return edgeProperty;
        }
        private void OnSelectPanel(MarkupLineSelectPropertyPanel panel) => Editor.SelectRuleEdge(panel);

        private void FillEdges()
        {
            FillEdge(From, FromChanged, Rule.From);
            FillEdge(To, ToChanged, Rule.To);
            Warning.isVisible = Settings.ShowPanelTip && !Editor.CanDivide;
        }
        private void FillEdge(MarkupLineSelectPropertyPanel panel, Action<ILinePartEdge> action, ILinePartEdge value)
        {
            if (panel == null)
                return;

            panel.OnSelectChanged -= action;
            panel.Clear();
            panel.AddRange(Editor.SupportPoints);
            panel.SelectedObject = value;

            if (Settings.ShowPanelTip)
            {
                panel.isVisible = true;
                panel.EnableControl = Editor.CanDivide;
            }
            else
            {
                panel.EnableControl = true;
                panel.isVisible = Editor.CanDivide;
            }

            panel.OnSelectChanged += action;
        }
        private void AddStyleTypeProperty()
        {
            switch (Editor.EditObject)
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
                ComponentPool.Free(property);

            StyleProperties.Clear();
        }

        private void OnSaveTemplate()
        {
            if (TemplateManager.StyleManager.AddTemplate(Rule.Style, out StyleTemplate template))
                Editor.NodeMarkupPanel.EditTemplate(template);
        }
        private void ApplyStyle(LineStyle style)
        {
            if ((Rule.Style.Type & Manager.Style.StyleType.GroupMask) != (style.Type & Manager.Style.StyleType.GroupMask))
                return;

            Rule.Style = style.CopyLineStyle();
            Style.SelectedObject = Rule.Style.Type;

            Editor.RefreshItem();
            ClearStyleProperties();
            AddStyleProperties();
        }
        private void OnSelectTemplate(StyleTemplate template)
        {
            if (template.Style is LineStyle style)
                ApplyStyle(style);
        }
        private void CopyStyle() => Buffer = Rule.Style.CopyLineStyle();
        private void PasteStyle()
        {
            if (Buffer is LineStyle style)
                ApplyStyle(style);
        }
        private void FromChanged(ILinePartEdge from) => Rule.From = from;
        private void ToChanged(ILinePartEdge to) => Rule.To = to;
        private void StyleChanged(Style.StyleType style)
        {
            if (style == Rule.Style.Type)
                return;

            var newStyle = TemplateManager.StyleManager.GetDefault<LineStyle>(style);
            Rule.Style.CopyTo(newStyle);

            Rule.Style = newStyle;

            Editor.RefreshItem();
            ClearStyleProperties();
            AddStyleProperties();
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
