using ColossalFramework.UI;
using NodeMarkup.Manager;
using NodeMarkup.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class CrosswalksEditor : Editor<CrosswalkItem, MarkupCrosswalk, StyleIcon>
    {
        private static CrosswalkStyle Buffer { get; set; }
        public override string Name => NodeMarkup.Localize.CrosswalkEditor_Crosswalks;
        public override string EmptyMessage => NodeMarkup.Localize.CrosswalkEditor_EmptyMessage;
        private List<UIComponent> StyleProperties { get; set; } = new List<UIComponent>();
        private MarkupCrosswalkSelectPropertyPanel RightBorder { get; set; }
        private MarkupCrosswalkSelectPropertyPanel LeftBorder { get; set; }
        private StylePropertyPanel Style { get; set; }

        private MarkupCrosswalkSelectPropertyPanel HoverBorderPanel { get; set; }
        private bool IsHoverBorderPanel => HoverBorderPanel != null;

        private MarkupCrosswalkSelectPropertyPanel _selectBorderPanel;
        private MarkupCrosswalkSelectPropertyPanel SelectBorderPanel
        {
            get => _selectBorderPanel;
            set
            {
                BorderLines = null;

                _selectBorderPanel = value;
                if (IsSelectBorderPanelMode)
                    BorderLines = HoverBorderPanel.Objects.Select(i => new MarkupLineBound(i, 0.5f)).ToArray();
            }
        }
        private bool IsSelectBorderPanelMode => SelectBorderPanel != null;

        private MarkupLineBound HoverLine { get; set; }
        private bool IsHoverLine => IsSelectBorderPanelMode && HoverLine != null;

        private MarkupLineBound[] BorderLines { get; set; }

        public CrosswalksEditor()
        {
            SettingsPanel.autoLayoutPadding = new RectOffset(10, 10, 0, 0);
        }

        protected override void FillItems()
        {
            foreach (var crosswalk in Markup.Crosswalks)
                AddItem(crosswalk);
        }
        protected override void OnObjectSelect()
        {
            AddHeader();
            AddBordersProperties();
            AddStyleTypeProperty();
            AddStyleProperties();
            if (StyleProperties.OfType<ColorPropertyPanel>().FirstOrDefault() is ColorPropertyPanel colorProperty)
                colorProperty.OnValueChanged += (Color32 c) => RefreshItem();
        }

        #region PROPERTIES PANELS

        private void AddHeader()
        {
            var header = SettingsPanel.AddUIComponent<StyleHeaderPanel>();
            header.Init(EditObject.Style.Type, false);
            header.OnSaveTemplate += SaveTemplate;
            header.OnSelectTemplate += SelectTemplate;
            header.OnCopy += CopyStyle;
            header.OnPaste += PasteStyle;
        }
        private void AddBordersProperties()
        {
            AddRightBorderProperty();
            AddLeftBorderProperty();
            FillBorders();
        }
        private void FillBorders()
        {
            FillBorder(RightBorder, RightBorgerChanged, GetBorderLines(BorderPosition.Right), EditObject.RightBorder);
            FillBorder(LeftBorder, LeftBorgerChanged, GetBorderLines(BorderPosition.Left), EditObject.LeftBorder);
        }
        private MarkupRegularLine[] GetBorderLines(BorderPosition border)
        {
            var point = border == BorderPosition.Right ^ EditObject.Line.IsInvert ? EditObject.Line.Start : EditObject.Line.End;
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
            panel.isVisible = lines.Any();
            panel.OnSelectChanged += action;
        }
        private void AddRightBorderProperty()
        {
            RightBorder = SettingsPanel.AddUIComponent<MarkupCrosswalkSelectPropertyPanel>();
            RightBorder.Text = NodeMarkup.Localize.CrosswalkEditor_RightBorder;
            RightBorder.Position = BorderPosition.Right;
            RightBorder.Init();
            RightBorder.OnSelect += SelectBorder;
            RightBorder.OnHover += HoverBorder;
            RightBorder.OnLeave += LeaveBorder;
        }
        private void AddLeftBorderProperty()
        {
            LeftBorder = SettingsPanel.AddUIComponent<MarkupCrosswalkSelectPropertyPanel>();
            LeftBorder.Text = NodeMarkup.Localize.CrosswalkEditor_LeftBorder;
            LeftBorder.Position = BorderPosition.Left;
            LeftBorder.Init();
            LeftBorder.OnSelect += SelectBorder;
            LeftBorder.OnHover += HoverBorder;
            LeftBorder.OnLeave += LeaveBorder;
        }
        private void RightBorgerChanged(MarkupRegularLine line) => EditObject.RightBorder = line;
        private void LeftBorgerChanged(MarkupRegularLine line) => EditObject.LeftBorder = line;

        private void AddStyleTypeProperty()
        {
            Style = SettingsPanel.AddUIComponent<CrosswalkPropertyPanel>();
            Style.Text = NodeMarkup.Localize.LineEditor_Style;
            Style.Init();
            Style.SelectedObject = EditObject.Style.Type;
            Style.OnSelectObjectChanged += StyleChanged;
        }
        private void AddStyleProperties() => StyleProperties = EditObject.Style.GetUIComponents(EditObject, SettingsPanel, isTemplate: true);

        #endregion

        private void SaveTemplate()
        {
            if (TemplateManager.AddTemplate(EditObject.Style, out StyleTemplate template))
                NodeMarkupPanel.EditTemplate(template);
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
        private void CopyStyle()
        {
                Buffer = EditObject.Style.CopyCrosswalkStyle();
        }
        private void PasteStyle()
        {
            if (Buffer is CrosswalkStyle style)
                ApplyStyle(style);
        }

        private void StyleChanged(Style.StyleType style)
        {
            if (style == EditObject.Style.Type)
                return;

            var newStyle = TemplateManager.GetDefault<CrosswalkStyle>(style);
            EditObject.Style.CopyTo(newStyle);

            EditObject.Style = newStyle;

            RefreshItem();
            ClearStyleProperties();
            AddStyleProperties();
        }
        private void ClearStyleProperties()
        {
            foreach (var property in StyleProperties)
            {
                RemoveUIComponent(property);
                Destroy(property);
            }
        }
        protected override void OnObjectDelete(MarkupCrosswalk crosswalk) => Markup.RemoveCrosswalk(crosswalk);
        protected override void OnObjectUpdate() => FillBorders();
        public void RefreshItem() => SelectItem.Refresh();

        #region EDITOR ACTION

        public void HoverBorder(MarkupCrosswalkSelectPropertyPanel selectPanel) => HoverBorderPanel = selectPanel;
        public void LeaveBorder(MarkupCrosswalkSelectPropertyPanel selectPanel) => HoverBorderPanel = null;

        public void SelectBorder(MarkupCrosswalkSelectPropertyPanel selectPanel)
        {
            if (IsSelectBorderPanelMode)
            {
                var isToggle = SelectBorderPanel == selectPanel;
                NodeMarkupPanel.EndEditorAction();
                if (isToggle)
                    return;
            }
            NodeMarkupPanel.StartEditorAction(this, out bool isAccept);
            if (isAccept)
            {
                selectPanel.Focus();
                SelectBorderPanel = selectPanel;
            }
        }
        public override void OnUpdate() => HoverLine = NodeMarkupTool.MouseRayValid ? BorderLines.FirstOrDefault(i => i.IntersectRay(NodeMarkupTool.MouseRay)) : null;
        public override void OnPrimaryMouseClicked(Event e, out bool isDone)
        {
            if (IsHoverLine)
            {
                SelectBorderPanel.SelectedObject = HoverLine?.Line as MarkupRegularLine;
                isDone = true;
            }
            else
                isDone = false;
        }
        public override void Render(RenderManager.CameraInfo cameraInfo)
        {
            if (IsSelectBorderPanelMode)
            {
                foreach (var borderLine in BorderLines)
                    NodeMarkupTool.RenderTrajectory(cameraInfo, MarkupColors.Red, borderLine.Trajectory);

                if (IsHoverLine)
                    NodeMarkupTool.RenderTrajectory(cameraInfo, MarkupColors.White, HoverLine.Trajectory, 1f);
            }
            else
            {
                if (IsHoverItem)
                    HoverItem.Object.Render(cameraInfo, MarkupColors.White);

                if (IsHoverBorderPanel && HoverBorderPanel.SelectedObject is MarkupRegularLine borderLine)
                    NodeMarkupTool.RenderTrajectory(cameraInfo, MarkupColors.White, borderLine.Trajectory);
            }
        }
        public override string GetInfo()
        {
            if (IsSelectBorderPanelMode)
            {
                switch (SelectBorderPanel.Position)
                {
                    case BorderPosition.Right:
                        return NodeMarkup.Localize.CrosswalkEditor_InfoSelectRightBorder;
                    case BorderPosition.Left:
                        return NodeMarkup.Localize.CrosswalkEditor_InfoSelectLeftBorder;
                }
            }

            return base.GetInfo();
        }
        public override void EndEditorAction()
        {
            if (IsSelectBorderPanelMode)
                SelectBorderPanel = null;
        }

        #endregion
    }

    public class CrosswalkItem : EditableItem<MarkupCrosswalk, StyleIcon>
    {
        public override void Init() => Init(true, true);

        public override string DeleteCaptionDescription => NodeMarkup.Localize.CrossWalkEditor_DeleteCaptionDescription;
        public override string DeleteMessageDescription => NodeMarkup.Localize.CrossWalkEditor_DeleteMessageDescription;
        protected override void OnObjectSet() => SetIcon();
        public override void Refresh()
        {
            base.Refresh();
            SetIcon();
        }
        private void SetIcon()
        {
            Icon.Type = Object.Style.Type;
            Icon.StyleColor = Object.Style.Color;
        }
    }
}
