using ColossalFramework.Math;
using ColossalFramework.UI;
using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class CrosswalkEditor : Editor<CrosswalkItem, MarkupCrosswalk, StyleIcon>
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
                if(IsSelectBorderPanelMode)
                    BorderLines = HoverBorderPanel.Objects.Select(i => new MarkupLineBound(i, 0.5f)).ToArray();
            }
        }
        private bool IsSelectBorderPanelMode => SelectBorderPanel != null;

        private MarkupLineBound HoverLine { get; set; }
        private bool IsHoverLine => IsSelectBorderPanelMode && HoverLine != null;

        private MarkupLineBound[] BorderLines { get; set; }

        public CrosswalkEditor()
        {
            SettingsPanel.autoLayoutPadding = new RectOffset(10, 10, 0, 0);
        }

        protected override void FillItems()
        {
            foreach (var line in Markup.Lines)
            {
                if (line is MarkupCrosswalk crosswalk)
                    AddItem(crosswalk);
            }
        }
        protected override void OnObjectSelect()
        {
            AddHeader();
            AddBordersProperties();
            AddStyleTypeProperty();
            AddStyleProperties();
            if (StyleProperties.FirstOrDefault() is ColorPropertyPanel colorProperty)
                colorProperty.OnValueChanged += (Color32 c) => SelectItem.Refresh();
        }

        #region PROPERTIES PANELS

        private void AddHeader()
        {
            var header = SettingsPanel.AddUIComponent<StyleHeaderPanel>();
            header.Init(EditObject.CrosswalkRule.Style.Type, false);
            header.OnSaveTemplate += SaveTemplate;
            header.OnSelectTemplate += SelectTemplate;
            header.OnCopy += CopyStyle;
            header.OnPaste += PasteStyle;
        }
        private void AddBordersProperties()
        {
            if (EditObject.CrosswalkRule.Style.Type == Manager.Style.StyleType.CrosswalkExistent)
                return;

            var first = EditObject.PointPair.First;
            var second = EditObject.PointPair.Second;

            var rightBorders = GetBorderLines(first.Num < second.Num ? first : second);
            if (rightBorders.Any())
                AddRightBorderProperty(rightBorders);

            var leftBorders = GetBorderLines(first.Num > second.Num ? first : second);
            if (leftBorders.Any())
                AddLeftBorderProperty(leftBorders);

            MarkupRegularLine[] GetBorderLines(MarkupPoint point)
            {
                if (point.Enter.TryGetPoint(point.Num, MarkupPoint.PointType.Enter, out MarkupPoint enterPoint))
                    return enterPoint.Markup.GetPointLines(enterPoint).OfType<MarkupRegularLine>().ToArray();
                else
                    return new MarkupRegularLine[0];
            }
        }
        private void AddRightBorderProperty(MarkupRegularLine[] borders)
        {
            RightBorder = SettingsPanel.AddUIComponent<MarkupCrosswalkSelectPropertyPanel>();
            RightBorder.Text = NodeMarkup.Localize.CrosswalkEditor_RightBorder;
            RightBorder.Position = MarkupCrosswalkSelectPropertyPanel.BorderPosition.Right;
            RightBorder.Init();
            RightBorder.AddRange(borders);
            RightBorder.SelectedObject = EditObject.CrosswalkRule.RightBorder;
            RightBorder.OnSelectChanged += (MarkupRegularLine line) => EditObject.CrosswalkRule.RightBorder = line;
            RightBorder.OnSelect += SelectBorder;
            RightBorder.OnHover += HoverBorder;
            RightBorder.OnLeave += LeaveBorder;
        }
        private void AddLeftBorderProperty(MarkupRegularLine[] borders)
        {
            LeftBorder = SettingsPanel.AddUIComponent<MarkupCrosswalkSelectPropertyPanel>();
            LeftBorder.Text = NodeMarkup.Localize.CrosswalkEditor_LeftBorder;
            LeftBorder.Position = MarkupCrosswalkSelectPropertyPanel.BorderPosition.Right;
            LeftBorder.Init();
            LeftBorder.AddRange(borders);
            LeftBorder.SelectedObject = EditObject.CrosswalkRule.RightBorder;
            LeftBorder.OnSelectChanged += (MarkupRegularLine line) => EditObject.CrosswalkRule.LeftBorder = line;
            LeftBorder.OnSelect += SelectBorder;
            LeftBorder.OnHover += HoverBorder;
            LeftBorder.OnLeave += LeaveBorder;
        }

        private void AddStyleTypeProperty()
        {
            Style = SettingsPanel.AddUIComponent<CrosswalkPropertyPanel>();
            Style.Text = NodeMarkup.Localize.LineEditor_Style;
            Style.Init();
            Style.SelectedObject = EditObject.CrosswalkRule.Style.Type;
            Style.OnSelectObjectChanged += StyleChanged;
        }
        private void AddStyleProperties() => StyleProperties = EditObject.CrosswalkRule.Style.GetUIComponents(EditObject, SettingsPanel, isTemplate: true);

        #endregion

        private void SaveTemplate()
        {
            if (TemplateManager.AddTemplate(EditObject.CrosswalkRule.Style, out StyleTemplate template))
                NodeMarkupPanel.EditTemplate(template);
        }
        private void ApplyStyle(CrosswalkStyle style)
        {
            if ((EditObject.CrosswalkRule.Style.Type & Manager.Style.StyleType.GroupMask) != (style.Type & Manager.Style.StyleType.GroupMask))
                return;

            EditObject.CrosswalkRule.Style = style.CopyCrosswalkStyle();
            Style.SelectedObject = EditObject.CrosswalkRule.Style.Type;

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
            if (EarlyAccess.CheckFunctionAccess(NodeMarkup.Localize.EarlyAccess_Function_CopyStyle))
                Buffer = EditObject.CrosswalkRule.Style.CopyCrosswalkStyle();
        }
        private void PasteStyle()
        {
            if (EarlyAccess.CheckFunctionAccess(NodeMarkup.Localize.EarlyAccess_Function_PasteStyle) && Buffer is CrosswalkStyle style)
                ApplyStyle(style);
        }

        private void StyleChanged(Style.StyleType style)
        {
            if (style == EditObject.CrosswalkRule.Style.Type)
                return;

            var newStyle = TemplateManager.GetDefault<CrosswalkStyle>(style);
            EditObject.CrosswalkRule.Style.CopyTo(newStyle);

            EditObject.CrosswalkRule.Style = newStyle;

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
        protected override void OnObjectDelete(MarkupCrosswalk crosswalk) => Markup.RemoveConnect(crosswalk);

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
        public override void OnUpdate() => HoverLine = NodeMarkupTool.MouseRayValid ? BorderLines.FirstOrDefault(i => i.IsIntersect(NodeMarkupTool.MouseRay)) : null;
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
                    NodeMarkupTool.RenderBezier(cameraInfo, Color.red, borderLine.Bezier);

                if (IsHoverLine)
                    NodeMarkupTool.RenderBezier(cameraInfo, Color.white, HoverLine.Bezier, 1f);
            }
            else
            {
                if (IsHoverItem)
                {
                    var bezier = new Bezier3()
                    {
                        a = HoverItem.Object.Start.Position,
                        b = HoverItem.Object.End.Position,
                        c = HoverItem.Object.Start.Position,
                        d = HoverItem.Object.End.Position,
                    };
                    NodeMarkupTool.RenderBezier(cameraInfo, Color.white, bezier, 3f);
                }
                if(IsHoverBorderPanel && HoverBorderPanel.SelectedObject is MarkupRegularLine borderLine)
                    NodeMarkupTool.RenderBezier(cameraInfo, Color.white, borderLine.Trajectory);
            }
        }
        public override string GetInfo()
        {
            if (IsSelectBorderPanelMode)
            {
                switch (SelectBorderPanel.Position)
                {
                    case MarkupCrosswalkSelectPropertyPanel.BorderPosition.Right:
                        return NodeMarkup.Localize.CrosswalkEditor_InfoSelectRightBorder;
                    case MarkupCrosswalkSelectPropertyPanel.BorderPosition.Left:
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

        public override string Description => NodeMarkup.Localize.CrossWalkEditor_ItemDescription;
        protected override void OnObjectSet() => SetIcon();
        public override void Refresh()
        {
            base.Refresh();
            SetIcon();
        }
        private void SetIcon()
        {
            Icon.Type = Object.CrosswalkRule.Style.Type;
            Icon.StyleColor = Object.CrosswalkRule.Style.Color;
        }
    }
}
