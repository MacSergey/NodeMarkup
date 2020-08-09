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
    public class RulePanel : UIPanel
    {
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
            header.Init(Rule.Style.Type, Editor.EditObject.SupportRules);
            header.OnDelete += () => Editor.DeleteRule(this);
            header.OnSaveTemplate += OnSaveTemplate;
            header.OnSelectTemplate += OnSelectTemplate;
            header.OnCopy += CopyStyle;
            header.OnPaste += PasteStyle;
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
            switch (Editor.EditObject)
            {
                case MarkupRegularLine regularLine:
                    Style = AddUIComponent<RegularStylePropertyPanel>();
                    break;
                case MarkupStopLine stopLine:
                    Style = AddUIComponent<StopStylePropertyPanel>();
                    break;
                case MarkupCrosswalk crosswalk:
                    Style = AddUIComponent<CrosswalkPropertyPanel>();
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
            if (EarlyAccess.CheckFunctionAccess(NodeMarkup.Localize.EarlyAccess_Function_CopyStyle))
                Buffer = Rule.Style.CopyLineStyle();
        }
        private void PasteStyle()
        {
            if(EarlyAccess.CheckFunctionAccess(NodeMarkup.Localize.EarlyAccess_Function_PasteStyle) && Buffer is LineStyle style)
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
