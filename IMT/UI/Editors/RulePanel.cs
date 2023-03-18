using ColossalFramework.UI;
using IMT.Manager;
using IMT.UI.Panel;
using IMT.Utilities;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace IMT.UI.Editors
{
    public class RulePanel : PropertyGroupPanel, IPropertyContainer
    {
        public event Action<RulePanel, UIMouseEventParameter> OnEnter;
        public event Action<RulePanel, UIMouseEventParameter> OnLeave;

        private bool isExpand;
        public bool IsExpand
        {
            get => isExpand;
            set
            {
                isExpand = value;
                Refresh();
            }
        }
        private LinesEditor Editor { get; set; }
        public IntersectionMarkingToolPanel Panel => Editor.Panel;
        private MarkingLine Line => Editor.EditObject;
        public MarkingLineRawRule Rule { get; private set; }

        private RuleHeaderPanel Header { get; set; }
        private ErrorTextProperty Error { get; set; }
        private WarningTextProperty Warning { get; set; }
        public RuleEdgeSelectPropertyPanel From { get; private set; }
        public RuleEdgeSelectPropertyPanel To { get; private set; }
        public StylePropertyPanel Style { get; private set; }

        object IPropertyEditor.EditObject => Rule;
        bool IPropertyEditor.IsTemplate => false;
        UIAutoLayoutPanel IPropertyContainer.MainPanel => this;
        Style IPropertyContainer.Style => Rule.Style;
        Dictionary<string, bool> IPropertyContainer.ExpandList { get; } = new Dictionary<string, bool>();

        Dictionary<string, IPropertyCategoryInfo> IPropertyContainer.CategoryInfos { get; } = new Dictionary<string, IPropertyCategoryInfo>();
        Dictionary<string, List<IPropertyInfo>> IPropertyContainer.PropertyInfos { get; } = new Dictionary<string, List<IPropertyInfo>>();
        Dictionary<string, CategoryItem> IPropertyContainer.CategoryItems { get; } = new Dictionary<string, CategoryItem>();
        List<EditorItem> IPropertyContainer.StyleProperties { get; } = new List<EditorItem>();

        public RulePanel() { }
        public void Init(LinesEditor editor, MarkingLineRawRule rule, bool isExpand)
        {
            Editor = editor;
            Rule = rule;
            this.isExpand = isExpand;

            PauseLayout(() =>
            {
                AddHeader();
                AddError();
                AddWarning();

                From = AddEdgeProperty(EdgePosition.Start, nameof(From), IMT.Localize.LineRule_From);
                To = AddEdgeProperty(EdgePosition.End, nameof(To), IMT.Localize.LineRule_To);

                AddStyleTypeProperty();
                AddStyleProperties();

                Refresh();
            });

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

            Editor = null;
            Rule = null;

            OnEnter = null;
            OnLeave = null;

            isExpand = false;

            (this as IPropertyContainer).ExpandList.Clear();
        }
        private void AddHeader()
        {
            Header = ComponentPool.Get<RuleHeaderPanel>(this, nameof(Header));
            Header.Init(this, Rule.Style.Value.Type, Line.IsSupportRules);
            Header.OnDelete += () => Editor.DeleteRule(this);
            Header.OnSaveTemplate += OnSaveTemplate;
            Header.OnCopy += CopyStyle;
            Header.OnPaste += PasteStyle;
            Header.OnReset += ResetStyle;
            Header.OnApplyAllRules += ApplyStyleToAllRules;
            Header.OnApplySameStyle += ApplyStyleSameStyle;
            Header.OnApplySameType += ApplyStyleSameType;
            Header.OnExpand += Expand;
            Header.OnSelectTemplate += OnSelectTemplate;
        }

        private void Expand()
        {
            if (Utility.ShiftIsPressed)
                Editor.ExpandRules(!IsExpand);
            else
                IsExpand = !IsExpand;
        }

        private void AddError()
        {
            Error = ComponentPool.Get<ErrorTextProperty>(this, nameof(Error));
            Error.Text = IMT.Localize.LineEditor_RuleOverlappedWarning;
            Error.Init();
        }
        private void AddWarning()
        {
            Warning = ComponentPool.Get<WarningTextProperty>(this, nameof(Warning));
            Warning.Text = Line.IsSupportRules ? IMT.Localize.LineEditor_RulesWarning : IMT.Localize.LineEditor_NotSupportRules;
            Warning.Init();
        }

        private RuleEdgeSelectPropertyPanel AddEdgeProperty(EdgePosition position, string name, string text)
        {
            var edgeProperty = ComponentPool.Get<RuleEdgeSelectPropertyPanel>(this, name);
            edgeProperty.Label = text;
            edgeProperty.Selector.Position = position;
            edgeProperty.Init();
            edgeProperty.OnSelect += OnSelectPanel;
            edgeProperty.OnEnter += Editor.EnterRuleEdge;
            edgeProperty.OnLeave += Editor.LeaveRuleEdge;
            return edgeProperty;
        }
        private void OnSelectPanel(RuleEdgeSelectPropertyPanel.RuleEdgeSelectButton button) => Editor.SelectRuleEdge(button);

        private void FillEdge(RuleEdgeSelectPropertyPanel panel, Action<ILinePartEdge> action, ILinePartEdge value)
        {
            if (panel == null || !panel.isVisible)
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
                case MarkingRegularLine regularLine:
                    Style = ComponentPool.Get<RegularStylePropertyPanel>(this, nameof(Style));
                    break;
                case MarkingStopLine stopLine:
                    Style = ComponentPool.Get<StopStylePropertyPanel>(this, nameof(Style));
                    break;
                default:
                    return;
            }

            Style.Label = IMT.Localize.Editor_Style;
            Style.Init(StyleSelector);
            Style.UseWheel = true;
            Style.WheelTip = true;
            Style.SelectedObject = Rule.Style.Value.Type;
            Style.OnSelectObjectChanged += StyleChanged;
        }
        private bool StyleSelector(Style.StyleType styleType)
        {
            var networkType = styleType.GetNetworkType();
            var lineType = styleType.GetLineType();
            return (Line.PointPair.NetworkType & networkType) != 0 && (Line.PointPair.LineType & lineType) != 0;
        }

        private void AddStyleProperties()
        {
            this.AddProperties();

            foreach (var property in (this as IPropertyContainer).StyleProperties)
            {
                if (property is ColorPropertyPanel colorProperty && colorProperty.name == nameof(Manager.Style.Color))
                    colorProperty.OnValueChanged += (Color32 c) => Editor.RefreshSelectedItem();
            }
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
            {
                ApplyStyle(style);
                IsExpand = true;
            }
        }
        private void CopyStyle() => Editor.Tool.ToStyleBuffer(Rule.Style.Value.Type.GetGroup(), Rule.Style.Value);
        private void PasteStyle()
        {
            if (Editor.Tool.FromStyleBuffer<LineStyle>(Rule.Style.Value.Type.GetGroup(), out var style))
            {
                ApplyStyle(style);
                IsExpand = true;
            }
        }
        private void ResetStyle()
        {
            ApplyStyle(Manager.Style.GetDefault<LineStyle>(Rule.Style.Value.Type));
            IsExpand = true;
        }
        private void ApplyStyleToAllRules()
        {
            foreach (var rulePanel in Editor.RulePanels)
            {
                if (rulePanel != this)
                    rulePanel.ApplyStyle(Rule.Style.Value);
            }
        }
        private void ApplyStyleSameStyle()
        {
            foreach (var line in Editor.Marking.Lines)
            {
                if (line == Line)
                    continue;

                foreach (var rule in line.Rules)
                {
                    if (rule.Style.Value.Type == Rule.Style.Value.Type)
                        rule.Style.Value = Rule.Style.Value.CopyStyle();
                }
            }

            foreach (var rulePanel in Editor.RulePanels)
            {
                if (rulePanel != this && rulePanel.Rule.Style.Value.Type == Rule.Style.Value.Type)
                    rulePanel.ApplyStyle(Rule.Style.Value);
            }

            Editor.RefreshEditor();
            Editor.ItemsPanel.RefreshItems();
        }
        private void ApplyStyleSameType()
        {
            var group = Rule.Style.Value.Type.GetGroup();
            foreach (var line in Editor.Marking.Lines)
            {
                if (line == Line)
                    continue;

                foreach (var rule in line.Rules)
                {
                    if (rule.Style.Value.Type.GetGroup() == group)
                        rule.Style.Value = Rule.Style.Value.CopyStyle();
                }
            }

            foreach (var rulePanel in Editor.RulePanels)
            {
                if (rulePanel != this)
                    rulePanel.ApplyStyle(Rule.Style.Value);
            }

            Editor.RefreshEditor();
            Editor.ItemsPanel.RefreshItems();
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
            AddStyleProperties();
            Header.StyleType = Rule.Style.Value.Type;
        }
        public void Refresh()
        {
            PauseLayout(() =>
            {
                var error = Rule.IsOverlapped;
                color = !IsExpand && error ? CommonColors.Error : NormalColor;
                Header.IsExpand = IsExpand;
                Error.isVisible = IsExpand && error;
                Warning.isVisible = IsExpand && Settings.ShowPanelTip && !Editor.CanDivide;
                From.isVisible = IsExpand;
                To.isVisible = IsExpand;
                Style.isVisible = IsExpand;

                foreach (var category in (this as IPropertyContainer).CategoryItems.Values)
                    category.isVisible = IsExpand;
            });

            FillEdge(From, FromChanged, Rule.From);
            FillEdge(To, ToChanged, Rule.To);

            PropertyEditorHelper.RefreshProperties(this);
        }
        void IPropertyEditor.RefreshProperties() => (Editor as IPropertyEditor).RefreshProperties();
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
