using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class RulePanel : PropertyGroupPanel
    {
        public event Action<RulePanel, UIMouseEventParameter> OnEnter;
        public event Action<RulePanel, UIMouseEventParameter> OnLeave;

        private LinesEditor Editor { get; set; }
        private MarkupLine Line => Editor.EditObject;
        public MarkupLineRawRule Rule { get; private set; }

        private StyleHeaderPanel Header { get; set; }
        private ErrorTextProperty Error { get; set; }
        private WarningTextProperty Warning { get; set; }
        public RuleEdgeSelectPropertyPanel From { get; private set; }
        public RuleEdgeSelectPropertyPanel To { get; private set; }
        public StylePropertyPanel Style { get; private set; }
        private MoreOptionsPanel MoreOptionsButton { get; set; }
        private bool ShowMoreOptions { get; set; }

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

            From = AddEdgeProperty(EdgePosition.Start, nameof(From), NodeMarkup.Localize.LineRule_From);
            To = AddEdgeProperty(EdgePosition.End, nameof(To), NodeMarkup.Localize.LineRule_To);

            Refresh();

            AddStyleTypeProperty();
            AddMoreOptions();
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
            MoreOptionsButton = null;
            ShowMoreOptions = false;
            StyleProperties.Clear();

            Editor = null;
            Rule = null;

            OnEnter = null;
            OnLeave = null;
        }
        private void AddHeader()
        {
            Header = ComponentPool.Get<StyleHeaderPanel>(this, nameof(Header));
            Header.Init(Rule.Style.Value.Type, OnSelectTemplate, Line.IsSupportRules);
            Header.OnDelete += () => Editor.DeleteRule(this);
            Header.OnSaveTemplate += OnSaveTemplate;
            Header.OnCopy += CopyStyle;
            Header.OnPaste += PasteStyle;
        }

        private void AddError()
        {
            Error = ComponentPool.Get<ErrorTextProperty>(this, nameof(Error));
            Error.Text = NodeMarkup.Localize.LineEditor_RuleOverlappedWarning;
            Error.Init();
        }
        private void AddWarning()
        {
            Warning = ComponentPool.Get<WarningTextProperty>(this, nameof(Warning));
            Warning.Text = Line.IsSupportRules ? NodeMarkup.Localize.LineEditor_RulesWarning : NodeMarkup.Localize.LineEditor_NotSupportRules;
            Warning.Init();
        }

        private RuleEdgeSelectPropertyPanel AddEdgeProperty(EdgePosition position, string name, string text)
        {
            var edgeProperty = ComponentPool.Get<RuleEdgeSelectPropertyPanel>(this, name);
            edgeProperty.Text = text;
            edgeProperty.Selector.Position = position;
            edgeProperty.Init();
            edgeProperty.OnSelect += OnSelectPanel;
            edgeProperty.OnEnter += Editor.EnterRuleEdge;
            edgeProperty.OnLeave += Editor.LeaveRuleEdge;
            return edgeProperty;
        }
        private void OnSelectPanel(RuleEdgeSelectPropertyPanel.RuleEdgeSelectButton button) => Editor.SelectRuleEdge(button);

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
            panel.Selector.Clear();
            panel.Selector.AddRange(Editor.SupportPoints);
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
                    Style = ComponentPool.Get<RegularStylePropertyPanel>(this, nameof(Style));
                    break;
                case MarkupStopLine stopLine:
                    Style = ComponentPool.Get<StopStylePropertyPanel>(this, nameof(Style));
                    break;
                default:
                    return;
            }

            Style.Text = NodeMarkup.Localize.Editor_Style;
            Style.Init(StyleSelector);
            Style.UseWheel = true;
            Style.WheelTip = true;
            Style.SelectedObject = Rule.Style.Value.Type;
            Style.OnSelectObjectChanged += StyleChanged;
        }
        private bool StyleSelector(Style.StyleType styleType)
        {
            var type = styleType.GetNetworkType();
            return (Line.PointPair.NetworkType & type) != 0;
        }
        private void AddMoreOptions()
        {
            MoreOptionsButton = ComponentPool.Get<MoreOptionsPanel>(this, nameof(MoreOptionsButton));
            MoreOptionsButton.Init();
            MoreOptionsButton.OnButtonClick += () =>
            {
                ShowMoreOptions = !ShowMoreOptions;
                SetOptionsCollapse();
            };
        }
        private void SetOptionsCollapse()
        {
            MoreOptionsButton.Text = ShowMoreOptions ? $"▲ {NodeMarkup.Localize.Editor_LessOptions} ▲" : $"▼ {NodeMarkup.Localize.Editor_MoreOptions} ▼";

            foreach (var option in StyleProperties)
                option.IsCollapsed = !ShowMoreOptions;
        }

        private void AddStyleProperties()
        {
            var startIndex = childCount;
            var style = Rule.Style.Value;
            StyleProperties = style.GetUIComponents(Rule.Line, this);
            StyleProperties.Sort((x, y) => style.GetUIComponentSortIndex(x) - style.GetUIComponentSortIndex(y));
            for (int i = 0; i < StyleProperties.Count; i += 1)
                StyleProperties[i].zOrder = startIndex + i;

            if (StyleProperties.OfType<ColorPropertyPanel>().FirstOrDefault() is ColorPropertyPanel colorProperty)
                colorProperty.OnValueChanged += (Color32 c) => Editor.RefreshSelectedItem();

            if(Settings.CollapseOptions && StyleProperties.Count(p => p.CanCollapse) >= 2)
            {
                MoreOptionsButton.isVisible = true;
                MoreOptionsButton.BringToFront();
                SetOptionsCollapse();
            }
            else
                MoreOptionsButton.isVisible = false;
        }

        private void ClearStyleProperties()
        {
            foreach (var property in StyleProperties)
                ComponentPool.Free(property);

            StyleProperties.Clear();
        }

        private void OnSaveTemplate()
        {
            if (SingletonManager<StyleTemplateManager>.Instance.AddTemplate(Rule.Style, out StyleTemplate template))
                Editor.Panel.EditStyleTemplate(template);
        }
        public void ApplyStyle(LineStyle style)
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
        private void CopyStyle() => Editor.Tool.ToStyleBuffer(Rule.Style.Value.Type.GetGroup(), Rule.Style.Value);
        private void PasteStyle()
        {
            if (Editor.Tool.FromStyleBuffer<LineStyle>(Rule.Style.Value.Type.GetGroup(), out var style))
                ApplyStyle(style);
        }
        private void FromChanged(ILinePartEdge from) => Rule.From = from;
        private void ToChanged(ILinePartEdge to) => Rule.To = to;
        private void StyleChanged(Style.StyleType style)
        {
            if (style == Rule.Style.Value.Type)
                return;

            var newStyle = SingletonManager<StyleTemplateManager>.Instance.GetDefault<LineStyle>(style);
            Rule.Style.Value.CopyTo(newStyle);
            Rule.Style.Value = newStyle;

            AfterStyleChanged();
        }
        private void AfterStyleChanged()
        {
            Editor.RefreshEditor();
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
            OnEnter?.Invoke(this, p);
        }
        protected override void OnMouseLeave(UIMouseEventParameter p)
        {
            base.OnMouseLeave(p);
            OnLeave?.Invoke(this, p);
        }
    }
}
