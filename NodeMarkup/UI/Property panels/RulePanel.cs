using ColossalFramework.UI;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class RulePanel : UIPanel
    {
        //public event Action<RulePanel> OnHover;
        //public event Action<RulePanel> OnLeave;

        private LinesEditor Editor { get; set; }
        public MarkupLineRawRule Rule { get; private set; }

        public MarkupLineSelectPropertyPanel From { get; private set; }
        public MarkupLineSelectPropertyPanel To { get; private set; }
        public StylePropertyPanel Style { get; private set; }
        //public UIPanel HoverPanel { get; private set; }

        private List<UIComponent> StyleProperties { get; set; } = new List<UIComponent>();

        public RulePanel()
        {
            atlas = NodeMarkupPanel.InGameAtlas;
            backgroundSprite = "AssetEditorItemBackground";
            autoLayout = true;
            autoFitChildrenVertically = true;
            autoLayoutDirection = LayoutDirection.Vertical;
            autoLayoutPadding = new RectOffset(5, 5, 0, 0);

            //HoverPanel = AddUIComponent<UIPanel>();
            //HoverPanel.isVisible = false;
            //HoverPanel.relativePosition = new Vector2(0, 0);
            //HoverPanel.eventMouseHover += (UIComponent component, UIMouseEventParameter eventParam) => OnHover?.Invoke(this);
            //HoverPanel.eventMouseLeave += (UIComponent component, UIMouseEventParameter eventParam) => OnLeave?.Invoke(this);
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
            var header = AddUIComponent<StyleHeaderPanel>();
            header.AddRange(TemplateManager.GetTemplates(Rule.Style.Type));
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
            To.AddRange(Editor.SupportPoints);
            To.SelectedObject = Rule.To;
            To.OnSelectChanged += ToChanged;
            To.OnSelect += (panel) => Editor.SelectRuleEdge(panel);
            To.OnHover += Editor.HoverRuleEdge;
            To.OnLeave += Editor.LeaveRuleEdge;
        }
        private void AddStyleTypeProperty()
        {
            switch (Rule.Style.Type & Manager.Style.StyleType.GroupMask)
            {
                case Manager.Style.StyleType.RegularLine:
                    Style = AddUIComponent<RegularStylePropertyPanel>();
                    break;
                case Manager.Style.StyleType.StopLine:
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
        private void OnSelectTemplate(StyleTemplate template)
        {
            if (template.Style.Copy() is LineStyle style)
            {
                Rule.Style = style;
                Style.SelectedObject = Rule.Style.Type;

                Editor.RefreshItem();
                ClearStyleProperties();
                AddStyleProperties();
            }
        }

        private void FromChanged(ILinePartEdge from) => Rule.From = from;
        private void ToChanged(ILinePartEdge to) => Rule.To = to;
        private void StyleChanged(Style.StyleType style)
        {
            if (style == Rule.Style.Type)
                return;

            var newStyle = TemplateManager.GetDefault<LineStyle>(style);
            newStyle.Color = Rule.Style.Color;
            newStyle.Width = Rule.Style.Width;
            if (newStyle is IDashedLine newDashed && Rule.Style is IDashedLine oldDashed)
            {
                newDashed.DashLength = oldDashed.DashLength;
                newDashed.SpaceLength = oldDashed.SpaceLength;
            }
            if (newStyle is IDoubleLine newDouble && Rule.Style is IDoubleLine oldDouble)
                newDouble.Offset = oldDouble.Offset;

            Rule.Style = newStyle;

            Editor.RefreshItem();
            ClearStyleProperties();
            AddStyleProperties();
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();

            foreach (var item in components)
            {
                item.width = width - autoLayoutPadding.horizontal;
            }
            //if(HoverPanel != null)
            //    HoverPanel.size = size;
        }
    }
}
