using ColossalFramework.Math;
using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class CrosswalksEditor : Editor<CrosswalkItem, MarkupCrosswalk, StyleIcon>
    {
        protected override bool UseGroupPanel => true;

        private static CrosswalkStyle Buffer { get; set; }

        public override string Name => NodeMarkup.Localize.CrosswalkEditor_Crosswalks;
        public override string EmptyMessage => NodeMarkup.Localize.CrosswalkEditor_EmptyMessage;

        private List<EditorItem> StyleProperties { get; set; } = new List<EditorItem>();
        private MarkupCrosswalkSelectPropertyPanel RightBorder { get; set; }
        private MarkupCrosswalkSelectPropertyPanel LeftBorder { get; set; }
        private WarningTextProperty Warning { get; set; }
        private StylePropertyPanel Style { get; set; }
        private CrosswalkBorderToolMode CrosswalkBorderToolMode { get; }

        public MarkupCrosswalkSelectPropertyPanel HoverBorderPanel { get; private set; }
        public bool IsHoverBorderPanel => HoverBorderPanel != null;

        public CrosswalksEditor()
        {
            CrosswalkBorderToolMode = Tool.CreateToolMode<CrosswalkBorderToolMode>();
            CrosswalkBorderToolMode.Init(this);
        }

        protected override void FillItems()
        {
            var crosswalks = Markup.Crosswalks.OrderBy(c => c.Line.Start.Enter).ThenBy(c => c.Line.Start.Num).ToArray();
            foreach (var crosswalk in crosswalks)
                AddItem(crosswalk);
        }
        protected override void OnObjectSelect()
        {
            AddHeader();
            AddWarning();

            AddBordersProperties();
            AddStyleTypeProperty();
            AddStyleProperties();
            if (StyleProperties.OfType<ColorPropertyPanel>().FirstOrDefault() is ColorPropertyPanel colorProperty)
                colorProperty.OnValueChanged += (Color32 c) => RefreshItem();
        }
        protected override void OnClear()
        {
            RightBorder = null;
            LeftBorder = null;
            Warning = null;
            Style = null;

            StyleProperties.Clear();
        }

        #region PROPERTIES PANELS

        private void AddHeader()
        {
            var header = ComponentPool.Get<CrosswalkHeaderPanel>(PropertiesPanel);
            header.Init(EditObject.Style.Type, SelectTemplate, false);
            header.OnSaveTemplate += SaveTemplate;
            header.OnCopy += CopyStyle;
            header.OnPaste += PasteStyle;
            header.OnCut += CutLines;
        }
        private void AddWarning()
        {
            Warning = ComponentPool.Get<WarningTextProperty>(PropertiesPanel);
            Warning.Text = NodeMarkup.Localize.CrosswalkEditor_BordersWarning;
            Warning.Init();
        }
        private void AddBordersProperties()
        {
            LeftBorder = AddBorderProperty(BorderPosition.Left, NodeMarkup.Localize.CrosswalkEditor_LeftBorder);
            RightBorder = AddBorderProperty(BorderPosition.Right, NodeMarkup.Localize.CrosswalkEditor_RightBorder);

            FillBorders();
        }
        private void FillBorders()
        {
            FillBorder(LeftBorder, LeftBorgerChanged, GetBorderLines(BorderPosition.Left), EditObject.LeftBorder);
            FillBorder(RightBorder, RightBorgerChanged, GetBorderLines(BorderPosition.Right), EditObject.RightBorder);

            Warning.isVisible = Settings.ShowPanelTip && (!LeftBorder.EnableControl || !RightBorder.EnableControl);
        }
        private MarkupRegularLine[] GetBorderLines(BorderPosition border)
        {
            var point = border == BorderPosition.Right ? EditObject.Line.Start : EditObject.Line.End;
            if (point.Enter.TryGetPoint(point.Num, MarkupPoint.PointType.Enter, out MarkupPoint enterPoint))
                return enterPoint.Markup.GetPointLines(enterPoint).OfType<MarkupRegularLine>().ToArray();
            else
                return new MarkupRegularLine[0];
        }
        private void FillBorder(MarkupCrosswalkSelectPropertyPanel panel, Action<MarkupRegularLine> action, MarkupRegularLine[] lines, MarkupRegularLine value)
        {
            panel.OnSelectChanged -= action;
            panel.Clear();
            panel.AddRange(lines);
            panel.SelectedObject = value;

            if (Settings.ShowPanelTip)
            {
                panel.isVisible = true;
                panel.EnableControl = lines.Any();
            }
            else
            {
                panel.EnableControl = true;
                panel.isVisible = lines.Any();
            }

            panel.OnSelectChanged += action;
        }
        private MarkupCrosswalkSelectPropertyPanel AddBorderProperty(BorderPosition position, string text)
        {
            var border = ComponentPool.Get<MarkupCrosswalkSelectPropertyPanel>(PropertiesPanel);
            border.Text = text;
            border.Position = position;
            border.Init();
            border.OnSelect += (panel) => SelectBorder(panel);
            border.OnHover += HoverBorder;
            border.OnLeave += LeaveBorder;
            return border;
        }

        private void RightBorgerChanged(MarkupRegularLine line) => EditObject.RightBorder = line;
        private void LeftBorgerChanged(MarkupRegularLine line) => EditObject.LeftBorder = line;

        private void AddStyleTypeProperty()
        {
            Style = ComponentPool.Get<CrosswalkPropertyPanel>(PropertiesPanel);
            Style.Text = NodeMarkup.Localize.Editor_Style;
            Style.Init();
            Style.SelectedObject = EditObject.Style.Type;
            Style.OnSelectObjectChanged += StyleChanged;
        }
        private void AddStyleProperties() => StyleProperties = EditObject.Style.GetUIComponents(EditObject, PropertiesPanel, isTemplate: false);

        #endregion

        private void SaveTemplate()
        {
            if (TemplateManager.StyleManager.AddTemplate(EditObject.Style, out StyleTemplate template))
                Panel.EditStyleTemplate(template);
        }
        private void ApplyStyle(CrosswalkStyle style)
        {
            EditObject.Style = style.CopyCrosswalkStyle();
            Style.SelectedObject = EditObject.Style.Type;

            RefreshItem();
            ClearStyleProperties();
            AddStyleProperties();
        }
        private void SelectTemplate(StyleTemplate template)
        {
            if (template.Style is CrosswalkStyle style)
                ApplyStyle(style);
        }
        private void CopyStyle() => Buffer = EditObject.Style.CopyCrosswalkStyle();
        private void PasteStyle()
        {
            if (Buffer is CrosswalkStyle style)
                ApplyStyle(style);
        }

        private void StyleChanged(Style.StyleType style)
        {
            if (style == EditObject.Style.Type)
                return;

            var newStyle = TemplateManager.StyleManager.GetDefault<CrosswalkStyle>(style);
            EditObject.Style.CopyTo(newStyle);

            EditObject.Style = newStyle;

            RefreshItem();
            ClearStyleProperties();
            AddStyleProperties();
        }
        private void ClearStyleProperties()
        {
            foreach (var property in StyleProperties)
                ComponentPool.Free(property);

            StyleProperties.Clear();
        }
        private void CutLines() => Markup.CutLinesByCrosswalk(EditObject);
        protected override void OnObjectDelete(MarkupCrosswalk crosswalk) => Markup.RemoveCrosswalk(crosswalk);
        protected override void OnObjectUpdate() => FillBorders();
        public void RefreshItem() => SelectItem.Refresh();

        #region EDITOR ACTION

        public void HoverBorder(MarkupCrosswalkSelectPropertyPanel selectPanel) => HoverBorderPanel = selectPanel;
        public void LeaveBorder(MarkupCrosswalkSelectPropertyPanel selectPanel) => HoverBorderPanel = null;

        public bool SelectBorder(MarkupCrosswalkSelectPropertyPanel selectPanel) => SelectBorder(selectPanel, null);
        public bool SelectBorder(MarkupCrosswalkSelectPropertyPanel selectPanel, Func<Event, bool> afterAction)
        {
            if (Tool.Mode == CrosswalkBorderToolMode && selectPanel == CrosswalkBorderToolMode.SelectPanel)
            {
                Tool.SetDefaultMode();
                return true;
            }
            else
            {
                Tool.SetMode(CrosswalkBorderToolMode);
                CrosswalkBorderToolMode.SelectPanel = selectPanel;
                CrosswalkBorderToolMode.AfterSelectPanel = afterAction;
                selectPanel.Focus();
                return false;
            }
        }
        public override void Render(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHoverItem)
                HoverItem.Object.Render(cameraInfo, Colors.Hover);

            if (IsHoverBorderPanel && HoverBorderPanel.SelectedObject is MarkupRegularLine borderLine)
                borderLine.Render(cameraInfo, Colors.Hover);
        }

        public void BorderSetup()
        {
            if (!Settings.QuickBorderSetup)
                return;

            var hasLeft = LeftBorder.Objects.Any();
            var hasRight = RightBorder.Objects.Any();

            if (hasLeft)
                SelectBorder(LeftBorder, hasRight ? (_) => SelectBorder(RightBorder) : (Func<Event, bool>)null);
            else if (hasRight)
                SelectBorder(RightBorder);
        }

        #endregion
    }
    public class CrosswalkBorderToolMode : BasePanelMode<CrosswalksEditor, MarkupCrosswalkSelectPropertyPanel, MarkupRegularLine>
    {
        protected override bool IsHover => IsHoverLine;
        protected override MarkupRegularLine Hover => HoverLine?.Line as MarkupRegularLine;

        private MarkupLineBound[] BorderLines { get; set; }
        private MarkupLineBound HoverLine { get; set; }
        private bool IsHoverLine => HoverLine != null;

        protected override void OnSetPanel() => BorderLines = SelectPanel.Objects.Select(i => new MarkupLineBound(i, 0.5f)).ToArray();

        public override void OnToolUpdate() => HoverLine = NodeMarkupTool.MouseRayValid ? BorderLines.FirstOrDefault(i => i.IntersectRay(NodeMarkupTool.MouseRay)) : null;
        public override string GetToolInfo()
        {
            return SelectPanel.Position switch
            {
                BorderPosition.Right => Localize.CrosswalkEditor_InfoSelectRightBorder,
                BorderPosition.Left => Localize.CrosswalkEditor_InfoSelectLeftBorder,
                _ => null,
            };
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            foreach (var borderLine in BorderLines)
                borderLine.Render(cameraInfo, SelectPanel.Position == BorderPosition.Left ? Colors.Green : Colors.Red);

            if (IsHoverLine)
                HoverLine.Render(cameraInfo, Colors.Hover, 1f);
        }
    }

    public class CrosswalkItem : EditableItem<MarkupCrosswalk, StyleIcon>
    {
        public override void Refresh()
        {
            base.Refresh();

            Icon.Type = Object.Style.Type;
            Icon.StyleColor = Object.Style.Color;
        }
    }
}
