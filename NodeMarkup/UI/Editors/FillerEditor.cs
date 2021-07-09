using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NodeMarkup.Manager;
using NodeMarkup.Tools;
using NodeMarkup.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeMarkup.UI.Editors
{
    public class FillerEditor : SimpleEditor<FillerItemsPanel, MarkupFiller>
    {
        #region PROPERTIES

        public override string Name => NodeMarkup.Localize.FillerEditor_Fillers;
        public override string EmptyMessage => string.Format(NodeMarkup.Localize.FillerEditor_EmptyMessage, NodeMarkupTool.AddFillerShortcut.ToString());
        public override Type SupportType { get; } = typeof(ISupportFillers);

        public StylePropertyPanel Style { get; private set; }
        private List<EditorItem> StyleProperties { get; set; } = new List<EditorItem>();

        private FillerRailToolMode FillerRailToolMode { get; }

        public FillerRailSelectPropertyPanel HoverRailPanel { get; private set; }

        #endregion

        #region BASIC

        public FillerEditor()
        {
            FillerRailToolMode = Tool.CreateToolMode<FillerRailToolMode>();
            FillerRailToolMode.Init(this);
        }
        protected override IEnumerable<MarkupFiller> GetObjects() => Markup.Fillers;

        protected override void OnFillPropertiesPanel(MarkupFiller filler)
        {
            AddHeader();
            AddStyleTypeProperty();
            AddStyleProperties();
        }
        protected override void OnObjectDelete(MarkupFiller filler)
        {
            Markup.RemoveFiller(filler);
            base.OnObjectDelete(filler);
        }
        protected override void OnClear()
        {
            base.OnClear();

            Style = null;
            StyleProperties.Clear();
        }

        private void AddHeader()
        {
            var header = ComponentPool.Get<StyleHeaderPanel>(PropertiesPanel, "Header");
            header.Init(Manager.Style.StyleType.Filler, SelectTemplate, false);
            header.OnSaveTemplate += SaveTemplate;
            header.OnCopy += CopyStyle;
            header.OnPaste += PasteStyle;
        }
        private void AddStyleTypeProperty()
        {
            Style = ComponentPool.Get<FillerStylePropertyPanel>(PropertiesPanel, nameof(Style));
            Style.Text = NodeMarkup.Localize.Editor_Style;
            Style.Init();
            Style.UseWheel = true;
            Style.WheelTip = true;
            Style.SelectedObject = EditObject.Style.Value.Type;
            Style.OnSelectObjectChanged += StyleChanged;
        }
        private void AddStyleProperties()
        {
            StyleProperties = EditObject.Style.Value.GetUIComponents(EditObject, PropertiesPanel);

            foreach (var property in StyleProperties)
            {
                if (property is ColorPropertyPanel colorProperty)
                    colorProperty.OnValueChanged += (Color32 c) => RefreshSelectedItem();
                else if (property is FillerRailSelectPropertyPanel railProperty)
                {
                    railProperty.OnSelect += (panel) => SelectRail(panel);
                    railProperty.OnEnter += HoverRail;
                    railProperty.OnLeave += LeaveRail;
                }
            }
        }
        private void ClearStyleProperties()
        {
            foreach (var property in StyleProperties)
                ComponentPool.Free(property);

            StyleProperties.Clear();
        }

        #endregion

        #region STYLE CHANGE

        private void StyleChanged(Style.StyleType style)
        {
            if (style == EditObject.Style.Value.Type)
                return;

            var newStyle = SingletonManager<StyleTemplateManager>.Instance.GetDefault<FillerStyle>(style);
            EditObject.Style.Value.CopyTo(newStyle);
            EditObject.Style.Value = newStyle;

            AfterStyleChanged();
        }
        private void AfterStyleChanged()
        {
            RefreshSelectedItem();
            PropertiesPanel.StopLayout();
            ClearStyleProperties();
            AddStyleProperties();
            PropertiesPanel.StartLayout();
        }

        private void ApplyStyle(FillerStyle style)
        {
            EditObject.Style.Value = style.CopyStyle();
            Style.SelectedObject = EditObject.Style.Value.Type;

            AfterStyleChanged();
        }

        #endregion

        #region HANDLERS

        private void SaveTemplate()
        {
            if (SingletonManager<StyleTemplateManager>.Instance.AddTemplate(EditObject.Style, out StyleTemplate template))
                Panel.EditStyleTemplate(template);
        }
        private void SelectTemplate(StyleTemplate template)
        {
            if (template.Style is FillerStyle style)
                ApplyStyle(style);
        }
        private void CopyStyle() => Tool.ToStyleBuffer(Manager.Style.StyleType.Filler, EditObject.Style.Value);
        private void PasteStyle()
        {
            if (Tool.FromStyleBuffer<FillerStyle>(Manager.Style.StyleType.Filler, out var style))
                ApplyStyle(style);
        }

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
            ItemsPanel.HoverObject?.Render(new OverlayData(cameraInfo) { Color = Colors.Hover });

            if (HoverRailPanel != null)
            {
                var rail = EditObject.Contour.GetRail(HoverRailPanel.Value.A, HoverRailPanel.Value.B, HoverRailPanel.OtherRail.Value.A, HoverRailPanel.OtherRail.Value.B);
                rail.Render(new OverlayData(cameraInfo) { Color = Colors.Hover });
            }
        }

        #endregion
    }
    public class FillerItemsPanel : ItemsPanel<FillerItem, MarkupFiller>
    {
        public override int Compare(MarkupFiller x, MarkupFiller y) => 0;
    }
    public class FillerItem : EditItem<MarkupFiller, StyleIcon>
    {
        public override void Refresh()
        {
            base.Refresh();

            Icon.Type = Object.Style.Value.Type;
            Icon.StyleColor = Object.Style.Value.Color;
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
            LineSelector = new LinesSelector<RailBound>(Contour.TrajectoriesProcessed.Select((t, i) => new RailBound(t, 0.5f, i)), Colors.Orange);
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
                SetValue(e, Contour.IndexOfProcessed(FirstPoint), Contour.IndexOfProcessed(PointsSelector.HoverPoint));
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
            SelectPanel.Value = new FillerRail(a % Contour.ProcessedCount, b % Contour.ProcessedCount);
            if (AfterSelectPanel?.Invoke(e) ?? true)
                Tool.SetDefaultMode();
        }
        public override string GetToolInfo() => !IsFirstSelected ? Localize.FillerEditor_InfoSelectRailFirst : Localize.FillerEditor_InfoSelectRailSecond;

        private PointsSelector<IFillerVertex> GetPointsSelector(IFillerVertex ignore = null)
        {
            if (ignore != null)
                ignore = ignore.ProcessedVertex;

            return new PointsSelector<IFillerVertex>(Contour.RawVertices.Where(v => ignore == null || !v.ProcessedVertex.Equals(ignore)), Colors.Purple);
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (!IsFirstSelected)
            {
                var overlayData = new OverlayData(cameraInfo) { Color = Colors.Hover };
                foreach (var part in Contour.RawParts)
                {
                    if (part.IsPoint)
                        part.Render(overlayData);
                }
                LineSelector.Render(cameraInfo, !(PointsSelector.IsHoverGroup || PointsSelector.IsHoverPoint));
            }
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
