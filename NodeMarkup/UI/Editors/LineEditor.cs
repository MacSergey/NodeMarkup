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
            foreach (var rule in EditObject.RawRules)
            {
                var rulePanel = SettingsPanel.AddUIComponent<RulePanel>();
                rulePanel.Init(rule);
            }
        }
    }

    public class LineItem : EditableItem<MarkupLine, UIPanel> { }

    public class RulePanel : UIPanel
    {
        public MarkupLineRawRule Rule { get; private set; }

        private List<UIComponent> StyleProperties { get;} = new List<UIComponent>();

        public RulePanel()
        {
            atlas = TextureUtil.GetAtlas("Ingame");
            backgroundSprite = "AssetEditorItemBackground";
            autoLayout = true;
            autoFitChildrenVertically = true;
            autoLayoutDirection = LayoutDirection.Vertical;
            autoLayoutPadding = new RectOffset(5, 5, 0, 0);
        }

        public void Init(MarkupLineRawRule rule)
        {
            Rule = rule;

            if (parent is UIScrollablePanel scrollablePanel)
                width = scrollablePanel.width - scrollablePanel.autoLayoutPadding.horizontal;
            else if (parent is UIPanel panel)
                width = panel.width - panel.autoLayoutPadding.horizontal;
            else
                width = parent.width;

            var fromProperty = AddUIComponent<MarkupLineListPropertyPanel>();
            fromProperty.Text = "From";
            fromProperty.NullText = "Begin";
            fromProperty.Init();
            fromProperty.AddRange(editob)
            fromProperty.SelectedObject = Rule.From;
            fromProperty.OnSelectObjectChanged += FromChanged; ;

            var toProperty = AddUIComponent<MarkupLineListPropertyPanel>();
            toProperty.Text = "To";
            toProperty.NullText = "End";
            toProperty.Init();
            toProperty.SelectedObject = Rule.To;
            toProperty.OnSelectObjectChanged += ToChanged;

            var colorProperty = AddUIComponent<ColorPropertyPanel>();
            colorProperty.Text = "Color";
            colorProperty.Init();
            colorProperty.Value = Rule.Style.Color;
            colorProperty.OnValueChanged += ColorChanged;

            var styleProperty = AddUIComponent<StylePropertyPanel>();
            styleProperty.Text = "Style";
            styleProperty.Init();
            styleProperty.SelectedObject = rule.Style.LineType;
            styleProperty.OnSelectObjectChanged += StyleChanged;

            FillStyleProperties();
        }
        private void FillStyleProperties()
        {
            if (Rule.Style is IDashedLine dashedStyle)
            {
                var dashLengthProperty = AddUIComponent<FloatPropertyPanel>();
                dashLengthProperty.Text = "Dashed lenght";
                dashLengthProperty.Init();
                dashLengthProperty.Value = dashedStyle.DashLength;
                dashLengthProperty.OnValueChanged += DashLengthChanged;
                StyleProperties.Add(dashLengthProperty);

                var spaceLengthProperty = AddUIComponent<FloatPropertyPanel>();
                spaceLengthProperty.Text = "Space lenght";
                spaceLengthProperty.Init();
                spaceLengthProperty.Value = dashedStyle.SpaceLength;
                spaceLengthProperty.OnValueChanged += SpaceLengthChanged;
                StyleProperties.Add(spaceLengthProperty);
            }
            if (Rule.Style is IDoubleLine doubleStyle)
            {
                var offsetProperty = AddUIComponent<FloatPropertyPanel>();
                offsetProperty.Text = "Offset";
                offsetProperty.Init();
                offsetProperty.Value = doubleStyle.Offset;
                offsetProperty.OnValueChanged += OffsetChanged;
                StyleProperties.Add(offsetProperty);
            }
        }
        private void ClearStyleProperties()
        {
            foreach(var property in StyleProperties)
            {
                RemoveUIComponent(property);
                Destroy(property);
            }

            StyleProperties.Clear();
        }

        private void ColorChanged(Color32 color) => Rule.Style.Color = color;
        private void FromChanged(MarkupLine from) => Rule.From = from;
        private void ToChanged(MarkupLine to) => Rule.To = to;
        private void StyleChanged(LineStyle.Type style)
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
            FillStyleProperties();
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
