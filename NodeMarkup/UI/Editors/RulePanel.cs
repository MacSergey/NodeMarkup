using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class RulePanel : UIPanel
    {
        private static Color32 NormalColor { get; } = new Color32(90, 123, 135, 255);
        private static Color32 ErrorColor { get; } = new Color32(246, 85, 85, 255);

        private static LineStyle Buffer { get; set; }
        private LinesEditor Editor { get; set; }
        public MarkupLineRawRule Rule { get; private set; }

        public MarkupLineSelectPropertyPanel From { get; private set; }
        public MarkupLineSelectPropertyPanel To { get; private set; }
        public StylePropertyPanel Style { get; private set; }

        private List<UIComponent> StyleProperties { get; set; } = new List<UIComponent>();

        public RulePanel()
        {
            atlas = TextureUtil.InGameAtlas;
            backgroundSprite = "ButtonWhite";
            autoLayout = true;
            autoFitChildrenVertically = true;
            autoLayoutDirection = LayoutDirection.Vertical;
            autoLayoutPadding = new RectOffset(5, 5, 0, 0);
        }
        public void Init(LinesEditor editor, MarkupLineRawRule rule)
        {
            Editor = editor;
            Rule = rule;
            Refresh();

            SetSize();

            AddHeader();
            AddEdgeProperties();
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
            header.Init(Rule.Style.Type, Editor.SupportRules);
            header.OnDelete += () => Editor.DeleteRule(this);
            header.OnSaveTemplate += OnSaveTemplate;
            header.OnSelectTemplate += OnSelectTemplate;
            header.OnCopy += CopyStyle;
            header.OnPaste += PasteStyle;
        }
        private void AddEdgeProperties()
        {
            AddFromProperty();
            AddToProperty();
            FillEdges();
        }
        private void AddFromProperty()
        {
            From = AddUIComponent<MarkupLineSelectPropertyPanel>();
            From.Text = NodeMarkup.Localize.LineEditor_From;
            From.Position = EdgePosition.Start;
            From.Init();
            From.OnSelect += (panel) => Editor.SelectRuleEdge(panel);
            From.OnHover += Editor.HoverRuleEdge;
            From.OnLeave += Editor.LeaveRuleEdge;
        }

        private void AddToProperty()
        {
            To = AddUIComponent<MarkupLineSelectPropertyPanel>();
            To.Text = NodeMarkup.Localize.LineEditor_To;
            To.Position = EdgePosition.End;
            To.Init();
            To.OnSelect += (panel) => Editor.SelectRuleEdge(panel);
            To.OnHover += Editor.HoverRuleEdge;
            To.OnLeave += Editor.LeaveRuleEdge;
        }
        private void FillEdges()
        {
            FillEdge(From, FromChanged, Rule.From);
            FillEdge(To, ToChanged, Rule.To);
        }
        private void FillEdge(MarkupLineSelectPropertyPanel panel, Action<ILinePartEdge> action, ILinePartEdge value)
        {
            if (panel == null)
                return;

            panel.OnSelectChanged -= action;
            panel.Clear();
            panel.AddRange(Editor.SupportPoints);
            panel.SelectedObject = value;
            panel.isVisible = Editor.CanDivide;
            panel.OnSelectChanged += action;
        }
        private void AddStyleTypeProperty()
        {
            switch (Editor.EditObject)
            {
                case MarkupRegularLine regularLine:
                    Style = AddUIComponent<RegularStylePropertyPanel>();
                    break;
                case MarkupStopLine stopLine:
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
        private void CopyStyle()
        {
                Buffer = Rule.Style.CopyLineStyle();
        }
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

            var newStyle = TemplateManager.GetDefault<LineStyle>(style);
            Rule.Style.CopyTo(newStyle);

            Rule.Style = newStyle;

            Editor.RefreshItem();
            ClearStyleProperties();
            AddStyleProperties();
        }
        public void Refresh()
        {
            if (Rule.IsOverlapped)
            {
                color = ErrorColor;
                tooltip = NodeMarkup.Localize.LineEditor_RuleOverlappedWarning;
            }
            else
            {
                color = NormalColor;
                tooltip = string.Empty;
            }

            FillEdges();
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
