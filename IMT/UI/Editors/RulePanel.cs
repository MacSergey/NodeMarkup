using ColossalFramework.UI;
using IMT.Manager;
using IMT.Utilities;
using ModsCommon;
using ModsCommon.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IMT.UI.Editors
{
    public class RulePanel : PropertyGroupPanel, IPropertyEditor
    {
        public event Action<RulePanel, UIMouseEventParameter> OnEnter;
        public event Action<RulePanel, UIMouseEventParameter> OnLeave;

        private LinesEditor Editor { get; set; }
        private MarkingLine Line => Editor.EditObject;
        public MarkingLineRawRule Rule { get; private set; }

        private StyleHeaderPanel Header { get; set; }
        private ErrorTextProperty Error { get; set; }
        private WarningTextProperty Warning { get; set; }
        public RuleEdgeSelectPropertyPanel From { get; private set; }
        public RuleEdgeSelectPropertyPanel To { get; private set; }
        public StylePropertyPanel Style { get; private set; }

        UIAutoLayoutPanel IPropertyEditor.MainPanel => this;
        object IPropertyEditor.EditObject => Rule.Line;
        Style IPropertyEditor.Style => Rule.Style;
        bool IPropertyEditor.IsTemplate => false;

        Dictionary<string, PropertyCategoryInfo> IPropertyEditor.CategoryInfos { get; } = new Dictionary<string, PropertyCategoryInfo>();
        Dictionary<string, List<IPropertyInfo>> IPropertyEditor.PropertyInfos { get; } = new Dictionary<string, List<IPropertyInfo>>();
        Dictionary<string, CategoryItem> IPropertyEditor.CategoryItems { get; } = new Dictionary<string, CategoryItem>();
        List<EditorItem> IPropertyEditor.StyleProperties { get; } = new List<EditorItem>();

        public RulePanel() { }
        public void Init(LinesEditor editor, MarkingLineRawRule rule)
        {
            Editor = editor;
            Rule = rule;

            StopLayout();

            AddHeader();
            AddError();
            AddWarning();

            From = AddEdgeProperty(EdgePosition.Start, nameof(From), IMT.Localize.LineRule_From);
            To = AddEdgeProperty(EdgePosition.End, nameof(To), IMT.Localize.LineRule_To);

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
            Header.OnReset += ResetStyle;
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
                case MarkingRegularLine regularLine:
                    Style = ComponentPool.Get<RegularStylePropertyPanel>(this, nameof(Style));
                    break;
                case MarkingStopLine stopLine:
                    Style = ComponentPool.Get<StopStylePropertyPanel>(this, nameof(Style));
                    break;
                default:
                    return;
            }

            Style.Text = IMT.Localize.Editor_Style;
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

            foreach (var property in (this as IPropertyEditor).StyleProperties)
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
                ApplyStyle(style);
        }
        private void CopyStyle() => Editor.Tool.ToStyleBuffer(Rule.Style.Value.Type.GetGroup(), Rule.Style.Value);
        private void PasteStyle()
        {
            if (Editor.Tool.FromStyleBuffer<LineStyle>(Rule.Style.Value.Type.GetGroup(), out var style))
                ApplyStyle(style);
        }
        private void ResetStyle() => ApplyStyle(Manager.Style.GetDefault<LineStyle>(Rule.Style.Value.Type));

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
            this.ClearProperties();
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
            OnEnter?.Invoke(this, p);
        }
        protected override void OnMouseLeave(UIMouseEventParameter p)
        {
            base.OnMouseLeave(p);
            OnLeave?.Invoke(this, p);
        }
    }
}
