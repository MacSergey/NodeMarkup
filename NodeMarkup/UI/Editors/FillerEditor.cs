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

            foreach (var property in StyleProperties)
            {
                if (property is ColorPropertyPanel colorProperty)
                    colorProperty.OnValueChanged += (Color32 c) => RefreshItem();
                else if (property is FillerRailSelectPropertyPanel railProperty)
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

            AfterStyleChanged();
        }
        private void AfterStyleChanged()
        {
            RefreshItem();
            PropertiesPanel.StopLayout();
            ClearStyleProperties();
            AddStyleProperties();
            PropertiesPanel.StartLayout();
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

            AfterStyleChanged();
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

            if (IsHoverRailPanel)
            {
                var rail = EditObject.Contour.GetRail(HoverRailPanel.Value.A, HoverRailPanel.Value.B, HoverRailPanel.OtherRail.Value.A, HoverRailPanel.OtherRail.Value.B);
                rail.Render(cameraInfo, Colors.Hover);
            }
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
        protected override FillerRail Hover => throw new NotImplementedException();
        protected override bool IsHover => throw new NotImplementedException();

        private IFillerVertex FirstPoint { get; set; }
        private bool IsFirstSelected => FirstPoint != null;

        public FillerContour Contour { get; set; }
        private PointsSelector<IFillerVertex> PointsSelector { get; set; }
        private LinesSelector<RailBound> LineSelector { get; set; }

        protected override void OnSetPanel()
        {
            FirstPoint = null;
            PointsSelector = GetPointsSelector();
            LineSelector = new LinesSelector<RailBound>(Contour.Trajectories.Select((t, i) => new RailBound(t, 0.5f, i)), Colors.Orange);
        }
        public override void OnToolUpdate()
        {
            if (!IsFirstSelected)
                LineSelector.OnUpdate();
            PointsSelector.OnUpdate();
        }

        public override void OnPrimaryMouseClicked(Event e)
        {
            if (!IsFirstSelected)
            {
                if (PointsSelector.IsHoverPoint)
                {
                    FirstPoint = PointsSelector.HoverPoint;
                    PointsSelector = GetPointsSelector(FirstPoint);
                }
                else if (LineSelector.IsHoverLine)
                    SetValue(e, LineSelector.HoverLine.Index, LineSelector.HoverLine.Index + 1);
            }
            else if (PointsSelector.IsHoverPoint)
            {
                var vertices = Contour.Vertices.ToList();
                SetValue(e, vertices.IndexOf(FirstPoint), vertices.IndexOf(PointsSelector.HoverPoint));
            }
        }
        public override void OnSecondaryMouseClicked()
        {
            if (IsFirstSelected)
            {
                FirstPoint = null;
                PointsSelector = GetPointsSelector();
            }
            else
                base.OnSecondaryMouseClicked();
        }
        private void SetValue(Event e, int a, int b)
        {
            SelectPanel.Value = new FillerRail(a % Contour.VertexCount, b % Contour.VertexCount);
            if (AfterSelectPanel?.Invoke(e) ?? true)
                Tool.SetDefaultMode();
        }
        public override string GetToolInfo() => !IsFirstSelected ? Localize.FillerEditor_InfoSelectRailFirst : Localize.FillerEditor_InfoSelectRailSecond;

        private PointsSelector<IFillerVertex> GetPointsSelector(object ignore = null) => new PointsSelector<IFillerVertex>(Contour.Vertices.Where(v => v != ignore), Colors.Purple);

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (!IsFirstSelected)
                LineSelector.Render(cameraInfo, !(PointsSelector.IsHoverGroup || PointsSelector.IsHoverPoint));
            PointsSelector.Render(cameraInfo);
        }

        private class RailBound : TrajectoryBound
        {
            public int Index { get; }
            public RailBound(ITrajectory trajectory, float size, int index) : base(trajectory, size)
            {
                Index = index;
            }
        }
    }
}
