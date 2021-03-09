using ColossalFramework.UI;
using ModsCommon.UI;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using NodeMarkup.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace NodeMarkup.UI.Editors
{
    public class FillerEditor : Editor<FillerItem, MarkupFiller, StyleIcon>
    {
        protected override bool UseGroupPanel => true;

        private static FillerStyle Buffer { get; set; }

        public override string Name => NodeMarkup.Localize.FillerEditor_Fillers;
        public override string EmptyMessage => string.Format(NodeMarkup.Localize.FillerEditor_EmptyMessage, NodeMarkupTool.AddFillerShortcut.ToString());
        public override Type SupportType { get; } = typeof(ISupportFillers);

        public StylePropertyPanel Style { get; private set; }
        private List<EditorItem> StyleProperties { get; set; } = new List<EditorItem>();

        private FillerRailToolMode FillerRailToolMode { get; }

        public FillerRailSelectPropertyPanel HoverRailPanel { get; private set; }
        public bool IsHoverRailPanel => HoverRailPanel != null;

        public FillerEditor()
        {
            FillerRailToolMode = Tool.CreateToolMode<FillerRailToolMode>();
            FillerRailToolMode.Init(this);
        }

        protected override void FillItems()
        {
            foreach (var filler in Markup.Fillers)
                AddItem(filler);
        }
        protected override void OnObjectSelect()
        {
            AddHeader();
            AddStyleTypeProperty();
            AddStyleProperties();
            SetEven();
        }
        protected override void OnClear()
        {
            Style = null;
            StyleProperties.Clear();
        }

        private void AddHeader()
        {
            var header = ComponentPool.Get<StyleHeaderPanel>(PropertiesPanel);
            header.Init(Manager.Style.StyleType.Filler, OnSelectTemplate, false);
            header.OnSaveTemplate += OnSaveTemplate;
            header.OnCopy += CopyStyle;
            header.OnPaste += PasteStyle;
        }
        private void AddStyleTypeProperty()
        {
            Style = ComponentPool.Get<FillerStylePropertyPanel>(PropertiesPanel);
            Style.Text = NodeMarkup.Localize.Editor_Style;
            Style.Init();
            Style.SelectedObject = EditObject.Style.Type;
            Style.OnSelectObjectChanged += StyleChanged;
        }
        private void AddStyleProperties()
        {
            StyleProperties = EditObject.Style.GetUIComponents(EditObject, PropertiesPanel);

            foreach(var property in StyleProperties)
            {
                if(property is ColorPropertyPanel colorProperty)
                    colorProperty.OnValueChanged += (Color32 c) => RefreshItem();
                else if(property is FillerRailSelectPropertyPanel railProperty)
                {
                    railProperty.OnSelect += (panel) => SelectRail(panel);
                    railProperty.OnHover += HoverRail;
                    railProperty.OnLeave += LeaveRail;
                }
            }
        }
        private void StyleChanged(Style.StyleType style)
        {
            if (style == EditObject.Style.Type)
                return;

            var newStyle = TemplateManager.StyleManager.GetDefault<FillerStyle>(style);
            EditObject.Style.CopyTo(newStyle);

            EditObject.Style = newStyle;

            RefreshItem();
            ClearStyleProperties();
            AddStyleProperties();
            SetEven();
        }

        private void OnSaveTemplate()
        {
            if (TemplateManager.StyleManager.AddTemplate(EditObject.Style, out StyleTemplate template))
                Panel.EditStyleTemplate(template);
        }
        private void ApplyStyle(FillerStyle style)
        {
            var newStyle = style.CopyStyle();

            newStyle.MedianOffset.Value = EditObject.Style.MedianOffset;
            if (newStyle is IRotateFiller newSimple && EditObject.Style is IRotateFiller oldSimple)
                newSimple.Angle.Value = oldSimple.Angle;

            EditObject.Style = newStyle;
            Style.SelectedObject = EditObject.Style.Type;

            RefreshItem();
            ClearStyleProperties();
            AddStyleProperties();
            SetEven();
        }
        private void OnSelectTemplate(StyleTemplate template)
        {
            if (template.Style is FillerStyle style)
                ApplyStyle(style);
        }
        private void CopyStyle() => Buffer = EditObject.Style.CopyStyle();
        private void PasteStyle()
        {
            if (Buffer is FillerStyle style)
                ApplyStyle(style);
        }
        private void ClearStyleProperties()
        {
            foreach (var property in StyleProperties)
                ComponentPool.Free(property);

            StyleProperties.Clear();
        }

        #region EDITOR ACTION
        public void HoverRail(FillerRailSelectPropertyPanel selectPanel) => HoverRailPanel = selectPanel;
        public void LeaveRail(FillerRailSelectPropertyPanel selectPanel) => HoverRailPanel = null;
        public bool SelectRail(FillerRailSelectPropertyPanel selectPanel) => SelectRail(selectPanel, null);
        public bool SelectRail(FillerRailSelectPropertyPanel selectPanel, Func<Event, bool> afterAction)
        {
            if (Tool.Mode == FillerRailToolMode && selectPanel == FillerRailToolMode.SelectPanel)
            {
                Tool.SetDefaultMode();
                return true;
            }
            else
            {
                Tool.SetMode(FillerRailToolMode);
                FillerRailToolMode.Contour = EditObject.Contour;
                FillerRailToolMode.SelectPanel = selectPanel;
                FillerRailToolMode.AfterSelectPanel = afterAction;              
                selectPanel.Focus();
                return false;
            }
        }

        public override void Render(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHoverItem)
                HoverItem.Object.Render(cameraInfo, Colors.Hover);
        }
        private void RefreshItem() => SelectItem.Refresh();
        protected override void OnObjectDelete(MarkupFiller filler) => Markup.RemoveFiller(filler);

        #endregion
    }
    public class FillerItem : EditableItem<MarkupFiller, StyleIcon>
    {
        public override void Refresh()
        {
            base.Refresh();

            Icon.Type = Object.Style.Type;
            Icon.StyleColor = Object.Style.Color;
        }
    }

    public class FillerRailToolMode : BasePanelMode<FillerEditor, FillerRailSelectPropertyPanel, FillerRail>
    {
        protected override bool IsHover => throw new NotImplementedException();
        protected override FillerRail Hover => throw new NotImplementedException();

        public FillerContour Contour { get; set; }
        private PointsSelector<IFillerVertex> PointsSelector { get; set; }
        private LinesSelector<TrajectoryBound> LineSelector { get; set; }

        protected override void OnSetPanel()
        {
            PointsSelector = new PointsSelector<IFillerVertex>(Contour.Vertices, Colors.Purple);
            LineSelector = new LinesSelector<TrajectoryBound>(Contour.Trajectories.Select(t => new TrajectoryBound(t, 0.5f)), Colors.Orange);
        }
        public override void OnToolUpdate()
        {
            PointsSelector.OnUpdate();
            LineSelector.OnUpdate();
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            LineSelector.Render(cameraInfo, !PointsSelector.IsHoverPoint);
            PointsSelector.Render(cameraInfo);
        }
    }
}
